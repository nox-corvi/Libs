using Nox.Net.Com.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com
{
    public class NetSocket
       : NetBase, INetSocket
    {
        #region Events
        public event EventHandler<MessageEventArgs> BindSocket;
        public event EventHandler<MessageEventArgs> ConnectClientSocket;
        #endregion

        #region OnRaiseEvent Methods
        public void OnBindSocket(object sender, MessageEventArgs e)
            => BindSocket?.Invoke(sender, e);

        public void OnConnectClientSocket(object sender, MessageEventArgs e)
            => ConnectClientSocket.Invoke(sender, e);
        #endregion

        public NetSocket(uint Signature1)
            : base(Signature1) { }
    }
}
