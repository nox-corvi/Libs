using Nox.Security;
using System;
using System.IO;

namespace Nox.Net.Com.Message
{
    public abstract class CryptoMessage<T>
        : RawMessage<T> where T : IDataBlock
    {
        private Laverna laverna;

        public byte[] Decode()
        {
            using (var Source = new MemoryStream(_user_data))
            using (var Dest = new MemoryStream())
            {
                var Result = laverna.Decode(Source, Dest);
                Dest.Position = 0;

                return Dest.ToArray();
            }
        }

        public void Encode(byte[] data)
        {
            using (var Source = new MemoryStream(data))
            using (var Dest = new MemoryStream())
            {
                var Result = laverna.Encode(Source, Dest);
                _user_data = Dest.ToArray();
            }
        }

        public CryptoMessage(uint Signature1, uint Signature2, Laverna laverna)
            : base(Signature1)
        {
            this.laverna = laverna;
            _signature2 = Signature2;
        }
    }
}
