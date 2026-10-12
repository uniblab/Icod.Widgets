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

/// <summary>Base class for widgets that own an ordered child collection.</summary>
public abstract class WidgetContainer : Widget {
	private readonly List<Widget> children = [];
	protected WidgetContainer( string id ) : base( id ) { }
	public IReadOnlyList<Widget> Children => this.children;

	public void Add( Widget child ) {
		ArgumentNullException.ThrowIfNull( child );
		this.ValidateChildAddition( child );
		if ( child.Parent is not null || child.Host is not null ) throw new InvalidOperationException( "The widget already belongs to a tree." );
		if ( ReferenceEquals( child, this ) ) throw new InvalidOperationException( "A widget cannot contain itself." );
		if ( child.DescendantsAndSelf().Any( item => ReferenceEquals( item, this ) ) ) throw new InvalidOperationException( "A widget cannot contain one of its ancestors." );
		this.Host?.AttachChild( child );
		child.Parent = this;
		this.children.Add( child );
		this.OnArrange( this.Bounds );
	}
	internal virtual void ValidateChildAddition( Widget child ) { }

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

