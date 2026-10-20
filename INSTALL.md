# Windows and Linux setup

AI Scammers 0.3.5 is a BepInEx plugin for Scam With Your Friends 0.1.82-swyf. Both installers update the mod and its shared library in an **existing configured mod loader**. They do not download or repair the loader, change Steam settings, or install HostBoss.

## First-time loader installation

Install the game's [official ScamWYF Setup](https://github.com/swyf-modding/Setup) first. This game needs its compatibility setup and unstripped corlib override in addition to BepInEx 5.4.23.5 x64. A plain BepInEx ZIP alone is insufficient for this game build. Launch once to confirm the loader works, then close the game. The shared ScamWYF mod-lib 1.1.0 is included with this mod; a newer installed shared library should be retained.

## Windows

Download the AI Scammers release ZIP and extract the entire archive. Keep the scripts, SHA256SUMS.txt and BepInEx folder together. Open PowerShell in the extracted folder:

```powershell
.\Setup-Windows.ps1 -GameDir 'C:\Program Files (x86)\Steam\steamapps\common\Scam With Your Friends'
```

Use your Steam library's actual path. `-WhatIf` validates the package and prerequisites without installing. If Windows marks a downloaded script as blocked, inspect it and use the file's Properties > Unblock option. Installation checks the two DLL hashes, keeps a newer shared library, backs up replaced files and attempts restoration on failure. Start the game normally through Steam afterward.

## Linux / Steam Deck (Windows game through Proton)

Use the Linux instructions supplied by official ScamWYF Setup to configure its loader first. These scripts target the Windows executable running under Proton, not a native Linux game build. Extract the AI Scammers ZIP, open a terminal there, and run:

```bash
bash Setup-Linux.sh "$HOME/.local/share/Steam/steamapps/common/Scam With Your Friends"
```

Steam Deck installations commonly use `~/.steam/steam/steamapps/common/Scam With Your Friends`; an SD card has a different library path. Select the actual folder through Steam > Properties > Installed Files > Browse. Required commands: bash, coreutils, awk, grep and pgrep. No root access is needed for a user-owned Steam library.

Set the game's Steam launch options to:

```text
WINEDLLOVERRIDES="winhttp=n,b" %command%
```

Merge this override with any existing settings. BepInEx documents why [Proton requires the winhttp override](https://docs.bepinex.dev/articles/advanced/proton_wine.html). The installer prints it and leaves Steam settings unchanged. Existing shared mod-lib is retained on Linux; ensure it is at least 1.1.0. Linux installation is checked with isolated fixtures; actual Linux/Proton gameplay is unverified on this Windows machine.

## Multiplayer and play

The host and hiring players need this same mod version. Vanilla guests have experimental NPC compatibility and do not get mod apps or custom live leaderboard rows. Configure the game's AI provider and your own API key in game. Keys and user settings are not included in the release. At week 2, complete three personal scam calls, buy Hire Scammers in Scamazon, then hire and configure workers. Scammer Screens comes with the same license.

To disable this mod, close the game and move only `BepInEx/plugins/ScamWYF.HireScammers.dll` out of the plugins folder. Keep the shared mod library and loader for other mods. Existing mod backups are under the game's `AI-Scammers/backup` folder; configurations and saves remain in place.
