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

public sealed record WidgetStatusField( string Name, string Text, int Priority ) {
	public string? CommandId { get; init; }
	public bool IsEnabled { get; init; } = true;
}

/// <summary>Fits prioritized caller-defined fields into one clipped line.</summary>
public sealed class WidgetStatusBar( string id ) : Widget( id ) {
	private sealed record Segment( WidgetStatusField Field, int Column, int Columns );
	private WidgetStatusField[] fields = [];
	private Segment[] visible = [];
	private string? focusedFieldName;
	private string? pressedFieldName;
	public IReadOnlyList<WidgetStatusField> Fields => this.fields;
	public IReadOnlyList<string> VisibleFieldNames => this.visible.Select( segment => segment.Field.Name ).ToArray();
	public string? FocusedFieldName => this.focusedFieldName;
	public override bool IsFocusable => this.visible.Any( segment => IsActionable( segment.Field ) );
	public void SetFields( IReadOnlyList<WidgetStatusField> fields ) {
		ArgumentNullException.ThrowIfNull( fields );
		if ( fields.Count > WidgetLimits.MaximumItems ) throw new ArgumentOutOfRangeException( nameof( fields ) );
		WidgetStatusField[] replacement = fields.ToArray(); HashSet<string> names = new( StringComparer.Ordinal );
		foreach ( WidgetStatusField field in replacement ) if ( field is null || string.IsNullOrWhiteSpace( field.Name ) || field.Text is null || !names.Add( field.Name ) )
			throw new ArgumentException( "Status fields require unique names and nonnull text.", nameof( fields ) );
		this.fields = replacement; this.Layout(); this.Host?.SynchronizeTree( this );
	}
	protected override void OnArrange( CursesRectangle bounds ) => this.Layout();
	protected override void OnCaptureCanceled() => this.pressedFieldName = null;
	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		if ( WidgetInputKind.Pointer == input.Kind && input.Pointer is { Button: CursesMouseButton.Primary } pointer ) {
			Segment? hit = pointer.Row == this.Bounds.Row ? this.visible.FirstOrDefault( segment => pointer.Column >= this.Bounds.Column + segment.Column && pointer.Column < this.Bounds.Column + segment.Column + segment.Columns ) : null;
			if ( CursesMouseAction.Press == pointer.Action ) { this.pressedFieldName = hit is not null && IsActionable( hit.Field ) ? hit.Field.Name : null; return hit is not null; }
			if ( CursesMouseAction.Release == pointer.Action ) {
				bool invoke = hit is not null && StringComparer.Ordinal.Equals( this.pressedFieldName, hit.Field.Name ) && IsActionable( hit.Field );
				this.pressedFieldName = null; if ( invoke ) { this.focusedFieldName = hit!.Field.Name; this.Invoke( hit.Field ); } return invoke;
			}
			return false;
		}
		if ( WidgetInputKind.Key != input.Kind ) return false;
		Segment[] actions = this.visible.Where( segment => IsActionable( segment.Field ) ).ToArray(); if ( actions.Length == 0 ) return false;
		if ( input.KeyCode is WidgetKey.Left or WidgetKey.Right ) {
			int index = Array.FindIndex( actions, segment => StringComparer.Ordinal.Equals( segment.Field.Name, this.focusedFieldName ) );
			if ( index < 0 ) index = 0;
			this.focusedFieldName = actions[ ( index + ( WidgetKey.Right == input.KeyCode ? 1 : actions.Length - 1 ) ) % actions.Length ].Field.Name; return true;
		}
		if ( input.KeyCode is WidgetKey.Enter or WidgetKey.Space ) {
			Segment? selected = actions.FirstOrDefault( segment => StringComparer.Ordinal.Equals( segment.Field.Name, this.focusedFieldName ) );
			if ( selected is null ) return false; this.Invoke( selected.Field ); return true;
		}
		return false;
	}
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) {
		CursesRectangle area = this.Bounds.Intersect( screen.Bounds ); if ( area.IsEmpty ) return;
		screen.StandardWindow.FillRectangle( area.Row, area.Column, 1, area.Columns, CursesCell.Blank( theme.Normal ) );
		int previousEnd = 0;
		foreach ( Segment segment in this.visible ) {
			int column = this.Bounds.Column + segment.Column; if ( column < area.Column || column >= area.Column + area.Columns ) continue;
			if ( segment.Column - previousEnd == 3 ) { screen.StandardWindow.Move( area.Row, this.Bounds.Column + previousEnd ); screen.StandardWindow.Write( " | ", theme.Normal ); }
			string label = CursesText.TruncateToColumns( segment.Field.Text, Math.Min( segment.Columns, area.Column + area.Columns - column ), screen.TextWidthProvider );
			if ( label.Length == 0 && segment.Columns > 0 ) label = "›";
			screen.StandardWindow.Move( area.Row, column ); screen.StandardWindow.Write( label, focused && StringComparer.Ordinal.Equals( segment.Field.Name, this.focusedFieldName ) ? theme.Focused : theme.Normal );
			previousEnd = segment.Column + segment.Columns;
		}
	}
	private static bool IsActionable( WidgetStatusField field ) => field.IsEnabled && !string.IsNullOrWhiteSpace( field.CommandId );
	private void Invoke( WidgetStatusField field ) { this.EnsureActionCapacity(); this.EmitAction( new WidgetInvokedAction( this.Id, field.CommandId! ) ); }
	private void Layout() {
		int columns = Math.Max( 0, this.Bounds.Columns ); int at = 0;
		ICursesTextWidthProvider width = this.Host?.Screen.TextWidthProvider ?? new CursesScreen( 1, 1 ).TextWidthProvider;
		List<Segment> segments = [];
		foreach ( WidgetStatusField field in this.fields.OrderByDescending( field => field.Priority ) ) {
			int textColumns = CursesText.MeasureColumns( field.Text, width ); int separator = segments.Count == 0 ? 0 : 3;
			if ( at + separator >= columns ) break;
			if ( segments.Count > 0 && at + separator + textColumns > columns ) continue;
			at += separator; int length = Math.Min( Math.Max( 1, textColumns ), columns - at );
			segments.Add( new Segment( field, at, length ) ); at += length;
		}
		this.visible = segments.ToArray();
		if ( !this.visible.Any( segment => IsActionable( segment.Field ) && StringComparer.Ordinal.Equals( segment.Field.Name, this.focusedFieldName ) ) )
			this.focusedFieldName = this.visible.FirstOrDefault( segment => IsActionable( segment.Field ) )?.Field.Name;
		this.pressedFieldName = null;
	}
}

