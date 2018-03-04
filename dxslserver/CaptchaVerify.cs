using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace dxslserver
{
    /// <summary>
    /// For server side Captcha verification.
    /// made by one1 (2013)
    /// </summary>
    public class CaptchaVerify
    {
        public delegate void OnResultReceived(bool resolved);

        private OnResultReceived ResultReceivedCB;

        public CaptchaVerify(string pvtkey, string chlg, string r, IPAddress addr, OnResultReceived orr)
        {
            string updata = "privatekey=" + pvtkey + "&remoteip=" + addr.ToString() + "&challenge=" + chlg + "&response=" + r;
            ResultReceivedCB = orr;

            WebClient wc = new WebClient();

            wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            wc.UploadStringCompleted += new UploadStringCompletedEventHandler(wc_UploadStringCompleted);
            wc.UploadStringAsync(new Uri("http://www.google.com/recaptcha/api/verify"), updata);
        }

        void wc_UploadStringCompleted(object sender, UploadStringCompletedEventArgs e)
        {
            ResultReceivedCB(e.Result.StartsWith("true"));
        }
    }
}
