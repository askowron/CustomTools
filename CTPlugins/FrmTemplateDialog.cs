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
