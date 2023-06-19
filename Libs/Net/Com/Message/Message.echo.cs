using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message
{
    public class EchoMessageEventArgs : EventArgs
    {
        /// <summary>
        /// unique guid for ping message
        /// </summary>
        public Guid PingId { get; }

        public DateTime PingTimestamp { get; }
        public DateTime EchoTimestamp { get; }

        public EchoMessageEventArgs(Guid PingId, DateTime PingTimestamp, DateTime EchoTimestamp)
        {
            this.PingId = PingId;
            this.PingTimestamp = PingTimestamp;
            this.EchoTimestamp = EchoTimestamp;
        }
    }

    public class MessageEchoData
        : DataBlock
    {
        private Guid _PingId;
        private DateTime _PingTimestamp;
        private DateTime _EchoTimestamp = DateTime.UtcNow;

        #region Properties
        public Guid PingId { get => _PingId; set => SetProperty(ref _PingId, value); }
        public DateTime PingTimestamp { get => _PingTimestamp; set => SetProperty(ref _PingTimestamp, value); }
        public DateTime EchoTimestamp { get => _EchoTimestamp; set => SetProperty(ref _EchoTimestamp, value); }
        #endregion

        public override void Read(byte[] data)
        {
            _PingId = Helpers.ExtractGuid(data, 0);

            int i = 16;
            _PingTimestamp = DateTime.FromBinary(BitConverter.ToInt64(data, i));

            i += sizeof(long);
            _EchoTimestamp = DateTime.FromBinary(BitConverter.ToInt64(data, i));
        }

        public override byte[] Write()
        {
            var Result = new List<byte>();
            Result.AddRange(_PingId.ToByteArray());
            Result.AddRange(BitConverter.GetBytes(_PingTimestamp.ToBinary()));
            Result.AddRange(BitConverter.GetBytes(_EchoTimestamp.ToBinary()));

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
