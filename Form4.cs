using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace dxsl
{
    public partial class Form4 : Form
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [StructLayout(LayoutKind.Sequential)]
        public struct FLASHWINFO
        {
            public UInt32 cbSize;
            public IntPtr hwnd;
            public UInt32 dwFlags;
            public UInt32 uCount;
            public UInt32 dwTimeout;
        }

        public const UInt32 FLASHW_ALL = 3;


        static public Chat chat;
        private Timer t;
        private Form1 f1;
        private bool isflashing;

        public Form4(Chat c, Form1 fr1)
        {
            InitializeComponent();

            f1 = fr1;
            f1.f4 = this;

            chat = c;
            c.UserListEventCB = UserListEvent;
            c.ChatRoomEventCB = ChatRoomEvent;
            c.ConnectionDroppedCB = ConnectionDropped;
            t = new Timer();
            t.Tick += new EventHandler(t_Tick);
            t.Interval = 5;
            t.Start();
        }

        void t_Tick(object sender, EventArgs e)
        {
            if (isflashing)
            {
                if (this.textBox1.Focused || this.richTextBox1.Focused ||
                    this.listBox1.Focused)
                    FlashTaskbar(false);
            }

            if (!chat.Process())
                ConnectionDropped();
        }

        private void UserListEvent(string usrname, bool isnew)
        {
            // find first
            foreach (object o in listBox1.Items)
            {
                if ((string)o == usrname)
                {
                    if (!isnew) listBox1.Items.Remove(o);
                    return;
                }
            }

            listBox1.Items.Add(usrname);
        }

        private void FlashTaskbar(bool on)
        {
            FLASHWINFO fInfo = new FLASHWINFO();

            fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
            fInfo.hwnd = this.Handle;
            if (on) fInfo.dwFlags = FLASHW_ALL;
            else fInfo.dwFlags = 0;
            fInfo.uCount = UInt32.MaxValue;
            fInfo.dwTimeout = 0;

            FlashWindowEx(ref fInfo);

            isflashing = on;
        }

        private void ChatRoomEvent(Color col, DateTime dt, string text, bool self)
        {
            // check if old lines need to be cleared
            if (richTextBox1.Lines.Length > 255)
            {
                richTextBox1.ReadOnly = false;
                richTextBox1.SelectionStart = 0;
                richTextBox1.SelectionLength = richTextBox1.Text.IndexOf("\n", 0) + 1;
                richTextBox1.SelectedText = "";
                richTextBox1.ReadOnly = true;
            }

            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.SelectionLength = 0;
            richTextBox1.SelectedText = Environment.NewLine;

            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.SelectionLength = 0;
            richTextBox1.SelectionColor = col;
            if (self) richTextBox1.SelectionFont = new Font(richTextBox1.Font, FontStyle.Bold);
            else richTextBox1.SelectionFont = richTextBox1.Font;
            richTextBox1.SelectedText = "[" + dt.ToLongTimeString() + "] " + text;

            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
            richTextBox1.Refresh();

            if (!this.textBox1.Focused)
                FlashTaskbar(true);
        }

        private void ConnectionDropped()
        {
            if (chat.Connected) MessageBox.Show("Connection with server terminated!");
            chat.Disconnect();
            Close();
        }

        private void Form4_Load(object sender, EventArgs e)
        {
            chat.SendMsg("USERLIST");
            textBox1.Select();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            PushText();
        }

        private void Form4_FormClosing(object sender, FormClosingEventArgs e)
        {
            chat.Disconnect();
            f1.f4 = null;
        }

        private void PushText()
        {
            chat.SendText(textBox1.Text);
            textBox1.Text = "";
            textBox1.Focus();
        }

        private void Form4_SizeChanged(object sender, EventArgs e)
        {
            richTextBox1.SelectionStart = richTextBox1.Text.Length;
            richTextBox1.ScrollToCaret();
            richTextBox1.Refresh();
        }

        private void richTextBox1_LinkClicked(object sender, LinkClickedEventArgs e)
        {
            if (e.LinkText.ToLower().StartsWith("deusex://"))
            {
                // fire up game
                if (f1.gamepath == "")
                {
                    MessageBox.Show("DeusEx.exe path not set!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
                    return;
                }
                string[] strArrays = new string[] { e.LinkText, " -hax0r ", f1.gameparams };
                System.Diagnostics.ProcessStartInfo processStartInfo = new System.Diagnostics.ProcessStartInfo(f1.gamepath, string.Concat(strArrays));
                System.Diagnostics.Process.Start(processStartInfo);
            }
            else
            {
                if (MessageBox.Show("Open link in default browser?", "DXSL Chat", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    System.Diagnostics.Process.Start(e.LinkText);
            }
        }

        public void CopyDXSRVLink(string link)
        {
            textBox1.Text += link;
        }
    }
}
