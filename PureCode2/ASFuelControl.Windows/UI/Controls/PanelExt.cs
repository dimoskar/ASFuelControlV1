using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Telerik.WinControls.Primitives;

namespace ASFuelControl.Windows.UI.Controls
{
    public class PanelExt : Telerik.WinControls.UI.RadPanel
    {
        public Color BackColor1 { set; get; }
        public Color BackColor2 { set; get; }

        public void ApplyBackColor()
        {
            ((FillPrimitive)this.PanelElement.Children[0]).BackColor = BackColor1;
            ((FillPrimitive)this.PanelElement.Children[0]).BackColor2 = BackColor2;
        }
    }
}
