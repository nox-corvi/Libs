using Nox.Net.Com.Message;
using Nox.Security;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Intrinsics.X86;

namespace Nox.Net.Com
{
    public abstract class NetClient<T>
        : NetBase, INetClient where T : SocketListener, IRunner
    {
        private Log4 Log = Log4.Create();

        private INetSocket _Listener;
        private TcpClient _Client;

        private string _ServerIP = "";

        #region Properties
        public string ServerIP { get { return _ServerIP; } }

        public bool IsConnected { get => (_Listener as T)?.IsInitialized ?? false; }

        public int SocketBufferLength { get => (_Listener as T)?.SocketBufferLength ?? 0; }
        public int MessageBufferLength { get => (_Listener as T)?.MessageBufferLength ?? 0; }

        //protected T TListener => (_Listener as T);
        #endregion

        #region Events
        public event EventHandler<MessageEventArgs> ConnectClientSocket;
        #endregion

        #region OnRaiseEvent Methods
        public void OnConnectClientSocket(object sender, MessageEventArgs e)
            => ConnectClientSocket?.Invoke(sender, e);
        #endregion

        protected abstract void BindEvents(T SocketListener);

        public virtual void Connect(string IP, int Port)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, IP, Port);

            StopClient();

            _Client = new TcpClient();
            _Client.Connect(new IPEndPoint(IPAddress.Parse(IP), Port));

            // create instance
            _Listener = (T)Activator.CreateInstance(typeof(T), Signature1, _Client.Client);
            (_Listener as T)?.Initialize();

            BindEvents(_Listener as T);
            
            _ServerIP = IP;
        }

        public void StopClient()
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace);
            if (_Listener != null)
            {
                (_Listener as T).Done(); ;
                _Listener = null;
            }
            _Client?.Dispose();

            _ServerIP = "";
        }

        public bool SendBuffer(byte[] byteBuffer)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, byteBuffer);
            try
            {
                if ((_Listener as T).IsInitialized)
                {
                    (_Listener as T).SendBuffer(byteBuffer);
                    return true;
                }
                else
                    return true;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }

        public bool SendDataBlock(DataBlock data)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, data);

            return SendBuffer(data.Write());
        }


        public override void Dispose()
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace);
            StopClient();
        }

        public NetClient(uint Signature1)
            : base(Signature1) { }


        ~NetClient()
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace);
            StopClient();
        }
    }

    public class NetGenericClient<T>
        : NetClient<T>, INetClient, INetGenericMessages where T : GenericSocketListener, IRunner
    {
        #region Events
        public event EventHandler<PingMessageEventArgs> PingMessage;
        
        public event EventHandler<EchoMessageEventArgs> EchoMessage;

        /// <summary>
        /// raised if a message respone occurs
        /// </summary>
        public event EventHandler<RespMessageEventArgs> RespMessage;

        /// <summary>
        /// raised if a message will obtained
        /// </summary>
        public event EventHandler<ObtainMessageEventArgs> ObtainMessage;

        /// <summary>
        /// raised if a message will obtained. event can be canceled
        /// </summary>
        public event EventHandler<ObtainCancelMessageEventArgs> ObtainCancelMessage;
        #endregion

        #region OnRaiseEvent Methods
        public void OnPingMessage(object sender, PingMessageEventArgs e) 
            => PingMessage?.Invoke(sender, e);


        public void OnEchoMessage(object sender, EchoMessageEventArgs e)
            => EchoMessage?.Invoke(sender, e);

        public void OnRespMessage(object sender, RespMessageEventArgs e)
            => RespMessage?.Invoke(sender, e);

        public void OnObtainMessage(object sender, ObtainMessageEventArgs e) 
            => ObtainMessage?.Invoke(sender, e);

        public void OnObtainCancelMessage(object sender, ObtainCancelMessageEventArgs e)   
            => ObtainCancelMessage?.Invoke(sender, e);
        #endregion

        protected override void BindEvents(T SocketListener)
        {
            SocketListener.PingMessage += OnPingMessage;
            SocketListener.EchoMessage += OnEchoMessage;
            SocketListener.RespMessage += OnRespMessage;
            SocketListener.ObtainMessage += OnObtainMessage;
            SocketListener.ObtainCancelMessage += OnObtainCancelMessage;
        }

        public NetGenericClient(uint Signature1)
            : base(Signature1) { }

    }

    public class NetSecureClient<T>
        : NetGenericClient<T>, INetClient, INetSecureClientMessages where T : SecureSocketListener, IRunner
    {
        #region Events
        public event EventHandler<EhloEventArgs> EhloMessage;
        public event EventHandler<KeyxEventArgs> KeyxMessage;
        public event EventHandler<SigxEventArgs> SigxMessage;

        public event EventHandler<PublicKeyEventArgs> ObtainPublicKey;
        #endregion

        #region OnRaiseEvent Methods
        public void OnEhloMessage(object sender, EhloEventArgs e)
            => EhloMessage?.Invoke(sender, e);

        public void OnSigxMessage(object sender, SigxEventArgs e)
            => SigxMessage?.Invoke(sender, e);

        public void OnKeyxMessage(object sender, KeyxEventArgs e)
            => KeyxMessage?.Invoke(sender, e);

        public void OnObtainPublicKey(object sender, PublicKeyEventArgs e)
            => ObtainPublicKey?.Invoke(sender, e);
        #endregion

        protected override void BindEvents(T SocketListener)
        {
            base.BindEvents(SocketListener);

            SocketListener.EhloMessage += OnEhloMessage;
            SocketListener.SigxMessage += OnSigxMessage;
            SocketListener.KeyxMessage += OnKeyxMessage;
            SocketListener.ObtainPublicKey  += OnObtainPublicKey;
        }

        public NetSecureClient(uint Signature1)
            : base(Signature1) { }

    }
}
