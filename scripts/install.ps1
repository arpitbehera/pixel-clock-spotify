$ErrorActionPreference = "Stop"

$runtime = dotnet --list-runtimes | Select-String "Microsoft.WindowsDesktop.App 8."
if (-not $runtime) {
  Write-Host "Install the .NET 8 Desktop Runtime, then rerun this script."
  exit 1
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$publishDir = Join-Path $env:LOCALAPPDATA "TerminalClockSpotify\app"
$startupDir = [Environment]::GetFolderPath("Startup")
$shortcutPath = Join-Path $startupDir "TerminalClockSpotify.lnk"

dotnet publish (Join-Path $repoRoot "src\TerminalClockSpotify\TerminalClockSpotify.csproj") -c Release -f net8.0-windows10.0.22621.0 -o $publishDir --self-contained false

$shell = New-Object -ComObject WScript.Shell
$shortcut = $shell.CreateShortcut($shortcutPath)
$shortcut.TargetPath = Join-Path $publishDir "TerminalClockSpotify.exe"
$shortcut.WorkingDirectory = $publishDir
$shortcut.Save()

Write-Host "Installed TerminalClockSpotify to $publishDir"
Write-Host "Startup shortcut created at $shortcutPath"
