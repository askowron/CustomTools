using CTPlugins;

namespace CustomTools
{
    public partial class FrmOptions : FrmTemplateDialog
    {
        public FrmOptions()
        {
            InitializeComponent();

            chkStartWithWindows.Checked = StartupManager.IsEnabled;

            OKClicked += delegate { StartupManager.SetEnabled(chkStartWithWindows.Checked); };
        }
    }
}
