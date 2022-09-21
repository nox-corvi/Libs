using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nox.Net.Com
{
    public abstract class SocketListener : NetBase
    {
        private const int BUFFER_SIZE = 1024;
        private const int RECEIVE_BUFFER_SIZE = 32768;

        public int _ReceiveTimeout  = 15;

        private Socket _Socket = null;
        private Thread _ListenerThread = null;

        private bool _StopClient = false;
        private bool _Delete = false;

        private DateTime _LastResponse = DateTime.UtcNow;

        public Guid Id { get; } = Guid.NewGuid();

        public bool IsConnected =>
            _Socket?.Connected ?? false;

        public bool Delete =>
            _Delete;

        public int ReceiveTimeout =>
            _ReceiveTimeout;

        public void StartListener()
        {
            if (_Socket != null)
                (_ListenerThread = new Thread(new ThreadStart(ListenerThreadStart))).Start();
        }

        private void ListenerThreadStart()
        {
            int size = 0;
            byte[] byteBuffer = new byte[BUFFER_SIZE];

            _LastResponse = DateTime.UtcNow;

            using (Timer t = new Timer(t =>
            {
                // stop if timout reached
                if (ReceiveTimeout != 0 && DateTime.UtcNow.Subtract(_LastResponse).TotalSeconds > ReceiveTimeout)
                    StopListener();

            }, null, ReceiveTimeout, ReceiveTimeout))
            {
                while (!_StopClient)
                {
                    try
                    {
                        size = _Socket.Receive(byteBuffer);
                        _LastResponse = DateTime.Now;


                        ParseReceiveBuffer(byteBuffer, 0, size);
                    }
                    catch (SocketException)
                    {
                        _StopClient = true;
                        _Delete = true;
                    }
                }

                t.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        public void StopListener()
        {
            if (_Socket != null)
            {
                _StopClient = true;
                _Socket.Close();

                _ListenerThread.Join(1000);
                _ListenerThread = null;

                _Socket = null;
                _Delete = true;
            }
        }

        public void SendBuffer(byte[] byteButter) =>
            _Socket.Send(byteButter);

        public void SendDataBlock(DataBlock data) =>
            SendBuffer(data.Write());

        public abstract void ParseReceiveBuffer(byte[] byteBuffer, int Offset, int size);

        public SocketListener(uint Signature1, Socket Socket)
            : base(Signature1) =>
            this._Socket = Socket;           

        public SocketListener(uint Signature1, Socket Socket, int ReceiveTimeout)
            : this(Signature1, Socket) => this._ReceiveTimeout = ReceiveTimeout;

        ~SocketListener() =>
            StopListener();
    }
}
