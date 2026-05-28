param(
  [switch]$RemoveAppFiles
)

$ErrorActionPreference = "Stop"

$publishDir = Join-Path $env:LOCALAPPDATA "TerminalClockSpotify\app"
$startupDir = [Environment]::GetFolderPath("Startup")
$shortcutPath = Join-Path $startupDir "TerminalClockSpotify.lnk"

if (Test-Path $shortcutPath) {
  Remove-Item $shortcutPath -Force
  Write-Host "Removed startup shortcut at $shortcutPath"
}
else {
  Write-Host "Startup shortcut not found at $shortcutPath"
}

if ($RemoveAppFiles) {
  if (Test-Path $publishDir) {
    Remove-Item $publishDir -Recurse -Force
    Write-Host "Removed installed files at $publishDir"
  }
  else {
    Write-Host "Installed files not found at $publishDir"
  }
}
