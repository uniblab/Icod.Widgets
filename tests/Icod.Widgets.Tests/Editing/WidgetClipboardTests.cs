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

namespace Icod.Widgets.Tests.Editing;

using Icod.DCurses;
using Xunit;

public sealed class WidgetClipboardTests {
	[Fact]
	public void ExistingClipboardConstructorsRemainAvailable() {
		Assert.NotNull( typeof( WidgetClipboardRequest ).GetConstructor( [
			typeof( Guid ), typeof( Guid ), typeof( Guid ), typeof( long ), typeof( long ),
			typeof( long ), typeof( long ), typeof( long ), typeof( long ),
			typeof( WidgetClipboardOperation ), typeof( string )
		] ) );
		Assert.NotNull( typeof( WidgetClipboardResult ).GetConstructor( [
			typeof( Guid ), typeof( WidgetClipboardStatus ), typeof( string )
		] ) );
	}

	[Fact]
	public void CopyHonorsUtf8ByteBudget() {
		using Fixture fixture = new( "界a", maximumBytes: 4 ); fixture.View.SetSelection( 0, 2 );

		WidgetClipboardRequest request = fixture.Request( WidgetEditCommand.Copy );

		Assert.Equal( WidgetClipboardOperation.Copy, request.Operation );
		Assert.Equal( "界a", request.Text );
		Assert.Equal( ( 0L, 2L ), ( request.SelectionStart, request.SelectionEnd ) );

		using Fixture exact = new( new string( 'x', 65_536 ) ); exact.View.SetSelection( 0, 65_536 ); Assert.Equal( 65_536, exact.Request( WidgetEditCommand.Copy ).Text!.Length );
		using Fixture over = new( new string( 'x', 65_537 ) ); over.View.SetSelection( 0, 65_537 ); Assert.Equal( WidgetCommandResult.Disabled, over.Host.ExecuteEditCommand( WidgetEditCommand.Copy, over.NextTime ) );
	}

	[Fact]
	public void NoSelectionCopyCapturesWholeLogicalLineWithExactCrLf() {
		using Fixture fixture = new( "one\r\ntwo\n" );
		fixture.View.SetSelection( 1, 1 );
		WidgetClipboardRequest request = fixture.Request( WidgetEditCommand.Copy );
		Assert.Equal( "one\r\n", request.Text );
		Assert.True( request.IsLinewise );
		Assert.Equal( "one\r\ntwo\n", fixture.Text );
	}

	[Fact]
	public void NoSelectionCutWaitsForSuccessAndDeletesWholeLineAsOneEdit() {
		using Fixture fixture = new( "one\r\ntwo" );
		fixture.View.SetSelection( 1, 1 );
		WidgetClipboardRequest request = fixture.Request( WidgetEditCommand.Cut );
		Assert.Equal( "one\r\n", request.Text );
		Assert.True( request.IsLinewise );
		Assert.Equal( "one\r\ntwo", fixture.Text );
		Assert.True( fixture.Host.ApplyClipboardResult( new WidgetClipboardResult( request.RequestId, WidgetClipboardStatus.Success ) ) );
		Assert.Equal( "two", fixture.Text );
		fixture.Host.DrainActions();
		Assert.Equal( WidgetCommandResult.Executed, fixture.Host.ExecuteEditCommand( WidgetEditCommand.Undo, fixture.NextTime ) );
		Assert.Equal( "one\r\ntwo", fixture.Text );
	}

	[Fact]
	public void InternalLinewisePasteInsertsAtLineStartAndSelectionPasteReplacesSelection() {
		using Fixture fixture = new( "one\ntwo" );
		fixture.View.SetSelection( 5, 5 );
		WidgetClipboardRequest linePaste = fixture.Request( WidgetEditCommand.Paste );
		Assert.True( fixture.Host.ApplyClipboardResult( new WidgetClipboardResult( linePaste.RequestId,
			WidgetClipboardStatus.Success, "one\n" ) { IsLinewise = true } ) );
		Assert.Equal( "one\none\ntwo", fixture.Text );
		fixture.Host.DrainActions();
		fixture.View.SetSelection( 8, 11 );
		WidgetClipboardRequest replace = fixture.Request( WidgetEditCommand.Paste );
		Assert.True( fixture.Host.ApplyClipboardResult( new WidgetClipboardResult( replace.RequestId,
			WidgetClipboardStatus.Success, "X" ) { IsLinewise = true } ) );
		Assert.Equal( "one\none\nX", fixture.Text );
	}

	[Fact]
	public void LinewisePasteOfUnterminatedLastLineSeparatesFollowingContent() {
		using Fixture fixture = new( "one\ntwo" );
		fixture.Editor.NewLine = "\r\n";
		fixture.View.SetSelection( 5, 5 );
		WidgetClipboardRequest paste = fixture.Request( WidgetEditCommand.Paste );
		Assert.True( fixture.Host.ApplyClipboardResult( new WidgetClipboardResult(
			paste.RequestId, WidgetClipboardStatus.Success, "last" ) { IsLinewise = true } ) );
		Assert.Equal( "one\nlast\r\ntwo", fixture.Text );
		fixture.Host.DrainActions();
		Assert.Equal( WidgetCommandResult.Executed, fixture.Host.ExecuteEditCommand( WidgetEditCommand.Undo, fixture.NextTime ) );
		Assert.Equal( "one\ntwo", fixture.Text );
	}

	[Fact]
	public void NoSelectionLastLineRetainsMissingTerminatorAndByteBudget() {
		using Fixture fixture = new( "one\n界", maximumBytes: 2 );
		fixture.View.SetSelection( 5, 5 );
		Assert.Equal( WidgetCommandResult.Disabled, fixture.Host.ExecuteEditCommand( WidgetEditCommand.Copy, fixture.NextTime ) );
		using Fixture allowed = new( "one\n界", maximumBytes: 3 );
		allowed.View.SetSelection( 5, 5 );
		Assert.Equal( "界", allowed.Request( WidgetEditCommand.Copy ).Text );
	}

	[Fact]
	public void NoSilentClipboardTruncation() {
		using Fixture fixture = new( "界ab", maximumBytes: 4 ); fixture.View.SetSelection( 0, 3 );

		WidgetCommandResult result = fixture.Host.ExecuteEditCommand( WidgetEditCommand.Copy, fixture.NextTime );

		Assert.Equal( WidgetCommandResult.Disabled, result );
		Assert.Empty( fixture.Host.DrainActions() );
	}

	[Fact]
	public void FailedCopyDoesNotCut() {
		using Fixture fixture = new( "keep" ); fixture.View.SetSelection( 0, 4 ); WidgetClipboardRequest request = fixture.Request( WidgetEditCommand.Cut );

		Assert.True( fixture.Host.ApplyClipboardResult( new WidgetClipboardResult( request.RequestId, WidgetClipboardStatus.Failed ) ) );

		Assert.Equal( "keep", fixture.Text );
		Assert.False( fixture.Document.CanUndo );
	}

	[Fact]
	public void AcceptedCutDeletesExactlyOnce() {
		using Fixture fixture = new( "cut me" ); fixture.View.SetSelection( 0, 3 ); WidgetClipboardRequest request = fixture.Request( WidgetEditCommand.Cut );
		WidgetClipboardResult result = new( request.RequestId, WidgetClipboardStatus.Success );

		Assert.True( fixture.Host.ApplyClipboardResult( result ) );
		Assert.Equal( " me", fixture.Text );
		Assert.False( fixture.Host.ApplyClipboardResult( result ) );
		Assert.Equal( " me", fixture.Text );
	}

	[Fact]
	public void LatePasteRejectsChangedSelectionRevisionFocusOrAttachment() {
		using Fixture selection = new( "a" ); WidgetClipboardRequest selectionRequest = selection.Request( WidgetEditCommand.Paste ); selection.View.SetSelection( 1, 1 );
		Assert.False( selection.Host.ApplyClipboardResult( Success( selectionRequest, "X" ) ) ); Assert.Equal( "a", selection.Text );

		using Fixture revision = new( "a" ); WidgetClipboardRequest revisionRequest = revision.Request( WidgetEditCommand.Paste ); revision.Document.Apply(
			[ new WidgetTextEdit( 1, 0, "b" ) ], revision.Document.Revision,
			new WidgetTextEditContext( Guid.NewGuid(), 1, 1, 2, 2, WidgetTextEditKind.Programmatic, TimeSpan.Zero ) );
		Assert.False( revision.Host.ApplyClipboardResult( Success( revisionRequest, "X" ) ) ); Assert.Equal( "ab", revision.Text );

		using Fixture focus = new( "a" ); WidgetClipboardRequest focusRequest = focus.Request( WidgetEditCommand.Paste ); WidgetButton other = new( "other", "Other", "other" );
		focus.Host.Attach( other ); other.Arrange( new CursesRectangle( 3, 0, 1, 10 ) ); Assert.True( focus.Host.Focus( other ));
		Assert.False( focus.Host.ApplyClipboardResult( Success( focusRequest, "X" ) ) ); Assert.Equal( "a", focus.Text );

		using Fixture attachment = new( "a" ); WidgetClipboardRequest attachmentRequest = attachment.Request( WidgetEditCommand.Paste ); Assert.True( attachment.Host.Detach( attachment.Editor ) );
		attachment.Host.Attach( attachment.Editor ); attachment.Editor.Arrange( new CursesRectangle( 0, 0, 3, 20 ) ); Assert.True( attachment.Host.Focus( attachment.Editor ) );
		Assert.False( attachment.Host.ApplyClipboardResult( Success( attachmentRequest, "X" ) ) ); Assert.Equal( "a", attachment.Text );
	}

	[Fact]
	public void ReadOnlyChangeInvalidatesRequest() {
		using Fixture fixture = new( "a" ); WidgetClipboardRequest request = fixture.Request( WidgetEditCommand.Paste ); fixture.Editor.IsReadOnly = true;

		Assert.False( fixture.Host.ApplyClipboardResult( Success( request, "X" ) ) );
		Assert.Equal( "a", fixture.Text );
	}

	[Fact]
	public void NewRequestSupersedesOld() {
		using Fixture fixture = new( "a" ); WidgetClipboardRequest first = fixture.Request( WidgetEditCommand.Paste ); WidgetClipboardRequest second = fixture.Request( WidgetEditCommand.Paste );

		Assert.False( fixture.Host.ApplyClipboardResult( Success( first, "X" ) ) );
		Assert.True( fixture.Host.ApplyClipboardResult( Success( second, "Y" ) ) );
		Assert.Equal( "Ya", fixture.Text );
	}

	[Fact]
	public void PasteCompletionIsOneUndoGroup() {
		using Fixture fixture = new( "ab" ); fixture.View.SetSelection( 1, 1 ); WidgetClipboardRequest request = fixture.Request( WidgetEditCommand.Paste );

		Assert.True( fixture.Host.ApplyClipboardResult( Success( request, "X\r\n界" ) ) );
		Assert.Equal( "aX\r\n界b", fixture.Text );
		fixture.Host.DrainActions(); Assert.Equal( WidgetCommandResult.Executed, fixture.Host.ExecuteEditCommand( WidgetEditCommand.Undo, fixture.NextTime ) );
		Assert.Equal( "ab", fixture.Text );
	}

	[Fact]
	public void InvalidOrOversizedPasteIsRejectedWithoutMutation() {
		using Fixture invalid = new( "a" ); WidgetClipboardRequest invalidRequest = invalid.Request( WidgetEditCommand.Paste );
		Assert.False( invalid.Host.ApplyClipboardResult( Success( invalidRequest, "\uD800" ) ) ); Assert.Equal( "a", invalid.Text );

		using Fixture oversized = new( "a", maximumBytes: 4 ); WidgetClipboardRequest oversizedRequest = oversized.Request( WidgetEditCommand.Paste );
		Assert.False( oversized.Host.ApplyClipboardResult( Success( oversizedRequest, "界ab" ) ) ); Assert.Equal( "a", oversized.Text );
	}

	[Theory]
	[InlineData( WidgetClipboardStatus.Denied )]
	[InlineData( WidgetClipboardStatus.Unsupported )]
	[InlineData( WidgetClipboardStatus.TimedOut )]
	[InlineData( WidgetClipboardStatus.Canceled )]
	[InlineData( WidgetClipboardStatus.TooLarge )]
	[InlineData( WidgetClipboardStatus.InvalidText )]
	public void ControlledClipboardFailuresConsumeWithoutCutting( WidgetClipboardStatus status ) {
		using Fixture fixture = new( "keep" ); fixture.View.SetSelection( 0, 4 ); WidgetClipboardRequest request = fixture.Request( WidgetEditCommand.Cut );

		Assert.True( fixture.Host.ApplyClipboardResult( new WidgetClipboardResult( request.RequestId, status ) ) );
		Assert.Equal( "keep", fixture.Text );
	}

	private static WidgetClipboardResult Success( WidgetClipboardRequest request, string text ) => new( request.RequestId, WidgetClipboardStatus.Success, text );

	private sealed class Fixture : IDisposable {
		private int tick;
		internal Fixture( string text, int maximumBytes = 65_536 ) {
			this.Document = new WidgetTextDocument( [ text.AsMemory() ] ); this.View = new WidgetTextViewState( this.Document );
			this.Editor = new WidgetTextEditor( "editor", this.View, new WidgetEditorOptions { MaximumClipboardBytes = maximumBytes } );
			this.Host = new WidgetHost( new CursesScreen( 20, 5 ), WidgetTheme.Default ); this.Host.Attach( this.Editor ); this.Editor.Arrange( new CursesRectangle( 0, 0, 3, 20 ) ); Assert.True( this.Host.Focus( this.Editor ) );
		}
		internal WidgetTextDocument Document { get; }
		internal WidgetTextViewState View { get; }
		internal WidgetTextEditor Editor { get; }
		internal WidgetHost Host { get; }
		internal TimeSpan NextTime => TimeSpan.FromMilliseconds( this.tick++ );
		internal string Text => this.Document.Read( 0, checked( (int)this.Document.Length ) );
		internal WidgetClipboardRequest Request( WidgetEditCommand command ) {
			Assert.Equal( WidgetCommandResult.Executed, this.Host.ExecuteEditCommand( command, this.NextTime ) );
			return Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single( this.Host.DrainActions() ) ).Request;
		}
		public void Dispose() { this.Host.Dispose(); this.View.Dispose(); }
	}
}
