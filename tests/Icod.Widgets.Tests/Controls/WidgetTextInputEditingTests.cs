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

namespace Icod.Widgets.Tests.Controls;

using System.Text;
using Icod.DCurses;
using Xunit;

public sealed class WidgetTextInputEditingTests {
	[Fact]
	public void SelectionReplacementAndUndo() {
		using Fixture fixture = new( "abc" ); fixture.Input.SetSelection( 1, 2 );

		fixture.Send( WidgetInput.TextInput( new Rune( 'X' ) ) );
		Assert.Equal( "aXc", fixture.Input.Value );
		Assert.Equal( WidgetCommandResult.Executed, fixture.Host.ExecuteEditCommand( WidgetEditCommand.Undo, fixture.NextTime ) );

		Assert.Equal( "abc", fixture.Input.Value );
		Assert.Equal( ( 1, 2 ), ( fixture.Input.Anchor, fixture.Input.Caret ) );
	}

	[Fact]
	public void PasteRemainsSingleLine() {
		using Fixture fixture = new();

		fixture.Send( WidgetInput.Paste( "one\r\n\t two\u0001three" ) );

		Assert.Equal( "one twothree", fixture.Input.Value );
	}

	[Fact]
	public void ExistingLimitsAndValueActionRemainCompatible() {
		using Fixture fixture = new(); fixture.Input.MaxTextElements = 1;

		WidgetDispatchResult result = fixture.Send( WidgetInput.Paste( "a\u0301b" ) );

		Assert.Equal( "a\u0301", fixture.Input.Value );
		WidgetValueChangedAction<string> action = Assert.IsType<WidgetValueChangedAction<string>>( Assert.Single( result.Actions ) );
		Assert.Equal( "a\u0301", action.Value );
	}

	[Fact]
	public void FocusedFieldWinsCopyCommand() {
		using Fixture fixture = new( "copy" ); fixture.Input.SetSelection( 0, 4 );

		Assert.Equal( WidgetCommandResult.Executed, fixture.Host.ExecuteEditCommand( WidgetEditCommand.Copy, fixture.NextTime ) );
		WidgetClipboardRequest request = Assert.IsType<WidgetClipboardRequestedAction>( Assert.Single( fixture.Host.DrainActions() ) ).Request;

		Assert.Equal( "copy", request.Text );
		Assert.Equal( WidgetClipboardOperation.Copy, request.Operation );
	}

	[Fact]
	public void FieldTabStillTraverses() {
		WidgetStack root = new( "root", WidgetOrientation.Vertical ); WidgetTextInput first = new( "first" ); WidgetTextInput second = new( "second" ); root.Add( first ); root.Add( second );
		using WidgetHost host = new( new CursesScreen( 20, 4 ), WidgetTheme.Default ); host.Attach( root ); host.Arrange( host.Screen.Bounds ); Assert.True( host.Focus( first ) );

		Assert.True( host.Process( WidgetInput.Key( WidgetKey.Tab ), TimeSpan.Zero ).Handled );

		Assert.Same( second, host.FocusedWidget );
	}

	[Fact]
	public void LimitReductionClampsSelectionAndInvalidatesPaste() {
		using Fixture fixture = new( "abcd" ); fixture.Input.SetSelection( 1, 4 );
		fixture.Send( WidgetInput.PasteChunk( CursesPastePhase.Begin ) ); fixture.Send( WidgetInput.PasteChunk( CursesPastePhase.Data, "pending" ) );

		fixture.Input.MaxTextElements = 2; fixture.Send( WidgetInput.PasteChunk( CursesPastePhase.End ) );

		Assert.Equal( "ab", fixture.Input.Value );
		Assert.Equal( ( 1, 2 ), ( fixture.Input.Anchor, fixture.Input.Caret ) );
	}

	[Fact]
	public void FieldCaretUsesUnicodeGeometry() {
		using Fixture fixture = new( "a\u0301界" ); fixture.Input.SetSelection( 2, 2 );

		WidgetCaretIntent intent = Assert.IsType<WidgetCaretIntent>( fixture.Host.GetCaretIntent() );

		Assert.Equal( 1, intent.Column );
	}

	private sealed class Fixture : IDisposable {
		private int tick;
		internal Fixture( string value = "" ) {
			this.Input = new WidgetTextInput( "input", value ); this.Host = new WidgetHost( new CursesScreen( 20, 3 ), WidgetTheme.Default );
			this.Host.Attach( this.Input ); this.Input.Arrange( new CursesRectangle( 0, 0, 1, 20 ) ); Assert.True( this.Host.Focus( this.Input ) );
		}
		internal WidgetTextInput Input { get; }
		internal WidgetHost Host { get; }
		internal TimeSpan NextTime => TimeSpan.FromMilliseconds( this.tick++ );
		internal WidgetDispatchResult Send( WidgetInput input ) => this.Host.Process( input, this.NextTime );
		public void Dispose() => this.Host.Dispose();
	}
}
