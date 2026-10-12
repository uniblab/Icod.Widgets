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

/// <summary>Collects bounded search intent; the application executes it against its chosen document.</summary>
public sealed class WidgetSearchForm : WidgetDialog {
	private readonly Guid formId = Guid.NewGuid();
	private readonly bool allowReplace;
	private Guid documentId;
	private Guid viewId;
	private long revision;
	private long generation;
	private WidgetSearchRequest? pending;
	private long pendingQueryRevision;
	private long pendingReplacementRevision;
	public WidgetSearchForm( string id, string title, bool allowReplace ) : base( id, title ) {
		this.allowReplace = allowReplace; this.QueryInput = new WidgetTextInput( id + ".query" ) { MaxTextElements = 100_000 }; this.Add( this.QueryInput );
		if ( allowReplace ) { this.ReplacementInput = new WidgetTextInput( id + ".replacement" ) { MaxTextElements = 100_000 }; this.Add( this.ReplacementInput ); }
	}
	public WidgetTextInput QueryInput { get; }
	public WidgetTextInput? ReplacementInput { get; }
	public WidgetSearchOptions Options { get; private set; } = new();
	public string? Error { get; private set; }
	internal override string? InitialFocusId => this.QueryInput.Id;
	public void SetContext( Guid documentId, Guid viewId, long revision ) {
		if ( documentId == Guid.Empty || viewId == Guid.Empty || revision < 0 ) throw new ArgumentException( "Search context needs valid document and view identities and revision." );
		this.documentId = documentId; this.viewId = viewId; this.revision = revision; this.Invalidate();
	}
	public void SetOptions( WidgetSearchOptions options ) {
		ArgumentNullException.ThrowIfNull( options ); if ( !Enum.IsDefined( options.Direction ) ) throw new ArgumentOutOfRangeException( nameof( options ) );
		if ( this.Options == options ) return; this.Options = options; this.Invalidate();
	}
	public WidgetSearchRequest Submit( WidgetSearchIntent intent ) {
		if ( !Enum.IsDefined( intent ) ) throw new ArgumentOutOfRangeException( nameof( intent ) );
		if ( !this.allowReplace && intent != WidgetSearchIntent.Find ) throw new InvalidOperationException( "Replacement is unavailable in a Find-only form." );
		if ( !this.IsOpen || this.Host is null || this.documentId == Guid.Empty ) throw new InvalidOperationException( "The search form needs an open document context." );
		string query = this.QueryInput.Value; string? replacement = this.ReplacementInput?.Value;
		if ( query.Length is 0 or > 4096 || replacement?.Length > 65_536 ) {
			this.Error = "The query must be 1–4096 UTF-16 units and replacement at most 65536 units."; throw new InvalidOperationException( this.Error );
		}
		this.EnsureActionCapacity(); this.Error = null;
		this.pending = new WidgetSearchRequest( Guid.NewGuid(), this.formId, checked( ++this.generation ), this.documentId, this.viewId,
			this.revision, intent, query, intent == WidgetSearchIntent.Find ? null : replacement, this.Options );
		this.pendingQueryRevision = this.QueryInput.ValueRevision; this.pendingReplacementRevision = this.ReplacementInput?.ValueRevision ?? 0;
		this.EmitAction( new WidgetSearchRequestedAction( this.Id, this.pending ) ); return this.pending;
	}
	public bool ApplyResult( WidgetSearchResult result ) {
		ArgumentNullException.ThrowIfNull( result );
		if ( !this.IsOpen || this.pending is null || result.Request != this.pending || this.pending.DocumentId != this.documentId ||
			this.pending.ViewId != this.viewId || this.pending.Revision != this.revision || this.pending.Query != this.QueryInput.Value ||
			this.pending.Options != this.Options || this.pendingQueryRevision != this.QueryInput.ValueRevision ||
			this.pendingReplacementRevision != ( this.ReplacementInput?.ValueRevision ?? 0 ) ||
			this.pending.Replacement != ( this.pending.Intent == WidgetSearchIntent.Find ? null : this.ReplacementInput?.Value ) ) return false;
		this.Error = result.Error ?? ( result.Found ? null : "Not found." ); this.pending = null; return true;
	}
	internal override void OnClosed() => this.Invalidate();
	private void Invalidate() { this.pending = null; this.Error = null; this.generation = checked( this.generation + 1 ); }
	protected override void OnArrange( CursesRectangle bounds ) {
		CursesRectangle area = this.Host is null ? bounds : this.GetContentBounds( this.Host.Screen );
		this.QueryInput.Arrange( new CursesRectangle( area.Row, area.Column, Math.Min( 1, area.Rows ), area.Columns ) );
		this.ReplacementInput?.Arrange( new CursesRectangle( area.Row + Math.Min( 1, area.Rows ), area.Column, Math.Min( 1, Math.Max( 0, area.Rows - 1 ) ), area.Columns ) );
	}
	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Key != input.Kind ) return false;
		if ( WidgetKey.Escape == input.KeyCode ) { this.Finish( host, WidgetDialogOutcome.Cancel ); return true; }
		WidgetSearchIntent? intent = input.KeyCode == WidgetKey.Enter ? WidgetSearchIntent.Find :
			input.KeyCode == WidgetKey.Character && input.Character?.Value is 'r' or 'R' && 0 != ( input.Modifiers & WidgetInputModifiers.Control ) ?
			( 0 != ( input.Modifiers & WidgetInputModifiers.Shift ) ? WidgetSearchIntent.ReplaceAll : WidgetSearchIntent.ReplaceOne ) : null;
		if ( intent is null ) return false;
		try { this.Submit( intent.Value ); } catch ( InvalidOperationException ) { }
		return true;
	}
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.RenderFrame( screen, theme ); if ( area.IsEmpty ) return;
		string help = this.allowReplace ? "Enter: Find | Ctrl+R: Replace | Esc: Cancel" : "Enter: Find | Esc: Cancel";
		WidgetBrowserView.WriteLine( screen, area, Math.Max( 0, area.Rows - 2 ), this.Error ?? help, this.Error is null ? theme.Normal : theme.Error );
	}
}
