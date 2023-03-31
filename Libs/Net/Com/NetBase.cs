using Nox.Net.Com.Message;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.Xml;

namespace Nox.Net.Com
{
    
    public class NetBase
        : INetBase, IDisposable
    {
        #region Properties
        public Guid Id { get; } = Guid.NewGuid();

        private uint _Signature1;
        public uint Signature1 { get => _Signature1; }
        #endregion

        #region Events
        public event EventHandler<MessageCancelEventArgs> Terminate;
        public event EventHandler<MessageEventArgs> CloseSocket;
        public event EventHandler<MessageEventArgs> Message;
        #endregion

        #region OnRaiseEvent Methods
        public void OnTerminate(object sender, MessageCancelEventArgs e)
            => Terminate?.Invoke(sender, e);

        public void OnCloseSocket(object sender, MessageEventArgs e)
            => CloseSocket?.Invoke(sender, e);

        public void OnMessage(object sender, MessageEventArgs e)
            => Message?.Invoke(sender, e);
        #endregion

        public NetBase(uint Signature1) =>
           _Signature1 = Signature1;

        public virtual void Dispose() { }
    }
}
