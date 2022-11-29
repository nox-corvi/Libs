using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Security
{
    public class tin55yKey
    {
        //private tinySHA _tinySHA = new();

        private byte[] _Key;
        private byte[] _Hash;

        #region Properties
        public byte[] Key { get => _Key; }
        public byte[] Hash { get => _Hash; }
        #endregion

        public tin55yKey(byte[] Key, byte[] Hash)
        {
            this._Key = Key;
            this._Hash = Hash;
        }

        public override string ToString()
        {
            string Key = Helpers.EnHEX(_Key);
            string Hash = Helpers.EnHEX(_Hash);
            return $"{nameof(tin55yKey)} {_Key.Length}, {_Hash.Length}{Environment.NewLine}" +
                $"{Hash}{Environment.NewLine}" +
                $"{Key}{Environment.NewLine}";
        }

        public static tin55yKey Parse(string value)
        {
            string[] items = value.Split(Environment.NewLine);

            if (items.Length != 3)
                throw new ArgumentOutOfRangeException("item count mismatch");

            var Header = items[0];
            if (!Header.StartsWith(nameof(tin55yKey)))
                throw new ArgumentException("header mismatch");

            var Hash = Helpers.DeHEX(items[1]);
            var Key = Helpers.DeHEX(items[2]);

            return new tin55yKey(Key, Hash);
        }

        public tin55yKey(byte[] Key)
        {
            _Key = Key;

            // quick n dirty
            _Hash = SHA384.Create().ComputeHash(_Key);
        }
    }
}
