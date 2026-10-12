# W1207 Save-Target Inspection

`WidgetSaveTargetPicker` composes a browser and editable target name. Its optional `IWidgetSaveTargetProvider` is separate from `IWidgetFileSystemProvider`, preserving all existing third-party implementations. The picker emits a typed inspection request and accepts a result only for its still-open picker, current generation, name, and directory. A valid observation completes with `WidgetSaveTargetSelection(TargetPath, ExistedAtInspection)`; invalid and unsupported observations display an error and keep the picker open. Escape completes Cancel.

The interface contains only `InspectTargetAsync`; neither this control nor the library writes, creates, truncates, or overwrites a target. The existence flag is a momentary observation. The application must recheck its write decision at save time because the filesystem can change between inspection and writing.

Verification: the new tests initially failed to compile for absent types; three focused tests and the whole net10 suite then passed: 265 tests, zero failures. Offline Roslyn/VSTest was used with existing dependency assemblies; restored SDK build and net8/net9 remain pending.
