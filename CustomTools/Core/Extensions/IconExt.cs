using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomTools.Core.Extensions
{
    public static class IconExt
    {
        public static Icon LoadFromByetArray(this Icon icon, byte[] bytes) 
        {
            icon = new Icon(new System.IO.MemoryStream(bytes));
            return icon;
        }
    }
}
