# WinKeySwitch

[Русская версия](README.ru.md)

A lightweight Windows utility that remaps the **Win** key (Left/Right) to switch keyboard layouts using your system's **Alt+Shift** combination.

Technically it:
- Installs a low-level keyboard hook (`WH_KEYBOARD_LL`) to intercept `LWin/RWin` and suppress the default Start menu behavior
- Switches layouts via `SendInput` (emulating `Alt+Shift`) to behave like a "real" keypress

**Important**: Many games/anti-cheats may flag any input hooks as suspicious. Use at your own risk.

## Requirements
- Windows 10/11
- .NET SDK 8 (for building from source)

## Building
```powershell
# From the project directory
$env:PATH = [System.Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [System.Environment]::GetEnvironmentVariable('Path','User')

dotnet build .\WinKeySwitch.csproj -c Release
```

The compiled binary will be at:  
`bin\Release\net8.0-windows\WinKeySwitch.exe`

## Running
```powershell
Start-Process .\bin\Release\net8.0-windows\WinKeySwitch.exe
```

Usage:
- Press **Win** with a quick tap (press/release) — the keyboard layout will switch
- The Start menu will not open when pressing Win

## Autostart
Two reliable autostart methods are available.

### Option A: via Registry (recommended)
```powershell
$exe = (Resolve-Path .\bin\Release\net8.0-windows\WinKeySwitch.exe).Path
New-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "WinKeySwitch" -PropertyType String -Value "`"$exe`"" -Force | Out-Null
```

### Option B: Shortcut in Startup folder
```powershell
$exe = (Resolve-Path .\bin\Release\net8.0-windows\WinKeySwitch.exe).Path
$startup = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup"
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$startup\WinKeySwitch.lnk")
$Shortcut.TargetPath = $exe
$Shortcut.WorkingDirectory = Split-Path $exe
$Shortcut.Save()
```

## Stopping / Removal
Stop the process:
```powershell
Stop-Process -Name WinKeySwitch -ErrorAction SilentlyContinue
```

Remove from autostart (registry):
```powershell
Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "WinKeySwitch" -ErrorAction SilentlyContinue
```

Remove shortcut from Startup:
```powershell
Remove-Item "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\WinKeySwitch.lnk" -ErrorAction SilentlyContinue
```

## Logs
The application writes logs to:  
`%LOCALAPPDATA%\WinKeySwitch\WinKeySwitch.log`

Useful for diagnostics (e.g., if `SendInput` returns an error).

## Notes
- The utility assumes Windows layout switching is configured to use **Alt+Shift**
- Some applications/games may block input hooks
