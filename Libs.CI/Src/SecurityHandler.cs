using Microsoft.Extensions.Logging;
using Nox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nox.CI
{
    public class SecurityHandler(CI CI, ILogger Logger)
        : CIBase(CI, Logger)
    {
        protected const string ERR_UNHANDLED_EXCEPTION = "error: unhandled exception";


        public enum CredentialType
        {
            _host,
            _local,
            _domain,
        }

        /// <summary>
        /// create a password with specified length
        /// </summary>
        /// <param name="Length">length of the password</param>
        /// <returns>password string</returns>
        public string PassGen(int Length)
        {
            Logger?.LogDebug($"pass gen using length {Length}");

            var r = new Random((int)(DateTime.Now.Ticks & 0xFFFF));

            var chars = "BCDFGHJKMPQRTVWXY2346789";
            var result = new StringBuilder();

            for (int i = 0; i < Length; i++)
            {
                int p = r.Next(0, chars.Length - 1),
                    q = r.Next(0, 99);

                // never start with a digit
                if (i == 0)
                    while (char.IsDigit(chars[p]))
                        p = r.Next(0, chars.Length - 1);

                if (char.IsDigit(chars[p]))
                    result.Append(chars[p]);
                else
                {
                    switch (q & 0x1)
                    {
                        case 0:
                            result.Append(chars[p].ToString().ToLower());
                            break;
                        case 1:
                            result.Append(chars[p].ToString().ToUpper());
                            break;
                    }
                }
            }

            return result.ToString();
        }

        public SecurityHandler(CI CI, ILogger<SecurityHandler> Logger)
            : this(CI, (ILogger)Logger) { }
    }
}
