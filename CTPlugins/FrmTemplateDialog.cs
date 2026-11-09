using System;
using System.Windows.Forms;

namespace CTPlugins
{
    public partial class FrmTemplateDialog : Form
    { 
        public event EventHandler OKClicked;
        public event EventHandler CancelClicked;

        public FrmTemplateDialog()
        {
            InitializeComponent();
        }

        /// <summary>Applied at load because derived forms add their controls after this constructor.</summary>
        protected override void OnLoad(EventArgs e)
        {
            if (!DesignMode)
                ApplyTheme(ThemePalette.Current);

            base.OnLoad(e);
        }

        public void ApplyTheme(ThemePalette palette)
        {
            DialogTheme.Apply(this, palette);
            panel1.BackColor = palette.Footer;
            pnlFooterLine.BackColor = palette.Border;
        }

        /// <summary>For dialogs with nothing to confirm: leaves one button, which Esc also triggers.</summary>
        protected void UseSingleCloseButton()
        {
            btnCancel.Visible = false;
            btnAccept.Location = btnCancel.Location;
            CancelButton = btnAccept;
        }

        protected void btnOK_Click(object sender, EventArgs e)
        {
            if(OKClicked != null)
                OKClicked(this, e);

            DialogResult = DialogResult.OK;
            Close();
        }

        protected void btnCancel_Click(object sender, EventArgs e)
        {
            if(CancelClicked != null)
                CancelClicked(this, e);

            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
