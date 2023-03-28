using Nox.Net.Com.Message;
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.Xml;

namespace Nox.Net.Com
{
    public interface INetBase
    {
        /// <summary>
        /// raised if communication should be terminated
        /// </summary>
        event EventHandler<MessageCancelEventArgs> Terminate;

        /// <summary>
        /// raised if socket will close
        /// </summary>
        event EventHandler<MessageEventArgs> CloseSocket;

        /// <summary>
        /// raised if a message received
        /// </summary>
        event EventHandler<MessageEventArgs> Message;

        /// <summary>
        /// will raise the Terminate event
        /// </summary>
        /// <param name="sender">sender object, usually the current object</param>
        /// <param name="e">MessageCancelEventArgs</param>
        void OnTerminate(object sender, MessageCancelEventArgs e);

        /// <summary>
        /// will raise the CloseSocket event
        /// </summary>
        /// <param name="sender">sender object, usually the current object</param>
        /// <param name="e">MessageEventArgs</param>
        void OnCloseSocket(object sender, MessageEventArgs e);

        /// <summary>
        /// will raise the message event
        /// </summary>
        /// <param name="MessageEventArgs">sender object, usually the current object</param>
        /// <param name="e"></param>
        void OnMessage(object sender, MessageEventArgs e);

        
    }

    public class NetBase
        : INetBase, IDisposable
    {
        #region Properties
        private uint _Signature1;
        public uint Signature1 { get => _Signature1; }
        #endregion

        #region Events
        public event EventHandler<MessageCancelEventArgs> Terminate;
        public event EventHandler<MessageEventArgs> CloseSocket;
        public event EventHandler<MessageEventArgs> Message;
        public event EventHandler<ObtainCancelMessageEventArgs> ObtainMessage;

        #endregion

        #region OnRaiseEvent Methods
        public void OnTerminate(object sender, MessageCancelEventArgs e)
            => Terminate?.Invoke(sender, e);

        public void OnCloseSocket(object sender, MessageEventArgs e)
            => CloseSocket?.Invoke(sender, e);

        public void OnMessage(object sender, MessageEventArgs e)
            => Message?.Invoke(sender, e);

        public void OnObtainMessage(object sender, ObtainCancelMessageEventArgs e) =>
            ObtainMessage?.Invoke(sender, e);
        #endregion

        public NetBase(uint Signature1) =>
           _Signature1 = Signature1;

        public virtual void Dispose() { }
    }

    public interface INetServerBase
    {
        #region Events
        public event EventHandler<MessageEventArgs> Bind;
        public event EventHandler<RplyEventArgs> RplyMessage;
        public event EventHandler<SigvEventArgs> SigvMessage;
        public event EventHandler<KeyvEventArgs> KeyvMessage;
        #endregion

        #region OnRaiseEvent Methods
        void OnBind(object sender, MessageEventArgs e);
        void OnRplyMessage(object sender, RplyEventArgs e);
        void OnSigvMessage(object sender, SigvEventArgs e);
        void OnKeyvMessage(object sender, KeyvEventArgs e);
        #endregion
    }

    public interface INetClientBase
    {
        #region Events
        public event EventHandler<MessageEventArgs> Connect;
        public event EventHandler<EhloEventArgs> EhloMessage;
        public event EventHandler<SigxEventArgs> SigxMessage;
        public event EventHandler<KeyxEventArgs> KeyxMessage;
        #endregion

        #region OnRaiseEvent Methods
        void OnConnect(object sender, MessageEventArgs e);
        void OnEhloMessage(object sender, EhloEventArgs e);
        void OnSigxMessage(object sender, SigxEventArgs e);
        void OnKeyxMessage(object sender, KeyxEventArgs e);
        #endregion
    }

    public interface INetSocketBase
        : INetServerBase, INetClientBase
    {
        #region Events
        public event EventHandler<PingEventArgs> PingMessage;
        public event EventHandler<EchoEventArgs> EchoMessage;

        public event EventHandler<RespEventArgs> RespMessage;
        public event EventHandler<PublicKeyEventArgs> ObtainPublicKey;
        #endregion

        #region OnRaiseEvent Methods
        public void OnPingMessage(object sender, PingEventArgs e);
        public void OnEchoMessage(object sender, EchoEventArgs e);  
        public void OnRespMessage(object sender, RespEventArgs e);  
        public void OnObtainPublicKey(object sender, PublicKeyEventArgs e);
        #endregion
    }

    public class NetSocketBase 
        : NetBase, INetSocketBase
    {
        public Guid Id { get; } = Guid.NewGuid();

        #region Events
        public event EventHandler<PingEventArgs> PingMessage;
        public event EventHandler<EchoEventArgs> EchoMessage;

        public event EventHandler<MessageEventArgs> Bind;
        public event EventHandler<RplyEventArgs> RplyMessage;
        public event EventHandler<SigvEventArgs> SigvMessage;
        public event EventHandler<KeyvEventArgs> KeyvMessage;

        public event EventHandler<MessageEventArgs> Connect;
        public event EventHandler<EhloEventArgs> EhloMessage;
        public event EventHandler<SigxEventArgs> SigxMessage;
        public event EventHandler<KeyxEventArgs> KeyxMessage;

        public event EventHandler<RespEventArgs> RespMessage;
        public event EventHandler<PublicKeyEventArgs> ObtainPublicKey;

        #endregion

        #region OnRaiseEvent Methods
        public void OnBind(object sender, MessageEventArgs e)
            => Bind?.Invoke(sender, e);
        public void OnRplyMessage(object sender, RplyEventArgs e) =>
            RplyMessage?.Invoke(sender, e);
        public void OnSigvMessage(object sender, SigvEventArgs e) =>
            SigvMessage?.Invoke(sender, e);
        public void OnKeyvMessage(object sender, KeyvEventArgs e) =>
            KeyvMessage?.Invoke(sender, e);


        public void OnConnect(object sender, MessageEventArgs e)
            => Connect.Invoke(sender, e);
        public void OnEhloMessage(object sender, EhloEventArgs e) =>
            EhloMessage?.Invoke(sender, e);
        public void OnSigxMessage(object sender, SigxEventArgs e) =>
            SigxMessage?.Invoke(sender, e);
        public void OnKeyxMessage(object sender, KeyxEventArgs e) =>
            KeyxMessage?.Invoke(sender, e);


        public void OnPingMessage(object sender, PingEventArgs e) => 
            PingMessage?.Invoke(sender, e);
        public void OnEchoMessage(object sender, EchoEventArgs e) =>
            EchoMessage?.Invoke(sender, e);

        public void OnRespMessage(object sender, RespEventArgs e) =>
            RespMessage?.Invoke(sender, e);
        public void OnObtainPublicKey(object sender, PublicKeyEventArgs e) =>
            ObtainPublicKey?.Invoke(sender, e);
        #endregion

        public NetSocketBase(uint Signature1)
            : base(Signature1) { }
    }
}
