/*
	Icod.Widgets.Tests
	Automated tests for the Icod.Widgets library.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

namespace Icod.Widgets.Tests.Qualification;

using System.Text;
using Icod.DCurses;
using Xunit;

public sealed class WidgetEditorQualificationTests {
	[Fact]
	public void PublicEditorWorkflowReplacesSelectionUndoesAndReturnsToCheckpoint() {
		using Fixture fixture = new( "alpha\r\nbeta\ngamma\rdelta" ); using WidgetTextSnapshot saved = fixture.Document.CreateSnapshot(); fixture.Document.MarkSaved( saved );
		fixture.View.SetSelection( 0, 5 ); fixture.Send( WidgetInput.TextInput( new Rune( 'X' ) ) );
		Assert.Equal( "X\r\nbeta\ngamma\rdelta", fixture.Text ); Assert.True( fixture.Document.IsModified );
		Assert.Equal( WidgetCommandResult.Executed, fixture.Host.ExecuteEditCommand( WidgetEditCommand.Undo, fixture.NextTime ) );
		Assert.Equal( "alpha\r\nbeta\ngamma\rdelta", fixture.Text ); Assert.False( fixture.Document.IsModified );
	}

	[Fact]
	public void PublicClipboardDenialAndStaleCompletionPreserveText() {
		using Fixture fixture = new( "safe" ); fixture.View.SetSelection( 0, 4 ); WidgetClipboardRequest cut = fixture.Request( WidgetEditCommand.Cut );
		Assert.True( fixture.Host.ApplyClipboardResult( new WidgetClipboardResult( cut.RequestId, WidgetClipboardStatus.Denied ) ) ); Assert.Equal( "safe", fixture.Text );
		WidgetClipboardRequest paste = fixture.Request( WidgetEditCommand.Paste ); fixture.View.SetSelection( 4, 4 );
		Assert.False( fixture.Host.ApplyClipboardResult( new WidgetClipboardResult( paste.RequestId, WidgetClipboardStatus.Success, "lost" ) ) ); Assert.Equal( "safe", fixture.Text );
	}

	[Fact]
	public void EditCommandsFollowFocusedScopeBetweenFieldAndEditor() {
		using Fixture fixture = new( "editor" ); WidgetTextInput field = new( "field", "field" ); fixture.Host.Attach( field ); field.Arrange( new CursesRectangle( 4, 0, 1, 20 ) );
		fixture.View.SetSelection( 0, fixture.Document.Length ); Assert.True( fixture.Host.Focus( field ));
		Assert.Equal( WidgetCommandResult.Executed, fixture.Host.ExecuteEditCommand( WidgetEditCommand.SelectAll, fixture.NextTime ) ); Assert.Equal( "field", field.Value );
		Assert.True( fixture.Host.Focus( fixture.Editor )); Assert.Equal( WidgetCommandResult.Executed, fixture.Host.ExecuteEditCommand( WidgetEditCommand.SelectAll, fixture.NextTime ) );
		fixture.Send( WidgetInput.TextInput( new Rune( 'E' ) ) ); Assert.Equal( "E", fixture.Text ); Assert.Equal( "field", field.Value );
	}

	[Fact]
	public void RepeatedAttachResizeModalAndShutdownLeavesNoPendingMutation() {
		for ( int index = 0; index < 50; index++ ) {
			using Fixture fixture = new( "text" ); fixture.Editor.Arrange( new CursesRectangle( 0, 0, 1 + index % 4, 5 + index % 20 ) ); fixture.Host.Render();
			WidgetClipboardRequest paste = fixture.Request( WidgetEditCommand.Paste ); using WidgetModalLease modal = fixture.Host.ShowModal( WidgetMessageDialog.Ok( $"d{index}", "Notice", "Modal" ) );
			Assert.False( fixture.Host.ApplyClipboardResult( new WidgetClipboardResult( paste.RequestId, WidgetClipboardStatus.Success, "stale" ) ) ); Assert.Equal( "text", fixture.Text );
		}
	}

	[Fact]
	public void LineNumberGutterWorkRemainsBoundedForLargeDocument() {
		string text = string.Join( '\n', Enumerable.Repeat( "x", 20_000 ) );
		WidgetEditorOptions options = new() { ShowLineNumbers = true, MaximumProjectionSourceUnits = 16, MaximumWorkPerUpdate = 2 };
		WidgetTextDocument document = new( [ text.AsMemory() ] ); using WidgetTextViewState view = new( document );
		WidgetTextEditor editor = new( "editor", view, options ); using WidgetHost host = new( new CursesScreen( 12, 3 ), WidgetTheme.Default );
		host.Attach( editor ); editor.Arrange( host.Screen.Bounds ); Assert.True( host.Focus( editor ) ); host.Render();
		long before = GC.GetAllocatedBytesForCurrentThread();

		for ( int iteration = 0; iteration < 50; iteration++ ) host.Render();

		long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.InRange( allocated, 0, 10_000_000 );
		Assert.Equal( "1", host.Screen.StandardWindow.GetCell( 0, 4 ).Content );
		Assert.Equal( "x", host.Screen.StandardWindow.GetCell( 0, 6 ).Content );
	}

	[Fact]
	public void LargeDocumentDoesNotAllocateOneEntryPerLine() {
		string text = string.Join( '\n', Enumerable.Repeat( "wrapped text", 20_000 ) );
		WidgetEditorOptions options = new() { MaximumProjectionSourceUnits = 64, MaximumWorkPerUpdate = 4, MaximumCachedRows = 8 };
		WidgetTextDocument document = new( [ text.AsMemory() ] ); using WidgetTextViewState view = new( document ); view.WrapMode = CursesTextWrapMode.Word;
		WidgetTextRowMap map = new( options );

		WidgetScrollRange range = map.GetRange( view, view.ViewportAnchor, UnicodeCursesTextWidthProvider.Instance, 6, 3 );

		Assert.False( range.IsExtentExact );
		Assert.InRange( map.CachedEntryCount, 1, options.MaximumCachedRows );
		Assert.True( map.CachedEntryCount < document.LineCount );
	}

	[Fact]
	public void HorizontalExtentDiscoveryIsBoundedAndEventuallyExact() {
		string text = string.Join( '\n', [ "short", new string( 'x', 40 ), "medium width" ] );
		WidgetEditorOptions options = new() { MaximumProjectionSourceUnits = 16, MaximumWorkPerUpdate = 1 };
		WidgetTextDocument document = new( [ text.AsMemory() ] ); using WidgetTextViewState view = new( document );
		WidgetHorizontalExtentCache cache = new( options );

		cache.ObserveVisible( view, UnicodeCursesTextWidthProvider.Instance, 8, 0, 9, true );
		Assert.False( cache.GetRange( view, UnicodeCursesTextWidthProvider.Instance, 8 ).IsExtentExact );
		Assert.True( cache.Advance( view, UnicodeCursesTextWidthProvider.Instance ) <= options.MaximumProjectionSourceUnits );
		Assert.False( cache.GetRange( view, UnicodeCursesTextWidthProvider.Instance, 8 ).IsExtentExact );

		for ( int index = 0; index < 10 && !cache.GetRange( view, UnicodeCursesTextWidthProvider.Instance, 8 ).IsExtentExact; index++ )
			Assert.True( cache.Advance( view, UnicodeCursesTextWidthProvider.Instance ) <= options.MaximumProjectionSourceUnits );

		WidgetScrollRange range = cache.GetRange( view, UnicodeCursesTextWidthProvider.Instance, 8 );
		Assert.True( range.IsExtentExact );
		Assert.Equal( 40, range.Extent );
	}

	[Fact]
	public void HorizontalExtentInvalidatesAfterEditAndRetainsScrolledLowerBound() {
		WidgetEditorOptions options = new() { MaximumProjectionSourceUnits = 16, MaximumWorkPerUpdate = 2 };
		WidgetTextDocument document = new( [ new string( 'x', 32 ).AsMemory() ] ); using WidgetTextViewState view = new( document );
		WidgetHorizontalExtentCache cache = new( options );
		while ( !cache.GetRange( view, UnicodeCursesTextWidthProvider.Instance, 8 ).IsExtentExact ) cache.Advance( view, UnicodeCursesTextWidthProvider.Instance );
		Assert.Equal( 32, cache.GetRange( view, UnicodeCursesTextWidthProvider.Instance, 8 ).Extent );

		WidgetTextEditContext context = new( view.Id, view.Anchor, view.Caret, 0, 0, WidgetTextEditKind.Delete, TimeSpan.Zero, false );
		Assert.Equal( WidgetTextEditStatus.Applied, document.Apply( [ new WidgetTextEdit( 0, 32, "x" ) ], document.Revision, context ).Status );
		view.SetScroll( 12, 0 );

		WidgetScrollRange range = cache.GetRange( view, UnicodeCursesTextWidthProvider.Instance, 8 );
		Assert.False( range.IsExtentExact );
		Assert.Equal( 20, range.Extent );
	}

	private sealed class Fixture : IDisposable {
		private int tick;
		internal Fixture( string text ) { this.Document = new WidgetTextDocument( [ text.AsMemory() ] ); this.View = new WidgetTextViewState( this.Document ); this.Editor = new WidgetTextEditor( "editor", this.View ); this.Host = new WidgetHost( new CursesScreen( 30, 6 ), WidgetTheme.Default ); this.Host.Attach( this.Editor ); this.Editor.Arrange( new CursesRectangle( 0, 0, 4, 30 ) ); Assert.True( this.Host.Focus( this.Editor ) ); }
		internal WidgetTextDocument Document { get; } internal WidgetTextViewState View { get; } internal WidgetTextEditor Editor { get; } internal WidgetHost Host { get; }
		internal TimeSpan NextTime => TimeSpan.FromMilliseconds( this.tick++ ); internal string Text => this.Document.Read( 0, checked( (int)this.Document.Length ) );
		internal WidgetDispatchResult Send( WidgetInput input ) => this.Host.Process( input, this.NextTime );
		internal WidgetClipboardRequest Request( WidgetEditCommand command ) { Assert.Equal( WidgetCommandResult.Executed, this.Host.ExecuteEditCommand( command, this.NextTime ) ); return Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single( this.Host.DrainActions() ) ).Request; }
		public void Dispose() { this.Host.Dispose(); this.View.Dispose(); }
	}
}
