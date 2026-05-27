using System.Diagnostics;
using System.Net.Sockets;

namespace WPLaunch
{
    internal class Program
    {
        private const int NGINX_PORT = 80;
        private const int PHP_PORT = 9000;
        private const int MYSQL_PORT = 3306;

        private static Process? _nginxProcess;
        private static Process? _phpProcess;
        private static Process? _mysqlProcess;

        static void Main(string[] args)
        {
            Console.Title = "🚀 WPLaunch - Local WordPress Server";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("╔════════════════════════════════════════╗");
            Console.WriteLine("║     🚀 WPLaunch v0.1.0                 ║");
            Console.WriteLine("║     Local WordPress Server             ║");
            Console.WriteLine("╚════════════════════════════════════════╝");
            Console.ResetColor();
            Console.WriteLine();

            while (true)
            {
                Console.WriteLine("\n[1] ▶️  Start Server");
                Console.WriteLine("[2] ⏹️  Stop Server");
                Console.WriteLine("[3] 🌐 Open localhost");
                Console.WriteLine("[4] 🗄️  Test MySQL Connection");
                Console.WriteLine("[5] ➕ Create New WordPress Site");
                Console.WriteLine("[6] 📋 List Sites");
                Console.WriteLine("[0] ❌ Exit");
                Console.Write("\nSelect option: ");

                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1": StartServer(); break;
                    case "2": StopServer(); break;
                    case "3": OpenBrowser("http://localhost"); break;
                    case "4": TestMySQL(); break;
                    case "5": CreateWordPressSite(); break;
                    case "6": ListSites(); break;
                    case "0": return;
                    default: Console.WriteLine("❌ Invalid option"); break;
                }
            }
        }

        static void StartServer()
        {
            Console.WriteLine("\n🚀 Starting WPLaunch Server...");

            string mysqlPath = @"C:\WPLaunch\core\mysql\bin\mysqld.exe";
            if (File.Exists(mysqlPath))
            {
                var mysqlStartInfo = new ProcessStartInfo
                {
                    FileName = mysqlPath,
                    Arguments = $"--datadir=\"C:\\WPLaunch\\data\\mysql\" --console",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                _mysqlProcess = Process.Start(mysqlStartInfo);
                Console.WriteLine("✅ MySQL started on port " + MYSQL_PORT);
            }
            else
            {
                Console.WriteLine($"❌ MySQL not found at {mysqlPath}");
            }

            string phpPath = @"C:\WPLaunch\core\php\php-cgi.exe";
            if (File.Exists(phpPath))
            {
                var phpStartInfo = new ProcessStartInfo
                {
                    FileName = phpPath,
                    Arguments = $"-b 127.0.0.1:{PHP_PORT} -c \"C:\\WPLaunch\\config\\php\\php.ini\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                _phpProcess = Process.Start(phpStartInfo);
                Console.WriteLine($"✅ PHP-CGI started on port {PHP_PORT}");
            }
            else
            {
                Console.WriteLine($"❌ PHP not found at {phpPath}");
            }

            Thread.Sleep(1000);

            string nginxPath = @"C:\WPLaunch\core\nginx\nginx.exe";
            if (File.Exists(nginxPath))
            {
                var nginxStartInfo = new ProcessStartInfo
                {
                    FileName = nginxPath,
                    Arguments = $"-c \"C:\\WPLaunch\\config\\nginx\\nginx.conf\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                _nginxProcess = Process.Start(nginxStartInfo);
                Console.WriteLine($"✅ Nginx started on port {NGINX_PORT}");
            }
            else
            {
                Console.WriteLine($"❌ Nginx not found at {nginxPath}");
            }

            Thread.Sleep(2000);

            if (IsPortInUse(NGINX_PORT))
            {
                Console.WriteLine("\n✅✅✅ SERVER IS RUNNING! ✅✅✅");
                Console.WriteLine("   Open http://localhost in your browser");
            }
            else
            {
                Console.WriteLine("\n❌ Server failed to start. Check logs:");
                Console.WriteLine("   - C:\\WPLaunch\\logs\\nginx_error.log");
            }
        }

        static void StopServer()
        {
            Console.WriteLine("\n⏹️ Stopping WPLaunch Server...");

            _nginxProcess?.Kill();
            _nginxProcess?.Dispose();
            Console.WriteLine("✅ Nginx stopped");

            _phpProcess?.Kill();
            _phpProcess?.Dispose();
            Console.WriteLine("✅ PHP stopped");

            _mysqlProcess?.Kill();
            _mysqlProcess?.Dispose();
            Console.WriteLine("✅ MySQL stopped");

            Console.WriteLine("✅ All services stopped");
        }

        static void TestMySQL()
        {
            Console.WriteLine("\n🔍 Testing MySQL connection...");
            try
            {
                using var client = new TcpClient();
                var result = client.BeginConnect("127.0.0.1", MYSQL_PORT, null, null);
                var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(1000));
                Console.WriteLine(success ? "✅ MySQL is running" : "❌ MySQL is not responding");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        static void CreateWordPressSite()
        {
            Console.Write("\n📝 Enter site name: ");
            string? siteName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(siteName)) return;

            siteName = System.Text.RegularExpressions.Regex.Replace(siteName, @"[^a-zA-Z0-9\-]", "");
            string sitePath = $@"C:\WPLaunch\www\{siteName}";

            if (Directory.Exists(sitePath))
            {
                Console.WriteLine("❌ Site exists!");
                return;
            }

            Directory.CreateDirectory(sitePath);
            File.WriteAllText(Path.Combine(sitePath, "index.php"), "<?php echo '<h1>🚀 Site: " + siteName + "</h1>'; phpinfo(); ?>");

            string nginxConf = $@"server {{
    listen 80;
    server_name {siteName}.wp;
    root C:/WPLaunch/www/{siteName};
    index index.php;
    location ~ \.php$ {{
        fastcgi_pass 127.0.0.1:9000;
        fastcgi_param SCRIPT_FILENAME $document_root$fastcgi_script_name;
        include fastcgi_params;
    }}
}}";
            Directory.CreateDirectory(@"C:\WPLaunch\config\nginx\sites");
            File.WriteAllText($@"C:\WPLaunch\config\nginx\sites\{siteName}.conf", nginxConf);

            Console.WriteLine($"\n✅ Site '{siteName}' created!");
            Console.WriteLine($"🌐 http://{siteName}.wp");
            Console.WriteLine("\n⚠️ Add to C:\\Windows\\System32\\drivers\\etc\\hosts:");
            Console.WriteLine($"   127.0.0.1 {siteName}.wp");
        }

        static void ListSites()
        {
            if (!Directory.Exists(@"C:\WPLaunch\www"))
            {
                Console.WriteLine("No sites found");
                return;
            }

            var sites = Directory.GetDirectories(@"C:\WPLaunch\www");
            Console.WriteLine("\n📋 Your Sites:");
            foreach (var site in sites)
                Console.WriteLine($"   🌐 {Path.GetFileName(site)}.wp");
        }

        static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
                Console.WriteLine($"🌐 Opened: {url}");
            }
            catch { Console.WriteLine("❌ Failed to open browser"); }
        }

        static bool IsPortInUse(int port)
        {
            try
            {
                using var client = new TcpClient();
                var result = client.BeginConnect("127.0.0.1", port, null, null);
                return result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(300));
            }
            catch { return false; }
        }
    }
}