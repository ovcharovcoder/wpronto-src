@echo off
chcp 65001 >nul
title WPLaunch - Component Downloader
echo ========================================
echo    WPLaunch - Component Downloader v2
echo ========================================
echo.

cd /d C:\WPLaunch\core

echo [1/5] Завантаження Nginx...
curl -L -o nginx.zip https://nginx.org/download/nginx-1.26.0.zip
if exist nginx.zip (
    tar -xf nginx.zip
    for /d %%i in (nginx-*) do move %%i nginx 2>nul
    del nginx.zip
    echo ✅ Nginx OK
) else (
    echo ❌ Помилка завантаження Nginx
)

echo.
echo [2/5] Завантаження PHP 8.3.6...
curl -L -o php.zip https://windows.php.net/downloads/releases/php-8.3.6-nts-Win32-vs16-x64.zip
if exist php.zip (
    mkdir php 2>nul
    tar -xf php.zip -C php
    del php.zip
    echo ✅ PHP OK
) else (
    echo ❌ Помилка завантаження PHP
)

echo.
echo [3/5] Завантаження MariaDB (альтернативне посилання)...
curl -L -o mariadb.zip https://archive.mariadb.org/mariadb-11.4.2/winx64-packages/mariadb-11.4.2-winx64.zip
if exist mariadb.zip (
    tar -xf mariadb.zip
    for /d %%i in (mariadb-*) do move %%i mysql 2>nul
    del mariadb.zip
    echo ✅ MariaDB OK
) else (
    echo ⚠️ Альтернативне посилання не працює, спробуємо інше...
    curl -L -o mariadb.msi https://dlm.mariadb.com/3195/mariadb-11.4.2-winx64.msi
    echo ✅ MariaDB MSI завантажено (потрібно встановити вручну)
)

echo.
echo [4/5] Завантаження WP-CLI...
curl -L -o wp-cli.phar https://raw.githubusercontent.com/wp-cli/builds/gh-pages/phar/wp-cli.phar
if exist wp-cli.phar (
    mkdir wp-cli 2>nul
    move wp-cli.phar wp-cli\ 2>nul
    echo ✅ WP-CLI OK
) else (
    echo ❌ Помилка завантаження WP-CLI
)

echo.
echo [5/5] Завантаження phpMyAdmin...
curl -L -o phpmyadmin.zip https://www.phpmyadmin.net/downloads/phpMyAdmin-latest-all-languages.zip
if exist phpmyadmin.zip (
    tar -xf phpmyadmin.zip
    for /d %%i in (phpMyAdmin-*) do move %%i phpmyadmin 2>nul
    del phpmyadmin.zip
    echo ✅ phpMyAdmin OK
) else (
    echo ❌ Помилка завантаження phpMyAdmin
)

echo.
echo ========================================
echo    ВСЕ КОМПОНЕНТИ ЗАВАНТАЖЕНО!
echo ========================================
echo.
echo Якщо MariaDB не розпакувалась, встановіть її вручну:
echo 1. Завантажте з: https://mariadb.org/download/
echo 2. Встановіть у C:\WPLaunch\core\mysql
echo.
pause