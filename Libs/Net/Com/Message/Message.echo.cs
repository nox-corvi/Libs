using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message
{
    public class MessageEchoEventArgs : EventArgs
    {
        public Guid PingId { get; }
        public DateTime PingTime { get; }
        public DateTime Timestamp { get; }

        public MessageEchoEventArgs(Guid PingId, DateTime PingTime, DateTime Timestamp)
        {
            this.PingId = PingId;
            this.PingTime = PingTime;
            this.Timestamp = Timestamp;
        }
    }

    public class MessageEchoData
        : DataBlock
    {
        private Guid _PingId;
        private DateTime _PingTime;
        private DateTime _Timestamp = DateTime.UtcNow;

        #region Properties
        public Guid PingId { get => _PingId; set => SetProperty(ref _PingId, value); }
        public DateTime PingTime { get => _PingTime; set => SetProperty(ref _PingTime, value); }
        public DateTime Timestamp { get => _Timestamp; set => SetProperty(ref _Timestamp, value); }
        #endregion

        public override void Read(byte[] data)
        {
            _PingId = Helpers.ExtractGuid(data, 0);

            int i = 16;
            _PingTime = DateTime.FromBinary(BitConverter.ToInt64(data, i));

            i += sizeof(long);
            _Timestamp = DateTime.FromBinary(BitConverter.ToInt64(data, i));
        }

        public override byte[] Write()
        {
            var Result = new List<byte>();
            Result.AddRange(_PingId.ToByteArray());
            Result.AddRange(BitConverter.GetBytes(_PingTime.ToBinary()));
            Result.AddRange(BitConverter.GetBytes(_Timestamp.ToBinary()));

            return Result.ToArray();
        }

        public MessageEchoData(uint Signature2)
            : base(Signature2) { }
    }

    public class MessageEcho
       : RawMessage<MessageEchoData>
    {
        public MessageEcho(uint Signature1)
            : base(Signature1, (uint)MessageTypeEnum.ECHO) { }
    }
}
