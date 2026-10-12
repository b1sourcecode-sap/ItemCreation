using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace ItemCreationInterCompanyUtility
{
    /// <summary>Error returned by SAP Business One Service Layer (or the HTTP call to it).</summary>
    public class ServiceLayerException : Exception
    {
        public int StatusCode { get; private set; }

        public ServiceLayerException(int statusCode, string message, Exception inner)
            : base(message, inner)
        {
            StatusCode = statusCode;
        }
    }

    /// <summary>
    /// Minimal SAP Business One Service Layer client: one instance = one session with one company.
    /// Login() opens the session, Logout() (or Dispose) always closes it so no session/licence is left open.
    /// </summary>
    public sealed class ServiceLayerClient : IDisposable
    {
        private readonly string _baseUrl;                 // e.g. https://server:50000/b1s/v1/
        private readonly bool _ignoreCertErrors;
        // Session cookie, handled the same way as the InterCompany add-on: the B1SESSION and ROUTEID
        // values from the Login answer are kept as text and sent back in a "Cookie" header on every call.
        // (A CookieContainer drops B1SESSION when Service Layer sends it without a path, and every call
        // after Login then fails with "301 - Invalid session or session already timeout".)
        private string _cookie;
        private string _userName;
        private string _password;
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };

        public string CompanyDB { get; private set; }
        public bool IsLoggedIn { get; private set; }
        public int TimeoutMs = 120000;

        static ServiceLayerClient()
        {
            // Service Layer needs TLS 1.2 and does not answer "Expect: 100-continue"
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            ServicePointManager.Expect100Continue = false;
        }

        public ServiceLayerClient(string baseUrl, bool ignoreCertErrors)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                throw new ArgumentException("Service Layer URL is empty.");
            _baseUrl = baseUrl.Trim().TrimEnd('/') + "/";
            _ignoreCertErrors = ignoreCertErrors;
        }

        public string BaseUrl { get { return _baseUrl; } }

        #region Session

        public void Login(string companyDb, string userName, string password)
        {
            var body = new Dictionary<string, object>
            {
                { "CompanyDB", companyDb },
                { "UserName", userName },
                { "Password", password }
            };
            _cookie = null;
            IsLoggedIn = false;
            CompanyDB = companyDb;
            _userName = userName;
            _password = password;

            string[] setCookie;
            string answer = SendOnce("POST", "Login", body, out setCookie);
            _cookie = BuildCookie(setCookie, answer);
            if (string.IsNullOrEmpty(_cookie))
                throw new ServiceLayerException(0, "Service Layer login to " + companyDb + " returned no session (B1SESSION).", null);
            IsLoggedIn = true;
        }

        /// <summary>"B1SESSION=..; ROUTEID=.." from the Login answer (Set-Cookie headers, SessionId in the body as fallback).</summary>
        private string BuildCookie(string[] setCookieHeaders, string loginAnswer)
        {
            string all = setCookieHeaders == null ? string.Empty : string.Join(",", setCookieHeaders);
            string session = CookieValue(all, "B1SESSION");
            string route = CookieValue(all, "ROUTEID");

            if (string.IsNullOrEmpty(session) && !string.IsNullOrEmpty(loginAnswer))
            {
                try
                {
                    var root = _json.DeserializeObject(loginAnswer) as Dictionary<string, object>;
                    object id;
                    if (root != null && root.TryGetValue("SessionId", out id) && id != null)
                        session = id.ToString();
                }
                catch
                {
                    // no usable body: handled by the caller
                }
            }

            if (string.IsNullOrEmpty(session)) return null;
            string cookie = "B1SESSION=" + session;
            if (!string.IsNullOrEmpty(route)) cookie += "; ROUTEID=" + route;   // keeps us on the same Service Layer node
            return cookie;
        }

        private static string CookieValue(string setCookie, string name)
        {
            Match m = Regex.Match(setCookie ?? string.Empty, @"(?:^|[,;\s])" + name + @"=([^;,\s]*)", RegexOptions.IgnoreCase);
            return m.Success ? m.Groups[1].Value : null;
        }

        /// <summary>Closes the session. Returns null when it worked, otherwise the error text.</summary>
        public string Logout()
        {
            if (!IsLoggedIn) return null;
            try
            {
                string[] ignored;
                SendOnce("POST", "Logout", null, out ignored);
                return null;
            }
            catch (ServiceLayerException ex)
            {
                // Session already closed on the server: nothing is left open, so this is not an error.
                if (IsSessionLost(ex)) return null;
                return ex.Message;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                IsLoggedIn = false;
                _cookie = null;
                _password = null;
            }
        }

        public void Dispose()
        {
            Logout();
        }

        #endregion

        #region Items

        public bool ItemExists(string itemCode)
        {
            try
            {
                Send("GET", "Items(" + Key(itemCode) + ")?$select=ItemCode", null);
                return true;
            }
            catch (ServiceLayerException ex)
            {
                if (ex.StatusCode == 404) return false;
                throw;
            }
        }

        public void AddItem(Dictionary<string, object> item)
        {
            Send("POST", "Items", item);
        }

        public void UpdateItem(string itemCode, Dictionary<string, object> changes)
        {
            Send("PATCH", "Items(" + Key(itemCode) + ")", changes);
        }

        /// <summary>Item group code (OITB.ItmsGrpCod) for a group name, or null when the group does not exist.</summary>
        public int? GetItemGroupCode(string groupName)
        {
            foreach (Dictionary<string, object> row in Query("ItemGroups?$select=Number,GroupName&$filter=GroupName eq " + Key(groupName)))
            {
                object number;
                if (row.TryGetValue("Number", out number) && number != null)
                    return Convert.ToInt32(number);
            }
            return null;
        }

        /// <summary>Type of the given OITM user fields (name without "U_" -> db_Alpha, db_Numeric, db_Date, db_Float, db_Memo).</summary>
        public Dictionary<string, string> GetItemUserFieldTypes(params string[] fieldNames)
        {
            var types = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (fieldNames == null || fieldNames.Length == 0) return types;

            var names = new List<string>();
            foreach (string n in fieldNames) names.Add("Name eq " + Key(n));
            string filter = "TableName eq 'OITM' and (" + string.Join(" or ", names.ToArray()) + ")";

            foreach (Dictionary<string, object> row in Query("UserFieldsMD?$select=Name,Type&$filter=" + filter))
            {
                object name, type;
                if (row.TryGetValue("Name", out name) && row.TryGetValue("Type", out type) && name != null && type != null)
                    types[name.ToString()] = type.ToString();
            }
            return types;
        }

        #endregion

        #region HTTP

        /// <summary>'value' quoted for an OData key or filter and escaped for the URL.</summary>
        private static string Key(string value)
        {
            return "'" + Uri.EscapeDataString((value ?? string.Empty).Replace("'", "''")) + "'";
        }

        private List<Dictionary<string, object>> Query(string relativeUrl)
        {
            var rows = new List<Dictionary<string, object>>();
            string text = Send("GET", relativeUrl.Replace(" ", "%20"), null);
            if (string.IsNullOrEmpty(text)) return rows;

            var root = _json.DeserializeObject(text) as Dictionary<string, object>;
            object value;
            if (root == null || !root.TryGetValue("value", out value)) return rows;

            var list = value as IEnumerable;
            if (list == null) return rows;
            foreach (object o in list)
            {
                var row = o as Dictionary<string, object>;
                if (row != null) rows.Add(row);
            }
            return rows;
        }

        /// <summary>True for "301 - Invalid session or session already timeout" (HTTP 401).</summary>
        private static bool IsSessionLost(ServiceLayerException ex)
        {
            return ex.StatusCode == 401
                || (ex.Message ?? string.Empty).IndexOf("Invalid session", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>One call in the current session. If the session was lost, logs in again and repeats the call once.</summary>
        private string Send(string method, string relativeUrl, object body)
        {
            string[] ignored;
            try
            {
                return SendOnce(method, relativeUrl, body, out ignored);
            }
            catch (ServiceLayerException ex)
            {
                if (!IsSessionLost(ex) || !IsLoggedIn || _password == null) throw;
            }

            Login(CompanyDB, _userName, _password);
            return SendOnce(method, relativeUrl, body, out ignored);
        }

        private string SendOnce(string method, string relativeUrl, object body, out string[] setCookie)
        {
            setCookie = null;
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(_baseUrl + relativeUrl);
            req.Method = method;
            if (!string.IsNullOrEmpty(_cookie))
                req.Headers.Add("Cookie", _cookie);  // B1SESSION + ROUTEID from Login
            req.UserAgent = "ItemCreationVehicle/1.0";
            req.Accept = "application/json";
            req.KeepAlive = true;
            req.Timeout = TimeoutMs;
            req.ReadWriteTimeout = TimeoutMs;
            req.ServicePoint.Expect100Continue = false;
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            if (_ignoreCertErrors)
                req.ServerCertificateValidationCallback = (sender, cert, chain, errors) => true;   // self-signed B1 certificate

            try
            {
                if (body != null)
                {
                    byte[] data = Encoding.UTF8.GetBytes(_json.Serialize(body));
                    req.ContentType = "application/json";
                    req.ContentLength = data.Length;
                    using (Stream s = req.GetRequestStream())
                        s.Write(data, 0, data.Length);
                }
                else if (method != "GET")
                {
                    req.ContentLength = 0;
                }

                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                {
                    setCookie = resp.Headers.GetValues("Set-Cookie");
                    return ReadBody(resp);
                }
            }
            catch (WebException ex)
            {
                HttpWebResponse resp = ex.Response as HttpWebResponse;
                if (resp == null)
                    throw new ServiceLayerException(0, "Service Layer not reachable at " + _baseUrl + " : " + ex.Message, ex);

                using (resp)
                {
                    int status = (int)resp.StatusCode;
                    throw new ServiceLayerException(status, ErrorText(ReadBody(resp), status, resp.StatusDescription), ex);
                }
            }
        }

        private static string ReadBody(HttpWebResponse resp)
        {
            using (Stream s = resp.GetResponseStream())
            {
                if (s == null) return string.Empty;
                using (StreamReader r = new StreamReader(s, Encoding.UTF8))
                    return r.ReadToEnd();
            }
        }

        /// <summary>Pulls "code - message" out of {"error":{"code":..,"message":{"value":".."}}}.</summary>
        private string ErrorText(string responseBody, int status, string statusDescription)
        {
            try
            {
                var root = _json.DeserializeObject(responseBody) as Dictionary<string, object>;
                object errObj;
                if (root != null && root.TryGetValue("error", out errObj))
                {
                    var err = errObj as Dictionary<string, object>;
                    if (err != null)
                    {
                        object code, msgObj;
                        err.TryGetValue("code", out code);
                        string message = null;
                        if (err.TryGetValue("message", out msgObj))
                        {
                            var msg = msgObj as Dictionary<string, object>;
                            object val;
                            if (msg != null && msg.TryGetValue("value", out val) && val != null)
                                message = val.ToString();
                            else if (msgObj != null)
                                message = msgObj.ToString();
                        }
                        if (!string.IsNullOrEmpty(message))
                            return (code != null ? code + " - " : "") + message;
                    }
                }
            }
            catch
            {
                // not JSON: fall through to the HTTP status
            }
            return "HTTP " + status + " " + statusDescription;
        }

        #endregion
    }
}
