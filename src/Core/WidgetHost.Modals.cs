/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

/// <summary>Modal-stack portion of <see cref="WidgetHost"/>.</summary>
public sealed partial class WidgetHost {
	private sealed record ModalFrame( WidgetDialog Dialog, Widget? PreviousFocus, WidgetModalLease Lease );
	private readonly List<ModalFrame> modalFrames = [];

	public int ModalDepth => this.modalFrames.Count;
	internal WidgetDialog? ActiveModal => 0 == this.modalFrames.Count ? null : this.modalFrames[ ^1 ].Dialog;
	internal bool IsModalOpen( WidgetDialog dialog ) => this.modalFrames.Any( frame => ReferenceEquals( frame.Dialog, dialog ) );

	public WidgetModalLease ShowModal( WidgetDialog dialog ) {
		ArgumentNullException.ThrowIfNull( dialog ); this.ThrowIfDisposed();
		if ( WidgetLimits.MaximumModalDepth <= this.modalFrames.Count ) throw new InvalidOperationException( $"A host cannot exceed {WidgetLimits.MaximumModalDepth} modal levels." );
		if ( dialog.Host is not null || dialog.IsOpen ) throw new InvalidOperationException( "The dialog is already open." );
		this.CancelPointerCapture();
		Widget? previous = this.FocusedWidget;
		this.Attach( dialog ); dialog.Arrange( this.Screen.Bounds ); dialog.IsOpen = true; dialog.HasCompleted = false;
		dialog.OnOpened();
		WidgetModalLease lease = new( this, dialog ); this.modalFrames.Add( new ModalFrame( dialog, previous, lease ) );
		_ = this.Focus( dialog ); return lease;
	}

	internal void CompleteDialog( WidgetDialog dialog, WidgetDialogOutcome outcome ) {
		this.CompleteDialog( dialog, new WidgetDialogResult( outcome ) );
	}

	internal void CompleteDialog( WidgetDialog dialog, WidgetDialogResult result ) {
		int index = this.modalFrames.FindIndex( frame => ReferenceEquals( frame.Dialog, dialog ) );
		if ( 0 <= index ) { this.CloseModal( dialog, result ); return; }
		if ( dialog.HasCompleted ) return;
		this.EnsureActionCapacity();
		dialog.HasCompleted = true; dialog.IsOpen = false; this.Enqueue( new WidgetDialogCompletedAction( dialog.Id, result ) );
		dialog.OnClosed();
		if ( ReferenceEquals( dialog.Host, this ) ) this.Detach( dialog );
	}

	internal void CloseModal( WidgetDialog dialog, WidgetDialogOutcome outcome ) {
		this.CloseModal( dialog, new WidgetDialogResult( outcome ) );
	}

	internal void CloseModal( WidgetDialog dialog, WidgetDialogResult result ) {
		if ( this.disposed ) return;
		int index = this.modalFrames.FindIndex( frame => ReferenceEquals( frame.Dialog, dialog ) ); if ( 0 > index ) return;
		int completionCount = this.modalFrames.Skip( index ).Count( frame => !frame.Dialog.HasCompleted );
		this.EnsureActionCapacity( completionCount );
		Widget? restore = this.modalFrames[ index ].PreviousFocus;
		for ( int current = this.modalFrames.Count - 1; current >= index; current-- ) {
			ModalFrame frame = this.modalFrames[ current ]; WidgetDialogResult completion = current == index ? result : new WidgetDialogResult( WidgetDialogOutcome.Cancel );
			if ( !frame.Dialog.HasCompleted ) { frame.Dialog.HasCompleted = true; frame.Dialog.IsOpen = false; this.Enqueue( new WidgetDialogCompletedAction( frame.Dialog.Id, completion ) ); }
			frame.Dialog.OnClosed();
			frame.Lease.MarkClosed(); this.modalFrames.RemoveAt( current );
			if ( ReferenceEquals( frame.Dialog.Host, this ) ) this.Detach( frame.Dialog );
		}
		if ( 0 < this.modalFrames.Count ) _ = this.Focus( this.modalFrames[ ^1 ].Dialog );
		else if ( restore is not null && ReferenceEquals( restore.Host, this ) && restore.IsEnabled && restore.IsVisible ) _ = this.Focus( restore );
	}

	private void ClearModalsSilently() {
		foreach ( ModalFrame frame in this.modalFrames ) { frame.Lease.MarkClosed(); frame.Dialog.IsOpen = false; frame.Dialog.OnClosed(); }
		this.modalFrames.Clear();
	}
}
