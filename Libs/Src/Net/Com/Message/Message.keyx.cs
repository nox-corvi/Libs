using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message
{
    public class KeyxEventArgs : EventArgs
    {
        /// <summary>
        /// Random Guid to identify the sequence
        /// </summary>
        public Guid SequenceId { get; }

        public byte[] EncryptedKey { get; }
        public byte[] KeyHash { get; }

        public KeyxEventArgs(Guid SequenceId, byte[] EncryptedKey, byte[] keyHash)
            : base()
        {
            this.SequenceId = SequenceId;
            this.EncryptedKey = EncryptedKey;
            this.KeyHash = keyHash;
        }
    }

    public class MessageKeyxData
        : DataBlock
    {
        private Guid _SequenceId = Guid.Empty;
        private byte[] _EncryptedKey= null!;
        private byte[] _KeyHash = null!;

        #region Properties
        public Guid SequenceId { get => _SequenceId; set => SetProperty(ref _SequenceId, value); }

        public byte[] EncryptedKey { get => _EncryptedKey; set => SetProperty(ref _EncryptedKey, value); }

        public byte[] KeyHash { get => _KeyHash; set => SetProperty(ref _KeyHash, value); }
        #endregion

        public override void Read(byte[] data)
        {
            _SequenceId = Helpers.ExtractGuid(data, 0);

            int i = 16, read;
            _EncryptedKey = Helpers.ExtractArrayWithLength(data, i, out read);

            i += read;
            _KeyHash = Helpers.ExtractArrayWithLength(data, i, out read);
        }

        public override byte[] Write()
        {
            var Result = new List<byte>();
            Result.AddRange(_SequenceId.ToByteArray());

            Result.AddRange(BitConverter.GetBytes(_EncryptedKey.Length));
            Result.AddRange(_EncryptedKey);

            Result.AddRange(BitConverter.GetBytes(_KeyHash.Length));
            Result.AddRange(_KeyHash);

            return Result.ToArray();
        }

        public MessageKeyxData(uint Signature2)
            : base(Signature2) { }
    }

    public class MessageKeyx
       : RawMessage<MessageKeyxData>
    {
        public MessageKeyx(uint Signature1)
            : base(Signature1, (uint)SecureMessageTypeEnum.KEYX) { }
    }
}
