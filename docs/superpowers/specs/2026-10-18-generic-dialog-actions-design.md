# Generic Dialog Actions and Dedit Dirty-Close Design

## Status

Approved in conversation on 2026-10-18. This specification defines the
Icod.Widgets 1.8.0 public contract and the dependent Icod.Dedit 1.0.0
integration. Implementation proceeds in that order and does not merge either
repository without owner approval.

## Intent

Dialogs, menus, pop-ups and related controls should remain generic. A caller
must be able to choose button captions, semantic outcomes, mnemonics, the
default action, the cancel action and local presentation without requiring a
new factory for every application phrase.

Icod.Dedit needs this contract for its dirty-document close decision. A clean
document closes without a prompt. A dirty document presents a red warning with
Yes, Don't Save and Cancel. The labels do not redefine the semantic results:
Y remains Yes and N remains No even though No is captioned Don't Save. Cancel
is the safe default.

## Scope

### Icod.Widgets 1.8.0

- Make an action array the canonical message-dialog button model.
- Add explicit action mnemonics shared by message and form dialogs.
- Preserve keyboard, pointer, default, cancel, enablement and typed-result
  behavior through one activation route.
- Preserve the existing two-button `WidgetMessageDialog.YesNo` behavior for
  binary compatibility, but mark the factory obsolete with a warning that
  directs new callers to the action-array constructor.
- Retain `Ok` and `OkCancel` as supported convenience factories implemented
  through the canonical action model.
- Demonstrate generic captions, mnemonics, defaults, cancellation and local
  dialog styles in the gallery sample.

### Icod.Dedit 1.0.0 candidate

- Rebind to the published Icod.Widgets 1.8.0 package.
- Replace the Save/Don't Save/Cancel form-dialog factory in the document-close
  path with a generic message dialog whose outcomes are Yes, No and Cancel.
- Apply a red warning style only to dirty-document close prompts.
- Preserve immediate, prompt-free close for clean documents.
- Use the same decision for Close, Close All and Exit.

This work does not change save-conflict, overwrite, external-change, About,
picker or search dialogs. Widgets does not gain a warning severity or a
mandatory red palette.

## Public API

`WidgetDialogAction` remains the common public action description and gains an
optional mnemonic:

```csharp
public sealed record WidgetDialogAction(
	string Id,
	string Label,
	WidgetDialogOutcome Outcome
) {
	public char? Mnemonic { get; init; }
	public bool IsDefault { get; init; }
	public bool IsCancel { get; init; }
	public bool RequiresValidation { get; init; }
	public bool IsEnabled { get; init; } = true;
	public object? Value { get; init; }
}
```

`WidgetMessageDialog` gains a public constructor:

```csharp
public WidgetMessageDialog(
	string id,
	string title,
	string message,
	IReadOnlyList<WidgetDialogAction> actions
);
```

The dialog exposes its actions as a read-only collection. Completing an action
returns that action's configured `WidgetDialogOutcome` and `Value` in the
existing `WidgetDialogResult`.

```csharp
public IReadOnlyList<WidgetDialogAction> Actions { get; }
public void SetActionEnabled( string actionId, bool enabled );
```

Message dialogs reject action arrays that contain:

- no actions or more than the established item limit;
- null actions;
- blank or duplicate IDs;
- null labels;
- anything other than exactly one default action;
- more than one cancel action;
- duplicate case-insensitive mnemonics; or
- `RequiresValidation`, which remains a form-dialog feature.

The default action must be enabled when the dialog is constructed. Disabled
actions cannot be selected or activated. If a caller later disables a selected
action through the supported action-enablement API, selection advances to the
next enabled action. Disabling every action is rejected.

`WidgetFormDialog` uses the same mnemonic semantics and validation for its
actions while retaining its validation-request workflow.

## Compatibility and obsolescence

The existing `WidgetMessageDialog.YesNo(string, string, string)` factory keeps
its established behavior: it displays Yes and No, selects No, and reports
Cancel on Escape. Its metadata changes only by adding a warning-level
`ObsoleteAttribute`:

```csharp
[Obsolete(
	"Use the WidgetMessageDialog action-array constructor so captions, "
	+ "mnemonics, default, and cancel behavior are explicit."
	, false
)]
```

The formatting above is normative for the implementation. `false` keeps
existing source consumers buildable while producing a migration warning.
Existing compiled consumers remain binary compatible. No new YesNo-specific
factory is introduced; callers that need Yes/No/Cancel use the action array.

`Ok` and `OkCancel` remain supported and are rebuilt internally from actions.
Their public behavior and signatures do not change.

## Input and activation

Every activation source resolves a `WidgetDialogAction` and passes it through
one internal activation procedure.

- Enter activates the selected action.
- Left and Up select the previous enabled action.
- Right, Down and Tab select the next enabled action; Tab wraps.
- Escape activates the declared cancel action. If no cancel action exists, it
  retains the established synthetic Cancel completion.
- An unmodified text or character-key press matching an enabled action's
  mnemonic activates that action immediately. Matching is case-insensitive.
- Ctrl, Alt or other modified character input does not trigger a mnemonic.
- Key releases do not trigger actions.
- A primary-button press followed by a release over the same enabled action
  activates that action. Moving away or losing pointer capture cancels it.
- Disabled actions neither capture pointer input nor activate by mnemonic.

The procedure enforces action capacity once, closes the modal once and emits
one `WidgetDialogCompletedAction`. Message-dialog completion includes the
action's value. Form-dialog validation behavior remains generation-safe.

## Rendering

Every action is rendered as a bracketed button, for example `[Yes]`,
`[Don't Save]` and `[Cancel]`. Selection and default state use the established
dialog action roles rather than adding or removing brackets. Layout and hit
testing use terminal display-column measurement rather than UTF-16 length.
This is required for customized Unicode captions.

If an action's mnemonic occurs in its sanitized caption, the first
case-insensitive matching text element is underlined. The underline augments
the resolved action style, preserving caller-supplied foreground, background
and other supported attributes. A mnemonic that does not occur in the caption
still activates the action but has no fabricated visual marker.

Button rendering, display-column advances and pointer hit regions derive from
the same measured presentation. Narrow dialogs truncate safely without moving
the semantic action or mnemonic bindings.

## Dedit dirty-close dialog

Dedit creates the following actions:

```csharp
[
	new( "yes", "Yes", WidgetDialogOutcome.Yes ) {
		Mnemonic = 'Y'
	},
	new( "no", "Don't Save", WidgetDialogOutcome.No ) {
		Mnemonic = 'N'
	},
	new( "cancel", "Cancel", WidgetDialogOutcome.Cancel ) {
		Mnemonic = 'C',
		IsDefault = true,
		IsCancel = true
	}
]
```

The title identifies the document during sequential Close All and Exit work:
`Unsaved Changes — <sanitized caption>`. The message is exactly
`Do you want to save these changes?`

Dedit maps the outcomes as follows:

| Outcome | Application behavior |
| --- | --- |
| Yes | Save the document; close only after a successful save. An untitled document continues through Save As. |
| No | Deliberately discard the dirty state and close the document. |
| Cancel | Retain the document and stop the current close operation or later batch work. |

`Y`, `N`, `C`, Escape, Enter after navigation and pointer clicks all resolve
through these same actions. Enter without navigation selects Cancel. Failed or
conflicting saves retain the document and its dirty state.

The existing `HasUnsavedChanges` check remains the gate. Clean documents never
construct or display this dialog.

## Dedit warning presentation

`DeditTheme` supplies application-owned `WidgetStyles` for this dialog only.
The close coordinator receives those styles rather than knowing terminal color
capabilities itself.

In this contract, style means graphical presentation only. Styles may select
foreground and background colors and supported text attributes for the dialog
surface, message, title, border and action states. Styles never select the
default or cancel action, bind a mnemonic, change navigation or hit testing,
choose an outcome, or decide whether Dedit saves, discards or retains a
document. Those behaviors remain explicit in `WidgetDialogAction`, the dialog
input contract and Dedit's close coordinator.

On a color-capable terminal:

- dialog surface, message and ordinary actions use white on red;
- title and border use an emphasized white-on-red style;
- the focused action uses red on white so the initial Cancel selection is
  immediately visible;
- the default-action role remains visible when focus representation differs;
- disabled actions retain the red surface and use the best supported dim
  presentation.

Direct RGB is preferred when advertised. Otherwise Dedit uses supported
indexed red and white colors. Each color axis is used only when the terminal
reports support, and attributes are intersected with the terminal's advertised
attribute set and color restrictions.

On a monochrome terminal, Dedit uses default colors, emphasizes the dialog
with bold, underline, standout or the best supported alternative, and reserves
reverse video for the focused action. The prompt remains distinguishable
without pretending that red is available.

All other dialogs continue resolving the ordinary Dedit theme.

## Verification

### Widgets

Tests cover:

- constructor validation for IDs, labels, defaults, cancel actions, mnemonics,
  enabled state and forbidden message validation;
- read-only actions and exact outcome/value completion;
- case-insensitive Y/N/C activation from replayable text and key input;
- ignored modifiers and key releases;
- disabled-action navigation and activation rejection;
- Escape through the declared cancel action and synthetic cancellation when no
  cancel action exists;
- Enter, arrow and wrapping Tab behavior;
- pointer press/release equivalence, drag-away cancellation and capture loss;
- display-column-correct Unicode captions, mnemonic underline and pointer hit
  regions;
- custom per-dialog styles on surface, text, title, border and actions;
- form-dialog mnemonic parity;
- unchanged Ok, OkCancel and legacy YesNo behavior;
- reflection-visible `ObsoleteAttribute` with the approved message and
  `IsError == false`;
- public API fingerprint and package-consumer compilation; and
- gallery sample launch and interaction contracts.

README documentation remains concise and version-neutral. CHANGELOG and
curated 1.8.0 release notes carry the version history and migration guidance.
Package identity and exact-content verification are updated for 1.8.0.

### Dedit

Tests cover:

- no modal for a clean document;
- exact title, question, labels, outcomes, mnemonics and Cancel default for a
  dirty document;
- Y save, N discard, C/Escape cancel and Enter-default cancel;
- pointer activation taking the same close path as its mnemonic;
- named and untitled save success, Save As continuation, save failure and
  external-conflict retention;
- Close, Close All and Exit sequencing and cancellation;
- RGB, indexed-color and monochrome warning styles;
- ordinary dialogs retaining ordinary styles;
- exact Icod.Widgets 1.8.0 project and packaged dependency; and
- aligned README, CHANGELOG, curated release notes, library contract and
  qualification record.

## Delivery sequence

1. Open an Icod.Widgets 1.8.0 PR from this specification and implementation.
2. Develop each Widgets behavior test-first, validate all net8.0/net9.0/net10.0
   targets on Linux, Windows and macOS, and obtain owner approval before merge.
3. Publish Icod.Widgets 1.8.0 to NuGet and wait for public indexing.
4. Rebind the existing Icod.Dedit 1.0.0 PR to the published package.
5. Implement the Dedit dialog, close-outcome mapping and application-owned
   warning styles test-first.
6. Run Dedit's full three-target, three-OS build/test/pack/install matrix and
   retain live-terminal dirty-close verification as an owner release gate.

