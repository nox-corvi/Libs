using Nox.Net.Com.Message;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nox.Net.Com
{
    public class NetBase : IDisposable
    {

        #region Events
        public event EventHandler<MessagePingEventArgs> PingMessage;
        public event EventHandler<MessageEchoEventArgs> EchoMessage;
        
        public event EventHandler<MessageEhloEventArgs> EhloMessage;
        public event EventHandler<MessageRplyEventArgs> RplyMessage;
        public event EventHandler<MessageSigxEventArgs> SigxMessage;
        public event EventHandler<MessageSigvEventArgs> SigvMessage;
        public event EventHandler<MessageKeyxEventArgs> KeyxMessage;
        public event EventHandler<MessageKeyvEventArgs> KeyvMessage;
        public event EventHandler<MessageRespEventArgs> RespMessage;

        public event EventHandler<ObtainPublicKeyEventArgs> ObtainPublicKey;
        public event EventHandler<ObtainMessageEventArgs> ObtainMessage;

        public event EventHandler<CloseSocketEventArgs> CloseSocket;
        public event EventHandler<MessageEventArgs> Message;
        #endregion


        private uint _Signature1;

        #region Properties
        public uint Signature1 { get => _Signature1; }
        #endregion


        #region OnRaiseEvent Methods
        protected virtual void OnPingMessage(object sender, MessagePingEventArgs e) => 
            PingMessage?.Invoke(sender, e);

        protected virtual void OnEchoMessage(object sender, MessageEchoEventArgs e) =>
            EchoMessage?.Invoke(sender, e);

        protected virtual void OnEhloMessage(object sender, MessageEhloEventArgs e) =>
            EhloMessage?.Invoke(sender, e);

        protected virtual void OnRplyMessage(object sender, MessageRplyEventArgs e) =>
            RplyMessage?.Invoke(sender, e);

        protected virtual void OnSigxMessage(object sender, MessageSigxEventArgs e) =>
            SigxMessage?.Invoke(sender, e);

        protected virtual void OnSigvMessage(object sender, MessageSigvEventArgs e) =>
            SigvMessage?.Invoke(sender, e);

        protected virtual void OnKeyxMessage(object sender, MessageKeyxEventArgs e) =>
            KeyxMessage?.Invoke(sender, e);

        protected virtual void OnKeyvMessage(object sender, MessageKeyvEventArgs e) =>
            KeyvMessage?.Invoke(sender, e);

        protected virtual void OnRespMessage(object sender, MessageRespEventArgs e) =>
            RespMessage?.Invoke(sender, e);

        protected virtual void OnObtainPublicKey(object sender, ObtainPublicKeyEventArgs e) =>
            ObtainPublicKey?.Invoke(sender, e);

        protected virtual void OnObtainMessage(object sender, ObtainMessageEventArgs e) =>
            ObtainMessage?.Invoke(sender, e);

        protected virtual void OnCloseSocket(object sender, CloseSocketEventArgs e) =>
            CloseSocket?.Invoke(sender, e);

        protected virtual void OnMessage(object sender, MessageEventArgs e) =>
            Message?.Invoke(sender, e);
        #endregion

        public NetBase(uint Signature1) =>
            _Signature1 = Signature1;

        public virtual void Dispose() { }
    }
}
