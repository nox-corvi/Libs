using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message.Defaults
{
    public abstract class HELO
       : RawMessage
    {
        private byte[] _PublicKey;

        #region Properties
        public byte[] PublicKey { get => _PublicKey; set => _PublicKey = value; }
        #endregion

        public HELO(uint Signature1)
            : base(Signature1) =>
            this.Signature2 = 0xFCA0;
    }
}
