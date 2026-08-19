using CTPlugins;
using System.Linq;
using System.Windows.Forms;

namespace CustomTools
{
    public partial class FrmOptions : FrmTemplateDialog
    {
        private readonly NotifyIcon _trayIcon;

        public FrmOptions(NotifyIcon trayIcon)
        {
            InitializeComponent();

            _trayIcon = trayIcon;

            chkStartWithWindows.Checked = StartupManager.IsEnabled;
            chkCheckForUpdates.Checked = UpdateChecker.IsEnabled;

            string savedLanguageCode = LanguageManager.GetSavedLanguageCode();
            cmbLanguage.Items.AddRange(LanguageManager.GetSupportedLanguages().Select(l => (object)l).ToArray());
            cmbLanguage.SelectedIndex = Enumerable.Range(0, cmbLanguage.Items.Count)
                .FirstOrDefault(i => ((LanguageManager.Language)cmbLanguage.Items[i]).Code == savedLanguageCode);

            OKClicked += delegate
            {
                StartupManager.SetEnabled(chkStartWithWindows.Checked);
                UpdateChecker.SetEnabled(chkCheckForUpdates.Checked);

                string selectedCode = ((LanguageManager.Language)cmbLanguage.SelectedItem).Code;
                LanguageManager.SetSavedLanguageCode(selectedCode);
                LanguageManager.Apply(selectedCode);
            };
        }

        private async void btnCheckNow_Click(object sender, System.EventArgs e)
        {
            btnCheckNow.Enabled = false;
            lblCheckNowStatus.Text = "";

            bool? found = await UpdateChecker.CheckNowAsync(_trayIcon);

            if (found == null)
                lblCheckNowStatus.Text = Properties.Strings.Options_CheckNow_Failed;
            else if (found == false)
                lblCheckNowStatus.Text = Properties.Strings.Options_CheckNow_NoUpdate;

            btnCheckNow.Enabled = true;
        }
    }
}
