using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace dxslserver
{
    public class Upgrader
    {
        private TcpListener listener;
        private List<UpgradeClient> upclients;
        public static uint fversion;
        public static byte[] filedata;

        public Upgrader(int port, uint ver)
        {
            fversion = ver;
            upclients = new List<UpgradeClient>();

            try
            {
                filedata = File.ReadAllBytes("dxsl.exe");
            }
            catch (IOException ex)
            {
                Console.WriteLine("UPGRADER: " + ex.Message);
                System.Threading.Thread.CurrentThread.Abort();
            }

            Console.WriteLine("UPGRADER: File read: " + filedata.Length + " bytes");

            try
            {
                listener = new TcpListener(IPAddress.Any, port);

                listener.Server.Blocking = false;
                listener.Start();

                Console.WriteLine("UPGRADER: server on port: " + port);
            }
            catch (SocketException se)
            {
                Console.WriteLine("UPGRADER: " + se.Message);
            }
        }

        public void UpgraderProcess()
        {
            // check for incoming new clients
            try
            {
                if (listener.Pending())
                {
                    upclients.Add(new UpgradeClient(listener.AcceptTcpClient()));
                }
            }
            catch (SocketException se)
            {
                Console.WriteLine("UPGRADER: " + se.Message);
            }

            // process clients
            List<UpgradeClient> cltorem = new List<UpgradeClient>();

            foreach (UpgradeClient c in upclients)
                if (!c.ClientProcess()) cltorem.Add(c);

            foreach (UpgradeClient c in cltorem)
            {
                c.ClientDisconnect();
                upclients.Remove(c);
            }
        }
    }

    public class UpgradeClient
    {
        private TcpClient tclient;
        private DateTime contime;

        public UpgradeClient(TcpClient cl)
        {
            tclient = cl;
            tclient.Client.Blocking = false;
            tclient.SendBufferSize = 0x20000; // 128kb
            contime = DateTime.Now;

            tclient.Client.Send(BitConverter.GetBytes(Upgrader.fversion)); // send version

            Console.WriteLine("UPGRADER [" + DateTime.Now.ToLongTimeString() + "] Version check from: " + ((IPEndPoint)tclient.Client.RemoteEndPoint).ToString());
        }

        public bool ClientProcess()
        {
            // recv 4 bytes, marking version
            if (tclient.Available == 4)
            {
                byte[] buff = new byte[4];
                tclient.Client.Receive(buff);

                if (BitConverter.ToUInt32(buff, 0) < Upgrader.fversion)
                {
                    Console.WriteLine("UPGRADER [" + DateTime.Now.ToLongTimeString() + "] Sending new DXSL to: " + ((IPEndPoint)tclient.Client.RemoteEndPoint).ToString());
                    tclient.Client.Send(BitConverter.GetBytes(Upgrader.filedata.Length));
                    tclient.Client.Send(Upgrader.filedata);
                }

                return false;
            }

            if ((contime + TimeSpan.FromSeconds(60.0)) < DateTime.Now)
                return false;

            return true;
        }

        public void ClientDisconnect()
        {
            tclient.Client.Disconnect(false);
            tclient.Client.Close();
            tclient.Close();
        }
    }
}
