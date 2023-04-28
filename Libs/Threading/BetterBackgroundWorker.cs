using System;
using System.Threading;

namespace Nox.Threading
{
    public delegate void DoWorkEventHandler(object sender, DoWorkEventArgs e);

    public class BetterBackgroundWorker 
        : IBetterBackgroundWorker
    {
        public event DoWorkEventHandler DoWork;
        //public delegate void RunWorkerCompletedEventHandler(object sender, RunWorkerCompletedEventArgs e);

        private Thread _WorkerThread;
        private bool _IsBusy = false;

        #region Properties
        public int CancelationThreadWait { get; set; } = 100;
        public bool CancellationPending { get; private set; }

        public int AutoLoopWaitTime { get; set; } = 0;
        public bool AutoLoopWorker { get; set; } = false;


        public bool IsBusy
            => _IsBusy;
        #endregion

        public void Run(object Argument = null)
        {
            CancellationPending = false;
            _WorkerThread =
                new Thread(
                    new ThreadStart(() =>
                    {
                        _IsBusy = true;

                        var e = new DoWorkEventArgs(Argument);
                        try
                        {
                            do
                            {
                                DoWork.Invoke(this, e);

                                if (CancellationPending) { break; }
                                Thread.Sleep(AutoLoopWaitTime);
                            } while ((AutoLoopWorker) & (!e.Cancel));
                        }
                        catch (Exception ex)
                        {
                            e.Result = ex;
                           
                        }
                        finally
                        {
                            _IsBusy = false;
                        }
                    }));

            _WorkerThread.Start();
        }

        public void Cancel()
        {
            CancellationPending = true;
            while (_IsBusy)
                Thread.Sleep(CancelationThreadWait);

        }

        public BetterBackgroundWorker() { }
    }

    public class DoWorkEventArgs
        : CancelEventArgs
    {
        #region Properties
        public object Argument { get; }

        public object Result { get; set; }
        #endregion

        public DoWorkEventArgs(object argument)
        {
            Argument = argument;
        }

    }

    //public class RunWorkerCompletedEventArgs
    //    : EventArgs
    //{
    //    private readonly object _result;

    //    #region Properties
    //    public object Result
    //    {
    //        get
    //        {
    //            return _result;
    //        }
    //    }
    //    #endregion

    //    public RunWorkerCompletedEventArgs(object result)
    //        : base() =>
    //        _result = result;
    //}
}
