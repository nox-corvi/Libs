﻿//using Microsoft.AspNetCore.Http;
//using Microsoft.EntityFrameworkCore.Metadata.Internal;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Security.Cryptography;
//using System.Text;
//using System.Threading.Tasks;

//namespace Nox.Security
//{
//    /// <summary>
//    /// rijndael encryption
//    /// limited to 128 bit due to the platform
//    /// </summary>
//    public class Laverna2 : IDisposable
//    {
//        private const int MAX_BLOCK_SIZE = 1024;
//        private const string SIGNATURE = "LE";

//        private byte[] _Key;
//        private byte[] _IV;
//        private byte[] _KeyPair;
//        private byte[] _ForeignKey;

//        private ECDiffieHellmanCng _diffieHellman;

//        #region Properties
//        public int MaxBlockSize { get; set; } = MAX_BLOCK_SIZE;

//        public byte[] KeyPair
//        {
//            get => _KeyPair;
//            set => _diffieHellman.ImportECPrivateKey(_KeyPair = value, out int read);
//        }

//        public byte[] ExportECPrivateKey()
//            => _diffieHellman.ExportECPrivateKey();

//        public byte[] PublicKey
//        {
//            get => _diffieHellman.PublicKey.ToByteArray();
//        }

//        public byte[] PrivateKey
//        {
//            get => _diffieHellman.ExportECPrivateKey();
//        }

//        public byte[] ForeignKey
//        {
//            get => _ForeignKey;
//            set => _ForeignKey = value;
//        }

//        public byte[] IV
//        {
//            get => _IV;
//            set => _IV = value;
//        }

//        #endregion

//        public void TransformKey()
//        {
//            var ec_key = new ECDHPublicKey(_ForeignKey);
//            var _Key = _diffieHellman.DeriveKeyMaterial(ec_key);
//        }

//        public void TransformKey(byte[] ForeignKey)
//        {
//            _ForeignKey = ForeignKey;
//            TransformKey();
//        }

//        public ICryptoTransform createEncryptorTransformObject()
//        {
//            using (var myAes = Aes.Create())
//            {
//                myAes.BlockSize = 128;
//                myAes.KeySize = 128;
//                myAes.Padding = PaddingMode.Zeros;
//                myAes.Mode = CipherMode.CBC;
//                myAes.FeedbackSize = 128;

//                return myAes.CreateEncryptor(_Key, _IV);
//            }
//        }

//        public ICryptoTransform createDecryptorTransformObject()
//        {
//            using (var myAes = Aes.Create())
//            {
//                myAes.BlockSize = 128;
//                myAes.KeySize = 128;
//                myAes.Padding = PaddingMode.Zeros;
//                myAes.Mode = CipherMode.CBC;
//                myAes.FeedbackSize = 128;

//                return myAes.CreateEncryptor(_Key, _IV);
//            }
//        }

//        /// <summary>
//        /// encode a stream using a given transform object
//        /// </summary>
//        /// <param name="Source">stream to read from</param>
//        /// <param name="Destination">stream to write to</param>
//        /// <param name="transform">cryptotransform object</param>
//        /// <returns>bytes written</returns>
//        private static int Encode(Stream Source, Stream Destination, ICryptoTransform transform)
//        {
//            int total = 0;

//            using (var CryptoStream = new CryptoStream(Destination, transform, CryptoStreamMode.Write))
//            {
//                BinaryWriter Writer = new BinaryWriter(CryptoStream);

//                byte[] data = new byte[MAX_BLOCK_SIZE]; int read = 0;
//                while ((read = Source.Read(data, 0, data.Length)) > 0)
//                {
//                    Writer.Write(data, 0, read);
//                    total += read;
//                }

//                Writer.Flush();
//                CryptoStream.Flush();

//                CryptoStream.FlushFinalBlock();
//            }

//            return total;
//        }

//        public int Encode(Stream Source, Stream Destination) =>
//            Encode(Source, Destination, createEncryptorTransformObject());

//        /// <summary>
//        /// decodes a stream using a given transform object
//        /// </summary>
//        /// <param name="Source">crypted source</param>
//        /// <param name="Destination">destination to write to</param>
//        /// <param name="transform">cryptotransform object</param>
//        /// <returns>bytes written</returns>
//        private static int Decode(Stream Source, Stream Destination, ICryptoTransform transform)
//        {
//            int total = 0;

//            using (var CryptoStream = new CryptoStream(Source, transform, CryptoStreamMode.Read))
//            {
//                BinaryReader Reader = new BinaryReader(CryptoStream);

//                byte[] data = new byte[MAX_BLOCK_SIZE]; int read = 0;
//                while ((read = Reader.Read(data, 0, data.Length)) > 0)
//                {
//                    Destination.Write(data, 0, read);
//                    total += read;
//                }

//                Destination.Flush();
//            }

//            return total;
//        }

//        public int Decode(Stream Source, Stream Destination) =>
//            Decode(Source, Destination, createDecryptorTransformObject());

//        /// <summary>
//        /// encodes a string
//        /// </summary>
//        /// <param name="Value">string to encode</param>
//        /// <returns>encoded string as base64</returns>
//        public string EncryptString(string Value)
//        {
//            using (var destStream = new MemoryStream())
//            {
//                using (var encodeStream = new MemoryStream())
//                {
//                    var source_bytes = Encoding.UTF8.GetBytes(Value);

//                    // encode
//                    int read = Encode(new MemoryStream(source_bytes), encodeStream, createEncryptorTransformObject());

//                    // get encodes bytes 
//                    byte[] encodedBytes = encodeStream.ToArray();

//                    // write LE
//                    var sigBytes = Encoding.UTF8.GetBytes(SIGNATURE);
//                    destStream.Write(sigBytes, 0, sigBytes.Length);

//                    // write crc, calculated from source data
//                    var crc = new tinyCRC();
//                    crc.Push(source_bytes);
//                    var crc_bytes = BitConverter.GetBytes(crc.CRC32);
//                    destStream.Write(crc_bytes, 0, crc_bytes.Length);

//                    // write LEN
//                    var lenBytes = BitConverter.GetBytes(Value.Length);
//                    destStream.Write(lenBytes, 0, lenBytes.Length);

//                    // write data
//                    destStream.Write(encodedBytes, 0, encodedBytes.Length);
//                }

//                // return as base64
//                return Convert.ToBase64String(destStream.ToArray());
//            }
//        }

//        /// <summary>
//        /// decodes a string
//        /// </summary>
//        /// <param name="Value">string to decode as base64</param>
//        /// <returns>decoded string</returns>
//        public string DecryptString(string Value)
//        {
//            using (var destStream = new MemoryStream())
//            {
//                int index = 0;
//                var data = Convert.FromBase64String(Value);

//                // test LE
//                var sig = Encoding.UTF8.GetString(data, index, SIGNATURE.Length);
//                if (sig != SIGNATURE)
//                    throw new InvalidDataException("signature missmatch");

//                index += SIGNATURE.Length;

//                // get crc
//                var crc_calc = BitConverter.ToUInt32(data, index);
//                index += sizeof(UInt32);

//                var len = BitConverter.ToInt32(data, index);
//                index += sizeof(Int32);

//                using (var decodeStream = new MemoryStream())
//                {
//                    // read as base64
//                    int read = Decode(new MemoryStream(data, index, data.Length - index),
//                        decodeStream, createDecryptorTransformObject());

//                    var decode_bytes = decodeStream.ToArray();

//                    // check the crc, but use len instead of array length because decrypted array may exceed source size due to padding chars
//                    var crc = new tinyCRC();
//                    crc.Push(decode_bytes, 0, len);

//                    if (crc.CRC32 != crc_calc)
//                        throw new InvalidDataException("crc missmatch");

//                    // return specified len as utf8
//                    return Encoding.UTF8.GetString(decodeStream.ToArray(), 0, len);
//                }
//            }
//        }


//        /// <summary>
//        /// prepares the internal data structure to ensure that the internal usage of key and iv are secure 
//        /// </summary>
//        /// <param name="Pass">password as plain text</param>
//        /// <param name="Salt">salt as plain text</param>
//        /// <param name="Iterations">iterations used by pbkdf2 <see cref="Rfc2898"/></param>
//        private void Prepare(string Key, string Salt, int Iterations = 16)
//        {
//            if (Salt.Length < 8)
//                throw new ArgumentOutOfRangeException("salt must be at least 8 characters long");

//            if (Iterations < 1)
//                throw new ArgumentOutOfRangeException("the number of iterations must be at least 1");

//            var _salt = new byte[Salt.Length]; byte pred = 0;
//            for (int i = 0; i < Salt.Length;)
//                unchecked
//                {
//                    pred = _salt[i] = (byte)((byte)Salt[i] + pred);
//                    i++;
//                }

//            using (var _2898 = new Rfc2898DeriveBytes(Key, _salt, Iterations))
//            {
//                _Key = _2898.GetBytes(32);
//                _IV = _2898.GetBytes(16);
//            }
//        }

//        public Laverna2(string Key, string Salt, int Iterations = 16) =>
//            Prepare(Key, Salt, Iterations);

//        public Laverna2()
//        {
//            _diffieHellman = new ECDiffieHellmanCng()
//            {
//                KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash,
//                HashAlgorithm = CngAlgorithm.Sha256,
//            };
//        }

//        public void Dispose()
//        {
//            //
//        }
//    }
//}
