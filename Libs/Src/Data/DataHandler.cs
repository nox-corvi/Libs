using Microsoft.Extensions.Logging;
using Nox.Net.Com;
using Nox.Threading;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nox.Data
{
    public class DataHandler<T>
        : IRunner, IList<T>, IDisposable
    {
        private const int TIMEOUT = 15;


        public event EventHandler<LoopEventArgs<T>> Loop;

        private ILogger _Logger;
        private ThreadSafeDataList<T> _data = new();

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

        public int Timeout { get; set; } = TIMEOUT;

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

            try
            {
                // create worker 
                _worker = new BetterBackgroundWorker();
                _worker.DoWork += (object sender, DoWorkEventArgs e) =>
                {
                    var e2 = new LoopEventArgs<T>(_data);
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
                            _Logger.LogError(ex.ToString());

                            // exit if an error occured
                            break;
                        }
                    }

                    e.Cancel = true;
                };

                IsInitialized = true;
            }
            catch (Exception ex)
            {
                _Logger.LogCritical(ex.ToString());
            }
        }

        public void Run()
        {
            if (IsInitialized)
                _worker.Run();
        }

        public void Done()
        {
            if (IsInitialized)
            {
                // stop worker 
                if (_worker.IsBusy)
                {
                    _Logger?.LogTrace("cancel worker");
                    _worker.Cancel();

                    _worker = null;
                    _Logger.LogTrace("await worker is done");
                }

                IsInitialized = false;
            }
        }

        public DataHandler(ILogger logger)
            => this._Logger = logger;

        public DataHandler(ILogger logger, bool InitWait = false)
            : this(logger)
        {
            if (!(InitWait | IsInitialized))
                Initialize();
        }

        public void Dispose() 
            => Done();
    }
}
