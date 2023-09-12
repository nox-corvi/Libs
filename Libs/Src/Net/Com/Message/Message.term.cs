using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message
{
    public class TermMessageData
        : DataBlock
    {
        private Guid _Id = Guid.NewGuid();

        #region Properties
        public Guid Id { get => _Id; }
        #endregion

        public override void Read(byte[] data)
        {
            _Id = Helpers.ExtractGuid(data, 0);
        }

        public override byte[] Write()
        {
            var Result = new List<byte>();
            Result.AddRange(_Id.ToByteArray());
            //Result.AddRange(BitConverter.GetBytes(_Timestamp.ToBinary()));

            return Result.ToArray();
        }

        public TermMessageData(uint Signature2)
            : base(Signature2) { }
    }

    public class MessageTerm
       : RawMessage<PingMessageData>
    {
        public MessageTerm(uint Signature1)
            : base(Signature1, (uint)MessageTypeEnum.TERM) { }
    }
}
