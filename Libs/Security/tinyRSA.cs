using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Security
{
    public class tinyRSA : IDisposable
    {
        private const int KEY_SIZE = 4096;

        private RSAParameters _parameters = new RSAParameters();

        protected RSA _rsa;

        #region Properties

        public RSASignaturePadding SigningPadding { get; set; } = RSASignaturePadding.Pkcs1;
        public RSAEncryptionPadding EncryptionPadding { get; set; } = RSAEncryptionPadding.Pkcs1;

        public HashAlgorithmName UsedHashAlgorithm { get; set; } = HashAlgorithmName.SHA384;
        #endregion

        //TODO:Optimize
        protected int FindBestMatchKeySize(int KeySize)
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

        #region Constructors
        /// <summary>
        /// new instance with best match approximate key length
        /// </summary>
        public tinyRSA()
            : this(KEY_SIZE) { }

        /// <summary>
        /// new instance importing private  / public key bundle
        /// </summary>
        /// <param name="privateKey"></param>
        public tinyRSA(byte[] privateKey)
        {
            int read = 0;

            _rsa = RSA.Create();
            _rsa.ImportRSAPrivateKey(privateKey, out read);
        }

        /// <summary>
        /// new instance with best match approximate key length
        /// </summary>
        /// <param name="keysize"></param>
        public tinyRSA(int keysize)
        {
            _rsa = RSA.Create();
            int bestMatchKeySize = FindBestMatchKeySize(keysize);

            _rsa.KeySize = bestMatchKeySize;
        }
        #endregion

        public byte[] Encrypt(byte[] data) =>
            _rsa.Encrypt(data, EncryptionPadding);

        public byte[] Decrypt(byte[] data) =>
            _rsa.Decrypt(data, EncryptionPadding);

        public byte[] ExportPrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters) =>
            _rsa.ExportEncryptedPkcs8PrivateKey(passwordBytes, pbeParameters);

        public byte[] ExportPrivateKey() =>
            _rsa.ExportRSAPrivateKey();

        public byte[] ExportPublicKey() =>
            _rsa.ExportRSAPublicKey();

        public int ImportPublicKey(byte[] bytes)
        {
            _rsa.ImportRSAPublicKey(bytes, out int readBytes);
            return readBytes;
        }

        public int ImportPrivateKey(byte[] bytes)
        {
            _rsa.ImportRSAPrivateKey(bytes, out int readBytes);
            return readBytes;
        }

        public void Dispose() =>
           _rsa.Clear();
    }
}
