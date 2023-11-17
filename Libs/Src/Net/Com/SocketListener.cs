using Microsoft.Extensions.Logging;
using Nox;
using Nox.Data;
using Nox.Net.Com.Message;
using Nox.Security;
using Nox.Threading;
using System;
using System.Collections;
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
        : NetSocket, IRunner
    {
        private const int RECEIVE_BUFFER_SIZE = 32768;
        private const uint EOM = 0xFEFE;

        protected Socket _Socket = null;

        private int _Timeout = 0;

        protected DataHandler<byte> _SocketHandler;
        protected DataHandler<byte[]> _MessageHandler;

        #region Properties 
        /// <summary>
        /// True if the Object is Disposed ans is ready to remove
        /// </summary>
        public bool CanPurge { get; private set; } = false;

        /// <summary>
        /// Timeout 
        /// </summary>
        public int Timeout
        {
            get => _Timeout;
            set
            {
                // pass through
                _Timeout = value;
                if (_SocketHandler != null)
                    _SocketHandler.Timeout = value;

                if (_MessageHandler != null)
                    _MessageHandler.Timeout = value;
            }
        }

        /// <summary>
        /// Timestamp of last 
        /// </summary>
        public DateTime LastResponse { get; private set; } = DateTime.UtcNow;

        public int SocketBufferLength
            => _SocketHandler?.Count ?? 0;

        public int MessageBufferLength
            => _MessageHandler?.Count ?? 0;
        #endregion

        #region Send Methods
        public void SendBuffer(byte[] byteBuffer)
            => _Socket.Send(byteBuffer);

        public void SendDataBlock(DataBlock data)
            => SendBuffer(data.Write());
        #endregion

        public void ParseReceiveBuffer(ThreadSafeDataList<byte> data)
        {
            int Start = 0, End = -1;
            int Length = data.Count;

            while (Start < Length)
                for (int i = 0; i < data.Count; i++)
                {
                    try
                    {
                        // get 4 bytes
                        var p = new byte[sizeof(uint)];
                        data.CopyTo(i, p, 0, p.Length);

                        if (BitConverter.ToUInt32(p, 0) == Signature1)
                            Start = i;

                        if (BitConverter.ToUInt32(p, 0) == EOM)
                            End = i;

                        if (End > Start)
                        {
                            byte[] Message = new byte[End - Start + 4];

                            data.CopyTo(Start, Message, 0, Message.Length);

                            // remove message block, give up leading data 
                            data.RemoveRange(Start, End + 4);
                            Length = data.Count;

                            // add message 
                            _MessageHandler.Add(Message);

                            break;
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        _Logger?.LogError(ex.Message);

                        return;
                    }
                }

            // remove leading data to keep receivebuffer in range
            while (_SocketHandler.Count > RECEIVE_BUFFER_SIZE)
                _SocketHandler.RemoveRange(0, _SocketHandler.Count - RECEIVE_BUFFER_SIZE);
        }

        public override void Initialize()
        {
            base.Initialize();
            try
            {
                // check, socket must not be null
                if (!IsInitialized)
                {
                    _SocketHandler = new(_Logger) { Timeout = Timeout };
                    _SocketHandler.Loop += (object sender, LoopEventArgs<byte> e) =>
                    {
                        try
                        {
                            int size;
                            // only if data available
                            if ((size = _Socket.Available) > 0)
                            {
                                byte[] byteBuffer = new byte[size];
                                _Socket.Receive(byteBuffer, size, SocketFlags.None);

                                // update response
                                LastResponse = DateTime.UtcNow;

                                e.DataList.AddRange(byteBuffer);
                                ParseReceiveBuffer(e.DataList);
                            }

                            // test if timeout occured
                            if (Timeout != 0 && DateTime.UtcNow.Subtract(LastResponse).TotalSeconds > Timeout)
                                e.Cancel = true;
                        }
                        catch (SocketException ex)
                        {
                            _Logger?.LogError(ex.ToString());
                            e.Cancel = true;
                        }
                    };
                    _SocketHandler.Initialize();

                    _MessageHandler = new(_Logger) { Timeout = Timeout };
                    _MessageHandler.Loop += (object sender, LoopEventArgs<byte[]> e) =>
                    {
                        if (_MessageHandler.Count > 0)
                        {
                            byte[] Message = _MessageHandler.First();
                            _MessageHandler.RemoveAt(0);

                            if (Message != null)
                                // message processing
                                ParseMessage(Message);
                        }
                    };
                    _MessageHandler.Initialize();

                    IsInitialized = true;
                }
            }
            catch (Exception ex)
            {
                _Logger?.LogError(ex.ToString());
                _Socket = null;
            }
        }

        public override void Run()
        {
            base.Run();

            _SocketHandler.Run();
            _MessageHandler.Run();
        }

        public override void Done()
        {
            base.Done();
            try
            {
                // stop listener - no more messages
                _SocketHandler?.Done();
                _Socket?.Close();

                _MessageHandler?.Done();

                CanPurge = true;
            }
            finally
            {
                IsInitialized = false;
            }
        }

        public abstract bool ParseMessage(byte[] Message);

        public SocketListener(uint Signature1, Socket Socket, ILogger<SocketListener> logger, int Timeout)
            : base(Signature1, logger)
        {
            this._Socket = Socket;
            this.Timeout = Timeout;
        }

        public SocketListener(uint Signature1, Socket Socket, ILogger<SocketListener> logger)
            : this(Signature1, Socket, logger, 0)
        { }

        public SocketListener(uint Signature1, Socket Socket)
            : this(Signature1, Socket, null, 0) { }

        public override void Dispose()
        {
            _SocketHandler.Dispose();
            _MessageHandler.Dispose();

            base.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public class GenericSocketListener
        : SocketListener, INetGenericSocket
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

        #region Handle Message Methods
        private bool HandlePingMessage(byte[] Message)
        {
            var ping = new MessagePing(Signature1);
            ping.Read(Message);

            // raise ping event 
            OnPingMessage(this, new PingMessageEventArgs(ping.dataBlock.Id, ping.dataBlock.Timestamp));

            // make a ping response
            var Echo = new MessageEcho(Signature1);

            // reply ping id
            Echo.dataBlock.PingId = ping.dataBlock.Id;

            // rply origin timestamp
            Echo.dataBlock.PingTimestamp = ping.dataBlock.Timestamp;

            //TODO: Time Difference of Client and Server!
            SendBuffer(Echo.Write());

            return true;
        }

        private bool HandleEchoMessage(byte[] Message)
        {
            var echo = new MessageEcho(Signature1);
            echo.Read(Message);

            // raise event, nothing more to do
            OnEchoMessage(this, new EchoMessageEventArgs(echo.dataBlock.PingId, echo.dataBlock.PingTimestamp, echo.dataBlock.EchoTimestamp));

            return true;
        }

        private bool HandleTerminateMessage(byte[] Message)
        {
            var terminate = new MessageTerminate(Signature1);
            terminate.Read(Message);

            OnTerminate(this, new MessageEventArgs("terminate confirmed, close socket!"));

            return true;
        }
        #endregion

        #region Send Methods
        public void SendPingMessage()
            => SendBuffer(new MessagePing(Signature1).Write());

        public void SendEchoMessage(Guid Id, DateTime Timestamp)
        {
            var echo = new MessageEcho(Signature1);

            echo.dataBlock.PingId = Id;
            echo.dataBlock.PingTimestamp = Timestamp;

            SendBuffer(echo.Write());
        }

        public void SendTerminateMessage()
            => SendBuffer(new MessageTerminate(Signature1).Write());
        #endregion

        public override bool ParseMessage(byte[] Message)
        {
            try
            {
                return BitConverter.ToUInt32(Message, sizeof(uint)) switch // Signature2
                {
                    (uint)MessageTypeEnum.PING => HandlePingMessage(Message),
                    (uint)MessageTypeEnum.ECHO => HandleEchoMessage(Message),
                    //case (uint)SecureMessageTypeEnum.RESP:
                    //    return HandleRespMessage(Message);
                    (uint)MessageTypeEnum.TERM => HandleTerminateMessage(Message),
                    _ => false,// unknown message type
                };
            }
            catch (Exception ex)
            {
                _Logger.LogCritical(ex.ToString());
                return false;
            }
        }

        public GenericSocketListener(uint Signature1, Socket socket, ILogger<GenericSocketListener> logger = null!, int Timeout = 0)
            : base(Signature1, socket, logger, Timeout) { }
    }

    public class SecureSocketListener
        : GenericSocketListener, INetSecureSocket
    {
        private Guid _SequenceId = Guid.Empty;

        #region Events
        public event EventHandler<EhloEventArgs> EhloMessage;
        public event EventHandler<RplyEventArgs> RplyMessage;

        public event EventHandler<SigxEventArgs> SigxMessage;
        public event EventHandler<SigvEventArgs> SigvMessage;

        public event EventHandler<KeyxEventArgs> KeyxMessage;
        public event EventHandler<KeyvEventArgs> KeyvMessage;

        public event EventHandler<ConSEventArgs> ConsMessage;

        public event EventHandler<CRawEventArgs> CRawMessage;
        public event EventHandler<URawEventArgs> URawMessage;

        public event EventHandler<PublicKeyEventArgs> ObtainPublicKey;
        #endregion

        #region OnRaiseEvent Methods
        public void OnEhloMessage(object sender, EhloEventArgs e)
            => EhloMessage?.Invoke(sender, e);

        public void OnRplyMessage(object sender, RplyEventArgs e)
            => RplyMessage?.Invoke(sender, e);

        public void OnSigvMessage(object sender, SigvEventArgs e)
            => SigvMessage?.Invoke(sender, e);

        public void OnSigxMessage(object sender, SigxEventArgs e)
            => SigxMessage?.Invoke(sender, e);

        public void OnKeyvMessage(object sender, KeyvEventArgs e)
            => KeyvMessage?.Invoke(sender, e);

        public void OnKeyxMessage(object sender, KeyxEventArgs e)
            => KeyxMessage?.Invoke(sender, e);

        public void OnConsMessage(object sender, ConSEventArgs e)
            => ConsMessage?.Invoke(sender, e);

        public void OnCRawMessage(object sender, CRawEventArgs e)
            => CRawMessage?.Invoke(sender, e);

        public void OnURawMessage(object sender, URawEventArgs e)
            => URawMessage?.Invoke(sender, e);


        public void OnObtainPublicKey(object sender, PublicKeyEventArgs e)
            => ObtainPublicKey?.Invoke(sender, e);
        #endregion

        #region Handle Secure Message Methods
        private bool HandleEhloMessage(byte[] Message)
        {
            var ehlo = new MessageEhlo(Signature1);
            ehlo.Read(Message);

            if (_SequenceId == Guid.Empty)
            {
                // obtain server instance public key 
                var p = new PublicKeyEventArgs();
                OnObtainPublicKey(this, p);

                if (!p.Cancel && p.publicKey != null)
                {
                    var m = new MessageEventArgs(); // server rply message part 
                    OnObtainRplyMessage(this, m);

                    if (m.Message != null)
                    {
                        // ehlo done, fix sequenceid, notify 
                        _SequenceId = ehlo.dataBlock.SequenceId;
                        OnEhloMessage(this, new EhloEventArgs(ehlo.dataBlock.SequenceId, ehlo.dataBlock.Timestamp, ehlo.dataBlock.PublicKey, ehlo.dataBlock.Message));

                        // send response back 
                        SendRplyMessage(ehlo.dataBlock.SequenceId, p.publicKey, m.Message);
                    }
                    else
                    {
                        // error obtain public key
                        OnMessage(this, new MessageEventArgs("cancel obtain random message or message is empty"));
                        Task.Run(() => Done());
                    }
                }
                else
                {
                    OnMessage(this, new MessageEventArgs("cancel obtain public key or public key is empty"));
                    Task.Run(() => Done());
                }

            }
            else
            {
                if (_SequenceId == ehlo.dataBlock.SequenceId)
                    // ignore
                    return true;
                else
                {
                    OnMessage(this, new MessageEventArgs("sequence mismatch"));
                    Task.Run(() => Done());

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

            _SequenceId = rply.dataBlock.SequenceId;
            OnRplyMessage(this, new RplyEventArgs(rply.dataBlock.SequenceId, rply.dataBlock.Timestamp, rply.dataBlock.PublicKey, rply.dataBlock.Message));

            return true;
        }

        private bool HandleSigxMessage(byte[] Message)
        {
            var sigx = new MessageSigx(Signature1);
            sigx.Read(Message);

            if (sigx.dataBlock.SequenceId == _SequenceId)
            {
                // sign exchange event
                var x = new SigxEventArgs(sigx.dataBlock.SequenceId, sigx.dataBlock.EncryptedHash);
                OnSigxMessage(this, x);

                if (x.Valid)
                    OnMessage(this, new MessageEventArgs("sigx signature is valid"));
                else
                    SendTerminateMessage();
            }
            else
            {
                SendTerminateMessage();
                Task.Run(() => Done());
            }
            // if ehlo message proceeded, ignore cancel, send always true
            return true;
        }

        private bool HandleSigvMessage(byte[] Message)
        {
            var sigv = new MessageSigx(Signature1);
            sigv.Read(Message);

            if (sigv.dataBlock.SequenceId == _SequenceId)
            {
                // sign exchange event
                var v = new SigvEventArgs(sigv.dataBlock.SequenceId, sigv.dataBlock.EncryptedHash);
                OnSigvMessage(this, v);

                if (v.Valid)
                    OnMessage(this, new MessageEventArgs("sigv signature is valid"));
                else
                    SendTerminateMessage();
            }
            else
            {
                SendTerminateMessage();
                Task.Run(() => Done());
            }
            // if ehlo message proceeded, ignore cancel, send always true
            return true;
        }

        private bool HandleKeyxMessage(byte[] Message)
        {
            var keyx = new MessageKeyx(Signature1);
            keyx.Read(Message);

            if (keyx.dataBlock.SequenceId == _SequenceId)
            {
                var v = new KeyxEventArgs(keyx.dataBlock.SequenceId, keyx.dataBlock.EncryptedKey, keyx.dataBlock.KeyHash);
                OnKeyxMessage(this, v);
            }
            else
            {
                SendTerminateMessage();
                Task.Run(() => Done());
            }

            return true;
        }

        private bool HandleKeyvMessage(byte[] Message)
        {
            var keyv = new MessageKeyv(Signature1);
            keyv.Read(Message);

            if (keyv.dataBlock.SequenceId == _SequenceId)
            {
                var v = new KeyvEventArgs(keyv.dataBlock.SequenceId, keyv.dataBlock.EncryptedIV, keyv.dataBlock.IVHash);
                OnKeyvMessage(this, v);
            }
            else
            {
                SendTerminateMessage();
                Task.Run(() => Done());
            }

            return true;
        }

        private bool HandleConSMessage(byte[] Message)
        {
            var cons = new MessageConS(Signature1);
            cons.Read(Message);

            if (cons.dataBlock.SequenceId == _SequenceId)
            {
                var v = new ConSEventArgs(cons.dataBlock.SequenceId);
                OnConsMessage(this, v);
            }
            else
            {
                SendTerminateMessage();
                Task.Run(() => Done());
            }

            return true;
        }

        private bool HandleCRawMessage(byte[] Message)
        {
            var craw = new MessageCRaw(Signature1);
            craw.Read(Message);

            if (craw.dataBlock.SequenceId == _SequenceId)
            {
                var c = new CRawEventArgs(craw.dataBlock.SequenceId, craw.dataBlock.EncryptedData, craw.dataBlock.Hash);
                OnCRawMessage(this, c);

                if (c.Valid)
                {
                    var u = new URawEventArgs(c.UnencryptedData);
                    OnURawMessage(this, u);
                }
            }
            else
            {
                SendTerminateMessage();
                Task.Run(() => Done());
            }

            return true;
        }

        private bool HandleRespMessage(byte[] Message)
        {
            var resp = new MessageResp(Signature1);
            resp.Read(Message);

            OnRespMessage(this, new RespMessageEventArgs(_SequenceId, resp.dataBlock.Response1, resp.dataBlock.Response2, resp.dataBlock.Response3));

            return true;
        }
        #endregion

        #region Secure Send Methods
        public Guid SendEhloMessage(byte[] PublicKey, string Message)
        {
            var ehlo = new MessageEhlo(Signature1);

            var SequenceId = Guid.NewGuid();
            ehlo.dataBlock.SequenceId = SequenceId;
            ehlo.dataBlock.Timestamp = DateTime.UtcNow;
            ehlo.dataBlock.PublicKey = PublicKey;
            ehlo.dataBlock.Message = Message;

            SendBuffer(ehlo.Write());

            return SequenceId;
        }

        public void SendRplyMessage(Guid SequenceId, byte[] PublicKey, string Message)
        {
            var rply = new MessageRply(Signature1);

            rply.dataBlock.SequenceId = SequenceId;
            rply.dataBlock.Timestamp = DateTime.UtcNow;
            rply.dataBlock.PublicKey = PublicKey;
            rply.dataBlock.Message = Message;

            SendBuffer(rply.Write());
        }

        public void SendSigxMessage(Guid SequenceId, byte[] EncryptedHash)
        {
            var sigx = new MessageSigx(Signature1);

            sigx.dataBlock.SequenceId = SequenceId;
            sigx.dataBlock.EncryptedHash = EncryptedHash;

            SendBuffer(sigx.Write());
        }

        public void SendSigvMessage(Guid SequenceId, byte[] EncryptedHash)
        {
            var sigv = new MessageSigv(Signature1);

            sigv.dataBlock.SequenceId = SequenceId;
            sigv.dataBlock.EncryptedHash = EncryptedHash;

            SendBuffer(sigv.Write());
        }

        public void SendRespMessage(Guid SequenceId, uint Response1, uint Response2, uint Response3)
        {
            var resp = new MessageResp(Signature1);

            resp.dataBlock.SequenceId = _SequenceId;
            resp.dataBlock.Response1 = Response1;
            resp.dataBlock.Response2 = Response2;
            resp.dataBlock.Response3 = Response3;

            SendBuffer(resp.Write());
        }

        public void SendCRawMessage(Guid SequenceId, byte[] EncryptedData)
        {
            var craw = new MessageCRaw(Signature1);

            craw.dataBlock.SequenceId = _SequenceId;
            craw.dataBlock.EncryptedData = EncryptedData;

            SendBuffer(craw.Write());
        }
        #endregion

        public override bool ParseMessage(byte[] Message)
        {
            if (!base.ParseMessage(Message))
            {
                try
                {
                    return BitConverter.ToUInt32(Message, sizeof(uint)) switch // Signature2
                    {
                        (uint)SecureMessageTypeEnum.EHLO => HandleEhloMessage(Message),
                        (uint)SecureMessageTypeEnum.RPLY => HandleRplyMessage(Message),
                        (uint)SecureMessageTypeEnum.SIGX => HandleSigxMessage(Message),
                        (uint)SecureMessageTypeEnum.SIGV => HandleSigvMessage(Message),
                        (uint)SecureMessageTypeEnum.KEYX => HandleKeyxMessage(Message),
                        (uint)SecureMessageTypeEnum.KEYV => HandleKeyvMessage(Message),
                        (uint)SecureMessageTypeEnum.CONS => HandleConSMessage(Message),
                        (uint)SecureMessageTypeEnum.CRAW => HandleCRawMessage(Message),
                        //case (uint)SecureMessageTypeEnum.RESP:
                        //    return HandleRespMessage(Message);
                        //case (uint)MessageTypeEnum.DONE:
                        //    return HandleTerminateMessage(Message);
                        _ => false,// unknown message type
                    };
                }
                catch (Exception ex)
                {
                    _Logger.LogCritical(ex.ToString());
                    return false;
                }
            }

            return true;
        }

        public SecureSocketListener(uint Signature1, Socket socket, ILogger<SecureSocketListener> logger, int Timeout)
            : base(Signature1, socket, logger, Timeout) { }

        public SecureSocketListener(uint Signature1, Socket socket, ILogger<SecureSocketListener> logger)
            : base(Signature1, socket, logger, 30) { }

        public SecureSocketListener(uint Signature1, Socket socket)
            : base(Signature1, socket, null, 30) { }
    }
}
