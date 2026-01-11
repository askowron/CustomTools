using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CTPlugins
{
    public class CtrlToolStripLabel : ToolStripLabel
    {
        private Pen secondPen;

        public CtrlToolStripLabel()
        { 
            Font = new Font("Segoe UI", 8F, System.Drawing.FontStyle.Italic | FontStyle.Underline);
            ForeColor = Color.DarkGray;
            Margin = new Padding(0);

            secondPen = new Pen(new SolidBrush(Color.Blue));
        }

        public CtrlToolStripLabel(string text) : this()
        {
            Text = text;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            //e.Graphics.DrawLine(secondPen, 0f, Height-1, Width, Height-1);
        }
    }
}
