using System;
using System.Collections.Generic;

namespace Nox.Net.Com.Message.Defaults
{
    public class kexc_data
        : DataBlock
    {
        private byte[] _PublicKey;

        #region Properties
        public byte[] PublicKey { get => _PublicKey; set => SetProperty(ref _PublicKey, value); }
        #endregion

        public override void Read(byte[] data)
        {
            _Id = new Guid(data);
            _PublicKey = new byte[data.Length - 16];

            Array.Copy(data, 16, _PublicKey, 0, data.Length - 16);  
        }

        public override byte[] Write()
        {
            var Result = new List<byte>();
            Result.AddRange(_Id.ToByteArray());
            Result.AddRange(_PublicKey);

            return Result.ToArray();
        }

        public kexc_data(uint Signature2)
            : base(Signature2) { }
    }

    public class KEXC
       : RawMessage<kexc_data>
    {
        public KEXC(uint Signature1)
            : base(Signature1, (uint)DefaultMessageTypeEnum.KEXC) { }
    }
}
