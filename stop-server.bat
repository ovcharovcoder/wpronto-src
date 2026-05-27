@echo off
echo Stopping WPLaunch Server...
taskkill /f /im nginx.exe 2>nul
taskkill /f /im php-cgi.exe 2>nul
taskkill /f /im mysqld.exe 2>nul
echo ✅ All services stopped
pause