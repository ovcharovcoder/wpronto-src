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

namespace WPLaunchGUI
{
    public enum ButtonStyle { Default, Primary, Danger, Success }
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
            try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "theme.config"), theme.ToString()); } catch { }
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
    }

    public class Form1 : Form
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

        private int _webPort = Config.DefaultPort;
        private int _pmaPort = Config.DefaultPort;

        // PHP version
        private string _currentPhpVersion = "8.3";

        // UI ELEMENTS
        private ListBox listSites;
        private RichTextBox txtLog;
        private SoftButton btnStart;
        private SoftButton btnStop;
        private SoftButton btnOpenLocalhost;
        private SoftButton btnPhpMyAdmin;
        private SoftButton btnCreateSite;
        private SoftButton btnBackupSite;
        private SoftButton btnDeleteSite;
        private SoftButton btnLicense;
        private SoftButton btnAbout;
        private Label lblStatus;
        private Label lblTitle;
        private Label lblSubtitle;
        private LinkLabel lnkWebsite;
        private Label btnTheme;
        private ComboBox comboPhpVersion;
        private Label lblPhpStatus;

        private float _dpiScale = 1.0f;
        private int ScaleInt(int value) => (int)(value * _dpiScale);

        public Form1()
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

            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
            if (File.Exists(iconPath))
                this.Icon = new Icon(iconPath);

            // Завантаження збереженої PHP версії
            _currentPhpVersion = LoadPhpVersionSetting();

            InitializePaths();
            EnsureDirectories();
            EnsureConfigFiles();
            CreateProfessionalLayout();

            var savedTheme = ThemeManager.LoadThemeSetting();
            ThemeManager.SetTheme(savedTheme);
            ThemeManager.ThemeChanged += (theme) => ApplyTheme();
            ApplyTheme();

            LoadSites();
            CheckServerStatus();
            ValidateTemplate();
            FindAvailablePorts();

            _statusTimer = new System.Windows.Forms.Timer();
            _statusTimer.Interval = 5000;
            _statusTimer.Tick += (s, e) => CheckServerStatus();
            _statusTimer.Start();
        }

        private string GetCurrentPhpFolder() => $"php{_currentPhpVersion.Replace(".", "")}";

        private void InitializePaths()
        {
            string exePath = AppDomain.CurrentDomain.BaseDirectory;
            _basePath = exePath.TrimEnd('\\');

            string phpVersionFolder = GetCurrentPhpFolder();

            _nginxPath = Path.Combine(_basePath, @"core\nginx\nginx.exe");
            _nginxWorkingDir = Path.Combine(_basePath, @"core\nginx");
            _nginxConfDir = Path.Combine(_basePath, @"core\nginx\conf");
            _nginxConf = Path.Combine(_nginxConfDir, "nginx.conf");

            _phpCgiPath = Path.Combine(_basePath, @"core\php", phpVersionFolder, "php-cgi.exe");
            _phpWorkingDir = Path.Combine(_basePath, @"core\php", phpVersionFolder);
            _phpIni = Path.Combine(_basePath, @"config\php", $"php_{_currentPhpVersion}.ini");

            _mysqlPath = Path.Combine(_basePath, @"core\mysql\bin\mysqld.exe");
            _mysqlClientPath = Path.Combine(_basePath, @"core\mysql\bin\mysql.exe");
            _mysqlData = Path.Combine(_basePath, @"data\mysql");
            _mysqlWorkingDir = Path.Combine(_basePath, @"core\mysql\bin");

            _wwwPath = Path.Combine(_basePath, @"www");
            _sitesPath = Path.Combine(_basePath, @"config\nginx\sites");
            _templatePath = Path.Combine(_basePath, @"template");
            _logsPath = Path.Combine(_basePath, @"logs");
            _tmpPath = Path.Combine(_basePath, @"tmp");
            _pmaPath = Path.Combine(_basePath, @"core\phpmyadmin");
            _backupPath = Path.Combine(_basePath, @"backups");
        }

        private void EnsureDirectories()
        {
            Directory.CreateDirectory(_wwwPath);
            Directory.CreateDirectory(Path.Combine(_wwwPath, "default"));
            Directory.CreateDirectory(_sitesPath);
            Directory.CreateDirectory(_logsPath);
            Directory.CreateDirectory(_tmpPath);
            Directory.CreateDirectory(_nginxConfDir);
            Directory.CreateDirectory(Path.Combine(_basePath, @"config\php"));
            Directory.CreateDirectory(Path.Combine(_basePath, @"config\nginx"));
            Directory.CreateDirectory(_mysqlData);
            Directory.CreateDirectory(_backupPath);
        }

        private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

        private void EnsurePhpConfigs()
        {
            var versions = new[] { "8.3", "8.5" };
            string basePathUnix = _basePath.Replace("\\", "/");

            foreach (var version in versions)
            {
                string phpIniPath = Path.Combine(_basePath, @"config\php", $"php_{version}.ini");
                string versionFolder = $"php{version.Replace(".", "")}";

                if (!File.Exists(phpIniPath))
                {
                    string phpIniContent = $@"[PHP]
extension_dir = ""{basePathUnix}/core/php/{versionFolder}/ext""
date.timezone = ""{Config.TimeZone}""
display_errors = Off
error_log = ""{basePathUnix}/logs/php_error_{version}.log""

upload_max_filesize = {Config.UploadMaxSize}M
post_max_size = {Config.UploadMaxSize}M
memory_limit = {Config.MemoryLimit}M
max_execution_time = 300
max_input_time = 300

extension=curl
extension=gd
extension=mbstring
extension=mysqli
extension=openssl
extension=pdo_mysql
extension=zip

cgi.fix_pathinfo=1
";
                    File.WriteAllText(phpIniPath, phpIniContent, Utf8NoBom);
                }
            }
        }

        private void EnsureConfigFiles()
        {
            string basePathUnix = _basePath.Replace("\\", "/");

            EnsurePhpConfigs();

            // mime.types
            string mimePath = Path.Combine(_nginxConfDir, "mime.types");
            if (!File.Exists(mimePath))
            {
                File.WriteAllText(mimePath, "types {\n    text/html html htm;\n    text/css css;\n    text/xml xml;\n" +
                    "    image/gif gif;\n    image/jpeg jpeg jpg;\n    application/javascript js;\n" +
                    "    text/plain txt;\n    image/png png;\n    image/svg+xml svg;\n    image/x-icon ico;\n" +
                    "    application/zip zip;\n}\n", Utf8NoBom);
            }

            // fastcgi_params
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

            // NGINX.CONF
            string nginxConfig =
                "worker_processes  1;\n" +
                "events { worker_connections 1024; }\n\n" +
                "http {\n" +
                "    include       mime.types;\n" +
                "    default_type  application/octet-stream;\n" +
                "    sendfile      on;\n" +
                "    keepalive_timeout 65;\n" +
                $"    client_max_body_size {Config.UploadMaxSize}M;\n\n" +
                $"    access_log \"{basePathUnix}/logs/nginx_access.log\";\n" +
                $"    error_log  \"{basePathUnix}/logs/nginx_error.log\";\n\n" +
                "    server {\n" +
                $"        listen       {_webPort};\n" +
                "        server_name  localhost;\n" +
                $"        root         \"{basePathUnix}/www/default\";\n" +
                "        index        index.php index.html;\n\n" +
                "        location / {\n" +
                "            try_files $uri $uri/ =404;\n" +
                "        }\n\n" +
                "        location /phpmyadmin {\n" +
                $"            root {basePathUnix}/core;\n" +
                "            index index.php;\n\n" +
                "            location ~ ^/phpmyadmin/(.+)\\.php$ {\n" +
                "                try_files $uri =404;\n" +
                "                fastcgi_pass 127.0.0.1:9000;\n" +
                "                fastcgi_index index.php;\n" +
                "                fastcgi_param SCRIPT_FILENAME $document_root$fastcgi_script_name;\n" +
                "                include fastcgi_params;\n" +
                "            }\n\n" +
                "            location ~* ^/phpmyadmin/(.+)\\.(jpg|jpeg|gif|css|png|js|ico|html|xml|txt)$ {\n" +
                "                expires max;\n" +
                "            }\n" +
                "        }\n\n" +
                "        location ~ \\.php$ {\n" +
                "            try_files $uri =404;\n" +
                "            fastcgi_pass 127.0.0.1:9000;\n" +
                "            fastcgi_param SCRIPT_FILENAME $document_root$fastcgi_script_name;\n" +
                "            include fastcgi_params;\n" +
                "        }\n" +
                "    }\n\n" +
                $"    include \"{basePathUnix}/config/nginx/sites/*.conf\";\n" +
                "}\n";

            File.WriteAllText(_nginxConf, nginxConfig, Encoding.ASCII);

            // .user.ini для сайтів
            string userIniContent = $"upload_max_filesize = {Config.UploadMaxSize}M\npost_max_size = {Config.UploadMaxSize}M\nmemory_limit = {Config.MemoryLimit}M\nmax_execution_time = 300\nmax_input_time = 300";

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

            // phpMyAdmin
            if (!Directory.Exists(_pmaPath))
                Directory.CreateDirectory(_pmaPath);

            string testIndex = Path.Combine(_pmaPath, "index.php");
            if (!File.Exists(testIndex))
            {
                string phpContent = "<?php echo '<h1>phpMyAdmin</h1><p>Please download phpMyAdmin from <a href=\"https://www.phpmyadmin.net/\" target=\"_blank\">www.phpmyadmin.net</a></p><p>Extract files to: " + _pmaPath + "</p>'; ?>";
                File.WriteAllText(testIndex, phpContent, Encoding.UTF8);
            }

            // info.php
            string infoFile = Path.Combine(_wwwPath, "default", "info.php");
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

        private void ApplyTheme()
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

        private void CreateProfessionalLayout()
        {
            var scheme = ThemeManager.CurrentScheme;

            // HEADER з версією v 2.0
            lblTitle = new Label
            {
                Text = "WPronto v2.0",
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

            // Кнопка теми
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

            // PHP VERSION SELECTOR
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
                                Log($"🔄 Switching PHP from {_currentPhpVersion} to {newVersion}...");
                                BtnStop_Click(null, null);

                                _currentPhpVersion = newVersion;
                                SavePhpVersionSetting(newVersion);
                                InitializePaths();

                                await DelayAsync(1000);
                                BtnStart_Click(null, null);

                                lblPhpStatus.Text = $"Active: {newVersion}";
                                Log($"✅ PHP switched to {newVersion}");
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
                            lblPhpStatus.Text = $"Active: {newVersion} (next start)";
                            Log($"PHP version set to {newVersion} (will be used on next start)");
                        }
                    }
                }
            };

            // TOP BUTTONS PANEL
            FlowLayoutPanel topButtonsPanel = new FlowLayoutPanel
            {
                Location = new Point(ScaleInt(317), ScaleInt(105)),
                Size = new Size(ScaleInt(540), ScaleInt(50)),
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false
            };

            btnStart = new SoftButton("Start (Ctrl+S)", ButtonStyle.Primary, _dpiScale);
            btnStart.Margin = new Padding(ScaleInt(2), 0, ScaleInt(18), 0);
            btnStart.Click += BtnStart_Click;

            btnStop = new SoftButton("Stop (Ctrl+X)", ButtonStyle.Danger, _dpiScale);
            btnStop.Margin = new Padding(ScaleInt(2), 0, ScaleInt(18), 0);
            btnStop.Click += BtnStop_Click;

            btnOpenLocalhost = new SoftButton("Open Admin", ButtonStyle.Primary, _dpiScale);
            btnOpenLocalhost.Margin = new Padding(ScaleInt(2), 0, ScaleInt(18), 0);
            btnOpenLocalhost.Click += BtnOpenAdmin_Click;

            btnPhpMyAdmin = new SoftButton("phpMyAdmin", ButtonStyle.Default, _dpiScale);
            btnPhpMyAdmin.Margin = new Padding(ScaleInt(2), 0, 0, 0);
            btnPhpMyAdmin.Click += BtnPhpMyAdmin_Click;

            topButtonsPanel.Controls.AddRange(new Control[] { btnStart, btnStop, btnOpenLocalhost, btnPhpMyAdmin });

            // SITES LABEL
            Label lblSitesTitle = new Label
            {
                Text = "SITES",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = scheme.TextMuted,
                Location = new Point(ScaleInt(32), ScaleInt(155)),
                AutoSize = true
            };

            // SITES CARD
            ModernCard cardSites = new ModernCard(ScaleInt(32), ScaleInt(175), ScaleInt(260), ScaleInt(285), _dpiScale);
            listSites = new ListBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ItemHeight = ScaleInt(32),
                DrawMode = DrawMode.OwnerDrawFixed,
                BackColor = scheme.BackgroundCard
            };
            listSites.DrawItem += ListSites_DrawItem;
            cardSites.Controls.Add(listSites);

            // LOG LABEL
            Label lblLogsTitle = new Label
            {
                Text = "SERVER LOG",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = scheme.TextMuted,
                Location = new Point(ScaleInt(315), ScaleInt(155)),
                AutoSize = true
            };

            // LOG CARD
            ModernCard cardLogs = new ModernCard(ScaleInt(315), ScaleInt(175), ScaleInt(485), ScaleInt(285), _dpiScale);
            txtLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.None,
                ReadOnly = true,
                BackColor = scheme.LogBackground,
                ForeColor = scheme.TextSecondary,
                Font = new Font("Consolas", 9f, FontStyle.Regular)
            };
            cardLogs.Controls.Add(txtLog);

            // FOOTER BUTTONS
            btnCreateSite = new SoftButton("Create Site", ButtonStyle.Primary, _dpiScale);
            btnCreateSite.Location = new Point(ScaleInt(32), ScaleInt(480));
            btnCreateSite.Width = ScaleInt(110);
            btnCreateSite.Click += BtnCreateSite_Click;

            btnBackupSite = new SoftButton("Backup Site", ButtonStyle.Success, _dpiScale);
            btnBackupSite.Location = new Point(ScaleInt(152), ScaleInt(480));
            btnBackupSite.Width = ScaleInt(110);
            btnBackupSite.Click += BtnBackupSite_Click;

            btnDeleteSite = new SoftButton("Delete Site", ButtonStyle.Danger, _dpiScale);
            btnDeleteSite.Location = new Point(ScaleInt(272), ScaleInt(480));
            btnDeleteSite.Width = ScaleInt(110);
            btnDeleteSite.Click += BtnDeleteSite_Click;

            btnLicense = new SoftButton("License", ButtonStyle.Default, _dpiScale);
            btnLicense.Location = new Point(ScaleInt(392), ScaleInt(480));
            btnLicense.Width = ScaleInt(90);
            btnLicense.Click += BtnLicense_Click;

            btnAbout = new SoftButton("About", ButtonStyle.Default, _dpiScale);
            btnAbout.Location = new Point(ScaleInt(492), ScaleInt(480));
            btnAbout.Width = ScaleInt(90);
            btnAbout.Click += BtnAbout_Click;

            // WEBSITE LINK
            lnkWebsite = new LinkLabel
            {
                Text = "Official Website",
                Location = new Point(ScaleInt(656), ScaleInt(480)),
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
                Location = new Point(ScaleInt(656), ScaleInt(500)),
                AutoSize = true,
                ForeColor = scheme.TextMuted,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };

            // Додаємо всі елементи (лейбл версії БІЛЬШЕ НЕ ДОДАЄМО)
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
            this.Controls.Add(btnDeleteSite);
            this.Controls.Add(btnLicense);
            this.Controls.Add(btnAbout);
            this.Controls.Add(lnkWebsite);
            this.Controls.Add(lblDev);
        }

        private void BtnTheme_Click(object sender, EventArgs e)
        {
            var currentTheme = ThemeManager.CurrentTheme;
            AppTheme newTheme = currentTheme == AppTheme.Light ? AppTheme.Dark : (currentTheme == AppTheme.Dark ? AppTheme.System : AppTheme.Light);
            ThemeManager.SetTheme(newTheme);
            Log($"🎨 Theme changed to: {newTheme}");
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S)) { BtnStart_Click(this, EventArgs.Empty); return true; }
            if (keyData == (Keys.Control | Keys.X)) { BtnStop_Click(this, EventArgs.Empty); return true; }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // =========================
        // HELPER METHODS FOR PHP VERSION
        // =========================
        private string LoadPhpVersionSetting()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "phpversion.config");
                if (File.Exists(configPath))
                {
                    string version = File.ReadAllText(configPath);
                    if (version == "8.3" || version == "8.5")
                        return version;
                }
            }
            catch { }
            return "8.3";
        }

        private void SavePhpVersionSetting(string version)
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "phpversion.config");
                File.WriteAllText(configPath, version);
            }
            catch { }
        }

        // =========================
        // SOFT BUTTON CLASS
        // =========================
        public class SoftButton : Button
        {
            private bool _isHovered = false, _isPressed = false;
            private ButtonStyle _style;
            private float _dpiScale;

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
                ThemeManager.ThemeChanged += (theme) => Invalidate();
            }

            protected override void OnPaint(PaintEventArgs pevent)
            {
                pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                pevent.Graphics.Clear(this.Parent.BackColor);
                Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
                int r = (int)(8 * _dpiScale);
                var scheme = ThemeManager.CurrentScheme;
                Color bg = scheme.BackgroundCard, border = scheme.BorderColor, text = scheme.TextSecondary;

                if (_style == ButtonStyle.Primary) { bg = _isHovered ? scheme.PrimaryHover : scheme.PrimaryColor; border = bg; text = Color.White; }
                else if (_style == ButtonStyle.Danger) { bg = _isHovered ? Color.FromArgb(200, 35, 51) : scheme.DangerColor; border = bg; text = Color.White; }
                else if (_style == ButtonStyle.Success) { bg = _isHovered ? Color.FromArgb(40, 150, 60) : scheme.SuccessColor; border = bg; text = Color.White; }
                else if (_isHovered) { bg = scheme.SelectionBackground; border = scheme.PrimaryColor; }
                if (_isPressed) bg = scheme.BorderColor;

                using (GraphicsPath path = CreateRoundedRect(rect, r))
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
        }

        // =========================
        // MODERN CARD CLASS
        // =========================
        public class ModernCard : Panel
        {
            private float _dpiScale;
            public ModernCard(int x, int y, int w, int h, float dpiScale = 1.0f)
            {
                this._dpiScale = dpiScale;
                this.Location = new Point(x, y);
                this.Size = new Size(w, h);
                this.BackColor = ThemeManager.CurrentScheme.BackgroundCard;
                this.Padding = new Padding(1);
                ThemeManager.ThemeChanged += (theme) => { this.BackColor = ThemeManager.CurrentScheme.BackgroundCard; this.Invalidate(); };
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                int r = (int)(8 * _dpiScale);
                Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath path = CreateRoundedRect(rect, r))
                {
                    this.Region = new Region(path);
                    using (Pen p = new Pen(ThemeManager.CurrentScheme.BorderColor, 1.0f))
                        e.Graphics.DrawPath(p, path);
                }
            }
        }

        private void ListSites_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            bool isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            string text = listSites.Items[e.Index]?.ToString() ?? "";
            var scheme = ThemeManager.CurrentScheme;
            e.Graphics.FillRectangle(new SolidBrush(scheme.BackgroundCard), e.Bounds);
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
            TextRenderer.DrawText(e.Graphics, text, listSites.Font, e.Bounds,
                isSelected ? scheme.PrimaryColor : scheme.TextSecondary,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.LeftAndRightPadding);
        }

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
        // APPLICATION LOGIC
        // =========================

        private bool CheckRequiredFiles()
        {
            string[] required = { _nginxPath, _phpCgiPath, _mysqlPath };
            bool allExist = true;
            foreach (var file in required)
            {
                if (!File.Exists(file))
                {
                    Log($"❌ Missing: {file}");
                    allExist = false;
                }
            }
            return allExist;
        }

        private bool IsPortAvailable(int port)
        {
            try
            {
                using (var client = new System.Net.Sockets.TcpClient())
                {
                    var result = client.BeginConnect("127.0.0.1", port, null, null);
                    var success = result.AsyncWaitHandle.WaitOne(500);
                    if (success) { client.EndConnect(result); return false; }
                }
            }
            catch { }
            return true;
        }

        private void FindAvailablePorts()
        {
            _webPort = Config.DefaultPort;
            _pmaPort = Config.DefaultPort;
            if (!IsPortAvailable(Config.DefaultPort))
            {
                _webPort = Config.AlternativePort;
                _pmaPort = Config.AlternativePort + 1;
                Log($"⚠️ Port {Config.DefaultPort} is busy, using alternative ports: {_webPort} and {_pmaPort}");
            }
        }

        private void LogToFile(string message)
        {
            try
            {
                string logFile = Path.Combine(_logsPath, "wplaunch.log");
                File.AppendAllText(logFile, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}{Environment.NewLine}");
            }
            catch { }
        }

        private string GetNginxVersion()
        {
            try
            {
                var process = Process.Start(new ProcessStartInfo { FileName = _nginxPath, Arguments = "-v", UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true });
                return process?.StandardError.ReadToEnd() ?? "Unknown";
            }
            catch { return "Unknown"; }
        }

        private bool IsValidWordPressInstall(string path) => File.Exists(Path.Combine(path, "wp-admin", "index.php")) && File.Exists(Path.Combine(path, "wp-includes", "version.php"));

        private void ValidateTemplate()
        {
            if (!Directory.Exists(_templatePath)) return;
            if (!IsValidWordPressInstall(_templatePath)) Log("⚠️ Warning: Template folder doesn't look like a valid WordPress installation");
        }

        private void BackupDatabase(string dbName, string backupDir)
        {
            try
            {
                string mysqldumpPath = Path.Combine(_basePath, @"core\mysql\bin\mysqldump.exe");
                if (File.Exists(mysqldumpPath))
                {
                    string backupFile = Path.Combine(backupDir, $"{dbName}.sql");
                    var process = Process.Start(new ProcessStartInfo { FileName = mysqldumpPath, Arguments = $"-u root --databases {dbName} --result-file=\"{backupFile}\"", UseShellExecute = false, CreateNoWindow = true, RedirectStandardError = true });
                    if (process != null)
                    {
                        process.WaitForExit(10000);
                        if (process.ExitCode == 0 && File.Exists(backupFile) && new FileInfo(backupFile).Length > 0) Log($"   ✓ Database backup: {backupFile}");
                        else Log($"   ⚠ Database backup failed: {process.StandardError.ReadToEnd()}");
                    }
                }
                else Log($"   ⚠ mysqldump not found, database backup skipped");
            }
            catch (Exception ex) { Log($"   ⚠ Database backup error: {ex.Message}"); }
        }

        private void BackupSiteFiles(string sitePath, string backupDir)
        {
            try
            {
                string filesBackupDir = Path.Combine(backupDir, "files");
                CopyDirectory(sitePath, filesBackupDir);
                Log($"   ✓ Files backup: {filesBackupDir}");
            }
            catch (Exception ex) { Log($"   ⚠ Files backup failed: {ex.Message}"); }
        }

        private void CreateFullBackup(string siteName)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string backupDir = Path.Combine(_backupPath, siteName, timestamp);
                Directory.CreateDirectory(backupDir);
                Log($"📦 Creating backup for '{siteName}'...");
                string sitePath = Path.Combine(_wwwPath, siteName);
                if (Directory.Exists(sitePath)) BackupSiteFiles(sitePath, backupDir);
                else Log($"   ⚠ Site folder not found: {sitePath}");
                string dbName = $"{siteName}_db";
                BackupDatabase(dbName, backupDir);
                Log($"✅ Backup completed successfully! Location: {backupDir}");
                MessageBox.Show($"Site '{siteName}' has been backed up successfully!\n\nBackup location:\n{backupDir}", "Backup Completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Log($"❌ Backup failed: {ex.Message}");
                MessageBox.Show($"Backup failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task DelayAsync(int milliseconds) => await Task.Delay(milliseconds);

        private void CheckServerStatus()
        {
            bool running = IsProcessRunning("nginx") && IsProcessRunning("php-cgi") && IsProcessRunning("mysqld");
            if (this.InvokeRequired) { this.Invoke(new Action(CheckServerStatus)); return; }
            var scheme = ThemeManager.CurrentScheme;
            if (running)
            {
                lblStatus.Text = "● SERVER RUNNING";
                lblStatus.ForeColor = scheme.StatusRunning;
                btnStart.Enabled = false;
                btnStop.Enabled = true;
            }
            else
            {
                lblStatus.Text = "● SERVER STOPPED";
                lblStatus.ForeColor = scheme.StatusStopped;
                btnStart.Enabled = true;
                btnStop.Enabled = false;
            }
        }

        private bool IsProcessRunning(string name) => Process.GetProcessesByName(name).Length > 0;

        private void LoadSites()
        {
            listSites.Items.Clear();
            if (!Directory.Exists(_wwwPath)) return;
            foreach (string dir in Directory.GetDirectories(_wwwPath))
            {
                string name = Path.GetFileName(dir);
                if (name != "default") listSites.Items.Add(name);
            }
            if (listSites.Items.Count > 0) listSites.SelectedIndex = 0;
        }

        private void Log(string message)
        {
            if (txtLog.InvokeRequired) { txtLog.Invoke(new Action(() => Log(message))); return; }
            string logMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
            txtLog.AppendText(logMessage + Environment.NewLine);
            txtLog.ScrollToCaret();
            LogToFile(message);
        }

        private async void BtnStart_Click(object sender, EventArgs e)
        {
            try
            {
                btnStart.Enabled = false;
                BtnStop_Click(sender, e);
                await DelayAsync(800);
                if (!CheckRequiredFiles())
                {
                    Log("❌ Required files missing. Please check installation.");
                    return;
                }

                Log($"Starting services with PHP {_currentPhpVersion}...");
                Log($"Nginx version: {GetNginxVersion()}");
                Log($"Using port: {_webPort}");

                string mysqlDataUnix = _mysqlData.Replace("\\", "/");
                if (!Directory.Exists(Path.Combine(_mysqlData, "mysql")))
                {
                    Log("Initializing MySQL...");
                    var initProcess = Process.Start(new ProcessStartInfo { FileName = _mysqlPath, Arguments = $"--initialize-insecure --datadir=\"{mysqlDataUnix}\"", UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = _mysqlWorkingDir });
                    if (initProcess != null) initProcess.WaitForExit(10000);
                    Log("✓ MySQL initialized");
                }

                Log("Starting MySQL...");
                Process.Start(new ProcessStartInfo { FileName = _mysqlPath, Arguments = $"--datadir=\"{mysqlDataUnix}\" --bind-address=127.0.0.1 --port={Config.MysqlPort}", UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = _mysqlWorkingDir });
                await DelayAsync(Config.ProcessStartDelay);

                Log($"Starting PHP-CGI {_currentPhpVersion}...");
                Process.Start(new ProcessStartInfo { FileName = _phpCgiPath, Arguments = $"-b 127.0.0.1:{Config.PhpPort} -c \"{_phpIni}\"", UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = _phpWorkingDir });
                await DelayAsync(Config.ProcessStartDelay);

                Log("Starting Nginx...");
                Process.Start(new ProcessStartInfo { FileName = _nginxPath, Arguments = $"-c \"{_nginxConf}\"", UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = _nginxWorkingDir });
                await DelayAsync(Config.NginxStartDelay);
                CheckServerStatus();

                Log($"✅ Server is running with PHP {_currentPhpVersion}!");
                Log($"   http://localhost:{(_webPort == 80 ? "" : _webPort.ToString())}/ - WordPress");
                Log($"   http://localhost:{(_pmaPort == 80 ? "" : _pmaPort.ToString())}/phpmyadmin - phpMyAdmin");
                Log($"   Max upload size: {Config.UploadMaxSize}MB");
            }
            catch (Exception ex) { Log($"❌ Start error: {ex.Message}"); }
            finally { btnStart.Enabled = true; }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            Log("Stopping services...");
            foreach (var name in new[] { "nginx", "php-cgi", "mysqld" })
                foreach (var p in Process.GetProcessesByName(name))
                    try { p.Kill(); p.WaitForExit(2000); Log($"✓ Stopped {name}"); } catch { }
            Log("⏹️ All services stopped");
            CheckServerStatus();
        }

        private void BtnOpenAdmin_Click(object sender, EventArgs e)
        {
            if (listSites.SelectedItem == null) { MessageBox.Show("Select a site first!", "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            string siteName = listSites.SelectedItem.ToString() ?? "";
            string port = _webPort == 80 ? "" : $":{_webPort}";
            string url = $"http://{siteName}.wp{port}/wp-admin";
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            Log($"Opened admin: {url}");
        }

        private void BtnPhpMyAdmin_Click(object sender, EventArgs e)
        {
            string port = _pmaPort == 80 ? "" : $":{_pmaPort}";
            string url = $"http://localhost{port}/phpmyadmin";
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
            Log($"Opening phpMyAdmin: {url}");
        }

        private void BtnCreateSite_Click(object sender, EventArgs e)
        {
            string siteName = Microsoft.VisualBasic.Interaction.InputBox("Enter site name (letters, numbers, hyphens):", "Create New Site", "mynewsite");
            if (string.IsNullOrWhiteSpace(siteName)) return;
            siteName = Regex.Replace(siteName, @"[^a-zA-Z0-9\-]", "").ToLower();
            string sitePath = Path.Combine(_wwwPath, siteName);
            if (Directory.Exists(sitePath)) { MessageBox.Show("Site already exists!", "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
            if (!Directory.Exists(_templatePath)) { MessageBox.Show("WordPress template not found.\n\nPlease add WordPress files to the 'template' folder.", "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

            try
            {
                Log($"Creating site: {siteName}");
                Directory.CreateDirectory(sitePath);
                CopyDirectory(_templatePath, sitePath);
                string userIniPath = Path.Combine(sitePath, ".user.ini");
                string userIniContent = $"upload_max_filesize = {Config.UploadMaxSize}M\npost_max_size = {Config.UploadMaxSize}M\nmemory_limit = {Config.MemoryLimit}M\nmax_execution_time = 300\nmax_input_time = 300";
                File.WriteAllText(userIniPath, userIniContent, Encoding.ASCII);
                Log($"   ✓ Created .user.ini ({Config.UploadMaxSize}MB limit)");

                string dbName = $"{siteName}_db";
                string rootUnix = sitePath.Replace("\\", "/");
                string nginxConfig = "server {\n" + $"    listen {_webPort};\n" + $"    server_name {siteName}.wp;\n" + $"    root \"{rootUnix}\";\n    index index.php;\n\n" + $"    client_max_body_size {Config.UploadMaxSize}M;\n\n" + "    location / { try_files $uri $uri/ /index.php?$args; }\n\n" + "    location ~ /\\. { deny all; }\n\n" + "    location ~ \\.php$ {\n        try_files     $uri =404;\n" + "        fastcgi_pass  127.0.0.1:9000;\n" + "        fastcgi_param SCRIPT_FILENAME $document_root$fastcgi_script_name;\n" + "        include       fastcgi_params;\n    }\n}\n";
                File.WriteAllText(Path.Combine(_sitesPath, $"{siteName}.conf"), nginxConfig, Utf8NoBom);
                AddHostEntry($"{siteName}.wp");
                CreateDatabase(dbName);
                CreateWpConfig(sitePath, dbName, siteName);
                ReloadNginx();
                LoadSites();

                string port = _webPort == 80 ? "" : $":{_webPort}";
                string siteUrl = $"http://{siteName}.wp{port}";
                Log($"✅ Site created successfully!");
                Log($"   URL: {siteUrl}");
                Log($"   Admin: {siteUrl}/wp-admin");
                Log($"   Database: {dbName} (user: root, no password)");
                if (MessageBox.Show($"Site '{siteName}' created!\n\nOpen WordPress install page?", "WPronto", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    Process.Start(new ProcessStartInfo { FileName = $"{siteUrl}/wp-admin/install.php", UseShellExecute = true });
            }
            catch (Exception ex) { Log($"❌ Error: {ex.Message}"); }
        }

        private void BtnBackupSite_Click(object sender, EventArgs e)
        {
            if (listSites.SelectedItem == null) { MessageBox.Show("Select a site first!", "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            string siteName = listSites.SelectedItem.ToString();
            if (MessageBox.Show($"Create a backup of site '{siteName}'?\n\nThis will create a copy of:\n✓ Website files\n✓ Database\n\nThe site will remain active.", "Confirm Backup", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                CreateFullBackup(siteName);
        }

        private void BtnDeleteSite_Click(object sender, EventArgs e)
        {
            if (listSites.SelectedItem == null) { MessageBox.Show("Select a site first!", "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            string siteName = listSites.SelectedItem.ToString();
            if (MessageBox.Show($"Are you sure you want to delete site '{siteName}'?\n\nThis will permanently delete:\n✓ Website files\n✓ Database\n✓ Nginx configuration\n✓ Hosts entry\n\nThis action cannot be undone!", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

            try
            {
                Log($"Deleting site: {siteName}");
                string sitePath = Path.Combine(_wwwPath, siteName);
                if (Directory.Exists(sitePath)) { Directory.Delete(sitePath, true); Log($"✓ Deleted folder: {sitePath}"); }
                string confPath = Path.Combine(_sitesPath, $"{siteName}.conf");
                if (File.Exists(confPath)) { File.Delete(confPath); Log($"✓ Deleted config: {siteName}.conf"); }
                string dbName = $"{siteName}_db";
                try { Process.Start(new ProcessStartInfo { FileName = _mysqlClientPath, Arguments = $"-u root -e \"DROP DATABASE IF EXISTS {dbName}\"", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(5000); Log($"✓ Deleted database: {dbName}"); } catch (Exception ex) { Log($"⚠ Could not delete database: {ex.Message}"); }
                try
                {
                    string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
                    string domain = $"{siteName}.wp";
                    if (File.Exists(hostsPath))
                    {
                        var lines = File.ReadAllLines(hostsPath).Where(line => !line.Contains(domain)).ToArray();
                        File.WriteAllLines(hostsPath, lines);
                        Log($"✓ Removed hosts entry: {domain}");
                    }
                }
                catch { Log("⚠ Could not remove hosts entry (run as admin)"); }
                ReloadNginx();
                LoadSites();
                Log($"✅ Site '{siteName}' deleted successfully!");
                MessageBox.Show($"Site '{siteName}' has been deleted.", "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex) { Log($"❌ Error deleting site: {ex.Message}"); MessageBox.Show($"Error deleting site: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        private void CreateDatabase(string dbName)
        {
            try
            {
                Log($"Creating database: {dbName}");
                Process.Start(new ProcessStartInfo { FileName = _mysqlClientPath, Arguments = $"-u root -e \"CREATE DATABASE IF NOT EXISTS {dbName};\"", UseShellExecute = false, CreateNoWindow = true })?.WaitForExit(5000);
                Log($"✓ Created database: {dbName}");
            }
            catch (Exception ex) { Log($"⚠ Database error: {ex.Message}"); }
        }

        private void CreateWpConfig(string sitePath, string dbName, string siteName)
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
                "define( 'AUTH_KEY',         '" + GenerateRandomKey(64) + "' );\n" +
                "define( 'SECURE_AUTH_KEY',  '" + GenerateRandomKey(64) + "' );\n" +
                "define( 'LOGGED_IN_KEY',    '" + GenerateRandomKey(64) + "' );\n" +
                "define( 'NONCE_KEY',        '" + GenerateRandomKey(64) + "' );\n" +
                "define( 'AUTH_SALT',        '" + GenerateRandomKey(64) + "' );\n" +
                "define( 'SECURE_AUTH_SALT', '" + GenerateRandomKey(64) + "' );\n" +
                "define( 'LOGGED_IN_SALT',   '" + GenerateRandomKey(64) + "' );\n" +
                "define( 'NONCE_SALT',       '" + GenerateRandomKey(64) + "' );\n\n" +
                "$table_prefix = 'wp_';\n" +
                "define( 'WP_DEBUG', false );\n" +
                "define( 'WP_DEBUG_DISPLAY', false );\n" +
                "define( 'WP_MEMORY_LIMIT', '" + Config.MemoryLimit + "M' );\n" +
                "define( 'WP_MAX_MEMORY_LIMIT', '" + Config.MemoryLimit + "M' );\n\n" +
                "if ( ! defined( 'ABSPATH' ) ) define( 'ABSPATH', __DIR__ . '/' );\n" +
                "require_once ABSPATH . 'wp-settings.php';";
            File.WriteAllText(Path.Combine(sitePath, "wp-config.php"), content, new UTF8Encoding(false));
            Log($"✓ Created wp-config.php for {siteName}");
        }

        private string GenerateRandomKey(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*()_-+=<>?";
            var random = new Random();
            return new string(Enumerable.Range(0, length).Select(_ => chars[random.Next(chars.Length)]).ToArray());
        }

        private void ReloadNginx()
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = _nginxPath, Arguments = "-s reload", UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = _nginxWorkingDir });
                Log("✓ Nginx reloaded");
            }
            catch (Exception ex) { Log($"⚠ Reload error: {ex.Message}"); }
        }

        private void AddHostEntry(string domain)
        {
            try
            {
                string hostsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "drivers", "etc", "hosts");
                string entry = $"127.0.0.1 {domain}";
                if (!File.ReadAllText(hostsPath).Contains(entry))
                {
                    File.AppendAllText(hostsPath, Environment.NewLine + entry);
                    Log($"✓ Hosts updated: {domain}");
                }
            }
            catch { Log("⚠ Run as Admin for hosts file access!"); }
        }

        private void CopyDirectory(string source, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (var f in Directory.GetFiles(source))
                File.Copy(f, Path.Combine(dest, Path.GetFileName(f)), true);
            foreach (var d in Directory.GetDirectories(source))
                CopyDirectory(d, Path.Combine(dest, Path.GetFileName(d)));
        }

        private void BtnLicense_Click(object sender, EventArgs e) => ShowTextFile("license.txt", "WPronto License (MIT)");
        private void BtnAbout_Click(object sender, EventArgs e) => ShowTextFile("about.txt", "About WPronto");

        private void ShowTextFile(string filename, string title)
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
    }
}
