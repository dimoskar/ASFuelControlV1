using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Test
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.SetPrices(1589, 2, 3);
        }

        public void SetPrices(int unitPrice, int decimalPlaces, int priceDecimalPlaces)
        {

            byte startByte = BitConverter.GetBytes(192 + 2 - 1)[0];
            List<Byte> commandBuffer = new List<byte>();
            byte cmd = 0xA3;
            commandBuffer.Add(startByte);
            commandBuffer.Add(cmd);
            for (int i = 0; i < 1; i++)
            {
                decimal price = (decimal)unitPrice / (decimal)Math.Pow(10, priceDecimalPlaces);
                if (price == 0)
                    return;
                byte[] price1 = this.GetDecimalBytes(price, priceDecimalPlaces, 2);
                byte[] price2 = this.GetDecimalBytes(price, priceDecimalPlaces, 2);
                commandBuffer.AddRange(price1.Take(2));
                commandBuffer.AddRange(price2.Take(2));
            }
            for (int i = 1; i < 8; i++)
            {
                byte[] price1 = this.GetDecimalBytes(0, priceDecimalPlaces, 2);
                byte[] price2 = this.GetDecimalBytes(0, priceDecimalPlaces, 2);
                commandBuffer.AddRange(price1.Take(2));
                commandBuffer.AddRange(price2.Take(2));
            }
            for (int i = 0; i < 8; i++)
            {
                commandBuffer.Add(0x00);
            }

            byte[] command = this.NormaliseBuffer(commandBuffer.ToArray());
        }

        private byte[] GetDecimalBytes(decimal value, int decimalPlaces, int byteCount)
        {
            long valueInt = (long)((double)value * Math.Pow(10, decimalPlaces));
            List<Byte> bufferList = new List<byte>();
            int maxPow = 2 * (byteCount - 1);
            double restValue = (double)valueInt;
            string str = "";
            for (int i = 0; i < byteCount; i++)
            {
                int pow = maxPow - (2 * i);
                double byteVal = (int)(restValue / System.Math.Pow(10, pow));
                int bv = (int)byteVal;
                str = str + bv.ToString();
                //bufferList.Add(b);
                restValue = restValue - (byteVal * System.Math.Pow(10, pow));
            }
            return StringToByteArray(str);
        }

        public byte[] StringToByteArray(string hex)
        {
            if (hex.Length % 2 != 0)
                hex = "0" + hex;
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        private byte[] NormaliseBuffer(byte[] buffer)
        {
            List<byte> newBuffer = new List<byte>();
            foreach (Byte b in buffer)
            {
                newBuffer.Add(b);
                newBuffer.Add((byte)(255 - b));
            }
            return newBuffer.ToArray();
        }
    }
}
