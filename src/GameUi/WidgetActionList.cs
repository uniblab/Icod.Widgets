/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/
namespace Icod.Widgets;

/// <summary>Invokes the selected caller-supplied action key.</summary>
public sealed class WidgetActionList( string id ) : WidgetList( id, WidgetSelectionMode.Single ) {
	protected internal override bool OnInput( WidgetInput input, WidgetHost host ) {
		bool handled = base.OnInput( input, host );
		if ( WidgetInputKind.Key == input.Kind && WidgetKey.Enter == input.KeyCode && this.CursorKey is not null ) { host.Enqueue( new WidgetInvokedAction( this.Id, this.CursorKey ) ); return true; }
		return handled;
	}
}

