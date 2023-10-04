using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message
{
    public class URawEventArgs : EventArgs
    {
        public byte[] Data { get; }

        public URawEventArgs(byte[] Data)
            : base()
        {
            this.Data = Data;
        }
    }

    public class MessageURawData
        : DataBlock
    {
        private byte[] _Data;

        #region Properties
        public byte[] Data { get => _Data; set => SetProperty(ref _Data, value); }
        #endregion

        public override void Read(byte[] data)
        {
            int i = 0, read;
            _Data = Helpers.ExtractArrayWithLength(data, i, out read);
        }

        public override byte[] Write()
        {
            var Result = new List<byte>();
            Result.AddRange(BitConverter.GetBytes(_Data.Length));
            Result.AddRange(_Data);

            return Result.ToArray();
        }

        public MessageURawData(uint Signature2)
            : base(Signature2) { }
    }

    public class MessageURaw
       : RawMessage<MessageURawData>
    {
        public MessageURaw(uint Signature1)
            : base(Signature1, (uint)SecureMessageTypeEnum.URAW) { }
    }
}
