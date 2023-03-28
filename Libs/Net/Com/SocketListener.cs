using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json.Linq;
using Nox;
using Nox.CI.CID;
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
    public interface IRunner
    {
        void Initialize();
        void Run();

        void Done();
    }

    public class DataListLoopEventArgs<T>
        : CancelEventArgs
    {
        public DataList<T> DataList { get; set; }

        public DataListLoopEventArgs(DataList<T> dataList)
        {
            DataList = dataList;
        }
    }

    public class DataList<T>
        : ICloneable, IList<T>
    //where T : struct
    {
        private System.Collections.Generic.List<T> _List = new();
        private Log4 _Log = null!;

        #region IList
        public T this[int index]
        {
            get
            {
                _Log?.LogMethod(Log4.Log4LevelEnum.Trace, index);

                lock (_List)
                {
                    return _List[index];
                }
            }
            set
            {
                _Log?.LogMethod(Log4.Log4LevelEnum.Trace, index);

                lock (_List)
                {
                    _List[index] = value;
                }

            }
        }

        public int Count
        {
            get
            {
                _Log?.LogMethod(Log4.Log4LevelEnum.Trace);

                lock (_List)
                {
                    return _List.Count;
                }
            }
        }

        public bool IsReadOnly
        {
            get
            {
                _Log?.LogMethod(Log4.Log4LevelEnum.Trace);

                return false;
            }
        }

        public void Add(T value)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, value);

            lock (_List)
            {
                _List.Add(value);
            }
        }

        public void AddRange(IEnumerable<T> collection)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, collection);

            lock (_List)
            {
                _List.AddRange(_List);
            }
        }

        public void Insert(int index, T value)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, index, value);

            lock (_List)
            {
                _List.Insert(index, value);
            }
        }
        public bool Remove(T item)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, item);

            lock (_List)
            {
                return _List.Remove(item);
            }
        }

        public void RemoveAt(int index)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, index);

            lock (_List)
            {
                _List.RemoveAt(index);
            }
        }

        public void RemoveRange(int index, int count)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, index, count);

            lock (_List)
            {
                _List.RemoveRange(index, count);
            }
        }

        public void Clear()
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace);

            lock (_List)
            {
                _List.Clear();
            }
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, array, arrayIndex);

            lock (_List)
            {
                _List.CopyTo(array, arrayIndex);
            }
        }

        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            // Delegate rest of error checking to Array.Copy.
            for (int i = 0; i < count; i++)
                array[arrayIndex + i] = _List[index + i];
        }

        public bool Contains(T value)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, value);

            lock (_List)
            {
                return _List.Contains(value);
            }
        }
        public IEnumerator GetEnumerator()
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace);

            lock (_List)
            {
                return _List.GetEnumerator();
            }
        }
        public int IndexOf(T value)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, value);

            lock (_List)
            {
                return _List.IndexOf(value);
            }
        }
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace);

            lock (_List)
            {
                return _List.GetEnumerator();
            }
        }
        #endregion

        #region ICloneable
        public object Clone()
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace);

            var Result = new DataList<T>();
            lock (_List)
            {
                for (int i = 0; i < _List.Count(); i++)
                    Result.Add(_List[i]);
            }

            return Result;
        }
        #endregion

        public DataList(Log4 Log)
            : base()
            => (_Log = Log)?.LogMethod(Log4.Log4LevelEnum.Trace);

        public DataList()
            : base() { }
    }

    public class DataHandler<T>
        : IRunner, IList<T>, IDisposable
    {
        public event EventHandler<DataListLoopEventArgs<T>> Loop;

        private Log4 _Log = null!;
        private DataList<T> _data = null!;

        private BetterBackgroundWorker _worker = null;

        #region Properties
        public bool IsInitialized { get; private set; }
        #endregion

        #region IList
        public T this[int index]
        {
            get => _data[index];
            set => _data[index] = value;
        }

        public int Count
            => _data.Count;

        public bool IsReadOnly
            => _data.IsReadOnly;

        public void Add(T value)
            => _data.Add(value);

        public void AddRange(IEnumerable<T> collection)
            => _data.AddRange(collection);

        public void Insert(int index, T value)
            => _data.Insert(index, value);

        public bool Remove(T item)
            => _data.Remove(item);

        public void RemoveAt(int index)
            => _data.RemoveAt(index);

        public void RemoveRange(int index, int count)
            => _data.RemoveRange(index, count);

        public void Clear()
            => _data.Clear();

        public void CopyTo(T[] array, int arrayIndex)
            => _data.CopyTo(array, arrayIndex);

        public void CopyTo(int index, T[] array, int arrayIndex, int count)
            => _data.CopyTo(index, array, arrayIndex, count);

        public bool Contains(T value)
            => _data.Contains(value);

        public IEnumerator GetEnumerator()
            => _data.GetEnumerator();

        public int IndexOf(T value)
            => _data.IndexOf(value);

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => (IEnumerator<T>)_data.GetEnumerator();
        #endregion

        public void Initialize()
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace);

            try
            {
                // create worker 
                _worker = new BetterBackgroundWorker();
                _worker.DoWork += (object sender, DoWorkEventArgs e) =>
                {
                    _Log?.LogMethod(Log4.Log4LevelEnum.Trace, sender, e);

                    var e2 = new DataListLoopEventArgs<T>(_data);
                    while (!(sender as BetterBackgroundWorker).CancellationPending)
                    {
                        try
                        {
                            Loop?.Invoke(sender, e2);

                            // wait 
                            Thread.Sleep(10);
                        }
                        catch (SocketException ex)
                        {
                            _Log?.LogException(ex);

                            // exit if an error occured
                            break;
                        }
                    }

                    e.Cancel = true;

                };

                IsInitialized = true;
            }
            catch (Exception e)
            {
                _Log?.LogException(e);
            }
        }

        public void Run()
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace);

            if (IsInitialized)
                _worker.Run();
        }

        public void Done()
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace);

            if (IsInitialized)
            {
                // stop worker 
                if (_worker.IsBusy)
                {
                    _Log?.LogMessage("cancel worker", Log4.Log4LevelEnum.Trace);
                    _worker.Cancel();

                    // wait for termination ...
                    _Log?.LogMessage("await worker is done", Log4.Log4LevelEnum.Trace);
                    while (_worker.IsBusy)
                        Thread.Sleep(100);
                }

                IsInitialized = false;
            }
        }

        public DataHandler(Log4 Log)
            => (_Log = Log)?.LogMethod(Log4.Log4LevelEnum.Trace);

        public DataHandler(Log4 Log, bool InitWait = false)
            : this(Log)
        {
            Log?.LogMethod(Log4.Log4LevelEnum.Trace);

            if (!(InitWait | IsInitialized))
                Initialize();
        }

        public void Dispose()
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace);
            Done();
        }
    }

    public abstract class SocketListener
        : NetSocketBase, IRunner
    {
        private const int RECEIVE_BUFFER_SIZE = 32768;
        private const uint EOM = 0xFEFE;

        protected Log4 _Log = null!;

        protected Socket _Socket = null;

        protected DataHandler<byte> _SocketHandler;
        protected DataHandler<byte[]> _MessageHandler;

        #region Properties 
        public int Timeout { get; } = 0;

        public DateTime LastResponse { get; private set; } = DateTime.UtcNow;

        public int SocketBufferLength
            => _SocketHandler?.Count ?? 0;

        public int MessageBufferLength
            => _MessageHandler?.Count ?? 0;
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
        #endregion

        public void ParseReceiveBuffer(DataList<byte> data)
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace);

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
                            //Length = _ReceiveBuffer.Count;

                            // add message 
                            _MessageHandler.Add(Message);

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
            while (_SocketHandler.Count > RECEIVE_BUFFER_SIZE)
                _SocketHandler.RemoveRange(0, _SocketHandler.Count - RECEIVE_BUFFER_SIZE);
        }

        public void Initialize()
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace);

            try
            {
                // check, socket must not be null
                if (_Socket != null)
                {
                    _SocketHandler = new DataHandler<byte>(Log);
                    _SocketHandler.Loop += (object sender, DataListLoopEventArgs<byte> e) =>
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
                            Log.LogException(ex);
                            e.Cancel = true;
                        }
                    };
                    _SocketHandler.Run();

                    _MessageHandler.Loop += (object sender, DataListLoopEventArgs<byte[]> e) =>
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
                }
            }
            catch (Exception e)
            {
                Log.LogException(e);
                _Socket = null;
            }
        }

        public void Run()
        {
            _SocketHandler.Run();
            _MessageHandler.Run();
        }

        public void Done()
        {
            Log.LogMethod(Log4.Log4LevelEnum.Trace);

            // stop listener - no more messages
            _SocketHandler.Done();
            _Socket?.Close();

            _MessageHandler.Done();
        }

        public abstract bool ParseMessage(byte[] Message);

        public SocketListener(uint Signature1, Socket Socket, Log4 Log = null!, int Timeout = 0)
            : base(Signature1)
        {
            this._Socket = Socket;
            this._Log = Log ?? Log4.Create();
            this.Timeout = Timeout;
        }

        public override void Dispose()
        {
            base.Dispose();

            _SocketHandler.Dispose();
            _MessageHandler.Dispose();
        }
    }

    public class GenericSocketListener
        : SocketListener
    {
        #region Handle Message Methods
        private bool HandlePingMessage(byte[] Message)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, Message);

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
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, Message);

            var echo = new MessageEcho(Signature1);
            echo.Read(Message);

            // nothing more to do
            OnEchoMessage(this, new EchoEventArgs(echo.DataBlock.PingId, echo.DataBlock.PingTime, echo.DataBlock.Timestamp));

            return true;
        }

        private bool HandleTerminate(byte[] Message)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, Message);

            var terminate = new MessageTerminate(Signature1);
            terminate.Read(Message);

            var e = new MessageCancelEventArgs("terminate message received, close socket?");
            OnTerminate(this, e);

            if (!e.Cancel)
            {
                OnCloseSocket(this, new MessageEventArgs("terminate confirmed, close socket!"));
            }
            return true;
        }
        #endregion

        #region Send Methods
        public void SendPingMessage()
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace);

            SendBuffer(new MessagePing(Signature1).Write());
        }

        public void SendEchoMessage(Guid Id, DateTime Timestamp)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, Id, Timestamp);

            var echo = new MessageEcho(Signature1);

            echo.DataBlock.PingId = Id;
            echo.DataBlock.PingTime = Timestamp;

            SendBuffer(echo.Write());
        }

        public void SendTerminateMessage()
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace);

            var terminate = new MessageTerminate(Signature1);

            SendBuffer(terminate.Write());
        }
        #endregion

        public override bool ParseMessage(byte[] Message)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, Message);

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

        public GenericSocketListener(uint Signature1, Socket socket, Log4 Log = null!, int Timeout = 0)
            : base(Signature1, socket, Log, Timeout) { }
    }

    public abstract class SecureSocketListener
        : GenericSocketListener
    {
        private Guid _SequenceId = Guid.Empty;

        
        #region Handle Secure Message Methods
        private bool HandleEhloMessage(byte[] Message)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, Message);

            var ehlo = new MessageEhlo(Signature1);
            ehlo.Read(Message);

            if (_SequenceId == Guid.Empty)
            {
                // obtain public key from counterpart
                var p = new PublicKeyEventArgs();
                OnObtainPublicKey(this, p);

                if (!p.Cancel && p.publicKey != null)
                {
                    var m = new ObtainCancelMessageEventArgs();
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
                Task.Run(() => Done());
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
                Task.Run(() => Done());
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

        

        #region Secure Send Methods
        public Guid SendEhloMessage(byte[] PublicKey, string Message)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, PublicKey, Message);
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
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, SequenceId, PublicKey, Message);

            var rply = new MessageRply(Signature1);

            rply.DataBlock.SequenceId = SequenceId;
            rply.DataBlock.Timestamp = DateTime.UtcNow;
            rply.DataBlock.PublicKey = PublicKey;
            rply.DataBlock.Message = Message;

            SendBuffer(rply.Write());
        }

        public void SendSigxMessage(Guid SequenceId, byte[] EncryptedHash)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, SequenceId, EncryptedHash);

            var sigx = new MessageSigx(Signature1);

            sigx.DataBlock.SequenceId = SequenceId;
            sigx.DataBlock.EncryptedHash = EncryptedHash;

            SendBuffer(sigx.Write());
        }

        public void SendSigvMessage(Guid SequenceId, byte[] EncryptedHash)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, SequenceId, EncryptedHash);

            var sigv = new MessageSigv(Signature1);

            sigv.DataBlock.SequenceId = SequenceId;
            sigv.DataBlock.EncryptedHash = EncryptedHash;

            SendBuffer(sigv.Write());
        }

        public void SendRespMessage(Guid SequenceId, uint Response1, uint Response2, uint Response3)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, SequenceId, Response1, Response2, Response3);

            var resp = new MessageResp(Signature1);

            resp.DataBlock.SequenceId = _SequenceId;
            resp.DataBlock.Response1 = Response1;
            resp.DataBlock.Response2 = Response2;
            resp.DataBlock.Response3 = Response3;

            SendBuffer(resp.Write());
        }

        public void SendKeyxMessage(Guid SequenceId)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, SequenceId);

            var keyx = new MessageKeyx(Signature1);

            keyx.DataBlock.SequenceId = _SequenceId;

            SendBuffer(keyx.Write());
        }
        public void SendKeyvMessage(Guid SequenceId)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, SequenceId);

            var keyv = new MessageKeyv(Signature1);

            keyv.DataBlock.SequenceId = _SequenceId;

            SendBuffer(keyv.Write());
        }
        #endregion


        public override bool ParseMessage(byte[] Message)
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace, Message);

            if (!base.ParseMessage(Message))
            {
                try
                {
                    switch (BitConverter.ToUInt32(Message, sizeof(uint))) // Signature2
                    {
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
                            return HandleTerminateMessage(Message);
                        default:
                            // unknown message type
                            return false;
                    }
                }
                catch (Exception e)
                {
                    _Log?.LogException(e);
                    return false;
                }
            }

            return true;
        }

        
        public GenericSocketListener(uint Signature1, Socket socket, Log4 Log = null!, int Timeout = 0)
            : base(Signature1, socket, Log, Timeout) { }
    }
}
