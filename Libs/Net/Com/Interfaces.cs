using Nox.Net.Com.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com
{

    /// <summary>
    /// base interface for all net.com classes
    /// </summary>
    public interface INetBase
    {
        /// <summary>
        /// raised if a message received
        /// </summary>
        event EventHandler<MessageEventArgs> Message;

        /// <summary>
        /// raised if communication should be terminated
        /// </summary>
        event EventHandler<MessageCancelEventArgs> Terminate;

        /// <summary>
        /// raised if socket will close
        /// </summary>
        event EventHandler<MessageEventArgs> CloseSocket;

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

    /// <summary>
    /// base interface for all server classes
    /// </summary>
    public interface INetServer
    {
        #region Events
        /// <summary>
        /// raised if the server bind to a socket
        /// </summary>
        public event EventHandler<MessageEventArgs> BindSocket;
        #endregion

        #region OnRaiseEvent Methods
        /// <summary>
        /// will raise the bindsocket event
        /// </summary>
        /// <param name="sender">sender object, usually the current object</param>
        /// <param name="e"></param>
        void OnBindSocket(object sender, MessageEventArgs e);
        #endregion
    }

    /// <summary>
    /// base interface for all client classes
    /// </summary>
    public interface INetClient
    {
        #region Events
        /// <summary>
        /// raised if the client connect to a socket
        /// </summary>
        public event EventHandler<MessageEventArgs> ConnectClientSocket;
        #endregion

        #region OnRaiseEvent Methods
        /// <summary>
        /// will raise the connectclientsocket event
        /// </summary>
        /// <param name="sender">sender object, usually the current object</param>
        /// <param name="e"></param>
        void OnConnectClientSocket(object sender, MessageEventArgs e);
        #endregion
    }

    public interface INetGenericMessages
    {
        #region Events
        /// <summary>
        /// raised if a ping occurs
        /// </summary>
        event EventHandler<PingMessageEventArgs> PingMessage;

        /// <summary>
        /// raised if a ping echo occurs
        /// </summary>
        event EventHandler<EchoMessageEventArgs> EchoMessage;


        /// <summary>
        /// raised if a message respone occurs
        /// </summary>
        event EventHandler<RespMessageEventArgs> RespMessage;

        /// <summary>
        /// raised if a message will obtained
        /// </summary>
        event EventHandler<ObtainMessageEventArgs> ObtainMessage;

        /// <summary>
        /// raised if a message will obtained. event can be canceled
        /// </summary>
        event EventHandler<ObtainCancelMessageEventArgs> ObtainCancelMessage;
        #endregion

        #region OnRaiseEvent Methods
        /// <summary>
        /// will raise the pingmessage event
        /// </summary>
        /// <param name="sender">sender object, usually the current object</param>
        /// <param name="e"></param>
        void OnPingMessage(object sender, PingMessageEventArgs e);

        /// <summary>
        /// will raise the echomessage event
        /// </summary>
        /// <param name="sender">sender object, usually the current object</param>
        /// <param name="e"></param>
        void OnEchoMessage(object sender, EchoMessageEventArgs e);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender">sender object, usually the current object</param>
        /// <param name="e"></param>
        void OnRespMessage(object sender, RespMessageEventArgs e);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender">sender object, usually the current object</param>
        /// <param name="e"></param>
        void OnObtainMessage(object sender, ObtainMessageEventArgs e);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender">sender object, usually the current object</param>
        /// <param name="e"></param>
        void OnObtainCancelMessage(object sender, ObtainCancelMessageEventArgs e);
        #endregion
    }

    public interface INetSecureMessages
        : INetGenericMessages
    {
        /// <summary>
        /// raised if a public key need to obtained
        /// </summary>
        event EventHandler<PublicKeyEventArgs> ObtainPublicKey;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender">sender object, usually the current object</param>
        /// <param name="e"></param>
        void OnObtainPublicKey(object sender, PublicKeyEventArgs e);
    }

    public interface INetSecureClientMessages
        : INetSecureMessages
    {
        public event EventHandler<EhloEventArgs> EhloMessage;
        public event EventHandler<SigxEventArgs> SigxMessage;
        public event EventHandler<KeyxEventArgs> KeyxMessage;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender">sender object, usually the current object</param>
        /// <param name="e"></param>
        void OnEhloMessage(object sender, EhloEventArgs e);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender">sender object, usually the current object</param>
        /// <param name="e"></param>
        void OnSigxMessage(object sender, SigxEventArgs e);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender">sender object, usually the current object</param>
        /// <param name="e"></param>
        void OnKeyxMessage(object sender, KeyxEventArgs e);

    }

    public interface INetSecureServerMessages
        : INetSecureMessages
    {
        public event EventHandler<RplyEventArgs> RplyMessage;
        public event EventHandler<SigvEventArgs> SigvMessage;
        public event EventHandler<KeyvEventArgs> KeyvMessage;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender">sender object, usually the current object</param>
        /// <param name="e"></param>
        void OnRplyMessage(object sender, RplyEventArgs e);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender">sender object, usually the current object</param>
        /// <param name="e"></param>
        void OnSigvMessage(object sender, SigvEventArgs e);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender">sender object, usually the current object</param>
        /// <param name="e"></param>
        void OnKeyvMessage(object sender, KeyvEventArgs e);
    }

    public interface INetSocket
        : INetServer, INetClient
    {
    }

    public interface INetGenericSocket
        : INetSocket, INetGenericMessages
    {
    }

    public interface INetSecureSocket
        : INetSocket, INetSecureMessages, INetSecureClientMessages, INetSecureServerMessages
    {
    }
}
