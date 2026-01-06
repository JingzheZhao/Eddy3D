using System;

namespace EddyLib
{
    public static partial class Utilities
    {
        public static bool CheckLicence()
        {
            bool licence = false;

            //DateTime dateNow = Utilities.GetNistTime();
            // Get the current date.
            DateTime dateCompile = DateTime.Today;

            //DateTime dateCompile = new DateTime(2020, 10, 1, 0, 00, 00).ToUniversalTime();
            TimeSpan licenceDuration = new TimeSpan(1480, 0, 0, 0);
            DateTime expiresAt = dateCompile.Add(licenceDuration);

            //try
            //{
            //    DateTime dateTime = DateTime.MinValue;
            //    DateTime dateTimeUTC = DateTime.MinValue;

            // System.Net.HttpWebRequest request =
            // (System.Net.HttpWebRequest)System.Net.WebRequest.Create("http://nist.time.gov/actualtime.cgi?lzbc=siqm9b");
            // ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 |
            // SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
            // request.Method = "GET"; request.Accept = "text/html, application/xhtml+xml, */*";
            // request.UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.1;
            // Trident/6.0)"; request.ContentType = "application/x-www-form-urlencoded";
            // //request.ProtocolVersion = HttpVersion.Version11; //request.CachePolicy = new
            // RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore); //No caching
            // System.Net.HttpWebResponse response =
            // (System.Net.HttpWebResponse)request.GetResponse(); if (response.StatusCode ==
            // (System.Net.HttpStatusCode.OK)) { System.IO.StreamReader stream = new
            // StreamReader(response.GetResponseStream()); string html =
            // stream.ReadToEnd();//<timestamp time=\"1395772696469995\" delay=\"1395772696469995\"/>
            // string time = System.Text.RegularExpressions.Regex.Match(html,
            // @"(?<=\btime="")[^""]*").Value; double milliseconds = Convert.ToInt64(time) / 1000.0;
            // dateTime = new DateTime(1970, 1, 1).AddMilliseconds(milliseconds).ToLocalTime();
            // dateTimeUTC = dateTime.ToUniversalTime(); }

            //    if ((dateTimeUTC - dateCompile) > licenceDuration)
            //    {
            //        licence = false;
            //    }
            //    else
            //    {
            //        licence = true;
            //    }
            //}
            //catch(Exception e) {
            //    Debug.WriteLine(e.Message);

            // if (DateTime.Now > expiresAt) licence = false; else licence = true;

            //}

            if (DateTime.Now > expiresAt)
            {
                licence = false;
            }
            else
            {
                licence = true;
            }

            return licence;
        }
    }
}
