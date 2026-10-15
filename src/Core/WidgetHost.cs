/*
	Icod.Widgets
	Reusable terminal UI controls for .NET.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU Lesser General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU Lesser General Public License for more details.

	You should have received a copy of the GNU Lesser General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.Widgets;

using Icod.DCurses;

/// <summary>Owns widget presentation and interaction registrations for one DCurses screen.</summary>
public sealed partial class WidgetHost : IDisposable {
	private readonly CursesInteractionRouter router;
	private readonly List<Widget> roots = [];
	private readonly Dictionary<Widget, CursesInteractionRegion> regions = [];
	private readonly Dictionary<CursesInteractionRegion, Widget> widgetsByRegion = [];
	private readonly Queue<WidgetAction> actions = [];
	private int attachedCount;
	private int nextTraversalOrder;
	private TimeSpan? currentTime;
	private Widget? pointerCapture;
	private bool disposed;
	private CursesInteractionScope? attachmentScope;

	public WidgetHost( CursesSession session, WidgetTheme theme )
		: this( ( session ?? throw new ArgumentNullException( nameof( session ) ) ).Screen, theme ) { }

	public WidgetHost( CursesScreen screen, WidgetTheme theme ) {
		this.Screen = screen ?? throw new ArgumentNullException( nameof( screen ) );
		this.Theme = theme ?? throw new ArgumentNullException( nameof( theme ) );
		this.router = new CursesInteractionRouter( screen );
	}

	public CursesScreen Screen { get; }
	public WidgetTheme Theme { get; private set; }
	/// <summary>Replaces the immutable theme; the application repaints on its next render/refresh.</summary>
	public void SetTheme( WidgetTheme theme ) {
		this.ThrowIfDisposed(); this.Theme = theme ?? throw new ArgumentNullException( nameof( theme ) );
	}
	public int AttachedCount => this.attachedCount;
	public TimeSpan CurrentTime => this.currentTime ?? TimeSpan.Zero;

	public Widget? FocusedWidget {
		get {
			this.ThrowIfDisposed();
			CursesInteractionRegion? region = this.router.FocusedRegion;
			return region is not null && this.widgetsByRegion.TryGetValue( region, out Widget? widget ) ? widget : null;
		}
	}

	public void Attach( Widget widget ) {
		ArgumentNullException.ThrowIfNull( widget );
		this.ThrowIfDisposed();
		if ( widget.Parent is not null || widget.Host is not null ) throw new InvalidOperationException( "The widget already belongs to a tree." );
		Widget[] subtree = widget.DescendantsAndSelf().ToArray();
		this.ValidateAddition( subtree );
		CursesPanel? panel = widget is WidgetDialog ? this.Screen.CreatePanel( 0, 0, this.Screen.Rows, this.Screen.Columns ) : null;
		try {
			this.attachmentPanel = panel; this.AttachValidated( subtree ); this.roots.Add( widget );
			if ( widget is WidgetDialog dialog ) { this.dialogPanels.Add( dialog, panel! ); dialog.IsOpen = true; dialog.HasCompleted = false; dialog.OnOpened(); this.Synchronize( dialog ); }
		} catch { panel?.Dispose(); throw; }
		finally { this.attachmentPanel = null; }
	}

	public bool Detach( Widget widget ) {
		ArgumentNullException.ThrowIfNull( widget );
		this.ThrowIfDisposed();
		if ( widget is WidgetDialog dialog && this.modalFrames.Any( frame => ReferenceEquals( frame.Dialog, dialog ) ) ) { this.CloseModal( dialog, WidgetDialogOutcome.Cancel ); return true; }
		if ( widget is WidgetDialog modeless && modeless.IsOpen && !modeless.HasCompleted ) { this.CompleteDialog( modeless, WidgetDialogOutcome.Cancel ); return true; }
		if ( !this.roots.Remove( widget ) ) return false;
		this.DetachValidated( widget.DescendantsAndSelf().ToArray() );
		return true;
	}

	public void Arrange( CursesRectangle bounds ) {
		this.ThrowIfDisposed();
		this.CancelPointerCapture();
		foreach ( Widget root in this.roots ) root.Arrange( bounds );
	}

	public bool Focus( Widget widget ) {
		ArgumentNullException.ThrowIfNull( widget );
		this.ThrowIfDisposed();
		if ( !ReferenceEquals( widget.Host, this ) ) throw new ArgumentException( "The widget is not attached to this host.", nameof( widget ) );
		Widget? previous = this.FocusedWidget; if ( !ReferenceEquals( previous, widget ) ) this.CancelPointerCapture();
		bool result = this.popup?.Owner == widget ? this.router.Focus( this.popup.Region ) :
			this.regions.TryGetValue( widget, out CursesInteractionRegion? region ) && this.router.Focus( region );
		if ( result && !ReferenceEquals( previous, this.FocusedWidget ) ) this.ClipboardFocusChanged( previous ); return result;
	}

	public bool MoveFocus( CursesFocusDirection direction ) {
		this.ThrowIfDisposed();
		if ( !Enum.IsDefined( direction ) ) throw new ArgumentOutOfRangeException( nameof( direction ) );
		if ( this.popup is not null ) this.DismissPopup();
		Widget? previous = this.FocusedWidget;
		CursesInteractionRegion? next = this.router.MoveFocus( direction );
		if ( next is not null && !ReferenceEquals( previous, this.FocusedWidget ) ) { this.CancelPointerCapture(); this.ClipboardFocusChanged( previous ); }
		return next is not null;
	}

	public WidgetCommandResult ExecuteEditCommand( WidgetEditCommand command, TimeSpan timestamp ) {
		if ( !Enum.IsDefined( command ) ) throw new ArgumentOutOfRangeException( nameof( command ) );
		this.SetCurrentTime( timestamp );
		if ( this.FocusedWidget is not IWidgetEditTarget target ) return WidgetCommandResult.NotApplicable;
		if ( !target.CanExecuteEditCommand( command ) ) return WidgetCommandResult.Disabled;
		WidgetCommandResult result = target.ExecuteEditCommand( command, timestamp );
		if ( !Enum.IsDefined( result ) || WidgetCommandResult.NotApplicable == result )
			throw new InvalidOperationException( "An applicable edit target must return Disabled or Executed." );
		return result;
	}

	internal void DispatchFocusedEditOrApplication( WidgetMenuItem item, string sourceWidgetId, bool? checkedValue = null ) {
		if ( item.FocusedEditCommand is { } editCommand && this.ExecuteEditCommand( editCommand, this.CurrentTime ) != WidgetCommandResult.NotApplicable ) return;
		if ( checkedValue.HasValue ) this.Enqueue( new WidgetCheckedInvokedAction( sourceWidgetId, item.CommandId!, checkedValue.Value ) );
		else this.Enqueue( new WidgetInvokedAction( sourceWidgetId, item.CommandId! ) );
	}

	public WidgetDispatchResult Process( CursesInputEvent input, TimeSpan timestamp ) => this.Process( WidgetInput.From( input ), timestamp );

	public WidgetDispatchResult Process( WidgetInput input, TimeSpan timestamp ) {
		ArgumentNullException.ThrowIfNull( input );
		this.SetCurrentTime( timestamp );
		if ( WidgetInputKind.Focus == input.Kind && CursesFocusState.Unfocused == input.FocusState ) this.CancelPointerCapture();
		if ( WidgetInputKind.Key == input.Kind && CursesKeyEventPhase.Release == input.KeyPhase )
			return new WidgetDispatchResult( false, this.DrainActions() );
		if ( WidgetInputKind.Pointer == input.Kind && input.Pointer is { } hover )
			foreach ( WidgetPopupMenu menu in this.roots.SelectMany( root => root.DescendantsAndSelf() ).OfType<WidgetPopupMenu>() ) menu.UpdateHover( hover, this.Screen );
		bool handled = false;

		if ( WidgetInputKind.Key == input.Kind && this.popup is null && this.ActiveModal is null && this.TryMenuBarInput( input ) ) handled = true;
		else if ( WidgetInputKind.Key == input.Kind && WidgetKey.Tab == input.KeyCode ) {
			CursesFocusDirection direction = 0 != ( input.Modifiers & WidgetInputModifiers.Shift )
				? CursesFocusDirection.Backward : CursesFocusDirection.Forward;
			Widget? tabTarget = this.ResolveTarget( input );
			if ( tabTarget?.AcceptsTab == true && tabTarget.IsEffectivelyEnabled && tabTarget.IsEffectivelyVisible ) handled = tabTarget.OnInput( input, this );
			else {
				if ( this.popup is not null ) this.DismissPopup();
				handled = this.ActiveModal?.OnInput( input, this ) ?? false;
				if ( !handled ) handled = this.MoveFocus( direction );
			}
		} else {
			Widget? target = this.ResolveTarget( input );
			if ( target is not null && target.IsEffectivelyEnabled && target.IsEffectivelyVisible ) handled = target.OnInput( input, this );
			if ( !handled && this.ActiveModal is { } modal && !ReferenceEquals( target, modal ) ) handled = modal.OnInput( input, this );
		}

		if ( handled && input.Kind == WidgetInputKind.Pointer && input.Pointer is { Action: CursesMouseAction.Press, Button: CursesMouseButton.Primary } && this.popup?.Owner is WidgetPopupMenu menuCapture )
			this.pointerCapture = menuCapture;
		return new WidgetDispatchResult( handled || this.ActiveModal is not null, this.DrainActions() );
	}

	public WidgetDispatchResult Update( TimeSpan timestamp ) {
		this.SetCurrentTime( timestamp );
		foreach ( Widget widget in this.roots.SelectMany( root => root.DescendantsAndSelf() ).ToArray() )
			if ( ReferenceEquals( widget.Host, this ) && widget.IsEffectivelyEnabled && widget.IsEffectivelyVisible ) widget.OnHostUpdate( timestamp );
		return new WidgetDispatchResult( this.ActiveModal is not null, this.DrainActions() );
	}

	private bool TryMenuBarInput( WidgetInput input ) {
		foreach ( WidgetMenuBar menu in this.roots.SelectMany( root => root.DescendantsAndSelf() ).OfType<WidgetMenuBar>() ) {
			if ( menu.IsEffectivelyEnabled && menu.IsEffectivelyVisible && !menu.Bounds.Intersect( this.Screen.Bounds ).IsEmpty && menu.TryGlobalInput( input, this ) ) return true;
		}
		return false;
	}

	public IReadOnlyList<WidgetAction> DrainActions() {
		this.ThrowIfDisposed();
		if ( 0 == this.actions.Count ) return Array.Empty<WidgetAction>();
		WidgetAction[] result = this.actions.ToArray();
		this.actions.Clear();
		return result;
	}

	public void Render() {
		this.ThrowIfDisposed();
		Widget? focused = this.FocusedWidget;
		foreach ( Widget root in this.roots ) {
			foreach ( Widget widget in root.DescendantsAndSelf() ) {
				if ( !widget.IsEffectivelyVisible || widget is WidgetDialog || this.DialogAncestor( widget ) is not null ) continue;
				if ( this.popup?.Owner == widget ) { if ( widget is WidgetDropDown drop ) drop.RenderSelection( this.Screen, this.Theme, ReferenceEquals( widget, focused ) ); else if ( widget is WidgetMenuBar bar ) bar.RenderBar( this.Screen, this.Theme, ReferenceEquals( widget, focused ) ); continue; }
				widget.OnRender( this.Screen, this.Theme, ReferenceEquals( widget, focused ) );
			}
		}
		foreach ( ( WidgetDialog dialog, CursesPanel panel ) in this.dialogPanels ) this.RenderPanel( dialog, panel );
		this.RenderModalBackdrop();
		if ( this.popup is { } popupFrame ) { if ( popupFrame.Owner is WidgetPopupMenu menu ) this.RenderMenuPanels( menu, popupFrame ); else this.RenderPanel( popupFrame.Owner, popupFrame.Panel ); }
	}

	internal void EnsureActionCapacity( int count = 1 ) { this.ThrowIfDisposed(); if ( count < 0 || count > WidgetLimits.MaximumPendingActions - this.actions.Count ) throw new InvalidOperationException( "The widget action queue is full." ); }
	internal void EnsureActionCapacityFor( Widget source, int count ) { this.ValidateActionSource( source ); this.EnsureActionCapacity( count ); }
	internal void EmitFor( Widget source, WidgetAction action ) {
		ArgumentNullException.ThrowIfNull( action ); this.ValidateActionSource( source );
		if ( !StringComparer.Ordinal.Equals( source.Id, action.WidgetId ) ) throw new ArgumentException( "An emitted action must identify its source widget.", nameof( action ) );
		this.EnsureActionCapacity(); this.actions.Enqueue( action );
	}

	internal void Enqueue( WidgetAction action ) {
		ArgumentNullException.ThrowIfNull( action );
		this.ThrowIfDisposed();
		if ( WidgetLimits.MaximumPendingActions <= this.actions.Count ) throw new InvalidOperationException( "The widget action queue is full." );
		this.actions.Enqueue( action );
	}

	internal void AttachChild( Widget child ) {
		this.ThrowIfDisposed();
		Widget[] subtree = child.DescendantsAndSelf().ToArray();
		this.ValidateAddition( subtree );
		this.AttachValidated( subtree );
	}

	internal void ValidateReplacement( Widget? oldContent, Widget? newContent ) {
		this.ThrowIfDisposed();
		if ( newContent is null || ReferenceEquals( oldContent, newContent ) ) return;
		if ( newContent.Parent is not null || newContent.Host is not null ) throw new InvalidOperationException( "The replacement already belongs to a tree." );
		Widget[] incoming = newContent.DescendantsAndSelf().ToArray();
		int outgoingCount = oldContent?.DescendantsAndSelf().Count() ?? 0;
		if ( incoming.Length > WidgetLimits.MaximumWidgetsPerHost - this.attachedCount + outgoingCount )
			throw new InvalidOperationException( $"A host cannot own more than {WidgetLimits.MaximumWidgetsPerHost} widgets." );
		HashSet<Widget> outgoing = oldContent is null ? [] : new( oldContent.DescendantsAndSelf() );
		HashSet<string> ids = new( this.roots.SelectMany( root => root.DescendantsAndSelf() )
			.Where( widget => !outgoing.Contains( widget ) ).Select( widget => widget.Id ), StringComparer.Ordinal );
		foreach ( Widget item in incoming ) {
			if ( item.Host is not null || !ids.Add( item.Id ) ) throw new InvalidOperationException( $"Widget id '{item.Id}' is already attached." );
		}
	}

	internal void DetachChild( Widget child ) {
		this.ThrowIfDisposed();
		this.DetachValidated( child.DescendantsAndSelf().ToArray() );
	}

	internal void Synchronize( Widget widget ) {
		if ( this.disposed ) return;
		Widget? previous = this.FocusedWidget;
		foreach ( Widget child in widget.DescendantsAndSelf().Skip( 1 ) ) this.SynchronizeRegion( child );
		this.SynchronizeRegion( widget );
		if ( !ReferenceEquals( previous, this.FocusedWidget ) ) this.ClipboardFocusChanged( previous );
	}
	private void SynchronizeRegion( Widget widget ) {
		if ( !this.regions.TryGetValue( widget, out CursesInteractionRegion? region ) ) {
			if ( !widget.IsFocusable || !ReferenceEquals( widget.Host, this ) ) return;
			region = this.router.RegisterRegion( new CursesInteractionRegionOptions( widget.Bounds.Intersect( this.Screen.Bounds ) ) {
				IsEnabled = widget.IsEffectivelyEnabled && widget.IsEffectivelyVisible && !widget.Bounds.IsEmpty,
				IsFocusable = widget.IsEffectivelyEnabled && widget.IsEffectivelyVisible && !widget.Bounds.IsEmpty,
				TraversalOrder = this.nextTraversalOrder++
			} );
			this.regions.Add( widget, region ); this.widgetsByRegion.Add( region, widget );
		}
		if ( widget is WidgetDialog dialog && this.dialogPanels.TryGetValue( dialog, out CursesPanel? panel ) ) {
			CursesRectangle clipped = widget.Bounds.Intersect( this.Screen.Bounds );
			if ( !clipped.IsEmpty ) { panel.SetBounds( clipped ); panel.Show(); } else panel.Hide();
			region.SetBounds( new CursesRectangle( 0, 0, clipped.Rows, clipped.Columns ) );
		} else region.SetBounds( widget.Bounds.Intersect( this.Screen.Bounds ) );
		region.IsEnabled = widget.IsEffectivelyEnabled && widget.IsEffectivelyVisible && !widget.Bounds.IsEmpty;
		region.IsFocusable = widget.IsFocusable && region.IsEnabled;
		if ( !region.IsEnabled && this.popup?.Owner == widget ) this.DismissPopup();
		if ( !region.IsEnabled && ReferenceEquals( this.pointerCapture, widget ) ) this.CancelPointerCapture();
	}

	private Widget? ResolveTarget( WidgetInput input ) {
		if ( WidgetInputKind.Pointer == input.Kind && input.Pointer is not null && this.pointerCapture is { } captured ) {
			if ( CursesMouseAction.Release == input.Pointer.Action ) this.pointerCapture = null;
			return captured;
		}
		if ( this.ActiveModal is { } modal ) {
			if ( WidgetInputKind.Pointer == input.Kind && input.Pointer is { } modalPointer ) {
				if ( CursesMouseAction.Release == modalPointer.Action ) return modal;
				CursesInteractionHit? hit = this.router.HitTest( modalPointer.Row, modalPointer.Column );
				if ( hit is not null && this.widgetsByRegion.TryGetValue( hit.Region, out Widget? child ) && this.DialogAncestor( child ) == modal ) {
					if ( child.IsFocusable && modalPointer.Action == CursesMouseAction.Press ) { Widget? previous = this.FocusedWidget; _ = this.router.Focus( hit.Region ); if ( !ReferenceEquals( previous, this.FocusedWidget ) ) this.ClipboardFocusChanged( previous ); }
					if ( CursesMouseAction.Press == modalPointer.Action ) this.pointerCapture = child;
					return child;
				}
				return modal;
			}
			Widget? focused = this.FocusedWidget;
			return focused is not null && this.DialogAncestor( focused ) == modal ? focused : modal;
		}
		if ( this.popup is { } frame ) {
			if ( input.Kind == WidgetInputKind.Pointer && input.Pointer is { Action: CursesMouseAction.Press } pointer && !( frame.Owner is WidgetPopupMenu popupMenu ? popupMenu.ContainsPopupPoint( pointer.Row, pointer.Column, this.Screen ) : frame.Panel.Bounds.Contains( pointer.Row, pointer.Column ) ) && !( frame.Owner is WidgetMenuBar bar && bar.BarBounds( this.Screen ).Contains( pointer.Row, pointer.Column ) ) ) { this.DismissPopup(); return null; }
			if ( input.Kind != WidgetInputKind.Pointer || frame.Owner is WidgetPopupMenu ) return frame.Owner;
		}
		if ( WidgetInputKind.Pointer == input.Kind && input.Pointer is not null ) {
			if ( CursesMouseAction.Release == input.Pointer.Action ) return null;
			CursesInteractionHit? hit = this.router.HitTest( input.Pointer.Row, input.Pointer.Column );
			if ( hit is not null && this.widgetsByRegion.TryGetValue( hit.Region, out Widget? pointerWidget ) ) {
				if ( pointerWidget is WidgetMenuBar && input.Pointer.Action == CursesMouseAction.Press ) return pointerWidget;
				if ( pointerWidget.IsFocusable && input.Pointer.Action == CursesMouseAction.Press ) { Widget? previous = this.FocusedWidget; _ = this.router.Focus( hit.Region ); if ( !ReferenceEquals( previous, this.FocusedWidget ) ) this.ClipboardFocusChanged( previous ); }
				if ( CursesMouseAction.Press == input.Pointer.Action ) this.pointerCapture = pointerWidget;
				return pointerWidget;
			}
		}
		return this.FocusedWidget;
	}
	private WidgetDialog? DialogAncestor( Widget widget ) {
		for ( Widget? current = widget; current is not null; current = current.Parent ) if ( current is WidgetDialog dialog ) return dialog;
		return null;
	}

	private void ValidateAddition( Widget[] subtree ) {
		if ( subtree.Length > WidgetLimits.MaximumWidgetsPerHost - this.attachedCount ) {
			throw new InvalidOperationException( $"A host cannot own more than {WidgetLimits.MaximumWidgetsPerHost} widgets." );
		}
		HashSet<string> ids = new( StringComparer.Ordinal );
		foreach ( Widget item in this.roots.SelectMany( root => root.DescendantsAndSelf() ) ) ids.Add( item.Id );
		foreach ( Widget item in subtree ) {
			if ( item.Host is not null || !ids.Add( item.Id ) ) throw new InvalidOperationException( $"Widget id '{item.Id}' is already attached." );
		}
	}

	private void AttachValidated( Widget[] subtree ) {
		List<Widget> completed = [];
		try {
			foreach ( Widget item in subtree ) {
				item.Host = this;
				this.RegisterAttachmentGeneration( item );
				this.attachedCount++;
				completed.Add( item );
				if ( item.IsFocusable ) {
					CursesInteractionRegion region = this.router.RegisterRegion( new CursesInteractionRegionOptions( item.Bounds ) {
						Scope = this.attachmentScope,
						Panel = this.attachmentPanel,
						IsEnabled = item.IsEffectivelyEnabled && item.IsEffectivelyVisible && !item.Bounds.IsEmpty,
						IsFocusable = item.IsEffectivelyEnabled && item.IsEffectivelyVisible && !item.Bounds.IsEmpty,
						TraversalOrder = this.nextTraversalOrder++
					} );
					this.regions.Add( item, region );
					this.widgetsByRegion.Add( region, item );
				}
			}
		} catch {
			this.DetachValidated( completed.ToArray() );
			throw;
		}
	}

	private void DetachValidated( Widget[] subtree ) {
		Widget? previousFocus = this.FocusedWidget;
		foreach ( Widget item in subtree ) {
			if ( this.popup?.Owner == item ) this.DismissPopup();
			if ( ReferenceEquals( this.pointerCapture, item ) ) this.CancelPointerCapture();
			if ( ReferenceEquals( item.Host, this ) ) item.OnDetached();
			if ( this.regions.Remove( item, out CursesInteractionRegion? region ) ) {
				this.widgetsByRegion.Remove( region );
				region.Dispose();
			}
			if ( item is WidgetDialog dialog && this.dialogPanels.Remove( dialog, out CursesPanel? panel ) ) panel.Dispose();
			if ( ReferenceEquals( item.Host, this ) ) {
				this.RemoveAttachmentGeneration( item );
				item.Host = null;
				this.attachedCount--;
			}
		}
		_ = this.router.FocusedRegion;
		if ( !ReferenceEquals( previousFocus, this.FocusedWidget ) ) this.ClipboardFocusChanged( previousFocus );
	}

	public void Dispose() {
		if ( this.disposed ) return;
		this.CancelPointerCapture();
		this.DismissPopup();
		this.ClearModalsSilently();
		this.modalBackdrop?.Dispose(); this.modalBackdrop = null; this.backdropRow = [];
		foreach ( CursesInteractionRegion region in this.regions.Values.ToArray() ) region.Dispose();
		this.regions.Clear();
		this.widgetsByRegion.Clear();
		foreach ( CursesPanel panel in this.dialogPanels.Values ) panel.Dispose(); this.dialogPanels.Clear();
		foreach ( Widget root in this.roots ) foreach ( Widget widget in root.DescendantsAndSelf() ) { widget.Host = null; if ( widget is WidgetDialog dialog ) { dialog.IsOpen = false; dialog.OnClosed(); } }
		this.roots.Clear();
		this.attachedCount = 0;
		this.actions.Clear();
		this.ClearClipboardRequests();
		this.router.Dispose();
		this.disposed = true;
	}

	internal void SynchronizeTree( Widget widget ) => this.Synchronize( widget );
	private void CancelPointerCapture() { Widget? captured = this.pointerCapture; this.pointerCapture = null; captured?.OnPointerCaptureCanceled(); }
	private void SetCurrentTime( TimeSpan timestamp ) {
		this.ThrowIfDisposed();
		if ( TimeSpan.Zero > timestamp || ( this.currentTime.HasValue && timestamp < this.currentTime.Value ) )
			throw new ArgumentOutOfRangeException( nameof( timestamp ), "Widget time must be nonnegative and monotonic." );
		this.currentTime = timestamp;
	}
	private void ValidateActionSource( Widget source ) {
		ArgumentNullException.ThrowIfNull( source ); this.ThrowIfDisposed();
		if ( !ReferenceEquals( source.Host, this ) ) throw new InvalidOperationException( "The action source is not attached to this host." );
	}
	private void ThrowIfDisposed() { if ( this.disposed ) throw new ObjectDisposedException( nameof( WidgetHost ) ); }
}
