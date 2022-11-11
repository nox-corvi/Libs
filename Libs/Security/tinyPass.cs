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
            if (Salt.Length < 8)
                throw new ArgumentOutOfRangeException("salt must be at least 8 characters long");

            if (Iterations < 1)
                throw new ArgumentOutOfRangeException("the number of iterations must be at least 1");

            var _salt = new byte[Salt.Length]; byte pred = 0;
            for (int i = 0; i < Salt.Length;)
                unchecked
                {
                    pred = _salt[i] = (byte)((byte)Salt[i] + pred);
                    i++;
                }
            _2898 = new Rfc2898DeriveBytes(Key, _salt, Iterations);
        }

        public ReadOnlySpan<byte> Gen(int Length = 32) =>
            new ReadOnlySpan<byte>(_2898.GetBytes(Length));

        public void Dispose() =>
            _2898.Dispose();

        public tinyPass(string Key, string Salt, int Iterations = 1000) =>
            Prepare(Key, Salt, Iterations);

    }
}
