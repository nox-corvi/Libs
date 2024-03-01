#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Text.RegularExpressions;
using System.Net.Mail;

namespace Nox
{
    /* web security
     * 
     * Request = true;
     * RegularExpressionValidator: "^[a-zA-Z'.\s]{1,40}$" Default Text
     * URI: ^(?:http|https|ftp)://[a-zA-Z0-9\.\-]+(?:\:\d{1,5})?(?:[A-Za-z0-9\.\;\:\@\&\=\+\$\,\?/]|%u[0-9A-Fa-f]{4}|%[0-9A-Fa-f]{2})*$
     * validateArguments e.G. numeric Fields, dateFields
     * 
     * try { string mappedPath = Request.MapPath( inputPath.Text, Request.ApplicationPath, false); }
     * catch (HttpException) { // Cross-application mapping attempted }
     * 
     * Use Command Parameters for SQL Queries
     * 
     * <customErrors mode="remoteOnly" />
     * <customErrors mode="On" defaultRedirect="YourErrorPage.htm" />
     */

    public static class WebHelper
    {
        public const string URI_PATTERN = @"^(?:http|https|ftp)://[a-zA-Z0-9\.\-]+(?:\:\d{1,5})?(?:[A-Za-z0-9\.\;\:\@\&\=\+\$\,\?/]|%u[0-9A-Fa-f]{4}|%[0-9A-Fa-f]{2})*$";

        /// <summary>
        /// Überprüft eine URL auf gültigkeit
        /// </summary>
        /// <param name="URI">die zu prüfende URL</param>
        /// <returns></returns>
        public static bool URIValid(string URI) => new Regex(URI_PATTERN).IsMatch(URI);

        private const int BUFFER_LEN = 2048;
        public static int FullHttpPost(Func<StringBuilder, StringBuilder> action)
        {
            if (HttpContext.Current == null)
                throw new ArgumentNullException();

            var c = HttpContext.Current;
            var inStream = c.Request.InputStream;

            var xmlRequest = new StringBuilder();
            byte[] buffer = new byte[BUFFER_LEN];

            while (inStream.Read(buffer, 0, BUFFER_LEN) == BUFFER_LEN)
                xmlRequest.Append(Encoding.UTF8.GetString(buffer));

            if (xmlRequest.Length != 0)
            {
                // ins Log schreiben wenn LogLevel erreicht
                //Global.Log.LogFunc(action: () => xmlRequest.ToString(), LogLevel: Log4.Log4LevelEnum.Debug);

                // aufrufen !
                var xmlResponse = action?.Invoke(xmlRequest);

                // ins Log schreiben wenn LogLevel erreicht
                //Global.Log.LogFunc(action: () => xmlResponse.ToString(), LogLevel: Log4.Log4LevelEnum.Debug);

                // und response schreiben
                c.Response.Write(xmlResponse);
            }

            return xmlRequest.Length;
        }

        public static bool SendMail(string SmtpServer, string From, string To, string Subject, string Message)
        {
            var message = new MailMessage(From, To);

            message.Body = Message;
            message.Subject = Subject;

            //Global.Log.LogMessage(Subject + ": " + Message, Log4.Log4LevelEnum.Debug);

            SmtpClient Client = new SmtpClient(SmtpServer);
            // Credentials are necessary if the server requires the client 
            // to authenticate before it will send email on the client's behalf.
            Client.UseDefaultCredentials = true;

            try
            {
                Client.Send(message);

                return true;
            }
            catch (Exception ex)
            {
                //TODO: sendmail with true/false, log if possible ...
                //Global.Log.LogException(ex);
                return false;
            }
        }
    }
}
#endif