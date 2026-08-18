namespace CustomTools
{
    partial class FrmLicense
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
            this.rtbLicense = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            //
            // rtbLicense
            //
            this.rtbLicense.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.rtbLicense.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rtbLicense.Font = new System.Drawing.Font("Consolas", 9F);
            this.rtbLicense.Location = new System.Drawing.Point(0, 0);
            this.rtbLicense.Name = "rtbLicense";
            this.rtbLicense.ReadOnly = true;
            this.rtbLicense.Size = new System.Drawing.Size(520, 430);
            this.rtbLicense.TabIndex = 0;
            this.rtbLicense.Text = "";
            this.rtbLicense.WordWrap = true;
            //
            // FrmLicense
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(520, 480);
            this.Controls.Add(this.rtbLicense);
            this.MinimumSize = new System.Drawing.Size(360, 300);
            this.Name = "FrmLicense";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = Properties.Strings.License_Title;
            this.Controls.SetChildIndex(this.rtbLicense, 0);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.RichTextBox rtbLicense;
    }
}
