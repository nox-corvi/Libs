using Nox.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message.Defaults
{
    public class KEXC : RawMessage
    {
        private ECDHPublicKey _PublicKey = null;

        #region Properties
        public byte[] PublicKey
        {
            get => _PublicKey.ToByteArray();
            set
            {
                ArgumentNullException.ThrowIfNull(value);

                _PublicKey = new(value);

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

        public KEXC(uint Signature1)
            : base(Signature1) =>
            this.Signature2 = 0xFCA1;

        public KEXC(uint Signature1, ECDHPublicKey PublicKey)
            : this(Signature1) => 
            this._PublicKey = PublicKey;
    }
}
