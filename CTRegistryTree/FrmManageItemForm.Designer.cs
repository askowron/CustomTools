namespace CTRegistryTree
{
    partial class FrmManageItemForm
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
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tbText = new System.Windows.Forms.TextBox();
            this.cbAction = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tbCommand = new System.Windows.Forms.TextBox();
            this.button1 = new System.Windows.Forms.Button();
            this.btnTest = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(28, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = Properties.Strings.Label_Text;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 35);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = Properties.Strings.Label_Action;
            // 
            // tbText
            //
            this.tbText.Location = new System.Drawing.Point(94, 6);
            this.tbText.Name = "tbText";
            this.tbText.Size = new System.Drawing.Size(354, 20);
            this.tbText.TabIndex = 4;
            //
            // cbAction
            //
            this.cbAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbAction.FormattingEnabled = true;
            this.cbAction.Location = new System.Drawing.Point(94, 32);
            this.cbAction.Name = "cbAction";
            this.cbAction.Size = new System.Drawing.Size(354, 21);
            this.cbAction.TabIndex = 5;
            //
            // label3
            //
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 63);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(54, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = Properties.Strings.Label_Command;
            //
            // tbCommand
            //
            this.tbCommand.Location = new System.Drawing.Point(94, 60);
            this.tbCommand.Multiline = true;
            this.tbCommand.WordWrap = true;
            this.tbCommand.AcceptsReturn = false;
            this.tbCommand.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.tbCommand.Name = "tbCommand";
            this.tbCommand.Size = new System.Drawing.Size(354, 64);
            this.tbCommand.TabIndex = 7;
            //
            // button1
            //
            this.button1.Location = new System.Drawing.Point(94, 130);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 8;
            this.button1.Text = Properties.Strings.Button_Find;
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            //
            // btnTest
            //
            this.btnTest.Location = new System.Drawing.Point(175, 130);
            this.btnTest.Name = "btnTest";
            this.btnTest.Size = new System.Drawing.Size(75, 23);
            this.btnTest.TabIndex = 9;
            this.btnTest.Text = Properties.Strings.Button_Test;
            this.btnTest.UseVisualStyleBackColor = true;
            this.btnTest.Click += new System.EventHandler(this.btnTest_Click);
            //
            // FrmManageItemForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(464, 210);
            this.Controls.Add(this.btnTest);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.tbCommand);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.cbAction);
            this.Controls.Add(this.tbText);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label2);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmManageItemForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = Properties.Strings.Form_ManageItem_Title;
            this.TopMost = true;
            this.Controls.SetChildIndex(this.label2, 0);
            this.Controls.SetChildIndex(this.label1, 0);
            this.Controls.SetChildIndex(this.tbText, 0);
            this.Controls.SetChildIndex(this.cbAction, 0);
            this.Controls.SetChildIndex(this.label3, 0);
            this.Controls.SetChildIndex(this.tbCommand, 0);
            this.Controls.SetChildIndex(this.button1, 0);
            this.Controls.SetChildIndex(this.btnTest, 0);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbText;
        private System.Windows.Forms.ComboBox cbAction;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbCommand;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btnTest;
    }
}