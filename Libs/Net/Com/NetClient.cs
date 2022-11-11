using Nox.Net.Com.Message.Defaults;
using Nox.Security;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Intrinsics.X86;

namespace Nox.Net.Com
{
    public class NetClient<T>
        : NetBase where T : SocketListener
    {
        public event EventHandler<PingEventArgs> OnPingMessage;
        public event EventHandler<EchoEventArgs> OnEchoMessage;
        public event EventHandler<EhloEventArgs> OnEhloMessage;
        public event EventHandler<RplyEventArgs> OnRplyMessage;
        public event EventHandler<RespEventArgs> OnRespMessage;

        private SocketListener _Listener;
        private TcpClient _Client;

        private string _ServerIP = "";

        #region Properties
        public Guid Id { get; } = Guid.NewGuid();
        public string ServerIP { get { return _ServerIP; } }

        public string SocketMessage { get; set; }

        public bool IsConnected =>
           _Listener?.IsConnected ?? false;

        public tinyKey publicKey { get; set; } = null!;


        public int ReceiveBufferLength => _Listener?.ReceiveBufferLength ?? 0;
        public int MessageCount => _Listener?.MessageCount ?? 0;
        #endregion

        public virtual void Connect(string IP, int Port)
        {
            StopClient();

            _Client = new TcpClient();
            _Client.Connect(new IPEndPoint(IPAddress.Parse(IP), Port));

            _Listener = (T)Activator.CreateInstance(typeof(T), Signature1, _Client.Client);
            _Listener.SocketMessage = SocketMessage;
            _Listener.publicKey = publicKey;


            // pass through
            _Listener.OnPingMessage += (object sender, PingEventArgs e) =>
                OnPingMessage?.Invoke(sender, e);
            _Listener.OnEchoMessage += (object sender, EchoEventArgs e) =>
                OnEchoMessage?.Invoke(sender, e);
            _Listener.OnEhloMessage += (object sender, EhloEventArgs e) =>
                OnEhloMessage?.Invoke(sender, e);
            _Listener.OnRplyMessage += (object sender, RplyEventArgs e) =>
                OnRplyMessage?.Invoke(sender, e);
            _Listener.OnRespMessage += (object sender, RespEventArgs e) =>
                OnRespMessage?.Invoke(sender, e);

            _Listener.StartListener();

            _ServerIP = IP;
        }

        public void StopClient()
        {
            if (_Listener != null)
            {
                _Listener.StopListener(); ;
                _Listener = null;
            }
            _ServerIP = "";
        }

        public bool SendBuffer(byte[] byteBuffer)
        {
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

        public bool SendDataBlock(DataBlock data) =>
            SendBuffer(data.Write());

        public override void Dispose() =>
            StopClient();

        public NetClient(uint Signature1)
            : base(Signature1) { }


        ~NetClient() =>
            StopClient();
    }
}
