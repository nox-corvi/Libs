using System;

namespace Nox.Net.Com
{
    public class NetBase : IDisposable
    {
        private uint _Signature1;

        #region Properties
        public uint Signature1 =>
            _Signature1;
        #endregion

        public virtual void Dispose() { }

        public NetBase(uint Signature1) =>
            this._Signature1 = Signature1;
    }
}
