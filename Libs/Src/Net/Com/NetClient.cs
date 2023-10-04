using Microsoft.Extensions.Logging;
using Nox.Net.Com.Message;
using Nox.Security;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Nox.Net.Com
{
    public abstract class NetClient<T>
        : NetBase, INetClient where T : SocketListener, IRunner
    {
        private readonly ILogger _Logger;

        private T _Listener;
        private TcpClient _Client;

        private string _ServerIP = "";

        public string T1 => _Listener?.Id.ToString() ?? "<null>";
        public string T2 => _Client?.Client?.Handle.ToString() ?? "<null>";


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
            StopClient();

            _Client = new TcpClient();
            _Client.Connect(new IPEndPoint(IPAddress.Parse(IP), Port));

            // create instance
            _Listener = (T)Activator.CreateInstance(typeof(T), Signature1, _Client.Client, _Logger, 0);
            _Listener.Terminate += (object sender, EventArgs e) =>
            {
                // notify
                OnTerminate(sender, e);

                // and close
                StopClient();
            };
            _Listener.Message += OnMessage;
            _Listener.Initialize();
         
            BindEvents(_Listener as T);
         
            _Listener.Run();

            _ServerIP = IP;
        }

        public void StopClient()
        {
            (_Listener as T)?.Done(); ;
            _Listener = null;
            
            _Client?.Dispose();

            _ServerIP = "";
        }

        public bool SendBuffer(byte[] byteBuffer)
        {
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
            => SendBuffer(data.Write());

        public override void Dispose()
            => StopClient();

        public NetClient(uint Signature1, ILogger logger)
            : this(Signature1)
            => this._Logger = logger;

        public NetClient(uint Signature1)
            : base(Signature1) { }
    }

    public class NetGenericClient<T>
        : NetClient<T>, INetClient, INetGenericMessages where T : GenericSocketListener, IRunner
    {
        #region Events
        public event EventHandler<PingMessageEventArgs> PingMessage;
        public event EventHandler<EchoMessageEventArgs> EchoMessage;
        public event EventHandler<RespMessageEventArgs> RespMessage;
        public event EventHandler<MessageEventArgs> ObtainRplyMessage;
        #endregion

        #region OnRaiseEvent Methods
        public void OnPingMessage(object sender, PingMessageEventArgs e)
            => PingMessage?.Invoke(sender, e);

        public void OnEchoMessage(object sender, EchoMessageEventArgs e)
            => EchoMessage?.Invoke(sender, e);

        public void OnRespMessage(object sender, RespMessageEventArgs e)
            => RespMessage?.Invoke(sender, e);

        public void OnObtainRplyMessage(object sender, MessageEventArgs e)
            => ObtainRplyMessage?.Invoke(sender, e);
        #endregion

        protected override void BindEvents(T SocketListener)
        {
            SocketListener.PingMessage += OnPingMessage;
            SocketListener.EchoMessage += OnEchoMessage;
            SocketListener.RespMessage += OnRespMessage;
            SocketListener.ObtainRplyMessage += OnObtainRplyMessage;
        }

        public NetGenericClient(uint Signature1, ILogger logger)
            : base(Signature1, logger) { }
        public NetGenericClient(uint Signature1)
            : base(Signature1) { }

    }

    public class NetSecureClient<T>
        : NetGenericClient<T>, INetClient, INetSecureClientMessages where T : SecureSocketListener, IRunner
    {
        #region Events
        public event EventHandler<RplyEventArgs> RplyMessage;
        public event EventHandler<SigvEventArgs> SigvMessage;
        public event EventHandler<KeyvEventArgs> KeyvMessage;
        public event EventHandler<CRawEventArgs> CRawMessage;
        public event EventHandler<URawEventArgs> URawMessage;

        public event EventHandler<PublicKeyEventArgs> ObtainPublicKey;
        #endregion

        #region OnRaiseEvent Methods
        public void OnRplyMessage(object sender, RplyEventArgs e)
            => RplyMessage?.Invoke(sender, e);

        public void OnSigvMessage(object sender, SigvEventArgs e)
            => SigvMessage?.Invoke(sender, e);

        public void OnKeyvMessage(object sender, KeyvEventArgs e)
            => KeyvMessage?.Invoke(sender, e);

        public void OnCRawMessage(object sender, CRawEventArgs e)
            => CRawMessage?.Invoke(sender, e);

        public void OnURawMessage(object sender, URawEventArgs e)
            => URawMessage?.Invoke(sender, e);

        public void OnObtainPublicKey(object sender, PublicKeyEventArgs e)
            => ObtainPublicKey?.Invoke(sender, e);
        #endregion

        protected override void BindEvents(T SocketListener)
        {
            base.BindEvents(SocketListener);

            SocketListener.RplyMessage += OnRplyMessage;
            SocketListener.SigvMessage += OnSigvMessage;
            SocketListener.KeyvMessage += OnKeyvMessage;
            SocketListener.CRawMessage += OnCRawMessage;
            SocketListener.URawMessage += OnURawMessage;

            SocketListener.ObtainPublicKey  += OnObtainPublicKey;
        }

        public NetSecureClient(uint Signature1, ILogger logger)
            : base(Signature1, logger) { }

        public NetSecureClient(uint Signature1)
            : base(Signature1) { }

    }
}
