using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace dxslserver
{
    class Program
    {
        static void Main(string[] args)
        {
            //test();

            Chat ChatProg = new Chat(23009);
            Upgrader UpgraderProg = new Upgrader(23010, 0x03030200);

            while (true)
            {
                ChatProg.ChatProcess();
                UpgraderProg.UpgraderProcess();
                System.Threading.Thread.Sleep(2);
            }
        }

        static void test()
        {
            System.Net.Mail.MailMessage message = new System.Net.Mail.MailMessage();

            message.To.Add("l33tsoftw@gmail.com");
            message.Subject = "DXSL Chat Registration Verification";
            message.From = new System.Net.Mail.MailAddress("dxsl.chat@gmail.com", "DXSL Chat");
            message.Body = "Testing goes here";
            System.Net.Mail.SmtpClient smtp = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587);
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new NetworkCredential("dxsl.chat@gmail.com", "/jsH4*s'dc");
            smtp.EnableSsl = true;
            smtp.Send(message);
        }
    }
}
