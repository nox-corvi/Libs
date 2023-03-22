using Nox.Net.Com.Message;
using Nox.Security;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Intrinsics.X86;

namespace Nox.Net.Com
{
    public class NetClient<T>
        : NetClientBase where T : NetClientSocketListener
    {
        private Log4 Log = Log4.Create();

        private SocketListener _Listener;
        private TcpClient _Client;

        private string _ServerIP = "";

        #region Properties
        public Guid Id { get; } = Guid.NewGuid();
        public string ServerIP { get { return _ServerIP; } }

        public bool IsConnected { get => _Listener?.IsConnected ?? false; }

        public int ReceiveBufferLength { get => _Listener?.ReceiveBufferLength ?? 0; }
        public int MessageCount { get => _Listener?.MessageCount ?? 0; }
        #endregion

        public virtual void Connect(string IP, int Port)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, IP, Port);

            StopClient();

            _Client = new TcpClient();
            _Client.Connect(new IPEndPoint(IPAddress.Parse(IP), Port));

            _Listener = (T)Activator.CreateInstance(typeof(T), Signature1, _Client.Client);

            // pass through
            _Listener.PingMessage += (object sender, PingEventArgs e) =>
                OnPingMessage(sender, e);
            _Listener.EchoMessage += (object sender, EchoEventArgs e) =>
                OnEchoMessage(sender, e);

            _Listener.EhloMessage += (object sender, EhloEventArgs e) =>
                OnEhloMessage(sender, e);
            _Listener.RplyMessage += (object sender, RplyEventArgs e) =>
                OnRplyMessage(sender, e);
            _Listener.SigxMessage += (object sender, SigxEventArgs e) =>
                OnSigxMessage(sender, e);
            _Listener.SigvMessage += (object sender, SigvEventArgs e) =>
                OnSigvMessage(sender, e);
            _Listener.KeyxMessage += (object sender, KeyxEventArgs e) =>
                OnKeyxMessage(sender, e);
            _Listener.KeyvMessage += (object sender, KeyvEventArgs e) =>
                OnKeyvMessage(sender, e);
            _Listener.RespMessage += (object sender, RespEventArgs e) =>
                OnRespMessage(sender, e);

            _Listener.CloseSocket += (object sender, CloseSocketEventArgs e) =>
               OnCloseSocket(sender, e);
            _Listener.Message += (object sender, MessageEventArgs e) =>
                OnMessage(sender, e);

            _Listener.ObtainMessage += (object sender, ObtainMessageEventArgs e) =>
                OnObtainMessage(sender, e);
            _Listener.ObtainPublicKey += (object sender, ObtainPublicKeyEventArgs e) =>
                OnObtainPublicKey(sender, e);

            _Listener.StartListener();

            _ServerIP = IP;
        }

        public void StopClient()
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace);
            if (_Listener != null)
            {
                _Listener.StopListener(); ;
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
                if (_Listener.IsConnected)
                {
                    _Listener.SendBuffer(byteBuffer);
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
}
