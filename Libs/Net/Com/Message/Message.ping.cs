using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message
{
    public class PingMessageEventArgs: EventArgs
    {
        public Guid Id { get; }
        public DateTime Timestamp { get; }

        public PingMessageEventArgs(Guid Id, DateTime Timestamp)
        {
            this.Id = Id;
            this.Timestamp = Timestamp;
        }
    }

    public class PingMessageData
        : DataBlock
    {
        private Guid _Id = Guid.NewGuid();
        private DateTime _Timestamp = DateTime.UtcNow;

        #region Properties
        public Guid Id { get => _Id; }
        public DateTime Timestamp { get => _Timestamp; set => SetProperty(ref _Timestamp, value); }
        #endregion

        public override void Read(byte[] data)
        {
            _Id = Helpers.ExtractGuid(data, 0);

            int i = 16;
            _Timestamp = DateTime.FromBinary(BitConverter.ToInt64(data, i));
        }

        public override byte[] Write()
        {
            var Result = new List<byte>();
            Result.AddRange(_Id.ToByteArray());
            Result.AddRange(BitConverter.GetBytes(_Timestamp.ToBinary()));

            return Result.ToArray();
        }

        public PingMessageData(uint Signature2)
            : base(Signature2) { }
    }

    public class MessagePing
       : RawMessage<PingMessageData>
    {
        public MessagePing(uint Signature1)
            : base(Signature1, (uint)MessageTypeEnum.PING) { }
    }
}
