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

/// <summary>Modal-stack portion of <see cref="WidgetHost"/>.</summary>
public sealed partial class WidgetHost {
	private sealed record ModalFrame( WidgetDialog Dialog, Widget? PreviousFocus, WidgetModalLease Lease, CursesInteractionScope Scope, CursesInteractionScopeLease ScopeLease );
	private readonly List<ModalFrame> modalFrames = [];

	public int ModalDepth => this.modalFrames.Count;
	internal WidgetDialog? ActiveModal => 0 == this.modalFrames.Count ? null : this.modalFrames[ ^1 ].Dialog;

	internal bool IsModalOpen( WidgetDialog dialog ) => this.modalFrames.Any( frame => ReferenceEquals( frame.Dialog, dialog ) );

	public WidgetModalLease ShowModal( WidgetDialog dialog ) {
		ArgumentNullException.ThrowIfNull( dialog ); this.ThrowIfDisposed();
		if ( WidgetLimits.MaximumModalDepth <= this.modalFrames.Count ) throw new InvalidOperationException( $"A host cannot exceed {WidgetLimits.MaximumModalDepth} modal levels." );
		if ( dialog.Host is not null || dialog.IsOpen ) throw new InvalidOperationException( "The dialog is already open." );
		this.DismissPopup(); this.CancelPointerCapture();
		Widget? previous = this.FocusedWidget;
		CursesInteractionScope scope = this.router.RegisterScope( new CursesInteractionScopeOptions { Parent = this.modalFrames.LastOrDefault()?.Scope } );
		try {
			this.attachmentScope = scope; this.Attach( dialog ); dialog.Arrange( this.Screen.Bounds );
			CursesInteractionScopeLease scopeLease = this.router.ActivateScope( scope );
			dialog.IsOpen = true; dialog.HasCompleted = false;
			WidgetModalLease lease = new( this, dialog ); this.modalFrames.Add( new ModalFrame( dialog, previous, lease, scope, scopeLease ) );
			this.CancelPointerCapture(); Widget[] eligible = dialog.DescendantsAndSelf().Skip( 1 ).Where( child => child.IsFocusable && child.IsEffectivelyEnabled && child.IsEffectivelyVisible && !child.Bounds.IsEmpty ).ToArray();
			Widget? initial = eligible.FirstOrDefault( child => StringComparer.Ordinal.Equals( child.Id, dialog.InitialFocusId ) ) ?? eligible.FirstOrDefault();
			_ = this.Focus( initial ?? dialog ); return lease;
		} catch { if ( ReferenceEquals( dialog.Host, this ) ) this.Detach( dialog ); scope.Dispose(); throw; }
		finally { this.attachmentScope = null; }
	}

	internal void CompleteDialog( WidgetDialog dialog, WidgetDialogOutcome outcome ) {
		this.CompleteDialog( dialog, new WidgetDialogResult( outcome ) );
	}

	internal void CompleteDialog( WidgetDialog dialog, WidgetDialogResult result ) {
		int index = this.modalFrames.FindIndex( frame => ReferenceEquals( frame.Dialog, dialog ) );
		if ( 0 <= index ) { this.CloseModal( dialog, result ); return; }
		if ( dialog.HasCompleted ) return;
		this.Enqueue( new WidgetDialogCompletedAction( dialog.Id, result ) ); dialog.HasCompleted = true; dialog.IsOpen = false; dialog.OnClosed();
		if ( ReferenceEquals( dialog.Host, this ) ) this.Detach( dialog );
	}

	internal void CloseModal( WidgetDialog dialog, WidgetDialogOutcome outcome ) {
		this.CloseModal( dialog, new WidgetDialogResult( outcome ) );
	}

	internal void CloseModal( WidgetDialog dialog, WidgetDialogResult result ) {
		if ( this.disposed ) return;
		int index = this.modalFrames.FindIndex( frame => ReferenceEquals( frame.Dialog, dialog ) ); if ( 0 > index ) return;
		this.EnsureActionCapacity( this.modalFrames.Skip( index ).Count( frame => !frame.Dialog.HasCompleted ) );
		Widget? restore = this.modalFrames[ index ].PreviousFocus;
		for ( int current = this.modalFrames.Count - 1; current >= index; current-- ) {
			ModalFrame frame = this.modalFrames[ current ]; WidgetDialogResult completion = current == index ? result : new WidgetDialogResult( WidgetDialogOutcome.Cancel );
			if ( !frame.Dialog.HasCompleted ) { frame.Dialog.HasCompleted = true; frame.Dialog.IsOpen = false; this.Enqueue( new WidgetDialogCompletedAction( frame.Dialog.Id, completion ) ); }
			frame.Dialog.OnClosed(); frame.Lease.MarkClosed(); frame.ScopeLease.Dispose(); this.modalFrames.RemoveAt( current );
			if ( ReferenceEquals( frame.Dialog.Host, this ) ) this.Detach( frame.Dialog );
			frame.Scope.Dispose();
		}
		if ( 0 < this.modalFrames.Count ) _ = this.Focus( restore is not null && ReferenceEquals( restore.Host, this ) && this.DialogAncestor( restore ) == this.modalFrames[ ^1 ].Dialog ? restore : this.modalFrames[ ^1 ].Dialog );
		else if ( restore is not null && ReferenceEquals( restore.Host, this ) && restore.IsEnabled && restore.IsVisible ) _ = this.Focus( restore );
	}

	private void ClearModalsSilently() {
		for ( int index = this.modalFrames.Count - 1; index >= 0; index-- ) { ModalFrame frame = this.modalFrames[ index ]; frame.Lease.MarkClosed(); frame.ScopeLease.Dispose(); frame.Dialog.IsOpen = false; }
		this.modalFrames.Clear();
	}
}
