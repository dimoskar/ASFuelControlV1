using System;
using System.Drawing;
using System.Windows.Forms;
using Telerik.WinControls.UI;

namespace ASFuelControl.Windows.UI.Controls
{
    internal sealed class InvoicingFields
    {
        internal readonly RadGroupBox Group;
        private readonly TableLayoutPanel table;
        internal InvoicingFields(string title)
        {
            Group = new RadGroupBox { Text = title, Dock = DockStyle.Fill, Padding = new Padding(10,24,10,8) };
            table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoScroll = true };
            // Leave enough room for the longer Greek captions. A caption that
            // reaches the second cell can paint over the left edge of a Telerik
            // checkbox and make the square look like a closing bracket.
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,220));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
            Group.Controls.Add(table);
        }
        internal T Row<T>(string label,T control) where T : Control
        {
            int row = table.RowCount++;
            table.RowStyles.Add(new RowStyle(SizeType.Absolute,30));
            table.Controls.Add(new RadLabel { Text = label, Dock = DockStyle.Fill, TextAlignment = ContentAlignment.MiddleLeft },0,row);
            var emptyCheckBox = control as RadCheckBox;
            if (emptyCheckBox != null && string.IsNullOrWhiteSpace(emptyCheckBox.Text))
            {
                // An empty RadCheckBox does not need the full editor cell. Give
                // its glyph a stable area so theme/DPI measurement cannot clip it.
                emptyCheckBox.AutoSize = false;
                emptyCheckBox.Dock = DockStyle.Left;
                emptyCheckBox.Width = 28;
                emptyCheckBox.Margin = new Padding(3);
            }
            else
                control.Dock = DockStyle.Fill;
            table.Controls.Add(control,1,row);
            return control;
        }
        internal void Note(string text)
        {
            var label = Row("",new RadLabel { Text = text, AutoSize = false });
            table.Controls.Remove(table.GetControlFromPosition(0,table.RowCount-1));
            table.SetColumn(label,0); table.SetColumnSpan(label,2);
            table.RowStyles[table.RowCount-1].Height = 44;
        }
        internal void SetRowVisible(Control control, bool visible)
        {
            int row = table.GetRow(control);
            table.GetControlFromPosition(0,row).Visible = visible;
            control.Visible = visible;
            table.RowStyles[row].Height = visible ? 30 : 0;
        }
        internal static RadTextBox Text(string value = "",bool secret = false) => new RadTextBox { Text = value ?? "", PasswordChar = secret ? '●' : '\0' };
        internal static RadDropDownList Choice(params string[] choices)
        {
            var control = new RadDropDownList { DropDownStyle = Telerik.WinControls.RadDropDownStyle.DropDownList };
            foreach (string value in choices) control.Items.Add(value);
            control.SelectedIndex = choices.Length == 0 ? -1 : 0;
            return control;
        }
    }
}
