# W1011 — Roguelike Conveniences

**Status:** complete

The game-oriented layer composes the same public controls without introducing a
game model. It provides multiple-selection inventory presentation, command-key
action lists, oldest-first bounded message history (default 1,000), prioritized
compact status fields, caller-resolved help bindings, and text/number prompts.

Number prompts parse with the caller's culture and inclusive range and keep the
dialog open with an error when invalid. Tests verify the dependency boundary,
1,001-message eviction, tiny status rendering, effective help labels, and
culture-aware numeric acceptance.
