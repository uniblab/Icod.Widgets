# W1003 — Basic Form Controls

**Status:** complete

The first form family includes labels, buttons, text input, checkboxes, radio
groups, borders, and scroll indicators. Text editing and limits operate on whole
Unicode text elements. Paste converts whitespace runs to one space and removes
other control characters. Value changes and invocations are emitted once as
immutable actions; property setters do not execute application commands.

Keyboard activation, disabled behavior, matching pointer release, Unicode
deletion, paste normalization, text limits, and radio navigation are covered by
focused tests. The GPL gallery sample uses only public APIs and provides a
repeatable `--smoke` path for forms, focus, actions, and retained rendering.
