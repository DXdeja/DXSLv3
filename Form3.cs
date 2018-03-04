using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace dxsl
{
    public partial class Form3 : Form
    {
        private string ChallengeKey;
        private Chat chat;
        private Timer t;
        private Form1 f1;

        public Form3(Form1 fr1)
        {
            InitializeComponent();

            f1 = fr1;
            textBox2.Text = f1.ChatNickName;
        }

        private void LoadCaptcha()
        {
            textBox1.Text = "";
            textBox1.Focus();
            Captcha c = new Captcha("6LcfS-USAAAAAOLVfw-I9FcPigkgEpxl8wR43ild", OnImageRecv);
        }

        private void Form3_Load(object sender, EventArgs e)
        {

        }

        private void OnImageRecv(byte[] imgdata, string chlgkey)
        {
            MemoryStream ms = new MemoryStream(imgdata);
            pictureBox1.Image = Image.FromStream(ms);
            pictureBox1.Size = new Size(pictureBox1.Image.Size.Width, pictureBox1.Image.Size.Height);
            label1.Visible = false;
            pictureBox1.Visible = true;
            ChallengeKey = chlgkey;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            LoadCaptcha();
        }

        private void EnableControls(bool en)
        {
            button1.Enabled = en;
            button2.Enabled = en;
            textBox1.Enabled = en;
            textBox2.Enabled = en;
        }

        private void StatusUpdateCB(string text)
        {
            if (InvokeRequired)
            {
                Chat.UpdateStatusText ust = new Chat.UpdateStatusText(StatusUpdateCB);
                object[] objArray = new object[] { text };
                Invoke(ust, objArray);
                return;
            }

            toolStripStatusLabel1.Text = text;
            Refresh();
        }

        private void CCStatus(bool success)
        {
            if (InvokeRequired)
            {
                Chat.ChatResult cr = new Chat.ChatResult(CCStatus);
                object[] objArray = new object[] { success };
                Invoke(cr, objArray);
                return;
            }

            t.Stop();

            if (success)
            {
                // close this form and open chat window
                f1.ChatNickName = textBox2.Text;
                Form4 f4 = new Form4(chat, f1);
                Close();
                f4.Show();
            }
            else
            {
                EnableControls(true);
                Refresh();
                LoadCaptcha();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (textBox1.Text.Length == 0)
            {
                MessageBox.Show("Please, resolve captcha to continue!");
                textBox1.Focus();
                return;
            }

            if (textBox2.Text.Length == 0)
            {
                MessageBox.Show("Please, enter your nickname!");
                textBox2.Focus();
                return;
            }

            EnableControls(false);
            chat = new Chat("127.0.0.1", 23009, textBox2.Text, ChallengeKey, textBox1.Text, StatusUpdateCB,
                CCStatus);
            t = new Timer();
            t.Tick += new EventHandler(t_Tick);
            t.Interval = 5;
            t.Start();
        }

        void t_Tick(object sender, EventArgs e)
        {
            chat.Process();
        }

        private void Form3_FormClosing(object sender, FormClosingEventArgs e)
        {
            f1.f3 = null;
        }

        private void Form3_Shown(object sender, EventArgs e)
        {
            Refresh();
            LoadCaptcha();
        }
    }
}
