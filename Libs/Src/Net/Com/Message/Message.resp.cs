using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message
{
    public class RespMessageEventArgs
        : EventArgs
    {
        public Guid SequenceId { get; }
        public uint Response1 { get; }        
        public uint Response2 { get; }
        public uint Response3 { get; }



        public RespMessageEventArgs(Guid SequenceId, uint Response1, uint Response2, uint Response3)
            : base()
        {
            this.SequenceId = SequenceId;
            this.Response1 = Response1;
            this.Response2 = Response2;
            this.Response3 = Response3;
        }
    }

    public class MessageRespData
        : DataBlock
    {
        private const int MAX_LENGTH = 64;

        private Guid _SequenceId = Guid.Empty;
        private uint _Response1 = 0;
        private uint _Response2 = 0;
        private uint _Response3 = 0;

        #region Properties
        public Guid SequenceId { get => _SequenceId; set => SetProperty(ref _SequenceId, value); }
        public uint Response1 { get => _Response1; set => SetProperty(ref _Response1, value); }
        public uint Response2 { get => _Response2; set => SetProperty(ref _Response2, value); }
        public uint Response3 { get => _Response3; set => SetProperty(ref _Response3, value); }
        #endregion

        public override void Read(byte[] data)
        {
            _SequenceId = Helpers.ExtractGuid(data, 0);

            int i = 16;
            _Response1 = BitConverter.ToUInt32(data, i);
            i += Marshal.SizeOf(_Response1.GetType());

            _Response2 = BitConverter.ToUInt32(data, i);
            i += Marshal.SizeOf(_Response1.GetType());

            _Response3 = BitConverter.ToUInt32(data, i);
            i += Marshal.SizeOf(_Response1.GetType());
        }

        public override byte[] Write()
        {
            List<byte> Result = new List<byte>();

            Result.AddRange(BitConverter.GetBytes(_Response1));
            Result.AddRange(BitConverter.GetBytes(_Response2));
            Result.AddRange(BitConverter.GetBytes(_Response3));

            return Result.ToArray();
        }

        public MessageRespData(uint Signature2)
            : base(Signature2) { }

        public MessageRespData(uint Signature2, uint Response1, uint Response2 = 0, uint Response3 = 0)
            : this(Signature2)
        {
            _Response1 = Response1;
            _Response2 = Response2;
            _Response3 = Response3;
        }
    }

    public class MessageResp
       : RawMessage<MessageRespData>
    {
        public MessageResp(uint Signature1)
            : base(Signature1, (uint)SecureMessageTypeEnum.RESP) { }
    }
}
