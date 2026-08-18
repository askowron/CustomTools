using CTPlugins;
using CustomTools.Properties;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace CustomTools
{
    public partial class FrmLicense : FrmTemplateDialog
    {
        private const string RawLicenseUrl = "https://raw.githubusercontent.com/askowron/CustomTools/master/LICENSE.txt";

        public FrmLicense()
        {
            InitializeComponent();

            rtbLicense.Rtf = Resources.LicenseRtf;

            Shown += FrmLicense_Shown;
        }

        private async void FrmLicense_Shown(object sender, EventArgs e)
        {
            string latestLicenseText = await TryDownloadLatestLicenseAsync();
            if (latestLicenseText != null && !IsDisposed)
            {
                rtbLicense.Text = latestLicenseText;
            }
        }

        private static async Task<string> TryDownloadLatestLicenseAsync()
        {
            try
            {
                using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(4) })
                {
                    return await client.GetStringAsync(RawLicenseUrl);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
