using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Drawing;

namespace dxsl
{
    public class Chat
    {
        public delegate void UpdateStatusText(string text);
        public delegate void ChatResult(bool success);

        public delegate void ChatRoomEvent(Color col, DateTime dt, string text, bool self);
        public delegate void UserListEvent(string text, bool newuser);
        public delegate void ConnectionDropped();

        private TcpClient cl;
        private NetworkStream netstr;
        private byte[] logindata;
        private UpdateStatusText StatusCB;
        private ChatResult ResultCB;
        private byte[] inbuff;
        private int inbuff_off;
        public bool Connected;

        public string NickName;

        public ChatRoomEvent ChatRoomEventCB;
        public UserListEvent UserListEventCB;
        public ConnectionDropped ConnectionDroppedCB;

        public Chat(string hostname, int port, string nickname, string chlg, string captcha, 
            UpdateStatusText statuscb, ChatResult cr)
        {
            logindata = ASCIIEncoding.ASCII.GetBytes("LOGIN\n" + "V2\n" +
                nickname + "\n" + chlg + "\n" + captcha).Concat(new byte[1] { (byte)'\r' }).ToArray();

            inbuff = new byte[0x1000];
            inbuff_off = 0;
            StatusCB = statuscb;
            ResultCB = cr;
            Connected = false;

            StatusCB("Connecting to chat...");

            try
            {
                cl = new TcpClient();
                cl.BeginConnect(hostname, port, ConnectCallback1, (object)cl);
            }
            catch (SocketException se)
            {
                StatusCB(se.Message);
                ResultCB(false);
            }
        }

        public bool Process()
        {
            if (!Connected) return true;
            // get data
            if (netstr.DataAvailable)
            {
                int r;
                try
                {
                    r = netstr.Read(inbuff, inbuff_off, 0x1000 - inbuff_off);
                }
                catch
                {
                    return false;
                }
                if (r == 0) return false;
                inbuff_off += r;
                while (Parse()) { }
            }

            return true;
        }

        private bool Parse()
        {
            if (inbuff_off == 0) return false;
            int i = Array.IndexOf<byte>(inbuff, (byte)'\r');
            if (i == -1) return false;

            byte[] msg = inbuff.Take(i).ToArray();
            inbuff_off -= i + 1;
            Buffer.BlockCopy(inbuff, i + 1, inbuff, 0, inbuff_off);

            ParseMsg(msg);

            return true;
        }

        private bool SendBytes(byte[] pb)
        {
            try
            {
                netstr.Write(pb, 0, pb.Length);
            }
            catch
            {
                if (ConnectionDroppedCB != null)
                    ConnectionDroppedCB();
                return false;
            }

            return true;
        }

        public bool SendMsg(string str)
        {
            byte[] pb = ASCIIEncoding.ASCII.GetBytes(str + "\r");
            return SendBytes(pb);
        }

        private void ParseMsg(byte[] msg)
        {
            string smsg = ASCIIEncoding.ASCII.GetString(msg);
            string[] splt = smsg.Split('\n');

            if (splt.Length == 0) return;

            if (splt.Length == 1 && splt[0] == "PING")
            {
                SendMsg("PONG");
            }

            if (splt.Length == 2 && splt[0] == "LOGINOK")
            {

                NickName = splt[1];

                // login all OK
                if (ResultCB != null)
                    ResultCB(true);
            }

            else if (splt.Length == 2 && splt[0] == "LOGINFAIL")
            {
                // failed to login
                if (StatusCB != null)
                    StatusCB("Failed to login: " + splt[1]);
                if (ResultCB != null)
                    ResultCB(false);

            }

            else if (splt[0] == "USERLIST")
            {
                splt = splt.Skip(1).ToArray();
                foreach (string s in splt)
                {
                    if (UserListEventCB != null)
                        UserListEventCB(s, true);
                }
            }

            else if (splt.Length == 2 && splt[0] == "JOINED")
            {
                if (ChatRoomEventCB != null)
                    ChatRoomEventCB(Color.Green, DateTime.Now, splt[1] + " has joined the chat.", splt[1] == NickName);
                if (UserListEventCB != null)
                    UserListEventCB(splt[1], true);
            }

            else if (splt.Length == 2 && splt[0] == "QUITTED")
            {
                if (ChatRoomEventCB != null)
                    ChatRoomEventCB(Color.Red, DateTime.Now, splt[1] + " has quitted the chat.", splt[1] == NickName);
                if (UserListEventCB != null)
                    UserListEventCB(splt[1], false);
            }

            else if (splt.Length == 3 && splt[0] == "MSGG")
            {
                if (ChatRoomEventCB != null)
                    ChatRoomEventCB(Color.Black, DateTime.Now, splt[1] + ": " + splt[2], splt[1] == NickName);
            }
        }

        private void ConnectCallback1(IAsyncResult ar)
        {
            try
            {
                cl.EndConnect(ar);
            }
            catch
            {
                StatusCB("Failed to connect.");
                ResultCB(false);
                return;
            }

            StatusCB("Connected. Sending login data...");

            netstr = cl.GetStream();
            netstr.Write(logindata, 0, logindata.Length);

            StatusCB("Verifying login data...");

            Connected = true;
        }

        public void SendText(string txt)
        {
            txt = txt.Replace("\n", "");
            txt = txt.Replace("\r", "");
            if (txt.Length > 0)
                SendMsg("MSGG\n" + txt);
        }

        public void Disconnect()
        {
            if (Connected)
            {
                Connected = false;

                SendMsg("QUIT");

                cl.Client.Disconnect(false);
                cl.Client.Close(1);
                cl.Close();
                netstr.Close(1);
            }
        }
    }
}
