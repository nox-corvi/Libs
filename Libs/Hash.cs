using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Libs
{
    public static class Hash
    {
        /// <summary>
        /// FNV-1a implementation to hash short string like filenames or keywords
        /// </summary>
        /// <param name="Filename"></param>
        /// <returns>Ein hashwert mit 32-Bit</returns>
        public static uint HashFNV1a32(string value)
        {
            uint hash   = 2166136261U;
            uint prime  = 16777619;

            foreach (var Item in value)
                hash = (hash ^ (byte)Item) * prime;

            return hash;
        }

        /// <summary>
        /// FNV-1a implementation to hash short string like filenames or keywords
        /// </summary>
        /// <param name="Filename"></param>
        /// <returns>Ein hashwert mit 64-Bit</returns>
        public static ulong HashFNV1a64(string value)
        {
            ulong hash  = 14695981039346656037UL;
            ulong prime = 1099511628211UL;

            foreach (var Item in value)
                hash = (hash ^ (byte)Item) * prime;

            return hash;
        }


        /// <summary>
        /// Computes a hash of a given string
        /// </summary>
        /// <param name="rawData">the string Value the hash has to compute for</param>
        /// <returns>a sha-256 hash value fix an fixed length of 256 bytes</returns>
        public static string ComputeSHA256Hash(string rawData)
        {
            // Create a SHA256   
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // ComputeHash - returns byte array  
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to a string   
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}
