# build_and_publish.ps1 - Повна збірка та створення інсталятора (v4.0)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   WPronto v4.0 - Build & Publish" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Крок 0: Зупинити всі процеси (ВАЖЛИВО!)
Write-Host "?? Зупиняємо серверні процеси..." -ForegroundColor Yellow
taskkill /F /IM nginx.exe 2>$null
taskkill /F /IM php-cgi.exe 2>$null
taskkill /F /IM mysqld.exe 2>$null
Start-Sleep -Seconds 2
Write-Host "? Серверні процеси зупинено" -ForegroundColor Green

# Крок 1: Очищення старих збірок
Write-Host "`n?? Очищення старих файлів..." -ForegroundColor Yellow
Remove-Item "C:\WPronto\publish" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "? Очищено" -ForegroundColor Green

# Крок 2: Очищення папки data\mysql
Write-Host "`n?? Очищення тестових баз даних..." -ForegroundColor Yellow
if (Test-Path "C:\WPronto\data\mysql") {
    Remove-Item "C:\WPronto\data\mysql\*_db" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "C:\WPronto\data\mysql\*.err" -Force -ErrorAction SilentlyContinue
    Remove-Item "C:\WPronto\data\mysql\*.pid" -Force -ErrorAction SilentlyContinue
    Write-Host "   ? Тестові бази очищено" -ForegroundColor Green
}

# Підготовка структури MySQL
Write-Host "   ?? Підготовка структури MySQL..." -ForegroundColor Yellow
$coreMysqlData = "C:\WPronto\core\mysql\data"
if (-not (Test-Path $coreMysqlData)) {
    New-Item -ItemType Directory -Path $coreMysqlData -Force | Out-Null
    Write-Host "   ? Створено папку: core\mysql\data" -ForegroundColor Green
}

# Крок 3: Перехід у папку проєкту
Write-Host "`n?? Перехід до папки проекту..." -ForegroundColor Yellow
cd "C:\WPronto\src\WPLaunchGUI"
Write-Host "? Поточна папка: $(Get-Location)" -ForegroundColor Green

# Крок 4: Відновлення пакетів
Write-Host "`n?? Відновлення пакетів..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Помилка відновлення пакетів!" -ForegroundColor Red
    exit 1
}
Write-Host "? Пакети відновлено" -ForegroundColor Green

# Крок 5: Компіляція Release версії
Write-Host "`n?? Компіляція проекту (Release)..." -ForegroundColor Yellow
dotnet build -c Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Помилка компіляції!" -ForegroundColor Red
    exit 1
}
Write-Host "? Компіляція успішна" -ForegroundColor Green

# Крок 6: Публікація
Write-Host "`n?? Публікація проекту..." -ForegroundColor Yellow
dotnet publish -c Release -o "C:\WPronto\publish" --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Помилка публікації!" -ForegroundColor Red
    exit 1
}
Write-Host "? Публікація успішна" -ForegroundColor Green

# Крок 7: Копіювання папок з сервером
Write-Host "`n?? Копіювання серверних папок..." -ForegroundColor Yellow
$folders = @("core", "config", "data", "template", "www", "logs", "backups", "tmp")
foreach ($folder in $folders) {
    $sourcePath = "C:\WPronto\$folder"
    $destPath = "C:\WPronto\publish\$folder"
    if (Test-Path $sourcePath) {
        Copy-Item -Path $sourcePath -Destination $destPath -Recurse -Force
        Write-Host "   ? Скопійовано: $folder"
    } else {
        New-Item -ItemType Directory -Path $destPath -Force | Out-Null
        Write-Host "   ?? Папка створена (була відсутня): $folder" -ForegroundColor Yellow
    }
}

# Крок 8: Копіювання файлів (оновлено - без about.txt)
Write-Host "`n?? Копіювання файлів..." -ForegroundColor Yellow
$filesToCopy = @("help.txt", "license.txt", "app.ico")
foreach ($file in $filesToCopy) {
    $sourcePath = "C:\WPronto\$file"
    $destPath = "C:\WPronto\publish\$file"
    if (Test-Path $sourcePath) {
        Copy-Item -Path $sourcePath -Destination $destPath -Force
        Write-Host "   ? Скопійовано: $file"
    } else {
        Write-Host "   ?? Файл не знайдено: $file" -ForegroundColor Yellow
    }
}

# Додатково копіюємо app.ico з src, якщо немає в корені
if (-not (Test-Path "C:\WPronto\app.ico")) {
    $srcIcon = "C:\WPronto\src\WPLaunchGUI\app.ico"
    if (Test-Path $srcIcon) {
        Copy-Item -Path $srcIcon -Destination "C:\WPronto\publish\app.ico" -Force
        Write-Host "   ? Скопійовано: app.ico (з src)" -ForegroundColor Green
    }
}

# Крок 9: Виправлення my.ini (відносні шляхи)
Write-Host "`n?? Виправлення my.ini..." -ForegroundColor Yellow
$myIniPath = "C:\WPronto\publish\data\mysql\my.ini"
if (Test-Path $myIniPath) {
    $content = Get-Content $myIniPath -Raw
    $content = $content -replace 'C:/WPLaunch/data/mysql', './data/mysql'
    $content = $content -replace 'C:\\WPLaunch\\core\\mysql', './core/mysql'
    $content = $content -replace 'C:/WPronto/data/mysql', './data/mysql'
    $content = $content -replace 'C:\\WPronto\\core\\mysql', './core/mysql'
    Set-Content $myIniPath $content -NoNewline
    Write-Host "   ? my.ini виправлено" -ForegroundColor Green
} else {
    Write-Host "   ?? Файл my.ini не знайдено, створюємо стандартний..." -ForegroundColor Yellow
    $defaultMyIni = @"
[mysqld]
basedir=./core/mysql
datadir=./data/mysql
port=3306
bind-address=127.0.0.1
"@
    $defaultMyIni | Set-Content -Path "C:\WPronto\publish\data\mysql\my.ini" -Encoding ASCII
    Write-Host "   ? Створено my.ini" -ForegroundColor Green
}

# Крок 10: Створення папки для сумісності
Write-Host "`n?? Створення структури для сумісності..." -ForegroundColor Yellow
$publishCoreMysqlData = "C:\WPronto\publish\core\mysql\data"
if (-not (Test-Path $publishCoreMysqlData)) {
    New-Item -ItemType Directory -Path $publishCoreMysqlData -Force | Out-Null
    Write-Host "   ? Створено папку для сумісності: core\mysql\data" -ForegroundColor Green
}

# Крок 11: Створення інсталятора
Write-Host "`n?? Створення інсталятора..." -ForegroundColor Yellow

$innoPaths = @(
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)

$iscc = $null
foreach ($path in $innoPaths) {
    if (Test-Path $path) {
        $iscc = $path
        break
    }
}

if ($iscc) {
    $issFile = "C:\WPronto\installer_final.iss"
    if (Test-Path $issFile) {
        Write-Host "   Запуск Inno Setup Compiler..." -ForegroundColor Yellow
        & $iscc $issFile
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Інсталятор створено в C:\WPronto\Installer\" -ForegroundColor Green
        } else {
            Write-Host "? Помилка створення інсталятора (код: $LASTEXITCODE)" -ForegroundColor Red
        }
    } else {
        Write-Host "   ?? Файл installer_final.iss не знайдено" -ForegroundColor Yellow
        Write-Host "   ?? Очікуваний шлях: C:\WPronto\installer_final.iss" -ForegroundColor Gray
    }
} else {
    Write-Host "?? Inno Setup не знайдено!" -ForegroundColor Red
    Write-Host "   Завантажте з: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
}

# Крок 12: Показуємо інформацію про збірку
Write-Host "`n========================================" -ForegroundColor Cyan
$publishSize = [math]::Round((Get-ChildItem "C:\WPronto\publish" -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1MB, 2)
$fileCount = (Get-ChildItem "C:\WPronto\publish" -Recurse -File -ErrorAction SilentlyContinue).Count

Write-Host "?? ІНФОРМАЦІЯ ПРО ЗБІРКУ:" -ForegroundColor Cyan
Write-Host "   ?? Версія: 4.0" -ForegroundColor White
Write-Host "   ?? Розмір: $publishSize MB" -ForegroundColor White
Write-Host "   ?? Файлів: $fileCount" -ForegroundColor White
Write-Host "   ?? Розташування: C:\WPronto\publish" -ForegroundColor White

if (Test-Path "C:\WPronto\publish\WProntoGUI.exe") {
    $exeSize = [math]::Round((Get-Item "C:\WPronto\publish\WProntoGUI.exe").Length / 1KB, 2)
    Write-Host "   ? Основний EXE файл: $exeSize KB" -ForegroundColor Green
} else {
    Write-Host "   ? ОСНОВНИЙ EXE ФАЙЛ НЕ ЗНАЙДЕН!" -ForegroundColor Red
}
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`n? Готово! Збірка v4.0 успішно створена!" -ForegroundColor Green

# Опціонально: запустити програму після збірки
$runAfterBuild = Read-Host "`nЗапустити програму? (y/n)"
if ($runAfterBuild -eq 'y') {
    Start-Process "C:\WPronto\publish\WProntoGUI.exe"
}