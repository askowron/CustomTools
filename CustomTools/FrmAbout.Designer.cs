namespace CustomTools
{
    partial class FrmAbout
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblProductName = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.lblAuthorCaption = new System.Windows.Forms.Label();
            this.llAuthor = new System.Windows.Forms.LinkLabel();
            this.lblLicenseCaption = new System.Windows.Forms.Label();
            this.llLicense = new System.Windows.Forms.LinkLabel();
            this.llSupport = new System.Windows.Forms.LinkLabel();
            this.SuspendLayout();
            //
            // lblProductName
            //
            this.lblProductName.AutoSize = true;
            this.lblProductName.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblProductName.Location = new System.Drawing.Point(12, 20);
            this.lblProductName.Name = "lblProductName";
            this.lblProductName.Size = new System.Drawing.Size(120, 21);
            this.lblProductName.TabIndex = 0;
            this.lblProductName.Text = "CustomTools";
            //
            // lblVersion
            //
            this.lblVersion.AutoSize = true;
            this.lblVersion.Location = new System.Drawing.Point(12, 52);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(60, 13);
            this.lblVersion.TabIndex = 1;
            this.lblVersion.Text = "Version";
            //
            // lblAuthorCaption
            //
            this.lblAuthorCaption.AutoSize = true;
            this.lblAuthorCaption.Location = new System.Drawing.Point(12, 82);
            this.lblAuthorCaption.Name = "lblAuthorCaption";
            this.lblAuthorCaption.Size = new System.Drawing.Size(41, 13);
            this.lblAuthorCaption.TabIndex = 2;
            this.lblAuthorCaption.Text = Properties.Strings.About_Author;
            //
            // llAuthor
            //
            this.llAuthor.AutoSize = true;
            this.llAuthor.Location = new System.Drawing.Point(90, 82);
            this.llAuthor.Name = "llAuthor";
            this.llAuthor.Size = new System.Drawing.Size(90, 13);
            this.llAuthor.TabIndex = 3;
            this.llAuthor.TabStop = true;
            this.llAuthor.Text = "Adam Skowroński";
            this.llAuthor.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.llAuthor_LinkClicked);
            //
            // lblLicenseCaption
            //
            this.lblLicenseCaption.AutoSize = true;
            this.lblLicenseCaption.Location = new System.Drawing.Point(12, 104);
            this.lblLicenseCaption.Name = "lblLicenseCaption";
            this.lblLicenseCaption.Size = new System.Drawing.Size(46, 13);
            this.lblLicenseCaption.TabIndex = 4;
            this.lblLicenseCaption.Text = Properties.Strings.About_License;
            //
            // llLicense
            //
            this.llLicense.AutoSize = true;
            this.llLicense.Location = new System.Drawing.Point(90, 104);
            this.llLicense.Name = "llLicense";
            this.llLicense.Size = new System.Drawing.Size(60, 13);
            this.llLicense.TabIndex = 5;
            this.llLicense.TabStop = true;
            this.llLicense.Text = "GPL-3.0";
            this.llLicense.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.llLicense_LinkClicked);
            //
            // llSupport
            //
            this.llSupport.AutoSize = true;
            this.llSupport.Location = new System.Drawing.Point(12, 132);
            this.llSupport.Name = "llSupport";
            this.llSupport.Size = new System.Drawing.Size(110, 13);
            this.llSupport.TabIndex = 6;
            this.llSupport.TabStop = true;
            this.llSupport.Text = "☕ Buy Me a Coffee";
            this.llSupport.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.llSupport_LinkClicked);
            //
            // FrmAbout
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(300, 210);
            this.Controls.Add(this.llSupport);
            this.Controls.Add(this.llLicense);
            this.Controls.Add(this.lblLicenseCaption);
            this.Controls.Add(this.llAuthor);
            this.Controls.Add(this.lblAuthorCaption);
            this.Controls.Add(this.lblVersion);
            this.Controls.Add(this.lblProductName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmAbout";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = Properties.Strings.About_Title;
            this.Controls.SetChildIndex(this.lblProductName, 0);
            this.Controls.SetChildIndex(this.lblVersion, 0);
            this.Controls.SetChildIndex(this.lblAuthorCaption, 0);
            this.Controls.SetChildIndex(this.llAuthor, 0);
            this.Controls.SetChildIndex(this.lblLicenseCaption, 0);
            this.Controls.SetChildIndex(this.llLicense, 0);
            this.Controls.SetChildIndex(this.llSupport, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblProductName;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.Label lblAuthorCaption;
        private System.Windows.Forms.LinkLabel llAuthor;
        private System.Windows.Forms.Label lblLicenseCaption;
        private System.Windows.Forms.LinkLabel llLicense;
        private System.Windows.Forms.LinkLabel llSupport;
    }
}
