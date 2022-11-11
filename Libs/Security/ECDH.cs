using System;
using System.Security.Cryptography;

namespace Nox.Security
{
    public class ECDH : IDisposable
    {
        public enum ECDHCurveEnum
        {
            nistP256,
            nistP384,
            nistP521
        }

        private tinyKey _PK;
        private ECDiffieHellman _DH;

        #region Properties 
        public tinyKey PublicKey { get => _PK; }
        #endregion



        public void b()
        {
            //_DH.DeriveKeyMaterial();
        }

        public byte[] ExtractDerivedKey(byte[] rawForeignKey)
        {
            var ForeignKey = new ECDHPublicKey(rawForeignKey);
            var key = _DH.DeriveKeyFromHash(ForeignKey, HashAlgorithmName.SHA256);

            return key;
        }

        public ECDH(ECDHCurveEnum Curve)
        {
            switch (Curve)
            {
                case ECDHCurveEnum.nistP256:
                    _DH = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                    break;
                case ECDHCurveEnum.nistP384:
                    _DH = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP384);
                    break;
                case ECDHCurveEnum.nistP521:
                    _DH = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP521);
                    break;
            }

            // retrieve public key
            _PK = new tinyKey(_DH.PublicKey.ToByteArray());
        }

        public void Dispose() =>
            _DH.Dispose();
    }
}
