using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com
{
    public abstract class NetClient<T> 
        : NetBase where T : SocketListener
    {
        private SocketListener _Listener;
        private TcpClient _Client;

        private string _ServerIP = "";

        public Guid Id { get; } = Guid.NewGuid();
        public string ServerIP { get { return _ServerIP; } }

        public bool IsConnected =>
           _Listener?.IsConnected ?? false;

        public virtual void Connect(string IP, int Port)
        {
            StopClient();

            _Client = new TcpClient();
            _Client.Connect(new IPEndPoint(IPAddress.Parse(IP), Port));
            _Listener = (T)Activator.CreateInstance(typeof(T), _Client.Client);
            _Listener.StartListener();

            _ServerIP = IP;
        }

        public void SendBuffer(byte[] byteBuffer) =>
            _Listener.SendBuffer(byteBuffer);

        public void SendDataBlock(DataBlock data) =>
            SendBuffer(data.Write());

        public void StopClient()
        {
            if (_Listener != null)
            {
                _Listener.StopListener(); ;
                _Listener = null;
            }
            _ServerIP = "";
        }

        public override void Dispose() =>
            StopClient();


        public NetClient(uint Signature1)
            : base(Signature1) { }


        ~NetClient() =>
            StopClient();
    }
}
