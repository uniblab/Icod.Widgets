/*
	BinaryCompatibilityConsumer
	Compiles an Icod.Widgets consumer against the released 1.0 package.
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

using Icod.DCurses;
using Icod.Widgets;

using WidgetHost host = new( new CursesScreen( 40, 8 ), WidgetTheme.Default );
WidgetTextInput input = new( "input", "one" );
host.Attach( input );
input.Arrange( new CursesRectangle( 0, 0, 1, 20 ) );
host.Focus( input );
WidgetDispatchResult result = host.Process( WidgetInput.TextInput( new( '!' ) ), TimeSpan.Zero );
return result.Actions.SingleOrDefault() is WidgetValueChangedAction<string> { Value: "one!" } ? 0 : 1;
