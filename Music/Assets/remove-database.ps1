Get-Process Music -ErrorAction SilentlyContinue | Stop-Process -Force
Remove-Item "$env:LOCALAPPDATA\.private\music\music.db" -Force -ErrorAction SilentlyContinue

$musicProcesses = Get-CimInstance Win32_Process |
    Where-Object { $_.CommandLine -match '(?i)D:\\Code\\music\\Music.*Music\.dll|Music\\bin\\Debug.*Music\.dll' }

$musicProcesses | Select-Object ProcessId, Name, CommandLine
$musicProcesses | ForEach-Object { Stop-Process -Id $_.ProcessId -Force }

Remove-Item "$env:LOCALAPPDATA\.private\music\music.db" -Force