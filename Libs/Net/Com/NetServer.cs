using Newtonsoft.Json.Linq;
using Nox.CI;
using Nox.CI.CID;
using Nox.Component;
using Nox.IO;
using Nox.Security;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nox.Net.Com
{
    public abstract class NetServer<T> 
        : NetBase where T : SocketListener
    {
        public abstract string IP { get; }
        public abstract int Port { get; }

        private TcpListener _Listener = null;
        private Dictionary<Guid, SocketListener> _ListOfListener;

        private Thread _ServerThread;
        private Thread _PurgeThread;

        private bool _StopServer = false;
        private bool _StopPurge = false;

        public Guid Id { get; } = Guid.NewGuid();

        public int ClientConnectionCount =>
            _ListOfListener?.Count() ?? 0;

        public void Initialize()
        {
            StopServer();

            _Listener = new TcpListener(new IPEndPoint(IPAddress.Parse(IP), Port));

            _ListOfListener = new Dictionary<Guid, SocketListener>();

            _Listener.Start();
            _ServerThread = new Thread(new ThreadStart(ServerThreadStart));
            _ServerThread.Start();

            _PurgeThread = new Thread(new ThreadStart(PurgeThreadStart));
            _PurgeThread.Priority = ThreadPriority.Lowest;
            _PurgeThread.Start();
        }

        public void StopServer()
        {
            if (_Listener != null)
            {
                _StopServer = true;
                _Listener.Stop();

                _ServerThread.Join(1000);
                _ServerThread = null;

                _StopPurge = true;

                _PurgeThread.Join(1000);
                _PurgeThread = null;

                // Free Server Object.
                _Listener = null;

                // Stop All clients.
                StopAllListers();
            }
        }

        private void StopAllListers()
        {
            var Keys = _ListOfListener.Keys.ToArray();
            for (int i = 0; i < _ListOfListener.Count; i++)
                _ListOfListener[Keys[i]].StopListener();

            _ListOfListener.Clear();
            _ListOfListener = null;
        }

        private void ServerThreadStart()
        {
            // Client Socket variable;
            Socket _ClientSocket = null;
            T _SocketListener = null;

            while (!_StopServer)
            {
                try
                {
                    if (_Listener.Pending())
                    {
                        _ClientSocket = _Listener.AcceptSocket();

                        // create using abstract method
                        _SocketListener = (T)Activator.CreateInstance(typeof(T), _ClientSocket);
                        lock (_ListOfListener)
                        {
                            _ListOfListener.Add(_SocketListener.Id, _SocketListener);
                        }

                        _SocketListener.StartListener();
                    }
                    else
                        Thread.Sleep(0);
                }
                catch (SocketException)
                {
                    _StopServer = true;
                }
            }
        }

        private void PurgeThreadStart()
        {
            while (!_StopPurge)
            {
                lock (_ListOfListener)
                {
                    var Keys = _ListOfListener.Keys.ToArray();
                    for (int i = 0; i < Keys.Length; i++)
                        if (_ListOfListener[Keys[i]].Delete)
                        {
                            _ListOfListener[Keys[i]].StopListener();
                            _ListOfListener.Remove(Keys[i]);
                        }
                }

                Thread.Sleep(500);
            }
        }

        public override void Dispose() =>
            StopServer();


        public NetServer(uint Signature1)
            : base(Signature1) { }

        ~NetServer() =>
            StopServer();
    }
}