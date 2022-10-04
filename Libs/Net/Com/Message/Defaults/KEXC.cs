namespace Nox.Net.Com.Message.Defaults
{
    public class kexc_data
        : DataBlock
    {
        private byte[] _PublicKey;

        #region Properties
        public byte[] PublicKey { get => _PublicKey; set => _PublicKey = value; }
        #endregion

        public override void Read(byte[] data) =>
            _PublicKey = data;

        public override byte[] Write() =>
            _PublicKey;

        public kexc_data(uint Signature2)
            : base(Signature2) { }
    }

    public class KEXC
       : RawMessage<kexc_data>
    {
        public KEXC(uint Signature1)
            : base(Signature1, (uint)DefaultMessageTypeEnum.KEXC) { }
    }
}
