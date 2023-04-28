using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message
{
    public class KeyvEventArgs : EventArgs
    {
        /// <summary>
        /// Random Guid to identify the sequence
        /// </summary>
        public Guid SequenceId { get; }

        public byte[] EncryptedIV { get; }

        public byte[] IVHash { get; }

        public KeyvEventArgs(Guid SequenceId, byte[] EncryptedIV, byte[] IVHash)
            : base()
        {
            this.SequenceId = SequenceId;
            this.EncryptedIV = EncryptedIV;
            this.IVHash = IVHash;
        }
    }

    public class MessageKeyvData
        : DataBlock
    {
        public Guid _SequenceId = Guid.Empty;
        private byte[] _EncryptedIV = null!;
        private byte[] _IVHash = null!;

        #region Properties
        public Guid SequenceId { get => _SequenceId; set => SetProperty(ref _SequenceId, value); }

        public byte[] EncryptedIV { get => _EncryptedIV; set => SetProperty(ref _EncryptedIV, value); }

        public byte[] IVHash { get => _IVHash; set => SetProperty(ref _IVHash, value); }
        #endregion

        public override void Read(byte[] data)
        {
            _SequenceId = Helpers.ExtractGuid(data, 0);

            int i = 16, read;
            _EncryptedIV = Helpers.ExtractArrayWithLength(data, i, out read);

            i += read;
            _IVHash = Helpers.ExtractArrayWithLength(data, i, out read);
        }

        public override byte[] Write()
        {
            var Result = new List<byte>();
            Result.AddRange(_SequenceId.ToByteArray());

            Result.AddRange(BitConverter.GetBytes(_EncryptedIV.Length));
            Result.AddRange(_EncryptedIV);

            Result.AddRange(BitConverter.GetBytes(_IVHash.Length));
            Result.AddRange(_IVHash);

            return Result.ToArray();
        }

        public MessageKeyvData(uint Signature2)
            : base(Signature2) { }
    }

    public class MessageKeyv
       : RawMessage<MessageKeyvData>
    {
        public MessageKeyv(uint Signature1)
            : base(Signature1, (uint)SecureMessageTypeEnum.KEYV) { }
    }
}
