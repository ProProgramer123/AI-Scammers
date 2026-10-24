[CmdletBinding(SupportsShouldProcess=$true)]
param([Parameter(Mandatory=$true)][string]$GameDir)
$ErrorActionPreference='Stop'
$gameRoot=(Resolve-Path -LiteralPath $GameDir).Path
foreach($name in @('Scam With Your Friends.exe','Scam With Your Friends_Data/Managed/Assembly-CSharp.dll','Scam With Your Friends_Data/Managed/ScriptsAssDef.dll')) {
    if(-not(Test-Path -LiteralPath (Join-Path $gameRoot $name) -PathType Leaf)){throw 'Choose the installed Scam With Your Friends game folder.'}
}
if(Get-Process -Name 'Scam With Your Friends' -ErrorAction SilentlyContinue){throw 'Close the game before installing AI Scammers.'}
foreach($name in @('BepInEx/core/BepInEx.dll','BepInEx/core/0Harmony.dll','winhttp.dll','unstripped_corlib/mscorlib.dll')) {
    if(-not(Test-Path -LiteralPath (Join-Path $gameRoot $name) -PathType Leaf)){throw 'Install the official ScamWYF mod-loader setup first. See INSTALL.md.'}
}
if((Get-Content -LiteralPath (Join-Path $gameRoot 'doorstop_config.ini') -Raw) -notmatch 'dll_search_path_override\s*=\s*unstripped_corlib'){throw 'Run the official ScamWYF setup to configure its compatibility override first.'}
$files=@('BepInEx/core/ScamWYF.Modding.Core.dll','BepInEx/plugins/ScamWYF.HireScammers.dll')
$manifest=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'SHA256SUMS.txt')
foreach($relative in $files){
    $line=@($manifest | Where-Object { $_.EndsWith('  '+$relative) })
    if($line.Count -ne 1 -or (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $relative) -Algorithm SHA256).Hash -ne $line[0].Substring(0,64)){throw "Package checksum mismatch: $relative"}
}
if(-not $PSCmdlet.ShouldProcess($gameRoot,'Install AI Scammers and shared mod library')){return}
$backup=Join-Path $gameRoot ('AI-Scammers/backup/'+[Guid]::NewGuid().ToString('N'))
$changed=New-Object 'System.Collections.Generic.List[object]'
try {
    foreach($relative in $files){
        $source=Join-Path $PSScriptRoot $relative;$target=Join-Path $gameRoot $relative
        if($relative -like '*/core/*' -and (Test-Path -LiteralPath $target)){
            if([Reflection.AssemblyName]::GetAssemblyName($target).Version -gt [Reflection.AssemblyName]::GetAssemblyName($source).Version){Write-Output 'Keeping newer shared mod library.';continue}
        }
        if([IO.Path]::GetFullPath($source) -eq [IO.Path]::GetFullPath($target)){continue}
        $exists=Test-Path -LiteralPath $target -PathType Leaf;$saved=Join-Path $backup $relative
        if($exists){New-Item -ItemType Directory -Path (Split-Path $saved) -Force | Out-Null;Copy-Item -LiteralPath $target -Destination $saved}
        $changed.Add([pscustomobject]@{Target=$target;Saved=$saved;Existed=$exists})
        New-Item -ItemType Directory -Path (Split-Path $target) -Force | Out-Null
        Copy-Item -LiteralPath $source -Destination $target -Force
        if((Get-FileHash -LiteralPath $source).Hash -ne (Get-FileHash -LiteralPath $target).Hash){throw 'Installed file did not match package.'}
    }
} catch {
    foreach($entry in $changed){try{if($entry.Existed){Copy-Item -LiteralPath $entry.Saved -Destination $entry.Target -Force}elseif(Test-Path -LiteralPath $entry.Target){Remove-Item -LiteralPath $entry.Target}}catch{Write-Warning "Restore failed for $($entry.Target). Backup: $($entry.Saved)"}}
    throw
}
Write-Output "AI Scammers 0.3.8 installed. Backups (if needed): $backup"
Write-Output 'Start through Steam and host a lobby. Hire Scammers unlocks in week 2 after 3 personal calls.'
