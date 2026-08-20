using CustomTools.Properties;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CustomTools
{
    public partial class FrmUpdateAvailable : Form
    {
        public FrmUpdateAvailable()
        {
            InitializeComponent();

            lblCurrentVersion.Text = string.Format(Strings.Update_CurrentVersion, CurrentVersion);
            lblNewVersion.Text = string.Format(Strings.Update_NewVersion, UpdateChecker.AvailableVersion);
        }

        private static string CurrentVersion
        {
            get
            {
                var assembly = Assembly.GetExecutingAssembly();
                var versionAttr = (AssemblyInformationalVersionAttribute)
                    Attribute.GetCustomAttribute(assembly, typeof(AssemblyInformationalVersionAttribute));
                return versionAttr?.InformationalVersion ?? assembly.GetName().Version.ToString();
            }
        }

        private void llReleaseNotes_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("https://github.com/askowron/CustomTools/releases/tag/v" + UpdateChecker.AvailableVersion);
        }

        private async void btnUpdateNow_Click(object sender, EventArgs e)
        {
            btnUpdateNow.Enabled = false;
            try
            {
                string installerPath = await DownloadInstallerAsync(UpdateChecker.AvailableDownloadUrl);
                string logPath = Path.Combine(Path.GetTempPath(), "CustomToolsUpdate.log");

                var startInfo = new ProcessStartInfo
                {
                    FileName = installerPath,
                    Arguments = $"/LOG=\"{logPath}\" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
                    UseShellExecute = true,
                    Verb = "runas"
                };
                Process.Start(startInfo);

                // CloseApplications/RestartApplications in the .iss detect this running
                // instance via Restart Manager (it locks its own .exe in {app}), close it,
                // and relaunch it after install — no Application.Exit() call here,
                // that would race the installer's own detection.
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, string.Format(Strings.Update_DownloadFailed, ex.Message), Strings.Update_Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnUpdateNow.Enabled = true;
            }
        }

        private static async Task<string> DownloadInstallerAsync(string downloadUrl)
        {
            string installerPath = Path.Combine(Path.GetTempPath(), "CustomToolsSetup.exe");

            using (var client = new HttpClient { Timeout = TimeSpan.FromMinutes(2) })
            using (var responseStream = await client.GetStreamAsync(downloadUrl))
            using (var fileStream = File.Create(installerPath))
            {
                await responseStream.CopyToAsync(fileStream);
            }

            return installerPath;
        }

        private void btnRemindLater_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btnSkipVersion_Click(object sender, EventArgs e)
        {
            UpdateChecker.SkipVersion(UpdateChecker.AvailableVersion);
            Close();
        }
    }
}
