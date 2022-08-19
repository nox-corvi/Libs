﻿using Nox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Libs.CI
{
    public class WebHandler
        : CIBase
    {
        const int MAX_BLOCK_SIZE = 1024;

        private async Task<string> HttpRequestAsync(string Uri)
        {
            //request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (var client = new HttpClient())
            using (var response = await client.GetAsync(Uri))
            using (var content = response.Content)
                return await content.ReadAsStringAsync();
        }

        private async Task HttpRequestAsync(string Uri, Stream outStream)
        {
            int total = 0, read = 0;
            var data = new byte[MAX_BLOCK_SIZE];

            using (var client = new HttpClient())
            using (var response = await client.GetAsync(Uri))
            using (var content = response.Content.ReadAsStream())
            {
                _logger?.LogMessage($"read content stream", Log4.Log4LevelEnum.Trace);
                while ((read = content.Read(data, 0, MAX_BLOCK_SIZE)) > 0)
                {
                    outStream.Write(data, 0, read);
                    total += read;
                }
            }
        }

        private string HttpRequestString(string Uri)
        {
            var request = HttpRequestAsync(Uri);
            request.Wait();

            return request.Result;
        }

        private void HttpRequestStream(string Uri, Stream outStream) =>
            HttpRequestAsync(Uri, outStream).Wait();

        // download to stream, return is the filesize
        private int GetFile(string Url, NetworkCredential credentials, Stream outStream)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Url, credentials, outStream);
            _logger?.LogMessage($"get web file to stream: {Url ?? ""}", Log4.Log4LevelEnum.Info);

            try
            {
                int total = 0, read = 0;
                var data = new byte[MAX_BLOCK_SIZE];

                var clh = new HttpClientHandler();
                if (credentials != null)
                    clh.Credentials = credentials;

                using (var client = new HttpClient(clh))
                using (var response = client.GetAsync(Url ?? throw new ArgumentNullException()))
                {
                    response.Wait();

                    var rr = response.Result;
                    using (var content = rr.Content.ReadAsStream())
                    {
                        _logger?.LogMessage($"read content stream", Log4.Log4LevelEnum.Trace);
                        while ((read = content.Read(data, 0, MAX_BLOCK_SIZE)) > 0)
                        {
                            outStream.Write(data, 0, read);
                            total += read;
                        }
                    }
                }

                _logger?.LogMessage($"{total} bytes read", Log4.Log4LevelEnum.Trace);

                return total;
            }
            catch (Exception e)
            {
                string ErrMsg = "error: file download failed";

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        private int GetFile(string Url, NetworkCredential credentials, out string Data)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Url, credentials);
            _logger?.LogMessage($"get web file to string: {Url ?? ""}", Log4.Log4LevelEnum.Info);

            Data = "";
            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    int FileLength = GetFile(Url, credentials, (Stream)memoryStream);

                    _logger?.LogMessage($"copy stream to string", Log4.Log4LevelEnum.Trace);
                    memoryStream.Position = 0;
                    using (var streamReader = new StreamReader(memoryStream, true))
                        Data = streamReader.ReadToEnd();

                    _logger?.LogMessage($"{FileLength} bytes read", Log4.Log4LevelEnum.Trace);
                    return FileLength;
                }
            }
            catch (Exception e)
            {
                string ErrMsg = "error: file download failed";

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        private int GetFile(string Url, NetworkCredential credentials, string filename)
        {
            // Log
            _logger?.LogMethod(Log4.Log4LevelEnum.Trace, Url, credentials);
            _logger?.LogMessage($"get web file to file: {Url ?? ""}", Log4.Log4LevelEnum.Info);

            try
            {
                using (var file = File.Create(filename))
                    return GetFile(Url, credentials, file);
            }
            catch (Exception e)
            {
                string ErrMsg = "error: file download failed";

                _CI.CancelWithMessage(ErrMsg);
                throw new ApplicationException(ErrMsg, e);
            }
        }

        public WebHandler(CI CI) 
            : base(CI) { }

        public WebHandler(CI CI, Log4 logger) 
            : base(CI, logger) { }
    }
}