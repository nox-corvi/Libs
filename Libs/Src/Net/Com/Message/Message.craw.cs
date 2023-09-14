using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message
{
    public class CRawEventArgs : EventArgs
    {
        public Guid SequenceId { get; }

        public byte[] EncryptedData { get; }

        public bool Valid { get; set; } = false;

        public CRawEventArgs(Guid SequenceId, byte[] EncryptedData)
            : base()
        {
            this.SequenceId = SequenceId;
            this.EncryptedData = EncryptedData;
        }
    }

    public class MessageCRawData
        : DataBlock
    {
        private Guid _SequenceId = Guid.NewGuid();
        private byte[] _EncryptedData;

        #region Properties
        public Guid SequenceId { get => _SequenceId; set => SetProperty(ref _SequenceId, value); }
        public byte[] EncryptedHash { get => _EncryptedHash; set => SetProperty(ref _EncryptedHash, value); }
        #endregion

        public override void Read(byte[] data)
        {
            _SequenceId = Helpers.ExtractGuid(data, 0);

            int i = 16, read;
            _EncryptedData = Helpers.ExtractArrayWithLength(data, i, out read);
        }

        public override byte[] Write()
        {
            var Result = new List<byte>();
            Result.AddRange(_SequenceId.ToByteArray());

            Result.AddRange(BitConverter.GetBytes(_EncryptedData.Length));

            Result.AddRange(_EncryptedData);

            return Result.ToArray();
        }

        public MessageCRawData(uint Signature2)
            : base(Signature2) { }
    }

    public class MessageCRaw
       : RawMessage<MessageCRawData>
    {
        public MessageCRaw(uint Signature1)
            : base(Signature1, (uint)SecureMessageTypeEnum.CRAW) { }
    }
}
