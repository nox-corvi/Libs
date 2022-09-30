using Nox.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message.Defaults
{
    public class keyv_data
        : DataBlock
    {
        private byte[] _PublicKey;

        #region Properties
        public byte[] PublicKey { get => _PublicKey; set => _PublicKey = value; }
        #endregion

        public override void Read(byte[] data) =>
            _PublicKey = data;

        public override byte[] Write() =>
            _PublicKey;

        public keyv_data(uint Signature2)
            : base(Signature2) { }
    }

    public class KEYV
       : RawMessage<keyv_data>
    {
        public KEYV(uint Signature1)
            : base(Signature1, (uint)DefaultMessageTypeEnum.KEYV) { }
    }
}
