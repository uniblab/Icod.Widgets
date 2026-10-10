/*
	Icod.Widgets
	Copyright (C) 2026 Timothy J. Bruce <uniblab@hotmail.com>
	SPDX-License-Identifier: LGPL-3.0-or-later
*/

namespace Icod.Widgets;

/// <summary>Base class for widgets that own an ordered child collection.</summary>
public abstract class WidgetContainer : Widget {
	private readonly List<Widget> children = [];
	protected WidgetContainer( string id ) : base( id ) { }
	public IReadOnlyList<Widget> Children => this.children;

	public void Add( Widget child ) {
		ArgumentNullException.ThrowIfNull( child );
		if ( child.Parent is not null || child.Host is not null ) throw new InvalidOperationException( "The widget already belongs to a tree." );
		if ( ReferenceEquals( child, this ) ) throw new InvalidOperationException( "A widget cannot contain itself." );
		this.Host?.AttachChild( child );
		child.Parent = this;
		this.children.Add( child );
		this.OnArrange( this.Bounds );
	}

	public bool Remove( Widget child ) {
		ArgumentNullException.ThrowIfNull( child );
		int index = this.children.IndexOf( child );
		if ( 0 > index ) return false;
		this.Host?.DetachChild( child );
		this.children.RemoveAt( index );
		child.Parent = null;
		this.OnArrange( this.Bounds );
		return true;
	}

	internal override IEnumerable<Widget> DescendantsAndSelf() {
		yield return this;
		foreach ( Widget child in this.children ) foreach ( Widget item in child.DescendantsAndSelf() ) yield return item;
	}
}

