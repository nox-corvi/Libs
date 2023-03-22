using Nox.Net.Com.Message;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Nox.Net.Com
{
    public class NetSocketBase 
        : IDisposable
    {

        #region Events
        public event EventHandler<PingEventArgs> PingMessage;
        public event EventHandler<EchoEventArgs> EchoMessage;

        public event EventHandler<BindEventArgs> Bind;
        public event EventHandler<RplyEventArgs> RplyMessage;
        public event EventHandler<SigvEventArgs> SigvMessage;
        public event EventHandler<KeyvEventArgs> KeyvMessage;

        public event EventHandler<ConnectEventArgs> Connect;
        public event EventHandler<EhloEventArgs> EhloMessage;
        public event EventHandler<SigxEventArgs> SigxMessage;
        public event EventHandler<KeyxEventArgs> KeyxMessage;


        public event EventHandler<RespEventArgs> RespMessage;
        public event EventHandler<ObtainPublicKeyEventArgs> ObtainPublicKey;
        public event EventHandler<ObtainMessageEventArgs> ObtainMessage;

        public event EventHandler<DoneEventArgs> Done;
        public event EventHandler<CloseSocketEventArgs> CloseSocket;
        public event EventHandler<MessageEventArgs> Message;
        #endregion

        private uint _Signature1;

        #region Properties
        public uint Signature1 { get => _Signature1; }
        #endregion


        #region OnRaiseEvent Methods
        protected virtual void OnBind(object sender, BindEventArgs e)
            => Bind?.Invoke(sender, e);
        protected virtual void OnRplyMessage(object sender, RplyEventArgs e) =>
            RplyMessage?.Invoke(sender, e);
        protected virtual void OnSigvMessage(object sender, SigvEventArgs e) =>
            SigvMessage?.Invoke(sender, e);
        protected virtual void OnKeyvMessage(object sender, KeyvEventArgs e) =>
            KeyvMessage?.Invoke(sender, e);


        protected virtual void OnConnect(object sender, ConnectEventArgs e)
            => Connect.Invoke(sender, e);
        protected virtual void OnEhloMessage(object sender, EhloEventArgs e) =>
            EhloMessage?.Invoke(sender, e);
        protected virtual void OnSigxMessage(object sender, SigxEventArgs e) =>
            SigxMessage?.Invoke(sender, e);
        protected virtual void OnKeyxMessage(object sender, KeyxEventArgs e) =>
            KeyxMessage?.Invoke(sender, e);


        protected virtual void OnPingMessage(object sender, PingEventArgs e) => 
            PingMessage?.Invoke(sender, e);
        protected virtual void OnEchoMessage(object sender, EchoEventArgs e) =>
            EchoMessage?.Invoke(sender, e);

        protected virtual void OnRespMessage(object sender, RespEventArgs e) =>
            RespMessage?.Invoke(sender, e);
        protected virtual void OnObtainPublicKey(object sender, ObtainPublicKeyEventArgs e) =>
            ObtainPublicKey?.Invoke(sender, e);
        protected virtual void OnObtainMessage(object sender, ObtainMessageEventArgs e) =>
            ObtainMessage?.Invoke(sender, e);

        protected virtual void OnDone(object sender, DoneEventArgs e) =>
            Done?.Invoke(sender, e);
        protected virtual void OnCloseSocket(object sender, CloseSocketEventArgs e) =>
            CloseSocket?.Invoke(sender, e);
        protected virtual void OnMessage(object sender, MessageEventArgs e) =>
            Message?.Invoke(sender, e);
        #endregion

        public NetSocketBase(uint Signature1) =>
            _Signature1 = Signature1;

        public virtual void Dispose() { }
    }
}
