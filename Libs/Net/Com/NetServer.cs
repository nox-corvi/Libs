using Nox.Net.Com.Message.Defaults;
using Nox.Security;
using Nox.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Nox.Net.Com
{
    public class NetServer<T>
        : NetBase where T : SocketListener
    {
        public event EventHandler<PingEventArgs> OnPingMessage;
        public event EventHandler<EchoEventArgs> OnEchoMessage;
        public event EventHandler<EhloEventArgs> OnEhloMessage;
        public event EventHandler<RplyEventArgs> OnRplyMessage;
        public event EventHandler<RespEventArgs> OnRespMessage;

        private string _ServerIP = "";
        private int _ServerPort = -1;

        private TcpListener _Listener = null;
        private List<SocketListener> _ListOfListener;

        private BetterBackgroundWorker _Server;
        private int _ServerWaitTime = 10;

        private BetterBackgroundWorker _Purge;
        private int _PurgeWaitTime = 100;

        #region Properties
        public string ServerIP => _ServerIP;
        public int ServerPort => _ServerPort;

        public Guid Id { get; } = Guid.NewGuid();

        public string SocketMessage { get; set; }

        public tinyKey publicKey { get; set; } = null!;

        public int ClientConnectionCount =>
            _ListOfListener?.Count() ?? 0;

        public int ReceiveBufferLength
        {
            get
            {
                int r = 0;
                for (int i = 0; i < _ListOfListener.Count; i++)
                    r += _ListOfListener[i]?.ReceiveBufferLength ?? 0;

                return r;
            }
        }
        public int MessageCount
        {
            get
            {
                int r = 0;
                for (int i = 0; i < _ListOfListener.Count; i++)
                    r += _ListOfListener[i]?.MessageCount ?? 0;

                return r;
            }
        }
        #endregion

        public void Bind(string IP, int Port)
        {
            StopServer();

            _ServerIP = IP; _ServerPort = Port;
            _Listener = new TcpListener(new IPEndPoint(IPAddress.Parse(IP), Port));

            _ListOfListener = new List<SocketListener>();
            _Listener.Start();

            _Server = new BetterBackgroundWorker();
            _Server.DoWork += new DoWorkEventHandler(Server_DoWork);
            _Server.Run();

            _Purge = new BetterBackgroundWorker();
            _Purge.DoWork += new DoWorkEventHandler(Purge_DoWork);
            _Purge.Run();
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
                        var SocketListener = (T)Activator.CreateInstance(typeof(T), Signature1, ClientSocket);
                        SocketListener.SocketMessage = SocketMessage;
                        SocketListener.publicKey = publicKey;

                        SocketListener.OnPingMessage += (object sender, PingEventArgs e) =>
                            OnPingMessage?.Invoke(sender, e);
                        SocketListener.OnEchoMessage += (object sender, EchoEventArgs e) =>
                            OnEchoMessage?.Invoke(sender, e);
                        SocketListener.OnEhloMessage += (object sender, EhloEventArgs e) =>
                            OnEhloMessage?.Invoke(sender, e);
                        SocketListener.OnRplyMessage += (object sender, RplyEventArgs e) =>
                            OnRplyMessage?.Invoke(sender, e);
                        SocketListener.OnRespMessage += (object sender, RespEventArgs e) =>
                            OnRespMessage?.Invoke(sender, e);

                        lock (_ListOfListener)
                        {
                            _ListOfListener.Add(SocketListener);
                        }

                        SocketListener.StartListener();
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
                        if (_ListOfListener[i]?.Remove ?? false)
                        {
                            _ListOfListener[i].StopListener();
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
                    _ListOfListener[i].StopListener();

                while (_ListOfListener.Count > 0)
                    _ListOfListener.RemoveAt(0);
                _ListOfListener = null;
            }
        }

        public bool SendBufferTo(int Index, byte[] byteBuffer)
        {
            try
            {
                if (_ListOfListener[Index].IsConnected)
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
                if (_ListOfListener[i].IsConnected)
                    if (SendBufferTo(i, byteBuffer))
                        Result++;

            return Result;
        }

        public void SendDataBlockTo(int Index, DataBlock data) =>
            SendBufferTo(Index, data.Write());

        public int SendDataBlock(DataBlock data) =>
            SendBuffer(data.Write());

        public override void Dispose() =>
            StopServer();

        public NetServer(uint Signature1)
            : base(Signature1) { }

        ~NetServer() =>
            StopServer();
    }
}
