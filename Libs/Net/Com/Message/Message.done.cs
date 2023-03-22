using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message
{
    public class DoneEventArgs : EventArgs
    {
        public DoneEventArgs() { }
    }

    public class MessageDone
       : RawMessage<MessagePlainData>
    {
        public MessageDone(uint Signature1)
            : base(Signature1, (uint)MessageTypeEnum.DONE) { }
    }
}
