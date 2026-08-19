using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace CustomTools
{
    internal static class UpdateChecker
    {
        private const string RegistryKeyPath = @"SOFTWARE\Appit\CustomTools";
        private const string CheckForUpdatesValueName = "CheckForUpdates";
        private const string LastCheckValueName = "UpdateLastCheckUtc";
        private const string SkippedVersionValueName = "UpdateSkippedVersion";
        private const string LatestReleaseApiUrl = "https://api.github.com/repos/askowron/CustomTools/releases/latest";
        private const string SetupAssetName = "CustomToolsSetup.exe";
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);

        private static System.Windows.Forms.Timer _timer;

        public static string AvailableVersion { get; private set; }
        public static string AvailableDownloadUrl { get; private set; }

        public static bool IsEnabled
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false))
                {
                    object value = key?.GetValue(CheckForUpdatesValueName);
                    return value == null || Convert.ToInt32(value) != 0;
                }
            }
        }

        public static void SetEnabled(bool enabled)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
            {
                key.SetValue(CheckForUpdatesValueName, enabled ? 1 : 0, RegistryValueKind.DWord);
            }
        }

        public static void SkipVersion(string version)
        {
            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
            {
                key.SetValue(SkippedVersionValueName, version ?? "");
            }
        }

        private static string SkippedVersion
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false))
                {
                    return key?.GetValue(SkippedVersionValueName) as string ?? "";
                }
            }
        }

        private static DateTime? LastCheckUtc
        {
            get
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, writable: false))
                {
                    string stored = key?.GetValue(LastCheckValueName) as string;
                    DateTime parsed;
                    if (stored != null && DateTime.TryParse(stored, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out parsed))
                        return parsed;
                    return null;
                }
            }
            set
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryKeyPath))
                {
                    key.SetValue(LastCheckValueName, value.Value.ToString("o", CultureInfo.InvariantCulture));
                }
            }
        }

        public static void StartBackgroundChecking(NotifyIcon trayIcon)
        {
            _ = CheckIfDueAsync(trayIcon);

            // System.Windows.Forms.Timer fires its Tick event on the UI thread (via the
            // message loop), unlike System.Threading.Timer whose callback runs on a
            // thread-pool thread with no SynchronizationContext. That matters here because
            // CheckIfDueAsync/CheckAsync touch the NotifyIcon (not thread-safe cross-thread)
            // and resolve localized Strings.* under the current UI culture, which is only
            // set correctly on the UI thread.
            _timer = new System.Windows.Forms.Timer();
            _timer.Interval = (int)CheckInterval.TotalMilliseconds;
            _timer.Tick += (s, e) => { var ignored = CheckIfDueAsync(trayIcon); };
            _timer.Start();
        }

        public static Task<bool?> CheckNowAsync(NotifyIcon trayIcon)
        {
            return CheckAsync(trayIcon);
        }

        private static async Task CheckIfDueAsync(NotifyIcon trayIcon)
        {
            if (!IsEnabled)
                return;

            DateTime? lastCheck = LastCheckUtc;
            if (lastCheck.HasValue && DateTime.UtcNow - lastCheck.Value < CheckInterval)
                return;

            await CheckAsync(trayIcon);
        }

        // Returns true if a newer, non-skipped version was found (balloon shown),
        // false if the check completed but nothing new applies, or null if the
        // check itself failed (network, parse, missing asset). Always stamps
        // LastCheckUtc so a single failed attempt doesn't cause retry storms on
        // every subsequent launch within the same day.
        private static async Task<bool?> CheckAsync(NotifyIcon trayIcon)
        {
            try
            {
                string json;
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(4) })
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("CustomTools-UpdateChecker");
                    json = await client.GetStringAsync(LatestReleaseApiUrl);
                }

                var serializer = new JavaScriptSerializer();
                var release = serializer.DeserializeObject(json) as Dictionary<string, object>;
                if (release == null)
                    return false;

                string tagName = release.ContainsKey("tag_name") ? release["tag_name"] as string : null;
                if (string.IsNullOrEmpty(tagName))
                    return false;

                string remoteVersion = tagName.TrimStart('v', 'V');

                string downloadUrl = null;
                var assets = release.ContainsKey("assets") ? release["assets"] as System.Collections.IEnumerable : null;
                if (assets != null)
                {
                    foreach (object assetObj in assets)
                    {
                        var asset = assetObj as Dictionary<string, object>;
                        if (asset == null)
                            continue;

                        string name = asset.ContainsKey("name") ? asset["name"] as string : null;
                        if (string.Equals(name, SetupAssetName, StringComparison.OrdinalIgnoreCase))
                        {
                            downloadUrl = asset.ContainsKey("browser_download_url") ? asset["browser_download_url"] as string : null;
                            break;
                        }
                    }
                }

                if (downloadUrl == null)
                    return false;

                Version remote = ParseVersion(remoteVersion);
                Version local = ParseVersion(CurrentVersion);
                if (remote == null || local == null || remote <= local)
                    return false;

                if (string.Equals(remoteVersion, SkippedVersion, StringComparison.OrdinalIgnoreCase))
                    return false;

                AvailableVersion = remoteVersion;
                AvailableDownloadUrl = downloadUrl;

                trayIcon.BalloonTipTitle = Properties.Strings.Update_BalloonTitle;
                trayIcon.BalloonTipText = string.Format(Properties.Strings.Update_BalloonText, remoteVersion);
                trayIcon.ShowBalloonTip(10000);

                return true;
            }
            catch
            {
                return null;
            }
            finally
            {
                try
                {
                    LastCheckUtc = DateTime.UtcNow;
                }
                catch
                {
                    // Swallow: a registry-write failure during cleanup must not escape
                    // CheckAsync either, since callers rely on this method never throwing.
                }
            }
        }

        private static string CurrentVersion
        {
            get
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var versionAttr = (System.Reflection.AssemblyInformationalVersionAttribute)
                    Attribute.GetCustomAttribute(assembly, typeof(System.Reflection.AssemblyInformationalVersionAttribute));
                return versionAttr?.InformationalVersion ?? assembly.GetName().Version.ToString();
            }
        }

        private static Version ParseVersion(string text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            string normalized = text.Contains(".") ? text : text + ".0";
            Version result;
            return Version.TryParse(normalized, out result) ? result : null;
        }
    }
}
