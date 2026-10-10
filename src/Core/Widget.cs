/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
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
	internal virtual void OnPointerCaptureCanceled() { }

	internal virtual IEnumerable<Widget> DescendantsAndSelf() { yield return this; }
}
