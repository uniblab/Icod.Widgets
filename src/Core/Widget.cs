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

/// <summary>Base class for retained terminal controls.</summary>
public abstract class Widget {
	private bool isEnabled = true;
	private bool isVisible = true;

	protected Widget( string id ) {
		if ( string.IsNullOrWhiteSpace( id ) ) throw new ArgumentException( "A widget id is required.", nameof( id ) );
		this.Id = id;
	}

	public string Id { get; }
	public CursesRectangle Bounds { get; private set; }
	public virtual bool IsFocusable => false;
	public virtual bool AcceptsTab => false;
	/// <summary>Gets or sets optional part styles for this widget and its descendants.</summary>
	public WidgetStyles Styles { get; set; } = WidgetStyles.Empty;
	public bool IsEnabled {
		get => this.isEnabled;
		set { if ( this.isEnabled == value ) return; this.isEnabled = value; this.Host?.SynchronizeTree( this ); }
	}
	public bool IsVisible {
		get => this.isVisible;
		set { if ( this.isVisible == value ) return; this.isVisible = value; this.Host?.SynchronizeTree( this ); }
	}

	internal WidgetContainer? Parent { get; set; }
	internal WidgetHost? Host { get; set; }
	internal bool IsEffectivelyEnabled => this.isEnabled && ( this.Parent?.IsEffectivelyEnabled ?? true );
	internal bool IsEffectivelyVisible => this.isVisible && ( this.Parent?.IsEffectivelyVisible ?? true );

	public void Arrange( CursesRectangle bounds ) {
		this.Bounds = bounds;
		this.OnArrange( bounds );
		this.Host?.Synchronize( this );
	}

	protected virtual void OnArrange( CursesRectangle bounds ) { }
	protected internal virtual bool OnInput( WidgetInput input, WidgetHost host ) => false;
	protected internal virtual void OnRender( CursesScreen screen, WidgetTheme theme, bool focused ) { }
	/// <summary>Resolves a style from the nearest widget override, then the host theme.</summary>
	protected CursesStyle Style( WidgetTheme theme, WidgetStyleRole role ) {
		ArgumentNullException.ThrowIfNull( theme );
		for ( Widget? current = this; current is not null; current = current.Parent )
			if ( current.Styles.TryGetValue( role, out CursesStyle style ) ) return style;
		return theme.Resolve( role );
	}
	protected bool HasPartStyle( WidgetTheme theme, WidgetStyleRole role ) {
		for ( Widget? current = this; current is not null; current = current.Parent )
			if ( current.Styles.TryGetValue( role, out _ ) ) return true;
		return theme.Parts.TryGetValue( role, out _ );
	}
	internal CursesStyle ResolveStyle( WidgetTheme theme, WidgetStyleRole role ) => this.Style( theme, role );
	protected void EnsureActionCapacity( int count = 1 ) => this.RequireHost().EnsureActionCapacityFor( this, count );
	protected void EmitAction( WidgetAction action ) => this.RequireHost().EmitFor( this, action );
	protected WidgetClipboardRequest RegisterClipboardRequest( WidgetClipboardRequest request ) => this.RequireHost().RegisterClipboardRequest( this, request );
	protected virtual void OnCaptureCanceled() { }
	protected virtual void OnUpdate( TimeSpan timestamp ) { }
	protected virtual WidgetCaretIntent? GetCaretIntent() => null;
	internal virtual void OnPointerCaptureCanceled() => this.OnCaptureCanceled();
	internal virtual void OnDetached() { }
	internal void OnHostUpdate( TimeSpan timestamp ) => this.OnUpdate( timestamp );
	internal WidgetCaretIntent? GetHostCaretIntent() => this.GetCaretIntent();

	internal virtual IEnumerable<Widget> DescendantsAndSelf() { yield return this; }

	private WidgetHost RequireHost() => this.Host ?? throw new InvalidOperationException( "The widget is not attached to a host." );
}
