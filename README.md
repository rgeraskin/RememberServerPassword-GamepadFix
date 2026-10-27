# RememberServerPassword Gamepad Fix

> **Temporary fix.** This mod only patches a bug in MagiCorp's [RememberServerPassword](https://thunderstore.io/c/valheim/p/MagiCorp/RememberServerPassword/). The fix has been sent upstream: [iMagic16/RememberServerPassword#3](https://github.com/iMagic16/RememberServerPassword/pull/3). Once a RememberServerPassword release includes it, uninstall this mod.

[RememberServerPassword](https://thunderstore.io/c/valheim/p/MagiCorp/RememberServerPassword/) adds a **Quick Connect** button to the main menu. With a gamepad (or the arrow keys) the button can't be used:

- D-pad up/down jumps straight over it: from *Start game* the selection goes on to *Cinematics*;
- the button isn't highlighted when it is selected.

This mod makes Quick Connect part of the menu's navigation, between *Start game* and the button below it, and gives it the same highlight as the other menu buttons. Nothing else changes.

## Install

Install with a mod manager (r2modman, Gale, Thunderstore App), or by hand: BepInExPack Valheim and RememberServerPassword first, then put `RememberServerPassword_GamepadFix.dll` into `BepInEx/plugins`. Client-side only; servers and other players don't need it.

Tested with RememberServerPassword 1.2.4. If a later RememberServerPassword fixes navigation itself, this mod notices that and doesn't touch the navigation again, so having both installed does no harm.

The log line `Linked Quick Connect into menu navigation: …` in `BepInEx/LogOutput.log` shows that it worked.

## Why it happens

Valheim's main menu buttons form an explicit up/down navigation chain, and the game keeps its own list of the menu buttons for the arrow keys, made before any mod runs. RememberServerPassword clones a menu button for Quick Connect, but neither the chain nor the list knows about the clone, so navigation skips it. It also replaces the clone's `Button` component with a fresh one and doesn't copy its animation triggers, which is what draws the highlight.

## Credits

The icon is RememberServerPassword's own (MIT, © 2026 MagiCorp) with a gamepad badge. The source code is on [GitHub](https://github.com/rgeraskin/RememberServerPassword-GamepadFix) (MIT).
