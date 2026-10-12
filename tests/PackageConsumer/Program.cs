/*
	PackageConsumer
	Package-only acceptance consumer for Icod.Widgets.
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

using Icod.DCurses;
using Icod.Widgets;
using WidgetHost host = new( new CursesScreen( 24, 5 ), WidgetTheme.Default );
WidgetMenuBar menu = new( "menu", [ WidgetMenuItem.Submenu( "file", "File", [ WidgetMenuItem.Command( "run", "Run", "run", 'r' ) ], 'f' ) ] );
host.Attach( menu ); host.Arrange( host.Screen.Bounds ); host.Focus( menu );
host.Process( WidgetInput.Key( WidgetKey.Character, WidgetInputModifiers.Alt, new( 'f' ) ), TimeSpan.Zero );
WidgetDispatchResult menuResult = host.Process( WidgetInput.Key( WidgetKey.Character, character: new( 'r' ) ), TimeSpan.FromMilliseconds( 1 ) );
WidgetMessageDialog notice = WidgetMessageDialog.Ok( "notice", "Notice", "Packaged API works." ); host.ShowModal( notice );
WidgetDispatchResult dialogResult = host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 2 ) );
PublicProbe probe = new( "public-probe" ); host.Attach( probe ); probe.Arrange( new CursesRectangle( 0, 0, 1, 12 ) ); host.Focus( probe );
WidgetDispatchResult probeResult = host.Process( WidgetInput.Key( WidgetKey.Enter ), TimeSpan.FromMilliseconds( 3 ) );
host.Update( TimeSpan.FromMilliseconds( 4 ) );
host.Process( WidgetInput.PointerInput( new CursesMouseEvent( CursesMouseAction.Press, CursesMouseButton.Primary, 1, 0 ) ), TimeSpan.FromMilliseconds( 5 ) );
probe.IsVisible = false;
WidgetTextDocument document = new( [ "one\r\ntwo\n".AsMemory() ] ); using WidgetTextViewState view = new( document ); WidgetTextEditor editor = new( "editor", view );
host.Attach( editor ); editor.Arrange( new CursesRectangle( 0, 0, 4, 24 ) ); host.Focus( editor ); view.SetSelection( 0, 3 );
host.Process( WidgetInput.TextInput( new System.Text.Rune( 'X' ) ), TimeSpan.FromMilliseconds( 6 ) );
bool edited = document.Read( 0, checked( (int)document.Length ) ) == "X\r\ntwo\n" && document.IsModified;
bool undone = host.ExecuteEditCommand( WidgetEditCommand.Undo, TimeSpan.FromMilliseconds( 7 ) ) == WidgetCommandResult.Executed &&
	document.Read( 0, checked( (int)document.Length ) ) == "one\r\ntwo\n" && !document.IsModified;
WidgetClipboardRequest paste = RequestClipboard( host, WidgetEditCommand.Paste, TimeSpan.FromMilliseconds( 8 ) ); view.SetSelection( document.Length, document.Length );
bool staleClipboard = !host.ApplyClipboardResult( new WidgetClipboardResult( paste.RequestId, WidgetClipboardStatus.Success, "stale" ) );
using WidgetTextSnapshot snapshot = document.CreateSnapshot(); document.MarkSaved( snapshot );
WidgetTabStrip tabs = new( "tabs" ); tabs.SetTabs( [ new WidgetTab( "one", "One" ), new WidgetTab( "two", "Two", true ) ], "two" );
WidgetContentHost content = new( "content" ); content.SetContent( new WidgetTextEditor( "workspace-editor", new WidgetTextViewState( document ) ) );
WidgetPopupMenu dynamicMenu = new( "dynamic", [ WidgetMenuItem.Command( "copy", "Copy", "edit.copy", WidgetEditCommand.Copy ) ] );
dynamicMenu.SetPresentation( [ new WidgetMenuItemPresentation( "copy", "Copy selection", true ) ] );
WidgetStatusField statusField = new( "find", "Find", 10 ) { CommandId = "edit.find" };
WidgetStatusBar statusBar = new( "status" ); statusBar.SetFields( [ statusField ] ); statusBar.Arrange( new CursesRectangle( 0, 0, 1, 24 ) );
WidgetFormDialog decision = WidgetFormDialog.SaveDontSaveCancel( "decision", "Close", "Save changes?" );
WidgetSaveTargetPicker saveTarget = new( "save", new LocalWidgetFileSystemProvider(), new PackageInspector(), "/tmp", "draft.txt" );
WidgetSearchForm search = new( "search", "Find", true ); search.SetContext( document.Id, view.Id, document.Revision );
WidgetGoToLineDialog goTo = new( "go-to", "Go To Line" ); goTo.SetContext( document.Id, view.Id, document.Revision );
bool workspaceApi = tabs.ActiveKey == "two" && content.Content is WidgetTextEditor && dynamicMenu is not null &&
	statusField.CommandId == "edit.find" && decision.Actions.Count == 3 && saveTarget.NameInput.Value == "draft.txt" &&
	search.ReplacementInput is not null && goTo.LineInput is not null && WidgetLimits.MaximumTabs == 1024;
return menuResult.Actions.Single() is WidgetInvokedAction &&
	dialogResult.Actions.Single() is WidgetDialogCompletedAction { Result.Outcome: WidgetDialogOutcome.Ok } &&
	probeResult.Actions.Single() is WidgetInvokedAction { CommandId: "public" } && probe.UpdateCount == 1 && probe.CancellationCount == 1 &&
	edited && undone && staleClipboard && workspaceApi && snapshot.Read( 0, checked( (int)snapshot.Length ) ) == "one\r\ntwo\n" ? 0 : 1;

static WidgetClipboardRequest RequestClipboard( WidgetHost host, WidgetEditCommand command, TimeSpan timestamp ) {
	if ( host.ExecuteEditCommand( command, timestamp ) != WidgetCommandResult.Executed ) throw new InvalidOperationException();
	return host.DrainActions().OfType<WidgetClipboardRequestedAction>().Single().Request;
}

sealed class PublicProbe( string id ) : Widget( id ) {
	public override bool IsFocusable => true;
	public int UpdateCount { get; private set; }
	public int CancellationCount { get; private set; }

	protected override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetKey.Enter != input.KeyCode ) return WidgetInputKind.Pointer == input.Kind;
		this.EnsureActionCapacity();
		this.EmitAction( new WidgetInvokedAction( this.Id, "public" ) );
		return true;
	}

	protected override void OnUpdate( TimeSpan timestamp ) => this.UpdateCount++;
	protected override void OnCaptureCanceled() => this.CancellationCount++;
}

sealed class PackageInspector : IWidgetSaveTargetProvider {
	public ValueTask<WidgetSaveTargetObservation> InspectTargetAsync( string directory, string fileName, CancellationToken token ) =>
		ValueTask.FromResult( new WidgetSaveTargetObservation( directory + "/" + fileName, false, true ) );
}
