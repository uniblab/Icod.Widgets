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

using System.Globalization;
using Icod.DCurses;

/// <summary>Parses a caller-culture number and enforces an inclusive range.</summary>
public sealed class WidgetNumberPrompt : WidgetTextPrompt {
	public WidgetNumberPrompt( string id, string title, CultureInfo culture, double minimum, double maximum, string value = "" ) : base( id, title, value ) { this.Culture = culture ?? throw new ArgumentNullException( nameof( culture ) ); if ( minimum > maximum ) throw new ArgumentOutOfRangeException( nameof( minimum ) ); this.Minimum = minimum; this.Maximum = maximum; }
	public CultureInfo Culture { get; }
	public double Minimum { get; }
	public double Maximum { get; }
	public string? Error { get; private set; }
	protected override bool Accept( WidgetHost host ) { if ( !double.TryParse( this.Value, NumberStyles.Number, this.Culture, out double value ) || !double.IsFinite( value ) || value < this.Minimum || value > this.Maximum ) { this.Error = $"Enter a number from {this.Minimum} to {this.Maximum}."; return true; } this.Error = null; this.Finish( host, new WidgetDialogResult( WidgetDialogOutcome.Accepted, value ) ); return true; }
	protected internal override void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) { base.OnRender( screen, theme, focused ); if ( this.Error is not null ) WidgetBrowserView.WriteLine( screen, this.GetContentBounds( screen ), 1, this.Error, theme.Error ); }
}
