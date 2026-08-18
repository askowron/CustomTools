using CTPlugins;
using CustomTools.Properties;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace CustomTools
{
    public partial class FrmAbout : FrmTemplateDialog
    {
        private const string AuthorUrl = "https://github.com/askowron";
        private const string SupportUrl = "https://buycoffee.to/rico";

        public FrmAbout()
        {
            InitializeComponent();

            Assembly assembly = Assembly.GetExecutingAssembly();

            var productAttr = (AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute));
            lblProductName.Text = productAttr?.Product ?? assembly.GetName().Name;

            var versionAttr = (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyInformationalVersionAttribute));
            string version = versionAttr?.InformationalVersion ?? assembly.GetName().Version.ToString();
            lblVersion.Text = string.Format(Strings.About_Version, version);
        }

        private void llAuthor_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(AuthorUrl);
        }

        private void llLicense_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            using (FrmLicense form = new FrmLicense())
            {
                form.ShowDialog(this);
            }
        }

        private void llSupport_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(SupportUrl);
        }
    }
}
