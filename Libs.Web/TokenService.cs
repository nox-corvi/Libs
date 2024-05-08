using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Nox.Web;

//public class TokenService
//{
//    public void SaveToken(string token, DateTime expiry)
//    {
//        localStorage.SetItem("authToken", token);
//        localStorage.SetItem("expiry", expiry.ToString());
//    }

//    public string GetToken()
//    {
//        return localStorage.GetItem("authToken");
//    }

//    public DateTime GetExpiry()
//    {
//        var expiryString = localStorage.GetItem("expiry");
//        return DateTime.Parse(expiryString);
//    }

//    public bool IsTokenValid()
//    {
//        var token = GetToken();
//        var expiry = GetExpiry();
//        return !string.IsNullOrEmpty(token) && DateTime.Now <= expiry;
//    }

//    public void ClearToken()
//    {
//        localStorage.RemoveItem("authToken");
//        localStorage.RemoveItem("expiry");
//    }
//}
