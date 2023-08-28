using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message
{
    public class MessageConS
       : RawMessage<MessagePlainData>
    {
        public MessageTerminate(uint Signature1)
            : base(Signature1, (uint)MessageTypeEnum.TERM) { }
    }
}
