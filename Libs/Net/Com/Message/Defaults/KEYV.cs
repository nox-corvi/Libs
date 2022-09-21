using Nox.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message.Defaults
{
    public class KEYV : RawMessage
    {
        private byte[] _KeyHash = null;

        #region Properties
        public byte[] KeyHash
        {
            get => _KeyHash;
            set
            {
                ArgumentNullException.ThrowIfNull(value);

                // get vars
                var len = (ushort)(value.Length);
                var p = sizeof(ushort);

                // resize message
                _data = new byte[len + p];

                // and copy to data
                Array.Copy(BitConverter.GetBytes(len), 0, _data, 0, p);
                Array.Copy(value, p, _data, p, len);
            }
        }
        #endregion

        public KEYV(ushort Signature1)
            : base(Signature1) =>
            this.Signature2 = 0xFCA2;

        public KEYV(ushort Signature1, byte[] keyHash)
            : this(Signature1) =>
            this._KeyHash = keyHash;
    }
}
