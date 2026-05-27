@echo off
echo Creating nginx config file...

(
echo worker_processes  1;
echo.
echo events {
echo     worker_connections  1024;
echo }
echo.
echo http {
echo     server {
echo         listen       80;
echo         server_name  localhost;
echo         root   C:/WPLaunch/www/default;
echo         index  index.html;
echo.
echo         location / {
echo             try_files $uri $uri/ =404;
echo         }
echo     }
echo }
) > C:\WPLaunch\config\nginx\nginx_test.conf

echo Done! Config file created.
echo Now run: cd C:\WPLaunch\core\nginx
echo nginx.exe -c C:\WPLaunch\config\nginx\nginx_test.conf
pause