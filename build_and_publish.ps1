# build_and_publish.ps1 - Full build and installer creation (v5.0) with ngrok

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "   WPronto v5.0 - Build & Publish" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Step 0: Stop all processes (IMPORTANT!)
Write-Host "?? Stopping server processes..." -ForegroundColor Yellow
taskkill /F /IM nginx.exe 2>$null
taskkill /F /IM php-cgi.exe 2>$null
taskkill /F /IM mysqld.exe 2>$null
taskkill /F /IM ngrok.exe 2>$null
Start-Sleep -Seconds 2
Write-Host "? Server processes stopped" -ForegroundColor Green

# Step 1: Clean old builds
Write-Host "`n?? Cleaning old files..." -ForegroundColor Yellow
Remove-Item "C:\WPronto\publish" -Recurse -Force -ErrorAction SilentlyContinue
Write-Host "? Cleaned" -ForegroundColor Green

# Step 2: Ensure tools folder exists and ngrok is present
Write-Host "`n?? Checking ngrok..." -ForegroundColor Yellow
$toolsPath = "C:\WPronto\tools"
if (-not (Test-Path $toolsPath)) {
    New-Item -ItemType Directory -Path $toolsPath -Force | Out-Null
    Write-Host "   ?? Created tools folder" -ForegroundColor Green
}

$ngrokPath = "C:\WPronto\tools\ngrok.exe"
if (-not (Test-Path $ngrokPath)) {
    Write-Host "   ?? ngrok.exe not found!" -ForegroundColor Yellow
    Write-Host "   ?? Downloading ngrok.exe..." -ForegroundColor Yellow
    
    try {
        $url = "https://bin.equinox.io/c/bNyj1mQVY4c/ngrok-v3-stable-windows-amd64.zip"
        $zipPath = "C:\WPronto\tools\ngrok.zip"
        
        Write-Host "   ?? Downloading from: $url" -ForegroundColor Gray
        Invoke-WebRequest -Uri $url -OutFile $zipPath -UseBasicParsing
        
        if (Test-Path $zipPath) {
            Write-Host "   ?? Extracting ngrok.exe..." -ForegroundColor Yellow
            Expand-Archive -Path $zipPath -DestinationPath $toolsPath -Force
            Remove-Item $zipPath -Force
            
            if (Test-Path $ngrokPath) {
                $size = [math]::Round((Get-Item $ngrokPath).Length / 1MB, 2)
                Write-Host "   ? Downloaded! Size: $size MB" -ForegroundColor Green
            } else {
                Write-Host "   ? Failed to extract ngrok.exe!" -ForegroundColor Red
                Write-Host "   ?? Please download manually from:" -ForegroundColor Yellow
                Write-Host "   https://ngrok.com/download" -ForegroundColor Yellow
                Write-Host "   And place ngrok.exe in:" -ForegroundColor Yellow
                Write-Host "   C:\WPronto\tools\ngrok.exe" -ForegroundColor Yellow
                exit 1
            }
        } else {
            Write-Host "   ? Failed to download ngrok!" -ForegroundColor Red
            Write-Host "   ?? Please download manually from:" -ForegroundColor Yellow
            Write-Host "   https://ngrok.com/download" -ForegroundColor Yellow
            Write-Host "   And place ngrok.exe in:" -ForegroundColor Yellow
            Write-Host "   C:\WPronto\tools\ngrok.exe" -ForegroundColor Yellow
            exit 1
        }
    }
    catch {
        Write-Host "   ? Error downloading ngrok: $_" -ForegroundColor Red
        Write-Host "   ?? Please download manually from:" -ForegroundColor Yellow
        Write-Host "   https://ngrok.com/download" -ForegroundColor Yellow
        Write-Host "   And place ngrok.exe in:" -ForegroundColor Yellow
        Write-Host "   C:\WPronto\tools\ngrok.exe" -ForegroundColor Yellow
        exit 1
    }
} else {
    $size = [math]::Round((Get-Item $ngrokPath).Length / 1MB, 2)
    Write-Host "   ? ngrok.exe found! Size: $size MB" -ForegroundColor Green
}

# Step 3: Clean data\mysql folder
Write-Host "`n?? Cleaning test databases..." -ForegroundColor Yellow
if (Test-Path "C:\WPronto\data\mysql") {
    Remove-Item "C:\WPronto\data\mysql\*_db" -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item "C:\WPronto\data\mysql\*.err" -Force -ErrorAction SilentlyContinue
    Remove-Item "C:\WPronto\data\mysql\*.pid" -Force -ErrorAction SilentlyContinue
    Write-Host "   ? Test databases cleaned" -ForegroundColor Green
}

# Prepare MySQL structure
Write-Host "   ?? Preparing MySQL structure..." -ForegroundColor Yellow
$coreMysqlData = "C:\WPronto\core\mysql\data"
if (-not (Test-Path $coreMysqlData)) {
    New-Item -ItemType Directory -Path $coreMysqlData -Force | Out-Null
    Write-Host "   ?? Created folder: core\mysql\data" -ForegroundColor Green
}

# Step 4: Navigate to project folder
Write-Host "`n?? Navigating to project folder..." -ForegroundColor Yellow
cd "C:\WPronto\src\WPLaunchGUI"
Write-Host "? Current folder: $(Get-Location)" -ForegroundColor Green

# Step 5: Restore packages
Write-Host "`n?? Restoring packages..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Failed to restore packages!" -ForegroundColor Red
    exit 1
}
Write-Host "? Packages restored" -ForegroundColor Green

# Step 6: Build Release version
Write-Host "`n?? Building project (Release)..." -ForegroundColor Yellow
dotnet build -c Release --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Build failed!" -ForegroundColor Red
    exit 1
}
Write-Host "? Build successful" -ForegroundColor Green

# Step 7: Publish
Write-Host "`n?? Publishing project..." -ForegroundColor Yellow
dotnet publish -c Release -o "C:\WPronto\publish" --no-build
if ($LASTEXITCODE -ne 0) {
    Write-Host "? Publish failed!" -ForegroundColor Red
    exit 1
}
Write-Host "? Publish successful" -ForegroundColor Green

# Step 8: Copy server folders
Write-Host "`n?? Copying server folders..." -ForegroundColor Yellow
$folders = @("core", "config", "data", "template", "www", "logs", "backups", "tmp", "tools")
foreach ($folder in $folders) {
    $sourcePath = "C:\WPronto\$folder"
    $destPath = "C:\WPronto\publish\$folder"
    if (Test-Path $sourcePath) {
        Copy-Item -Path $sourcePath -Destination $destPath -Recurse -Force
        Write-Host "   ? Copied: $folder"
    } else {
        New-Item -ItemType Directory -Path $destPath -Force | Out-Null
        Write-Host "   ?? Folder created (was missing): $folder" -ForegroundColor Yellow
    }
}

# Step 9: Copy files
Write-Host "`n?? Copying files..." -ForegroundColor Yellow
$filesToCopy = @("help.txt", "license.txt", "app.ico")
foreach ($file in $filesToCopy) {
    $sourcePath = "C:\WPronto\$file"
    $destPath = "C:\WPronto\publish\$file"
    if (Test-Path $sourcePath) {
        Copy-Item -Path $sourcePath -Destination $destPath -Force
        Write-Host "   ? Copied: $file"
    } else {
        Write-Host "   ?? File not found: $file" -ForegroundColor Yellow
    }
}

# Additionally copy app.ico from src if missing in root
if (-not (Test-Path "C:\WPronto\app.ico")) {
    $srcIcon = "C:\WPronto\src\WPLaunchGUI\app.ico"
    if (Test-Path $srcIcon) {
        Copy-Item -Path $srcIcon -Destination "C:\WPronto\publish\app.ico" -Force
        Write-Host "   ? Copied: app.ico (from src)" -ForegroundColor Green
    }
}

# Step 10: VERIFY AND ENSURE ngrok is in build
Write-Host "`n?? Verifying ngrok in build..." -ForegroundColor Yellow
$ngrokSourcePath = "C:\WPronto\tools\ngrok.exe"
$ngrokBuildPath = "C:\WPronto\publish\tools\ngrok.exe"

# Перевіряємо чи папка tools існує в publish
$toolsBuildPath = "C:\WPronto\publish\tools"
if (-not (Test-Path $toolsBuildPath)) {
    New-Item -ItemType Directory -Path $toolsBuildPath -Force | Out-Null
    Write-Host "   ?? Created tools folder in publish" -ForegroundColor Green
}

if (Test-Path $ngrokBuildPath) {
    $size = [math]::Round((Get-Item $ngrokBuildPath).Length / 1MB, 2)
    Write-Host "   ? ngrok.exe already in build! Size: $size MB" -ForegroundColor Green
} else {
    Write-Host "   ?? ngrok.exe NOT in build!" -ForegroundColor Yellow
    Write-Host "   ?? Copying from source..." -ForegroundColor Yellow
    
    if (Test-Path $ngrokSourcePath) {
        Copy-Item -Path $ngrokSourcePath -Destination $ngrokBuildPath -Force
        
        if (Test-Path $ngrokBuildPath) {
            $size = [math]::Round((Get-Item $ngrokBuildPath).Length / 1MB, 2)
            Write-Host "   ? Copied to build! Size: $size MB" -ForegroundColor Green
        } else {
            Write-Host "   ? Failed to copy ngrok.exe!" -ForegroundColor Red
        }
    } else {
        Write-Host "   ? Source ngrok.exe not found at: $ngrokSourcePath" -ForegroundColor Red
    }
}

# Створюємо порожній файл ngrok_token.config для програми
$ngrokTokenFile = "C:\WPronto\publish\ngrok_token.config"
if (-not (Test-Path $ngrokTokenFile)) {
    $tokenContent = "# Add your ngrok auth token here (optional, can be added via UI)" + "`n"
    $tokenContent += "# Get it from: https://dashboard.ngrok.com/auth" + "`n"
    $tokenContent += "YOUR_NGROK_AUTH_TOKEN_HERE"
    Set-Content -Path $ngrokTokenFile -Value $tokenContent -Encoding UTF8
    Write-Host "   ?? Created ngrok_token.config" -ForegroundColor Green
}

# Step 11: Fix my.ini (relative paths)
Write-Host "`n?? Fixing my.ini..." -ForegroundColor Yellow
$myIniPath = "C:\WPronto\publish\data\mysql\my.ini"
if (Test-Path $myIniPath) {
    $content = Get-Content $myIniPath -Raw
    $content = $content -replace 'C:/WPLaunch/data/mysql', './data/mysql'
    $content = $content -replace 'C:\\WPLaunch\\core\\mysql', './core/mysql'
    $content = $content -replace 'C:/WPronto/data/mysql', './data/mysql'
    $content = $content -replace 'C:\\WPronto\\core\\mysql', './core/mysql'
    Set-Content $myIniPath $content -NoNewline
    Write-Host "   ? my.ini fixed" -ForegroundColor Green
} else {
    Write-Host "   ?? my.ini not found, creating default..." -ForegroundColor Yellow
    $defaultMyIni = @"
[mysqld]
basedir=./core/mysql
datadir=./data/mysql
port=3306
bind-address=127.0.0.1
max_allowed_packet=64M
innodb_log_file_size=128M
"@
    $defaultMyIni | Set-Content -Path "C:\WPronto\publish\data\mysql\my.ini" -Encoding ASCII
    Write-Host "   ?? Created my.ini" -ForegroundColor Green
}

# Step 12: Create compatibility folder
Write-Host "`n?? Creating compatibility structure..." -ForegroundColor Yellow
$publishCoreMysqlData = "C:\WPronto\publish\core\mysql\data"
if (-not (Test-Path $publishCoreMysqlData)) {
    New-Item -ItemType Directory -Path $publishCoreMysqlData -Force | Out-Null
    Write-Host "   ?? Created compatibility folder: core\mysql\data" -ForegroundColor Green
}

# Step 13: Create nginx.conf with relative paths
Write-Host "`n?? Creating nginx.conf..." -ForegroundColor Yellow
$nginxConfPath = "C:\WPronto\publish\core\nginx\conf\nginx.conf"
if (Test-Path $nginxConfPath) {
    Write-Host "   ? nginx.conf already exists" -ForegroundColor Green
} else {
    Write-Host "   ?? Creating default nginx.conf..." -ForegroundColor Yellow
    $defaultNginxConf = @"
worker_processes  1;
events { worker_connections 1024; }

http {
    include       mime.types;
    default_type  application/octet-stream;
    sendfile      on;
    keepalive_timeout 65;
    client_max_body_size 256M;

    fastcgi_buffers 16 16k;
    fastcgi_buffer_size 32k;

    access_log   logs/nginx_access.log;
    error_log    logs/nginx_error.log;

    # Main site
    server {
        listen       80;
        server_name  localhost;
        root         www/default;
        index        index.php index.html;

        location / {
            try_files $uri $uri/ =404;
        }

        location ~ \.php$ {
            try_files $uri =404;
            fastcgi_pass 127.0.0.1:9000;
            fastcgi_param SCRIPT_FILENAME $document_root$fastcgi_script_name;
            include fastcgi_params;
            fastcgi_read_timeout 600;
        }
    }

    # phpMyAdmin on separate port
    server {
        listen       8080;
        server_name  localhost;
        root         core/phpmyadmin;
        index        index.php;

        location / {
            try_files $uri $uri/ =404;
        }

        location ~ \.php$ {
            try_files $uri =404;
            fastcgi_pass 127.0.0.1:9000;
            fastcgi_param SCRIPT_FILENAME $document_root$fastcgi_script_name;
            include fastcgi_params;
            fastcgi_read_timeout 600;
        }
    }

    include config/nginx/sites/*.conf;
}
"@
    $defaultNginxConf | Set-Content -Path $nginxConfPath -Encoding ASCII
    Write-Host "   ?? Created default nginx.conf" -ForegroundColor Green
}

# Step 14: Create installer
Write-Host "`n?? Creating installer..." -ForegroundColor Yellow

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
        Write-Host "   ?? Running Inno Setup Compiler..." -ForegroundColor Yellow
        & $iscc $issFile
        if ($LASTEXITCODE -eq 0) {
            Write-Host "? Installer created in C:\WPronto\Installer\" -ForegroundColor Green
        } else {
            Write-Host "? Installer creation failed (code: $LASTEXITCODE)" -ForegroundColor Red
        }
    } else {
        Write-Host "   ?? installer_final.iss not found" -ForegroundColor Yellow
        Write-Host "   ?? Expected path: C:\WPronto\installer_final.iss" -ForegroundColor Gray
    }
} else {
    Write-Host "? Inno Setup not found!" -ForegroundColor Red
    Write-Host "   ?? Download from: https://jrsoftware.org/isdl.php" -ForegroundColor Yellow
}

# Step 15: Show build information
Write-Host "`n========================================" -ForegroundColor Cyan
$publishSize = [math]::Round((Get-ChildItem "C:\WPronto\publish" -Recurse -ErrorAction SilentlyContinue | Measure-Object -Property Length -Sum).Sum / 1MB, 2)
$fileCount = (Get-ChildItem "C:\WPronto\publish" -Recurse -File -ErrorAction SilentlyContinue).Count

Write-Host "?? BUILD INFORMATION:" -ForegroundColor Cyan
Write-Host "   ?? Version: 5.0 (ngrok)" -ForegroundColor White
Write-Host "   ?? Size: $publishSize MB" -ForegroundColor White
Write-Host "   ?? Files: $fileCount" -ForegroundColor White
Write-Host "   ?? Location: C:\WPronto\publish" -ForegroundColor White

if (Test-Path "C:\WPronto\publish\WProntoGUI.exe") {
    $exeSize = [math]::Round((Get-Item "C:\WPronto\publish\WProntoGUI.exe").Length / 1KB, 2)
    Write-Host "   ? Main EXE file: $exeSize KB" -ForegroundColor Green
} else {
    Write-Host "   ? MAIN EXE FILE NOT FOUND!" -ForegroundColor Red
}

# Check for ngrok in the build
$ngrokCheck = "C:\WPronto\publish\tools\ngrok.exe"
if (Test-Path $ngrokCheck) {
    $ngrokSize = [math]::Round((Get-Item $ngrokCheck).Length / 1MB, 2)
    Write-Host "   ? ngrok: $ngrokSize MB (included in build)" -ForegroundColor Green
} else {
    Write-Host "   ?? ngrok: missing (will be downloaded automatically by the app)" -ForegroundColor Yellow
}

# Check for ngrok token config
$tokenCheck = "C:\WPronto\publish\ngrok_token.config"
if (Test-Path $tokenCheck) {
    Write-Host "   ? ngrok_token.config: present" -ForegroundColor Green
} else {
    Write-Host "   ?? ngrok_token.config: missing (will be created by app)" -ForegroundColor Yellow
}

Write-Host "========================================" -ForegroundColor Cyan

Write-Host "`n? Done! Build v5.0 (ngrok) successfully created!" -ForegroundColor Green
Write-Host ""
Write-Host "?? Next steps for ngrok:" -ForegroundColor Yellow
Write-Host "   1. Sign up at https://ngrok.com" -ForegroundColor White
Write-Host "   2. Get your auth token from https://dashboard.ngrok.com/auth" -ForegroundColor White
Write-Host "   3. Enter the token in WPronto when prompted (or add to ngrok_token.config)" -ForegroundColor White
Write-Host ""

# Optional: run the app after build
$runAfterBuild = Read-Host "Run the application? (y/n)"
if ($runAfterBuild -eq 'y') {
    Start-Process "C:\WPronto\publish\WProntoGUI.exe"
}