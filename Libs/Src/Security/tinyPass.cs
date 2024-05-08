using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Security
{
    public class tinyPass : IDisposable
    {
        Rfc2898DeriveBytes _2898 = null;

        private void Prepare(string Key, string Salt, int Iterations = 1000)
        {
            if (Salt.Length < 25)
                throw new ArgumentOutOfRangeException("salt must be at least 25 characters long");

            if (Iterations < 100)
                throw new ArgumentOutOfRangeException("the number of iterations must be at least 100");

            var _salt = new byte[Salt.Length]; byte pred = 0;
            for (int i = 0; i < Salt.Length;)
                unchecked
                {
                    pred = _salt[i] = (byte)((byte)Salt[i] + pred);
                    i++;
                }
#if NETFRAMEWORK
            _2898 = new Rfc2898DeriveBytes(Key, _salt, Iterations);
#elif NETCOREAPP
            _2898 = new Rfc2898DeriveBytes(Key, _salt, Iterations, HashAlgorithmName.SHA256);
#endif
        }

        public ReadOnlySpan<byte> Gen(int Length = 32) =>
            new ReadOnlySpan<byte>(_2898.GetBytes(Length));
            
        public void Reset() => 
            _2898.Reset();

        public void Dispose() =>
            _2898.Dispose();

        public tinyPass(string Key, string Salt, int Iterations = 1000) =>
            Prepare(Key, Salt, Iterations);

    }
}