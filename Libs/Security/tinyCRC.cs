using System;
using System.Text;

namespace Nox.Security
{
    public class tinyCRC
    {
        private UInt32[] crc32Table;
        private const int BUFFER_SIZE = 8192;

        private UInt32 Result;
        private byte[] Buffer;

        public void Push(byte[] Buffer, int Start, int Length)
        {
            unchecked
            {
                int Index = Start;
                for (int i = 0; i < Length; i++)
                    Result = ((Result) >> 8) ^ crc32Table[(Buffer[Index++]) ^ ((Result) & 0x000000FF)];
            }
        }
        public void Push(byte[] Buffer)
        {
            Push(Buffer, 0, Buffer.Length);
        }

        public void Push(byte Value)
        {
            var Raw = BitConverter.GetBytes(Value);
            Push(Raw, 0, Raw.Length);
        }

        public void Push(string Value)
        {
            var Raw = Encoding.UTF8.GetBytes(Value);
            Push(Raw, 0, Raw.Length);
        }

        public void Push(int Value)
        {
            var Raw = BitConverter.GetBytes(Value);
            Push(Raw, 0, Raw.Length);
        }
        public void Push(long Value)
        {
            var Raw = BitConverter.GetBytes(Value);
            Push(Raw, 0, Raw.Length);
        }

        public UInt32 CRC32 { get { return ~Result; } }

        public tinyCRC()
        {
            unchecked
            {
                // This is the official polynomial used by CRC32 in PKZip.
                // Often the polynomial is shown reversed as 0x04C11DB7.
                UInt32 dwPolynomial = 0xEDB88320;
                UInt32 i, j;

                crc32Table = new UInt32[256];

                UInt32 dwCrc;
                for (i = 0; i < 256; i++)
                {
                    dwCrc = i;
                    for (j = 8; j > 0; j--)
                        if ((dwCrc & 1) == 1)
                            dwCrc = (dwCrc >> 1) ^ dwPolynomial;
                        else
                            dwCrc >>= 1;
                    crc32Table[i] = dwCrc;
                }
            }

            Result = 0xFFFFFFFF;
            Buffer = new byte[BUFFER_SIZE];
        }
    }
}
