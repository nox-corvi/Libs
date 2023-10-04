using Microsoft.Extensions.Logging;
using Nox.Net.Com.Message;
using Nox.Security;
using Nox.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;

namespace Nox.Net.Com
{
    public abstract class NetServer<T>
        : NetBase, INetServer where T : SocketListener
    {
        private readonly ILogger _Logger;

        private TcpListener _Listener = null;
        private readonly List<SocketListener> _ListOfListener = new();

        private BetterBackgroundWorker _Server;
        private readonly int _ServerWaitTime = 10;

        private BetterBackgroundWorker _Purge;
        private readonly int _PurgeWaitTime = 100;

        #region Properties
        public string IP { get; set; } = "";
        public int Port { get; set; } = -1;

        public string SocketMessage { get; set; }

        public byte[] PublicKey { get; set; } = null!;

        public int ClientConnectionCount =>
            _ListOfListener?.Count ?? 0;

        public int AllReceiveBufferLength
        {
            get
            {
                int r = 0;
                for (int i = 0; i < _ListOfListener.Count; i++)
                    r += _ListOfListener[i]?.SocketBufferLength ?? 0;

                return r;
            }
        }
        public int MessageCount
        {
            get
            {
                int r = 0;
                for (int i = 0; i < _ListOfListener.Count; i++)
                    r += _ListOfListener[i]?.MessageBufferLength ?? 0;

                return r;
            }
        }

        public bool Bound { get; private set; } = false;
        #endregion

        protected abstract void BindEvents(T SocketListener);

        public virtual void Bind(string IP, int Port)
        {
            StopServer();

            this.IP = IP; this.Port = Port;
            _Listener = new TcpListener(new IPEndPoint(IPAddress.Parse(IP), Port));
            _Listener.Start();

            _Server = new BetterBackgroundWorker();
            _Server.DoWork += new DoWorkEventHandler(Server_DoWork);
            _Server.Run();

            _Purge = new BetterBackgroundWorker();
            _Purge.DoWork += new DoWorkEventHandler(Purge_DoWork);
            _Purge.Run();

            Bound = true;
        }

        private void Server_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BetterBackgroundWorker;

            while (!worker.CancellationPending)
            {
                try
                {
                    if (_Listener.Pending())
                    {
                        var ClientSocket = _Listener.AcceptSocket();

                        // create using abstract method
                        var SocketListener = (T)Activator.CreateInstance(typeof(T), Signature1, ClientSocket, _Logger, 0);

                        SocketListener.Terminate += (object sender, EventArgs e) =>
                        {
                            // notify
                            OnTerminate(sender, e);

                            var sl = (sender as Com.SocketListener);

                            // and remove
                            sl.Done();
                            _ListOfListener.Remove(sl);
                        };
                        SocketListener.Message += OnMessage;

                        BindEvents(SocketListener);
                        lock (_ListOfListener)
                        {
                            _ListOfListener.Add(SocketListener);
                        }
                        SocketListener.Initialize();
                        SocketListener.Run();
                    }
                    else
                        Thread.Sleep(_ServerWaitTime);
                }
                catch (SocketException)
                {
                    e.Cancel = true;
                    return;
                }
            }
        }

        private void Purge_DoWork(object sender, DoWorkEventArgs e)
        {
            var worker = sender as BetterBackgroundWorker;

            while (!worker.CancellationPending)
            {
                lock (_ListOfListener)
                {
                    for (int i = _ListOfListener.Count - 1; i >= 0; i--)
                        if (_ListOfListener[i]?.CanPurge ?? false)
                        {
                            _ListOfListener[i].Done();
                            _ListOfListener.RemoveAt(i);
                        }
                }

                Thread.Sleep(_PurgeWaitTime);
            }
        }

        public void StopServer()
        {
            // no further more connections
            if (_Server?.IsBusy ?? false)
            {
                Bound = false;
                _Listener.Stop();

                _Server.Cancel();
                while (_Server.IsBusy)
                    Thread.Sleep(100);
            }
            StopAllListers();


            if (_Purge?.IsBusy ?? false)
            {
                _Purge.Cancel();
                while (_Purge.IsBusy)
                    Thread.Sleep(100);
            }
        }

        private void StopAllListers()
        {
            if (_ListOfListener != null)
            {
                for (int i = 0; i < _ListOfListener.Count; i++)
                    _ListOfListener[i].Done();

                while (_ListOfListener.Count > 0)
                    _ListOfListener.RemoveAt(0);
                //_ListOfListener = null;
            }
        }

        public bool SendBufferTo(int Index, byte[] byteBuffer)
        {
            try
            {
                if (_ListOfListener[Index].IsInitialized)
                {
                    _ListOfListener[Index].SendBuffer(byteBuffer);
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

        public int SendBuffer(byte[] byteBuffer)
        {
            int Result = 0;

            for (int i = 0; i < _ListOfListener.Count; i++)
                if (_ListOfListener[i].IsInitialized)
                    if (SendBufferTo(i, byteBuffer))
                        Result++;

            return Result;
        }

        public bool SendBufferTo(Guid Id, byte[] byteBuffer)
        {
            for (int i = 0; i < _ListOfListener.Count; i++)
                if (_ListOfListener[i].Id == Id)
                    if (_ListOfListener[i].IsInitialized)
                        if (SendBufferTo(i, byteBuffer))
                            return true;

            return false;
        }

        public void SendDataBlockTo(int Index, DataBlock data)
        {
            SendBufferTo(Index, data.Write());
        }

        public int SendDataBlock(DataBlock data)
        {
            return SendBuffer(data.Write());
        }

        public override void Dispose()
        {
            StopServer();

            base.Dispose();
            GC.SuppressFinalize(this);
        }

        public NetServer(uint Signature1, ILogger logger)
            : this(Signature1)
            => this._Logger = logger;

        public NetServer(uint Signature1)
        : base(Signature1) { }
    }

    public class NetGenericServer<T>
         : NetServer<T>, INetGenericMessages where T : GenericSocketListener
    {
        #region Events
        public event EventHandler<PingMessageEventArgs> PingMessage;

        public event EventHandler<EchoMessageEventArgs> EchoMessage;

        /// <summary>
        /// raised if a message respone occurs
        /// </summary>
        public event EventHandler<RespMessageEventArgs> RespMessage;

        /// <summary>
        /// raised if a message will obtained
        /// </summary>
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


        protected override void BindEvents(T SocketListener)
        {
            SocketListener.PingMessage += OnPingMessage;
            SocketListener.EchoMessage += OnEchoMessage;
            SocketListener.RespMessage += OnRespMessage;
            SocketListener.ObtainRplyMessage += OnObtainRplyMessage;
        }


        public NetGenericServer(uint Signature1, ILogger logger)
            : base(Signature1, logger) { }

        public NetGenericServer(uint Signature1)
            : base(Signature1) { }
    }

    public class NetSecureServer<T>
        : NetGenericServer<T>, INetSecureServerMessages where T : SecureSocketListener    
    {
        #region Events
        public event EventHandler<EhloEventArgs> EhloMessage;
        public event EventHandler<KeyxEventArgs> KeyxMessage;
        public event EventHandler<SigxEventArgs> SigxMessage;
        public event EventHandler<ConSEventArgs> ConsMessage;
        public event EventHandler<CRawEventArgs> CRawMessage;
        public event EventHandler<URawEventArgs> URawMessage;

        public event EventHandler<PublicKeyEventArgs> ObtainPublicKey;
        #endregion

        #region OnRaiseEvent Methods
        public void OnEhloMessage(object sender, EhloEventArgs e)
            => EhloMessage?.Invoke(sender, e);

        public void OnSigxMessage(object sender, SigxEventArgs e)
            => SigxMessage?.Invoke(sender, e);

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

        protected override void BindEvents(T SocketListener)
        {
            base.BindEvents(SocketListener);

            SocketListener.EhloMessage += OnEhloMessage;
            SocketListener.SigxMessage += OnSigxMessage;
            SocketListener.KeyxMessage += OnKeyxMessage;
            SocketListener.ConsMessage += OnConsMessage;
            SocketListener.CRawMessage += OnCRawMessage;
            SocketListener.URawMessage += OnURawMessage;

            SocketListener.ObtainPublicKey += OnObtainPublicKey;
        }

        public NetSecureServer(uint Signature1, ILogger logger)
            : base(Signature1, logger) { }
        public NetSecureServer(uint Signature1)
           : base(Signature1) { }
    }
}
