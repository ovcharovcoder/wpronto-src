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
using System.Net.Http;
using System.Text.Json;

namespace WPLaunchGUI
{
    public enum ButtonStyle { Default, Primary, Danger, Success, Warning }
    public enum AppTheme { Light, Dark, System }

    // =========================
    // LOCALIZATION MANAGER
    // =========================
    public static class LocalizationManager
    {
        public enum Language { English, Ukrainian }

        private static Language _currentLanguage = Language.English;
        public static event Action<Language> LanguageChanged;

        public static Language CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    SaveLanguageSetting(value);
                    LanguageChanged?.Invoke(value);
                }
            }
        }

        public static void Initialize()
        {
            _currentLanguage = LoadLanguageSetting();
        }

        private static Language LoadLanguageSetting()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language.config");
                if (File.Exists(configPath))
                {
                    string lang = File.ReadAllText(configPath).Trim();
                    if (lang == "uk" || lang == "Ukrainian")
                        return Language.Ukrainian;
                }
            }
            catch { }
            return Language.English;
        }

        private static void SaveLanguageSetting(Language lang)
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "language.config");
                File.WriteAllText(configPath, lang == Language.Ukrainian ? "uk" : "en");
            }
            catch { }
        }

        public static string GetString(string key)
        {
            return CurrentLanguage == Language.Ukrainian
                ? UkrainianStrings.Get(key)
                : EnglishStrings.Get(key);
        }

        public static string FormatString(string key, params object[] args)
        {
            string template = GetString(key);
            return string.Format(template, args);
        }
    }

    // =========================
    // ENGLISH STRINGS
    // =========================
    public static class EnglishStrings
    {
        private static readonly Dictionary<string, string> _strings = new()
        {
            // Main Window
            { "AppTitle", "WPronto v5.0" },
            { "AppSubtitle", "Local WordPress Development Environment" },
            { "ServerRunning", "● SERVER RUNNING" },
            { "ServerStopped", "● SERVER STOPPED" },
            { "ShareOpen", "🔓 Share: Open" },
            { "ShareStarting", "⏳ Share: Starting..." },
            { "ShareClosed", "🔒 Share: Closed" },
            { "Sites", "SITES" },
            { "ServerLog", "SERVER LOG" },
            { "PHP", "PHP" },
            { "ActivePHP", "Active: {0}" },

            // Buttons - БЕЗ ІКОНОК
            { "Start", "Start" },
            { "Restart", "Restart" },
            { "Stop", "Stop" },
            { "Create", "Create" },
            { "Backup", "Backup" },
            { "Restore", "Restore" },
            { "Delete", "Delete" },
            { "Help", "Help" },

            // Context menu - З ІКОНКАМИ
            { "OpenAdmin", "🌐 Open Admin" },
            { "OpenProject", "📁 Open Project" },
            { "OpenBackup", "📁 Open Backup" },
            { "MenuBackup", "💾 Backup" },   
            { "MenuRestore", "⏪ Restore" }, 
            { "MenuDelete", "🗑️ Delete" },
            { "Share", "🔗 Share" },
            { "Refresh", "🔄 Refresh" },
            { "CloseShare", "⛓️‍💥 Close" },

            // Messages
            { "ServerAlreadyRunning", "Server is already running!" },
            { "ServerStartFailed", "Failed to start server!" },
            { "SelectSiteFirst", "Select a site first!" },
            { "SiteExists", "Site already exists!" },
            { "SiteCreated", "Site created successfully!" },
            { "SiteDeleted", "Site deleted successfully!" },
            { "BackupCreated", "Backup created successfully!" },
            { "BackupRestored", "Backup restored successfully!" },
            { "NoBackups", "No backups found!" },
            { "ConfirmDelete", "Are you sure you want to delete site '{0}'?" },
            { "ConfirmRestore", "Are you sure you want to restore site '{0}' from backup?" },
            { "ConfirmBackup", "Create a backup of site '{0}'?" },
            { "PhpModeWarning", "PHP Learning Mode\n\nYou are about to create a site with the name 'php'.\n\nThis is a SPECIAL mode:\n• No WordPress will be installed\n• No database will be created\n• Only pure PHP for learning and testing\n\nFiles will be located in: WPronto/www/php/\n\nDo you want to continue?" },
            { "TemplateNotFound", "WordPress template not found.\n\nPlease add WordPress files to the 'template' folder." },
            { "TemplateInvalid", "Template folder is missing required WordPress files!\n\nPlease ensure you have a complete WordPress installation in the 'template' folder.\n\nRequired files:\n• wp-admin/index.php\n• wp-includes/version.php\n\nDownload WordPress from: https://wordpress.org/download/" },
            { "SiteNameInvalid", "Site name cannot be empty after removing invalid characters.\nUse only letters, numbers, hyphens." },
            { "SiteNamePrompt", "Enter site name (letters, numbers, hyphens):" },
            { "SiteNameTitle", "Create New Site" },
            { "DefaultSiteName", "mynewsite" },
            { "ServerStopConfirm", "Server is still running.\n\nStop services before closing the program?" },
            { "PortBusyWarning", "⚠️ Port {0} is busy!\n\nAnother MySQL/MariaDB server appears to be running.\nWPronto may fail to start correctly.\n\nContinue anyway?" },
            { "PortConflict", "Port Conflict" },
            { "Error", "Error" },
            { "Info", "Info" },
            { "Warning", "Warning" },
            { "Question", "Question" },
            { "Confirm", "Confirm" },
            { "Cancel", "Cancel" },

            // Log messages
            { "LogServerStarting", "Starting web server..." },
            { "LogServerStopped", "Server stopped" },
            { "LogSiteCreating", "Creating site: {0}" },
            { "LogSiteDeleting", "Deleting site: {0}" },
            { "LogBackupCreating", "Creating backup for '{0}'..." },
            { "LogBackupRestoring", "Restoring site '{0}' from backup..." },
            { "LogTunnelStarting", "Starting tunnel..." },
            { "LogTunnelStopped", "Tunnel stopped" },
            { "LogThemeChanged", "Theme changed to: {0}" },
            { "LogLanguageChanged", "Language changed to: {0}" },
            { "LogPhpSwitched", "PHP switched to {0}" },
            { "LogPhpSet", "PHP version set to {0} (will be used on next start)" },
            { "LogNginxReloaded", "Nginx reloaded successfully (no downtime)" },

            // Backup/Restore
            { "BackupFull", "✓ Full backup" },
            { "BackupFilesOnly", "📁 Files only" },
            { "BackupDbOnly", "💾 DB only" },
            { "BackupIncomplete", "⚠ Incomplete" },
            { "BackupLocation", "Backup location:" },
            { "BackupComplete", "Site '{0}' has been backed up successfully!" },
            { "BackupFailed", "Backup failed: {0}" },
            { "RestoreComplete", "Site '{0}' has been restored successfully from backup!" },
            { "RestoreFailed", "Restore failed: {0}" },
            { "BackupLabel", "Backup" },
            { "BackupInfo", "This will create a copy of:\nWebsite files\nDatabase\n\nThe site will remain active." },
            { "BackupConfirmTitle", "Confirm Backup" },
            { "RestoreConfirmTitle", "Confirm Restore" },
            { "SelectBackup", "Select Backup to Restore" },
            { "RestoreBackupTitle", "Restore Backup - {0}" },

            // PHP
            { "SwitchPhp", "Switch PHP from {0} to {1}?\n\nServer will restart." },
            { "SwitchPhpTitle", "Change PHP Version" },
            { "PhpInfo", "PHP Settings" },
            { "PhpUploadLimit", "You can upload files up to" },
            { "PhpServerCreated", "PHP Server '{0}' created!" },
            { "PhpServerCreatedMessage", "PHP Server '{0}' created!\n\nOpen site?" },
            { "PhpModeInfo", "PHP Mode" },

            // Tunnel
            { "TunnelActive", "Tunnel is active!\n\nShare this URL:\n{0}\n\nURL copied to clipboard." },
            { "TunnelAlreadyActive", "Tunnel is already active for '{0}'.\n\nURL: {1}" },
            { "TunnelNoActive", "No active tunnel to refresh." },
            { "TunnelNoActiveStop", "No active tunnel to stop." },
            { "TunnelStopped", "Sharing has been stopped." },
            { "TunnelUrlCopied", "Tunnel URL copied to clipboard: {0}" },
            { "TunnelCopied", "✅ Copied!" },
            { "TunnelCopyHint", "Click to copy URL to clipboard" },
            { "TunnelWaitingHint", "Waiting for tunnel to start..." },
            { "OpenInstallPage", "Open WordPress install page" },
            { "OpenSite", "Open site" },
            { "ServerNotRunning", "Server is not running!\n\nPlease start the server first." },

            // Ngrok
            { "NgrokAuthRequired", "To use ngrok, you need an auth token.\n\n1. Go to https://dashboard.ngrok.com/signup\n2. Create a free account\n3. Get your auth token from https://dashboard.ngrok.com/auth\n\nWould you like to enter your auth token now?" },
            { "NgrokAuthTitle", "Ngrok Authentication Required" },
            { "NgrokTokenPrompt", "Enter your ngrok auth token:" },
            { "NgrokTokenTitle", "Ngrok Auth Token" },
            { "NgrokAuthFailed", "Failed to authenticate ngrok.\n\nPlease sign up at ngrok.com and add your auth token.\n\nYou can enter it again by clicking the Share button." },
            { "NgrokDownloadFailed", "Failed to download ngrok.\n\nPlease download ngrok manually from:\nhttps://ngrok.com/download\n\nPlace ngrok.exe in:\n{0}\n\nAnd create an account at ngrok.com to get your auth token." },
            { "NgrokNotFound", "ngrok.exe not found!\n\nChecked paths:\n{0}" },

            // Theme
            { "ThemeLight", "☀️" },
            { "ThemeDark", "🌙" },
            { "ThemeSystem", "🌓" },

            // Language
            { "LanguageEN", "🇬🇧 EN" },
            { "LanguageUA", "🇺🇦 UA" },

            // Help
            { "HelpTitle", "WPronto Help" },
            { "HelpNotFound", "help.txt not found." },

            // Error messages
            { "ErrorInit", "Error initializing application: {0}" },
            { "ErrorOpenBrowser", "Failed to open browser: {0}" },
            { "ErrorOpenFolder", "Failed to open folder: {0}" },
            { "ErrorCreateSite", "Error creating site: {0}" },
            { "ErrorDeleteSite", "Error deleting site: {0}" },
            { "ErrorRestoreSite", "Error restoring site: {0}" },
            { "ErrorBackupSite", "Error backing up site: {0}" },
            { "ErrorTunnel", "Failed to start tunnel: {0}" },
            { "SiteFolderNotFound", "Site folder not found: {0}" },
            { "NoBackupsForSite", "No backups found for '{0}'.\n\nCreate a backup first using the Backup button." },
            { "PhpModeNoBackup", "PHP Mode sites don't have databases to backup.\n\nYou can manually copy the files from the 'www/php' folder." },
        };

        public static string Get(string key)
        {
            return _strings.TryGetValue(key, out string value) ? value : key;
        }
    }

    // =========================
    // UKRAINIAN STRINGS
    // =========================
    public static class UkrainianStrings
    {
        private static readonly Dictionary<string, string> _strings = new()
        {
            // Main Window
            { "AppTitle", "WPronto v5.0" },
            { "AppSubtitle", "Локальне середовище для WordPress" },
            { "ServerRunning", "● СЕРВЕР ПРАЦЮЄ" },
            { "ServerStopped", "● СЕРВЕР ЗУПИНЕНО" },
            { "ShareOpen", "🔓 Доступ: Відкрито" },
            { "ShareStarting", "⏳ Доступ: Запуск..." },
            { "ShareClosed", "🔒 Доступ: Закрито" },
            { "Sites", "САЙТИ" },
            { "ServerLog", "ЛОГ СЕРВЕРА" },
            { "PHP", "PHP" },
            { "ActivePHP", "Активний: {0}" },

            // Buttons - БЕЗ ІКОНОК
            { "Start", "Запустити" },
            { "Restart", "Перезапустити" },
            { "Stop", "Зупинити" },
            { "Create", "Створити" },
            { "Backup", "Бекап" },
            { "Restore", "Відновити" },
            { "Delete", "Видалити" },
            { "Help", "Довідка" },

            // Context menu - З ІКОНКАМИ
            { "OpenAdmin", "🌐 Відкрити адмінку" },
            { "OpenProject", "📁 Відкрити проєкт" },
            { "OpenBackup", "📁 Відкрити бекап" },
            { "MenuBackup", "💾 Бекап" },
            { "MenuRestore", "⏪ Відновити" },
            { "MenuDelete", "🗑️ Видалити" }, 
            { "Share", "🔗 Поділитися" },
            { "Refresh", "🔄 Оновити" },
            { "CloseShare", "⛓️‍💥 Закрити" },

            // Messages
            { "ServerAlreadyRunning", "Сервер вже працює!" },
            { "ServerStartFailed", "Не вдалося запустити сервер!" },
            { "SelectSiteFirst", "Спочатку оберіть сайт!" },
            { "SiteExists", "Сайт вже існує!" },
            { "SiteCreated", "Сайт успішно створено!" },
            { "SiteDeleted", "Сайт успішно видалено!" },
            { "BackupCreated", "Бекап створено!" },
            { "BackupRestored", "Бекап відновлено!" },
            { "NoBackups", "Бекап копії не знайдено!" },
            { "ConfirmDelete", "Ви впевнені, що хочете видалити сайт '{0}'?" },
            { "ConfirmRestore", "Ви впевнені, що хочете відновити сайт '{0}' з резервної копії?" },
            { "ConfirmBackup", "Створити резервну копію сайту '{0}'?" },
            { "PhpModeWarning", "PHP Режим Навчання\n\nВи збираєтеся створити сайт з ім'ям 'php'.\n\nЦе СПЕЦІАЛЬНИЙ режим:\n• WordPress не буде встановлено\n• База даних не буде створена\n• Тільки чистий PHP для навчання та тестування\n\nФайли будуть розташовані в: WPronto/www/php/\n\nБажаєте продовжити?" },
            { "TemplateNotFound", "Шаблон WordPress не знайдено.\n\nБудь ласка, додайте файли WordPress до папки 'template'." },
            { "TemplateInvalid", "У папці шаблону відсутні необхідні файли WordPress!\n\nБудь ласка, переконайтеся, що ви маєте повну інсталяцію WordPress у папці 'template'.\n\nНеобхідні файли:\n• wp-admin/index.php\n• wp-includes/version.php\n\nЗавантажте WordPress з: https://wordpress.org/download/" },
            { "SiteNameInvalid", "Назва сайту не може бути порожньою після видалення недопустимих символів.\nВикористовуйте лише літери, цифри та дефіси." },
            { "SiteNamePrompt", "Введіть назву сайту (літери, цифри, дефіси):" },
            { "SiteNameTitle", "Створення нового сайту" },
            { "DefaultSiteName", "mynewsite" },
            { "ServerStopConfirm", "Сервер все ще працює.\n\nЗупинити сервіси перед закриттям програми?" },
            { "PortBusyWarning", "⚠️ Порт {0} зайнятий!\n\nСхоже, запущено інший MySQL/MariaDB сервер.\nWPronto може не запуститися коректно.\n\nПродовжити в будь-якому випадку?" },
            { "PortConflict", "Конфлікт портів" },
            { "Error", "Помилка" },
            { "Info", "Інформація" },
            { "Warning", "Попередження" },
            { "Question", "Питання" },
            { "Confirm", "Підтвердження" },
            { "Cancel", "Скасувати" },

            // Log messages
            { "LogServerStarting", "Запуск веб-сервера..." },
            { "LogServerStopped", "Сервер зупинено" },
            { "LogSiteCreating", "Створення сайту: {0}" },
            { "LogSiteDeleting", "Видалення сайту: {0}" },
            { "LogBackupCreating", "Створення резервної копії для '{0}'..." },
            { "LogBackupRestoring", "Відновлення сайту '{0}' з резервної копії..." },
            { "LogTunnelStarting", "Запуск тунелю..." },
            { "LogTunnelStopped", "Доступ закрито" },
            { "LogThemeChanged", "Тему змінено на: {0}" },
            { "LogLanguageChanged", "Мову змінено на: {0}" },
            { "LogPhpSwitched", "PHP переключено на {0}" },
            { "LogPhpSet", "Версію PHP встановлено на {0} (буде використана при наступному запуску)" },
            { "LogNginxReloaded", "Nginx перезавантажено успішно (без простою)" },

            // Backup/Restore
            { "BackupFull", "✓ Повний бекап" },
            { "BackupFilesOnly", "📁 Тільки файли" },
            { "BackupDbOnly", "💾 Тільки БД" },
            { "BackupIncomplete", "⚠ Неповний" },
            { "BackupLocation", "Розташування бекапу:" },
            { "BackupComplete", "Сайт '{0}' успішно забекаплено!" },
            { "BackupFailed", "Помилка бекапу: {0}" },
            { "RestoreComplete", "Сайт '{0}' успішно відновлено з бекапу!" },
            { "RestoreFailed", "Помилка відновлення: {0}" },
            { "BackupLabel", "Бекап" },
            { "BackupInfo", "Це створить копію:\nФайлів сайту\nБази даних\n\nСайт залишиться активним." },
            { "BackupConfirmTitle", "Підтвердження бекапу" },
            { "RestoreConfirmTitle", "Підтвердження відновлення" },
            { "SelectBackup", "Виберіть бекап для відновлення" },
            { "RestoreBackupTitle", "Відновлення бекапу - {0}" },

            // PHP
            { "SwitchPhp", "Переключити PHP з {0} на {1}?\n\nСервер буде перезапущено." },
            { "SwitchPhpTitle", "Зміна версії PHP" },
            { "PhpInfo", "Налаштування PHP" },
            { "PhpUploadLimit", "Ви можете завантажувати файли розміром до" },
            { "PhpServerCreated", "PHP сервер '{0}' створено!" },
            { "PhpServerCreatedMessage", "PHP сервер '{0}' створено!\n\nВідкрити сайт?" },
            { "PhpModeInfo", "PHP Режим" },

            // Tunnel
            { "TunnelActive", "Тунель активний!\n\nПоділіться цим URL:\n{0}\n\nURL скопійовано в буфер обміну." },
            { "TunnelAlreadyActive", "Тунель вже активний для '{0}'.\n\nURL: {1}" },
            { "TunnelNoActive", "Немає активного тунелю для оновлення." },
            { "TunnelNoActiveStop", "Немає активного тунелю для зупинки." },
            { "TunnelStopped", "Доступ зупинено." },
            { "TunnelUrlCopied", "URL тунелю скопійовано в буфер обміну: {0}" },
            { "TunnelCopied", "✅ Скопійовано!" },
            { "TunnelCopyHint", "Натисніть, щоб скопіювати URL в буфер обміну" },
            { "TunnelWaitingHint", "Очікування запуску тунелю..." },
            { "OpenInstallPage", "Відкрити сторінку встановлення WordPress" },
            { "OpenSite", "Відкрити сайт" },
            { "ServerNotRunning", "Сервер не запущено!\n\nБудь ласка, спочатку запустіть сервер." },

            // Ngrok
            { "NgrokAuthRequired", "Для використання ngrok потрібен токен авторизації.\n\n1. Перейдіть на https://dashboard.ngrok.com/signup\n2. Створіть безкоштовний аккаунт\n3. Отримайте токен авторизації з https://dashboard.ngrok.com/auth\n\nБажаєте ввести токен зараз?" },
            { "NgrokAuthTitle", "Потрібна авторизація ngrok" },
            { "NgrokTokenPrompt", "Введіть ваш токен авторизації ngrok:" },
            { "NgrokTokenTitle", "Токен авторизації ngrok" },
            { "NgrokAuthFailed", "Не вдалося авторизувати ngrok.\n\nБудь ласка, зареєструйтеся на ngrok.com та додайте токен авторизації.\n\nВи можете ввести його знову, натиснувши кнопку Поділитися." },
            { "NgrokDownloadFailed", "Не вдалося завантажити ngrok.\n\nБудь ласка, завантажте ngrok вручну з:\nhttps://ngrok.com/download\n\nПомістіть ngrok.exe в:\n{0}\n\nТа створіть аккаунт на ngrok.com, щоб отримати токен авторизації." },
            { "NgrokNotFound", "ngrok.exe не знайдено!\n\nПеревірені шляхи:\n{0}" },

            // Theme
            { "ThemeLight", "☀️" },
            { "ThemeDark", "🌙" },
            { "ThemeSystem", "🌓" },

            // Language
            { "LanguageEN", "🇬🇧 EN" },
            { "LanguageUA", "🇺🇦 UA" },

            // Help
            { "HelpTitle", "Довідка WPronto" },
            { "HelpNotFound", "help.txt не знайдено." },

            // Error messages
            { "ErrorInit", "Помилка ініціалізації програми: {0}" },
            { "ErrorOpenBrowser", "Не вдалося відкрити браузер: {0}" },
            { "ErrorOpenFolder", "Не вдалося відкрити папку: {0}" },
            { "ErrorCreateSite", "Помилка створення сайту: {0}" },
            { "ErrorDeleteSite", "Помилка видалення сайту: {0}" },
            { "ErrorRestoreSite", "Помилка відновлення сайту: {0}" },
            { "ErrorBackupSite", "Помилка бекапу сайту: {0}" },
            { "ErrorTunnel", "Помилка запуску тунелю: {0}" },
            { "SiteFolderNotFound", "Папку сайту не знайдено: {0}" },
            { "NoBackupsForSite", "Не знайдено бекапів для '{0}'.\n\nСпочатку створіть бекап за допомогою кнопки Бекап." },
            { "PhpModeNoBackup", "Сайти в режимі PHP не мають баз даних для бекапу.\n\nВи можете вручну скопіювати файли з папки 'www/php'." },
        };

        public static string Get(string key)
        {
            return _strings.TryGetValue(key, out string value) ? value : key;
        }
    }

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
            if (HasFiles && HasDatabase) status = LocalizationManager.GetString("BackupFull");
            else if (HasFiles) status = LocalizationManager.GetString("BackupFilesOnly");
            else if (HasDatabase) status = LocalizationManager.GetString("BackupDbOnly");
            else status = LocalizationManager.GetString("BackupIncomplete");

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
        private Label btnLanguage;
        private ComboBox comboPhpVersion;
        private Label lblPhpStatus;
        private ToolTip toolTip;

        // Tunnel variables - NGROK
        private Process _ngrokProcess;
        private string _currentTunnelUrl = string.Empty;
        private string _currentTunnelSite = string.Empty;
        private bool _isTunnelActive = false;
        private System.Windows.Forms.Timer _tunnelStatusTimer;
        private Label lblTunnelStatus;
        private Label lblTunnelUrl;

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
                // Initialize localization first
                LocalizationManager.Initialize();
                LocalizationManager.LanguageChanged += (lang) =>
                {
                    this.Invoke(new Action(() => UpdateUIStrings()));
                };

#if DEBUG
                AttachDebugConsole();
                Console.WriteLine("Starting WPronto in DEBUG mode...");
#endif

                using (Graphics g = this.CreateGraphics())
                {
                    _dpiScale = g.DpiX / 96f;
                    if (_dpiScale > 1.15f) _dpiScale = 1.15f;
                }

                this.Text = LocalizationManager.GetString("AppTitle");
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

                // Update UI strings after layout is created
                UpdateUIStrings();

                var savedTheme = ThemeManager.LoadThemeSetting();
                ThemeManager.SetTheme(savedTheme);
                ThemeManager.ThemeChanged += (theme) => ApplyTheme();
                ApplyTheme();

                LoadSites();
                CreateTunnelFixPlugin();
                FixExistingSitesForTunnel();
                CheckServerStatus();
                ValidateTemplate();
                CheckAdminRights();
                CleanupTempFiles();

                _statusTimer = new System.Windows.Forms.Timer();
                _statusTimer.Interval = STATUS_CHECK_INTERVAL_MS;
                _statusTimer.Tick += (s, e) => CheckServerStatus();
                _statusTimer.Start();

                _tunnelStatusTimer = new System.Windows.Forms.Timer();
                _tunnelStatusTimer.Interval = 3000;
                _tunnelStatusTimer.Tick += TunnelStatusTimer_Tick;
            }
            catch (Exception ex)
            {
                LogError(ex, "Form1_Constructor");
                MessageBox.Show(LocalizationManager.FormatString("ErrorInit", ex.Message), LocalizationManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // =========================
        // DEBUG CONSOLE
        // =========================
#if DEBUG
        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        private void AttachDebugConsole()
        {
            try
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
                Console.WriteLine("=== DEBUG CONSOLE ATTACHED ===");
                Console.WriteLine($"Base Path: {_basePath}");
                Console.WriteLine($"Web Port: {_webPort}");
                Console.WriteLine($"PHP Version: {_currentPhpVersion}");
            }
            catch (Exception ex)
            {
                // Ignore
            }
        }
#endif

        // =========================
        // TUNNEL LOGGING
        // =========================
        private void LogTunnel(string message)
        {
            try
            {
                string logMessage = $"[TUNNEL] {message}";

#if DEBUG
                Console.WriteLine($"{DateTime.Now:HH:mm:ss.fff} {logMessage}");
#endif

                Log(logMessage);

                string tunnelLogPath = Path.Combine(_logsPath, "tunnel.log");
                Directory.CreateDirectory(Path.GetDirectoryName(tunnelLogPath));
                File.AppendAllText(tunnelLogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {message}{Environment.NewLine}");
            }
            catch { }
        }

        // =========================
        // FORM CLOSING HANDLER
        // =========================
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                StopTunnel();

                if (IsProcessRunning("nginx") || IsProcessRunning("php-cgi") || IsProcessRunning("mysqld"))
                {
                    var result = MessageBox.Show(
                        LocalizationManager.GetString("ServerStopConfirm"),
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
        // UI STRINGS UPDATE
        // =========================
        private void UpdateUIStrings()
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(UpdateUIStrings));
                    return;
                }

                this.Text = LocalizationManager.GetString("AppTitle");
                if (lblTitle != null) lblTitle.Text = LocalizationManager.GetString("AppTitle");
                if (lblSubtitle != null) lblSubtitle.Text = LocalizationManager.GetString("AppSubtitle");
                if (btnStart != null) btnStart.Text = LocalizationManager.GetString("Start");
                if (btnRestart != null) btnRestart.Text = LocalizationManager.GetString("Restart");
                if (btnStop != null) btnStop.Text = LocalizationManager.GetString("Stop");
                if (btnCreateSite != null) btnCreateSite.Text = LocalizationManager.GetString("Create");
                if (btnBackupSite != null) btnBackupSite.Text = LocalizationManager.GetString("Backup");
                if (btnRestoreBackup != null) btnRestoreBackup.Text = LocalizationManager.GetString("Restore");
                if (btnDeleteSite != null) btnDeleteSite.Text = LocalizationManager.GetString("Delete");
                if (btnHelp != null) btnHelp.Text = LocalizationManager.GetString("Help");
                if (btnLanguage != null)
                {
                    btnLanguage.Text = LocalizationManager.CurrentLanguage == LocalizationManager.Language.English
                        ? LocalizationManager.GetString("LanguageEN")
                        : LocalizationManager.GetString("LanguageUA");
                }

                // Update context menu items - використовуємо оновлення за індексами
                if (listSites?.ContextMenuStrip != null)
                {
                    var menu = listSites.ContextMenuStrip;
                    if (menu.Items.Count > 0) menu.Items[0].Text = LocalizationManager.GetString("OpenAdmin");
                    if (menu.Items.Count > 1) menu.Items[1].Text = LocalizationManager.GetString("OpenProject");

                    // Оновлені рядки:
                    if (menu.Items.Count > 3) menu.Items[3].Text = LocalizationManager.GetString("MenuBackup");
                    if (menu.Items.Count > 4) menu.Items[4].Text = LocalizationManager.GetString("MenuRestore");
                    if (menu.Items.Count > 5) menu.Items[5].Text = LocalizationManager.GetString("OpenBackup");

                    if (menu.Items.Count > 7) menu.Items[7].Text = LocalizationManager.GetString("Share");
                    if (menu.Items.Count > 8) menu.Items[8].Text = LocalizationManager.GetString("Refresh");
                    if (menu.Items.Count > 9) menu.Items[9].Text = LocalizationManager.GetString("CloseShare");

                    // Оновлений рядок:
                    if (menu.Items.Count > 11) menu.Items[11].Text = LocalizationManager.GetString("MenuDelete");
                }

                CheckServerStatus();
                UpdateTunnelUI();

                // Update PHP version label
                if (lblPhpStatus != null)
                {
                    bool running = IsProcessRunning("nginx") && IsProcessRunning("php-cgi");
                    string status = running
                        ? LocalizationManager.FormatString("ActivePHP", _currentPhpVersion)
                        : LocalizationManager.FormatString("ActivePHP", _currentPhpVersion);
                    lblPhpStatus.Text = status;
                }

                // Update Labels
                foreach (Control control in this.Controls)
                {
                    if (control is Label label && label != lblTitle && label != lblSubtitle && label != lblStatus && label != btnTheme && label != btnLanguage && label != lblPhpStatus && label != lblTunnelStatus && label != lblTunnelUrl)
                    {
                        if (!(label is LinkLabel))
                        {
                            if (label.Text == "PHP:" || label.Text == "PHP:")
                                label.Text = LocalizationManager.GetString("PHP") + ":";
                            else if (label.Text == "SITES" || label.Text == "САЙТИ")
                                label.Text = LocalizationManager.GetString("Sites");
                            else if (label.Text == "SERVER LOG" || label.Text == "ЛОГ СЕРВЕРА")
                                label.Text = LocalizationManager.GetString("ServerLog");
                        }
                    }
                }

                // Update theme button
                if (btnTheme != null)
                {
                    string themeIcon = ThemeManager.CurrentTheme == AppTheme.Light ? LocalizationManager.GetString("ThemeLight") :
                                      (ThemeManager.CurrentTheme == AppTheme.Dark ? LocalizationManager.GetString("ThemeDark") :
                                      LocalizationManager.GetString("ThemeSystem"));
                    btnTheme.Text = themeIcon;
                }

                // Update tooltip for tunnel URL
                if (lblTunnelUrl != null)
                {
                    if (!string.IsNullOrEmpty(_currentTunnelUrl))
                        toolTip.SetToolTip(lblTunnelUrl, LocalizationManager.GetString("TunnelCopyHint"));
                    else if (_isTunnelActive)
                        toolTip.SetToolTip(lblTunnelUrl, LocalizationManager.GetString("TunnelWaitingHint"));
                    else
                        toolTip.SetToolTip(lblTunnelUrl, "");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "UpdateUIStrings");
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
                        "echo '<h2>" + LocalizationManager.GetString("PhpInfo") + "</h2>';\n" +
                        "echo '<strong>upload_max_filesize:</strong> ' . ini_get('upload_max_filesize') . '<br>';\n" +
                        "echo '<strong>post_max_size:</strong> ' . ini_get('post_max_size') . '<br>';\n" +
                        "echo '<strong>memory_limit:</strong> ' . ini_get('memory_limit') . '<br>';\n" +
                        "echo '<strong>max_execution_time:</strong> ' . ini_get('max_execution_time') . ' seconds<br>';\n" +
                        "echo '<hr>';\n" +
                        "echo '<h2>WordPress Upload Limits</h2>';\n" +
                        "echo '<p>" + LocalizationManager.GetString("PhpUploadLimit") + " <strong style=\"color:green\">' . ini_get('upload_max_filesize') . '</strong></p>';\n" +
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
                    string themeIcon = ThemeManager.CurrentTheme == AppTheme.Light ? LocalizationManager.GetString("ThemeLight") :
                                      (ThemeManager.CurrentTheme == AppTheme.Dark ? LocalizationManager.GetString("ThemeDark") :
                                      LocalizationManager.GetString("ThemeSystem"));
                    btnTheme.Text = themeIcon;
                    btnTheme.ForeColor = scheme.TextPrimary;
                }

                if (btnLanguage != null)
                {
                    btnLanguage.ForeColor = scheme.TextPrimary;
                }

                if (comboPhpVersion != null)
                {
                    comboPhpVersion.BackColor = scheme.BackgroundCard;
                    comboPhpVersion.ForeColor = scheme.TextSecondary;
                }
                if (lblPhpStatus != null) lblPhpStatus.ForeColor = scheme.TextMuted;

                if (lblTunnelStatus != null)
                {
                    if (_isTunnelActive && !string.IsNullOrEmpty(_currentTunnelUrl))
                    {
                        lblTunnelStatus.Text = LocalizationManager.GetString("ShareOpen");
                        lblTunnelStatus.ForeColor = scheme.SuccessColor;
                    }
                    else if (_isTunnelActive)
                    {
                        lblTunnelStatus.Text = LocalizationManager.GetString("ShareStarting");
                        lblTunnelStatus.ForeColor = scheme.WarningColor;
                    }
                    else
                    {
                        lblTunnelStatus.Text = LocalizationManager.GetString("ShareClosed");
                        lblTunnelStatus.ForeColor = scheme.TextMuted;
                    }
                }

                if (lblTunnelUrl != null)
                {
                    lblTunnelUrl.ForeColor = string.IsNullOrEmpty(_currentTunnelUrl)
                        ? scheme.TextMuted
                        : scheme.PrimaryColor;
                }

                foreach (Control control in this.Controls)
                {
                    if (control is Label label && label != lblTitle && label != lblSubtitle && label != lblStatus && label != btnTheme && label != btnLanguage && label != lblPhpStatus && label != lblTunnelStatus && label != lblTunnelUrl)
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

            toolTip = new ToolTip();

            lblTitle = new Label
            {
                Text = "WPronto v5.0",
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

            lblTunnelStatus = new Label
            {
                Text = "🔒 Share: Closed",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                ForeColor = scheme.TextMuted,
                Location = new Point(ScaleInt(540), ScaleInt(56)),
                Size = new Size(ScaleInt(220), ScaleInt(20)),
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = false
            };

            lblTunnelUrl = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                ForeColor = scheme.TextMuted,
                Location = new Point(ScaleInt(540), ScaleInt(76)),
                Size = new Size(ScaleInt(220), ScaleInt(20)),
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize = false,
                Cursor = Cursors.Hand
            };
            lblTunnelUrl.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_currentTunnelUrl))
                {
                    try
                    {
                        Clipboard.SetText(_currentTunnelUrl);
                        Log(LocalizationManager.FormatString("TunnelUrlCopied", _currentTunnelUrl));
                        string originalText = lblTunnelUrl.Text;
                        lblTunnelUrl.Text = LocalizationManager.GetString("TunnelCopied");
                        lblTunnelUrl.ForeColor = ThemeManager.CurrentScheme.SuccessColor;
                        System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                        timer.Interval = 1500;
                        timer.Tick += (s2, e2) =>
                        {
                            lblTunnelUrl.Text = originalText;
                            lblTunnelUrl.ForeColor = ThemeManager.CurrentScheme.PrimaryColor;
                            timer.Stop();
                            timer.Dispose();
                        };
                        timer.Start();
                    }
                    catch (Exception ex)
                    {
                        LogError(ex, "lblTunnelUrl_Click");
                    }
                }
            };
            toolTip.SetToolTip(lblTunnelUrl, LocalizationManager.GetString("TunnelCopyHint"));

            btnTheme = new Label
            {
                Text = ThemeManager.CurrentTheme == AppTheme.Light ? LocalizationManager.GetString("ThemeLight") :
                       (ThemeManager.CurrentTheme == AppTheme.Dark ? LocalizationManager.GetString("ThemeDark") :
                       LocalizationManager.GetString("ThemeSystem")),
                Size = new Size(ScaleInt(40), ScaleInt(32)),
                Location = new Point(ScaleInt(770), ScaleInt(28)),
                Font = new Font("Segoe UI", 14f),
                Cursor = Cursors.Hand,
                ForeColor = scheme.TextPrimary,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btnTheme.Click += BtnTheme_Click;

            Label lblLanguageSwitch = new Label
            {
                Text = LocalizationManager.CurrentLanguage == LocalizationManager.Language.English
                    ? "🌐 Українська"
                    : "🌐 English",
                Location = new Point(ScaleInt(710), ScaleInt(505)),
                AutoSize = true,
                ForeColor = scheme.PrimaryColor,
                Font = new Font("Segoe UI", 8f, FontStyle.Underline),
                Cursor = Cursors.Hand
            };
            lblLanguageSwitch.Click += (s, e) =>
            {
                var newLang = LocalizationManager.CurrentLanguage == LocalizationManager.Language.English
                    ? LocalizationManager.Language.Ukrainian
                    : LocalizationManager.Language.English;

                LocalizationManager.CurrentLanguage = newLang;
                UpdateUIStrings();

                lblLanguageSwitch.Text = newLang == LocalizationManager.Language.English
                    ? "🌐 Українська"
                    : "🌐 English";

                Log(LocalizationManager.FormatString("LogLanguageChanged", newLang));
            };

            this.Controls.Add(lblLanguageSwitch);

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
                Text = LocalizationManager.FormatString("ActivePHP", _currentPhpVersion),
                Location = new Point(ScaleInt(145), ScaleInt(114)),
                Size = new Size(ScaleInt(130), ScaleInt(20)),
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
                                    LocalizationManager.FormatString("SwitchPhp", _currentPhpVersion, newVersion),
                                    LocalizationManager.GetString("SwitchPhpTitle"),
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

                                    lblPhpStatus.Text = LocalizationManager.FormatString("ActivePHP", newVersion);
                                    Log(LocalizationManager.FormatString("LogPhpSwitched", newVersion));
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

                                lblPhpStatus.Text = LocalizationManager.FormatString("ActivePHP", newVersion) + " " + LocalizationManager.GetString("NextStart");
                                Log(LocalizationManager.FormatString("LogPhpSet", newVersion));
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

            btnStart = new SoftButton(LocalizationManager.GetString("Start"), ButtonStyle.Primary, _dpiScale);
            btnStart.Margin = new Padding(ScaleInt(2), 0, ScaleInt(18), 0);
            btnStart.Click += BtnStart_Click;

            btnRestart = new SoftButton(LocalizationManager.GetString("Restart"), ButtonStyle.Warning, _dpiScale);
            btnRestart.Margin = new Padding(ScaleInt(2), 0, ScaleInt(18), 0);
            btnRestart.Click += BtnRestart_Click;

            btnStop = new SoftButton(LocalizationManager.GetString("Stop"), ButtonStyle.Danger, _dpiScale);
            btnStop.Margin = new Padding(ScaleInt(2), 0, ScaleInt(18), 0);
            btnStop.Click += BtnStop_Click;

            btnPhpMyAdmin = new SoftButton("phpMyAdmin", ButtonStyle.Default, _dpiScale);
            btnPhpMyAdmin.Margin = new Padding(ScaleInt(2), 0, 0, 0);
            btnPhpMyAdmin.Click += BtnPhpMyAdmin_Click;

            topButtonsPanel.Controls.AddRange(new Control[] { btnStart, btnRestart, btnStop, btnPhpMyAdmin });

            Label lblSitesTitle = new Label
            {
                Text = LocalizationManager.GetString("Sites"),
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
                            LocalizationManager.GetString("ServerNotRunning"),
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
                        MessageBox.Show(LocalizationManager.FormatString("ErrorOpenBrowser", ex.Message),
                            LocalizationManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            ContextMenuStrip contextMenu = new ContextMenuStrip();

            // 1. Відкрити адмінку - Ctrl+Shift+A (прихована)
            ToolStripMenuItem openAdminItem = new ToolStripMenuItem();
            openAdminItem.Text = LocalizationManager.GetString("OpenAdmin");
            openAdminItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.A;
            openAdminItem.ShowShortcutKeys = true;
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

            // 2. Відкрити проєкт - Ctrl+Shift+O (прихована)
            ToolStripMenuItem openProjectItem = new ToolStripMenuItem();
            openProjectItem.Text = LocalizationManager.GetString("OpenProject");
            openProjectItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.O;
            openProjectItem.ShowShortcutKeys = true;
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
                            MessageBox.Show(LocalizationManager.FormatString("ErrorOpenFolder", ex.Message),
                                LocalizationManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show(LocalizationManager.FormatString("SiteFolderNotFound", sitePath), "WPronto",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show(LocalizationManager.GetString("SelectSiteFirst"), "WPronto",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            contextMenu.Items.Add(openProjectItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // 3. Бекап - Ctrl+Shift+B (прихована)
            ToolStripMenuItem backupItem = new ToolStripMenuItem();
            backupItem.Text = LocalizationManager.GetString("Backup");
            backupItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.B;
            backupItem.ShowShortcutKeys = true;
            backupItem.Click += (s, e) => BtnBackupSite_Click(s, e);
            contextMenu.Items.Add(backupItem);

            // 4. Відновити - Ctrl+Shift+F (прихована)
            ToolStripMenuItem restoreItem = new ToolStripMenuItem();
            restoreItem.Text = LocalizationManager.GetString("Restore");
            restoreItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.F;
            restoreItem.ShowShortcutKeys = true;
            restoreItem.Click += (s, e) => BtnRestoreBackup_Click(s, e);
            contextMenu.Items.Add(restoreItem);

            // 5. Відкрити бекап - Ctrl+Shift+G (прихована)
            ToolStripMenuItem openBackupItem = new ToolStripMenuItem();
            openBackupItem.Text = LocalizationManager.GetString("OpenBackup");
            openBackupItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.G;
            openBackupItem.ShowShortcutKeys = true;
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
                            MessageBox.Show(LocalizationManager.FormatString("ErrorOpenFolder", ex.Message),
                                LocalizationManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show(LocalizationManager.FormatString("NoBackupsForSite", siteName),
                            "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else
                {
                    MessageBox.Show(LocalizationManager.GetString("SelectSiteFirst"), "WPronto",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            contextMenu.Items.Add(openBackupItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // 6. Поділитися - Ctrl+Shift+T (прихована)
            ToolStripMenuItem shareItem = new ToolStripMenuItem();
            shareItem.Text = LocalizationManager.GetString("Share");
            shareItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.T;
            shareItem.ShowShortcutKeys = true;
            shareItem.Click += BtnShareSite_Click;
            contextMenu.Items.Add(shareItem);

            // 7. Оновити - Ctrl+Shift+U (прихована)
            ToolStripMenuItem refreshItem = new ToolStripMenuItem();
            refreshItem.Text = LocalizationManager.GetString("Refresh");
            refreshItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.U;
            refreshItem.ShowShortcutKeys = true;
            refreshItem.Click += BtnRefreshTunnel_Click;
            contextMenu.Items.Add(refreshItem);

            // 8. Закрити доступ - Ctrl+Shift+W (прихована)
            ToolStripMenuItem stopShareItem = new ToolStripMenuItem();
            stopShareItem.Text = LocalizationManager.GetString("CloseShare");
            stopShareItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.W;
            stopShareItem.ShowShortcutKeys = true;
            stopShareItem.Click += BtnStopSharing_Click;
            contextMenu.Items.Add(stopShareItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            // 9. Видалити - Ctrl+Shift+D (прихована)
            ToolStripMenuItem deleteItem = new ToolStripMenuItem();
            deleteItem.Text = LocalizationManager.GetString("Delete");
            deleteItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.D;
            deleteItem.ShowShortcutKeys = true;
            deleteItem.Click += (s, e) => BtnDeleteSite_Click(s, e);
            contextMenu.Items.Add(deleteItem);

            listSites.ContextMenuStrip = contextMenu;

            cardSites.Controls.Add(listSites);

            Label lblLogsTitle = new Label
            {
                Text = LocalizationManager.GetString("ServerLog"),
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

            ModernCard cardLogs = new ModernCard(ScaleInt(315), ScaleInt(175), ScaleInt(485), ScaleInt(285), _dpiScale);
            cardLogs.Controls.Add(txtLog);

            btnCreateSite = new SoftButton(LocalizationManager.GetString("Create"), ButtonStyle.Primary, _dpiScale);
            btnCreateSite.Location = new Point(ScaleInt(32), ScaleInt(480));
            btnCreateSite.Width = ScaleInt(100);
            btnCreateSite.Click += BtnCreateSite_Click;

            btnBackupSite = new SoftButton(LocalizationManager.GetString("Backup"), ButtonStyle.Success, _dpiScale);
            btnBackupSite.Location = new Point(ScaleInt(140), ScaleInt(480));
            btnBackupSite.Width = ScaleInt(90);
            btnBackupSite.Click += BtnBackupSite_Click;

            btnRestoreBackup = new SoftButton(LocalizationManager.GetString("Restore"), ButtonStyle.Warning, _dpiScale);
            btnRestoreBackup.Location = new Point(ScaleInt(240), ScaleInt(480));
            btnRestoreBackup.Width = ScaleInt(90);
            btnRestoreBackup.Click += BtnRestoreBackup_Click;

            btnDeleteSite = new SoftButton(LocalizationManager.GetString("Delete"), ButtonStyle.Danger, _dpiScale);
            btnDeleteSite.Location = new Point(ScaleInt(340), ScaleInt(480));
            btnDeleteSite.Width = ScaleInt(90);
            btnDeleteSite.Click += BtnDeleteSite_Click;

            btnHelp = new SoftButton(LocalizationManager.GetString("Help"), ButtonStyle.Default, _dpiScale);
            btnHelp.Location = new Point(ScaleInt(440), ScaleInt(480));
            btnHelp.Width = ScaleInt(90);
            btnHelp.Click += BtnHelp_Click;

            lnkWebsite = new LinkLabel
            {
                Text = "Official website",
                Location = new Point(ScaleInt(710), ScaleInt(480)),
                AutoSize = true,
                LinkColor = scheme.PrimaryColor,
                Font = new Font("Segoe UI Semibold", 8.5f, FontStyle.Regular)
            };
            lnkWebsite.LinkClicked += (s, e) =>
            {
                try { Process.Start(new ProcessStartInfo { FileName = "https://ovcharovcoder.github.io/wpronto", UseShellExecute = true }); }
                catch { }
            };

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblSubtitle);
            this.Controls.Add(lblStatus);
            this.Controls.Add(lblTunnelStatus);
            this.Controls.Add(lblTunnelUrl);
            this.Controls.Add(btnTheme);
            this.Controls.Add(btnLanguage);
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
                Log(LocalizationManager.FormatString("LogThemeChanged", newTheme));
            }
            catch (Exception ex)
            {
                LogError(ex, "BtnTheme_Click");
            }
        }

        private void BtnLanguage_Click(object sender, EventArgs e)
        {
            try
            {
                var newLang = LocalizationManager.CurrentLanguage == LocalizationManager.Language.English
                    ? LocalizationManager.Language.Ukrainian
                    : LocalizationManager.Language.English;

                LocalizationManager.CurrentLanguage = newLang;
                UpdateUIStrings();

                btnLanguage.Text = newLang == LocalizationManager.Language.English
                    ? LocalizationManager.GetString("LanguageEN")
                    : LocalizationManager.GetString("LanguageUA");

                Log(LocalizationManager.FormatString("LogLanguageChanged", newLang));
            }
            catch (Exception ex)
            {
                LogError(ex, "BtnLanguage_Click");
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
                // Основні дії
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

                // Гарячі клавіші для контекстного меню
                if (keyData == (Keys.Control | Keys.Shift | Keys.A))
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
                            LogError(ex, "ProcessCmdKey_OpenAdmin");
                        }
                    }
                    return true;
                }
                if (keyData == (Keys.Control | Keys.Shift | Keys.O))
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
                                LogError(ex, "ProcessCmdKey_OpenProject");
                            }
                        }
                    }
                    return true;
                }
                if (keyData == (Keys.Control | Keys.Shift | Keys.G))
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
                                LogError(ex, "ProcessCmdKey_OpenBackup");
                            }
                        }
                    }
                    return true;
                }

                // Гарячі клавіші для тунелю
                if (keyData == (Keys.Control | Keys.Shift | Keys.T))
                {
                    BtnShareSite_Click(this, EventArgs.Empty);
                    return true;
                }
                if (keyData == (Keys.Control | Keys.Shift | Keys.U))
                {
                    BtnRefreshTunnel_Click(this, EventArgs.Empty);
                    return true;
                }
                if (keyData == (Keys.Control | Keys.Shift | Keys.W))
                {
                    BtnStopSharing_Click(this, EventArgs.Empty);
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
        // LOGGING
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
                    Log(LocalizationManager.GetString("LogNginxReloaded"));
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
                        LocalizationManager.FormatString("PortBusyWarning", Config.MysqlPort),
                        LocalizationManager.GetString("PortConflict"),
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
        // SERVER STATUS
        // =========================
        private void CheckServerStatus()
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(CheckServerStatus));
                    return;
                }

                bool running = IsProcessRunning("nginx") && IsProcessRunning("php-cgi") && IsProcessRunning("mysqld");
                var scheme = ThemeManager.CurrentScheme;

                if (running)
                {
                    lblStatus.Text = LocalizationManager.GetString("ServerRunning");
                    lblStatus.ForeColor = scheme.StatusRunning;
                    btnStart.Enabled = false;
                    btnRestart.Enabled = true;
                    btnStop.Enabled = true;
                }
                else
                {
                    lblStatus.Text = LocalizationManager.GetString("ServerStopped");
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
        // FIX EXISTING SITES FOR TUNNEL
        // =========================
        private void FixExistingSitesForTunnel()
        {
            try
            {
                if (!Directory.Exists(_wwwPath)) return;

                Log("Fixing existing sites for tunnel support...");

                foreach (string dir in Directory.GetDirectories(_wwwPath))
                {
                    string siteName = Path.GetFileName(dir);
                    if (siteName == "default") continue;

                    string wpConfigPath = Path.Combine(dir, "wp-config.php");
                    if (!File.Exists(wpConfigPath)) continue;

                    string content = File.ReadAllText(wpConfigPath);

                    if (content.Contains("WPRONTO DYNAMIC URL FIX"))
                    {
                        Log($"Site {siteName} already has tunnel fix");
                        continue;
                    }

                    Log($"Adding tunnel fix to site: {siteName}");

                    string backupPath = wpConfigPath + ".backup";
                    if (!File.Exists(backupPath))
                        File.Copy(wpConfigPath, backupPath);

                    int insertPos = content.IndexOf("require_once ABSPATH . 'wp-settings.php'");
                    if (insertPos == -1)
                        insertPos = content.IndexOf("require_once( ABSPATH . 'wp-settings.php' )");
                    if (insertPos == -1)
                        insertPos = content.IndexOf("require_once");
                    if (insertPos == -1)
                        insertPos = content.Length;

                    string fixCode = @"
// ========== WPRONTO DYNAMIC URL FIX ==========
// Автоматичне визначення хосту з використанням Output Buffering
$protocol = 'http://';
if (
    (isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] !== 'off') ||
    (isset($_SERVER['HTTP_X_FORWARDED_PROTO']) && $_SERVER['HTTP_X_FORWARDED_PROTO'] === 'https')
) {
    $protocol = 'https://';
    $_SERVER['HTTPS'] = 'on';
}

if (isset($_SERVER['HTTP_HOST'])) {
    $current_host = $_SERVER['HTTP_HOST'];
    
    define('WP_HOME', $protocol . $current_host);
    define('WP_SITEURL', $protocol . $current_host);
    
    ob_start(function($buffer) use ($current_host) {
        $local_domain = '" + siteName + @".wp';
        if ($current_host !== $local_domain) {
            return str_replace($local_domain, $current_host, $buffer);
        }
        return $buffer;
    });
}
// =============================================
";

                    content = content.Insert(insertPos, "\n" + fixCode + "\n");
                    File.WriteAllText(wpConfigPath, content, new UTF8Encoding(false));
                    Log($"Added tunnel fix to {siteName}");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "FixExistingSitesForTunnel");
            }
        }

        // =========================
        // FIX TUNNEL URLS IN DATABASE
        // =========================
        private async Task FixTunnelUrlsInDatabase(string siteName, string tunnelUrl)
        {
            try
            {
                LogTunnel($"Fixing URLs in database for {siteName}...");
                string dbName = $"{siteName}_db";
                string currentUrl = $"http://{siteName}.wp";
                string newUrl = tunnelUrl;

                LogTunnel($"Replacing {currentUrl} with {newUrl} in database...");

                string sql = $@"
UPDATE {dbName}.wp_options 
SET option_value = REPLACE(option_value, '{currentUrl}', '{newUrl}') 
WHERE option_name IN ('siteurl', 'home');

UPDATE {dbName}.wp_posts 
SET guid = REPLACE(guid, '{currentUrl}', '{newUrl}');

UPDATE {dbName}.wp_posts 
SET post_content = REPLACE(post_content, '{currentUrl}', '{newUrl}');

UPDATE {dbName}.wp_postmeta 
SET meta_value = REPLACE(meta_value, '{currentUrl}', '{newUrl}');

DELETE FROM {dbName}.wp_options WHERE option_name LIKE '%transient%';
DELETE FROM {dbName}.wp_options WHERE option_name LIKE '%cache%';
";

                string sqlFile = Path.Combine(Path.GetTempPath(), $"fix_urls_{Guid.NewGuid()}.sql");
                File.WriteAllText(sqlFile, sql, Encoding.UTF8);

                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = _mysqlClientPath,
                    Arguments = $"-u root --database={dbName} < \"{sqlFile}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true
                });

                if (process != null)
                {
                    process.WaitForExit(10000);
                    if (process.ExitCode == 0)
                    {
                        LogTunnel($"✅ Database URLs fixed successfully!");
                    }
                    else
                    {
                        string error = process.StandardError.ReadToEnd();
                        LogTunnel($"❌ Database fix error: {error}");
                    }
                }

                try { File.Delete(sqlFile); } catch { }
            }
            catch (Exception ex)
            {
                LogTunnel($"Error fixing database URLs: {ex.Message}");
                LogError(ex, "FixTunnelUrlsInDatabase");
            }
        }

        // =========================
        // START SERVER
        // =========================
        private async void BtnStart_Click(object sender, EventArgs e)
        {
            try
            {
                btnStart.Enabled = false;

                if (IsProcessRunning("nginx") && IsProcessRunning("php-cgi") && IsProcessRunning("mysqld"))
                {
                    Log(LocalizationManager.GetString("ServerAlreadyRunning"));
                    return;
                }

                if (!await ValidatePreStartupAsync())
                {
                    Log(LocalizationManager.GetString("ServerStartFailed"));
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
                Log(LocalizationManager.GetString("LogServerStopped"));
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
                    MessageBox.Show(LocalizationManager.FormatString("ErrorOpenBrowser", ex.Message), LocalizationManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "BtnPhpMyAdmin_Click");
            }
        }

        // =========================
        // CREATE SITE
        // =========================
        private async void BtnCreateSite_Click(object sender, EventArgs e)
        {
            try
            {
                string siteName = Microsoft.VisualBasic.Interaction.InputBox(
                    LocalizationManager.GetString("SiteNamePrompt"),
                    LocalizationManager.GetString("SiteNameTitle"),
                    LocalizationManager.GetString("DefaultSiteName"));
                if (string.IsNullOrWhiteSpace(siteName)) return;
                siteName = Regex.Replace(siteName, @"[^a-zA-Z0-9\-]", "").ToLower();

                if (string.IsNullOrEmpty(siteName))
                {
                    MessageBox.Show(LocalizationManager.GetString("SiteNameInvalid"),
                        "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                bool isPhpOnlyMode = (siteName == "php");

                if (isPhpOnlyMode)
                {
                    DialogResult result = MessageBox.Show(
                        LocalizationManager.GetString("PhpModeWarning"),
                        "WPronto - PHP Mode",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result != DialogResult.Yes) return;
                }

                string sitePath = Path.Combine(_wwwPath, siteName);
                if (Directory.Exists(sitePath))
                {
                    MessageBox.Show(LocalizationManager.GetString("SiteExists"), "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!isPhpOnlyMode && !Directory.Exists(_templatePath))
                {
                    MessageBox.Show(LocalizationManager.GetString("TemplateNotFound"), "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    Log(LocalizationManager.FormatString("LogSiteCreating", siteName) + $" (Mode: {(isPhpOnlyMode ? "PHP Server" : "WordPress")})");
                    Directory.CreateDirectory(sitePath);

                    if (isPhpOnlyMode)
                    {
                        await CreatePhpOnlySiteAsync(sitePath, siteName);
                    }
                    else
                    {
                        if (!IsValidWordPressInstall(_templatePath))
                        {
                            MessageBox.Show(LocalizationManager.GetString("TemplateInvalid"),
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
                            "        fastcgi_param HTTP_X_FORWARDED_PROTO $http_x_forwarded_proto;\n" +
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

                        if (MessageBox.Show(LocalizationManager.GetString("SiteCreated") + $"\n\n{LocalizationManager.GetString("OpenInstallPage")}?", "WPronto",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            try
                            {
                                Process.Start(new ProcessStartInfo { FileName = $"{siteUrl}/wp-admin/install.php", UseShellExecute = true });
                                Log($"Opened WordPress install page: {siteUrl}/wp-admin/install.php");
                            }
                            catch (Exception ex)
                            {
                                LogError(ex, "BtnCreateSite_Click - Open browser");
                                MessageBox.Show(LocalizationManager.FormatString("ErrorOpenBrowser", ex.Message), LocalizationManager.GetString("Error"),
                                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogError(ex, $"BtnCreateSite_Click - Create {siteName}");
                    MessageBox.Show(LocalizationManager.FormatString("ErrorCreateSite", ex.Message), LocalizationManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        fastcgi_param HTTP_X_FORWARDED_PROTO $http_x_forwarded_proto;
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

                if (MessageBox.Show(LocalizationManager.FormatString("PhpServerCreatedMessage", siteName), "WPronto",
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
                string wpContentPath = Path.Combine(sitePath, "wp-content");
                if (!Directory.Exists(wpContentPath))
                {
                    Directory.CreateDirectory(wpContentPath);
                    Log($"Created wp-content for {siteName}");
                }

                string muPluginsPath = Path.Combine(wpContentPath, "mu-plugins");
                if (!Directory.Exists(muPluginsPath))
                {
                    Directory.CreateDirectory(muPluginsPath);
                    Log($"Created mu-plugins for {siteName}");
                }

                string pluginPath = Path.Combine(muPluginsPath, "wpronto-tunnel-fix.php");
                string pluginContent = @"<?php
/**
 * Plugin Name: WPronto Ultimate Image Fix
 */

$current_host = $_SERVER['HTTP_HOST'] ?? '';
$is_tunnel = (strpos($current_host, 'ngrok-free') !== false);

if ($is_tunnel) {
    $_SERVER['HTTPS'] = 'on';

    $protocol = 'https://';
    $tunnel_url = $protocol . $current_host;
    
    add_filter('option_siteurl', function() { return 'https://' . $_SERVER['HTTP_HOST']; });
    add_filter('option_home', function() { return 'https://' . $_SERVER['HTTP_HOST']; });

    ob_start(function($buffer) use ($current_host) {
        $local_domain = $_SERVER['SERVER_NAME'];
        
        if (!empty($current_host) && $current_host !== $local_domain) {
            $buffer = str_ireplace('http://' . $local_domain, 'https://' . $current_host, $buffer);
            $buffer = str_ireplace('//' . $local_domain, '//' . $current_host, $buffer);
            $buffer = str_ireplace($local_domain, $current_host, $buffer);
        }
        return $buffer;
    });
}
";
                File.WriteAllText(pluginPath, pluginContent, new UTF8Encoding(false));
                Log($"Created tunnel fix plugin for {siteName}");

                StringBuilder content = new StringBuilder();
                content.AppendLine("<?php");
                content.AppendLine("");
                content.AppendLine("// ========== WPRONTO PROXY FIX ==========");
                content.AppendLine("if (isset($_SERVER['HTTP_X_FORWARDED_PROTO']) && $_SERVER['HTTP_X_FORWARDED_PROTO'] === 'https') {");
                content.AppendLine("    $_SERVER['HTTPS'] = 'on';");
                content.AppendLine("}");
                content.AppendLine("if (isset($_SERVER['HTTP_X_FORWARDED_HOST'])) {");
                content.AppendLine("    $_SERVER['HTTP_HOST'] = $_SERVER['HTTP_X_FORWARDED_HOST'];");
                content.AppendLine("}");
                content.AppendLine("// ======================================");
                content.AppendLine("");
                content.AppendLine("/** WPronto Database Configuration */");
                content.AppendLine("define( 'DB_NAME', '" + dbName + "' );");
                content.AppendLine("define( 'DB_USER', 'root' );");
                content.AppendLine("define( 'DB_PASSWORD', '' );");
                content.AppendLine("define( 'DB_HOST', '127.0.0.1' );");
                content.AppendLine("define( 'DB_CHARSET', 'utf8mb4' );");
                content.AppendLine("define( 'DB_COLLATE', '' );");
                content.AppendLine("");
                content.AppendLine("// ========== WPRONTO DYNAMIC URL FIX ==========");
                content.AppendLine("$protocol = (isset($_SERVER['HTTPS']) && $_SERVER['HTTPS'] === 'on') ? 'https://' : 'http://';");
                content.AppendLine("$host = $_SERVER['HTTP_HOST'] ?? 'localhost';");
                content.AppendLine("$site_url = $protocol . $host;");
                content.AppendLine("define('WP_HOME', $site_url);");
                content.AppendLine("define('WP_SITEURL', $site_url);");
                content.AppendLine("// ======================================================");
                content.AppendLine("");
                content.AppendLine("define( 'AUTH_KEY',         '" + GenerateSecureKey(64) + "' );");
                content.AppendLine("define( 'SECURE_AUTH_KEY',  '" + GenerateSecureKey(64) + "' );");
                content.AppendLine("define( 'LOGGED_IN_KEY',    '" + GenerateSecureKey(64) + "' );");
                content.AppendLine("define( 'NONCE_KEY',        '" + GenerateSecureKey(64) + "' );");
                content.AppendLine("define( 'AUTH_SALT',        '" + GenerateSecureKey(64) + "' );");
                content.AppendLine("define( 'SECURE_AUTH_SALT', '" + GenerateSecureKey(64) + "' );");
                content.AppendLine("define( 'LOGGED_IN_SALT',   '" + GenerateSecureKey(64) + "' );");
                content.AppendLine("define( 'NONCE_SALT',       '" + GenerateSecureKey(64) + "' );");
                content.AppendLine("");
                content.AppendLine("$table_prefix = 'wp_';");
                content.AppendLine("define( 'WP_DEBUG', false );");
                content.AppendLine("define( 'WP_DEBUG_DISPLAY', false );");
                content.AppendLine("define( 'WP_MEMORY_LIMIT', '" + Config.MemoryLimit + "M' );");
                content.AppendLine("define( 'WP_MAX_MEMORY_LIMIT', '" + Config.MemoryLimit + "M' );");
                content.AppendLine("");
                content.AppendLine("if ( ! defined( 'ABSPATH' ) ) {");
                content.AppendLine("    define( 'ABSPATH', __DIR__ . '/' );");
                content.AppendLine("}");
                content.AppendLine("require_once ABSPATH . 'wp-settings.php';");

                File.WriteAllText(Path.Combine(sitePath, "wp-config.php"), content.ToString(), new UTF8Encoding(false));
                Log($"Created wp-config.php for {siteName}");

                CreateTunnelFixPlugin();
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
                Log(LocalizationManager.FormatString("LogBackupCreating", siteName));
                string sitePath = Path.Combine(_wwwPath, siteName);
                if (Directory.Exists(sitePath))
                    BackupSiteFiles(sitePath, backupDir);
                else
                    Log($"   Site folder not found: {sitePath}");
                string dbName = $"{siteName}_db";
                BackupDatabase(dbName, backupDir);
                Log($"Backup completed successfully! Location: {backupDir}");
                MessageBox.Show(LocalizationManager.FormatString("BackupComplete", siteName) + $"\n\n{LocalizationManager.GetString("BackupLocation")}\n{backupDir}",
                    LocalizationManager.GetString("BackupCreated"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogError(ex, $"CreateFullBackup - {siteName}");
                MessageBox.Show(LocalizationManager.FormatString("BackupFailed", ex.Message), LocalizationManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        // RESTORE FROM BACKUP
        // =========================
        private async Task RestoreFromBackup(string siteName, BackupInfo backup)
        {
            try
            {
                Log(LocalizationManager.FormatString("LogBackupRestoring", siteName) + $": {backup.Timestamp}");

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
                        bool finished = importProcess.WaitForExit(timeoutMs);
                        double elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;

                        if (!finished)
                        {
                            Log($"   Import timeout after {timeoutMs / 1000} sec — process still running");
                            await DelayAsync(1000);
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
                    "        fastcgi_param HTTP_X_FORWARDED_PROTO $http_x_forwarded_proto;\n" +
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
                MessageBox.Show(LocalizationManager.FormatString("RestoreComplete", siteName) + $"\n\n{LocalizationManager.GetString("BackupLabel")}: {backup.Timestamp}",
                    LocalizationManager.GetString("BackupRestored"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogError(ex, $"RestoreFromBackup - {siteName}");
                MessageBox.Show(LocalizationManager.FormatString("RestoreFailed", ex.Message), LocalizationManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnBackupSite_Click(object sender, EventArgs e)
        {
            try
            {
                if (listSites.SelectedItem == null)
                {
                    MessageBox.Show(LocalizationManager.GetString("SelectSiteFirst"), "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string siteName = listSites.SelectedItem.ToString() ?? "";
                bool isPhpOnly = IsPhpOnlySite(siteName);

                if (isPhpOnly)
                {
                    MessageBox.Show(LocalizationManager.GetString("PhpModeNoBackup"),
                        LocalizationManager.GetString("Info"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (MessageBox.Show(LocalizationManager.FormatString("ConfirmBackup", siteName) + $"\n\n{LocalizationManager.GetString("BackupInfo")}",
                    LocalizationManager.GetString("BackupConfirmTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
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
                    MessageBox.Show(LocalizationManager.GetString("NoBackups"), LocalizationManager.GetString("Info"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                Form backupForm = new Form
                {
                    Text = string.IsNullOrEmpty(preselectedSite) ? LocalizationManager.GetString("SelectBackup") : LocalizationManager.FormatString("RestoreBackupTitle", preselectedSite),
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
                    Text = LocalizationManager.GetString("Restore"),
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
                    Text = LocalizationManager.GetString("Cancel"),
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
                        LocalizationManager.FormatString("ConfirmRestore", siteName) +
                        $"\n\n{LocalizationManager.GetString("BackupLabel")}: {selectedBackup.Timestamp}",
                        LocalizationManager.GetString("RestoreConfirmTitle"),
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
                MessageBox.Show(LocalizationManager.FormatString("ErrorRestoreSite", ex.Message), LocalizationManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                    MessageBox.Show(LocalizationManager.GetString("SelectSiteFirst"), "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                string siteName = listSites.SelectedItem.ToString() ?? "";
                bool isPhpOnly = IsPhpOnlySite(siteName);

                if (MessageBox.Show(LocalizationManager.FormatString("ConfirmDelete", siteName),
                    LocalizationManager.GetString("Confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                    return;

                try
                {
                    Log(LocalizationManager.FormatString("LogSiteDeleting", siteName));
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
                    MessageBox.Show(LocalizationManager.FormatString("SiteDeleted"), "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    LogError(ex, $"BtnDeleteSite_Click - Delete {siteName}");
                    MessageBox.Show(LocalizationManager.FormatString("ErrorDeleteSite", ex.Message), LocalizationManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "BtnDeleteSite_Click");
            }
        }

        // =========================
        // COPY DIRECTORY
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
                ShowTextFile("help.txt", LocalizationManager.GetString("HelpTitle"));
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
                if (!File.Exists(path)) { MessageBox.Show(LocalizationManager.GetString("HelpNotFound"), "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
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

        // =========================
        // TUNNEL MANAGEMENT - NGROK
        // =========================
        private string GetNgrokPath()
        {
            string toolsDir = Path.Combine(_basePath, "tools");
            if (!Directory.Exists(toolsDir))
            {
                Directory.CreateDirectory(toolsDir);
            }
            return Path.Combine(toolsDir, "ngrok.exe");
        }

        private async Task<bool> EnsureNgrokAsync()
        {
            try
            {
                string ngrokPath = GetNgrokPath();
                string toolsDir = Path.GetDirectoryName(ngrokPath);

                LogTunnel($"Checking ngrok at: {ngrokPath}");

                if (!Directory.Exists(toolsDir))
                {
                    LogTunnel($"Creating tools directory: {toolsDir}");
                    Directory.CreateDirectory(toolsDir);
                }

                // ПЕРЕВІРЯЄМО ЧИ ВЖЕ Є NGROK
                if (File.Exists(ngrokPath))
                {
                    LogTunnel($"ngrok.exe exists, size: {new FileInfo(ngrokPath).Length} bytes");

                    // Перевіряємо чи працює
                    try
                    {
                        var testProcess = Process.Start(new ProcessStartInfo
                        {
                            FileName = ngrokPath,
                            Arguments = "version",
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        });

                        if (testProcess != null)
                        {
                            testProcess.WaitForExit(5000);
                            if (testProcess.ExitCode == 0)
                            {
                                string version = testProcess.StandardOutput.ReadToEnd().Trim();
                                LogTunnel($"ngrok.exe version: {version}");
                                LogTunnel($"✅ ngrok.exe is ready to use!");
                                return true; // ВИХОДИМО - ВСЕ ДОБРЕ!
                            }
                            else
                            {
                                LogTunnel($"ngrok.exe test failed with exit code: {testProcess.ExitCode}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogTunnel($"ngrok.exe test failed: {ex.Message}");
                        try { File.Delete(ngrokPath); } catch { }
                    }
                }

                // Якщо дійшли сюди - ngrok немає або пошкоджений, завантажуємо
                LogTunnel("ngrok.exe not found or corrupted, downloading...");
                string downloadUrl = "https://bin.equinox.io/c/bNyj1mQVY4c/ngrok-v3-stable-windows-amd64.zip";

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromMinutes(5);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("WPronto/5.0");

                    LogTunnel($"Downloading from: {downloadUrl}");
                    var response = await client.GetAsync(downloadUrl);
                    response.EnsureSuccessStatusCode();
                    var fileBytes = await response.Content.ReadAsByteArrayAsync();

                    string zipPath = Path.Combine(toolsDir, "ngrok.zip");
                    LogTunnel($"Downloaded {fileBytes.Length} bytes, saving to: {zipPath}");

                    // Зберігаємо zip
                    File.WriteAllBytes(zipPath, fileBytes);

                    LogTunnel("Extracting ngrok.exe from zip...");
                    System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, toolsDir, true);
                    File.Delete(zipPath);
                    LogTunnel($"ngrok.exe extracted to: {ngrokPath}");
                }

                if (File.Exists(ngrokPath))
                {
                    var size = new FileInfo(ngrokPath).Length;
                    LogTunnel($"Download successful! Size: {size} bytes");
                    return true;
                }
                else
                {
                    LogTunnel("Download failed - file not found after extraction");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogTunnel($"Download failed: {ex.Message}");
                LogError(ex, "EnsureNgrokAsync");

                // Перевіряємо чи може є помилка антивіруса
                if (ex.Message.Contains("virus") || ex.Message.Contains("antivirus"))
                {
                    MessageBox.Show(
                        "⚠️ Antivirus software blocked ngrok download!\n\n" +
                        "Please add WPronto folder to antivirus exceptions or\n" +
                        "manually download ngrok from:\n" +
                        "https://ngrok.com/download\n\n" +
                        "Place ngrok.exe in:\n" +
                        Path.Combine(_basePath, "tools") + "\n\n" +
                        "Then restart WPronto.",
                        "Antivirus Blocked Download",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show(
                        LocalizationManager.FormatString("NgrokDownloadFailed", Path.Combine(_basePath, "tools")),
                        LocalizationManager.GetString("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
                return false;
            }
        }

        private async Task<bool> ConfigureNgrokAuthAsync(string ngrokPath)
        {
            try
            {
                string authToken = LoadNgrokAuthToken();

                if (string.IsNullOrEmpty(authToken))
                {
                    LogTunnel("No ngrok auth token found. You need to sign up at ngrok.com and add your token.");

                    DialogResult result = MessageBox.Show(
                        LocalizationManager.GetString("NgrokAuthRequired"),
                        LocalizationManager.GetString("NgrokAuthTitle"),
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        string token = Microsoft.VisualBasic.Interaction.InputBox(
                            LocalizationManager.GetString("NgrokTokenPrompt"),
                            LocalizationManager.GetString("NgrokTokenTitle"),
                            "");

                        if (!string.IsNullOrWhiteSpace(token))
                        {
                            SaveNgrokAuthToken(token.Trim());
                            authToken = token.Trim();
                        }
                        else
                        {
                            LogTunnel("No auth token provided.");
                            return false;
                        }
                    }
                    else
                    {
                        LogTunnel("User declined to enter auth token.");
                        return false;
                    }
                }

                LogTunnel($"Configuring ngrok with auth token...");

                var process = Process.Start(new ProcessStartInfo
                {
                    FileName = ngrokPath,
                    Arguments = $"config add-authtoken {authToken}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    WorkingDirectory = Path.GetDirectoryName(ngrokPath)
                });

                if (process != null)
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();
                    process.WaitForExit(10000);

                    if (process.ExitCode == 0)
                    {
                        LogTunnel("✅ Ngrok authenticated successfully!");
                        LogTunnel($"Output: {output}");
                        return true;
                    }
                    else
                    {
                        LogTunnel($"❌ Ngrok authentication failed: {error}");
                        return false;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                LogTunnel($"Error configuring ngrok auth: {ex.Message}");
                LogError(ex, "ConfigureNgrokAuthAsync");
                return false;
            }
        }

        private string LoadNgrokAuthToken()
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ngrok_token.config");
                if (File.Exists(configPath))
                {
                    return File.ReadAllText(configPath).Trim();
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "LoadNgrokAuthToken");
            }
            return string.Empty;
        }

        private void SaveNgrokAuthToken(string token)
        {
            try
            {
                string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ngrok_token.config");
                File.WriteAllText(configPath, token);
                LogTunnel("Ngrok auth token saved.");
            }
            catch (Exception ex)
            {
                LogError(ex, "SaveNgrokAuthToken");
            }
        }

        // =========================
        // SHARE SITE - NGROK
        // =========================
        private async void BtnShareSite_Click(object sender, EventArgs e)
        {
            try
            {
                LogTunnel("=== START SHARING WITH NGROK ===");
                LogTunnel($"Current directory: {Environment.CurrentDirectory}");
                LogTunnel($"Base path: {_basePath}");

                if (listSites.SelectedItem == null)
                {
                    MessageBox.Show(LocalizationManager.GetString("SelectSiteFirst"), "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (!IsProcessRunning("nginx"))
                {
                    MessageBox.Show(LocalizationManager.GetString("ServerNotRunning"),
                        "WPronto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string siteName = listSites.SelectedItem.ToString();
                LogTunnel($"Selected site: {siteName}");

                if (_isTunnelActive && _currentTunnelSite == siteName)
                {
                    MessageBox.Show(LocalizationManager.FormatString("TunnelAlreadyActive", siteName, _currentTunnelUrl),
                        "Tunnel Active", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (_isTunnelActive)
                {
                    LogTunnel("Stopping existing tunnel...");
                    StopTunnel();
                    await DelayAsync(500);
                }

                LogTunnel("Checking for ngrok...");
                if (!await EnsureNgrokAsync())
                {
                    LogTunnel("Ngrok not available.");
                    return;
                }

                string ngrokPath = GetNgrokPath();
                LogTunnel($"Ngrok path: {ngrokPath}");

                if (!File.Exists(ngrokPath))
                {
                    LogTunnel($"ERROR: ngrok.exe not found at {ngrokPath}");

                    string[] possiblePaths = {
                        Path.Combine(_basePath, "tools", "ngrok.exe"),
                        Path.Combine(Environment.CurrentDirectory, "tools", "ngrok.exe"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "ngrok.exe")
                    };

                    foreach (var path in possiblePaths)
                    {
                        if (File.Exists(path))
                        {
                            LogTunnel($"Found ngrok at: {path}");
                            ngrokPath = path;
                            break;
                        }
                    }

                    if (!File.Exists(ngrokPath))
                    {
                        MessageBox.Show(LocalizationManager.FormatString("NgrokNotFound", string.Join("\n", possiblePaths)),
                            LocalizationManager.GetString("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                if (!await ConfigureNgrokAuthAsync(ngrokPath))
                {
                    MessageBox.Show(
                        LocalizationManager.GetString("NgrokAuthFailed"),
                        LocalizationManager.GetString("Warning"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                _currentTunnelSite = siteName;
                _currentTunnelUrl = string.Empty;
                UpdateTunnelUI();

                string url = $"http://localhost:{_webPort}";
                string args = $"http {url} --host-header={siteName}.wp";

                LogTunnel($"Arguments: {args}");
                LogTunnel($"Working directory: {Path.GetDirectoryName(ngrokPath)}");

                var psi = new ProcessStartInfo
                {
                    FileName = ngrokPath,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8,
                    WorkingDirectory = Path.GetDirectoryName(ngrokPath)
                };

                psi.EnvironmentVariables["PATH"] = Environment.GetEnvironmentVariable("PATH") + ";" + Path.GetDirectoryName(ngrokPath);

                LogTunnel("Creating process...");
                _ngrokProcess = new Process { StartInfo = psi };
                _ngrokProcess.OutputDataReceived += Ngrok_OutputDataReceived;
                _ngrokProcess.ErrorDataReceived += Ngrok_ErrorDataReceived;
                _ngrokProcess.Exited += Ngrok_Exited;
                _ngrokProcess.EnableRaisingEvents = true;

                LogTunnel("Starting process...");
                bool started = _ngrokProcess.Start();
                if (!started)
                {
                    LogTunnel("Failed to start ngrok process!");
                    _isTunnelActive = false;
                    UpdateTunnelUI();
                    MessageBox.Show("Failed to start ngrok process.", LocalizationManager.GetString("Error"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                LogTunnel($"Process started with ID: {_ngrokProcess.Id}");
                _ngrokProcess.BeginOutputReadLine();
                _ngrokProcess.BeginErrorReadLine();

                _isTunnelActive = true;
                _tunnelStatusTimer.Start();
                UpdateTunnelUI();
                LogTunnel("Ngrok tunnel starting... Waiting for URL.");

                int waitTime = 0;
                int maxWait = 60000;
                LogTunnel($"Waiting up to {maxWait / 1000} seconds for URL...");

                while (string.IsNullOrEmpty(_currentTunnelUrl) && waitTime < maxWait && _isTunnelActive)
                {
                    await Task.Delay(1000);
                    waitTime += 1000;

                    if (waitTime % 5000 == 0)
                    {
                        LogTunnel($"Still waiting... {waitTime / 1000}s elapsed");
                        if (_ngrokProcess != null)
                        {
                            try
                            {
                                if (_ngrokProcess.HasExited)
                                {
                                    LogTunnel($"Process exited with code: {_ngrokProcess.ExitCode}");
                                }
                                else
                                {
                                    LogTunnel($"Process is still running (ID: {_ngrokProcess.Id})");
                                }
                            }
                            catch (Exception ex)
                            {
                                LogTunnel($"Could not check process state: {ex.Message}");
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(_currentTunnelUrl) && _isTunnelActive)
                {
                    LogTunnel("Timeout waiting for tunnel URL. Trying API...");
                    await GetNgrokUrlFromApi();
                }
                else if (!string.IsNullOrEmpty(_currentTunnelUrl))
                {
                    LogTunnel($"✅ SUCCESS! Tunnel URL: {_currentTunnelUrl}");
                    LogTunnel("=== SHARING COMPLETED SUCCESSFULLY ===");
                    UpdateTunnelUI();
                    Clipboard.SetText(_currentTunnelUrl);

                    await FixTunnelUrlsInDatabase(siteName, _currentTunnelUrl);
                }
            }
            catch (Exception ex)
            {
                LogTunnel($"❌ ERROR: {ex.Message}");
                LogTunnel($"Stack trace: {ex.StackTrace}");
                LogError(ex, "BtnShareSite_Click");
                _isTunnelActive = false;
                UpdateTunnelUI();
                MessageBox.Show(LocalizationManager.FormatString("ErrorTunnel", ex.Message), LocalizationManager.GetString("Error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Ngrok_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    LogTunnel($"stdout: {e.Data}");
                    LogToFile($"ngrok stdout: {e.Data}");

                    var match = Regex.Match(e.Data, @"https:\/\/[a-zA-Z0-9\-]+\.ngrok-free\.(app|dev)");
                    if (match.Success && string.IsNullOrEmpty(_currentTunnelUrl))
                    {
                        _currentTunnelUrl = match.Value;
                        LogTunnel($"✅✅✅ TUNNEL URL FOUND (stdout): {_currentTunnelUrl}");

                        if (this.InvokeRequired)
                        {
                            this.Invoke(new Action(() =>
                            {
                                UpdateTunnelUI();
                                Clipboard.SetText(_currentTunnelUrl);
                                MessageBox.Show(LocalizationManager.FormatString("TunnelActive", _currentTunnelUrl),
                                    LocalizationManager.GetString("Info"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }));
                        }
                    }

                    if (e.Data.Contains("started tunnel") && string.IsNullOrEmpty(_currentTunnelUrl))
                    {
                        LogTunnel("Ngrok reported tunnel started, looking for URL...");
                    }
                }
            }
            catch (Exception ex)
            {
                LogTunnel($"Error in OutputDataReceived: {ex.Message}");
                LogError(ex, "Ngrok_OutputDataReceived");
            }
        }

        private void Ngrok_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(e.Data))
                {
                    LogTunnel($"stderr: {e.Data}");
                    LogToFile($"ngrok stderr: {e.Data}");

                    var match = Regex.Match(e.Data, @"https:\/\/[a-zA-Z0-9\-]+\.ngrok-free\.(app|dev)");
                    if (match.Success && string.IsNullOrEmpty(_currentTunnelUrl))
                    {
                        _currentTunnelUrl = match.Value;
                        LogTunnel($"✅✅✅ TUNNEL URL FOUND (stderr): {_currentTunnelUrl}");

                        if (this.InvokeRequired)
                        {
                            this.Invoke(new Action(() =>
                            {
                                UpdateTunnelUI();
                                Clipboard.SetText(_currentTunnelUrl);
                                MessageBox.Show(LocalizationManager.FormatString("TunnelActive", _currentTunnelUrl),
                                    LocalizationManager.GetString("Info"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }));
                        }
                    }

                    if (e.Data.Contains("authenticate") || e.Data.Contains("account"))
                    {
                        LogTunnel($"⚠️ Ngrok authentication issue: {e.Data}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogTunnel($"Error in ErrorDataReceived: {ex.Message}");
                LogError(ex, "Ngrok_ErrorDataReceived");
            }
        }

        private async Task GetNgrokUrlFromApi()
        {
            try
            {
                if (_ngrokProcess == null || _ngrokProcess.HasExited)
                {
                    LogTunnel("Ngrok process is not running.");
                    return;
                }

                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    LogTunnel("Attempting to get tunnel URL via API...");
                    var response = await client.GetStringAsync("http://localhost:4040/api/tunnels");

                    using (var doc = JsonDocument.Parse(response))
                    {
                        var root = doc.RootElement;
                        if (root.TryGetProperty("tunnels", out var tunnelsArray) && tunnelsArray.GetArrayLength() > 0)
                        {
                            var tunnel = tunnelsArray[0];
                            if (tunnel.TryGetProperty("public_url", out var urlElement))
                            {
                                _currentTunnelUrl = urlElement.GetString();
                                if (!string.IsNullOrEmpty(_currentTunnelUrl))
                                {
                                    LogTunnel($"✅ Tunnel URL found via API: {_currentTunnelUrl}");
                                    if (this.InvokeRequired)
                                    {
                                        this.Invoke(new Action(() =>
                                        {
                                            UpdateTunnelUI();
                                            Clipboard.SetText(_currentTunnelUrl);
                                            MessageBox.Show(LocalizationManager.FormatString("TunnelActive", _currentTunnelUrl),
                                                LocalizationManager.GetString("Info"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                                        }));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogTunnel($"Error getting tunnel URL via API: {ex.Message}");
                LogError(ex, "GetNgrokUrlFromApi");
            }
        }

        private void Ngrok_Exited(object sender, EventArgs e)
        {
            try
            {
                LogTunnel($"Ngrok process exited. HasExited={_ngrokProcess?.HasExited}, ExitCode={_ngrokProcess?.ExitCode}");
                if (_isTunnelActive)
                {
                    _isTunnelActive = false;
                    _currentTunnelUrl = string.Empty;
                    UpdateTunnelUI();
                    LogTunnel("Tunnel process exited while active");
                }
            }
            catch (Exception ex)
            {
                LogTunnel($"Error in Ngrok_Exited: {ex.Message}");
                LogError(ex, "Ngrok_Exited");
            }
        }

        private void UpdateTunnelUI()
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(UpdateTunnelUI));
                    return;
                }

                var scheme = ThemeManager.CurrentScheme;

                if (_isTunnelActive && !string.IsNullOrEmpty(_currentTunnelUrl))
                {
                    lblTunnelStatus.Text = LocalizationManager.GetString("ShareOpen");
                    lblTunnelStatus.ForeColor = scheme.SuccessColor;
                    lblTunnelUrl.Text = _currentTunnelUrl;
                    lblTunnelUrl.ForeColor = scheme.PrimaryColor;
                    lblTunnelUrl.Cursor = Cursors.Hand;
                    toolTip.SetToolTip(lblTunnelUrl, LocalizationManager.GetString("TunnelCopyHint"));
                }
                else if (_isTunnelActive)
                {
                    lblTunnelStatus.Text = LocalizationManager.GetString("ShareStarting");
                    lblTunnelStatus.ForeColor = scheme.WarningColor;
                    lblTunnelUrl.Text = "⏳ Waiting for URL...";
                    lblTunnelUrl.ForeColor = scheme.TextMuted;
                    lblTunnelUrl.Cursor = Cursors.Default;
                    toolTip.SetToolTip(lblTunnelUrl, LocalizationManager.GetString("TunnelWaitingHint"));
                }
                else
                {
                    lblTunnelStatus.Text = LocalizationManager.GetString("ShareClosed");
                    lblTunnelStatus.ForeColor = scheme.TextMuted;
                    lblTunnelUrl.Text = "";
                    lblTunnelUrl.ForeColor = scheme.TextMuted;
                    lblTunnelUrl.Cursor = Cursors.Default;
                    toolTip.SetToolTip(lblTunnelUrl, "");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "UpdateTunnelUI");
            }
        }

        private void TunnelStatusTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_ngrokProcess == null || _ngrokProcess.HasExited)
                {
                    if (_isTunnelActive)
                    {
                        _isTunnelActive = false;
                        _currentTunnelUrl = string.Empty;
                        UpdateTunnelUI();
                        LogTunnel("Tunnel disconnected (timer)");
                    }
                    _tunnelStatusTimer.Stop();
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "TunnelStatusTimer_Tick");
            }
        }

        private async void BtnRefreshTunnel_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_isTunnelActive || string.IsNullOrEmpty(_currentTunnelSite))
                {
                    MessageBox.Show(LocalizationManager.GetString("TunnelNoActive"), "WPronto",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                LogTunnel("Refreshing tunnel...");
                string siteName = _currentTunnelSite;
                StopTunnel();
                await DelayAsync(1000);
                BtnShareSite_Click(sender, e);
            }
            catch (Exception ex)
            {
                LogError(ex, "BtnRefreshTunnel_Click");
            }
        }

        private void BtnStopSharing_Click(object sender, EventArgs e)
        {
            try
            {
                if (!_isTunnelActive)
                {
                    MessageBox.Show(LocalizationManager.GetString("TunnelNoActiveStop"), "WPronto",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                StopTunnel();
                LogTunnel("Tunnel stopped by user");
                MessageBox.Show(LocalizationManager.GetString("TunnelStopped"), "WPronto",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogError(ex, "BtnStopSharing_Click");
            }
        }

        private void StopTunnel()
        {
            try
            {
                LogTunnel("Stopping tunnel...");
                _tunnelStatusTimer.Stop();

                if (_ngrokProcess != null && !_ngrokProcess.HasExited)
                {
                    try
                    {
                        _ngrokProcess.Kill();
                        _ngrokProcess.WaitForExit(3000);
                        LogTunnel("Tunnel process killed");
                    }
                    catch (Exception ex)
                    {
                        LogTunnel($"Error killing tunnel process: {ex.Message}");
                    }
                }

                _ngrokProcess?.Dispose();
                _ngrokProcess = null;
                _isTunnelActive = false;
                _currentTunnelUrl = string.Empty;
                _currentTunnelSite = string.Empty;

                UpdateTunnelUI();
                LogTunnel("Tunnel stopped");
            }
            catch (Exception ex)
            {
                LogError(ex, "StopTunnel");
            }
        }

        // =========================
        // CREATE TUNNEL FIX PLUGIN
        // =========================
        private void CreateTunnelFixPlugin()
        {
            try
            {
                if (!Directory.Exists(_wwwPath)) return;

                Log("Creating tunnel fix plugin for all sites...");

                foreach (string dir in Directory.GetDirectories(_wwwPath))
                {
                    string siteName = Path.GetFileName(dir);
                    if (siteName == "default") continue;

                    string muPluginsPath = Path.Combine(dir, "wp-content", "mu-plugins");
                    Directory.CreateDirectory(muPluginsPath);

                    string pluginPath = Path.Combine(muPluginsPath, "wpronto-tunnel-fix.php");

                    if (File.Exists(pluginPath))
                    {
                        string content = File.ReadAllText(pluginPath);
                        if (content.Contains("WPronto Ultimate Image Fix"))
                        {
                            Log($"Tunnel fix plugin already exists for {siteName}");
                            continue;
                        }
                    }

                    Log($"Creating tunnel fix plugin for {siteName}...");

                    string pluginContent = @"<?php
/**
 * Plugin Name: WPronto Ultimate Image Fix
 */

$current_host = $_SERVER['HTTP_HOST'] ?? '';
$is_tunnel = (strpos($current_host, 'ngrok-free') !== false);

if ($is_tunnel) {
    $_SERVER['HTTPS'] = 'on';

    $protocol = 'https://';
    $tunnel_url = $protocol . $current_host;
    
    add_filter('option_siteurl', function() { return 'https://' . $_SERVER['HTTP_HOST']; });
    add_filter('option_home', function() { return 'https://' . $_SERVER['HTTP_HOST']; });

    ob_start(function($buffer) use ($current_host) {
        $local_domain = $_SERVER['SERVER_NAME'];
        
        if (!empty($current_host) && $current_host !== $local_domain) {
            $buffer = str_ireplace('http://' . $local_domain, 'https://' . $current_host, $buffer);
            $buffer = str_ireplace('//' . $local_domain, '//' . $current_host, $buffer);
            $buffer = str_ireplace($local_domain, $current_host, $buffer);
        }
        return $buffer;
    });
}
";
                    File.WriteAllText(pluginPath, pluginContent, new UTF8Encoding(false));
                    Log($"Created tunnel fix plugin for {siteName}: {pluginPath}");
                }
            }
            catch (Exception ex)
            {
                LogError(ex, "CreateTunnelFixPlugin");
            }
        }

        // =========================
        // HELPERS
        // =========================
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