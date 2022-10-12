using Nox.Net.Com.Message.Defaults;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Nox.Net.Com
{
    public class NetClient<T>
        : NetBase where T : SocketListener
    {
        public event EventHandler<EHLOReplyEventHandler> EHLOReply;

        private SocketListener _Listener;
        private TcpClient _Client;

        private string _ServerIP = "";

        #region Properties
        public Guid Id { get; } = Guid.NewGuid();
        public string ServerIP { get { return _ServerIP; } }

        public bool IsConnected =>
           _Listener?.IsConnected ?? false;
        #endregion

        public virtual void Connect(string IP, int Port)
        {
            StopClient();

            _Client = new TcpClient();
            _Client.Connect(new IPEndPoint(IPAddress.Parse(IP), Port));

            _Listener = (T)Activator.CreateInstance(typeof(T), Signature1, _Client.Client);

            // pass through
            _Listener.EHLOReply += (object sender, EHLOReplyEventHandler e) =>
                EHLOReply.Invoke(sender, e);

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
