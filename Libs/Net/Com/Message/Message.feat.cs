using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message
{
    public class MessageFeatEventArgs : EventArgs
    {
        public MessageFeatEventArgs(Guid SequenceId)
        {
        }
    }

    public class MessageFeatData
        : DataBlock
    {
        #region Properties
        
        #endregion

        public override void Read(byte[] data)
        {

        }

        public override byte[] Write()
        {
            var Result = new List<byte>();
            

            return Result.ToArray();
        }

        public MessageFeatData(uint Signature2)
            : base(Signature2) { }
    }

    public class MessageFeat
       : RawMessage<MessageFeatData>
    {
        public MessageFeat(uint Signature1)
            : base(Signature1, (uint)MessageTypeEnum.FEAT) { }
    }
}
