using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox
{
    public interface IRunner
    {
        bool IsInitialized { get; }

        void Initialize();
        void Run();

        void Done();
    }
}
