using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Common
{
    public class Helpers
    {
        public static UInt32 CalculateCRC32(object entity)
        {
            PropertyInfo[] properties = entity.GetType().GetProperties();
            List<byte> bytes = new List<byte>();
            foreach (PropertyInfo prop in properties)
            {
                if (prop.GetSetMethod() == null)
                    continue;
                if (prop.PropertyType.IsArray)
                    continue;
                if (prop.Name == "CRC")
                    continue;
                //if (prop.PropertyType.IsArray)
                //    continue;
                object value = prop.GetValue(entity, null);
                if (value == null)
                    continue;
                bytes.AddRange(System.Text.Encoding.Default.GetBytes(value.ToString()));
            }
            return Crc32.Compute(bytes.ToArray());
        }

        public static string GetLatinFromGreek(string str)
        {
            if (str == null)
                return "";
            str = str.ToUpper();
            string strLatin = "";
            for(int i=0; i < str.Length; i++)
            {
                string c = str[i].ToString();
                switch(str[i])
                {
                    case 'Α':
                    case 'Ά':
                        c = "A";
                        break;
                    case 'Β':
                        c = "B";
                        break;
                    case 'Γ':
                        c = "G";
                        break;
                    case 'Δ':
                        c = "D";
                        break;
                    case 'Ε':
                    case 'Έ':
                        c = "E";
                        break;
                    case 'Ζ':
                        c = "Z";
                        break;
                    case 'Η':
                    case 'Ή':
                        c = "H";
                        break;
                    case 'Θ':
                        c = "TH";
                        break;
                    case 'Ι':
                    case 'Ί':
                    case 'Ϊ':
                        c = "I";
                        break;
                    case 'Κ':
                        c = "K";
                        break;
                    case 'Λ':
                        c = "L";
                        break;
                    case 'Μ':
                        c = "M";
                        break;
                    case 'Ν':
                        c = "N";
                        break;
                    case 'Ξ':
                        c = "X";
                        break;
                    case 'Ο':
                    case 'Ό':
                    case 'Ω':
                    case 'Ώ':
                        c = "O";
                        break;
                    case 'Π':
                        c = "P";
                        break;
                    case 'Ρ':
                        c = "R";
                        break;
                    case 'Σ':
                        c = "S";
                        break;
                    case 'Τ':
                        c = "T";
                        break;
                    case 'Υ':
                    case 'Ύ':
                    case 'Ϋ':
                        c = "Y";
                        break;
                    case 'Φ':
                        c = "F";
                        break;
                    case 'Χ':
                        c = "X";
                        break;
                    case 'Ψ':
                        c = "PS";
                        break;
                    default:
                        c = str[i].ToString();
                        break;
                }
                strLatin = strLatin + c;
            }
            return strLatin;
        }
    }

    public sealed class Crc32 : HashAlgorithm
    {
        public const UInt32 DefaultPolynomial = 0xedb88320u;
        public const UInt32 DefaultSeed = 0xffffffffu;

        private static UInt32[] defaultTable;

        private readonly UInt32 seed;
        private readonly UInt32[] table;
        private UInt32 hash;

        public Crc32()
            : this(DefaultPolynomial, DefaultSeed)
        {
        }

        public Crc32(UInt32 polynomial, UInt32 seed)
        {
            table = InitializeTable(polynomial);
            this.seed = hash = seed;
        }

        public override void Initialize()
        {
            hash = seed;
        }

        protected override void HashCore(byte[] buffer, int start, int length)
        {
            hash = CalculateHash(table, hash, buffer, start, length);
        }

        protected override byte[] HashFinal()
        {
            var hashBuffer = UInt32ToBigEndianBytes(~hash);
            HashValue = hashBuffer;
            return hashBuffer;
        }

        public override int HashSize { get { return 32; } }

        public static UInt32 Compute(byte[] buffer)
        {
            return Compute(DefaultSeed, buffer);
        }

        public static UInt32 Compute(UInt32 seed, byte[] buffer)
        {
            return Compute(DefaultPolynomial, seed, buffer);
        }

        public static UInt32 Compute(UInt32 polynomial, UInt32 seed, byte[] buffer)
        {
            return ~CalculateHash(InitializeTable(polynomial), seed, buffer, 0, buffer.Length);
        }

        private static UInt32[] InitializeTable(UInt32 polynomial)
        {
            if (polynomial == DefaultPolynomial && defaultTable != null)
                return defaultTable;

            var createTable = new UInt32[256];
            for (var i = 0; i < 256; i++)
            {
                var entry = (UInt32)i;
                for (var j = 0; j < 8; j++)
                    if ((entry & 1) == 1)
                        entry = (entry >> 1) ^ polynomial;
                    else
                        entry = entry >> 1;
                createTable[i] = entry;
            }

            if (polynomial == DefaultPolynomial)
                defaultTable = createTable;

            return createTable;
        }

        private static UInt32 CalculateHash(UInt32[] table, UInt32 seed, IList<byte> buffer, int start, int size)
        {
            var crc = seed;
            for (var i = start; i < size - start; i++)
                crc = (crc >> 8) ^ table[buffer[i] ^ crc & 0xff];
            return crc;
        }

        private static byte[] UInt32ToBigEndianBytes(UInt32 uint32)
        {
            var result = BitConverter.GetBytes(uint32);

            if (BitConverter.IsLittleEndian)
                Array.Reverse(result);

            return result;
        }
    }
}
