using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Security
{
    //public class tinyööSHA : IDisposable
    //{
    //    private HashAlgorithm _sha;

    //    public void Initialize() =>
    //        _sha.Initialize();

    //    public byte[] ComputeHash(byte[] data, int offset, int length) 
    //    {
    //        Initialize();
    //        return _sha.ComputeHash(data);
    //    }

    //    public byte[] ComputeHash(byte[] data) =>
    //        ComputeHash(data, 0, data.Length);

    //    public void Dispose() =>
    //        _sha.Dispose();

    //    public tinySHA() =>
    //        _sha = SHA256.Create();

        
    //    public static PbeParameters PbeParameters() =>
    //        PbeParameters(17926);

    //    public static PbeParameters PbeParameters(int Iterations) =>
    //        new PbeParameters(PbeEncryptionAlgorithm.Aes256Cbc, HashAlgorithm, Iterations);
    //}
}
