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

    public class BindEventArgs : EventArgs
    {
        public Guid Id { get; set; }
        public BindEventArgs(Guid Id)
            : base() => this.Id = Id;
    }

    public class ConnectEventArgs : EventArgs
    {
        public Guid Id { get; set; }
        public ConnectEventArgs(Guid Id)
            : base() => this.Id = Id;
    }

    public class CloseSocketEventArgs : MessageEventArgs
    {
        public CloseSocketEventArgs(string Message)
            : base(Message) { }
    }

    public class ObtainPublicKeyEventArgs : CancelEventArgs
    {
        public byte[] publicKey { get; set; }

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
