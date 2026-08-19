namespace CustomTools
{
    partial class FrmOptions
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
            this.chkStartWithWindows = new System.Windows.Forms.CheckBox();
            this.lblLanguage = new System.Windows.Forms.Label();
            this.cmbLanguage = new System.Windows.Forms.ComboBox();
            this.chkCheckForUpdates = new System.Windows.Forms.CheckBox();
            this.btnCheckNow = new System.Windows.Forms.Button();
            this.lblCheckNowStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            //
            // chkStartWithWindows
            //
            this.chkStartWithWindows.AutoSize = true;
            this.chkStartWithWindows.Location = new System.Drawing.Point(12, 20);
            this.chkStartWithWindows.Name = "chkStartWithWindows";
            this.chkStartWithWindows.Size = new System.Drawing.Size(152, 17);
            this.chkStartWithWindows.TabIndex = 0;
            this.chkStartWithWindows.Text = Properties.Strings.Options_StartWithWindows;
            this.chkStartWithWindows.UseVisualStyleBackColor = true;
            //
            // lblLanguage
            //
            this.lblLanguage.AutoSize = true;
            this.lblLanguage.Location = new System.Drawing.Point(12, 53);
            this.lblLanguage.Name = "lblLanguage";
            this.lblLanguage.Size = new System.Drawing.Size(46, 13);
            this.lblLanguage.TabIndex = 1;
            this.lblLanguage.Text = Properties.Strings.Options_Language;
            //
            // cmbLanguage
            //
            this.cmbLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbLanguage.FormattingEnabled = true;
            this.cmbLanguage.Location = new System.Drawing.Point(12, 69);
            this.cmbLanguage.Name = "cmbLanguage";
            this.cmbLanguage.Size = new System.Drawing.Size(200, 21);
            this.cmbLanguage.TabIndex = 2;
            //
            // chkCheckForUpdates
            //
            this.chkCheckForUpdates.AutoSize = true;
            this.chkCheckForUpdates.Location = new System.Drawing.Point(12, 100);
            this.chkCheckForUpdates.Name = "chkCheckForUpdates";
            this.chkCheckForUpdates.Size = new System.Drawing.Size(180, 17);
            this.chkCheckForUpdates.TabIndex = 3;
            this.chkCheckForUpdates.Text = Properties.Strings.Options_CheckForUpdates;
            this.chkCheckForUpdates.UseVisualStyleBackColor = true;
            //
            // btnCheckNow
            //
            this.btnCheckNow.Location = new System.Drawing.Point(12, 125);
            this.btnCheckNow.Name = "btnCheckNow";
            this.btnCheckNow.Size = new System.Drawing.Size(100, 23);
            this.btnCheckNow.TabIndex = 4;
            this.btnCheckNow.Text = Properties.Strings.Options_CheckNow;
            this.btnCheckNow.UseVisualStyleBackColor = true;
            this.btnCheckNow.Click += new System.EventHandler(this.btnCheckNow_Click);
            //
            // lblCheckNowStatus
            //
            this.lblCheckNowStatus.AutoSize = true;
            this.lblCheckNowStatus.Location = new System.Drawing.Point(120, 130);
            this.lblCheckNowStatus.Name = "lblCheckNowStatus";
            this.lblCheckNowStatus.Size = new System.Drawing.Size(0, 13);
            this.lblCheckNowStatus.TabIndex = 5;
            //
            // FrmOptions
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(280, 200);
            this.Controls.Add(this.lblCheckNowStatus);
            this.Controls.Add(this.btnCheckNow);
            this.Controls.Add(this.chkCheckForUpdates);
            this.Controls.Add(this.cmbLanguage);
            this.Controls.Add(this.lblLanguage);
            this.Controls.Add(this.chkStartWithWindows);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmOptions";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = Properties.Strings.Options_Title;
            this.Controls.SetChildIndex(this.chkStartWithWindows, 0);
            this.Controls.SetChildIndex(this.lblLanguage, 0);
            this.Controls.SetChildIndex(this.cmbLanguage, 0);
            this.Controls.SetChildIndex(this.chkCheckForUpdates, 0);
            this.Controls.SetChildIndex(this.btnCheckNow, 0);
            this.Controls.SetChildIndex(this.lblCheckNowStatus, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chkStartWithWindows;
        private System.Windows.Forms.Label lblLanguage;
        private System.Windows.Forms.ComboBox cmbLanguage;
        private System.Windows.Forms.CheckBox chkCheckForUpdates;
        private System.Windows.Forms.Button btnCheckNow;
        private System.Windows.Forms.Label lblCheckNowStatus;
    }
}
