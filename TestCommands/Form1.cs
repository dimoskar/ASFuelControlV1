using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestCommands
{
    public partial class Form1 : Form
    {
        const int INITIALIZE            =   7;
        const int STATUS1               =   1;
        const int STATUS2               =   3;
        const int STATUS3               =   6;
        const int STATUS4               =   2;
        const int GETPRICE              =   2;
        const int GETDATA               =   2;
        const int TOTALSVOLUME          =   2;
        const int TOTALSAMOUNT          =   2;
        const int AUTHORIZE             =   4;

        const int SUB_STATUS4           =  75;
        const int SUB_GETPRICE1         = 195;
        const int SUB_GETPRICE2         = 165;
        const int SUB_GETPRICE3         =  90;
        const int SUB_GETDATA           = 180;
        const int SUB_TOTALSVOLUME      = 135;
        const int SUB_TOTALSAMOUNT      = 170;
        const int SUB_TOTALSVOLUMEMULTI =  45;
        const int SUB_TOTALSAMOUNTMULTI =  30;
        const int SUB_AUTHORIZE         = 225;

        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var b1 = GetCommand(1, TOTALSVOLUME, SUB_TOTALSVOLUME);
        }

        public byte[] GetCommand(int address, int cmd, int? subCommand = null)
        {
            BitArray baCmd = new BitArray(new int[] { cmd });
            BitArray baAddress = new BitArray(new int[] { address });
            BitArray baTotal = subCommand.HasValue ? new BitArray(16) : new BitArray(8);
            int offset = subCommand.HasValue ? 8 : 0;
            for (int i = 0; i < 5; i++)
            {
                baTotal.Set(i + offset + 3, baAddress.Get(i));
            }
            for (int i = 0; i < 2; i++)
            {
                baTotal.Set(i + offset, baCmd.Get(i));
            }
            if(subCommand.HasValue)
            {
                BitArray baSubCommand = new BitArray(new int[] { subCommand.Value });
                for (int i = 0; i < 8; i++)
                {
                    baTotal.Set(i, baSubCommand.Get(i));
                }
            }

            byte[] bytes = subCommand.HasValue ? new byte[2] : new byte[1];
            baTotal.CopyTo(bytes, 0);
            
            Console.WriteLine(ConverBitArrayTostring(baTotal));
            return bytes;
        }

        private string ConverBitArrayTostring(BitArray objBit)
        {
            try
            {
                string sReturnValue = "";
                for (int i = objBit.Length - 1; i >=0; i--)
                {
                    sReturnValue += (objBit[i]) ? "1" : "0";
                }
                return sReturnValue;
            }
            catch
            {
                return "";
            }
        }
    }
}
