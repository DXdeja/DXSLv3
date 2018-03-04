using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace dxsl
{
	internal class ms
	{
		private static string dxcode;

		private TcpClient tcpc;

		public List<IPEndPoint> addrs;

		static ms()
		{
			ms.dxcode = "Av3M99";
		}

		public ms(string host, int port, mscallback mscb, Form owner)
		{
			mscb(string.Concat("Connecting to: ", host, ":", port.ToString()));
			this.tcpc = new TcpClient(AddressFamily.InterNetwork);
			try
			{
				this.tcpc.Connect(host, port);
			}
			catch
			{
                if (!versioncheck.TerminateProcess)
				    MessageBox.Show(owner, string.Concat("Failed to connect to ", host), "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
            if (versioncheck.TerminateProcess) return;
			mscb("Receiving data...");
			while (this.tcpc.Client.Available < 22)
			{
				Thread.Sleep(1);
                if (versioncheck.TerminateProcess) return;
			}
			byte[] numArray = new byte[22];
			this.tcpc.Client.Receive(numArray, 22, SocketFlags.None);
			byte[] numArray1 = new byte[6];
			Buffer.BlockCopy(numArray, 15, numArray1, 0, 6);
			byte[] numArray2 = new byte[8];
			ms.gsgetsecure(Encoding.ASCII.GetString(numArray1), ms.dxcode, 0, numArray2);
			mscb("Sending list request...");
			this.tcpc.Client.Send(Encoding.ASCII.GetBytes("\\gamename\\deusex\\location\\0\\validate\\"));
			this.tcpc.Client.Send(numArray2);
			this.tcpc.Client.Send(Encoding.ASCII.GetBytes("\\final\\\\list\\\\gamename\\deusex\\final\\"));
			mscb("Receiving list...");
			int num = 0;
			numArray = new byte[0];
			string str = "";
			do
			{
                if (versioncheck.TerminateProcess) return;
				if (this.tcpc.Client.Available > 0)
				{
					int available = this.tcpc.Client.Available;
					num = num + available;
					byte[] numArray3 = new byte[num];
					Buffer.BlockCopy(numArray, 0, numArray3, 0, (int)numArray.Length);
					this.tcpc.Client.Receive(numArray3, (int)numArray.Length, available, SocketFlags.None);
					str = Encoding.ASCII.GetString(numArray3);
					numArray = numArray3;
				}
				Thread.Sleep(1);
			}
			while (!str.Contains("\\final\\") && this.tcpc.Connected);
			mscb("Parsing list...");
			this.addrs = new List<IPEndPoint>();
			IPEndPoint pEndPoint = null;
			char[] chrArray = new char[] { '\\' };
			string[] strArrays = str.Split(chrArray);
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				string str1 = strArrays[i];
				if (!(str1 == "ip") && !(str1 == "final"))
				{
					char[] chrArray1 = new char[] { ':' };
					string[] strArrays1 = str1.Split(chrArray1);
					if ((int)strArrays1.Length == 2)
					{
						try
						{
							pEndPoint = new IPEndPoint(IPAddress.Parse(strArrays1[0]), int.Parse(strArrays1[1]));
                            this.addrs.Add(pEndPoint);
						}
						catch
						{
							//goto Label0;
						}
					}
				}
			//Label0:
			}
		}

		[DllImport("gsenc.dll", CharSet=CharSet.None, ExactSpelling=false)]
		public static extern int gsgetsecure(string seccode, string key, int enctype, byte[] dest);
	}
}