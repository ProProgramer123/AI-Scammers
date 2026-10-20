#!/usr/bin/env bash
set -euo pipefail
if [[ $# -ne 1 ]]; then printf 'Usage: bash Setup-Linux.sh "/path/to/Scam With Your Friends"\n' >&2; exit 2; fi
package_root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P)
game_root=$(cd -- "$1" && pwd -P)
for tool in sha256sum cp mkdir pgrep awk grep cmp rm date dirname; do command -v "$tool" >/dev/null || { printf 'Missing tool: %s\n' "$tool" >&2; exit 1; }; done
for relative in 'Scam With Your Friends.exe' 'Scam With Your Friends_Data/Managed/Assembly-CSharp.dll' 'Scam With Your Friends_Data/Managed/ScriptsAssDef.dll'; do
    [[ -f "$game_root/$relative" ]] || { printf 'Choose the Windows game installation used by Steam/Proton.\n' >&2; exit 1; }
done
if pgrep -fi '[S]cam With Your Friends[.]exe' >/dev/null; then printf 'Close the game before installing.\n' >&2; exit 1; fi
for relative in BepInEx/core/BepInEx.dll BepInEx/core/0Harmony.dll winhttp.dll unstripped_corlib/mscorlib.dll; do
    [[ -f "$game_root/$relative" ]] || { printf 'Install the official ScamWYF loader first. See INSTALL.md.\n' >&2; exit 1; }
done
grep -Eq 'dll_search_path_override[[:space:]]*=[[:space:]]*unstripped_corlib' "$game_root/doorstop_config.ini" || { printf 'Official loader compatibility configuration is missing.\n' >&2; exit 1; }
files=(BepInEx/core/ScamWYF.Modding.Core.dll BepInEx/plugins/ScamWYF.HireScammers.dll)
for relative in "${files[@]}"; do
    expected=$(awk -v path="$relative" '$2 == path {print $1}' "$package_root/SHA256SUMS.txt")
    actual=$(sha256sum -- "$package_root/$relative"); actual=${actual%% *}
    [[ -n "$expected" && "$expected" == "$actual" ]] || { printf 'Package checksum mismatch: %s\n' "$relative" >&2; exit 1; }
done
backup="$game_root/AI-Scammers/backup/$(date +%Y%m%d-%H%M%S)-$$"
changed=(); existed=()
rollback(){
    local status=$? index
    trap - ERR
    for ((index=${#changed[@]}-1;index>=0;index--)); do
        if [[ ${existed[index]} == yes ]]; then cp -p -- "$backup/${changed[index]}" "$game_root/${changed[index]}" || printf 'Restore failed: %s\n' "${changed[index]}" >&2
        else rm -f -- "$game_root/${changed[index]}"; fi
    done
    printf 'Installation failed. Backups: %s\n' "$backup" >&2
    exit "$status"
}
trap rollback ERR
for relative in "${files[@]}"; do
    [[ "$package_root" != "$game_root" ]] || continue
    # Shared mod-lib is retained if already present; it may belong to a newer mod.
    if [[ "$relative" == */core/* && -f "$game_root/$relative" ]]; then printf 'Keeping existing shared mod library (requires 1.1.0+).\n'; continue; fi
    had_file=no
    if [[ -f "$game_root/$relative" ]]; then mkdir -p -- "$backup/$(dirname -- "$relative")"; cp -p -- "$game_root/$relative" "$backup/$relative"; had_file=yes; fi
    changed+=("$relative"); existed+=("$had_file")
    mkdir -p -- "$game_root/$(dirname -- "$relative")"
    cp -- "$package_root/$relative" "$game_root/$relative"
    cmp -s -- "$package_root/$relative" "$game_root/$relative"
done
trap - ERR
printf 'AI Scammers 0.3.5 installed. Backups (if needed): %s\n' "$backup"
printf 'Steam launch option: WINEDLLOVERRIDES="winhttp=n,b" %%command%%\n'
printf 'Merge with existing launch options. Use Steam/Proton; Linux gameplay has not been verified.\n'
