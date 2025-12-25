# WinKeySwitch

[English version](README.md)

Небольшая утилита для Windows, которая переназначает клавишу **Win** (Left/Right) на переключение раскладки клавиатуры через вашу системную комбинацию **Alt+Shift**.

Технически это:
- low-level keyboard hook (`WH_KEYBOARD_LL`), который перехватывает `LWin/RWin`
- **Одиночное нажатие Win** (тап и отпускание) переключает раскладку через `SendInput` (эмуляция `Alt+Shift`)
- **Win + другие клавиши** (например, `Win+D`, `Win+E`, `Win+L`) работают как обычно — утилита переинжектит событие Win для сохранения стандартных комбинаций Windows

Важно: многие игры/античиты могут считать любые перехватчики ввода «подозрительными». Используйте на свой риск.

## Требования
- Windows 10/11
- .NET SDK 8 (для сборки из исходников)

## Установка

### Вариант 1: через инсталлятор (рекомендуется)
- Скачайте `WinKeySwitchSetup.exe` из последнего релиза
- Во время установки можно отметить чекбокс «Запускать WinKeySwitch автоматически при входе» — добавит запись в HKCU\Run

### Вариант 2: из исходников
```powershell
# из папки проекта
$env:PATH = [System.Environment]::GetEnvironmentVariable('Path','Machine') + ';' + [System.Environment]::GetEnvironmentVariable('Path','User')

dotnet build .\WinKeySwitch.csproj -c Release
Start-Process .\bin\Release\net8.0-windows\WinKeySwitch.exe
```

Готовый бинарник будет здесь: `bin\Release\net8.0-windows\WinKeySwitch.exe`

## Использование
- нажмите **Win** соло (быстрый тап: нажал/отпустил без других клавиш) — раскладка переключится, меню «Пуск» не откроется
- **Win + другие клавиши** (например, `Win+D`, `Win+E`, `Win+L`, `Win+1-9`) работают как обычно — все стандартные комбинации Windows сохранены

## Автозагрузка (вручную)
Если пропустили чекбокс в инсталляторе — два варианта:

### Вариант A: через реестр (HKCU)
```powershell
$exe = (Resolve-Path .\bin\Release\net8.0-windows\WinKeySwitch.exe).Path
New-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "WinKeySwitch" -PropertyType String -Value "`"$exe`"" -Force | Out-Null
```

### Вариант B: ярлык в папке Startup
```powershell
$exe = (Resolve-Path .\bin\Release\net8.0-windows\WinKeySwitch.exe).Path
$startup = "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup"
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut("$startup\WinKeySwitch.lnk")
$Shortcut.TargetPath = $exe
$Shortcut.WorkingDirectory = Split-Path $exe
$Shortcut.Save()
```

## Остановка / удаление
Остановить процесс:
```powershell
Stop-Process -Name WinKeySwitch -ErrorAction SilentlyContinue
```

Убрать из автозагрузки (реестр):
```powershell
Remove-ItemProperty -Path "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run" -Name "WinKeySwitch" -ErrorAction SilentlyContinue
```

Убрать ярлык из Startup:
```powershell
Remove-Item "$env:APPDATA\Microsoft\Windows\Start Menu\Programs\Startup\WinKeySwitch.lnk" -ErrorAction SilentlyContinue
```

## Логи
Приложение пишет лог в:
`%LOCALAPPDATA%\WinKeySwitch\WinKeySwitch.log`

Полезно для диагностики (например, если `SendInput` возвращает ошибку).

## Примечания
- Утилита ориентируется на то, что в настройках Windows переключение раскладки назначено на **Alt+Shift**.
- Некоторые приложения/игры могут блокировать перехват ввода.
