using Nox.Component;
using System;

namespace Nox.Net.Com
{
    public interface IDataBlock
    {
        uint Signature2 { get; }

        void Read(byte[] data);
        byte[] Write();
    }

    public abstract class DataBlock : ObservableObject, IDataBlock
    {
        protected bool _dirty = false;

        #region Properties
        public bool Dirty => _dirty;

        #endregion

        public uint Signature2 { get; private set; }

        public abstract void Read(byte[] data);

        public abstract byte[] Write();
        
        public DataBlock(uint Signature2)
            : this() =>
            this.Signature2 = Signature2;

        private DataBlock() =>
            PropertyChanged += (sender, e) =>
                _dirty = true;
    }

    // to make rawMessage a pure byte Message
    public class ByteDataBlock : DataBlock
    {
        private byte[] _data;

        #region Properties
        public byte[] Data { get => _data; set => _data = value; }
        #endregion

        public override void Read(byte[] data) =>
            _data = data;

        public override byte[] Write() =>
            _data;

        public ByteDataBlock(uint Signature2)
            : base(Signature2) { }
    }
}
