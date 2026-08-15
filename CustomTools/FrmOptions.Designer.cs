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
            // FrmOptions
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(280, 96);
            this.Controls.Add(this.chkStartWithWindows);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmOptions";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = Properties.Strings.Options_Title;
            this.Controls.SetChildIndex(this.chkStartWithWindows, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.CheckBox chkStartWithWindows;
    }
}
