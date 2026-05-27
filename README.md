# 🚀 WPronto

**Local WordPress Development Environment for Windows**
WPronto is a lightweight, portable local WordPress server for Windows that allows you to create and manage multiple WordPress sites with a single click. Built for developers who need a fast, simple, and reliable local development environment.

---

## 🛠️ Technology Stack

### Core Technologies

| Component | Technology | Version | Purpose |
|-----------|------------|---------|---------|
| **Programming Language** | C# | 12.0 | Application logic and UI |
| **Framework** | .NET | 8.0 | Runtime and libraries |
| **UI Framework** | Windows Forms | .NET 8.0 | Graphical user interface |
| **Build Tool** | MSBuild / dotnet CLI | - | Compilation and publishing |

### Server Components

| Component | Technology | Version | Purpose |
|-----------|------------|---------|---------|
| **Web Server** | Nginx | 1.26.0 | HTTP server, request handling |
| **PHP Engine** | PHP (Non-Thread Safe) | 8.5.6 | WordPress execution |
| **Database Server** | MariaDB | 11.4.2 | MySQL-compatible database |
| **Database Manager** | phpMyAdmin | 5.2.3 | Web-based database administration |
| **WordPress CLI** | WP-CLI | 2.12.0 | Command-line WordPress management |

### Development Tools

| Tool | Version | Purpose |
|------|---------|---------|
| **IDE** | Visual Studio 2022 Community | Code editing, debugging, design |
| **Installer Creator** | Inno Setup 6 | Windows installer generation |
| **Version Control** | Git | Source code management |
| **Scripting** | PowerShell 5.1+ | Automation and testing |

### Third-Party Libraries

| Library | Purpose |
|---------|---------|
| **Microsoft.VisualBasic** | InputBox dialogs |
| **System.Drawing** | Graphics and icons |
| **System.Net.Sockets** | Port checking and diagnostics |
| **System.Text.Encoding** | UTF-8 without BOM support |

---

## Project map
```
WPronto/
│
├── 📁 core/                          # Server core (all components)
│   ├── 📁 nginx/                     # Nginx web server
│   │   ├── nginx.exe                 # Nginx executable
│   │   ├── 📁 conf/                  # Nginx configurations
│   │   │   ├── nginx.conf            # Main config (copied from config/)
│   │   │   ├── fastcgi_params        # FastCGI parameters
│   │   │   └── mime.types            # MIME types
│   │   └── 📁 html/                  # Default page (not used)
│   │
│   ├── 📁 php/                       # PHP 8.5.6
│   │   ├── php.exe                   # PHP CLI
│   │   ├── php-cgi.exe               # PHP FastCGI (for Nginx)
│   │   ├── 📁 ext/                   # PHP extensions
│   │   │   ├── php_curl.dll
│   │   │   ├── php_gd.dll
│   │   │   ├── php_mbstring.dll
│   │   │   ├── php_mysqli.dll
│   │   │   ├── php_openssl.dll
│   │   │   ├── php_pdo_mysql.dll
│   │   │   └── php_zip.dll
│   │   └── ... (other PHP files)
│   │
│   ├── 📁 mysql/                     # MariaDB 11.4.2
│   │   └── 📁 bin/
│   │       ├── mysqld.exe            # MySQL server
│   │       ├── mysql.exe             # MySQL client
│   │       └── mysqladmin.exe        # Admin utility
│   │
│   ├── 📁 wp-cli/                    # WP-CLI
│   │   └── wp-cli.phar               # WP-CLI (PHP archive)
│   │
│   └── 📁 phpmyadmin/                # phpMyAdmin 5.2.3
│       ├── index.php                 # Main file
│       ├── config.inc.php            # Configuration (created by app)
│       └── ... (other phpMyAdmin files)
│
├── 📁 config/                        # Configuration files
│   ├── 📁 nginx/
│   │   ├── nginx.conf                # Main Nginx config
│   │   ├── fastcgi_params            # FastCGI parameters
│   │   ├── mime.types                # MIME types
│   │   └── 📁 sites/                 # Individual site configs
│   │       ├── site1.conf            # Example site config
│   │       ├── site2.conf
│   │       └── ...
│   │
│   └── 📁 php/
│       └── php.ini                   # PHP configuration (created by app)
│
├── 📁 data/                          # Server data
│   └── 📁 mysql/                     # MySQL database files
│       ├── ibdata1                   # InnoDB system files
│       ├── mysql/                    # MySQL system DB
│       ├── performance_schema/       # System DB
│       ├── site1_db/                 # site1 database
│       ├── site2_db/                 # site2 database
│       └── ...
│
├── 📁 template/                      # WordPress template (copied to new sites)
│   ├── index.php                     # WordPress main file
│   ├── wp-admin/                     # WordPress admin
│   ├── wp-content/                   # Themes, plugins, uploads
│   ├── wp-includes/                  # WordPress core
│   ├── wp-config-sample.php          # Sample config
│   └── ... (other WordPress files)
│
├── 📁 www/                           # User websites
│   ├── 📁 default/                   # Default site (demo)
│   │   └── index.html                # Test page
│   │
│   ├── 📁 site1/                     # First WordPress site
│   │   ├── index.php
│   │   ├── wp-admin/
│   │   ├── wp-content/
│   │   ├── wp-includes/
│   │   ├── wp-config.php             # Database connection config
│   │   └── WP_CREDENTIALS.txt        # Saved passwords (if generated)
│   │
│   ├── 📁 site2/                     # Second WordPress site
│   │   └── ...
│   │
│   └── ...
│
├── 📁 logs/                          # Server logs
│   ├── nginx_access.log              # Nginx access log
│   ├── nginx_error.log               # Nginx error log
│   └── php_error.log                 # PHP error log
│
├── 📁 tmp/                           # Temporary files
│   └── ... (temp files from phpMyAdmin and other services)
│
├── 📁 publish/                       # Compiled application (after dotnet publish)
│   ├── WProntoGUI.exe                # Application executable
│   ├── WProntoGUI.dll                # Application library
│   ├── WProntoGUI.runtimeconfig.json
│   ├── WProntoGUI.deps.json
│   ├── app.ico                       # Application icon
│   └── ... (all files from root are copied here)
│
├── 📁 src/                           # Source code
│   └── 📁 WProntoGUI/               # Visual Studio project
│       ├── Form1.cs                  # Main form (application code)
│       ├── Form1.Designer.cs         # Form designer
│       ├── WProntoGUI.csproj        # Project file
│       └── 📁 bin/                   # Compiled files
│           └── 📁 Release/           # Release version
│               └── 📁 net8.0-windows/
│                   └── ...
│
├── 📁 Installer/                     # Installer (after Inno Setup compilation)
│   └── WPronto.exe            # Ready installer for client
│
├── about.txt                         # Program info (updated)
├── license.txt                       # MIT License
└── installer_final.iss               # Inno Setup script for creating installer

```
```
📋 Brief structure (main):
Folder          Purpose
core\           All server components (Nginx, PHP, MySQL, phpMyAdmin, WP-CLI)
config\         Configuration files (Nginx, PHP)
data\           MySQL databases
template\       WordPress template for new sites
www\            WordPress sites
logs\           Server logs
publish\        Ready application (.exe)
src\            Source code (Visual Studio project)
Installer\      Ready installer for client

📁 Folder sizes (approximate):
Folder               Size
core\mysql\          ~150-200 MB
core\php\            ~40-50 MB
core\nginx\          ~5-10 MB
core\wp-cli\         ~5-10 MB
core\phpmyadmin\     ~20-30 MB
template\            ~50-60 MB
www\                 ~50-100 MB (depends on number of sites)
data\                ~50-150 MB (depends on databases)
publish\             ~50-100 MB
Total                ~400-600 MB
```

## For developers
```bash
# Clone the repository
git clone https://github.com/ovcharovcoder/wpronto-src.git

# Navigate to project
cd WPLaunch/src/WProntoGUI

# Build the project
dotnet build -c Release

# Publish the application
dotnet publish -c Release -o publish
```

### Publish the application
```
dotnet publish -c Release -o publish
```

### Copy required folders
```
Copy-Item -Path "core" -Destination "publish" -Recurse
Copy-Item -Path "config" -Destination "publish" -Recurse
Copy-Item -Path "data" -Destination "publish" -Recurse
Copy-Item -Path "template" -Destination "publish" -Recurse
Copy-Item -Path "www" -Destination "publish" -Recurse
```

### Compile installer using Inno Setup
```
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer_final.iss
```

 ## License
This project is licensed under the MIT License – see the LICENSE file for details.

## 👤 Author

<img 
  src="https://raw.githubusercontent.com/ovcharovcoder/wp-password-generator/main/screenshots/avatar.png"
  alt="Andrii Ovcharov"
  width="60"
/>

**Andrii Ovcharov**<br>
📧 ovcharovcoder@gmail.com<br>
🔗 [LinkedIn](https://www.linkedin.com/in/andrii-ovcharov-101a24196/) | [GitHub](https://github.com/ovcharovcoder)

