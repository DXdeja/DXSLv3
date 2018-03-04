using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace dxsl
{
    /// <summary>
    /// Class for obtaining Captcha image from Google Recaptcha service (async)
    /// made by one1 (2013)
    /// </summary>
    public class Captcha
    {
        public delegate void OnImageReceived(byte[] imgdata, string chlgkey);

        private OnImageReceived ImageReceivedCB;
        private string Key;
        private string ChlgKey;

        public Captcha(string key, OnImageReceived ircb)
        {
            Key = key;
            ImageReceivedCB = ircb;

            WebClient wc = new WebClient();
            wc.DownloadDataCompleted += new DownloadDataCompletedEventHandler(wc_DownloadDataCompleted1);
            wc.DownloadDataAsync(new Uri("http://www.google.com/recaptcha/api/challenge?k=" + Key));
        }

        private void wc_DownloadDataCompleted1(object sender, DownloadDataCompletedEventArgs e)
        {
            string wdata = ASCIIEncoding.ASCII.GetString(e.Result);
            wdata = wdata.Substring(wdata.IndexOf("challenge"));
            wdata = wdata.Substring(13);
            ChlgKey = wdata.Remove(wdata.IndexOf('\''));

            WebClient wc = new WebClient();
            wc.DownloadDataCompleted += new DownloadDataCompletedEventHandler(wc_DownloadDataCompleted2);
            wc.DownloadDataAsync(new Uri("http://www.google.com/recaptcha/api/image?c=" + ChlgKey));
        }

        private void wc_DownloadDataCompleted2(object sender, DownloadDataCompletedEventArgs e)
        {
            ImageReceivedCB(e.Result, ChlgKey);
        }
    }
}
