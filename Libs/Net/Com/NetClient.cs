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

        protected T TListener => (_Listener as T);
        #endregion

        #region Events
        public event EventHandler<MessageEventArgs> ConnectClient;
        public event EventHandler<EhloEventArgs> EhloMessage;
        public event EventHandler<SigxEventArgs> SigxMessage;
        public event EventHandler<KeyxEventArgs> KeyxMessage;
        #endregion

        #region OnRaiseEvent Methods
        public void OnConnectClient(object sender, MessageEventArgs e)
            => ConnectClient?.Invoke(sender, e);

        public void OnEhloMessage(object sender, EhloEventArgs e)
            => EhloMessage?.Invoke(sender, e);

        public void OnSigxMessage(object sender, SigxEventArgs e)
            => SigxMessage?.Invoke(sender, e);

        public void OnKeyxMessage(object sender, KeyxEventArgs e)
            => KeyxMessage?.Invoke(sender, e);
        #endregion

        protected abstract void BindEvents();

        public virtual void Connect(string IP, int Port)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, IP, Port);

            StopClient();

            _Client = new TcpClient();
            _Client.Connect(new IPEndPoint(IPAddress.Parse(IP), Port));

            // create instance
            _Listener = (T)Activator.CreateInstance(typeof(T), Signature1, _Client.Client);
            TListener?.Initialize();

            BindEvents();
            
            _ServerIP = IP;
        }

        public void StopClient()
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace);
            if (_Listener != null)
            {
                TListener.Done(); ;
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
                if (TListener.IsInitialized)
                {
                    TListener.SendBuffer(byteBuffer);
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
        : NetClient<T>, INetClient, INetGenericMessages where T : SocketListener, IRunner
    {
        #region Events
        public event EventHandler<RespMessageEventArgs> RespMessage;
        #endregion

        #region OnRaiseEvent Methods
        public void OnRespMessage(object sender, MessageEventArgs e)
            => RespMessage?.Invoke(sender, e);
        #endregion

        protected override void BindEvents()
        {
            TListener.CloseSocket += (object sender, MessageEventArgs e) =>
               OnCloseSocket(sender, e);
        }

        public NetGenericClient(uint Signature1)
            : base(Signature1) { }

    }

    public class NetSecureClient<T>
        : NetGenericClient<T>, INetClient, INetSecureMessages where T : SecureSocketListener, IRunner
    {
        #region Events
        
        #endregion

        #region OnRaiseEvent Methods
        public void OnConnectClient(object sender, MessageEventArgs e)
            => ConnectClient?.Invoke(sender, e);

        public void OnEhloMessage(object sender, EhloEventArgs e)
            => EhloMessage?.Invoke(sender, e);

        public void OnSigxMessage(object sender, SigxEventArgs e)
            => SigxMessage?.Invoke(sender, e);

        public void OnKeyxMessage(object sender, KeyxEventArgs e)
            => KeyxMessage?.Invoke(sender, e);
        #endregion

        protected override void BindEvents()
        {
            base.BindEvents();



            // pass through
            //TListener.PingMessage += (object sender, PingEventArgs e) =>
            //    OnPingMessage(sender, e);
            //TListener.EchoMessage += (object sender, EchoEventArgs e) =>
            //    OnEchoMessage(sender, e);

            //_Listener.EhloMessage += (object sender, EhloEventArgs e) =>
            //    OnEhloMessage(sender, e);
            ////_Listener.RplyMessage += (object sender, RplyEventArgs e) =>
            ////    OnRplyMessage(sender, e);
            //_Listener.SigxMessage += (object sender, SigxEventArgs e) =>
            //    OnSigxMessage(sender, e);
            ////_Listener.SigvMessage += (object sender, SigvEventArgs e) =>
            ////    OnSigvMessage(sender, e);
            //_Listener.KeyxMessage += (object sender, KeyxEventArgs e) =>
            //    OnKeyxMessage(sender, e);
            ////_Listener.KeyvMessage += (object sender, KeyvEventArgs e) =>
            ////    OnKeyvMessage(sender, e);
            ////_Listener.RespMessage += (object sender, RespEventArgs e) =>
            ////    OnRespMessage(sender, e);

            //(_Listener as T).CloseSocket += (object sender, MessageEventArgs e) =>
            //   OnCloseSocket(sender, e);
            //(_Listener as T).Message += (object sender, MessageEventArgs e) =>
            //    OnMessage(sender, e);

            //(_Listener as T).ObtainMessage += (object sender, ObtainMessageEventArgs e) =>
            //    OnObtainMessage(sender, e);
            _Listener.ObtainPublicKey += (object sender, PublicKeyEventArgs e) =>
            {

            };

        }

        public NetSecureClient(uint Signature1)
            : base(Signature1) { }

    }
}
