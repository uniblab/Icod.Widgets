/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
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

	public WidgetHost( CursesSession session, WidgetTheme theme )
		: this( ( session ?? throw new ArgumentNullException( nameof( session ) ) ).Screen, theme ) { }

	public WidgetHost( CursesScreen screen, WidgetTheme theme ) {
		this.Screen = screen ?? throw new ArgumentNullException( nameof( screen ) );
		this.Theme = theme ?? throw new ArgumentNullException( nameof( theme ) );
		this.router = new CursesInteractionRouter( screen );
	}

	public CursesScreen Screen { get; }
	public WidgetTheme Theme { get; }
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
		this.AttachValidated( subtree );
		this.roots.Add( widget );
	}

	public bool Detach( Widget widget ) {
		ArgumentNullException.ThrowIfNull( widget );
		this.ThrowIfDisposed();
		if ( widget is WidgetDialog dialog && this.IsModalOpen( dialog ) ) {
			this.CloseModal( dialog, WidgetDialogOutcome.Cancel );
			return true;
		}
		if ( !this.roots.Remove( widget ) ) return false;
		this.DetachValidated( widget.DescendantsAndSelf().ToArray() );
		return true;
	}

	public void Arrange( CursesRectangle bounds ) {
		this.ThrowIfDisposed();
		foreach ( Widget root in this.roots ) root.Arrange( bounds );
	}

	public bool Focus( Widget widget ) {
		ArgumentNullException.ThrowIfNull( widget );
		this.ThrowIfDisposed();
		if ( !ReferenceEquals( widget.Host, this ) ) throw new ArgumentException( "The widget is not attached to this host.", nameof( widget ) );
		return this.regions.TryGetValue( widget, out CursesInteractionRegion? region ) && this.router.Focus( region );
	}

	public WidgetDispatchResult Process( CursesInputEvent input, TimeSpan timestamp ) => this.Process( WidgetInput.From( input ), timestamp );

	public WidgetDispatchResult Process( WidgetInput input, TimeSpan timestamp ) {
		ArgumentNullException.ThrowIfNull( input );
		this.ThrowIfDisposed();
		if ( TimeSpan.Zero > timestamp || ( this.currentTime.HasValue && timestamp < this.currentTime.Value ) ) {
			throw new ArgumentOutOfRangeException( nameof( timestamp ), "Widget time must be nonnegative and monotonic." );
		}
		this.currentTime = timestamp;
		bool handled = false;
		bool modalActive = this.ActiveModal is not null;

		if ( !modalActive && WidgetInputKind.Key == input.Kind && WidgetKey.Tab == input.KeyCode ) {
			CursesFocusDirection direction = 0 != ( input.Modifiers & WidgetInputModifiers.Shift )
				? CursesFocusDirection.Backward : CursesFocusDirection.Forward;
			handled = this.router.MoveFocus( direction ) is not null;
		} else {
			Widget? target = this.ResolveTarget( input );
			if ( target is not null && target.IsEffectivelyEnabled && target.IsEffectivelyVisible ) handled = target.OnInput( input, this );
		}
		if ( modalActive && input.Kind is not WidgetInputKind.Focus and not WidgetInputKind.EndOfInput ) handled = true;

		return new WidgetDispatchResult( handled, this.DrainActions() );
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
				if ( widget.IsEffectivelyVisible ) widget.OnRender( this.Screen, this.Theme, ReferenceEquals( widget, focused ) );
			}
		}
	}

	internal void Enqueue( WidgetAction action ) {
		ArgumentNullException.ThrowIfNull( action );
		this.ThrowIfDisposed();
		this.EnsureActionCapacity();
		this.actions.Enqueue( action );
	}

	internal void EnsureActionCapacity( int count = 1 ) {
		this.ThrowIfDisposed();
		if ( count < 0 || count > WidgetLimits.MaximumPendingActions - this.actions.Count ) throw new InvalidOperationException( "The widget action queue is full." );
	}

	internal void AttachChild( Widget child ) {
		this.ThrowIfDisposed();
		Widget[] subtree = child.DescendantsAndSelf().ToArray();
		this.ValidateAddition( subtree );
		this.AttachValidated( subtree );
	}

	internal void DetachChild( Widget child ) {
		this.ThrowIfDisposed();
		this.DetachValidated( child.DescendantsAndSelf().ToArray() );
	}

	internal void Synchronize( Widget widget ) {
		if ( this.disposed || !this.regions.TryGetValue( widget, out CursesInteractionRegion? region ) ) return;
		region.SetBounds( widget.Bounds );
		region.IsEnabled = widget.IsEffectivelyEnabled && widget.IsEffectivelyVisible && !widget.Bounds.IsEmpty;
		region.IsFocusable = widget.IsFocusable && region.IsEnabled;
		if ( !region.IsEnabled && ReferenceEquals( this.pointerCapture, widget ) ) this.CancelPointerCapture();
	}

	internal void SynchronizeTree( Widget widget ) { foreach ( Widget item in widget.DescendantsAndSelf() ) this.Synchronize( item ); }

	private Widget? ResolveTarget( WidgetInput input ) {
		if ( this.ActiveModal is not null ) return this.ActiveModal;
		if ( WidgetInputKind.Pointer == input.Kind && input.Pointer is not null ) {
			if ( CursesMouseAction.Release == input.Pointer.Action && this.pointerCapture is not null ) {
				Widget captured = this.pointerCapture; this.pointerCapture = null; return captured;
			}
			if ( CursesMouseAction.Release == input.Pointer.Action ) return null;
			CursesInteractionHit? hit = this.router.HitTest( input.Pointer.Row, input.Pointer.Column );
			if ( hit is not null && this.widgetsByRegion.TryGetValue( hit.Region, out Widget? pointerWidget ) ) {
				if ( pointerWidget.IsFocusable ) _ = this.router.Focus( hit.Region );
				if ( CursesMouseAction.Press == input.Pointer.Action ) this.pointerCapture = pointerWidget;
				return pointerWidget;
			}
		}
		return this.FocusedWidget;
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
				this.attachedCount++;
				completed.Add( item );
				if ( item.IsFocusable ) {
					CursesInteractionRegion region = this.router.RegisterRegion( new CursesInteractionRegionOptions( item.Bounds ) {
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
		foreach ( Widget item in subtree ) {
			if ( ReferenceEquals( this.pointerCapture, item ) ) this.CancelPointerCapture();
			if ( this.regions.Remove( item, out CursesInteractionRegion? region ) ) {
				this.widgetsByRegion.Remove( region );
				region.Dispose();
			}
			if ( ReferenceEquals( item.Host, this ) ) {
				item.Host = null;
				this.attachedCount--;
			}
		}
		_ = this.router.FocusedRegion;
	}

	public void Dispose() {
		if ( this.disposed ) return;
		this.ClearModalsSilently();
		foreach ( CursesInteractionRegion region in this.regions.Values.ToArray() ) region.Dispose();
		this.regions.Clear();
		this.widgetsByRegion.Clear();
		foreach ( Widget root in this.roots ) foreach ( Widget widget in root.DescendantsAndSelf() ) widget.Host = null;
		this.roots.Clear();
		this.attachedCount = 0;
		this.actions.Clear();
		this.CancelPointerCapture();
		this.router.Dispose();
		this.disposed = true;
	}

	private void ThrowIfDisposed() { if ( this.disposed ) throw new ObjectDisposedException( nameof( WidgetHost ) ); }
	private void CancelPointerCapture() { Widget? captured = this.pointerCapture; this.pointerCapture = null; captured?.OnPointerCaptureCanceled(); }
}
