using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Security
{
    public class tinyKey
    {
        private tinySHA _tinySHA = new();

        private byte[] _Key;
        private byte[] _Hash;

        #region Properties
        public byte[] Key { get => _Key; }
        public byte[] Hash { get => _Hash; }
        #endregion

        public tinyKey(byte[] Key, byte[] Hash)
        {
            this._Key = Key;
            this._Hash = Hash;
        }

        public override string ToString()
        {
            string Key = Helpers.EnHEX(_Key);
            string Hash = Helpers.EnHEX(_Hash);
            return $"{nameof(tinyKey)} {_Key.Length}, {_Hash.Length}{Environment.NewLine}" +
                $"{Hash}{Environment.NewLine}" +
                $"{Key}{Environment.NewLine}";
        }

        public static tinyKey Parse(string value)
        {
            string[] items = value.Split(Environment.NewLine);

            if (items.Length != 3)
                throw new ArgumentOutOfRangeException("item count mismatch");

            var Header = items[0];
            if (!Header.StartsWith(nameof(tinyKey)))
                throw new ArgumentException("header mismatch");

            var Hash = Helpers.DeHEX(items[1]);
            var Key = Helpers.DeHEX(items[2]);

            return new tinyKey(Key, Hash);
        }

        public tinyKey(byte[] Key)
        {
            _Key = Key;
            _Hash = _tinySHA.ComputeHash(_Key);
        }
    }
}
