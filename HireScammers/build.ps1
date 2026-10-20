param([string]$GameDir=(Split-Path $PSScriptRoot))
$ErrorActionPreference='Stop'
$managed=Join-Path $GameDir 'Scam With Your Friends_Data/Managed'
$sdk=& dotnet --list-sdks | Select-Object -Last 1
if($sdk -notmatch '^(\S+)\s+\[(.+)\]$'){throw 'A .NET SDK is required'}
$csc=Join-Path (Join-Path $Matches[2] $Matches[1]) 'Roslyn/bincore/csc.dll'
New-Item -ItemType Directory -Force "$PSScriptRoot/bin" | Out-Null
$refs=@(Get-ChildItem $managed -Filter *.dll | Where-Object Name -ne 'HostBoss.Runtime.dll' | ForEach-Object {'/r:'+ $_.FullName})
$refs+=@(Get-ChildItem "$GameDir/BepInEx/core" -Filter *.dll | Where-Object Name -in @('BepInEx.dll','0Harmony.dll','ScamWYF.Modding.Core.dll') | ForEach-Object {'/r:'+ $_.FullName})
& dotnet $csc /nologo /noconfig /nostdlib+ /target:library /deterministic+ /optimize+ "/out:$PSScriptRoot/bin/ScamWYF.HireScammers.dll" $refs (Get-ChildItem "$PSScriptRoot/src" -Filter *.cs | ForEach-Object FullName)
if($LASTEXITCODE){throw 'Build failed'}
