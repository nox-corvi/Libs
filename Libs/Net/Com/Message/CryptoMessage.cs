using Nox.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message
{
    public abstract class CryptoMessage
        : RawMessage
    {
        private Laverna laverna;

        public byte[] Decode()
        {
            using (var Source = new MemoryStream(_data))
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
                _data = Dest.ToArray();
            }
        }

        public CryptoMessage(uint Signature1, uint Signature2, Laverna laverna)
            : base(Signature1)
        {
            this.laverna = laverna; 
            this._signature2 = Signature2;
        }
    }
}
