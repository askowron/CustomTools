namespace CustomTools
{
    partial class FrmUpdateAvailable
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
            this.lblCurrentVersion = new System.Windows.Forms.Label();
            this.lblNewVersion = new System.Windows.Forms.Label();
            this.llReleaseNotes = new System.Windows.Forms.LinkLabel();
            this.btnSkipVersion = new System.Windows.Forms.Button();
            this.btnRemindLater = new System.Windows.Forms.Button();
            this.btnUpdateNow = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lblCurrentVersion
            //
            this.lblCurrentVersion.AutoSize = true;
            this.lblCurrentVersion.Location = new System.Drawing.Point(12, 15);
            this.lblCurrentVersion.Name = "lblCurrentVersion";
            this.lblCurrentVersion.Size = new System.Drawing.Size(90, 13);
            this.lblCurrentVersion.TabIndex = 0;
            this.lblCurrentVersion.Text = "Current version:";
            //
            // lblNewVersion
            //
            this.lblNewVersion.AutoSize = true;
            this.lblNewVersion.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);
            this.lblNewVersion.Location = new System.Drawing.Point(12, 35);
            this.lblNewVersion.Name = "lblNewVersion";
            this.lblNewVersion.Size = new System.Drawing.Size(75, 13);
            this.lblNewVersion.TabIndex = 1;
            this.lblNewVersion.Text = "New version:";
            //
            // llReleaseNotes
            //
            this.llReleaseNotes.AutoSize = true;
            this.llReleaseNotes.Location = new System.Drawing.Point(12, 60);
            this.llReleaseNotes.Name = "llReleaseNotes";
            this.llReleaseNotes.Size = new System.Drawing.Size(100, 13);
            this.llReleaseNotes.TabIndex = 2;
            this.llReleaseNotes.TabStop = true;
            this.llReleaseNotes.Text = Properties.Strings.Update_ReleaseNotes;
            this.llReleaseNotes.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.llReleaseNotes_LinkClicked);
            //
            // btnSkipVersion
            //
            this.btnSkipVersion.Location = new System.Drawing.Point(12, 140);
            this.btnSkipVersion.Name = "btnSkipVersion";
            this.btnSkipVersion.Size = new System.Drawing.Size(100, 23);
            this.btnSkipVersion.TabIndex = 3;
            this.btnSkipVersion.Text = Properties.Strings.Update_BtnSkip;
            this.btnSkipVersion.UseVisualStyleBackColor = true;
            this.btnSkipVersion.Click += new System.EventHandler(this.btnSkipVersion_Click);
            //
            // btnRemindLater
            //
            this.btnRemindLater.Location = new System.Drawing.Point(118, 140);
            this.btnRemindLater.Name = "btnRemindLater";
            this.btnRemindLater.Size = new System.Drawing.Size(100, 23);
            this.btnRemindLater.TabIndex = 4;
            this.btnRemindLater.Text = Properties.Strings.Update_BtnRemindLater;
            this.btnRemindLater.UseVisualStyleBackColor = true;
            this.btnRemindLater.Click += new System.EventHandler(this.btnRemindLater_Click);
            //
            // btnUpdateNow
            //
            this.btnUpdateNow.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUpdateNow.Location = new System.Drawing.Point(224, 140);
            this.btnUpdateNow.Name = "btnUpdateNow";
            this.btnUpdateNow.Size = new System.Drawing.Size(100, 23);
            this.btnUpdateNow.TabIndex = 5;
            this.btnUpdateNow.Text = Properties.Strings.Update_BtnUpdateNow;
            this.btnUpdateNow.UseVisualStyleBackColor = true;
            this.btnUpdateNow.Click += new System.EventHandler(this.btnUpdateNow_Click);
            //
            // FrmUpdateAvailable
            //
            this.AcceptButton = this.btnUpdateNow;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(336, 175);
            this.Controls.Add(this.btnUpdateNow);
            this.Controls.Add(this.btnRemindLater);
            this.Controls.Add(this.btnSkipVersion);
            this.Controls.Add(this.llReleaseNotes);
            this.Controls.Add(this.lblNewVersion);
            this.Controls.Add(this.lblCurrentVersion);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmUpdateAvailable";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = Properties.Strings.Update_Title;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblCurrentVersion;
        private System.Windows.Forms.Label lblNewVersion;
        private System.Windows.Forms.LinkLabel llReleaseNotes;
        private System.Windows.Forms.Button btnSkipVersion;
        private System.Windows.Forms.Button btnRemindLater;
        private System.Windows.Forms.Button btnUpdateNow;
    }
}
