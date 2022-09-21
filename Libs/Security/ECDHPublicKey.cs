using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Security
{
    public class ECDHPublicKey
        : ECDiffieHellmanPublicKey
    {
        public ECDHPublicKey(byte[] rawPublicKey)
            : base(rawPublicKey)
        {

        }
    }
}
