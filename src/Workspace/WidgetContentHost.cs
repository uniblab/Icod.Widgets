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

/// <summary>Displays one caller-owned content tree at a time without disposing a replaced view.</summary>
public sealed class WidgetContentHost( string id ) : WidgetContainer( id ) {
	public Widget? Content => this.Children.Count == 0 ? null : this.Children[ 0 ];
	internal override void ValidateChildAddition( Widget child ) {
		if ( this.Children.Count != 0 ) throw new InvalidOperationException( "The content host must have at most one child." );
	}

	/// <summary>Replaces content on the caller's loop, preserving the old tree if validation fails.</summary>
	public void SetContent( Widget? content ) {
		Widget? previous = this.Content;
		if ( ReferenceEquals( previous, content ) ) return;
		if ( this.Children.Count > 1 ) throw new InvalidOperationException( "The content host must have at most one child." );
		if ( content is not null && ( content.Parent is not null || content.Host is not null ||
			content.DescendantsAndSelf().Any( child => ReferenceEquals( child, this ) ) ) )
			throw new InvalidOperationException( "The replacement already belongs to a widget tree." );
		this.Host?.ValidateReplacement( previous, content );
		Widget? focused = this.Host?.FocusedWidget;
		bool transferFocus = focused is not null && previous is not null && previous.DescendantsAndSelf().Contains( focused );
		if ( previous is not null ) this.Remove( previous );
		try {
			if ( content is not null ) this.Add( content );
			if ( transferFocus && content is not null ) {
				Widget? target = content.DescendantsAndSelf().FirstOrDefault( widget => widget.IsFocusable && widget.IsEffectivelyEnabled && widget.IsEffectivelyVisible && !widget.Bounds.IsEmpty );
				if ( target is not null ) this.Host?.Focus( target );
			}
		} catch {
			if ( content is not null && this.Children.Contains( content ) ) this.Remove( content );
			if ( previous is not null ) { this.Add( previous ); if ( transferFocus && focused is not null ) this.Host?.Focus( focused ); }
			throw;
		}
	}

	protected override void OnArrange( CursesRectangle bounds ) {
		foreach ( Widget child in this.Children ) child.Arrange( bounds );
	}
}
