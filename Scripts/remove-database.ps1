Get-Process Resona -ErrorAction SilentlyContinue | Stop-Process -Force
Remove-Item "$env:LOCALAPPDATA\.private\music\music.db" -Force -ErrorAction SilentlyContinue

$resonaProcesses = Get-CimInstance Win32_Process |
    Where-Object { $_.CommandLine -match '(?i)src\\Resona.*Resona\.dll|Resona\\bin\\Debug.*Resona\.dll' }

$resonaProcesses | Select-Object ProcessId, Name, CommandLine
$resonaProcesses | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }

Remove-Item "$env:LOCALAPPDATA\.private\music\music.db" -Force
