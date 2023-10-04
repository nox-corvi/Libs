using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message
{
    public class MessageFeatEventArgs <T>
        : EventArgs
        where T : Enum
    {
        public T Feature { get; }

        public MessageFeatEventArgs(T feature)
            => this.Feature = feature;
    }

    public class MessageFeatData<T>
        : DataBlock
        where T : Enum
    {
        private T _Feature;

        #region Properties
        public T Feature { get => _Feature; set => SetProperty(ref _Feature, value); }
        #endregion

        public override void Read(byte[] data)
        {
            int i = 16;
            _Feature = (T)Enum.ToObject(typeof(T), BitConverter.ToUInt32(data, i));
            //i += sizeof(uint);
        }

        public override byte[] Write()
        {
            var Result = new List<byte>();

            Result.AddRange(BitConverter.GetBytes(Convert.ToUInt32(_Feature)));

            return Result.ToArray();
        }

        public MessageFeatData(uint Signature2)
            : base(Signature2) { }
    }

    public class MessageFeat<T>
       : RawMessage<MessageFeatData<T>>
        where T : Enum
    {
        public MessageFeat(uint Signature1)
            : base(Signature1, (uint)MessageTypeEnum.FEAT) { }
    }
}
