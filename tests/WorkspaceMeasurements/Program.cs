/*
	WorkspaceMeasurements
	Measures Icod.Widgets document workspace switching and tab layout.
	Copyright (C) 2026  Timothy J. Bruce <uniblab@hotmail.com>
*/

/*
	This program is free software: you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation, either version 3 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System.Diagnostics;
using Icod.DCurses;
using Icod.Widgets;

const int documentUnits = 16 * 1024 * 1024;
string[] text = Enumerable.Range( 0, 3 ).Select( _ => new string( 'a', documentUnits ) ).ToArray();
WidgetTextDocument[] documents = text.Select( value => new WidgetTextDocument( [ value.AsMemory() ] ) ).ToArray();
WidgetTextViewState[] views = documents.Select( document => new WidgetTextViewState( document ) ).ToArray();
WidgetTextEditor[] editors = views.Select( ( view, index ) => new WidgetTextEditor( "editor" + index, view ) ).ToArray();
using WidgetHost host = new( new CursesScreen( 80, 25 ), WidgetTheme.Default );
WidgetContentHost content = new( "content" ); host.Attach( content ); content.Arrange( host.Screen.Bounds );
for ( int i = 0; i < 20; i++ ) content.SetContent( editors[ i % 3 ] );
GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
long retainedBefore = GC.GetTotalMemory( false ); long before = GC.GetAllocatedBytesForCurrentThread(); Stopwatch watch = Stopwatch.StartNew();
for ( int i = 0; i < 10_000; i++ ) content.SetContent( editors[ i % 3 ] );
watch.Stop(); long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
if ( allocated / 10_000 >= 256 * 1024 ) throw new InvalidOperationException( $"Allocated {allocated / 10_000} bytes per switch (budget < 262144)." );
if ( documents.Any( document => document.Length != documentUnits || document.Revision != 0 ) ) throw new InvalidOperationException( "Switching copied or edited a document." );
Console.WriteLine( $"switches=10000 documents=3 x {documentUnits} UTF-16 units elapsed-ms={watch.Elapsed.TotalMilliseconds:F1} allocated-per-switch={allocated / 10_000} retained-delta={GC.GetTotalMemory( true ) - retainedBefore}" );
WidgetTabStrip tabs = new( "tabs" ); WidgetTab[] tabList = Enumerable.Range( 0, 500 ).Select( i => new WidgetTab( $"t{i}", $"Document {i}", i % 5 == 0 ) ).ToArray();
host.Attach( tabs ); tabs.SetTabs( tabList, "t499" );
foreach ( int width in new[] { 1, 12, 80 } ) {
	tabs.Arrange( new CursesRectangle( 0, 0, 1, width ) );
	if ( !tabs.VisibleKeys.Contains( "t499" ) ) throw new InvalidOperationException( "Overflow hid the active tab." );
	Console.WriteLine( $"tabs=500 width={width} visible={tabs.VisibleKeys.Count} active-visible=true" );
}
WidgetPopupMenu menu = new( "menu", [ WidgetMenuItem.Command( "a", "A", "a" ), WidgetMenuItem.Command( "b", "B", "b" ) ] );
for ( int i = 0; i < 1000; i++ ) menu.SetPresentation( [ new( "a", $"A{i}", true ), new( "b", "B", i % 2 == 0 ) ] );
Console.WriteLine( "menu-updates=1000 PASS" );
foreach ( WidgetTextViewState view in views ) view.Dispose();
