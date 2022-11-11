using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Nox.Security;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Net.Com.Message.Defaults
{
    public class RplyEventArgs : EventArgs
    {
        public Guid EhloId { get; }
        public tinyKey Key { get; }

        public string Message { get; }

        public RplyEventArgs(Guid EhloId, string Message)
        {
            this.EhloId = EhloId;
            this.Key = Key;
            this.Message = Message;
        }
    }  

    public class RplyData
        : DataBlock
    {
        public const int MAX_LENGTH = 64;

        private Guid _EhloId = Guid.Empty;
        
        public tinyKey _publicKey;
        private string _Message = "";

        private ASCIIEncoding ASC = new ASCIIEncoding();

        #region Properties
        public Guid EhloId { get => _EhloId; set => SetProperty(ref _EhloId, value); }
        public tinyKey Key { get => _publicKey; set => SetProperty(ref _publicKey, value); }

        public string Message { get => _Message; set => SetProperty(ref _Message, value.LimitLength(MAX_LENGTH)); }
        #endregion

        public override void Read(byte[] data)
        {
            _EhloId = Helpers.ExtractGuid(data, 0);

            int i = 16, j = 0;
            j = BitConverter.ToInt32(data, i); i += sizeof(int);

            byte[] k = new byte[j];
            Array.Copy(data, i, k, 0, j);
            i += j;

            j = BitConverter.ToInt32(data, i); i += sizeof(int);

            byte[] h = new byte[j];
            Array.Copy(data, i, h, 0, j);
            i += j;

            Key = new tinyKey(k, h);

            _Message = ASC.GetString(data, 16, data.Length - 16).LimitLength(MAX_LENGTH).Trim();
        }

        public override byte[] Write()
        {
            var Result = new List<byte>();
            Result.AddRange(_EhloId.ToByteArray());

            Result.AddRange(BitConverter.GetBytes(Key.Key.Length));
            Result.AddRange(Key.Key);
            Result.AddRange(BitConverter.GetBytes(Key.Hash.Length));
            Result.AddRange(Key.Hash);

            Result.AddRange(ASC.GetBytes(_Message.LimitLength(MAX_LENGTH)));

            return Result.ToArray();
        }

        public RplyData(uint Signature2)
            : base(Signature2) { }
    }

    public class RPLY
       : RawMessage<RplyData>
    {
        public RPLY(uint Signature1)
            : base(Signature1, (uint)DefaultMessageTypeEnum.RPLY) { }
    }
}
