using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Nox.Net.Com.Message.Defaults
{
    public class resp_data
        : DataBlock
    {
        private const int MAX_LENGTH = 64;

        private uint _Response1 = 0;
        private uint _Response2 = 0;
        private uint _Response3 = 0;

        #region Properties
        public uint Response1 { get { return _Response1; } set { SetProperty(ref _Response1, value); } }
        public uint Response2 { get { return _Response2; } set { SetProperty(ref _Response2, value); } }
        public uint Response3 { get { return _Response3; } set { SetProperty(ref _Response3, value); } }
        #endregion

        public override void Read(byte[] data)
        {
            var Result = new List<byte>();

            _Id = new Guid(data);

            int i = 16;
            _Response1 = BitConverter.ToUInt32(data, i);
            i += Marshal.SizeOf(_Response1.GetType());

            _Response2 = BitConverter.ToUInt32(data, i);
            i += Marshal.SizeOf(_Response1.GetType());

            _Response3 = BitConverter.ToUInt32(data, i);
            i += Marshal.SizeOf(_Response1.GetType());
        }

        public override byte[] Write()
        {
            List<byte> Result = new List<byte>();

            Result.AddRange(_Id.ToByteArray());
            Result.AddRange(BitConverter.GetBytes(_Response1));
            Result.AddRange(BitConverter.GetBytes(_Response2));
            Result.AddRange(BitConverter.GetBytes(_Response3));

            return Result.ToArray();
        }

        public resp_data(uint Signature2)
            : base(Signature2) { }

        public resp_data(uint Signature2, uint Response1, uint Response2 = 0, uint Response3 = 0)
            : this(Signature2)
        {
            _Response1 = Response1;
            _Response2 = Response2;
            _Response3 = Response3;
        }
    }

    public class RESP
       : RawMessage<resp_data>
    {
        public RESP(uint Signature1)
            : base(Signature1, (uint)DefaultMessageTypeEnum.RESP) { }
    }
}
