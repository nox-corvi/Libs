using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message
{
    public class MessageSigvEventArgs : EventArgs
    {
        public Guid SequenceId { get; }
        public byte[] EncryptedHash { get; }

        public bool Valid { get; set; } = false;

        public MessageSigvEventArgs(Guid _SequenceId, byte[] EncryptedHash)
            : base()
        {
            SequenceId = SequenceId;
            this.EncryptedHash = EncryptedHash;
        }
    }

    public class SigvData
        : DataBlock
    {
        private Guid _SequenceId = Guid.NewGuid();
        private byte[] _EncryptedHash;

        #region Properties
        public Guid SequenceId { get => _SequenceId; set => SetProperty(ref _SequenceId, value); }
        public byte[] EncryptedHash { get => _EncryptedHash; set => SetProperty(ref _EncryptedHash, value); }
        #endregion

        public override void Read(byte[] data)
        {
            _SequenceId = Helpers.ExtractGuid(data, 0);

            int i = 16, read;
            _EncryptedHash = Helpers.ExtractArrayWithLength(data, i, out read);
        }

        public override byte[] Write()
        {
            var Result = new List<byte>();
            Result.AddRange(_SequenceId.ToByteArray());

            Result.AddRange(BitConverter.GetBytes(_EncryptedHash.Length));
            Result.AddRange(_EncryptedHash);

            return Result.ToArray();
        }

        public SigvData(uint Signature2)
            : base(Signature2) { }
    }

    public class MessageSigv
       : RawMessage<SigvData>
    {
        public MessageSigv(uint Signature1)
            : base(Signature1, (uint)SecureMessageTypeEnum.SIGV) { }
    }
}
