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

/// <summary>Replaces one range addressed against a common document revision.</summary>
public sealed record WidgetTextEdit( long Start, long DeleteLength, string InsertText );

/// <summary>Identifies the user-visible operation represented by a transaction.</summary>
public enum WidgetTextEditKind { Programmatic, Typing, Backspace, Delete, Paste, Indent, Outdent, NewLine, Overwrite, Cut }

/// <summary>Supplies invoking-view state and explicit undo-grouping policy.</summary>
public sealed record WidgetTextEditContext(
	Guid ViewId,
	long BeforeAnchor,
	long BeforeCaret,
	long AfterAnchor,
	long AfterCaret,
	WidgetTextEditKind Kind,
	TimeSpan Timestamp,
	bool AllowCoalescing = false );

public enum WidgetTextEditStatus { Applied, NoChange, Stale, LimitExceeded, InvalidRange }

/// <summary>Represents a directional selection in UTF-16 document coordinates.</summary>
public readonly record struct WidgetTextSelection( long Anchor, long Caret );

/// <summary>Reports the outcome and resulting identity of a document operation.</summary>
public sealed record WidgetTextEditResult(
	WidgetTextEditStatus Status,
	long Revision,
	long StateId,
	Guid? ViewId = null,
	WidgetTextSelection? Selection = null );

/// <summary>Describes one committed replacement without carrying document text.</summary>
public readonly record struct WidgetTextChange( long Start, long OldLength, long NewLength );

public enum WidgetTextChangeReason { Apply, Undo, Redo }

/// <summary>Describes an already-committed document change.</summary>
public sealed class WidgetTextDocumentChangedEventArgs(
	Guid documentId,
	long revision,
	long stateId,
	WidgetTextChangeReason reason,
	IReadOnlyList<WidgetTextChange> changes ) : EventArgs {
	public Guid DocumentId { get; } = documentId;
	public long Revision { get; } = revision;
	public long StateId { get; } = stateId;
	public WidgetTextChangeReason Reason { get; } = reason;
	public IReadOnlyList<WidgetTextChange> Changes { get; } = Array.AsReadOnly( ( changes ?? throw new ArgumentNullException( nameof( changes ) ) ).ToArray() );
}
