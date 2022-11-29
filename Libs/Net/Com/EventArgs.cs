using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com
{
    public class MessageEventArgs : EventArgs
    {
        public string Message { get; set; }

        public MessageEventArgs(string Message)
            : base() =>
            this.Message = Message;
    }

    public class CloseSocketEventArgs : MessageEventArgs
    {
        public CloseSocketEventArgs(string Message)
            : base(Message) { }
    }

    public class ObtainPublicKeyEventArgs : CancelEventArgs
    {
        public byte[] PublicKey { get; set; }

        public ObtainPublicKeyEventArgs()
            : base() { }
    }

    public class ObtainMessageEventArgs : CancelEventArgs
    {
        public string Message { get; set; }

        public ObtainMessageEventArgs()
            : base() { }
    }
}
