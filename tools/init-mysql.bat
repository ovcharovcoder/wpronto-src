@echo off
chcp 65001 >nul
title WPLaunch - MariaDB Initialization
echo ========================================
echo    WPLaunch - MariaDB Initialization
echo ========================================
echo.

cd /d C:\WPLaunch\core\mysql\bin

echo Створюємо папку для даних...
mkdir C:\WPLaunch\data\mysql 2>nul

echo Очищаємо папку даних (якщо щось було)...
rmdir /s /q C:\WPLaunch\data\mysql 2>nul
mkdir C:\WPLaunch\data\mysql

echo.
echo Ініціалізація MariaDB (це може зайняти 20-30 секунд)...
echo.

mysql_install_db.exe --datadir=C:\WPLaunch\data\mysql --service=WPLaunchMySQL

if %errorlevel% equ 0 (
    echo.
    echo ========================================
    echo    ✅ MariaDB успішно ініціалізовано!
    echo ========================================
    echo.
    echo 📌 Пароль root: порожній
    echo.
) else (
    echo.
    echo ❌ Помилка ініціалізації
    echo Спробуємо альтернативний спосіб...
    echo.
    
    mysqld --datadir=C:\WPLaunch\data\mysql --initialize
)

echo.
pause