namespace FreeboxOS
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Web;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Jint;

    delegate string StringTranform(string str);

    public class FreeboxOS
    {
        private static Regex SetCookieRegex = new Regex(@"FREEBOXOS\s*=\s*""(?<Token>[^""]+)\""\s*;\s*Max-Age=(?<MaxAge>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private const string LoginPath = "api/v3/login/";

        private const string CallLogPath = "api/v3/call/log/";

        private readonly Uri endpoint;

        private readonly string password;

        private string token;

        private DateTime tokenExpiration;

        public FreeboxOS(Uri endpoint, string password)
        {
            this.endpoint = endpoint;
            this.password = password;
        }

        public async Task LoginAsync()
        {
            var challengeAndSalt = await GetChallengeAndSaltAsync();
            var obfuscatedPassword = ObfuscatePassword(this.password, challengeAndSalt.Item1, challengeAndSalt.Item2);

            // Fill the form fields
            var formValues = new NameValueCollection();
            formValues.Add("password", obfuscatedPassword);

            Uri loginUri = new Uri(this.endpoint.ToString() + LoginPath);
            using (var webClient = new WebClient())
            {
                webClient.Headers.Add("X-FBX-FREEBOX0S", "1");
                webClient.Headers.Add(HttpRequestHeader.ContentType, "application/x-www-form-urlencoded");

                try
                {
                    var responseBytes = await webClient.UploadValuesTaskAsync(loginUri, "POST", formValues);
                    var responseString = Encoding.UTF8.GetString(responseBytes);
                    dynamic responseJson = JObject.Parse(responseString);

                    if (responseJson.success != true)
                    {
                        throw new Exception("Failed to login. Response: " + responseString);
                    }

                    var setCookieHeader = webClient.ResponseHeaders[HttpResponseHeader.SetCookie];
                    var match = SetCookieRegex.Match(setCookieHeader);
                    var maxAge = int.Parse(match.Groups["MaxAge"].Value);

                    this.token = match.Groups["Token"].Value;
                    this.tokenExpiration = DateTime.UtcNow.AddSeconds(maxAge);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                }
            }
        }

        public async Task<IEnumerable<CallLogEntry>> DownloadCallLog()
        {
            Uri loginUri = new Uri(this.endpoint.ToString() + CallLogPath);
            using (var webClient = new WebClient())
            {
                await this.SetupWebClientForRequest(webClient);
                string responseString = await webClient.DownloadStringTaskAsync(loginUri);                    
                dynamic responseJson = JObject.Parse(responseString);

                if (responseJson.success != true)
                {
                    throw new Exception("Failed to download call log. Response: " + responseString);
                }

                var callLogEntries = new List<CallLogEntry>();
                foreach (var entry in responseJson.result)
                {
                    callLogEntries.Add(new CallLogEntry
                    {
                        Number = (string)entry.number,
                        Type = (CallType)Enum.Parse(typeof(CallType), (string)entry.type, true),
                        Duration = TimeSpan.FromSeconds((long)entry.duration),
                        Timestamp = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds((long)entry.datetime),
                        Name = (string)entry.name
                    });
                }

                return callLogEntries;
            }
        }

        private async Task SetupWebClientForRequest(WebClient webClient)
        {
            if (string.IsNullOrWhiteSpace(this.token) || (DateTime.UtcNow - this.tokenExpiration) < TimeSpan.FromMinutes(5))
            {
                await this.LoginAsync();
            }

            webClient.Headers.Add("X-FBX-FREEBOX0S", "1");
            webClient.Headers.Add(HttpRequestHeader.Cookie, string.Format(@"FREEBOXOS=""{0}"";", this.token));
        }

        private async Task<Tuple<string, string>> GetChallengeAndSaltAsync()
        {
            Uri loginUri = new Uri(this.endpoint.ToString() + LoginPath);
            using (var webClient = new WebClient())
            {
                webClient.Headers.Add("X-FBX-FREEBOX0S", "1");
                string jsonString = await webClient.DownloadStringTaskAsync(loginUri);
                dynamic jsonObject = JObject.Parse(jsonString);

                if (jsonObject.success != true)
                {
                    throw new Exception("Failed to fetch challenge. Response: " + jsonString);
                }

                string salt = jsonObject.result.password_salt;
                string challenge = string.Empty;

                // The challenge is an array of javacript code.
                // The code in each cell is executed an the results concatenated.
                try
                {
                    foreach (var challengeEntry in jsonObject.result.challenge)
                    {
                        var engine = new Engine();
                        engine.SetValue("challengeEntry", challengeEntry.ToString());
                        engine.SetValue("result", "");
                        engine.SetValue("unescape", (StringTranform)delegate(string s) { return HttpUtility.UrlDecode(s); });
                        string script = @"
                            result = eval(challengeEntry)";
                        engine.Execute(script);
                        challenge += engine.GetValue("result").AsString();
                    }

                    return new Tuple<string, string>(challenge, salt);
                }
                catch (Exception exception)
                {
                    return null;
                }
            }
        }



        private static string ObfuscatePassword(string password, string challenge, string salt)
        {
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                // The bytes of the hexadecimal string of the SHA-1 hash of the bytes of the salt
                // concatenated with the password is used as the key of HMAC SHA-1
                var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(salt + password));
                using (var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(ByteArrayToHexadecimalString(hash))))
                {
                    return ByteArrayToHexadecimalString(hmac.ComputeHash(Encoding.UTF8.GetBytes(challenge)));
                }
            }
        }

        private static string ByteArrayToHexadecimalString(byte[] byteArray)
        {
            return string.Join("", byteArray.Select(b => b.ToString("x2")));
        }
    }
}
