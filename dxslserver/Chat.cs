using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace dxslserver
{
    public class Chat
    {
        public const string CAPTCHA_PVT_KEY = "6LcfS-USAAAAANxOn4hXknzK-TsCKUlTOs0N32wb";

        public enum ChatEventType
        {
            JOINED,
            QUITTED,
            MSGG,
            USERLIST
        }

        private TcpListener listener;
        private List<ChatClient> clients;
        public Dictionary<string, string> GameSrvAuth;

        public Chat(int port)
        {
            GameSrvAuth = new Dictionary<string, string>();

            try
            {
                string[] servers = File.ReadAllLines("servers.txt");
                foreach (string s in servers)
                {
                    string[] splt = s.Split('\t');
                    if (splt.Length == 2)
                    {
                        GameSrvAuth.Add(splt[0], splt[1]);
                        Console.WriteLine("CHAT: Game server auth added: " + splt[0] + " pw: " + splt[1]);
                    }
                }
            }
            catch
            {
            }

            clients = new List<ChatClient>();

            try
            {
                listener = new TcpListener(IPAddress.Any, port);

                listener.Server.Blocking = false;
                listener.Start();

                Console.WriteLine("CHAT: server on port: " + port);
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.Message);
            }
        }

        public void ChatProcess()
        {
            // check for incoming new clients
            if (clients.Count < 32)
            {
                try
                {
                    if (listener.Pending())
                    {
                        clients.Add(new ChatClient(listener.AcceptTcpClient(), this));
                    }
                }
                catch (SocketException se)
                {
                    Console.WriteLine(se.Message);
                }
            }

            // process clients
            List<ChatClient> cltorem = new List<ChatClient>();

            foreach (ChatClient c in clients)
                if (!c.ClientProcess()) cltorem.Add(c);

            foreach (ChatClient c in cltorem)
            {
                c.ClientDisconnect();
                clients.Remove(c);
            }
        }

        public void ChatEvent(ChatClient cc, ChatEventType cet, object opt)
        {
            string[] sndstr = new string[2];

            sndstr[0] = cet.ToString();
            if (opt != null) sndstr[1] = (string)opt;

            switch (cet)
            {
                case ChatEventType.JOINED:
                case ChatEventType.QUITTED:
                    sndstr[1] = cc.NickName;
                    break;

                case ChatEventType.MSGG:
                    if (sndstr[1].Length > 512) sndstr[1] = sndstr[1].Remove(512);
                    sndstr[0] += "\n" + cc.NickName;
                    break;

                default:
                    break;
            }

            foreach (ChatClient c in clients)
            {
                if (cc.type == ChatClient.ClientType.SERVER && c.type == ChatClient.ClientType.SERVER)
                    continue; // forbid SERVER TO SERVER communication
                c.SendMsg(sndstr);
            }
        }

        public bool IsNickAllowed(ChatClient w, ref string nick, out bool notify)
        {
            notify = false;

            if (nick.Length == 0) return false;

            if (nick.Length > 32)
                nick = nick.Remove(32);

            if (w.type == ChatClient.ClientType.USER && nick.StartsWith("*"))
                return false;

            foreach (ChatClient c in clients)
            {
                if (c.NickName == null) continue;
                if (c.state != ChatClient.ClientState.OK) continue;
                if (c.NickName.ToLower() == nick.ToLower())
                {
                    if (c.type == ChatClient.ClientType.SERVER)
                    {
                        // temporary; server does not close connections gracefully on map restart
                        // let it in by silently kill old instance of server
                        c.state = ChatClient.ClientState.DESTROY;
                        return true;
                    }
                    else
                        return false;
                }
            }

            notify = true;
            return true;
        }

        public string GetAllUsers(ChatClient cc)
        {
            string ret = "";

            foreach (ChatClient c in clients)
            {
                //if (c == cc) continue;
                ret += c.NickName + "\n";
            }

            if (ret.Length > 0) ret = ret.Remove(ret.Length - 1);

            return ret;
        }
    }

    public class ChatClient
    {
        public enum ClientState
        {
            PENDING,
            OK,
            DESTROY
        }

        public enum ClientType
        {
            USER,
            SERVER
        }

        private TcpClient cl;
        public ClientState state;
        private NetworkStream netstr;
        private byte[] inbuff;
        private int inbuff_off;
        private DateTime ConTime;
        public string NickName;
        private Chat ChatOwner;
        private DateTime PingTime;
        private bool PingSent;
        private bool ConError;
        private DateTime LastMSGGTime;
        public ClientType type;

        public ChatClient(TcpClient incl, Chat s)
        {
            ChatOwner = s;
            cl = incl;
            state = ClientState.PENDING;
            cl.Client.Blocking = true;
            netstr = cl.GetStream();
            inbuff = new byte[0x500];
            inbuff_off = 0;
            ConTime = DateTime.Now;
            PingSent = false;
            PingTime = new DateTime(0);
            ConError = false;

            ConsoleNotify("new connection");
        }

        public string GetAddress()
        {
            return ((IPEndPoint)cl.Client.RemoteEndPoint).ToString();
        }

        public void ClientDisconnect()
        {
            ConsoleNotify("closing connection");

            if (state == ClientState.OK)
            {
                // do some notifications
                ChatOwner.ChatEvent(this, Chat.ChatEventType.QUITTED, null);
            }

            try
            {
                cl.Client.Disconnect(false);
                cl.Client.Close(1);
                cl.Close();
                netstr.Close(1);
            }
            catch { }
        }

        public bool ClientProcess()
        {
            if (state == ClientState.DESTROY || ConError)
                return false;

            // get data
            if (netstr.DataAvailable)
            {
                int r;
                try
                {
                    r = netstr.Read(inbuff, inbuff_off, 0x500 - inbuff_off);
                }
                catch
                {
                    return false;
                }
                if (r == 0) return false;
                inbuff_off += r;
                while (Parse()) { }
            }

            if (state == ClientState.PENDING && ConTime < (DateTime.Now - TimeSpan.FromSeconds(60.0)))
            {
                // waiting too long for login
                ConsoleNotify("login timeout");
                return false;
            }

            if (state == ClientState.OK)
            {
                if (!PingSent && (PingTime + TimeSpan.FromSeconds(8.0)) < DateTime.Now)
                {
                    PingSent = true;
                    PingTime = DateTime.Now;
                    //ConsoleNotify("ping");
                    return SendMsg("PING");
                }
                else if (PingSent && (PingTime + TimeSpan.FromSeconds(5.0)) < DateTime.Now)
                {
                    ConsoleNotify("connection timeout");
                    return false;
                }
            }

            return true;
        }

        private bool SendBytes(byte[] pb)
        {
            //ConsoleNotify("sending " + pb.Length.ToString() + " bytes");
            try
            {
                netstr.Write(pb, 0, pb.Length);
            }
            catch
            {
                ConError = true;
                return false;
            }

            return true;
        }

        public bool SendMsg(string str)
        {
            byte[] pb = ASCIIEncoding.ASCII.GetBytes(str + "\r");
            return SendBytes(pb);
        }

        public bool SendMsg(string[] strs)
        {
            string a = string.Join("\n", strs);
            byte[] pb = ASCIIEncoding.ASCII.GetBytes(a + "\r");
            return SendBytes(pb);
        }

        private void ConsoleNotify(string txt)
        {
            Console.WriteLine("CHAT [" + DateTime.Now.ToLongTimeString() + "]: Client " + GetAddress() + ": " + txt);
        }

        private bool Parse()
        {
            if (inbuff_off == 0) return false;
            int i = Array.IndexOf<byte>(inbuff, (byte)'\r');
            if (i == -1) return false;

            byte[] msg = inbuff.Take(i).ToArray();
            inbuff_off -= i + 1;
            if (inbuff_off < 0) inbuff_off = 0;
            Buffer.BlockCopy(inbuff, i + 1, inbuff, 0, inbuff_off);

            ParseMsg(msg);

            return true;
        }

        private void ParseMsg(byte[] msg)
        {
            bool notify;
            string smsg = ASCIIEncoding.ASCII.GetString(msg);

            if (state == ClientState.PENDING)
            {
                // read login data
                string[] logstr = smsg.Split('\n');

                if (logstr.Length == 5 && logstr[0] == "LOGIN")
                {
                    if (logstr[1] != "V2")
                    {
                        SendMsg(new string[2] { "LOGINFAIL", "Old protocol version." });
                        state = ClientState.DESTROY;
                        return;
                    }

                    if (logstr[2] == "SERVER")
                    {
                        if (!ChatOwner.GameSrvAuth.Keys.Contains(logstr[3]) || 
                            ChatOwner.GameSrvAuth[logstr[3]] != logstr[4])
                        {
                            SendMsg(new string[2] { "LOGINFAIL", "Wrong server or password." });
                            state = ClientState.DESTROY;
                            return;
                        }

                        type = ClientType.SERVER;
                        NickName = "*SERVER " + logstr[3];
                        if (!ChatOwner.IsNickAllowed(this, ref NickName, out notify))
                        {
                            SendMsg(new string[2] { "LOGINFAIL", "Nickname already in use." });
                            state = ClientState.DESTROY;
                            return;
                        }

                        ConsoleNotify("login data; server=" + NickName);

                        state = ClientState.OK;

                        ConsoleNotify("login success");

                        SendMsg(new string[2] { "LOGINOK", NickName });
                        if (notify)
                            ChatOwner.ChatEvent(this, Chat.ChatEventType.JOINED, null);

                        return;
                    }

                    if (!ChatOwner.IsNickAllowed(this, ref logstr[2], out notify))
                    {
                        SendMsg(new string[2] { "LOGINFAIL", "Incorrect nickname or already taken" });
                        state = ClientState.DESTROY;
                        return;
                    }

                    type = ClientType.USER;
                    NickName = logstr[2];

                    ConsoleNotify("login data; nick=" + NickName + " challenge=" + logstr[3] + " captcha=" + logstr[4]);

                    CaptchaVerify captcha = new CaptchaVerify(Chat.CAPTCHA_PVT_KEY, logstr[3],
                        logstr[4], ((IPEndPoint)cl.Client.RemoteEndPoint).Address, CaptchaCB);

                    return;
                }
                else
                {
                    state = ClientState.DESTROY;
                }

                return;
            }

            if (state == ClientState.OK)
            {
                string[] indata = smsg.Split('\n');

                if (indata.Length == 1 && indata[0] == "PONG")
                {
                    PingSent = false;
                }

                if (indata.Length == 2 && indata[0] == "MSGG")
                {
                    // message from user
                    if (type == ClientType.USER) // spam check only user messages, not server
                        if ((LastMSGGTime + TimeSpan.FromSeconds(0.5)) > DateTime.Now) return; // antispam
                    LastMSGGTime = DateTime.Now;
                    ChatOwner.ChatEvent(this, Chat.ChatEventType.MSGG, indata[1]);
                }

                if (indata.Length == 1 && indata[0] == "USERLIST")
                {
                    SendMsg("USERLIST\n" + ChatOwner.GetAllUsers(this));
                }

                if (indata.Length == 1 && indata[0] == "QUIT")
                {
                    ConError = true;
                    return;
                }
            }
        }

        private void CaptchaCB(bool result)
        {
            if (result)
            {
                // logged in
                state = ClientState.OK;

                ConsoleNotify("login success");

                SendMsg(new string[2] { "LOGINOK", NickName });
                ChatOwner.ChatEvent(this, Chat.ChatEventType.JOINED, null);
            }
            else
            {
                SendMsg(new string[2] { "LOGINFAIL", "Incorrect captcha" });

                state = ClientState.DESTROY;

                ConsoleNotify("login failed");
            }
        }
    }
}
