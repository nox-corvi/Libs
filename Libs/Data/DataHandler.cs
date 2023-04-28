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
        public event EventHandler<LoopEventArgs<T>> Loop;

        private Log4 _Log = null!;
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
            => this._Log = Log;

        public DataHandler(Log4 Log, bool InitWait = false)
            : this(Log)
        {
            if (!(InitWait | IsInitialized))
                Initialize();
        }

        public void Dispose()
        {
            _Log?.LogMethod(Log4.Log4LevelEnum.Trace);
            Done();
        }
    }
}
