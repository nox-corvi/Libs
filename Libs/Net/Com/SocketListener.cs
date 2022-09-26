using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private const uint EOM = 0xFEFE;

        private BackgroundWorker _Listener;
        private BackgroundWorker _MessageProcess;

        private Socket _Socket = null;
        private Thread _ListenerThread = null;
        private bool _StopClient = false;

        private bool _Delete = false;
        private int _ReceiveTimeout = 15;

        private DateTime _LastResponse = DateTime.UtcNow;

        private List<byte> _ReceiveBuffer = new();
        private List<byte[]> _MessageBuffer = new();

        #region Properties
        public Guid Id { get; } = Guid.NewGuid();

        public bool IsConnected =>
            _Socket?.Connected ?? false;

        public bool Delete =>
            _Delete;

        public int ReceiveTimeout =>
            _ReceiveTimeout;
        #endregion

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
                        if (_Socket.Available > 0)
                        {
                            size = _Socket.Receive(byteBuffer);

                            _LastResponse = DateTime.Now;
                            lock (_ReceiveBuffer)
                            {
                                _ReceiveBuffer.AddRange(byteBuffer);
                                ParseReceiveBuffer();
                            }
                        }
                        Thread.Sleep(0);
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
                        if (_Socket.Available > 0)
                        {
                            size = _Socket.Receive(byteBuffer);

                            _LastResponse = DateTime.Now;
                            lock (_ReceiveBuffer)
                            {
                                _ReceiveBuffer.AddRange(byteBuffer);
                                ParseReceiveBuffer();
                            }
                        }
                        Thread.Sleep(0);
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

                _ListenerThread?.Join(1000);
                _ListenerThread = null;

                _Socket = null;
                _Delete = true;
            }
        }

        public void SendBuffer(byte[] byteButter) =>
            _Socket.Send(byteButter);

        public void SendDataBlock(DataBlock data) =>
            SendBuffer(data.Write());

        public void ParseMessage(byte[] Message)
        {

        }

        private void PreParseMessage(byte[] Message)
        {
            //int i = 0, p = sizeof(uint);
            //uint Sig1 = BitConverter.ToUInt32(Message, i);
            //if (Sig1 != Signature1)
            //    throw new Exception("invalid signature");

            //i += p;
            //uint Sig2 = BitConverter.ToUInt32(Message, i);

            //switch (Sig2)
            //{
            //    case 0x3001:
            //        // client ping
            //        var cp = new ClientPingMessage(Signature1);
            //        cp.Read(Message);
            //        var cpd = new ClientPing(Signature1, "127.0.0.1");
            //        cpd.Read(cp.Data);
            //        break;
            //}
        }



        public void ParseMessage(byte[] Message)
        {

        }

        public void ParseReceiveBuffer()
        {
            int Start = 0, End = -1;

            for (int i = 0; i < _ReceiveBuffer.Count; i++)
            {
                try
                {
                    // get 4 bytes
                    var p = new byte[sizeof(uint)];
                    _ReceiveBuffer.CopyTo(i, p, 0, p.Length);

                    if (BitConverter.ToUInt32(p, 0) == Signature1)
                        Start = i;

                    if (BitConverter.ToUInt32(p, 0) == EOM)
                        End = i;

                    if (End > Start)
                    {
                        byte[] Message = new byte[End - Start + 4];
                        
                        _ReceiveBuffer.CopyTo(Start, Message, 0, Message.Length);

                        // remove message block, give up leading data 
                        _ReceiveBuffer.RemoveRange(Start, End+4);

                        // parse 
                        ParseMessage(Message);

                        break;
                    }
                }
                catch (System.ArgumentException)
                {
                    return;
                }
            }

            // remove leading data to keep receivebuffer in range
            while (_ReceiveBuffer.Count > RECEIVE_BUFFER_SIZE)
                _ReceiveBuffer.RemoveRange(0, _ReceiveBuffer.Count - RECEIVE_BUFFER_SIZE);
        }

        public SocketListener(uint Signature1, Socket Socket)
            : base(Signature1) =>
            this._Socket = Socket;           

        public SocketListener(uint Signature1, Socket Socket, int ReceiveTimeout)
            : this(Signature1, Socket) => this._ReceiveTimeout = ReceiveTimeout;


        ~SocketListener() =>
            StopListener();
    }
}
