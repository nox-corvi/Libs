using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Security
{
    public class tinyRSA : IDisposable
    {
        private const int KEY_SIZE = 4096;

        private RSA _rsa;

        //private tinyKey _publicKey;
        //private tinyKey _privateKey;

        #region Properties
        //public tinyKey PublicKey =>
        //    _publicKey;

        //public tinyKey PrivateKey =>
        //    _privateKey;

        public int DefaultPassLength { get; set; } = 32;
        #endregion

        private tinyRSA(int KeySizeInBytes, byte[] privateKey, byte[] publicKey)
        {
            int read = 0;

            _rsa = RSA.Create(KeySizeInBytes);
            _rsa.ImportRSAPrivateKey(privateKey, out read);
            _rsa.ImportRSAPublicKey(publicKey, out read);   
        }

        public void b()
        {
            //_rsa.ImportEncryptedPkcs8PrivateKey()
        }

        public tinyKey ExportPrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters) =>
            new tinyKey(_rsa.ExportEncryptedPkcs8PrivateKey(passwordBytes, pbeParameters));

        public tinyKey ExportPrivateKey() =>
            new tinyKey(_rsa.ExportRSAPrivateKey());

        public tinyKey ExportPublicKeyPlain() =>
            new tinyKey(_rsa.ExportRSAPublicKey());

        //TODO:Optimize
        private int FindBestMatchKeySize(int KeySize)
        {
            var keySizes = _rsa.LegalKeySizes;
            int bestMatch = int.MinValue;

            foreach (var item in keySizes)
            {
                int i = item.MinSize;
                while (i <= item.MaxSize)
                {
                    // difference current keysize
                    int c = Math.Abs(KeySize - i);
                    int b = Math.Abs(KeySize - bestMatch);

                    if (c < b)
                        bestMatch = i;

                    i += item.SkipSize;

                    if (item.SkipSize == 0)
                        break;
                }
            }

            return bestMatch;
        }

        public tinyRSA(int keysize)
        {
            _rsa = RSA.Create();
            int bestMatchKeySize = FindBestMatchKeySize(keysize);

            _rsa.KeySize = bestMatchKeySize;
        }
        public tinyRSA()
            : this(KEY_SIZE) { }

        public void Dispose() =>
            _rsa.Clear();
    }
}
