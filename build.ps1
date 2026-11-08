$ErrorActionPreference = 'Stop'
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    $compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework 4.x compiler not found.' }
Push-Location $PSScriptRoot
try {
    $production=@(Get-ChildItem -LiteralPath $PSScriptRoot -File -Filter '*.cs' | Where-Object Name -ne 'IconBuilder.cs' | Select-Object -ExpandProperty FullName)
    $prologueScore=Join-Path $PSScriptRoot 'Assets\prologue.wav'
    if(-not (Test-Path -LiteralPath $prologueScore -PathType Leaf)){throw 'Missing embedded prologue score: Assets\prologue.wav'}
    $prologueResource='/resource:'+$prologueScore+',NeonRift.Prologue.wav'
    & $compiler /nologo /target:winexe /optimize+ /platform:anycpu /main:NeonRift.Program '/out:NeonRift.exe' '/win32icon:NeonRift.ico' '/win32manifest:app.manifest' /r:System.Drawing.dll /r:System.Windows.Forms.dll $prologueResource @production
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
    Write-Host "Built: $PSScriptRoot\NeonRift.exe"
} finally { Pop-Location }


