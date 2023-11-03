using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com
{
    public class LoopEventArgs<T>
        : CancelEventArgs
    {
        public ThreadSafeDataList<T> DataList { get; set; }

        public LoopEventArgs(ThreadSafeDataList<T> dataList)
        {
            DataList = dataList;
        }
    }
}
