using Nox.Component;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com
{
    public abstract class DataBlock : ObservableObject
    {
        protected bool _dirty = false;

        #region Properties
        public bool Dirty => _dirty;
        #endregion

        // TODO implement r/w in Message class
        public uint Signature1 { get; private set; }

        public abstract void Read(byte[] data);
        public abstract byte[] Write();

        public DataBlock(uint Signature2)
            : this() =>
            this.Signature1 = Signature1;

        private DataBlock() =>
            PropertyChanged += (sender, e) =>
                _dirty = true;
    }
}
