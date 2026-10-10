/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

using System.Globalization;

/// <summary>Parses a caller-culture number and enforces an inclusive range.</summary>
public sealed class WidgetNumberPrompt : WidgetTextPrompt {
	public WidgetNumberPrompt( string id, string title, CultureInfo culture, double minimum, double maximum, string value = "" ) : base( id, title, value ) { this.Culture = culture ?? throw new ArgumentNullException( nameof( culture ) ); if ( minimum > maximum ) throw new ArgumentOutOfRangeException( nameof( minimum ) ); this.Minimum = minimum; this.Maximum = maximum; }
	public CultureInfo Culture { get; }
	public double Minimum { get; }
	public double Maximum { get; }
	public string? Error { get; private set; }
	protected override bool Accept( WidgetHost host ) { if ( !double.TryParse( this.Value, NumberStyles.Number, this.Culture, out double value ) || value < this.Minimum || value > this.Maximum ) { this.Error = $"Enter a number from {this.Minimum} to {this.Maximum}."; return true; } this.Error = null; this.Finish( host, new WidgetDialogResult( WidgetDialogOutcome.Accepted, value ) ); return true; }
}
