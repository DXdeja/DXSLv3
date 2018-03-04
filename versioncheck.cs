using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;

namespace dxsl
{
    public class versioncheck
    {
        public static bool TerminateProcess;

        private const uint fversion = 0x03030200;
        private static Thread maint;

        public versioncheck()
        {
            maint = Thread.CurrentThread;
            Thread oThread = new Thread(new ThreadStart(VCStart));
            oThread.Start();
        }

        public void VCStart()
        {
            TcpClient cl = new TcpClient();

            try
            {
                cl.Connect("update.dxsl.dxmp.tk", 23010);
                cl.ReceiveBufferSize = 0x20000;

                while (cl.Client.Available < 4) Thread.Sleep(10);

                byte[] sversion = new byte[4];
                cl.Client.Receive(sversion);

                if (BitConverter.ToUInt32(sversion, 0) > fversion)
                {
                    if (MessageBox.Show("New version of DXSL is available. Would you like to upgrade?", "DXSL Upgrade", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    {
                        // send version
                        cl.Client.Send(BitConverter.GetBytes(fversion));

                        // receive file size
                        byte[] fsize = new byte[4];
                        cl.Client.Receive(fsize, fsize.Length, SocketFlags.None);

                        // receive file
                        byte[] fdata = new byte[BitConverter.ToInt32(fsize, 0)];
                        int r = 0;
                        while (r < fdata.Length)
                            r += cl.Client.Receive(fdata, r, fdata.Length - r, SocketFlags.None);

                        File.WriteAllBytes(Application.StartupPath + "\\_tmp.bin", fdata);

                        string batchfile = "@ECHO off\r\n" +
                                           "title DXSL Upgrade\r\n" +
                                           "ECHO Finalizing upgrade of DXSL. Please wait...\r\n" +
                                           "ECHO DXSL is restarting. Do not shut down the console!\r\n" +
                                           "ping 127.0.0.1 -n 4 > NUL\r\n" +
                                           "erase dxsl.exe > NUL\r\n" +
                                           "rename _tmp.bin dxsl.exe > NUL\r\n" +
                                           "start dxsl.exe > NUL\r\n" +
                                           "erase _upgrade.bat\r\n" +
                                           "exit\r\n";

                        File.WriteAllBytes(Application.StartupPath + "\\_upgrade.bat", ASCIIEncoding.ASCII.GetBytes(batchfile));

                        Process.Start(Application.StartupPath + "\\_upgrade.bat");

                        TerminateProcess = true;
                    }
                }
            }
            catch
            {
            }

            try
            {
                // close socket
                cl.Client.Disconnect(false);
                cl.Client.Close();
                cl.Close();
            }
            catch { }
        }
    }
}
