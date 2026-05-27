@echo off
title WPronto Server
echo ========================================
echo    🚀 WPronto Server Starting...
echo ========================================
echo.

cd /d C:\WPronto\core\php
start /B php-cgi.exe -b 127.0.0.1:9000 -c C:\WPronto\config\php\php.ini
echo ✅ PHP started

cd /d C:\WPronto\core\mysql\bin
start /B mysqld.exe --datadir=C:\WPronto\data\mysql
echo ✅ MySQL started

cd /d C:\WPronto\core\nginx
start /B nginx.exe -c C:\WPronto\config\nginx\nginx.conf
echo ✅ Nginx started

echo.
echo ========================================
echo    ✅ WPronto is running!
echo    🌐 Open http://localhost
echo ========================================
pause