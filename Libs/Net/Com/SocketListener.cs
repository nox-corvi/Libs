using Nox.Net.Com.Message.Defaults;
using Nox.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;

namespace Nox.Net.Com
{
    

    public abstract class SocketListener : NetBase
    {
        public event EventHandler<EHLOReplyEventHandler> EHLOReply;

        private const int BUFFER_SIZE = 1024;
        private const int RECEIVE_BUFFER_SIZE = 32768;
        private const uint EOM = 0xFEFE;

        private BetterBackgroundWorker _Listener = null;
        private BetterBackgroundWorker _MessageProcess = null;

        private Socket _Socket = null;

        private bool _Remove = false;
        private int _ReceiveTimeout = 0;

        private DateTime _LastResponse = DateTime.UtcNow;

        private List<byte> _ReceiveBuffer = new List<byte>();
        private List<byte[]> _MessageBuffer = new List<byte[]>();

        #region Properties
        public Guid Id { get; } = Guid.NewGuid();

        public bool IsConnected =>
            _Socket?.Connected ?? false;

        public bool Remove =>
            _Remove;

        public int ReceiveTimeout =>
            _ReceiveTimeout;

        public int ReceiveBufferLength
        {
            get
            {
                lock (_ReceiveBuffer)
                {
                    return _ReceiveBuffer.Count;
                }
            }
        }

        public int MessageCount
        {
            get
            {
                lock (_MessageBuffer)
                {
                    return _MessageBuffer.Count;
                }
            }
        }
        #endregion

        private bool AlreadStarted = false;
        public void StartListener()
        {
            // check, socket must not be null
            if ((_Socket != null) & (!AlreadStarted))
            {
                _Listener = new BetterBackgroundWorker();
                _Listener.DoWork += new DoWorkEventHandler(Listener_DoWork);
                _Listener.Run();

                _MessageProcess = new BetterBackgroundWorker();
                _MessageProcess.DoWork += new DoWorkEventHandler(MessageProcess_DoWork);
                _MessageProcess.Run();

                AlreadStarted = true;
            }
        }

        private void Listener_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BetterBackgroundWorker;

            while (!worker.CancellationPending)
            {
                int size = 0;

                try
                {
                    // only if data available
                    if ((size = _Socket.Available) > 0)
                    {

                        byte[] byteBuffer = new byte[size];
                        _Socket.Receive(byteBuffer, size, SocketFlags.None);

                        // update response
                        _LastResponse = DateTime.UtcNow;
                        lock (_ReceiveBuffer)
                        {
                            _ReceiveBuffer.AddRange(byteBuffer);
                            ParseReceiveBuffer();
                        }
                    }

                    // test if timeout occured
                    if (ReceiveTimeout != 0 && DateTime.UtcNow.Subtract(_LastResponse).TotalSeconds > ReceiveTimeout)
                    {
                        e.Cancel = true;
                        break;
                    }

                    // wait 
                    Thread.Sleep(10);
                }
                catch (SocketException)
                {
                    _Remove = true;

                    // exit if an error occured
                    break;
                }
            }

            e.Cancel = true;
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
                        _ReceiveBuffer.RemoveRange(Start, End + 4);

                        // add message 
                        lock (_MessageBuffer)
                        {
                            _MessageBuffer.Add(Message);
                        }

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

        private void MessageProcess_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BetterBackgroundWorker;

            while (!worker.CancellationPending)
            {
                byte[] Message = null;

                lock (_MessageBuffer)
                {
                    if (_MessageBuffer.Count > 0)
                    {
                        Message = _MessageBuffer.First();
                        _MessageBuffer.RemoveAt(0);
                    }
                }

                // test if message assigned
                if (Message != null)
                    // proceed outside of the lock
                    // if message is not a default message type
                    if (!PreParseMessage(Message))
                        // user-defined message processing
                        ParseMessage(Message);

                Thread.Sleep(10);
            }
        }

        public void StopListener()
        {
            // stop listener - no more messages
            if (_Listener.IsBusy)
            {
                _Listener.Cancel();

                while (_Listener.IsBusy)
                    Thread.Sleep(100);

                _Socket?.Close();
            }

            // wait messages proceed
            if (_MessageProcess.IsBusy)
            {
                //int c = 0;
                while (MessageCount > 0) { Thread.Sleep(100); };

                _MessageProcess.Cancel();
                while (_MessageProcess.IsBusy)
                    Thread.Sleep(100);
            }
            _Remove = true;
        }

        public void SendBuffer(byte[] byteButter) =>
            _Socket.Send(byteButter);

        public void SendDataBlock(DataBlock data) =>
            SendBuffer(data.Write());

        private bool PreParseMessage(byte[] Message)
        {
            uint Signature2 = BitConverter.ToUInt32(Message, sizeof(uint));

            var Response = new RESP(Signature1);
            switch (Signature2)
            {
                case (uint)DefaultMessageTypeEnum.EHLO:
                    var EHLO = new EHLO(Signature1);
                    EHLO.Read(Message);

                    var Reply = new RPLY(Signature1);
                    Reply.DataBlock.EhloId = EHLO.DataBlock.Id;
                    Reply.DataBlock.EhloTime = EHLO.DataBlock.Timestamp;

                    Reply.DataBlock.Message = $"{nameof(EHLO)} {EHLO.DataBlock.Id}";
                    SendBuffer(Reply.Write());

                    return true;
                case (uint)DefaultMessageTypeEnum.KEXC:
                    var KEXC = new KEXC(Signature1);

                    Response.DataBlock.Response1 = 250;
                    SendBuffer(Response.Write());

                    return true;
                case (uint)DefaultMessageTypeEnum.KEYV:
                    var KEYV = new KEYV(Signature1);

                    Response.DataBlock.Response1 = 250;
                    SendBuffer(Response.Write());

                    return true;
                case (uint)DefaultMessageTypeEnum.RESP:
                    var RESP = new RESP(Signature1);

                    RESP.Read(Message);
                    switch (RESP.DataBlock.Response1)
                    {
                        case 250:
                            break;

                    }

                    return true;
                case (uint)DefaultMessageTypeEnum.RPLY:
                    var RPLY = new RPLY(Signature1);

                    RPLY.Read(Message);
                    EHLOReply.Invoke(this, new EHLOReplyEventHandler(RPLY.DataBlock.EhloId,
                        RPLY.DataBlock.EhloTime,
                        RPLY.DataBlock.RplyTime,
                        RPLY.DataBlock.Message));

                    return true;
                default:
                    return false;
            }
        }

        public abstract void ParseMessage(byte[] Message);


        public SocketListener(uint Signature1, Socket Socket)
            : base(Signature1) =>
            this._Socket = Socket;

        public SocketListener(uint Signature1, Socket Socket, int ReceiveTimeout)
            : this(Signature1, Socket) => this._ReceiveTimeout = ReceiveTimeout;

        ~SocketListener() =>
            StopListener();
    }
}
