using CTPlugins;
using System.Linq;

namespace CustomTools
{
    public partial class FrmOptions : FrmTemplateDialog
    {
        public FrmOptions()
        {
            InitializeComponent();

            chkStartWithWindows.Checked = StartupManager.IsEnabled;

            string savedLanguageCode = LanguageManager.GetSavedLanguageCode();
            cmbLanguage.Items.AddRange(LanguageManager.GetSupportedLanguages().Select(l => (object)l).ToArray());
            cmbLanguage.SelectedIndex = Enumerable.Range(0, cmbLanguage.Items.Count)
                .FirstOrDefault(i => ((LanguageManager.Language)cmbLanguage.Items[i]).Code == savedLanguageCode);

            OKClicked += delegate
            {
                StartupManager.SetEnabled(chkStartWithWindows.Checked);

                string selectedCode = ((LanguageManager.Language)cmbLanguage.SelectedItem).Code;
                LanguageManager.SetSavedLanguageCode(selectedCode);
                LanguageManager.Apply(selectedCode);
            };
        }
    }
}
