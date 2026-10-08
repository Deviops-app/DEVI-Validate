# Release smoke test for the window app. Run on Windows from an interactive desktop session.
# Launches DEVI-Validate.exe and checks that:
#   1. the real main window appears (a WPF HwndWrapper window, not an error message box),
#   2. the log records "Main window shown",
#   3. a second launch exits and leaves exactly one new process,
#   4. closing the window ends the process with exit code 0.
#   .\installer\smoke-launch.ps1 -Exe "C:\path\to\app\DEVI-Validate.exe"
param(
  [Parameter(Mandatory = $true)][string]$Exe,
  [int]$TimeoutSeconds = 20
)
$ErrorActionPreference = 'Stop'
Add-Type -Namespace DeviSmoke -Name Win32 -MemberDefinition @'
[DllImport("user32.dll", CharSet = CharSet.Unicode)]
public static extern int GetClassName(System.IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);
'@
function Get-ClassName([IntPtr]$h) { $sb = New-Object System.Text.StringBuilder 256; [void][DeviSmoke.Win32]::GetClassName($h, $sb, 256); $sb.ToString() }

$name = [IO.Path]::GetFileNameWithoutExtension($Exe)
$log = Join-Path $env:LOCALAPPDATA ("DEVI\Validate\logs\validate-{0}.log" -f (Get-Date -Format yyyyMMdd))
$logStart = if (Test-Path $log) { (Get-Content $log).Count } else { 0 }
$before = @(Get-Process -Name $name -ErrorAction SilentlyContinue | ForEach-Object Id)
if ($before.Count -gt 0) { Write-Warning "Already running: $($before -join ', ') (ignored; only new processes are counted)." }

$p = Start-Process -FilePath $Exe -WorkingDirectory (Split-Path $Exe) -PassThru
$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
do {
  Start-Sleep -Milliseconds 250
  $p.Refresh()
} until ($p.HasExited -or $p.MainWindowHandle -ne 0 -or (Get-Date) -gt $deadline)

$ok = $true
if ($p.HasExited) { Write-Host "FAIL: process exited with code $($p.ExitCode) before showing a window"; $ok = $false }
elseif ($p.MainWindowHandle -eq 0) { Write-Host "FAIL: no window after $TimeoutSeconds s (pid $($p.Id))"; $ok = $false }
else {
  Start-Sleep -Milliseconds 1500
  $p.Refresh()
  $cls = Get-ClassName $p.MainWindowHandle
  $desc = "window 0x{0:X} class '{1}' title '{2}' (pid {3})" -f [int64]$p.MainWindowHandle, $cls, $p.MainWindowTitle, $p.Id
  if ($cls -like 'HwndWrapper*') { Write-Host "PASS: main $desc" }
  else { Write-Host "FAIL: not the main window (an error dialog is class #32770): $desc"; $ok = $false }
}
$newLog = if (Test-Path $log) { @(Get-Content $log | Select-Object -Skip $logStart) } else { @() }
if ($newLog -match 'Main window shown') { Write-Host "PASS: log says 'Main window shown'" } else { Write-Host "FAIL: log has no 'Main window shown'"; $ok = $false }

if (-not $p.HasExited) {
  $second = Start-Process -FilePath $Exe -WorkingDirectory (Split-Path $Exe) -PassThru
  if (-not $second.WaitForExit(10000)) { Write-Host "FAIL: second launch did not exit (pid $($second.Id))"; $ok = $false; $second.Kill() }
  elseif ($second.ExitCode -ne 0) { Write-Host "FAIL: second launch exit code $($second.ExitCode)"; $ok = $false }
  else { Write-Host "PASS: second launch handed off and exited with code 0" }
  $now = @(Get-Process -Name $name -ErrorAction SilentlyContinue | Where-Object { $before -notcontains $_.Id })
  if ($now.Count -ne 1) { Write-Host "FAIL: expected 1 new process, found $($now.Count)"; $ok = $false }
  else { Write-Host "PASS: one new process running" }

  [void]$p.CloseMainWindow()
  if (-not $p.WaitForExit(10000)) { Write-Host "FAIL: did not close within 10 s"; $p.Kill(); $ok = $false }
  elseif ($p.ExitCode -ne 0) { Write-Host "FAIL: exit code $($p.ExitCode) after closing"; $ok = $false }
  else { Write-Host "PASS: closed with exit code 0" }
}
Write-Host "--- log ($log)"
if (Test-Path $log) { Get-Content $log | Select-Object -Skip $logStart | Select-Object -First 40 }
if ($ok) { Write-Host "SMOKE PASS"; exit 0 } else { Write-Host "SMOKE FAIL"; exit 1 }
