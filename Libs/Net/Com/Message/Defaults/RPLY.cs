using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message.Defaults
{
    public class rply_data
        : DataBlock
    {
        public const int MAX_LENGTH = 64;

        private string _Message = "";
        private Guid _EhloId = Guid.Empty;
        private DateTime _EhloTime = DateTime.UtcNow;
        private DateTime _RplyTime = DateTime.UtcNow;

        private ASCIIEncoding ASC = new ASCIIEncoding();

        #region Properties
        public Guid EhloId { get => _EhloId; set => SetProperty(ref _EhloId, value); }
        public DateTime EhloTime { get => _EhloTime; set => SetProperty(ref _EhloTime, value); }

        public DateTime RplyTime { get => _EhloTime; set => SetProperty(ref _RplyTime, value); }
        public string Message { get => _Message; set => SetProperty(ref _Message, value.LimitLength(MAX_LENGTH)); }

        #endregion

        public override void Read(byte[] data)
        {
            int i = 0;

            byte[] buffer1 = new byte[16];
            Array.Copy(data, i, buffer1, i, 16);
            _Id = new Guid(buffer1);

            i += 16;
            Array.Copy(data, i, buffer1, 0, i);
            _EhloId = new Guid(buffer1);

            i += 16;
            _EhloTime = DateTime.FromBinary(BitConverter.ToInt64(data, i));

            i += sizeof(long);
            _RplyTime = DateTime.FromBinary(BitConverter.ToInt64(data, i));

            i += sizeof(long);
            _Message = ASC.GetString(data, 16, data.Length - 16).LimitLength(MAX_LENGTH).Trim();
        }

        public override byte[] Write()
        {
            var Result = new List<byte>();
            Result.AddRange(_Id.ToByteArray());
            Result.AddRange(_EhloId.ToByteArray());
            Result.AddRange(BitConverter.GetBytes(_EhloTime.ToBinary()));
            Result.AddRange(BitConverter.GetBytes(_RplyTime.ToBinary()));
            Result.AddRange(ASC.GetBytes(_Message.LimitLength(MAX_LENGTH)));

            return Result.ToArray();
        }

        public rply_data(uint Signature2)
            : base(Signature2) { }
    }

    public class RPLY
       : RawMessage<rply_data>
    {
        public RPLY(uint Signature1)
            : base(Signature1, (uint)DefaultMessageTypeEnum.RPLY) { }
    }
}
