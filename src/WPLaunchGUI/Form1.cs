#nullable disable
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Security.Cryptography;

namespace WPLaunchGUI
{
    public enum ButtonStyle { Default, Primary, Danger, Success, Warning }
    public enum AppTheme { Light, Dark, System }

    // =========================
    // THEME MANAGER
    // =========================
    public static class ThemeManager
    {
        public static AppTheme CurrentTheme { get; private set; } = AppTheme.Light;
        public static event Action<AppTheme> ThemeChanged;
        private static ColorScheme _currentScheme = new LightColorScheme();
        public static ColorScheme CurrentScheme => _currentScheme;

        public static void SetTheme(AppTheme theme)
        {
            CurrentTheme = theme;
            if (theme == AppTheme.System)
            {
                bool isDark = IsSystemDarkMode();
                _currentScheme = isDark ? new DarkColorScheme() : new LightColorScheme();
            }
            else if (theme == AppTheme.Dark)
                _currentScheme = new DarkColorScheme();
            else
                _currentScheme = new LightColorScheme();

            ThemeChanged?.Invoke(CurrentTheme);
            SaveThemeSetting(theme);
        }

        private static bool IsSystemDarkMode()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key?.GetValue("AppsUseLightTheme") is int value)
                        return value == 0;
                }
            }
            catch { }
            return false;
        }

        private static void SaveThemeSetting(AppTheme theme)
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "theme.config");
                string configDir = Path.GetDirectoryName(configPath);
                if (!Directory.Exists(configDir) && !string.IsNullOrEmpty(configDir))
                    Directory.CreateDirectory(configDir);
                File.WriteAllText(configPath, theme.ToString());
            }
            catch { }
        }

        public static AppTheme LoadThemeSetting()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "theme.config");
                if (File.Exists(configPath) && Enum.TryParse<AppTheme>(File.ReadAllText(configPath), out AppTheme theme))
                    return theme;
            }
            catch { }
            return AppTheme.Light;
        }
    }

    // =========================
    // COLOR SCHEMES
    // =========================
    public abstract class ColorScheme
    {
        public abstract Color BackgroundPrimary { get; }
        public abstract Color BackgroundSecondary { get; }
        public abstract Color BackgroundCard { get; }
        public abstract Color TextPrimary { get; }
        public abstract Color TextSecondary { get; }
        public abstract Color TextMuted { get; }
        public abstract Color BorderColor { get; }
        public abstract Color SuccessColor { get; }
        public abstract Color DangerColor { get; }
        public abstract Color PrimaryColor { get; }
        public abstract Color PrimaryHover { get; }
        public abstract Color SelectionBackground { get; }
        public abstract Color LogBackground { get; }
        public abstract Color StatusRunning { get; }
        public abstract Color StatusStopped { get; }
        public abstract Color WarningColor { get; }
    }

    public class LightColorScheme : ColorScheme
    {
        public override Color BackgroundPrimary => Color.FromArgb(243, 245, 248);
        public override Color BackgroundSecondary => Color.White;
        public override Color BackgroundCard => Color.White;
        public override Color TextPrimary => Color.FromArgb(30, 35, 45);
        public override Color TextSecondary => Color.FromArgb(45, 50, 60);
        public override Color TextMuted => Color.FromArgb(110, 120, 135);
        public override Color BorderColor => Color.FromArgb(205, 208, 215);
        public override Color SuccessColor => Color.FromArgb(34, 139, 34);
        public override Color DangerColor => Color.FromArgb(196, 43, 28);
        public override Color PrimaryColor => Color.FromArgb(0, 103, 192);
        public override Color PrimaryHover => Color.FromArgb(0, 93, 180);
        public override Color SelectionBackground => Color.FromArgb(240, 244, 250);
        public override Color LogBackground => Color.White;
        public override Color StatusRunning => Color.FromArgb(34, 197, 94);
        public override Color StatusStopped => Color.FromArgb(196, 43, 28);
        public override Color WarningColor => Color.FromArgb(204, 102, 0);
    }

    public class DarkColorScheme : ColorScheme
    {
        public override Color BackgroundPrimary => Color.FromArgb(32, 33, 36);
        public override Color BackgroundSecondary => Color.FromArgb(41, 42, 45);
        public override Color BackgroundCard => Color.FromArgb(41, 42, 45);
        public override Color TextPrimary => Color.FromArgb(232, 234, 237);
        public override Color TextSecondary => Color.FromArgb(189, 193, 198);
        public override Color TextMuted => Color.FromArgb(128, 131, 140);
        public override Color BorderColor => Color.FromArgb(55, 58, 64);
        public override Color SuccessColor => Color.FromArgb(46, 160, 67);
        public override Color DangerColor => Color.FromArgb(220, 53, 69);
        public override Color PrimaryColor => Color.FromArgb(26, 115, 232);
        public override Color PrimaryHover => Color.FromArgb(31, 125, 242);
        public override Color SelectionBackground => Color.FromArgb(60, 65, 70);
        public override Color LogBackground => Color.FromArgb(30, 30, 35);
        public override Color StatusRunning => Color.FromArgb(40, 167, 69);
        public override Color StatusStopped => Color.FromArgb(220, 53, 69);
        public override Color WarningColor => Color.FromArgb(230, 115, 0);
    }

    // =========================
    // BACKUP INFO CLASS
    // =========================
    public class BackupInfo
    {
        public string Path { get; set; }
        public string Timestamp { get; set; }
        public DateTime? BackupDate { get; set; }
        public bool HasFiles { get; set; }
        public bool HasDatabase { get; set; }
        public string Size { get; set; }

        public override string ToString()
        {
            string dateStr = BackupDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? Timestamp;
            string status = "";
            if (HasFiles && HasDatabase) status = "✓ Full backup";
            else if (HasFiles) status = "📁 Files only";
            else if (HasDatabase) status = "💾 DB only";
            else status = "⚠ Incomplete";

            return $"[{dateStr}] {status} ({Size})";
        }
    }

    // =========================
    // CONFIGURATION CLASS
    // =========================
    public static class Config
    {
        public const int DefaultPort = 80;
        public const int AlternativePort = 8080;
        public const int PhpPort = 9000;
        public const int MysqlPort = 3306;
        public const int UploadMaxSize = 256;
        public const int MemoryLimit = 512;
        public const string TimeZone = "Europe/Kyiv";
        public const int ProcessStartDelay = 2000;
        public const int NginxStartDelay = 3000;
        public const int PhpMyAdminPort = 8080;
    }

    public partial class Form1 : Form
    {
        // LOGIC VARIABLES
        private System.Windows.Forms.Timer _statusTimer;
        private string _basePath = string.Empty;
        private string _nginxPath = string.Empty;
        private string _nginxConf = string.Empty;
        private string _nginxWorkingDir = string.Empty;
        private string _nginxConfDir = string.Empty;
        private string _phpCgiPath = string.Empty;
        private string _phpIni = string.Empty;
        private string _phpWorkingDir = string.Empty;
        private string _mysqlPath = string.Empty;
        private string _mysqlClientPath = string.Empty;
        private string _mysqlData = string.Empty;
        private string _mysqlWorkingDir = string.Empty;
        private string _wwwPath = string.Empty;
        private string _sitesPath = string.Empty;
        private string _templatePath = string.Empty;
        private string _logsPath = string.Empty;
        private string _tmpPath = string.Empty;
        private string _pmaPath = string.Empty;
        private string _backupPath = string.Empty;
        private string _mysqlConfPath = string.Empty;

        private int _webPort = Config.DefaultPort;

        // PHP version
        private string _currentPhpVersion = "8.3";

        // UI ELEMENTS
        private ListBox listSites;
        private RichTextBox txtLog;
        private SoftButton btnStart;
        private SoftButton btnRestart;
        private SoftButton btnStop;
        private SoftButton btnPhpMyAdmin;
        private SoftButton btnCreateSite;
        private SoftButton btnBackupSite;
        private SoftButton btnRestoreBackup;
        private SoftButton btnDeleteSite;
        private SoftButton btnHelp;
        private Label lblStatus;
        private Label lblTitle;
        private Label lblSubtitle;
        private LinkLabel lnkWebsite;
        private Label btnTheme;
        private ComboBox comboPhpVersion;
        private Label lblPhpStatus;
        private ToolTip toolTip;

        private float _dpiScale = 1.0f;
        private int ScaleInt(int value) => (int)(value * _dpiScale);

        private const int MAX_LOG_LINES = 1000;
        private const int STATUS_CHECK_INTERVAL_MS = 10000;
        private int _logTrimCounter = 0;
        private const int LOG_TRIM_THRESHOLD = 100;

        // =========================
        // CONSTRUCTOR
        // =========================
        public Form1()
        {
            try
            {
                using (Graphics g = this.CreateGraphics())
                {
                    _dpiScale = g.DpiX / 96f;
                    if (_dpiScale > 1.15f) _dpiScale = 1.15f;
                }

                this.Text = "WPronto — Local WP Environment";
                this.Size = new Size(ScaleInt(840), ScaleInt(580));
                this.MinimumSize = new Size(ScaleInt(840), ScaleInt(580));
                this.MaximumSize = new Size(ScaleInt(840), ScaleInt(580));
                this.FormBorderStyle = FormBorderStyle.FixedSingle;
                this.MaximizeBox = false;
                this.StartPosition = FormStartPosition.CenterScreen;
                this.FormClosing += Form1_FormClosing;

                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(iconPath))
                    this.Icon = new Icon(iconPath);

                _currentPhpVersion = LoadPhpVersionSetting();

                InitializePaths();
                EnsureDirectories();

                FindAvailablePorts();
                EnsureConfigFiles();

                CreateProfessionalLayout();

                var savedTheme = ThemeManager.LoadThemeSetting();
                ThemeManager.SetTheme(savedTheme);
                ThemeManager.ThemeChanged += (theme) => ApplyTheme();
                ApplyTheme();

                LoadSites();
                CheckServerStatus();
                ValidateTemplate();
                CheckAdminRights();
                CleanupTempFiles();

                _statusTimer = new System.Windows.Forms.Timer();
                _statusTimer.Interval = STATUS_CHECK_INTERVAL_MS;
                _statusTimer.Tick += (s, e) => CheckServerStatus();
                _statusTimer.Start();
            }
            catch (Exception ex)
            {
                LogError(ex, "Form1_Constructor");
                MessageBox.Show($"Error initializing application: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================
        // FORM CLOSING HANDLER
        // =========================
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                if (IsProcessRunning("nginx") || IsProcessRunning("php-cgi") || IsProcessRunning("mysqld"))
                {
                    var result = MessageBox.Show(
                        "Сервер все ще працює.\n\nЗупинити сервіси перед закриттям програми?",
                        "WPronto", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                        BtnStop_Click(null, null);
                    else if (result == DialogResult.Cancel)
                        e.Cancel = true;
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "Form1_FormClosing");
            }
        }

        // =========================
        // ERROR LOGGING
        // =========================
        private void LogError(Exception ex, string context)
        {
            try
            {
                Log($"❌ ERROR in {context}: {ex.Message}");
                LogToFile($"ERROR [{context}]: {ex}");
            }
            catch { }
        }

        // =========================
        // PORT CHECK WITH RETRY
        // =========================
        private async Task<bool> WaitForPortsAvailable(int[] ports, int timeoutMs = 5000)
        {
            try
            {
                int elapsed = 0;
                while (elapsed < timeoutMs)
                {
                    bool allAvailable = true;
                    foreach (int port in ports)
                        if (!IsPortAvailable(port)) { allAvailable = false; break; }
                    if (allAvailable) return true;
                    await Task.Delay(200);
                    elapsed += 200;
                }
                return false;
            }
            catch (Exception ex)
            {
                LogError(ex, "WaitForPortsAvailable");
                return false;
            }
        }

        // =========================
        // GRACEFUL PROCESS STOP
        // =========================
        private void StopProcessGracefully(string name, int timeoutMs = 3000)
        {
            try
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    try
                    {
                        if (!p.HasExited)
                        {
                            p.CloseMainWindow();
                            if (!p.WaitForExit(timeoutMs))
                                p.Kill();
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, $"StopProcessGracefully - {name}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, $"StopProcessGracefully - {name} (outer)");
            }
        }

        // =========================
        // PATH HELPERS
        // =========================
        private string GetCurrentPhpFolder() => $"php{_currentPhpVersion.Replace(".", "")}";

        private string GetCorrectBasePath()
        {
            try
            {
                string exePath = Application.ExecutablePath;
                string exeDir = Path.GetDirectoryName(exePath);

                if (Directory.Exists(Path.Combine(exeDir, "core", "nginx")))
                    return exeDir;

                string currentDir = Environment.CurrentDirectory;
                if (Directory.Exists(Path.Combine(currentDir, "core", "nginx")))
                    return currentDir;

                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                if (Directory.Exists(Path.Combine(baseDir, "core", "nginx")))
                    return baseDir;

                string parentDir = Path.GetDirectoryName(exeDir);
                if (!string.IsNullOrEmpty(parentDir) && Directory.Exists(Path.Combine(parentDir, "core", "nginx")))
                    return parentDir;

                return currentDir;
            }
            catch (Exception ex)
            {
                LogError(ex, "GetCorrectBasePath");
                return Path.GetDirectoryName(Application.ExecutablePath);
            }
        }

        // =========================
        // CREATE PHP.INI IN PHP FOLDER
        // =========================
        private void CreatePhpIni()
        {
            try
            {
                string phpVersionFolder = GetCurrentPhpFolder();
                string phpIniPath = Path.Combine(_basePath, @"core\php", phpVersionFolder, "php.ini");
                string basePathUnix = _basePath.Replace("\\", "/");

                string phpIniContent = $@"[PHP]
extension_dir = ""ext""
date.timezone = ""{Config.TimeZone}""
display_errors = On
display_startup_errors = On
error_reporting = E_ALL
log_errors = On
error_log = ""{basePathUnix}/logs/php_error_{_currentPhpVersion}.log""

upload_max_filesize = {Config.UploadMaxSize}M
post_max_size = {Config.UploadMaxSize}M
memory_limit = {Config.MemoryLimit}M
max_execution_time = 600
max_input_time = 600
max_input_vars = 5000

extension=curl
extension=gd
extension=mbstring
extension=mysqli
extension=openssl
extension=pdo_mysql
extension=zip

cgi.fix_pathinfo=1
fastcgi.logging = 0
";

                string phpDir = Path.GetDirectoryName(phpIniPath);
                if (!Directory.Exists(phpDir))
                    Directory.CreateDirectory(phpDir);

                File.WriteAllText(phpIniPath, phpIniContent, Utf8NoBom);
                LogToFile($"Created php.ini at: {phpIniPath}");
            }
            catch (Exception ex)
            {
                LogError(ex, "CreatePhpIni");
            }
        }

        private void EnsurePhpIni()
        {
            if (!File.Exists(_phpIni))
                CreatePhpIni();
        }

        // =========================
        // INITIALIZE PATHS
        // =========================
        private void InitializePaths()
        {
            try
            {
                _basePath = GetCorrectBasePath();

                if (_basePath == "C:\\" || _basePath == "C:" || string.IsNullOrWhiteSpace(_basePath))
                    _basePath = Path.GetDirectoryName(Application.ExecutablePath);

                if (!Directory.Exists(_basePath))
                    _basePath = AppDomain.CurrentDomain.BaseDirectory;

                string phpVersionFolder = GetCurrentPhpFolder();

                _nginxPath = Path.Combine(_basePath, @"core\nginx\nginx.exe");
                _nginxWorkingDir = Path.Combine(_basePath, @"core\nginx");
                _nginxConfDir = Path.Combine(_basePath, @"core\nginx\conf");
                _nginxConf = Path.Combine(_nginxConfDir, "nginx.conf");

                _phpCgiPath = Path.Combine(_basePath, @"core\php", phpVersionFolder, "php-cgi.exe");
                _phpWorkingDir = Path.Combine(_basePath, @"core\php", phpVersionFolder);

                _phpIni = Path.Combine(_phpWorkingDir, "php.ini");

                _mysqlPath = Path.Combine(_basePath, @"core\mysql\bin\mysqld.exe");
                _mysqlClientPath = Path.Combine(_basePath, @"core\mysql\bin\mysql.exe");
                _mysqlData = Path.Combine(_basePath, @"data\mysql");
                _mysqlWorkingDir = Path.Combine(_basePath, @"core\mysql\bin");
                _mysqlConfPath = Path.Combine(_basePath, @"config\mysql\my.ini");

                _wwwPath = Path.Combine(_basePath, @"www");
                _sitesPath = Path.Combine(_basePath, @"config\nginx\sites");
                _templatePath = Path.Combine(_basePath, @"template");
                _logsPath = Path.Combine(_basePath, @"logs");
                _tmpPath = Path.Combine(_basePath, @"tmp");
                _pmaPath = Path.Combine(_basePath, @"core\phpmyadmin");
                _backupPath = Path.Combine(_basePath, @"backups");

                EnsurePhpIni();

                try
                {
                    if (Directory.Exists(_logsPath))
                        LogToFile($"Base path initialized to: {_basePath}");
                }
                catch { }
            }
            catch (Exception ex)
            {
                LogError(ex, "InitializePaths");
            }
        }

        // =========================
        // DIRECTORY SETUP
        // =========================
        private void EnsureDirectories()
        {
            try
            {
                Directory.CreateDirectory(_wwwPath);
                Directory.CreateDirectory(Path.Combine(_wwwPath, "default"));
                Directory.CreateDirectory(_sitesPath);
                Directory.CreateDirectory(_logsPath);
                Directory.CreateDirectory(_tmpPath);
                Directory.CreateDirectory(_nginxConfDir);
                Directory.CreateDirectory(Path.Combine(_basePath, @"config\php"));
                Directory.CreateDirectory(Path.Combine(_basePath, @"config\nginx"));
                Directory.CreateDirectory(Path.Combine(_basePath, @"config\mysql"));
                Directory.CreateDirectory(_mysqlData);
                Directory.CreateDirectory(_backupPath);

                string coreMysqlData = Path.Combine(_basePath, @"core\mysql\data");
                if (!Directory.Exists(coreMysqlData))
                {
                    Directory.CreateDirectory(coreMysqlData);
                    LogToFile("Created folder: core\\mysql\\data");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "EnsureDirectories");
            }
        }

        // =========================
        // PHP CONFIG
        // =========================
        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        // =========================
        // NGINX CONFIG
        // =========================
        private void EnsureConfigFiles()
        {
            try
            {
                if (File.Exists(_nginxConf))
                {
                    string oldContent = File.ReadAllText(_nginxConf);
                    if (oldContent.Contains("C:/WPronto") || oldContent.Contains("C:/WPLaunch") || oldContent.Contains("D:/WPronto"))
                    {
                        File.Delete(_nginxConf);
                        LogToFile("Old nginx.conf with absolute paths removed, creating fresh one...");
                    }
                }

                EnsurePhpIni();

                string mimePath = Path.Combine(_nginxConfDir, "mime.types");
                if (!File.Exists(mimePath))
                {
                    File.WriteAllText(mimePath, "types {\n    text/html html htm;\n    text/css css;\n    text/xml xml;\n" +
                        "    image/gif gif;\n    image/jpeg jpeg jpg;\n    application/javascript js;\n" +
                        "    text/plain txt;\n    image/png png;\n    image/svg+xml svg;\n    image/x-icon ico;\n" +
                        "    application/zip zip;\n}\n", Utf8NoBom);
                }

                string fastcgiPath = Path.Combine(_nginxConfDir, "fastcgi_params");
                if (!File.Exists(fastcgiPath))
                {
                    File.WriteAllText(fastcgiPath,
                        "fastcgi_param  QUERY_STRING       $query_string;\n" +
                        "fastcgi_param  REQUEST_METHOD     $request_method;\n" +
                        "fastcgi_param  CONTENT_TYPE       $content_type;\n" +
                        "fastcgi_param  CONTENT_LENGTH     $content_length;\n" +
                        "fastcgi_param  SCRIPT_NAME        $fastcgi_script_name;\n" +
                        "fastcgi_param  REQUEST_URI        $request_uri;\n" +
                        "fastcgi_param  DOCUMENT_URI       $document_uri;\n" +
                        "fastcgi_param  DOCUMENT_ROOT      $document_root;\n" +
                        "fastcgi_param  SERVER_PROTOCOL    $server_protocol;\n" +
                        "fastcgi_param  GATEWAY_INTERFACE  CGI/1.1;\n" +
                        "fastcgi_param  SERVER_SOFTWARE    nginx/$nginx_version;\n" +
                        "fastcgi_param  REMOTE_ADDR        $remote_addr;\n" +
                        "fastcgi_param  REMOTE_PORT        $remote_port;\n" +
                        "fastcgi_param  SERVER_ADDR        $server_addr;\n" +
                        "fastcgi_param  SERVER_PORT        $server_port;\n" +
                        "fastcgi_param  SERVER_NAME        $server_name;\n" +
                        "fastcgi_param  REDIRECT_STATUS    200;\n", Utf8NoBom);
                }

                string basePathUnix = _basePath.Replace("\\", "/");

                string nginxConfig =
        "worker_processes  1;\n" +
        "events { worker_connections 1024; }\n\n" +
        "http {\n" +
        "    include       mime.types;\n" +
        "    default_type  application/octet-stream;\n" +
        "    sendfile      on;\n" +
        "    keepalive_timeout 65;\n" +
        $"    client_max_body_size {Config.UploadMaxSize}M;\n\n" +
        "    fastcgi_buffers 16 16k;\n" +
        "    fastcgi_buffer_size 32k;\n" +
        "    proxy_buffer_size   128k;\n" +
        "    proxy_buffers     4 256k;\n" +
        "    proxy_busy_buffers_size 256k;\n\n" +
        $"    access_log   \"{basePathUnix}/logs/nginx_access.log\";\n" +
        $"    error_log    \"{basePathUnix}/logs/nginx_error.log\";\n\n" +
        "    # Main site\n" +
        "    server {\n" +
        $"        listen       {_webPort};\n" +
        "        server_name  localhost;\n" +
        $"        root         \"{basePathUnix}/www/default\";\n" +
        "        index        index.php index.html;\n\n" +
        "        location / {\n" +
        "            try_files $uri $uri/ =404;\n" +
        "        }\n\n" +
        "        location ~ \\.php$ {\n" +
        "            try_files $uri =404;\n" +
        "            fastcgi_pass 127.0.0.1:9000;\n" +
        "            fastcgi_param SCRIPT_FILENAME $document_root$fastcgi_script_name;\n" +
        "            include fastcgi_params;\n" +
        "            fastcgi_read_timeout 600;\n" +
        "            fastcgi_send_timeout 600;\n" +
        "        }\n" +
        "    }\n\n" +
        "    # phpMyAdmin on separate port\n" +
        "    server {\n" +
        $"        listen       {Config.PhpMyAdminPort};\n" +
        "        server_name  localhost;\n" +
        $"        root         \"{basePathUnix}/core/phpmyadmin\";\n" +
        "        index        index.php;\n\n" +
        "        location / {\n" +
        "            try_files $uri $uri/ =404;\n" +
        "        }\n\n" +
        "        location ~ \\.php$ {\n" +
        "            try_files $uri =404;\n" +
        "            fastcgi_pass 127.0.0.1:9000;\n" +
        "            fastcgi_param SCRIPT_FILENAME $document_root$fastcgi_script_name;\n" +
        "            include fastcgi_params;\n" +
        "            fastcgi_read_timeout 600;\n" +
        "        }\n" +
        "    }\n\n" +
        $"    include \"{basePathUnix}/config/nginx/sites/*.conf\";\n" +
        "}\n";

                File.WriteAllText(_nginxConf, nginxConfig, Encoding.ASCII);
                LogToFile($"nginx.conf created at: {_nginxConf}");

                // Always rewrite my.ini to use current paths
                string mysqlDataUnix = _mysqlData.Replace("\\", "/");
                string myIniContent = $@"[mysqld]
port={Config.MysqlPort}
basedir=""{basePathUnix}/core/mysql""
datadir=""{mysqlDataUnix}""
max_allowed_packet=64M
innodb_log_file_size=128M
innodb_flush_log_at_trx_commit=2
sync_binlog=0
";
                File.WriteAllText(_mysqlConfPath, myIniContent, Encoding.ASCII);
                LogToFile($"Updated my.ini at: {_mysqlConfPath}");

                string userIniContent = $"upload_max_filesize = {Config.UploadMaxSize}M\npost_max_size = {Config.UploadMaxSize}M\nmemory_limit = {Config.MemoryLimit}M\nmax_execution_time = 600\nmax_input_time = 600";

                string defaultUserIni = Path.Combine(_wwwPath, "default", ".user.ini");
                if (!File.Exists(defaultUserIni))
                {
                    if (!Directory.Exists(Path.Combine(_wwwPath, "default")))
                        Directory.CreateDirectory(Path.Combine(_wwwPath, "default"));
                    File.WriteAllText(defaultUserIni, userIniContent, Encoding.ASCII);
                }

                if (!Directory.Exists(_templatePath))
                    Directory.CreateDirectory(_templatePath);
                string templateUserIni = Path.Combine(_templatePath, ".user.ini");
                if (!File.Exists(templateUserIni))
                    File.WriteAllText(templateUserIni, userIniContent, Encoding.ASCII);

                if (!Directory.Exists(_pmaPath))
                    Directory.CreateDirectory(_pmaPath);

                string testIndex = Path.Combine(_pmaPath, "index.php");
                if (!File.Exists(testIndex))
                {
                    string phpContent = "<?php echo '<h1>phpMyAdmin</h1><p>Please download phpMyAdmin from <a href=\"https://www.phpmyadmin.net/\" target=\"_blank\">www.phpmyadmin.net</a></p><p>Extract files to: " + _pmaPath + "</p>'; ?>";
                    File.WriteAllText(testIndex, phpContent, Encoding.UTF8);
                }

                string infoFile = Path.Combine(_wwwPath, "default", "info.php");
                if (!File.Exists(infoFile))
                {
                    string phpInfo = "<?php\n" +
                        "echo '<h2>PHP Settings</h2>';\n" +
                        "echo '<strong>upload_max_filesize:</strong> ' . ini_get('upload_max_filesize') . '<br>';\n" +
                        "echo '<strong>post_max_size:</strong> ' . ini_get('post_max_size') . '<br>';\n" +
                        "echo '<strong>memory_limit:</strong> ' . ini_get('memory_limit') . '<br>';\n" +
                        "echo '<strong>max_execution_time:</strong> ' . ini_get('max_execution_time') . ' seconds<br>';\n" +
                        "echo '<hr>';\n" +
                        "echo '<h2>WordPress Upload Limits</h2>';\n" +
                        "echo '<p>You can upload files up to <strong style=\"color:green\">' . ini_get('upload_max_filesize') . '</strong></p>';\n" +
                        "?>";
                    File.WriteAllText(infoFile, phpInfo, Encoding.UTF8);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "EnsureConfigFiles");
            }
        }

        // =========================
        // THEME APPLICATION
        // =========================
        private void ApplyTheme()
        {
            try
            {
                var scheme = ThemeManager.CurrentScheme;
                this.BackColor = scheme.BackgroundPrimary;

                if (lblTitle != null) lblTitle.ForeColor = scheme.TextPrimary;
                if (lblSubtitle != null) lblSubtitle.ForeColor = scheme.TextMuted;

                if (listSites != null)
                {
                    listSites.BackColor = scheme.BackgroundCard;
                    listSites.ForeColor = scheme.TextSecondary;
                    listSites.Invalidate();
                }

                if (txtLog != null)
                {
                    txtLog.BackColor = scheme.LogBackground;
                    txtLog.ForeColor = scheme.TextSecondary;
                }

                bool running = IsProcessRunning("nginx") && IsProcessRunning("php-cgi") && IsProcessRunning("mysqld");
                if (lblStatus != null)
                    lblStatus.ForeColor = running ? scheme.StatusRunning : scheme.StatusStopped;

                if (btnTheme != null)
                {
                    string themeIcon = ThemeManager.CurrentTheme == AppTheme.Light ? "☀️" : (ThemeManager.CurrentTheme == AppTheme.Dark ? "🌙" : "🌓");
                    btnTheme.Text = themeIcon;
                    btnTheme.ForeColor = scheme.TextPrimary;
                }

                if (comboPhpVersion != null)
                {
                    comboPhpVersion.BackColor = scheme.BackgroundCard;
                    comboPhpVersion.ForeColor = scheme.TextSecondary;
                }
                if (lblPhpStatus != null) lblPhpStatus.ForeColor = scheme.TextMuted;

                foreach (Control control in this.Controls)
                {
                    if (control is Label label && label != lblTitle && label != lblSubtitle && label != lblStatus && label != btnTheme && label != lblPhpStatus)
                    {
                        if (!(label is LinkLabel))
                            label.ForeColor = scheme.TextMuted;
                    }
                    control.Invalidate();
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "ApplyTheme");
            }
        }

        // =========================
        // UI LAYOUT
        // =========================
        private void CreateProfessionalLayout()
        {
            var scheme = ThemeManager.CurrentScheme;

            lblTitle = new Label
            {
                Text = "WPronto v4.0",
                Font = new Font("Segoe UI Semibold", 22f, FontStyle.Bold),
                ForeColor = scheme.TextPrimary,
                Location = new Point(ScaleInt(32), ScaleInt(25)),
                AutoSize = true
            };

            lblSubtitle = new Label
            {
                Text = "Local WordPress Development Environment",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = scheme.TextMuted,
                Location = new Point(ScaleInt(35), ScaleInt(70)),
                AutoSize = true
            };

            lblStatus = new Label
            {
                Text = "● SERVER STOPPED",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = scheme.StatusStopped,
                Location = new Point(ScaleInt(540), ScaleInt(32)),
                Size = new Size(ScaleInt(220), ScaleInt(30)),
                TextAlign = ContentAlignment.MiddleRight
            };

            btnTheme = new Label
            {
                Text = ThemeManager.CurrentTheme == AppTheme.Light ? "☀️" : (ThemeManager.CurrentTheme == AppTheme.Dark ? "🌙" : "🌓"),
                Size = new Size(ScaleInt(40), ScaleInt(32)),
                Location = new Point(ScaleInt(770), ScaleInt(28)),
                Font = new Font("Segoe UI", 14f),
                Cursor = Cursors.Hand,
                ForeColor = scheme.TextPrimary,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnTheme.Click += BtnTheme_Click;

            Label lblPhpVersion = new Label
            {
                Text = "PHP:",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = scheme.TextMuted,
                Location = new Point(ScaleInt(32), ScaleInt(115)),
                AutoSize = true
            };

            comboPhpVersion = new ComboBox
            {
                Location = new Point(ScaleInt(70), ScaleInt(110)),
                Size = new Size(ScaleInt(70), ScaleInt(25)),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f),
                BackColor = scheme.BackgroundCard,
                ForeColor = scheme.TextSecondary
            };

            comboPhpVersion.Items.Clear();
            comboPhpVersion.Items.Add("8.3");
            comboPhpVersion.Items.Add("8.5");

            if (comboPhpVersion.Items.Contains(_currentPhpVersion))
                comboPhpVersion.SelectedItem = _currentPhpVersion;
            else
                comboPhpVersion.SelectedIndex = 0;

            lblPhpStatus = new Label
            {
                Text = $"Active: {_currentPhpVersion}",
                Location = new Point(ScaleInt(145), ScaleInt(114)),
                Size = new Size(ScaleInt(100), ScaleInt(20)),
                Font = new Font("Segoe UI", 7f),
                ForeColor = scheme.TextMuted
            };

            comboPhpVersion.SelectedIndexChanged += async (s, e) =>
            {
                try
                {
                    if (comboPhpVersion.SelectedItem != null)
                    {
                        string newVersion = comboPhpVersion.SelectedItem.ToString();
                        if (newVersion != _currentPhpVersion)
                        {
                            bool wasRunning = IsProcessRunning("nginx") && IsProcessRunning("php-cgi");

                            if (wasRunning)
                            {
                                DialogResult result = MessageBox.Show(
                                    $"Switch PHP from {_currentPhpVersion} to {newVersion}?\n\nServer will restart.",
                                    "Change PHP Version",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Question);

                                if (result == DialogResult.Yes)
                                {
                                    Log($"Switching PHP from {_currentPhpVersion} to {newVersion}...");
                                    BtnStop_Click(null, null);

                                    _currentPhpVersion = newVersion;
                                    SavePhpVersionSetting(newVersion);
                                    InitializePaths();

                                    EnsurePhpIni();

                                    await DelayAsync(1000);
                                    BtnStart_Click(null, null);

                                    lblPhpStatus.Text = $"Active: {newVersion}";
                                    Log($"PHP switched to {newVersion}");
                                }
                                else
                                {
                                    comboPhpVersion.SelectedItem = _currentPhpVersion;
                                }
                            }
                            else
                            {
                                _currentPhpVersion = newVersion;
                                SavePhpVersionSetting(newVersion);
                                InitializePaths();

                                EnsurePhpIni();

                                lblPhpStatus.Text = $"Active: {newVersion} (next start)";
                                Log($"PHP version set to {newVersion} (will be used on next start)");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, "comboPhpVersion_SelectedIndexChanged");
                }
            };

            FlowLayoutPanel topButtonsPanel = new FlowLayoutPanel
            {
                Location = new Point(ScaleInt(317), ScaleInt(105)),
                Size = new Size(ScaleInt(540), ScaleInt(50)),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            btnStart = new SoftButton("Start", ButtonStyle.Primary, _dpiScale);
            btnStart.Margin = new Padding(ScaleInt(2), 0, ScaleInt(18), 0);
            btnStart.Click += BtnStart_Click;

            btnRestart = new SoftButton("Restart", ButtonStyle.Warning, _dpiScale);
            btnRestart.Margin = new Padding(ScaleInt(2), 0, ScaleInt(18), 0);
            btnRestart.Click += BtnRestart_Click;

            btnStop = new SoftButton("Stop", ButtonStyle.Danger, _dpiScale);
            btnStop.Margin = new Padding(ScaleInt(2), 0, ScaleInt(18), 0);
            btnStop.Click += BtnStop_Click;

            btnPhpMyAdmin = new SoftButton("phpMyAdmin", ButtonStyle.Default, _dpiScale);
            btnPhpMyAdmin.Margin = new Padding(ScaleInt(2), 0, 0, 0);
            btnPhpMyAdmin.Click += BtnPhpMyAdmin_Click;

            topButtonsPanel.Controls.AddRange(new Control[] { btnStart, btnRestart, btnStop, btnPhpMyAdmin });

            Label lblSitesTitle = new Label
            {
                Text = "SITES",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = scheme.TextMuted,
                Location = new Point(ScaleInt(32), ScaleInt(155)),
                AutoSize = true
            };

            ModernCard cardSites = new ModernCard(ScaleInt(32), ScaleInt(175), ScaleInt(260), ScaleInt(285), _dpiScale);
            listSites = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ItemHeight = ScaleInt(32),
                DrawMode = DrawMode.OwnerDrawFixed,
                BackColor = scheme.BackgroundCard,
                Cursor = Cursors.Hand
            };
            listSites.DrawItem += ListSites_DrawItem;
            listSites.SelectedIndexChanged += ListSites_SelectedIndexChanged;

            listSites.DoubleClick += (s, e) =>
            {
                if (listSites.SelectedItem != null)
                {
                    string siteName = listSites.SelectedItem.ToString();

                    if (!IsProcessRunning("nginx") || !IsProcessRunning("php-cgi"))
                    {
                        MessageBox.Show(
                            "Server is not running!\n\nPlease start the server first.",
                            "WPronto",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    bool isPhpOnly = IsPhpOnlySite(siteName);
                    string url = isPhpOnly
                        ? $"http://{siteName}.wp:{_webPort}/"
                        : $"http://{siteName}.wp:{_webPort}/wp-admin";

                    try
                    {
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                        Log($"Opened via double-click: {url}");
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "DoubleClick_OpenAdmin");
                        MessageBox.Show($"Failed to open browser: {ex.Message}",
                            "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            ContextMenuStrip contextMenu = new ContextMenuStrip();

            ToolStripMenuItem openAdminItem = new ToolStripMenuItem("🌐 Open Admin");
            openAdminItem.Click += (s, e) =>
            {
                if (listSites.SelectedItem != null)
                {
                    string siteName = listSites.SelectedItem.ToString();
                    bool isPhpOnly = IsPhpOnlySite(siteName);
                    string url = isPhpOnly
                        ? $"http://{siteName}.wp:{_webPort}/"
                        : $"http://{siteName}.wp:{_webPort}/wp-admin";
                    try
                    {
                        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                        Log($"Opened: {url}");
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "ContextMenu_OpenAdmin");
                    }
                }
            };
            contextMenu.Items.Add(openAdminItem);

            ToolStripMenuItem openProjectItem = new ToolStripMenuItem("📁 Open Project");
            openProjectItem.Click += (s, e) =>
            {
                if (listSites.SelectedItem != null)
                {
                    string siteName = listSites.SelectedItem.ToString();
                    string sitePath = Path.Combine(_wwwPath, siteName);
                    if (Directory.Exists(sitePath))
                    {
                        try
                        {
                            Process.Start("explorer.exe", sitePath);
                            Log($"Opened project folder: {sitePath}");
                        }
                        catch (Exception ex)
                        {
                            LogError(ex, "OpenProject");
                            MessageBox.Show($"Failed to open folder: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"Site folder not found: {sitePath}", "WPronto",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Select a site first!", "WPronto",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            contextMenu.Items.Add(openProjectItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem backupItem = new ToolStripMenuItem("💾 Backup");
            backupItem.Click += (s, e) => BtnBackupSite_Click(s, e);
            contextMenu.Items.Add(backupItem);

            ToolStripMenuItem restoreItem = new ToolStripMenuItem("↩️ Restore");
            restoreItem.Click += (s, e) => BtnRestoreBackup_Click(s, e);
            contextMenu.Items.Add(restoreItem);

            ToolStripMenuItem openBackupItem = new ToolStripMenuItem("📁 Open Backup");
            openBackupItem.Click += (s, e) =>
            {
                if (listSites.SelectedItem != null)
                {
                    string siteName = listSites.SelectedItem.ToString();
                    string backupPath = Path.Combine(_backupPath, siteName);

                    if (Directory.Exists(backupPath))
                    {
                        try
                        {
                            Process.Start("explorer.exe", backupPath);
                            Log($"Opened backup folder: {backupPath}");
                        }
                        catch (Exception ex)
                        {
                            LogError(ex, "OpenBackup");
                            MessageBox.Show($"Failed to open backup folder: {ex.Message}",
                                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show($"No backups found for '{siteName}'.\n\n" +
                            "Create a backup first using the Backup button.",
                            "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Select a site first!", "WPronto",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            contextMenu.Items.Add(openBackupItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem deleteItem = new ToolStripMenuItem("🗑️ Delete");
            deleteItem.Click += (s, e) => BtnDeleteSite_Click(s, e);
            contextMenu.Items.Add(deleteItem);

            listSites.ContextMenuStrip = contextMenu;

            toolTip = new ToolTip();
            toolTip.SetToolTip(listSites, "Double-click to open admin panel\nRight-click for more options");

            cardSites.Controls.Add(listSites);

            Label lblLogsTitle = new Label
            {
                Text = "SERVER LOG",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = scheme.TextMuted,
                Location = new Point(ScaleInt(315), ScaleInt(155)),
                AutoSize = true
            };

            txtLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                BackColor = scheme.LogBackground,
                ForeColor = scheme.TextSecondary,
                Font = new Font("Consolas", 9f, FontStyle.Regular)
            };
            txtLog.Click += (s, e) =>
            {
                string logFile = Path.Combine(_logsPath, "wpronto.log");
                if (File.Exists(logFile))
                {
                    try { Process.Start("notepad.exe", logFile); }
                    catch { }
                }
            };
            toolTip.SetToolTip(txtLog, "Click to open log file in Notepad");

            ModernCard cardLogs = new ModernCard(ScaleInt(315), ScaleInt(175), ScaleInt(485), ScaleInt(285), _dpiScale);
            cardLogs.Controls.Add(txtLog);

            btnCreateSite = new SoftButton("Create", ButtonStyle.Primary, _dpiScale);
            btnCreateSite.Location = new Point(ScaleInt(32), ScaleInt(480));
            btnCreateSite.Width = ScaleInt(100);
            btnCreateSite.Click += BtnCreateSite_Click;

            btnBackupSite = new SoftButton("Backup", ButtonStyle.Success, _dpiScale);
            btnBackupSite.Location = new Point(ScaleInt(140), ScaleInt(480));
            btnBackupSite.Width = ScaleInt(90);
            btnBackupSite.Click += BtnBackupSite_Click;

            btnRestoreBackup = new SoftButton("Restore", ButtonStyle.Warning, _dpiScale);
            btnRestoreBackup.Location = new Point(ScaleInt(240), ScaleInt(480));
            btnRestoreBackup.Width = ScaleInt(90);
            btnRestoreBackup.Click += BtnRestoreBackup_Click;

            btnDeleteSite = new SoftButton("Delete", ButtonStyle.Danger, _dpiScale);
            btnDeleteSite.Location = new Point(ScaleInt(340), ScaleInt(480));
            btnDeleteSite.Width = ScaleInt(90);
            btnDeleteSite.Click += BtnDeleteSite_Click;

            btnHelp = new SoftButton("Help", ButtonStyle.Default, _dpiScale);
            btnHelp.Location = new Point(ScaleInt(440), ScaleInt(480));
            btnHelp.Width = ScaleInt(90);
            btnHelp.Click += BtnHelp_Click;

            lnkWebsite = new LinkLabel
            {
                Text = "Official website",
                Location = new Point(ScaleInt(660), ScaleInt(480)),
                AutoSize = true,
                LinkColor = scheme.PrimaryColor,
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Regular)
            };
            lnkWebsite.LinkClicked += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo { FileName = "https://ovcharovcoder.github.io/wpronto", UseShellExecute = true }); }
                catch { }
            };

            Label lblDev = new Label
            {
                Text = "© 2026 Andrii Ovcharov",
                Location = new Point(ScaleInt(660), ScaleInt(500)),
                AutoSize = true,
                ForeColor = scheme.TextMuted,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblSubtitle);
            this.Controls.Add(lblStatus);
            this.Controls.Add(btnTheme);
            this.Controls.Add(lblPhpVersion);
            this.Controls.Add(comboPhpVersion);
            this.Controls.Add(lblPhpStatus);
            this.Controls.Add(topButtonsPanel);
            this.Controls.Add(lblSitesTitle);
            this.Controls.Add(cardSites);
            this.Controls.Add(lblLogsTitle);
            this.Controls.Add(cardLogs);
            this.Controls.Add(btnCreateSite);
            this.Controls.Add(btnBackupSite);
            this.Controls.Add(btnRestoreBackup);
            this.Controls.Add(btnDeleteSite);
            this.Controls.Add(btnHelp);
            this.Controls.Add(lnkWebsite);
            this.Controls.Add(lblDev);
        }

        // =========================
        // EVENT HANDLERS
        // =========================
        private void BtnTheme_Click(object sender, EventArgs e)
        {
            try
            {
                var currentTheme = ThemeManager.CurrentTheme;
                AppTheme newTheme = currentTheme == AppTheme.Light ? AppTheme.Dark : (currentTheme == AppTheme.Dark ? AppTheme.System : AppTheme.Light);
                ThemeManager.SetTheme(newTheme);
                Log($"Theme changed to: {newTheme}");
            }
            catch (Exception ex)
            {
                LogError(ex, "BtnTheme_Click");
            }
        }

        private async void BtnRestart_Click(object sender, EventArgs e)
        {
            try
            {
                btnRestart.Enabled = false;
                Log("Restarting web server (Nginx + PHP)...");
                Log("MySQL will remain running to preserve database connections.");

                StopProcessGracefully("nginx", 2000);
                StopProcessGracefully("php-cgi", 1500);
                await DelayAsync(800);

                EnsurePhpIni();

                Log($"Starting PHP-CGI {_currentPhpVersion}...");
                string phpArgs = $"-b 127.0.0.1:{Config.PhpPort} -c \"{_phpIni}\"";
                if (!StartProcessSafely(_phpCgiPath, phpArgs, _phpWorkingDir, "PHP-CGI"))
                {
                    Log("Failed to start PHP-CGI!");
                    return;
                }
                await DelayAsync(Config.ProcessStartDelay);

                Log("Starting Nginx...");
                if (!StartProcessSafely(_nginxPath, $"-c \"{_nginxConf}\"", _nginxWorkingDir, "Nginx"))
                {
                    Log("Failed to start Nginx!");
                    return;
                }
                await DelayAsync(Config.NginxStartDelay);

                CheckServerStatus();
                Log("Web server restarted. MySQL connection preserved.");
                Log($"   http://localhost:{(_webPort == 80 ? "" : _webPort.ToString())}/ - WordPress");
                Log($"   http://localhost:{Config.PhpMyAdminPort}/ - phpMyAdmin");
            }
            catch (Exception ex)
            {
                LogError(ex, "BtnRestart_Click");
            }
            finally
            {
                btnRestart.Enabled = true;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            try
            {
                if (keyData == (Keys.Control | Keys.Shift | Keys.S))
                {
                    BtnStart_Click(this, EventArgs.Empty);
                    return true;
                }
                if (keyData == (Keys.Control | Keys.Shift | Keys.X))
                {
                    BtnStop_Click(this, EventArgs.Empty);
                    return true;
                }
                if (keyData == (Keys.Control | Keys.Shift | Keys.R))
                {
                    BtnRestart_Click(this, EventArgs.Empty);
                    return true;
                }
                if (keyData == (Keys.Control | Keys.Shift | Keys.P))
                {
                    BtnPhpMyAdmin_Click(this, EventArgs.Empty);
                    return true;
                }
                if (keyData == (Keys.Control | Keys.Shift | Keys.C))
                {
                    BtnCreateSite_Click(this, EventArgs.Empty);
                    return true;
                }
                if (keyData == (Keys.Control | Keys.Shift | Keys.B))
                {
                    BtnBackupSite_Click(this, EventArgs.Empty);
                    return true;
                }
                if (keyData == (Keys.Control | Keys.Shift | Keys.F))
                {
                    BtnRestoreBackup_Click(this, EventArgs.Empty);
                    return true;
                }
                if (keyData == (Keys.Control | Keys.Shift | Keys.D))
                {
                    BtnDeleteSite_Click(this, EventArgs.Empty);
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "ProcessCmdKey");
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ListSites_SelectedIndexChanged(object sender, EventArgs e)
        {
            // No logging to avoid log spam
        }

        // =========================
        // SITE TYPE CHECK
        // =========================
        private bool IsPhpOnlySite(string siteName)
        {
            try
            {
                if (siteName == "php") return true;

                string sitePath = Path.Combine(_wwwPath, siteName);
                if (!Directory.Exists(sitePath)) return false;

                if (File.Exists(Path.Combine(sitePath, "wp-config.php")))
                    return false;

                if (File.Exists(Path.Combine(sitePath, "wp-admin", "index.php")))
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                LogError(ex, $"IsPhpOnlySite - {siteName}");
                return false;
            }
        }

        // =========================
        // LISTBOX DRAW
        // =========================
        private void ListSites_DrawItem(object sender, DrawItemEventArgs e)
        {
            try
            {
                if (e.Index < 0) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
                string siteName = listSites.Items[e.Index]?.ToString() ?? "";
                var scheme = ThemeManager.CurrentScheme;

                e.Graphics.FillRectangle(new SolidBrush(scheme.BackgroundCard), e.Bounds);

                bool isPhpOnly = IsPhpOnlySite(siteName);
                string icon = isPhpOnly ? "🐘 " : "🌐 ";
                string displayText = icon + siteName;

                if (isSelected)
                {
                    Rectangle highlightRect = new Rectangle(e.Bounds.X + (int)(5 * _dpiScale), e.Bounds.Y + (int)(2 * _dpiScale),
                        e.Bounds.Width - (int)(10 * _dpiScale), e.Bounds.Height - (int)(4 * _dpiScale));
                    using (GraphicsPath path = CreateRoundedRect(highlightRect, (int)(5 * _dpiScale)))
                    {
                        using (SolidBrush b = new SolidBrush(scheme.SelectionBackground))
                            e.Graphics.FillPath(b, path);
                        using (Pen p = new Pen(scheme.PrimaryColor, 1.0f))
                            e.Graphics.DrawPath(p, path);
                    }
                }

                TextRenderer.DrawText(e.Graphics, displayText, listSites.Font, e.Bounds,
                    isSelected ? scheme.PrimaryColor : scheme.TextSecondary,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.LeftAndRightPadding);
            }
            catch (Exception ex)
            {
                LogError(ex, "ListSites_DrawItem");
            }
        }

        // =========================
        // UI HELPERS
        // =========================
        public static GraphicsPath CreateRoundedRect(Rectangle rect, int r)
        {
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, r, r, 180, 90);
            path.AddArc(rect.Right - r, rect.Y, r, r, 270, 90);
            path.AddArc(rect.Right - r, rect.Bottom - r, r, r, 0, 90);
            path.AddArc(rect.X, rect.Bottom - r, r, r, 90, 90);
            path.CloseFigure();
            return path;
        }

        // =========================
        // SYSTEM CHECKS
        // =========================
        private void CheckAdminRights()
        {
            try
            {
                using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
                {
                    var principal = new System.Security.Principal.WindowsPrincipal(identity);
                    bool isAdmin = principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

                    if (!isAdmin)
                    {
                        Log("NOT RUNNING AS ADMINISTRATOR!");
                        Log("Hosts file modification will not work!");
                        Log("Virtual domains like site.wp will not resolve!");
                        Log("Please restart the program as Administrator for full functionality");
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "CheckAdminRights");
            }
        }

        private void CleanupTempFiles()
        {
            try
            {
                string tempPath = Path.GetTempPath();
                var oldBatFiles = Directory.GetFiles(tempPath, "mysql_restore_*.bat");
                foreach (var file in oldBatFiles)
                {
                    try
                    {
                        if (File.GetCreationTime(file) < DateTime.Now.AddHours(-1))
                            File.Delete(file);
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "CleanupTempFiles");
            }
        }

        // =========================
        // FILE CHECKS
        // =========================
        private bool CheckRequiredFiles()
        {
            try
            {
                string[] required = { _nginxPath, _phpCgiPath, _mysqlPath };
                bool allExist = true;
                foreach (var file in required)
                {
                    if (!File.Exists(file))
                    {
                        LogToFile($"Missing: {file}");
                        allExist = false;
                    }
                }
                return allExist;
            }
            catch (Exception ex)
            {
                LogError(ex, "CheckRequiredFiles");
                return false;
            }
        }

        private bool IsValidWordPressInstall(string path)
        {
            try
            {
                return File.Exists(Path.Combine(path, "wp-admin", "index.php")) &&
                       File.Exists(Path.Combine(path, "wp-includes", "version.php"));
            }
            catch (Exception ex)
            {
                LogError(ex, $"IsValidWordPressInstall - {path}");
                return false;
            }
        }

        private void ValidateTemplate()
        {
            try
            {
                if (!Directory.Exists(_templatePath))
                {
                    Directory.CreateDirectory(_templatePath);
                    LogToFile("Created template folder");
                }
                if (!IsValidWordPressInstall(_templatePath))
                    LogToFile("Warning: Template folder doesn't look like a valid WordPress installation");
            }
            catch (Exception ex)
            {
                LogError(ex, "ValidateTemplate");
            }
        }

        // =========================
        // PORT MANAGEMENT
        // =========================
        private bool IsPortAvailable(int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    var result = client.BeginConnect("127.0.0.1", port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(1000);
                    if (success)
                    {
                        client.EndConnect(result);
                        return false;
                    }
                }
            }
            catch (SocketException) { return true; }
            catch { }
            return true;
        }

        private void FindAvailablePorts()
        {
            try
            {
                _webPort = Config.DefaultPort;
                if (!IsPortAvailable(Config.DefaultPort))
                {
                    _webPort = Config.AlternativePort;
                    LogToFile($"Port {Config.DefaultPort} is busy, using alternative port: {_webPort}");
                }

                // Check phpMyAdmin port
                if (!IsPortAvailable(Config.PhpMyAdminPort))
                {
                    LogToFile($"WARNING: Port {Config.PhpMyAdminPort} (phpMyAdmin) is busy!");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "FindAvailablePorts");
            }
        }

        // =========================
        // LOGGING (FIXED - with optimization)
        // =========================
        private void Log(string message)
        {
            try
            {
                if (txtLog == null)
                {
                    try { LogToFile(message); } catch { }
                    return;
                }

                if (txtLog.InvokeRequired)
                {
                    txtLog.Invoke(new Action(() => Log(message)));
                    return;
                }

                string logMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
                txtLog.AppendText(logMessage + Environment.NewLine);

                // Optimize: trim only every LOG_TRIM_THRESHOLD messages
                _logTrimCounter++;
                if (_logTrimCounter >= LOG_TRIM_THRESHOLD && txtLog.Lines.Length > MAX_LOG_LINES + 100)
                {
                    _logTrimCounter = 0;
                    var lines = txtLog.Lines;
                    var newLines = new string[MAX_LOG_LINES];
                    Array.Copy(lines, lines.Length - MAX_LOG_LINES, newLines, 0, MAX_LOG_LINES);
                    txtLog.Lines = newLines;
                }

                txtLog.ScrollToCaret();
                LogToFile(message);
            }
            catch { }
        }

        private void LogToFile(string message)
        {
            try
            {
                if (string.IsNullOrEmpty(_logsPath))
                    return;

                if (!Directory.Exists(_logsPath))
                    Directory.CreateDirectory(_logsPath);

                string logFile = Path.Combine(_logsPath, "wpronto.log");
                File.AppendAllText(logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
            }
            catch { }
        }

        // =========================
        // NGINX HELPERS
        // =========================
        private string GetNginxVersion()
        {
            try
            {
                var process = Process.Start(new ProcessStartInfo { FileName = _nginxPath, Arguments = "-v", UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true });
                return process?.StandardError.ReadToEnd() ?? "Unknown";
            }
            catch (Exception ex)
            {
                LogError(ex, "GetNginxVersion");
                return "Unknown";
            }
        }

        private bool ReloadNginx()
        {
            try
            {
                if (!IsProcessRunning("nginx"))
                {
                    Log("Nginx is not running, skipping reload");
                    return false;
                }

                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = _nginxPath,
                    Arguments = "-s reload",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = _nginxWorkingDir,
                    RedirectStandardError = true
                });

                if (process != null)
                {
                    process.WaitForExit(3000);
                    if (process.ExitCode != 0)
                    {
                        string error = process.StandardError.ReadToEnd();
                        Log($"Nginx reload error: {error}");
                        return false;
                    }
                    Log("Nginx reloaded successfully (no downtime)");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                LogError(ex, "ReloadNginx");
                return false;
            }
        }

        // =========================
        // HOSTS MANAGEMENT
        // =========================
        private void AddHostEntry(string domain)
        {
            try
            {
                string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
                string entry = $"127.0.0.1 {domain}";
                string content = File.ReadAllText(hostsPath);
                if (!content.Contains(entry))
                {
                    File.AppendAllText(hostsPath, Environment.NewLine + entry);
                    Log($"Hosts updated: {domain}");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, $"AddHostEntry - {domain}");
            }
        }

        // =========================
        // PHP VERSION MANAGEMENT
        // =========================
        private string LoadPhpVersionSetting()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "phpversion.config");
                if (File.Exists(configPath))
                {
                    string version = File.ReadAllText(configPath).Trim();
                    if (version == "8.3" || version == "8.5")
                        return version;
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "LoadPhpVersionSetting");
            }
            return "8.3";
        }

        private void SavePhpVersionSetting(string version)
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "phpversion.config");
                File.WriteAllText(configPath, version);
            }
            catch (Exception ex)
            {
                LogError(ex, "SavePhpVersionSetting");
            }
        }

        // =========================
        // PROCESS MANAGEMENT
        // =========================
        private bool IsProcessRunning(string name)
        {
            try
            {
                return Process.GetProcessesByName(name).Length > 0;
            }
            catch { return false; }
        }

        private void KillProcess(string name)
        {
            try
            {
                foreach (var p in Process.GetProcessesByName(name))
                {
                    try
                    {
                        if (!p.HasExited)
                        {
                            p.Kill();
                            p.WaitForExit(2000);
                            Log($"Stopped {name}");
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, $"KillProcess - {name}");
            }
        }

        private bool StartProcessSafely(string fileName, string arguments, string workingDirectory, string processName)
        {
            try
            {
                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardError = true
                });

                if (process == null)
                {
                    Log($"Failed to start {processName}");
                    return false;
                }

                System.Threading.Thread.Sleep(500);

                if (process.HasExited)
                {
                    Log($"{processName} started but exited immediately with code {process.ExitCode}");
                    return false;
                }

                string processExeName = Path.GetFileNameWithoutExtension(fileName).ToLower();
                if (!IsProcessRunning(processExeName))
                {
                    Log($"{processName} started but not found in process list");
                    return false;
                }

                Log($"{processName} started successfully");
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex, $"StartProcessSafely - {processName}");
                return false;
            }
        }

        private void StartProcessWithEnv(string path, string args, string workingDir)
        {
            try
            {
                if (!File.Exists(path))
                {
                    Log($"Error: Executable not found at {path}");
                    return;
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = args,
                    WorkingDirectory = workingDir,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                if (path.Contains("php-cgi.exe"))
                {
                    psi.EnvironmentVariables["PHP_FCGI_MAX_REQUESTS"] = "0";
                    psi.EnvironmentVariables["PHP_FCGI_CHILDREN"] = "0";
                }

                Process.Start(psi);
                Log($"Started: {Path.GetFileName(path)}");
            }
            catch (Exception ex)
            {
                LogError(ex, $"StartProcessWithEnv - {path}");
            }
        }

        // =========================
        // VALIDATE PRE-STARTUP
        // =========================
        private async Task<bool> ValidatePreStartupAsync()
        {
            try
            {
                Log("Running pre-startup checks...");

                if (!IsPortAvailable(Config.MysqlPort))
                {
                    Log($"⚠️ WARNING: Port {Config.MysqlPort} is busy!");
                    Log("Another MySQL/MariaDB server might be running (XAMPP, Laragon, OpenServer).");
                    Log("Please stop other local servers before starting WPronto.");

                    DialogResult result = MessageBox.Show(
                        $"⚠️ Port {Config.MysqlPort} is busy!\n\n" +
                        "Another MySQL/MariaDB server appears to be running.\n" +
                        "WPronto may fail to start correctly.\n\n" +
                        "Continue anyway?",
                        "Port Conflict",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (result == DialogResult.No)
                        return false;
                }

                int[] portsToCheck = new int[] { Config.PhpPort };
                if (!await WaitForPortsAvailable(portsToCheck, 3000))
                {
                    Log($"Port {Config.PhpPort} is busy!");
                    return false;
                }

                if (!CheckRequiredFiles())
                {
                    Log("Required files are missing!");
                    return false;
                }

                EnsurePhpIni();

                string[] requiredDirs = { _wwwPath, _logsPath, _tmpPath, _mysqlData };
                foreach (var dir in requiredDirs)
                {
                    if (!Directory.Exists(dir))
                    {
                        try { Directory.CreateDirectory(dir); }
                        catch (Exception ex)
                        {
                            LogError(ex, $"ValidatePreStartup - CreateDirectory {dir}");
                            return false;
                        }
                    }
                }

                Log("Pre-startup checks passed!");
                return true;
            }
            catch (Exception ex)
            {
                LogError(ex, "ValidatePreStartup");
                return false;
            }
        }

        // =========================
        // SERVER STATUS (FIXED)
        // =========================
        private void CheckServerStatus()
        {
            try
            {
                // Check InvokeRequired first
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(CheckServerStatus));
                    return;
                }

                bool running = IsProcessRunning("nginx") && IsProcessRunning("php-cgi") && IsProcessRunning("mysqld");
                var scheme = ThemeManager.CurrentScheme;

                if (running)
                {
                    lblStatus.Text = "● SERVER RUNNING";
                    lblStatus.ForeColor = scheme.StatusRunning;
                    btnStart.Enabled = false;
                    btnRestart.Enabled = true;
                    btnStop.Enabled = true;
                }
                else
                {
                    lblStatus.Text = "● SERVER STOPPED";
                    lblStatus.ForeColor = scheme.StatusStopped;
                    btnStart.Enabled = true;
                    btnRestart.Enabled = false;
                    btnStop.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "CheckServerStatus");
            }
        }

        // =========================
        // LOAD SITES
        // =========================
        private void LoadSites()
        {
            try
            {
                listSites.Items.Clear();
                if (!Directory.Exists(_wwwPath)) return;

                foreach (string dir in Directory.GetDirectories(_wwwPath))
                {
                    string name = Path.GetFileName(dir);
                    if (name != "default")
                    {
                        listSites.Items.Add(name);
                    }
                }
                if (listSites.Items.Count > 0) listSites.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                LogError(ex, "LoadSites");
            }
        }

        // =========================
        // START SERVER (FIXED)
        // =========================
        private async void BtnStart_Click(object sender, EventArgs e)
        {
            try
            {
                btnStart.Enabled = false;

                if (IsProcessRunning("nginx") && IsProcessRunning("php-cgi") && IsProcessRunning("mysqld"))
                {
                    Log("Server is already running!");
                    return;
                }

                // Validate pre-startup ALWAYS, regardless of MySQL state
                if (!await ValidatePreStartupAsync())
                {
                    Log("Server startup validation failed!");
                    return;
                }

                bool needStopNginx = IsProcessRunning("nginx");
                bool needStopPhp = IsProcessRunning("php-cgi");

                if (needStopNginx || needStopPhp)
                {
                    Log("Stopping web server (Nginx + PHP) before restart...");
                    if (needStopNginx) StopProcessGracefully("nginx", 2000);
                    if (needStopPhp) StopProcessGracefully("php-cgi", 1500);
                    await DelayAsync(800);
                }

                if (!IsProcessRunning("mysqld"))
                {
                    Log("MySQL is not running, starting...");

                    string mysqlDataUnix = _mysqlData.Replace("\\", "/");

                    bool mysqlNeedsInit = !Directory.Exists(Path.Combine(_mysqlData, "mysql"));

                    if (mysqlNeedsInit)
                    {
                        Log("Initializing MySQL...");
                        var initProcess = Process.Start(new ProcessStartInfo
                        {
                            FileName = _mysqlPath,
                            Arguments = $"--initialize-insecure --datadir=\"{mysqlDataUnix}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = _mysqlWorkingDir
                        });

                        if (initProcess != null)
                        {
                            if (!initProcess.WaitForExit(30000))
                            {
                                Log("MySQL initialization timeout, but continuing...");
                                try { initProcess.Kill(); } catch { }
                            }
                            else
                            {
                                Log("MySQL initialized");
                            }
                        }
                    }

                    int[] portsToCheck = new int[] { Config.PhpPort, Config.MysqlPort };
                    if (!await WaitForPortsAvailable(portsToCheck, 5000))
                    {
                        Log($"Ports {Config.PhpPort} or {Config.MysqlPort} are not available!");
                        return;
                    }

                    Log("Starting MySQL...");
                    string mysqlArgs = $"--defaults-file=\"{_mysqlConfPath}\" --datadir=\"{mysqlDataUnix}\" --bind-address=127.0.0.1 --port={Config.MysqlPort}";
                    if (!StartProcessSafely(_mysqlPath, mysqlArgs, _mysqlWorkingDir, "MySQL"))
                    {
                        Log("Failed to start MySQL!");
                        return;
                    }
                    await DelayAsync(Config.ProcessStartDelay);
                }
                else
                {
                    Log("MySQL is already running, skipping restart");
                }

                EnsurePhpIni();

                Log($"Starting PHP-CGI {_currentPhpVersion}...");
                string phpArgs = $"-b 127.0.0.1:{Config.PhpPort} -c \"{_phpIni}\"";
                Log($"PHP args: {phpArgs}");
                if (!StartProcessSafely(_phpCgiPath, phpArgs, _phpWorkingDir, "PHP-CGI"))
                {
                    Log("Failed to start PHP-CGI!");
                    return;
                }
                await DelayAsync(Config.ProcessStartDelay);

                Log("Starting Nginx...");
                if (!StartProcessSafely(_nginxPath, $"-c \"{_nginxConf}\"", _nginxWorkingDir, "Nginx"))
                {
                    Log("Failed to start Nginx!");
                    return;
                }
                await DelayAsync(Config.NginxStartDelay);

                CheckServerStatus();

                Log($"Server is running with PHP {_currentPhpVersion}!");
                Log($"   http://localhost:{(_webPort == 80 ? "" : _webPort.ToString())}/ - WordPress");
                Log($"   http://localhost:{Config.PhpMyAdminPort}/ - phpMyAdmin");
                Log($"   Max upload size: {Config.UploadMaxSize}MB");
            }
            catch (Exception ex)
            {
                LogError(ex, "BtnStart_Click");
            }
            finally
            {
                btnStart.Enabled = true;
            }
        }

        // =========================
        // STOP SERVER
        // =========================
        private void BtnStop_Click(object sender, EventArgs e)
        {
            try
            {
                Log("Stopping services gracefully...");
                StopProcessGracefully("nginx", 2000);
                StopProcessGracefully("php-cgi", 1500);
                StopProcessGracefully("mysqld", 5000);
                Log("All services stopped");
                CheckServerStatus();
            }
            catch (Exception ex)
            {
                LogError(ex, "BtnStop_Click");
            }
        }

        // =========================
        // OPEN PHPMYADMIN
        // =========================
        private void BtnPhpMyAdmin_Click(object sender, EventArgs e)
        {
            try
            {
                string url = $"http://localhost:{Config.PhpMyAdminPort}/";
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                    Log($"Opening phpMyAdmin: {url}");
                }
                catch (Exception ex)
                {
                    LogError(ex, "BtnPhpMyAdmin_Click - Process.Start");
                    MessageBox.Show($"Failed to open browser: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "BtnPhpMyAdmin_Click");
            }
        }

        // =========================
        // CREATE SITE (FIXED)
        // =========================
        private async void BtnCreateSite_Click(object sender, EventArgs e)
        {
            try
            {
                string siteName = Microsoft.VisualBasic.Interaction.InputBox("Enter site name (letters, numbers, hyphens):", "Create New Site", "mynewsite");
                if (string.IsNullOrWhiteSpace(siteName)) return;
                siteName = Regex.Replace(siteName, @"[^a-zA-Z0-9\-]", "").ToLower();

                // FIXED: Check if site name is empty after filtering
                if (string.IsNullOrEmpty(siteName))
                {
                    MessageBox.Show("Site name cannot be empty after removing invalid characters.\nUse only letters, numbers, hyphens.",
                        "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                bool isPhpOnlyMode = (siteName == "php");

                if (isPhpOnlyMode)
                {
                    DialogResult result = MessageBox.Show(
                        "PHP Learning Mode\n\n" +
                        "You are about to create a site with the name 'php'.\n\n" +
                        "This is a SPECIAL mode:\n" +
                        "• No WordPress will be installed\n" +
                        "• No database will be created\n" +
                        "• Only pure PHP for learning and testing\n\n" +
                        "Files will be located in: WPronto/www/php/\n\n" +
                        "Do you want to continue?",
                        "WPronto - PHP Mode",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result != DialogResult.Yes) return;
                }

                string sitePath = Path.Combine(_wwwPath, siteName);
                if (Directory.Exists(sitePath))
                {
                    MessageBox.Show("Site already exists!", "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!isPhpOnlyMode && !Directory.Exists(_templatePath))
                {
                    MessageBox.Show("WordPress template not found.\n\nPlease add WordPress files to the 'template' folder.", "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    Log($"Creating site: {siteName} (Mode: {(isPhpOnlyMode ? "PHP Server" : "WordPress")})");
                    Directory.CreateDirectory(sitePath);

                    if (isPhpOnlyMode)
                    {
                        await CreatePhpOnlySiteAsync(sitePath, siteName);
                    }
                    else
                    {
                        if (!IsValidWordPressInstall(_templatePath))
                        {
                            MessageBox.Show("Template folder is missing required WordPress files!\n\n" +
                                "Please ensure you have a complete WordPress installation in the 'template' folder.\n\n" +
                                "Required files:\n" +
                                "• wp-admin/index.php\n" +
                                "• wp-includes/version.php\n\n" +
                                "Download WordPress from: https://wordpress.org/download/",
                                "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            Directory.Delete(sitePath, true);
                            return;
                        }

                        CopyDirectoryIterative(_templatePath, sitePath);

                        string userIniPath = Path.Combine(sitePath, ".user.ini");
                        string userIniContent = $"upload_max_filesize = {Config.UploadMaxSize}M\npost_max_size = {Config.UploadMaxSize}M\nmemory_limit = {Config.MemoryLimit}M\nmax_execution_time = 600\nmax_input_time = 600";
                        File.WriteAllText(userIniPath, userIniContent, Encoding.ASCII);
                        Log($"   Created .user.ini ({Config.UploadMaxSize}MB limit)");

                        string dbName = $"{siteName}_db";
                        string rootUnix = sitePath.Replace("\\", "/");
                        string nginxConfig = "server {\n" +
                            $"    listen {_webPort};\n" +
                            $"    server_name {siteName}.wp;\n" +
                            $"    root \"{rootUnix}\";\n" +
                            "    index index.php;\n\n" +
                            $"    client_max_body_size {Config.UploadMaxSize}M;\n\n" +
                            "    location / { try_files $uri $uri/ /index.php?$args; }\n\n" +
                            "    location ~ /\\. { deny all; }\n\n" +
                            "    location ~ \\.php$ {\n" +
                            "        try_files     $uri =404;\n" +
                            "        fastcgi_pass  127.0.0.1:9000;\n" +
                            "        fastcgi_param SCRIPT_FILENAME $document_root$fastcgi_script_name;\n" +
                            "        include       fastcgi_params;\n" +
                            "        fastcgi_read_timeout 600;\n" +
                            "    }\n}\n";
                        File.WriteAllText(Path.Combine(_sitesPath, $"{siteName}.conf"), nginxConfig, Utf8NoBom);
                        AddHostEntry($"{siteName}.wp");
                        CreateDatabase(dbName);
                        CreateWpConfig(sitePath, dbName, siteName);

                        if (!ReloadNginx())
                        {
                            Log("   Nginx reload failed, performing full restart...");
                            KillProcess("nginx");
                            StartProcessWithEnv(_nginxPath, $"-c \"{_nginxConf}\"", _nginxWorkingDir);
                        }

                        LoadSites();

                        string port = _webPort == 80 ? "" : $":{_webPort}";
                        string siteUrl = $"http://{siteName}.wp{port}";
                        Log($"Site created successfully!");
                        Log($"   URL: {siteUrl}");
                        Log($"   Admin: {siteUrl}/wp-admin");
                        Log($"   Database: {dbName} (user: root, no password)");
                        if (MessageBox.Show($"Site '{siteName}' created!\n\nOpen WordPress install page?", "WPronto", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                            Process.Start(new ProcessStartInfo { FileName = $"{siteUrl}/wp-admin/install.php", UseShellExecute = true });
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, $"BtnCreateSite_Click - Create {siteName}");
                    MessageBox.Show($"Error creating site: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "BtnCreateSite_Click");
            }
        }

        // =========================
        // CREATE PHP-ONLY SITE
        // =========================
        private async Task CreatePhpOnlySiteAsync(string sitePath, string siteName)
        {
            try
            {
                Log($"   Creating PHP-only development environment...");

                string indexContent = @"<?php
// Enable error reporting for easy debugging
ini_set('display_errors', 1);
ini_set('display_startup_errors', 1);
error_reporting(E_ALL);

// Your code starts here
echo 'Hello, World!';
";

                await File.WriteAllTextAsync(Path.Combine(sitePath, "index.php"), indexContent, Encoding.UTF8);

                string userIniContent = @"; WPronto PHP Server Settings
upload_max_filesize = 256M
post_max_size = 256M
memory_limit = 512M
max_execution_time = 600
max_input_time = 600
display_errors = On
display_startup_errors = On
error_reporting = E_ALL";

                await File.WriteAllTextAsync(Path.Combine(sitePath, ".user.ini"), userIniContent, Encoding.ASCII);

                int port = _webPort;
                string rootUnix = sitePath.Replace("\\", "/");

                string nginxConfig = $@"server {{
    listen {port};
    server_name {siteName}.wp;
    root ""{rootUnix}"";
    index index.php index.html;
    
    client_max_body_size 256M;
    
    location / {{
        try_files $uri $uri/ =404;
    }}
    
    location ~ \.php$ {{
        try_files $uri =404;
        fastcgi_pass 127.0.0.1:{Config.PhpPort};
        fastcgi_param SCRIPT_FILENAME $document_root$fastcgi_script_name;
        include fastcgi_params;
        fastcgi_read_timeout 600;
    }}
}}";

                await File.WriteAllTextAsync(Path.Combine(_sitesPath, $"{siteName}.conf"), nginxConfig, Utf8NoBom);

                AddHostEntry($"{siteName}.wp");

                if (!ReloadNginx())
                {
                    Log("   Nginx reload failed, performing full restart...");
                    KillProcess("nginx");
                    StartProcessWithEnv(_nginxPath, $"-c \"{_nginxConf}\"", _nginxWorkingDir);
                }

                LoadSites();

                string portStr = _webPort == 80 ? "" : $":{_webPort}";
                string siteUrl = $"http://{siteName}.wp{portStr}";

                Log($"   PHP server created successfully!");
                Log($"   Files: index.php, .user.ini");
                Log($"   Error reporting enabled (display_errors = On)");
                Log($"   No database created");
                Log($"Site created successfully!");
                Log($"   URL: {siteUrl}");
                Log($"   Mode: PHP Server (no WordPress, no database)");

                if (MessageBox.Show($"PHP Server '{siteName}' created!\n\nOpen site?", "WPronto",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Process.Start(new ProcessStartInfo { FileName = siteUrl, UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                LogError(ex, $"CreatePhpOnlySiteAsync - {siteName}");
                throw;
            }
        }

        // =========================
        // DATABASE HELPERS
        // =========================
        private void CreateDatabase(string dbName)
        {
            try
            {
                Log($"Creating database: {dbName}");
                Process.Start(new ProcessStartInfo { FileName = _mysqlClientPath, Arguments = $"-u root -e \"CREATE DATABASE IF NOT EXISTS {dbName};\"", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(5000);
                Log($"Created database: {dbName}");
            }
            catch (Exception ex)
            {
                LogError(ex, $"CreateDatabase - {dbName}");
            }
        }

        // =========================
        // WORDPRESS CONFIG
        // =========================
        private void CreateWpConfig(string sitePath, string dbName, string siteName)
        {
            try
            {
                string port = _webPort == 80 ? "" : $":{_webPort}";
                string content = "<?php\n" +
                    "/** WPronto config */\n" +
                    "define( 'DB_NAME', '" + dbName + "' );\n" +
                    "define( 'DB_USER', 'root' );\n" +
                    "define( 'DB_PASSWORD', '' );\n" +
                    "define( 'DB_HOST', '127.0.0.1' );\n" +
                    "define( 'DB_CHARSET', 'utf8mb4' );\n" +
                    "define( 'DB_COLLATE', '' );\n\n" +
                    "if ( ! defined( 'WP_CLI' ) ) {\n" +
                    "    define( 'WP_SITEURL', 'http://" + siteName + ".wp" + port + "' );\n" +
                    "    define( 'WP_HOME',    'http://" + siteName + ".wp" + port + "' );\n" +
                    "}\n\n" +
                    "define( 'AUTH_KEY',         '" + GenerateSecureKey(64) + "' );\n" +
                    "define( 'SECURE_AUTH_KEY',  '" + GenerateSecureKey(64) + "' );\n" +
                    "define( 'LOGGED_IN_KEY',    '" + GenerateSecureKey(64) + "' );\n" +
                    "define( 'NONCE_KEY',        '" + GenerateSecureKey(64) + "' );\n" +
                    "define( 'AUTH_SALT',        '" + GenerateSecureKey(64) + "' );\n" +
                    "define( 'SECURE_AUTH_SALT', '" + GenerateSecureKey(64) + "' );\n" +
                    "define( 'LOGGED_IN_SALT',   '" + GenerateSecureKey(64) + "' );\n" +
                    "define( 'NONCE_SALT',       '" + GenerateSecureKey(64) + "' );\n\n" +
                    "$table_prefix = 'wp_';\n" +
                    "define( 'WP_DEBUG', false );\n" +
                    "define( 'WP_DEBUG_DISPLAY', false );\n" +
                    "define( 'WP_MEMORY_LIMIT', '" + Config.MemoryLimit + "M' );\n" +
                    "define( 'WP_MAX_MEMORY_LIMIT', '" + Config.MemoryLimit + "M' );\n\n" +
                    "if ( ! defined( 'ABSPATH' ) ) define( 'ABSPATH', __DIR__ . '/' );\n" +
                    "require_once ABSPATH . 'wp-settings.php';";
                File.WriteAllText(Path.Combine(sitePath, "wp-config.php"), content, new UTF8Encoding(false));
                Log($"Created wp-config.php for {siteName}");
            }
            catch (Exception ex)
            {
                LogError(ex, $"CreateWpConfig - {siteName}");
            }
        }

        // =========================
        // CRYPTOGRAPHICALLY SECURE KEY GENERATOR
        // =========================
        private string GenerateSecureKey(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_-+=<>?";
            byte[] bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);

            char[] result = new char[length];
            for (int i = 0; i < length; i++)
            {
                result[i] = chars[bytes[i] % chars.Length];
            }
            return new string(result);
        }

        // =========================
        // BACKUP SYSTEM
        // =========================
        private void BackupDatabase(string dbName, string backupDir)
        {
            try
            {
                string mysqldumpPath = Path.Combine(_basePath, @"core\mysql\bin\mysqldump.exe");
                if (File.Exists(mysqldumpPath))
                {
                    string backupFile = Path.Combine(backupDir, $"{dbName}.sql");
                    var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = mysqldumpPath,
                        Arguments = $"-u root --databases {dbName} --result-file=\"{backupFile}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    });
                    if (process != null)
                    {
                        process.WaitForExit(10000);
                        if (process.ExitCode == 0 && File.Exists(backupFile) && new FileInfo(backupFile).Length > 0)
                            LogToFile($"   Database backup: {backupFile}");
                        else
                            LogToFile($"   Database backup failed: {process.StandardError.ReadToEnd()}");
                    }
                }
                else
                    LogToFile($"   mysqldump not found, database backup skipped");
            }
            catch (Exception ex)
            {
                LogError(ex, $"BackupDatabase - {dbName}");
            }
        }

        private void BackupSiteFiles(string sitePath, string backupDir)
        {
            try
            {
                string filesBackupDir = Path.Combine(backupDir, "files");
                CopyDirectoryIterative(sitePath, filesBackupDir);
                LogToFile($"   Files backup: {filesBackupDir}");
            }
            catch (Exception ex)
            {
                LogError(ex, $"BackupSiteFiles - {sitePath}");
            }
        }

        private void CreateFullBackup(string siteName)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string backupDir = Path.Combine(_backupPath, siteName, timestamp);
                Directory.CreateDirectory(backupDir);
                Log($"Creating backup for '{siteName}'...");
                string sitePath = Path.Combine(_wwwPath, siteName);
                if (Directory.Exists(sitePath))
                    BackupSiteFiles(sitePath, backupDir);
                else
                    Log($"   Site folder not found: {sitePath}");
                string dbName = $"{siteName}_db";
                BackupDatabase(dbName, backupDir);
                Log($"Backup completed successfully! Location: {backupDir}");
                MessageBox.Show($"Site '{siteName}' has been backed up successfully!\n\nBackup location:\n{backupDir}", "Backup Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogError(ex, $"CreateFullBackup - {siteName}");
                MessageBox.Show($"Backup failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private List<BackupInfo> GetAvailableBackups(string siteName)
        {
            var backups = new List<BackupInfo>();
            try
            {
                string backupPath = Path.Combine(_backupPath, siteName);

                if (!Directory.Exists(backupPath))
                    return backups;

                foreach (string dir in Directory.GetDirectories(backupPath))
                {
                    string timestamp = Path.GetFileName(dir);
                    DateTime? backupDate = null;

                    if (DateTime.TryParseExact(timestamp, "yyyy-MM-dd_HH-mm-ss",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out DateTime parsed))
                    {
                        backupDate = parsed;
                    }

                    bool hasFiles = Directory.Exists(Path.Combine(dir, "files"));
                    bool hasDatabase = File.Exists(Path.Combine(dir, $"{siteName}_db.sql"));

                    backups.Add(new BackupInfo
                    {
                        Path = dir,
                        Timestamp = timestamp,
                        BackupDate = backupDate,
                        HasFiles = hasFiles,
                        HasDatabase = hasDatabase,
                        Size = GetDirectorySize(dir)
                    });
                }

                return backups.OrderByDescending(b => b.BackupDate).ToList();
            }
            catch (Exception ex)
            {
                LogError(ex, $"GetAvailableBackups - {siteName}");
                return backups;
            }
        }

        private string GetDirectorySize(string path)
        {
            try
            {
                long size = 0;
                var files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                    size += new FileInfo(file).Length;

                string[] sizes = { "B", "KB", "MB", "GB" };
                int order = 0;
                double len = size;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len /= 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }
            catch (Exception ex)
            {
                LogError(ex, $"GetDirectorySize - {path}");
                return "Unknown";
            }
        }

        // =========================
        // RESTORE FROM BACKUP (FIXED)
        // =========================
        private async Task RestoreFromBackup(string siteName, BackupInfo backup)
        {
            try
            {
                Log($"Restoring site '{siteName}' from backup: {backup.Timestamp}");

                string sitePath = Path.Combine(_wwwPath, siteName);

                if (!Directory.Exists(sitePath))
                {
                    Log($"   Site folder doesn't exist, creating: {sitePath}");
                    Directory.CreateDirectory(sitePath);
                }

                bool wasNginxRunning = IsProcessRunning("nginx");
                bool wasPhpRunning = IsProcessRunning("php-cgi");
                bool wasMysqlRunning = IsProcessRunning("mysqld");

                if (wasNginxRunning || wasPhpRunning)
                {
                    Log("   Stopping web server (Nginx + PHP) for restore...");
                    StopProcessGracefully("nginx", 2000);
                    StopProcessGracefully("php-cgi", 1500);
                    await DelayAsync(1000);
                }

                if (!wasMysqlRunning)
                {
                    Log("   MySQL is not running! Starting MySQL...");
                    string mysqlDataUnix = _mysqlData.Replace("\\", "/");
                    string mysqlArgs = $"--defaults-file=\"{_mysqlConfPath}\" --datadir=\"{mysqlDataUnix}\" --bind-address=127.0.0.1 --port={Config.MysqlPort}";
                    StartProcessWithEnv(_mysqlPath, mysqlArgs, _mysqlWorkingDir);
                    await DelayAsync(Config.ProcessStartDelay);
                }

                string filesBackup = Path.Combine(backup.Path, "files");
                if (Directory.Exists(filesBackup))
                {
                    Log("   Restoring files...");

                    foreach (var dir in Directory.GetDirectories(sitePath))
                    {
                        try { Directory.Delete(dir, true); } catch { }
                    }
                    foreach (var file in Directory.GetFiles(sitePath))
                    {
                        try { File.Delete(file); } catch { }
                    }

                    CopyDirectoryIterative(filesBackup, sitePath);
                    Log($"   Files restored from: {filesBackup}");
                }
                else
                {
                    Log("   No files backup found, skipping...");
                }

                string dbBackup = Path.Combine(backup.Path, $"{siteName}_db.sql");
                if (File.Exists(dbBackup))
                {
                    Log("   Restoring database...");

                    string dbName = $"{siteName}_db";
                    string mysqlPath = _mysqlClientPath;

                    Log($"   Dropping old database '{dbName}'...");
                    var dropProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = mysqlPath,
                        Arguments = $"-u root -e \"DROP DATABASE IF EXISTS {dbName}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    });
                    if (dropProcess != null)
                    {
                        dropProcess.WaitForExit(5000);
                        Log($"   Old database dropped");
                    }

                    Log($"   Creating new database '{dbName}'...");
                    var createProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = mysqlPath,
                        Arguments = $"-u root -e \"CREATE DATABASE {dbName}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    });
                    if (createProcess != null)
                    {
                        createProcess.WaitForExit(5000);
                        Log($"   New database created");
                    }

                    long fileSize = new FileInfo(dbBackup).Length;
                    int timeoutMs = 60000;

                    if (fileSize > 100 * 1024 * 1024)
                        timeoutMs = 300000;
                    if (fileSize > 500 * 1024 * 1024)
                        timeoutMs = 600000;
                    if (fileSize > 1024 * 1024 * 1024)
                        timeoutMs = 1200000;

                    string sizeStr = "";
                    if (fileSize < 1024 * 1024)
                        sizeStr = $"{fileSize / 1024:F1} KB";
                    else if (fileSize < 1024 * 1024 * 1024)
                        sizeStr = $"{fileSize / (1024.0 * 1024.0):F1} MB";
                    else
                        sizeStr = $"{fileSize / (1024.0 * 1024.0 * 1024.0):F1} GB";

                    Log($"   Importing database from backup (size: {sizeStr}, timeout: {timeoutMs / 1000} sec)...");

                    DateTime startTime = DateTime.Now;

                    string batFile = Path.Combine(Path.GetTempPath(), $"mysql_restore_{Guid.NewGuid()}.bat");
                    string batContent = $"\"{mysqlPath}\" -u root --default-character-set=utf8mb4 {dbName} < \"{dbBackup}\"";
                    File.WriteAllText(batFile, batContent, Encoding.ASCII);

                    var importProcess = Process.Start(new ProcessStartInfo
                    {
                        FileName = batFile,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardError = true
                    });

                    if (importProcess != null)
                    {
                        // FIXED: Check if process finished within timeout
                        bool finished = importProcess.WaitForExit(timeoutMs);
                        double elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;

                        if (!finished)
                        {
                            Log($"   Import timeout after {timeoutMs / 1000} sec — process still running");
                            // Don't kill the process - let it complete naturally
                            await DelayAsync(1000);
                            // Check again after a short delay
                            if (!importProcess.HasExited)
                            {
                                Log($"   Import still running, waiting longer...");
                                importProcess.WaitForExit();
                            }
                        }

                        if (importProcess.ExitCode == 0)
                        {
                            Log($"   Database restored successfully! (Completed in {elapsedSeconds:F1} seconds)");
                        }
                        else
                        {
                            string error = await importProcess.StandardError.ReadToEndAsync();
                            Log($"   Database restore completed with exit code {importProcess.ExitCode} (took {elapsedSeconds:F1} sec)");
                            if (!string.IsNullOrEmpty(error))
                                Log($"   Error: {error}");
                        }
                    }

                    try { File.Delete(batFile); } catch { }
                }
                else
                {
                    Log("   No database backup found, skipping...");
                }

                string confPath = Path.Combine(_sitesPath, $"{siteName}.conf");
                string rootUnix = sitePath.Replace("\\", "/");

                string nginxConfig = "server {\n" +
                    $"    listen {_webPort};\n" +
                    $"    server_name {siteName}.wp;\n" +
                    $"    root \"{rootUnix}\";\n" +
                    "    index index.php index.html;\n\n" +
                    $"    client_max_body_size {Config.UploadMaxSize}M;\n\n" +
                    "    location / { try_files $uri $uri/ /index.php?$args; }\n\n" +
                    "    location ~ /\\. { deny all; }\n\n" +
                    "    location ~ \\.php$ {\n" +
                    "        try_files     $uri =404;\n" +
                    "        fastcgi_pass  127.0.0.1:9000;\n" +
                    "        fastcgi_param SCRIPT_FILENAME $document_root$fastcgi_script_name;\n" +
                    "        include       fastcgi_params;\n" +
                    "        fastcgi_read_timeout 600;\n" +
                    "    }\n}\n";

                File.WriteAllText(confPath, nginxConfig, Utf8NoBom);
                Log("   Nginx config created/updated");

                AddHostEntry($"{siteName}.wp");

                if (wasNginxRunning || wasPhpRunning)
                {
                    Log("   Restarting web server...");
                    await DelayAsync(500);

                    EnsurePhpIni();

                    Log($"   Starting PHP-CGI {_currentPhpVersion}...");
                    StartProcessWithEnv(_phpCgiPath, $"-b 127.0.0.1:{Config.PhpPort} -c \"{_phpIni}\"", _phpWorkingDir);
                    await DelayAsync(Config.ProcessStartDelay);

                    Log("   Starting Nginx...");
                    StartProcessWithEnv(_nginxPath, $"-c \"{_nginxConf}\"", _nginxWorkingDir);
                    await DelayAsync(Config.NginxStartDelay);
                }

                LoadSites();

                for (int i = 0; i < listSites.Items.Count; i++)
                {
                    if (listSites.Items[i].ToString() == siteName)
                    {
                        listSites.SelectedIndex = i;
                        break;
                    }
                }

                Log($"Site '{siteName}' restored successfully from backup: {backup.Timestamp}");
                MessageBox.Show($"Site '{siteName}' has been restored successfully from backup!\n\nBackup: {backup.Timestamp}",
                    "Restore Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogError(ex, $"RestoreFromBackup - {siteName}");
                MessageBox.Show($"Restore failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnBackupSite_Click(object sender, EventArgs e)
        {
            try
            {
                if (listSites.SelectedItem == null)
                {
                    MessageBox.Show("Select a site first!", "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string siteName = listSites.SelectedItem.ToString() ?? "";
                bool isPhpOnly = IsPhpOnlySite(siteName);

                if (isPhpOnly)
                {
                    MessageBox.Show("PHP Mode sites don't have databases to backup.\n\nYou can manually copy the files from the 'www/php' folder.",
                        "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (MessageBox.Show($"Create a backup of site '{siteName}'?\n\nThis will create a copy of:\nWebsite files\nDatabase\n\nThe site will remain active.", "Confirm Backup", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    CreateFullBackup(siteName);
            }
            catch (Exception ex)
            {
                LogError(ex, "BtnBackupSite_Click");
            }
        }

        private async void BtnRestoreBackup_Click(object sender, EventArgs e)
        {
            try
            {
                List<BackupInfo> availableBackups = new List<BackupInfo>();
                string preselectedSite = null;

                if (listSites.SelectedItem != null)
                {
                    preselectedSite = listSites.SelectedItem.ToString();
                    availableBackups = GetAvailableBackups(preselectedSite);
                }
                else
                {
                    if (Directory.Exists(_backupPath))
                    {
                        foreach (string siteDir in Directory.GetDirectories(_backupPath))
                        {
                            string siteName = Path.GetFileName(siteDir);
                            var backups = GetAvailableBackups(siteName);
                            foreach (var backup in backups)
                            {
                                availableBackups.Add(backup);
                            }
                        }
                    }
                }

                if (availableBackups.Count == 0)
                {
                    MessageBox.Show("No backups found!", "No Backups",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Form backupForm = new Form
                {
                    Text = string.IsNullOrEmpty(preselectedSite) ? "Select Backup to Restore" : $"Restore Backup - {preselectedSite}",
                    Size = new Size(ScaleInt(550), ScaleInt(450)),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = ThemeManager.CurrentScheme.BackgroundPrimary
                };

                ListBox backupList = new ListBox
                {
                    Dock = DockStyle.Fill,
                    Font = new Font("Segoe UI", 10f),
                    ItemHeight = 30,
                    BackColor = ThemeManager.CurrentScheme.BackgroundCard,
                    ForeColor = ThemeManager.CurrentScheme.TextSecondary,
                    BorderStyle = BorderStyle.None
                };

                foreach (var backup in availableBackups)
                    backupList.Items.Add(backup);

                if (backupList.Items.Count > 0)
                    backupList.SelectedIndex = 0;

                Panel buttonPanel = new Panel
                {
                    Dock = DockStyle.Bottom,
                    Height = ScaleInt(70),
                    Padding = new Padding(ScaleInt(10)),
                    BackColor = ThemeManager.CurrentScheme.BackgroundPrimary
                };

                Button btnRestore = new Button
                {
                    Text = "Restore",
                    DialogResult = DialogResult.OK,
                    Size = new Size(ScaleInt(100), ScaleInt(34)),
                    BackColor = ThemeManager.CurrentScheme.PrimaryColor,
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    FlatAppearance = { BorderSize = 0 }
                };

                btnRestore.Paint += (s, ev) =>
                {
                    Button btn = (Button)s;
                    GraphicsPath path = CreateRoundedRect(new Rectangle(0, 0, btn.Width, btn.Height), 8);
                    btn.Region = new Region(path);
                };

                Button btnCancel = new Button
                {
                    Text = "Cancel",
                    DialogResult = DialogResult.Cancel,
                    Size = new Size(ScaleInt(100), ScaleInt(34)),
                    BackColor = Color.Transparent,
                    ForeColor = ThemeManager.CurrentScheme.TextSecondary,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    FlatAppearance = { BorderSize = 1, BorderColor = ThemeManager.CurrentScheme.BorderColor }
                };

                btnCancel.Paint += (s, ev) =>
                {
                    Button btn = (Button)s;
                    GraphicsPath path = CreateRoundedRect(new Rectangle(0, 0, btn.Width, btn.Height), 8);
                    btn.Region = new Region(path);
                };

                ThemeManager.ThemeChanged += (theme) =>
                {
                    var scheme = ThemeManager.CurrentScheme;
                    backupForm.BackColor = scheme.BackgroundPrimary;
                    buttonPanel.BackColor = scheme.BackgroundPrimary;
                    backupList.BackColor = scheme.BackgroundCard;
                    backupList.ForeColor = scheme.TextSecondary;
                    btnRestore.BackColor = scheme.PrimaryColor;
                    btnCancel.ForeColor = scheme.TextSecondary;
                    btnCancel.FlatAppearance.BorderColor = scheme.BorderColor;
                };

                btnRestore.Location = new Point((buttonPanel.Width - btnRestore.Width - btnCancel.Width - ScaleInt(20)) / 2, (buttonPanel.Height - btnRestore.Height) / 2);
                btnCancel.Location = new Point(btnRestore.Right + ScaleInt(20), (buttonPanel.Height - btnCancel.Height) / 2);

                buttonPanel.Resize += (s, ev) =>
                {
                    btnRestore.Location = new Point((buttonPanel.Width - btnRestore.Width - btnCancel.Width - ScaleInt(20)) / 2, (buttonPanel.Height - btnRestore.Height) / 2);
                    btnCancel.Location = new Point(btnRestore.Right + ScaleInt(20), (buttonPanel.Height - btnCancel.Height) / 2);
                };

                buttonPanel.Controls.AddRange(new Control[] { btnRestore, btnCancel });
                backupForm.Controls.Add(backupList);
                backupForm.Controls.Add(buttonPanel);

                if (backupForm.ShowDialog() == DialogResult.OK && backupList.SelectedItem != null)
                {
                    BackupInfo selectedBackup = (BackupInfo)backupList.SelectedItem;

                    string siteName;
                    if (!string.IsNullOrEmpty(preselectedSite))
                    {
                        siteName = preselectedSite;
                    }
                    else
                    {
                        DirectoryInfo backupDir = new DirectoryInfo(selectedBackup.Path);
                        siteName = backupDir.Parent.Name;
                    }

                    DialogResult confirm = MessageBox.Show(
                        $"WARNING: Restoring will OVERWRITE current site data!\n\n" +
                        $"Site: {siteName}\n" +
                        $"Backup: {selectedBackup.Timestamp}\n\n" +
                        $"This action cannot be undone.\n\n" +
                        $"Do you want to continue?",
                        "Confirm Restore",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Warning);

                    if (confirm == DialogResult.Yes)
                    {
                        await RestoreFromBackup(siteName, selectedBackup);
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "BtnRestoreBackup_Click");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================
        // DELETE SITE
        // =========================
        private void BtnDeleteSite_Click(object sender, EventArgs e)
        {
            try
            {
                if (listSites.SelectedItem == null)
                {
                    MessageBox.Show("Select a site first!", "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string siteName = listSites.SelectedItem.ToString() ?? "";
                bool isPhpOnly = IsPhpOnlySite(siteName);

                if (MessageBox.Show($"Are you sure you want to delete site '{siteName}'?\n\nThis will permanently delete:\nWebsite files\nDatabase (if exists)\nNginx configuration\nHosts entry\n\nThis action cannot be undone!", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                try
                {
                    Log($"Deleting site: {siteName}");
                    string sitePath = Path.Combine(_wwwPath, siteName);
                    if (Directory.Exists(sitePath)) { Directory.Delete(sitePath, true); Log($"Deleted folder: {sitePath}"); }
                    string confPath = Path.Combine(_sitesPath, $"{siteName}.conf");
                    if (File.Exists(confPath)) { File.Delete(confPath); Log($"Deleted config: {siteName}.conf"); }

                    if (!isPhpOnly && siteName != "php")
                    {
                        string dbName = $"{siteName}_db";
                        try { Process.Start(new ProcessStartInfo { FileName = _mysqlClientPath, Arguments = $"-u root -e \"DROP DATABASE IF EXISTS {dbName}\"", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(5000); Log($"Deleted database: {dbName}"); }
                        catch (Exception ex) { LogError(ex, $"BtnDeleteSite_Click - Drop database {dbName}"); }
                    }

                    try
                    {
                        string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
                        string domain = $"{siteName}.wp";
                        if (File.Exists(hostsPath))
                        {
                            var lines = File.ReadAllLines(hostsPath).Where(line => !line.Contains(domain)).ToArray();
                            File.WriteAllLines(hostsPath, lines);
                            Log($"Removed hosts entry: {domain}");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "BtnDeleteSite_Click - Remove hosts entry");
                    }

                    ReloadNginx();
                    LoadSites();
                    Log($"Site '{siteName}' deleted successfully!");
                    MessageBox.Show($"Site '{siteName}' has been deleted.", "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    LogError(ex, $"BtnDeleteSite_Click - Delete {siteName}");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "BtnDeleteSite_Click");
            }
        }

        // =========================
        // COPY DIRECTORY (ITERATIVE - ОПТИМІЗОВАНИЙ)
        // =========================
        private void CopyDirectoryIterative(string source, string dest)
        {
            try
            {
                Directory.CreateDirectory(dest);

                var stack = new Stack<(string Source, string Dest)>();
                stack.Push((source, dest));

                int fileCount = 0;
                while (stack.Count > 0)
                {
                    var (src, dst) = stack.Pop();
                    Directory.CreateDirectory(dst);

                    foreach (var file in Directory.GetFiles(src))
                    {
                        File.Copy(file, Path.Combine(dst, Path.GetFileName(file)), true);
                        fileCount++;
                        if (fileCount % 100 == 0)
                            LogToFile($"   Copied {fileCount} files...");
                    }

                    foreach (var dir in Directory.GetDirectories(src))
                    {
                        stack.Push((dir, Path.Combine(dst, Path.GetFileName(dir))));
                    }
                }
            }
            catch (Exception ex)
            {
                LogError(ex, $"CopyDirectoryIterative - {source} -> {dest}");
                throw;
            }
        }

        // =========================
        // HELP
        // =========================
        private void BtnHelp_Click(object sender, EventArgs e)
        {
            try
            {
                ShowTextFile("help.txt", "WPronto Help");
            }
            catch (Exception ex)
            {
                LogError(ex, "BtnHelp_Click");
            }
        }

        private void ShowTextFile(string filename, string title)
        {
            try
            {
                string path = Path.Combine(_basePath, filename);
                if (!File.Exists(path)) { MessageBox.Show($"{filename} not found.", "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
                Form f = new Form
                {
                    Text = title,
                    Size = new Size(ScaleInt(700), ScaleInt(520)),
                    StartPosition = FormStartPosition.CenterParent,
                    FormBorderStyle = FormBorderStyle.FixedDialog,
                    MaximizeBox = false,
                    MinimizeBox = false,
                    BackColor = ThemeManager.CurrentScheme.BackgroundCard,
                };
                Panel panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(ScaleInt(20), ScaleInt(15), ScaleInt(20), ScaleInt(15)) };
                RichTextBox txt = new RichTextBox
                {
                    Dock = DockStyle.Fill,
                    ReadOnly = true,
                    BackColor = ThemeManager.CurrentScheme.LogBackground,
                    ForeColor = ThemeManager.CurrentScheme.TextSecondary,
                    Font = new Font("Consolas", 9f, FontStyle.Regular),
                    Text = File.ReadAllText(path),
                    BorderStyle = BorderStyle.None,
                };
                panel.Controls.Add(txt);
                f.Controls.Add(panel);
                f.ShowDialog();
            }
            catch (Exception ex)
            {
                LogError(ex, $"ShowTextFile - {filename}");
            }
        }

        private Task DelayAsync(int milliseconds) => Task.Delay(milliseconds);
    }

    // =========================
    // SOFT BUTTON CLASS
    // =========================
    public class SoftButton : Button
    {
        private bool _isHovered = false, _isPressed = false;
        private readonly ButtonStyle _style;
        private readonly float _dpiScale;
        private readonly Action<AppTheme> _themeHandler;

        public SoftButton(string text, ButtonStyle style = ButtonStyle.Default, float dpiScale = 1.0f)
        {
            this.Text = text;
            _style = style;
            _dpiScale = dpiScale;
            int scale(int v) => (int)(v * dpiScale);
            this.Size = new Size(scale(105), scale(34));
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.Cursor = Cursors.Hand;
            this.Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Bold);
            this.Margin = new Padding(scale(2), 0, scale(2), 0);

            this.MouseEnter += (s, e) => { _isHovered = true; Invalidate(); };
            this.MouseLeave += (s, e) => { _isHovered = false; _isPressed = false; Invalidate(); };
            this.MouseDown += (s, e) => { _isPressed = true; Invalidate(); };
            this.MouseUp += (s, e) => { _isPressed = false; Invalidate(); };

            _themeHandler = (theme) => Invalidate();
            ThemeManager.ThemeChanged += _themeHandler;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ThemeManager.ThemeChanged -= _themeHandler;
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            try
            {
                pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                pevent.Graphics.Clear(this.Parent?.BackColor ?? SystemColors.Control);
                Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
                int r = (int)(8 * _dpiScale);
                var scheme = ThemeManager.CurrentScheme;
                Color bg = scheme.BackgroundCard, border = scheme.BorderColor, text = scheme.TextSecondary;

                if (_style == ButtonStyle.Primary) { bg = _isHovered ? scheme.PrimaryHover : scheme.PrimaryColor; border = bg; text = Color.White; }
                else if (_style == ButtonStyle.Danger) { bg = _isHovered ? Color.FromArgb(200, 35, 51) : scheme.DangerColor; border = bg; text = Color.White; }
                else if (_style == ButtonStyle.Success) { bg = _isHovered ? Color.FromArgb(40, 150, 60) : scheme.SuccessColor; border = bg; text = Color.White; }
                else if (_style == ButtonStyle.Warning) { bg = _isHovered ? Color.FromArgb(255, 120, 0) : scheme.WarningColor; border = bg; text = Color.White; }
                else if (_isHovered) { bg = scheme.SelectionBackground; border = scheme.PrimaryColor; }
                if (_isPressed) bg = scheme.BorderColor;

                using (GraphicsPath path = Form1.CreateRoundedRect(rect, r))
                {
                    using (SolidBrush b = new SolidBrush(Enabled ? bg : scheme.BorderColor))
                        pevent.Graphics.FillPath(b, path);
                    using (Pen p = new Pen(Enabled ? border : scheme.BorderColor, 1.0f))
                        pevent.Graphics.DrawPath(p, path);
                }
                TextRenderer.DrawText(pevent.Graphics, this.Text, this.Font, rect,
                    Enabled ? text : scheme.TextMuted,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
            catch { }
        }
    }

    // =========================
    // MODERN CARD CLASS
    // =========================
    public class ModernCard : Panel
    {
        private readonly float _dpiScale;
        private readonly Action<AppTheme> _themeHandler;

        public ModernCard(int x, int y, int w, int h, float dpiScale = 1.0f)
        {
            _dpiScale = dpiScale;
            this.Location = new Point(x, y);
            this.Size = new Size(w, h);
            this.BackColor = ThemeManager.CurrentScheme.BackgroundCard;
            this.Padding = new Padding(1);

            _themeHandler = (theme) =>
            {
                this.BackColor = ThemeManager.CurrentScheme.BackgroundCard;
                this.Invalidate();
            };
            ThemeManager.ThemeChanged += _themeHandler;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                ThemeManager.ThemeChanged -= _themeHandler;
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                int r = (int)(8 * _dpiScale);
                Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath path = Form1.CreateRoundedRect(rect, r))
                {
                    this.Region = new Region(path);
                    using (Pen p = new Pen(ThemeManager.CurrentScheme.BorderColor, 1.0f))
                        e.Graphics.DrawPath(p, path);
                }
            }
            catch { }
        }
    }
}