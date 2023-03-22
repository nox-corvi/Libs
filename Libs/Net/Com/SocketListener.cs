using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Nox;
using Nox.Net.Com.Message;
using Nox.Security;
using Nox.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace Nox.Net.Com
{
    public abstract class SocketListener 
        : NetBase
    {
        private const int RECEIVE_BUFFER_SIZE = 32768;
        private const uint EOM = 0xFEFE;

        private Log4 Log = Log4.Create();

        private BetterBackgroundWorker _Listener = null;
        private BetterBackgroundWorker _MessageProcess = null;

        private Socket _Socket = null;

        private List<byte> _ReceiveBuffer = new List<byte>();
        private List<byte[]> _MessageBuffer = new List<byte[]>();

        private Guid _SequenceId = Guid.Empty;

        #region Properties 
        public Guid Id { get; } = Guid.NewGuid();

        public bool IsConnected { get => _Socket?.Connected ?? false; }

        public bool Remove { get; private set; } = false;

        public int ReceiveTimeout { get; } = 0;

        public DateTime LastResponse { get; private set; } = DateTime.UtcNow;

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
            Log.LogMethod(Log4.Log4LevelEnum.Trace);

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
            Log.LogMethod(Log4.Log4LevelEnum.Trace, sender, e);

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
                        LastResponse = DateTime.UtcNow;
                        lock (_ReceiveBuffer)
                        {
                            _ReceiveBuffer.AddRange(byteBuffer);
                            ParseReceiveBuffer();
                        }
                    }

                    // test if timeout occured
                    if (ReceiveTimeout != 0 && DateTime.UtcNow.Subtract(LastResponse).TotalSeconds > ReceiveTimeout)
                    {
                        e.Cancel = true;
                        break;
                    }

                    // wait 
                    Thread.Sleep(10);
                }
                catch (SocketException ex)
                {
                    Log.LogException(ex);
                    Remove = true;

                    // exit if an error occured
                    break;
                }
            }

            e.Cancel = true;
        }

        public void ParseReceiveBuffer()
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace);

            int Start = 0, End = -1;
            int Length = _ReceiveBuffer.Count;

            while (Start < Length)
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
                            Length = _ReceiveBuffer.Count;

                            // add message 
                            lock (_MessageBuffer)
                            {
                                _MessageBuffer.Add(Message);
                            }

                            break;
                        }
                    }
                    catch (System.ArgumentException e)
                    {
                        Log.LogException(e);
                        return;
                    }
                }

            // remove leading data to keep receivebuffer in range
            while (_ReceiveBuffer.Count > RECEIVE_BUFFER_SIZE)
                _ReceiveBuffer.RemoveRange(0, _ReceiveBuffer.Count - RECEIVE_BUFFER_SIZE);
        }

        private void MessageProcess_DoWork(object sender, DoWorkEventArgs e)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, sender, e);

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
                    // user-defined message processing
                    if (!ParseOwnMessage(Message))
                        ParseMessage(Message);

                Thread.Sleep(10);
            }
        }

        public void StopListener()
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace);

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
            Remove = true;
        }

        #region Handle Message Methods
        private bool HandlePingMessage(byte[] Message)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, Message);

            var ping = new MessagePing(Signature1);
            ping.Read(Message);

            OnPingMessage(this, new PingEventArgs(ping.DataBlock.Id, ping.DataBlock.Timestamp));

            // make a ping response
            var PingResponse = new MessageEcho(Signature1);
            PingResponse.DataBlock.PingId = ping.DataBlock.Id;
            PingResponse.DataBlock.PingTime = ping.DataBlock.Timestamp;
            SendBuffer(PingResponse.Write());

            return true;
        }

        private bool HandleEchoMessage(byte[] Message)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, Message);

            var echo = new MessageEcho(Signature1);
            echo.Read(Message);

            // nothing more to do
            OnEchoMessage(this, new EchoEventArgs(echo.DataBlock.PingId, echo.DataBlock.PingTime, echo.DataBlock.Timestamp));

            return true;
        }

        private bool HandleDoneMessage(byte[] Message)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, Message);

            var done = new MessageDone(Signature1);
            done.Read(Message);

            OnCloseSocket(this, new CloseSocketEventArgs("done package received, close socket"));

            return true;
        }
        #endregion

        #region Handle Secure Message Methods
        private bool HandleEhloMessage(byte[] Message)
        {
            var ehlo = new MessageEhlo(Signature1);
            ehlo.Read(Message);

            if (_SequenceId == Guid.Empty)
            {
                // obtain public key from counterpart
                var p = new ObtainPublicKeyEventArgs();
                OnObtainPublicKey(this, p);

                if (!p.Cancel && p.publicKey != null)
                {
                    var m = new ObtainMessageEventArgs();
                    OnObtainMessage(this, m);

                    if (!m.Cancel && m.Message != null)
                    {
                        // ehlo done, fix sequenceid, notify 
                        _SequenceId = ehlo.DataBlock.SequenceId;
                        OnEhloMessage(this, new EhloEventArgs(ehlo.DataBlock.SequenceId, ehlo.DataBlock.Timestamp, ehlo.DataBlock.PublicKey, ehlo.DataBlock.Message));

                        // send response back 
                        SendRplyMessage(ehlo.DataBlock.SequenceId, p.publicKey, m.Message);
                    }
                    else
                        // error obtain public key
                        OnMessage(this, new MessageEventArgs("cancel obtain random message or message is empty"));
                }
                else
                    OnMessage(this, new MessageEventArgs("cancel obtain public key or public key is empty"));
                
            }
            else
            {

                if (_SequenceId == ehlo.DataBlock.SequenceId)
                    // ignore
                    return true;
                else
                {
                    OnMessage(this, new MessageEventArgs("sequence mismatch"));
                    Task.Run(() => StopListener());

                    // no done package, sequenceid is not set at counterpart
                    return true;
                }
            }
            // if ehlo message proceeded, ignore cancel, send always true
            return true;
        }

        private bool HandleRplyMessage(byte[] Message)
        {
            var rply = new MessageRply(Signature1);
            rply.Read(Message);

            OnRplyMessage(this, new RplyEventArgs(rply.DataBlock.SequenceId, rply.DataBlock.Timestamp, rply.DataBlock.PublicKey, rply.DataBlock.Message));
           
            return true;
        }

        private bool HandleSigxMessage(byte[] Message)
        {
            var sigx = new MessageSigx(Signature1);
            sigx.Read(Message);

            if (sigx.DataBlock.SequenceId == _SequenceId)
            {
                // sign exchange event
                var x = new SigxEventArgs(sigx.DataBlock.SequenceId, sigx.DataBlock.EncryptedHash);
                OnSigxMessage(this, x);

                if (x.Valid)
                {
                    OnMessage(this, new MessageEventArgs("sigx signature is valid"));
                }
            }
            else
            {
                Task.Run(() => StopListener());
                SendDoneMessage();
            }
            // if ehlo message proceeded, ignore cancel, send always true
            return true;
        }

        private bool HandleSigvMessage(byte[] Message)
        {
            var sigv = new MessageSigx(Signature1);
            sigv.Read(Message);

            if (sigv.DataBlock.SequenceId == _SequenceId)
            {
                // sign exchange event
                var v = new SigvEventArgs(sigv.DataBlock.SequenceId, sigv.DataBlock.EncryptedHash);
                OnSigvMessage(this, v);

                if (v.Valid)
                {
                    OnMessage(this, new MessageEventArgs("sigv signature is valid"));
                }
            }
            else
            {
                Task.Run(() => StopListener());
                SendDoneMessage();
            }
            // if ehlo message proceeded, ignore cancel, send always true
            return true;
        }

        private bool HandleKeyxMessage(byte[] Message)
        {
            var keyx = new MessageKeyx(Signature1);
            keyx.Read(Message);

            OnKeyxMessage(this, new KeyxEventArgs());

            return true;
        }

        private bool HandleKeyvMessage(byte[] Message)
        {
            var keyv = new MessageKeyv(Signature1);
            keyv.Read(Message);

            OnKeyvMessage(this, new KeyvEventArgs());

            return true;
        }

        private bool HandleRespMessage(byte[] Message)
        {
            var RESP = new MessageResp(Signature1);
            RESP.Read(Message);

            OnRespMessage(this, new RespEventArgs(_SequenceId, RESP.DataBlock.Response1, RESP.DataBlock.Response2, RESP.DataBlock.Response3));

            return true;
        }
        #endregion

        #region Send Methods
        public void SendBuffer(byte[] byteBuffer)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, byteBuffer);
            _Socket.Send(byteBuffer);
        }

        public void SendDataBlock(DataBlock data)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, data);

            SendBuffer(data.Write());
        }

        public void SendPingMessage()
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace);

            SendBuffer(new MessagePing(Signature1).Write());
        }

        public void SendEchoMessage(Guid Id, DateTime Timestamp)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, Id, Timestamp);

            var echo = new MessageEcho(Signature1);

            echo.DataBlock.PingId = Id;
            echo.DataBlock.PingTime = Timestamp;

            SendBuffer(echo.Write());
        }

        public void SendDoneMessage()
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace);

            var done = new MessageDone(Signature1);

            //done.DataBlock.SequenceId = SequenceId;

            SendBuffer(done.Write());
        }
        #endregion

        #region Secure Send Methods
        public Guid SendEhloMessage(byte[] PublicKey, string Message)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, PublicKey, Message);
            var ehlo = new MessageEhlo(Signature1);

            var SequenceId = Guid.NewGuid();
            ehlo.DataBlock.SequenceId = SequenceId;
            ehlo.DataBlock.Timestamp = DateTime.UtcNow;
            ehlo.DataBlock.PublicKey = PublicKey;
            ehlo.DataBlock.Message = Message;

            SendBuffer(ehlo.Write());

            return SequenceId;
        }

        public void SendRplyMessage(Guid SequenceId, byte[] PublicKey, string Message)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, SequenceId, PublicKey, Message);

            var rply = new MessageRply(Signature1);

            rply.DataBlock.SequenceId = SequenceId;
            rply.DataBlock.Timestamp = DateTime.UtcNow;
            rply.DataBlock.PublicKey = PublicKey;
            rply.DataBlock.Message = Message;

            SendBuffer(rply.Write());
        }

        public void SendSigxMessage(Guid SequenceId, byte[] EncryptedHash)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, SequenceId, EncryptedHash);

            var sigx = new MessageSigx(Signature1);

            sigx.DataBlock.SequenceId = SequenceId;
            sigx.DataBlock.EncryptedHash = EncryptedHash;

            SendBuffer(sigx.Write());
        }

        public void SendSigvMessage(Guid SequenceId, byte[] EncryptedHash)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, SequenceId, EncryptedHash);

            var sigv = new MessageSigv(Signature1);

            sigv.DataBlock.SequenceId = SequenceId;
            sigv.DataBlock.EncryptedHash = EncryptedHash;

            SendBuffer(sigv.Write());
        }

        public void SendRespMessage(Guid SequenceId, uint Response1, uint Response2, uint Response3)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, SequenceId, Response1, Response2, Response3);

            var resp = new MessageResp(Signature1);

            resp.DataBlock.SequenceId = _SequenceId;
            resp.DataBlock.Response1 = Response1;
            resp.DataBlock.Response2 = Response2;
            resp.DataBlock.Response3 = Response3;

            SendBuffer(resp.Write());
        }

        public void SendKeyxMessage(Guid SequenceId)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, SequenceId);

            var keyx = new MessageKeyx(Signature1);

            keyx.DataBlock.SequenceId = _SequenceId;

            SendBuffer(keyx.Write());
        }
        public void SendKeyvMessage(Guid SequenceId)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, SequenceId);

            var keyv = new MessageKeyv(Signature1);

            keyv.DataBlock.SequenceId = _SequenceId;

            SendBuffer(keyv.Write());
        }
        #endregion

        public abstract void ParseMessage(byte[] Message);

        private bool ParseOwnMessage(byte[] Message)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace, Message);

            try
            {
                switch (BitConverter.ToUInt32(Message, sizeof(uint))) // Signature2
                {
                    case (uint)MessageTypeEnum.PING:
                        return HandlePingMessage(Message);
                    case (uint)MessageTypeEnum.ECHO:
                        return HandleEchoMessage(Message);

                    case (uint)SecureMessageTypeEnum.EHLO:
                        return HandleEhloMessage(Message);
                    case (uint)SecureMessageTypeEnum.RPLY:
                        return HandleRplyMessage(Message);

                    case (uint)SecureMessageTypeEnum.SIGX:
                        return HandleSigxMessage(Message);
                    case (uint)SecureMessageTypeEnum.SIGV:
                        return HandleSigvMessage(Message);

                    case (uint)SecureMessageTypeEnum.KEYX:
                        return HandleKeyxMessage(Message);
                    case (uint)SecureMessageTypeEnum.KEYV:
                        return HandleKeyvMessage(Message);

                    case (uint)SecureMessageTypeEnum.RESP:
                        return HandleRespMessage(Message);

                    case (uint)MessageTypeEnum.DONE:
                        return HandleDoneMessage(Message);
                    default:
                        // unknown message type
                        return false;
                }
            }
            catch (Exception e)
            {
                Log.LogException(e);
                return false;
            }
        }

        public SocketListener(uint Signature1, Socket Socket)
                    : base(Signature1) =>
                    this._Socket = Socket;

        public SocketListener(uint Signature1, Socket Socket, int ReceiveTimeout)
            : this(Signature1, Socket) => this.ReceiveTimeout = ReceiveTimeout;

        ~SocketListener() =>
            StopListener();
    }
}
