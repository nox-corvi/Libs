using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Libs.CI
{
    public class ProcessCredential
    {
        public string Username { get; }
        public string Password { get; set; }

        public string Domain { get; set; }

        public ProcessCredential(string Username, string Password, string Domain = "")
        {
            this.Username = Username;
            this.Password = Password;
            this.Domain = Domain;
        }

        public override string ToString() =>
            $"{Username ?? "<null>"}@{Domain ?? "<null>"}:{new string('#', (Password ?? "").Length)}";
    }
}
