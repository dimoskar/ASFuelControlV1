using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ASFuelControl.Windows.UI.Controls
{
    public class DoubleBufferTableLayout : TableLayoutPanel
    {
        public DoubleBufferTableLayout()
        {
            this.DoubleBuffered = true;

            ////// or

            //SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            //SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            //UpdateStyles();
        }
    }
}
