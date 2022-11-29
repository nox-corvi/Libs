using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message
{
    public class MessageDoneEventArgs : EventArgs
    {
        public MessageDoneEventArgs() { }
    }

    public class MessageDone
       : RawMessage<MessagePlainData>
    {
        public MessageDone(uint Signature1)
            : base(Signature1, (uint)MessageTypeEnum.DONE) { }
    }
}
