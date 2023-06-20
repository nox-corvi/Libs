using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message
{
    public class EhloEventArgs : EventArgs
    {
        /// <summary>
        /// Random Guid to identify the sequence
        /// </summary>
        public Guid SequenceId { get; }
        public DateTime Timestamp { get; }
        public byte[] PublicKey { get; }
        public string Message { get; }

        public EhloEventArgs(Guid SequenceId, DateTime Timestamp, byte[] publicKey, string Message)
            : base()
        {
            this.SequenceId = SequenceId;
            this.Timestamp = Timestamp;
            this.PublicKey = publicKey;
            this.Message = Message;
        }
    }

    public class MessageEhloData
        : DataBlock
    {
        public const int MAX_LENGTH = 800;

        private Guid _SequenceId = Guid.NewGuid();
        private DateTime _Timestamp = DateTime.UtcNow;
        private byte[] _PublicKey;
        private string _Message = "";

        #region Properties
        public Guid SequenceId { get => _SequenceId; set => SetProperty(ref _SequenceId, value); }
        public DateTime Timestamp { get => _Timestamp; set => SetProperty(ref _Timestamp, value); }
        public byte[] PublicKey { get => _PublicKey; set => SetProperty(ref _PublicKey, value); }
        public string Message { get => _Message; set => SetProperty(ref _Message, value.LimitLength(MAX_LENGTH)); }
        #endregion

        public override void Read(byte[] data)
        {
            _SequenceId = Helpers.ExtractGuid(data, 0);

            int i = 16, read;
            _Timestamp = DateTime.FromBinary(BitConverter.ToInt64(data, i));
            i += sizeof(long);

            _PublicKey = Helpers.ExtractArrayWithLength(data, i, out read);
            i += read;

            _Message = Encoding.ASCII.GetString(data, i, data.Length - i).LimitLength(MAX_LENGTH).Trim();
        }

        public override byte[] Write()
        {
            var Result = new List<byte>();
            Result.AddRange(_SequenceId.ToByteArray());
            Result.AddRange(BitConverter.GetBytes(_Timestamp.ToBinary()));
            Result.AddRange(BitConverter.GetBytes(_PublicKey.Length));
            Result.AddRange(_PublicKey);
            Result.AddRange(Encoding.ASCII.GetBytes(_Message.LimitLength(MAX_LENGTH)));

            return Result.ToArray();
        }

        public MessageEhloData(uint Signature2)
            : base(Signature2) { }
    }

    public class MessageEhlo
       : RawMessage<MessageEhloData>
    {
        public MessageEhlo(uint Signature1)
            : base(Signature1, (uint)SecureMessageTypeEnum.EHLO) { }
    }
}
