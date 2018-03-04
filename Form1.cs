using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using Microsoft.Win32;

namespace dxsl
{
	public class Form1 : Form
	{
        private const string RUN_LOCATION = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";

		private UdpClient uc;

		private byte[] status;

		private string indata;

		private byte[] bindata;

		private EndPoint ep;

		private List<Form1.server_> slist;

		private List<Form1.server_> watchlist;

		private FormWindowState last_state;

		private List<string> masterservers;

		public string gamepath;

        public string gameparams;

        private bool bAutoRefresh;

        private bool bAutoMinimize;

		private Timer after_refresh;

		private int total_servers;

		private int responding_servers;

		private int total_players;

		private SoundPlayer splayer;

		private wd_settings wds;

		private Timer wds_timer;

		private IContainer components;

		private MenuStrip menuStrip1;

		private StatusStrip statusStrip1;

		private ListView listView1;

		private SplitContainer splitContainer1;

		private ListView listView2;

		private ToolStripMenuItem refreshToolStripMenuItem;

		private ColumnHeader columnHeader1;

		private ColumnHeader columnHeader2;

		private ColumnHeader columnHeader3;

		private ColumnHeader columnHeader4;

		private ColumnHeader columnHeader5;

		private ColumnHeader columnHeader6;

		private ColumnHeader columnHeader7;

		private ColumnHeader columnHeader8;

		private ColumnHeader columnHeader9;

		private NotifyIcon notifyIcon1;

		private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;

		private ToolStripMenuItem exitToolStripMenuItem;

		private ToolStripStatusLabel toolStripStatusLabel1;

		private ToolStripMenuItem restoreToolStripMenuItem;

		private ToolStripMenuItem settingsToolStripMenuItem;

		private ToolStripMenuItem aboutToolStripMenuItem;

		private ToolStripMenuItem exitToolStripMenuItem1;

		private ImageList imageList1;

		private ColumnHeader columnHeader10;

		private System.Windows.Forms.ContextMenuStrip contextMenuStrip2;

		private ToolStripMenuItem refreshToolStripMenuItem1;

		private ToolStripMenuItem joinToolStripMenuItem;

		private ToolStripMenuItem watchdogToolStripMenuItem;

		private ToolStripMenuItem addToolStripMenuItem;

		private ToolStripMenuItem removeToolStripMenuItem;

		private ToolStripSeparator toolStripSeparator1;

		private ToolStripMenuItem configureToolStripMenuItem;

		private ToolStripSeparator toolStripSeparator2;

		private ToolStripMenuItem clearListToolStripMenuItem;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripMenuItem copyToolStripMenuItem;
        private ToolStripMenuItem addressToolStripMenuItem;
        private ToolStripMenuItem addressPortToolStripMenuItem;
        private ToolStripMenuItem serverInfoToolStripMenuItem;
        private ToolStripMenuItem viewOnGameTrackerToolStripMenuItem;
        private ToolStripMenuItem chatToolStripMenuItem;

        private ListViewColumnSorter lvwColumnSorter;

        public Form3 f3;
        public Form4 f4;
        private ToolStripMenuItem forDXSLChatToolStripMenuItem;
        private ToolStripMenuItem adminEMailToolStripMenuItem;

        public string ChatNickName;

		public Form1()
		{
            this.InitializeComponent();

            versioncheck vc = new versioncheck();

            this.wds = new wd_settings();
            this.masterservers = new List<string>();

            bool bmax = LoadSettings();

            if (bAutoMinimize) WindowState = FormWindowState.Minimized;
            else
            {
                if (bmax) WindowState = FormWindowState.Maximized;
                else WindowState = FormWindowState.Normal;
            }

			Form1.SetDoubleBuffered(this.listView1);
			Form1.SetDoubleBuffered(this.listView2);
			
			this.splayer = new SoundPlayer(string.Concat(Application.StartupPath, "\\beep.wav"));
			this.slist = new List<Form1.server_>();
			this.watchlist = new List<Form1.server_>();
			this.bindata = new byte[1500];
			this.status = Encoding.ASCII.GetBytes("\\status\\");
			this.uc = new UdpClient(AddressFamily.InterNetwork);
			this.ep = new IPEndPoint((long)0, 0);
			this.uc.Client.Bind(this.ep);
			this.start_async();
			this.wds_timer = new Timer();
			this.wds_timer.Interval = this.wds.checkinterval * 1000;
			this.wds_timer.Tick += new EventHandler(this.wds_timer_Tick);
			this.wds_timer.Start();

            // Create an instance of a ListView column sorter and assign it 
            // to the ListView control.
            lvwColumnSorter = new DXSLMainSorter();
            this.listView1.ListViewItemSorter = lvwColumnSorter;
		}

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (versioncheck.TerminateProcess)
            {
                Application.Exit();
            }
        }

        private bool LoadSettings()
        {
            IniFile ifl = new IniFile(Application.StartupPath + "\\" + "dxsl.ini");

            ChatNickName = ifl.IniReadValue("Chat", "Name");

            gamepath = ifl.IniReadValue("General", "GamePath");

            string a;

            a = ifl.IniReadValue("General", "MasterServers");
            if (a == "") masterservers.Add("master0.gamespy.com:28900");
            else
            {
                string[] b = a.Split(';');

                foreach (string c in b)
                    masterservers.Add(c);
            }

            gameparams = ifl.IniReadValue("General", "GameParameters");

            a = ifl.IniReadValue("General", "AutoRefresh");
            if (a == "" || a.ToLower() == "false") bAutoRefresh = false;
            else bAutoRefresh = true;

            a = ifl.IniReadValue("General", "AutoMinimize");
            if (a == "" || a.ToLower() == "false") bAutoMinimize = false;
            else bAutoMinimize = true;

            a = ifl.IniReadValue("WatchDog", "Beep");
            if (a == "" || a.ToLower() == "true") wds.beep = true;
            else wds.beep = false;

            a = ifl.IniReadValue("WatchDog", "Focus");
            if (a == "" || a.ToLower() == "true") wds.focus = true;
            else wds.focus = false;

            a = ifl.IniReadValue("WatchDog", "AutoJoin");
            if (a == "" || a.ToLower() == "false") wds.@join = false;
            else wds.@join = true;

            a = ifl.IniReadValue("WatchDog", "NotifyOnce");
            if (a == "" || a.ToLower() == "false") wds.@remove = false;
            else wds.@remove = true;

            try
            {
                wds.numplayers = int.Parse(ifl.IniReadValue("WatchDog", "NumPlayers"));
            }
            catch { }

            try
            {
                wds.checkinterval = int.Parse(ifl.IniReadValue("WatchDog", "CheckInterval"));
            }
            catch { }
            
            if (wds.numplayers == 0) wds.numplayers = 5;
            if (wds.checkinterval == 0) wds.checkinterval = 30;

            try { this.Size = new Size(int.Parse(ifl.IniReadValue("Window", "SizeX")), this.Size.Height); }
            catch { }
            try { this.Size = new Size(this.Size.Width, int.Parse(ifl.IniReadValue("Window", "SizeY"))); }
            catch { }
            try { this.splitContainer1.SplitterDistance = int.Parse(ifl.IniReadValue("Window", "SplitX")); }
            catch { }

            for (int i = 0; i < this.listView1.Columns.Count; i++)
            {
                try { this.listView1.Columns[i].Width = int.Parse(ifl.IniReadValue("Window", "Column0_" + i.ToString() + "X")); }
                catch { }
            }

            for (int i = 0; i < this.listView2.Columns.Count; i++)
            {
                try { this.listView2.Columns[i].Width = int.Parse(ifl.IniReadValue("Window", "Column1_" + i.ToString() + "X")); }
                catch { }
            }

            a = ifl.IniReadValue("Window", "Maximized");
            if (a == "" || a.ToLower() == "false") return false;
            else return true;
        }

        private void SaveSettings()
        {
            IniFile ifl = new IniFile(Application.StartupPath + "\\" + "dxsl.ini");

            ifl.IniWriteValue("General", "GamePath", gamepath);
            ifl.IniWriteValue("General", "GameParameters", gameparams);

            string a = "";
            foreach (string s in masterservers)
                a += s + ";";
            if (a.Length > 0) a = a.Remove(a.Length - 1);
            ifl.IniWriteValue("General", "MasterServers", a);

            ifl.IniWriteValue("General", "AutoRefresh", bAutoRefresh.ToString());
            ifl.IniWriteValue("General", "AutoMinimize", bAutoMinimize.ToString());

            ifl.IniWriteValue("WatchDog", "Beep", wds.beep.ToString());
            ifl.IniWriteValue("WatchDog", "Focus", wds.focus.ToString());
            ifl.IniWriteValue("WatchDog", "AutoJoin", wds.@join.ToString());
            ifl.IniWriteValue("WatchDog", "NotifyOnce", wds.@remove.ToString());
            ifl.IniWriteValue("WatchDog", "NumPlayers", wds.numplayers.ToString());
            ifl.IniWriteValue("WatchDog", "CheckInterval", wds.checkinterval.ToString());
        }

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			MessageBox.Show("Copyright (c) 2010-2013 one1\r\n\r\nSpecial thanks to:\r\nDJ - for icon");
		}

		private void addToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.listView1.SelectedItems.Count == 1)
			{
				Form1.server_ tag = (Form1.server_)this.listView1.SelectedItems[0].Tag;
				if (this.watchlist.Contains(tag))
				{
					MessageBox.Show(string.Concat("Server:\r\n\r\n", tag.props.hostname, "\r\n\r\nalready in watchlist!"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
					return;
				}
				this.watchlist.Add(tag);
				MessageBox.Show(string.Concat("Server:\r\n\r\n", tag.props.hostname, "\r\n\r\nadded to watchlist!"), "Information", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
			}
		}

		private void after_refresh_Tick(object sender, EventArgs e)
		{
			foreach (Form1.server_ _server_ in this.slist)
			{
				if (_server_.lvi != null)
				{
					continue;
				}
				_server_.lvi = this.listView1.Items.Add("");
				_server_.lvi.Tag = _server_;
				int port = _server_.address.Port - 1;
				_server_.lvi.SubItems.Add(string.Concat(_server_.address.Address.ToString(), ":", port.ToString()));
				for (int i = 0; i < 4; i++)
				{
					_server_.lvi.SubItems.Add("");
				}
				_server_.lvi.SubItems.Add("9999");
			}
			this.after_refresh.Stop();
			this.after_refresh.Dispose();
		}

		private void clearListToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.watchlist.Clear();
			MessageBox.Show("All servers removed from watchlist!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}

		private void configureToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.open_settings(3);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && this.components != null)
			{
				this.components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			base.Close();
		}

		private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			base.Close();
		}

        protected override void OnClosing(CancelEventArgs e)
        {
            if (f4 != null)
            {
                if (MessageBox.Show("DXSL Chat is running. If you close DXSL, chat session will be terminated. Continue?", "DXSL Exit", MessageBoxButtons.YesNo) == DialogResult.No)
                {
                    e.Cancel = true;
                    return;
                }
            }
            base.OnClosing(e);

        }

		private void Form1_FormClosing(object sender, FormClosingEventArgs e)
		{
            if (e.Cancel == true) return;

            if (Form4.chat != null) Form4.chat.Disconnect();

			this.notifyIcon1.Visible = false;

            IniFile ifl = new IniFile(Application.StartupPath + "\\" + "dxsl.ini");

            if (ChatNickName != null)
                ifl.IniWriteValue("Chat", "Name", ChatNickName);

            if (this.WindowState == FormWindowState.Normal)
            {
                ifl.IniWriteValue("Window", "SizeX", this.Size.Width.ToString());
                ifl.IniWriteValue("Window", "SizeY", this.Size.Height.ToString());
                ifl.IniWriteValue("Window", "SplitX", this.splitContainer1.SplitterDistance.ToString());
            }

            for (int i = 0; i < this.listView1.Columns.Count; i++)
                ifl.IniWriteValue("Window", "Column0_" + i.ToString() + "X", this.listView1.Columns[i].Width.ToString());

            for (int i = 0; i < this.listView2.Columns.Count; i++)
                ifl.IniWriteValue("Window", "Column1_" + i.ToString() + "X", this.listView2.Columns[i].Width.ToString());

            ifl.IniWriteValue("Window", "Maximized", (this.WindowState == FormWindowState.Maximized).ToString());
		}

		private void Form1_Resize(object sender, EventArgs e)
		{
			if (FormWindowState.Minimized == base.WindowState)
			{
				base.Hide();
				return;
			}
			this.last_state = base.WindowState;
		}

		private int get_num_responded_servers()
		{
			int num = 0;
			foreach (ListViewItem item in this.listView1.Items)
			{
				if (item.SubItems[5].Text == "9999")
				{
					continue;
				}
				num++;
			}
			return num;
		}

		private int get_num_total_players()
		{
			int count = 0;
			foreach (Form1.server_ _server_ in this.slist)
			{
				count = count + _server_.players.Count;
			}
			return count;
		}

		private void incb(IAsyncResult iar)
		{
			this.incbsec(iar, DateTime.Now);
		}

		private void incbsec(IAsyncResult iar, DateTime time)
		{
            if (this.listView1.InvokeRequired)
            {
                dincbsec _dincbsec = new dincbsec(this.incbsec);
                object[] objArray = new object[] { iar, time };
                Invoke(_dincbsec, objArray);
                return;
            }
			int num = this.uc.Client.EndReceiveFrom(iar, ref this.ep);
			this.indata = Encoding.ASCII.GetString(this.bindata, 0, num);
			this.parse_packet((IPEndPoint)this.ep, time);
			this.start_async();
		}

		private void InitializeComponent()
		{
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.refreshToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.chatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.listView1 = new System.Windows.Forms.ListView();
            this.columnHeader10 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader4 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader5 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader6 = new System.Windows.Forms.ColumnHeader();
            this.contextMenuStrip2 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.refreshToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.joinToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.watchdogToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.removeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.configureToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addressToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.addressPortToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.serverInfoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.forDXSLChatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewOnGameTrackerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.imageList1 = new System.Windows.Forms.ImageList(this.components);
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.listView2 = new System.Windows.Forms.ListView();
            this.columnHeader7 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader8 = new System.Windows.Forms.ColumnHeader();
            this.columnHeader9 = new System.Windows.Forms.ColumnHeader();
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.restoreToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.adminEMailToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.contextMenuStrip2.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.chatToolStripMenuItem,
            this.aboutToolStripMenuItem,
            this.exitToolStripMenuItem1});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(873, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // refreshToolStripMenuItem
            // 
            this.refreshToolStripMenuItem.Name = "refreshToolStripMenuItem";
            this.refreshToolStripMenuItem.Size = new System.Drawing.Size(57, 20);
            this.refreshToolStripMenuItem.Text = "Refresh";
            this.refreshToolStripMenuItem.Click += new System.EventHandler(this.refreshToolStripMenuItem_Click);
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(58, 20);
            this.settingsToolStripMenuItem.Text = "Settings";
            this.settingsToolStripMenuItem.Click += new System.EventHandler(this.settingsToolStripMenuItem_Click);
            // 
            // chatToolStripMenuItem
            // 
            this.chatToolStripMenuItem.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.chatToolStripMenuItem.Name = "chatToolStripMenuItem";
            this.chatToolStripMenuItem.Size = new System.Drawing.Size(45, 20);
            this.chatToolStripMenuItem.Text = "Chat";
            this.chatToolStripMenuItem.Click += new System.EventHandler(this.chatToolStripMenuItem_Click);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
            this.aboutToolStripMenuItem.Text = "About";
            this.aboutToolStripMenuItem.Click += new System.EventHandler(this.aboutToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem1
            // 
            this.exitToolStripMenuItem1.Name = "exitToolStripMenuItem1";
            this.exitToolStripMenuItem1.Size = new System.Drawing.Size(37, 20);
            this.exitToolStripMenuItem1.Text = "Exit";
            this.exitToolStripMenuItem1.Click += new System.EventHandler(this.exitToolStripMenuItem1_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 380);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(873, 22);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(10, 17);
            this.toolStripStatusLabel1.Text = " ";
            // 
            // listView1
            // 
            this.listView1.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader10,
            this.columnHeader1,
            this.columnHeader2,
            this.columnHeader3,
            this.columnHeader4,
            this.columnHeader5,
            this.columnHeader6});
            this.listView1.ContextMenuStrip = this.contextMenuStrip2;
            this.listView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView1.FullRowSelect = true;
            this.listView1.HideSelection = false;
            this.listView1.Location = new System.Drawing.Point(0, 0);
            this.listView1.MultiSelect = false;
            this.listView1.Name = "listView1";
            this.listView1.ShowItemToolTips = true;
            this.listView1.Size = new System.Drawing.Size(630, 356);
            this.listView1.SmallImageList = this.imageList1;
            this.listView1.TabIndex = 2;
            this.listView1.UseCompatibleStateImageBehavior = false;
            this.listView1.View = System.Windows.Forms.View.Details;
            this.listView1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.listView1_MouseDoubleClick);
            this.listView1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.listView1_MouseClick);
            this.listView1.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listView1_ColumnClick);
            // 
            // columnHeader10
            // 
            this.columnHeader10.Text = "";
            this.columnHeader10.Width = 25;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Address";
            this.columnHeader1.Width = 111;
            // 
            // columnHeader2
            // 
            this.columnHeader2.Text = "Server Name";
            this.columnHeader2.Width = 176;
            // 
            // columnHeader3
            // 
            this.columnHeader3.Text = "Game Type";
            this.columnHeader3.Width = 113;
            // 
            // columnHeader4
            // 
            this.columnHeader4.Text = "Map Name";
            this.columnHeader4.Width = 114;
            // 
            // columnHeader5
            // 
            this.columnHeader5.Text = "Players";
            this.columnHeader5.Width = 48;
            // 
            // columnHeader6
            // 
            this.columnHeader6.Text = "Ping";
            this.columnHeader6.Width = 37;
            // 
            // contextMenuStrip2
            // 
            this.contextMenuStrip2.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.refreshToolStripMenuItem1,
            this.joinToolStripMenuItem,
            this.toolStripSeparator2,
            this.watchdogToolStripMenuItem,
            this.toolStripSeparator3,
            this.copyToolStripMenuItem,
            this.viewOnGameTrackerToolStripMenuItem});
            this.contextMenuStrip2.Name = "contextMenuStrip2";
            this.contextMenuStrip2.Size = new System.Drawing.Size(178, 148);
            // 
            // refreshToolStripMenuItem1
            // 
            this.refreshToolStripMenuItem1.Name = "refreshToolStripMenuItem1";
            this.refreshToolStripMenuItem1.Size = new System.Drawing.Size(177, 22);
            this.refreshToolStripMenuItem1.Text = "Refresh";
            this.refreshToolStripMenuItem1.Click += new System.EventHandler(this.refreshToolStripMenuItem1_Click);
            // 
            // joinToolStripMenuItem
            // 
            this.joinToolStripMenuItem.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Bold);
            this.joinToolStripMenuItem.Name = "joinToolStripMenuItem";
            this.joinToolStripMenuItem.Size = new System.Drawing.Size(177, 22);
            this.joinToolStripMenuItem.Text = "Join";
            this.joinToolStripMenuItem.Click += new System.EventHandler(this.joinToolStripMenuItem_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(174, 6);
            // 
            // watchdogToolStripMenuItem
            // 
            this.watchdogToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addToolStripMenuItem,
            this.removeToolStripMenuItem,
            this.clearListToolStripMenuItem,
            this.toolStripSeparator1,
            this.configureToolStripMenuItem});
            this.watchdogToolStripMenuItem.Name = "watchdogToolStripMenuItem";
            this.watchdogToolStripMenuItem.Size = new System.Drawing.Size(177, 22);
            this.watchdogToolStripMenuItem.Text = "Watchdog";
            // 
            // addToolStripMenuItem
            // 
            this.addToolStripMenuItem.Name = "addToolStripMenuItem";
            this.addToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
            this.addToolStripMenuItem.Text = "Add";
            this.addToolStripMenuItem.Click += new System.EventHandler(this.addToolStripMenuItem_Click);
            // 
            // removeToolStripMenuItem
            // 
            this.removeToolStripMenuItem.Name = "removeToolStripMenuItem";
            this.removeToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
            this.removeToolStripMenuItem.Text = "Remove";
            this.removeToolStripMenuItem.Click += new System.EventHandler(this.removeToolStripMenuItem_Click);
            // 
            // clearListToolStripMenuItem
            // 
            this.clearListToolStripMenuItem.Name = "clearListToolStripMenuItem";
            this.clearListToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
            this.clearListToolStripMenuItem.Text = "Remove all";
            this.clearListToolStripMenuItem.Click += new System.EventHandler(this.clearListToolStripMenuItem_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(123, 6);
            // 
            // configureToolStripMenuItem
            // 
            this.configureToolStripMenuItem.Name = "configureToolStripMenuItem";
            this.configureToolStripMenuItem.Size = new System.Drawing.Size(126, 22);
            this.configureToolStripMenuItem.Text = "Configure";
            this.configureToolStripMenuItem.Click += new System.EventHandler(this.configureToolStripMenuItem_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(174, 6);
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.addressToolStripMenuItem,
            this.addressPortToolStripMenuItem,
            this.serverInfoToolStripMenuItem,
            this.adminEMailToolStripMenuItem,
            this.forDXSLChatToolStripMenuItem});
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.Size = new System.Drawing.Size(177, 22);
            this.copyToolStripMenuItem.Text = "Copy";
            // 
            // addressToolStripMenuItem
            // 
            this.addressToolStripMenuItem.Name = "addressToolStripMenuItem";
            this.addressToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.addressToolStripMenuItem.Text = "Address";
            this.addressToolStripMenuItem.Click += new System.EventHandler(this.addressToolStripMenuItem_Click);
            // 
            // addressPortToolStripMenuItem
            // 
            this.addressPortToolStripMenuItem.Name = "addressPortToolStripMenuItem";
            this.addressPortToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.addressPortToolStripMenuItem.Text = "Address + Port";
            this.addressPortToolStripMenuItem.Click += new System.EventHandler(this.addressPortToolStripMenuItem_Click);
            // 
            // serverInfoToolStripMenuItem
            // 
            this.serverInfoToolStripMenuItem.Name = "serverInfoToolStripMenuItem";
            this.serverInfoToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.serverInfoToolStripMenuItem.Text = "Server Info";
            this.serverInfoToolStripMenuItem.Click += new System.EventHandler(this.serverInfoToolStripMenuItem_Click);
            // 
            // forDXSLChatToolStripMenuItem
            // 
            this.forDXSLChatToolStripMenuItem.Name = "forDXSLChatToolStripMenuItem";
            this.forDXSLChatToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.forDXSLChatToolStripMenuItem.Text = "For DXSL Chat";
            this.forDXSLChatToolStripMenuItem.Click += new System.EventHandler(this.forDXSLChatToolStripMenuItem_Click);
            // 
            // viewOnGameTrackerToolStripMenuItem
            // 
            this.viewOnGameTrackerToolStripMenuItem.Name = "viewOnGameTrackerToolStripMenuItem";
            this.viewOnGameTrackerToolStripMenuItem.Size = new System.Drawing.Size(177, 22);
            this.viewOnGameTrackerToolStripMenuItem.Text = "View on GameTracker";
            this.viewOnGameTrackerToolStripMenuItem.Click += new System.EventHandler(this.viewOnGameTrackerToolStripMenuItem_Click);
            // 
            // imageList1
            // 
            this.imageList1.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imageList1.ImageStream")));
            this.imageList1.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList1.Images.SetKeyName(0, "info_ico.gif");
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 24);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.listView1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.listView2);
            this.splitContainer1.Size = new System.Drawing.Size(873, 356);
            this.splitContainer1.SplitterDistance = 630;
            this.splitContainer1.TabIndex = 3;
            // 
            // listView2
            // 
            this.listView2.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader7,
            this.columnHeader8,
            this.columnHeader9});
            this.listView2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView2.FullRowSelect = true;
            this.listView2.Location = new System.Drawing.Point(0, 0);
            this.listView2.Name = "listView2";
            this.listView2.Size = new System.Drawing.Size(239, 356);
            this.listView2.TabIndex = 0;
            this.listView2.UseCompatibleStateImageBehavior = false;
            this.listView2.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader7
            // 
            this.columnHeader7.Text = "Player Name";
            this.columnHeader7.Width = 135;
            // 
            // columnHeader8
            // 
            this.columnHeader8.Text = "Kills";
            this.columnHeader8.Width = 40;
            // 
            // columnHeader9
            // 
            this.columnHeader9.Text = "Ping";
            this.columnHeader9.Width = 44;
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "DeusEx Server Lister v3.3 beta";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.restoreToolStripMenuItem,
            this.exitToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(113, 48);
            // 
            // restoreToolStripMenuItem
            // 
            this.restoreToolStripMenuItem.Name = "restoreToolStripMenuItem";
            this.restoreToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.restoreToolStripMenuItem.Text = "Restore";
            this.restoreToolStripMenuItem.Click += new System.EventHandler(this.restoreToolStripMenuItem_Click);
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Size = new System.Drawing.Size(112, 22);
            this.exitToolStripMenuItem.Text = "Exit";
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // adminEMailToolStripMenuItem
            // 
            this.adminEMailToolStripMenuItem.Name = "adminEMailToolStripMenuItem";
            this.adminEMailToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
            this.adminEMailToolStripMenuItem.Text = "Admin E-Mail";
            this.adminEMailToolStripMenuItem.Click += new System.EventHandler(this.adminEMailToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(873, 402);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Form1";
            this.ShowInTaskbar = false;
            this.Text = "DeusEx Server Lister v3.3 beta";
            this.Shown += new System.EventHandler(this.Form1_Shown);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Resize += new System.EventHandler(this.Form1_Resize);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.contextMenuStrip2.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		private void insert_packet(Form1.server_ s)
		{
			Form1.packet_ _packet_ = new Form1.packet_(this.indata);
			s.in_packets.Add(_packet_);
			if (!s.gotfinal && this.indata.Contains("\\final\\"))
			{
				s.gotfinal = true;
			}
			if (s.gotfinal)
			{
				bool flag = true;
				int num = 0;
				Comparison<Form1.packet_> comparison = new Comparison<Form1.packet_>(Form1.packet_.compare);
				s.in_packets.Sort(comparison);
				foreach (Form1.packet_ inPacket in s.in_packets)
				{
					num++;
					if (inPacket.id == num)
					{
						continue;
					}
					flag = false;
					break;
				}
				if (flag)
				{
					s.indata = "";
					foreach (Form1.packet_ inPacket1 in s.in_packets)
					{
						Form1.server_ _server_ = s;
						_server_.indata = string.Concat(_server_.indata, inPacket1.data);
					}
				}
			}
		}

		private void joinToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.listView1.SelectedItems.Count == 1)
			{
				this.run_game((Form1.server_)this.listView1.SelectedItems[0].Tag);
			}
		}

		private void listView1_MouseClick(object sender, MouseEventArgs e)
		{
			if (e.Button == System.Windows.Forms.MouseButtons.Left && this.listView1.SelectedItems.Count == 1)
			{
				if (e.X <= this.listView1.Columns[0].Width)
				{
					return;
				}
				this.refreshToolStripMenuItem1_Click(sender, e);
			}
		}

		private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			if (this.listView1.SelectedItems.Count == 1)
			{
				this.run_game((Form1.server_)this.listView1.SelectedItems[0].Tag);
			}
		}

		private void mscb(string text)
		{
			this.toolStripStatusLabel1.Text = text;
			Application.DoEvents();
		}

		private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			base.Activate();
			base.BringToFront();
			base.Show();
			base.WindowState = this.last_state;
		}

		private void open_settings(int tab)
		{
            bool startupon = CheckRegStartup();
			Form2 form2 = new Form2(this.gamepath, this.masterservers, ref this.wds, tab, bAutoRefresh, 
                startupon, bAutoMinimize, gameparams);
			if (form2.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				this.gamepath = form2.gamepath;
				this.masterservers = form2.masterservers;
				this.wds_timer.Interval = this.wds.checkinterval * 1000;
                this.bAutoRefresh = form2.bAutoRefresh;
                this.bAutoMinimize = form2.bStartMinimized;
                this.gameparams = form2.gameparams;
                if (startupon != form2.bAutoStart)
                {
                    if (form2.bAutoStart)
                        AddRegStartup();
                    else
                        RemoveRegStartup();
                }

                SaveSettings();
			}
		}

		private void parse_packet(IPEndPoint from, DateTime recvtime)
		{
			foreach (Form1.server_ _server_ in this.slist)
			{
				if (!(_server_.address.Address.ToString() == from.Address.ToString()) || _server_.address.Port != from.Port)
				{
					continue;
				}
				this.update_server(_server_, recvtime);
				return;
			}
		}

		private void refresh_server(Form1.server_ existing)
		{
			if (this.listView1.SelectedItems.Count == 1 && this.listView1.SelectedItems[0].Tag == existing)
			{
				this.listView2.Items.Clear();
			}
			existing.reset();
			existing.rsent = DateTime.Now;
			this.uc.Client.SendTo(this.status, existing.address);
		}

		private void refreshToolStripMenuItem_Click(object sender, EventArgs e)
		{
            refreshToolStripMenuItem.Enabled = false;
			this.slist.Clear();
			this.watchlist.Clear();
			this.listView1.Items.Clear();
            this.listView2.Items.Clear();
			this.total_servers = 0;
			this.responding_servers = 0;
			this.total_players = 0;
			List<IPEndPoint> pEndPoints = new List<IPEndPoint>();
			foreach (string masterserver in this.masterservers)
			{
				char[] chrArray = new char[] { ':' };
				string[] strArrays = masterserver.Split(chrArray);
				if ((int)strArrays.Length != 2)
				{
					continue;
				}
				int num = 0;
				try
				{
					num = int.Parse(strArrays[1]);
				}
				catch
				{
				}
                try
                {
                    ms m = new ms(strArrays[0], num, new mscallback(this.mscb), this);
                    foreach (IPEndPoint addr in m.addrs)
                    {
                        bool flag = true;
                        foreach (IPEndPoint pEndPoint in pEndPoints)
                        {
                            if (!(pEndPoint.Address.ToString() == addr.Address.ToString()) || pEndPoint.Port != addr.Port)
                            {
                                continue;
                            }
                            flag = false;
                            break;
                        }
                        if (!flag)
                        {
                            continue;
                        }
                        pEndPoints.Add(addr);
                    }
                }
                catch { }
			}
			this.mscb("Querying servers...");
			foreach (IPEndPoint pEndPoint1 in pEndPoints)
			{
				Form1.server_ _server_ = new Form1.server_(pEndPoint1);
				this.slist.Add(_server_);
				_server_.rsent = DateTime.Now;
				this.uc.Client.SendTo(this.status, pEndPoint1);
			}
			this.total_servers = pEndPoints.Count;
			this.after_refresh = new Timer();
			this.after_refresh.Interval = 2000;
			this.after_refresh.Tick += new EventHandler(this.after_refresh_Tick);
			this.after_refresh.Start();
            refreshToolStripMenuItem.Enabled = true;
		}

		private void refreshToolStripMenuItem1_Click(object sender, EventArgs e)
		{
			if (this.listView1.SelectedItems.Count == 1)
			{
				this.refresh_server((Form1.server_)this.listView1.SelectedItems[0].Tag);
			}
		}

		private void removeToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if (this.listView1.SelectedItems.Count == 1)
			{
				Form1.server_ tag = (Form1.server_)this.listView1.SelectedItems[0].Tag;
				if (this.watchlist.Contains(tag))
				{
					this.watchlist.Remove(tag);
					MessageBox.Show(string.Concat("Server:\r\n\r\n", tag.props.hostname, "\r\n\r\nremoved from watchlist!"), "Information", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
					return;
				}
				MessageBox.Show(string.Concat("Server:\r\n\r\n", tag.props.hostname, "\r\n\r\nwas not in watchlist!"), "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
			}
		}

		private void restoreToolStripMenuItem_Click(object sender, EventArgs e)
		{
			base.BringToFront();
			base.Show();
			base.WindowState = this.last_state;
		}

		private void run_game(Form1.server_ s)
		{
			if (this.gamepath == "")
			{
				MessageBox.Show("DeusEx.exe path not set!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Hand);
				return;
			}
			string str = this.gamepath;
            string hport = s.props.hostport;
            if (hport == null) hport = (s.address.Port - 1).ToString();
			string[] strArrays = new string[] { "deusex://", s.address.Address.ToString(), ":", hport, " -hax0r ", gameparams};
			ProcessStartInfo processStartInfo = new ProcessStartInfo(str, string.Concat(strArrays));
			Process.Start(processStartInfo);
		}

		private static void SetDoubleBuffered(Control control)
		{
			Type type = typeof(Control);
			object[] objArray = new object[] { true };
			type.InvokeMember("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.SetProperty, null, control, objArray);
		}

		private void settingsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.open_settings(0);
		}

		private void start_async()
		{
			for (int i = 0; i < (int)this.bindata.Length; i++)
			{
				this.bindata[i] = 0;
			}
			this.uc.Client.BeginReceiveFrom(this.bindata, 0, 1500, SocketFlags.None, ref this.ep, new AsyncCallback(this.incb), null);
		}

		private void update_server(Form1.server_ s, DateTime recvtime)
		{
			this.insert_packet(s);
			if (s.lvi == null)
			{
				s.lvi = this.listView1.Items.Add("", 0);
				s.lvi.Tag = s;
				for (int i = 0; i < 6; i++)
				{
					s.lvi.SubItems.Add("");
				}
			}
			if (!s.pingset)
			{
				s.ping = recvtime - s.rsent;
				s.pingset = true;
			}
			s.props.fill(this.indata);
			s.lvi.ImageIndex = 0;
			s.lvi.SubItems[1].Text = string.Concat(s.address.Address.ToString(), ":");
			if (s.props.hostport != null)
			{
				ListViewItem.ListViewSubItem item = s.lvi.SubItems[1];
				item.Text = string.Concat(item.Text, s.props.hostport);
			}
			if (s.props.hostname != null)
			{
				s.lvi.SubItems[2].Text = s.props.hostname;
			}
			if (s.props.gametype != null)
			{
				s.lvi.SubItems[3].Text = s.props.gametype;
			}
			if (s.props.mapname != null)
			{
				s.lvi.SubItems[4].Text = s.props.mapname;
			}
			if (s.props.maxplayers != null && s.props.numplayers != null)
			{
				s.lvi.SubItems[5].Text = string.Concat(s.props.numplayers, "/", s.props.maxplayers);
				int num = 0;
				try
				{
					num = int.Parse(s.props.numplayers);
				}
				catch
				{
				}
				if (num >= this.wds.numplayers && this.watchlist.Contains(s))
				{
					this.wd_notify(s);
				}
			}
			ListViewItem.ListViewSubItem str = s.lvi.SubItems[6];
			int totalMilliseconds = (int)s.ping.TotalMilliseconds;
			str.Text = totalMilliseconds.ToString();
			s.lvi.ToolTipText = "";
			if (s.props.AdminName != null)
			{
				ListViewItem listViewItem = s.lvi;
				listViewItem.ToolTipText = string.Concat(listViewItem.ToolTipText, "AdminName:\t", s.props.AdminName, "\r\n");
			}
			if (s.props.AdminEMail != null)
			{
				ListViewItem listViewItem1 = s.lvi;
				listViewItem1.ToolTipText = string.Concat(listViewItem1.ToolTipText, "AdminEMail:\t", s.props.AdminEMail, "\r\n");
			}
			if (s.props.gamever != null)
			{
				ListViewItem listViewItem2 = s.lvi;
				listViewItem2.ToolTipText = string.Concat(listViewItem2.ToolTipText, "gamever:\t", s.props.gamever, "\r\n");
			}
			if (s.props.password != null)
			{
				ListViewItem listViewItem3 = s.lvi;
				listViewItem3.ToolTipText = string.Concat(listViewItem3.ToolTipText, "password:\t", s.props.password, "\r\n");
			}
			if (s.props.TimeToWin != null)
			{
				ListViewItem listViewItem4 = s.lvi;
				listViewItem4.ToolTipText = string.Concat(listViewItem4.ToolTipText, "TimeToWin:\t", s.props.TimeToWin, "\r\n");
			}
			if (s.props.KillsToWin != null)
			{
				ListViewItem listViewItem5 = s.lvi;
				listViewItem5.ToolTipText = string.Concat(listViewItem5.ToolTipText, "KillsToWin:\t", s.props.KillsToWin, "\r\n");
			}
			if (s.props.InitialAugs != null)
			{
				ListViewItem listViewItem6 = s.lvi;
				listViewItem6.ToolTipText = string.Concat(listViewItem6.ToolTipText, "InitialAugs:\t", s.props.InitialAugs, "\r\n");
			}
			if (s.props.AugsPerKill != null)
			{
				ListViewItem listViewItem7 = s.lvi;
				listViewItem7.ToolTipText = string.Concat(listViewItem7.ToolTipText, "AugsPerKill:\t", s.props.AugsPerKill, "\r\n");
			}
			if (s.props.SkillsAvail != null)
			{
				ListViewItem listViewItem8 = s.lvi;
				listViewItem8.ToolTipText = string.Concat(listViewItem8.ToolTipText, "SkillsAvail:\t", s.props.SkillsAvail, "\r\n");
			}
			if (s.props.SkillsPerKill != null)
			{
				ListViewItem listViewItem9 = s.lvi;
				listViewItem9.ToolTipText = string.Concat(listViewItem9.ToolTipText, "SkillsPerKill:\t", s.props.SkillsPerKill, "\r\n");
			}
			if (s.indata != null)
			{
				while (true)
				{
					Form1.player_ _player_ = new Form1.player_();
					s.indata = _player_.fill_first(s.indata);
					if (s.indata == null)
					{
						break;
					}
					s.players.Add(_player_);
				}
				if (this.listView1.SelectedItems.Count == 1 && this.listView1.SelectedItems[0].Tag == s)
				{
					foreach (Form1.player_ player in s.players)
					{
						ListViewItem listViewItem10 = this.listView2.Items.Add(player.name);
						listViewItem10.SubItems.Add(player.frags);
						listViewItem10.SubItems.Add(player.ping);
					}
				}
			}
			this.responding_servers = this.get_num_responded_servers();
			this.total_players = this.get_num_total_players();
			ToolStripStatusLabel toolStripStatusLabel = this.toolStripStatusLabel1;
			string[] strArrays = new string[] { "Total servers: ", this.total_servers.ToString(), "   Responding servers: ", this.responding_servers.ToString(), "   Total players: ", this.total_players.ToString() };
			toolStripStatusLabel.Text = string.Concat(strArrays);
		}

		private void wd_notify(Form1.server_ s)
		{
			this.notifyIcon1.ShowBalloonTip(30000, s.props.hostname, string.Concat("Has ", s.props.numplayers, " players now."), ToolTipIcon.Info);
			if (this.wds.beep)
			{
				this.splayer.Play();
			}
			if (this.wds.focus)
			{
				base.Activate();
				base.BringToFront();
				base.Show();
				base.WindowState = this.last_state;
			}
			if (this.wds.@remove)
			{
				this.watchlist.Remove(s);
			}
			if (this.wds.@join)
			{
				this.watchlist.Clear();
				this.run_game(s);
			}
		}

		private void wds_timer_Tick(object sender, EventArgs e)
		{
			foreach (Form1.server_ _server_ in this.watchlist)
			{
				this.refresh_server(_server_);
			}
		}

		private delegate void dincbsec(IAsyncResult iar, DateTime time);

		public class packet_
		{
			public string data;

			public int id;

			public packet_(string indata)
			{
				this.data = string.Copy(indata);
				this.id = 0;
				int num = indata.IndexOf("\\queryid\\");
				if (num >= 0)
				{
					indata = indata.Substring(num);
					num = indata.IndexOf('.');
					int num1 = indata.Substring(num).IndexOf('\\');
					num1 = (num1 != -1 ? num1 - 1 : indata.Length - num - 1);
					string str = indata.Substring(num + 1, num1);
					try
					{
						this.id = int.Parse(str);
					}
					catch
					{
					}
				}
			}

			public static int compare(Form1.packet_ p1, Form1.packet_ p2)
			{
				if (p1.id > p2.id)
				{
					return 1;
				}
				if (p1.id < p2.id)
				{
					return -1;
				}
				return 0;
			}
		}

		public class player_
		{
			public string id;

			public string name;

			public string frags;

			public string ping;

			public player_()
			{
			}

			public string fill_first(string data)
			{
				int num = data.IndexOf("\\player_");
				if (num < 0)
				{
					return null;
				}
				num = num + 8;
				data = data.Substring(num);
				num = data.IndexOf('\\');
				this.id = data.Substring(0, num);
				data = data.Substring(num + 1);
				this.name = data.Substring(0, data.IndexOf('\\'));
				this.frags = this.get_value(data, string.Concat("\\frags_", this.id, "\\"));
				if (this.frags.Contains<char>('.'))
				{
					string str = this.frags;
					char[] chrArray = new char[] { '.' };
					this.frags = str.Split(chrArray)[0];
				}
				this.ping = this.get_value(data, string.Concat("\\ping_", this.id, "\\"));
				return data;
			}

			private string get_value(string data, string option)
			{
				int length = data.IndexOf(option);
				if (length < 0)
				{
					return null;
				}
				length = length + option.Length;
				string str = data.Substring(length);
				return str.Substring(0, str.IndexOf('\\'));
			}
		}

		public class properties_
		{
			public string hostname;

			public string hostport;

			public string mapname;

			public string gametype;

			public string numplayers;

			public string maxplayers;

			public string gamever;

			public string password;

			public string TimeToWin;

			public string KillsToWin;

			public string AdminEMail;

			public string AdminName;

			public string SkillsAvail;

			public string SkillsPerKill;

			public string InitialAugs;

			public string AugsPerKill;

			public string mutators;

			public properties_()
			{
			}

			public void fill(string data)
			{
				string _value = this.get_value(data, "\\hostname\\");
				if (_value != null)
				{
					this.hostname = _value;
				}
				_value = this.get_value(data, "\\hostport\\");
				if (_value != null)
				{
					this.hostport = _value;
				}
				_value = this.get_value(data, "\\mapname\\");
				if (_value != null)
				{
					this.mapname = _value;
				}
				_value = this.get_value(data, "\\gametype\\");
				if (_value != null)
				{
					this.gametype = _value;
				}
				_value = this.get_value(data, "\\numplayers\\");
				if (_value != null)
				{
					this.numplayers = _value;
				}
				_value = this.get_value(data, "\\maxplayers\\");
				if (_value != null)
				{
					this.maxplayers = _value;
				}
				_value = this.get_value(data, "\\gamever\\");
				if (_value != null)
				{
					this.gamever = _value;
				}
				_value = this.get_value(data, "\\password\\");
				if (_value != null)
				{
					this.password = _value;
				}
				_value = this.get_value(data, "\\TimeToWin\\");
				if (_value != null)
				{
					this.TimeToWin = _value;
				}
				_value = this.get_value(data, "\\KillsToWin\\");
				if (_value != null)
				{
					this.KillsToWin = _value;
				}
				_value = this.get_value(data, "\\AdminEMail\\");
				if (_value != null)
				{
					this.AdminEMail = _value;
				}
				_value = this.get_value(data, "\\AdminName\\");
				if (_value != null)
				{
					this.AdminName = _value;
				}
				_value = this.get_value(data, "\\SkillsAvail\\");
				if (_value != null)
				{
					this.SkillsAvail = _value;
				}
				_value = this.get_value(data, "\\SkillsPerKill\\");
				if (_value != null)
				{
					this.SkillsPerKill = _value;
				}
				_value = this.get_value(data, "\\InitialAugs\\");
				if (_value != null)
				{
					this.InitialAugs = _value;
				}
				_value = this.get_value(data, "\\AugsPerKill\\");
				if (_value != null)
				{
					this.AugsPerKill = _value;
				}
				_value = this.get_value(data, "\\mutators\\");
				if (_value != null)
				{
					this.mutators = _value;
				}
			}

			private string get_value(string data, string option)
			{
				string str;
				int length = data.IndexOf(option);
				if (length < 0)
				{
					return null;
				}
				length = length + option.Length;
				try
				{
					string str1 = data.Substring(length);
					str = str1.Substring(0, str1.IndexOf('\\'));
				}
				catch
				{
					str = null;
				}
				return str;
			}
		}

		public class server_
		{
			public IPEndPoint address;

			public Form1.properties_ props;

			public ListViewItem lvi;

			public TimeSpan ping;

			public List<Form1.packet_> in_packets;

			public string indata;

			public bool gotfinal;

			public bool pingset;

			public List<Form1.player_> players;

			public DateTime rsent;

			public server_(IPEndPoint addr)
			{
				this.address = new IPEndPoint(addr.Address, addr.Port);
				this.lvi = null;
				this.reset();
			}

			public void reset()
			{
				this.props = new Form1.properties_();
				this.in_packets = new List<Form1.packet_>();
				this.indata = null;
				this.gotfinal = false;
				this.pingset = false;
				this.players = new List<Form1.player_>();
			}
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            if (bAutoRefresh)
            {
                refreshToolStripMenuItem_Click(sender, e);
            }
        }

        static public void AddRegStartup()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(RUN_LOCATION);
                key.SetValue("DeusEx Server Lister", Assembly.GetExecutingAssembly().Location);
            }
            catch { }
        }

        static public void RemoveRegStartup()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(RUN_LOCATION);
                key.DeleteValue("DeusEx Server Lister");
            }
            catch { }
        }

        static public bool CheckRegStartup()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(RUN_LOCATION);
                if (Assembly.GetExecutingAssembly().Location.ToLower() == 
                    ((string)key.GetValue("DeusEx Server Lister")).ToLower())
                    return true;
            }
            catch
            {
                return false;
            }
            return true;
        }

        private void listView1_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Determine if clicked column is already the column that is being sorted.
            if (e.Column == lvwColumnSorter.SortColumn)
            {
                // Reverse the current sort direction for this column.
                if (lvwColumnSorter.Order == SortOrder.Ascending)
                {
                    lvwColumnSorter.Order = SortOrder.Descending;
                }
                else
                {
                    lvwColumnSorter.Order = SortOrder.Ascending;
                }
            }
            else
            {
                // Set the column number that is to be sorted; default to ascending.
                lvwColumnSorter.SortColumn = e.Column;
                lvwColumnSorter.Order = SortOrder.Ascending;
            }

            // Perform the sort with these new sort options.
            this.listView1.Sort();
        }

        private void addressToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count == 1)
			{
                Clipboard.SetText(((Form1.server_)this.listView1.SelectedItems[0].Tag).address.Address.ToString());
			}
        }

        private void addressPortToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count == 1)
            {
                Clipboard.SetText(this.listView1.SelectedItems[0].SubItems[1].Text);
            }
        }

        private void serverInfoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count == 1)
            {
                Clipboard.SetText(listView1.SelectedItems[0].SubItems[1].Text + " | " +
                    listView1.SelectedItems[0].SubItems[2].Text + " | " +
                    listView1.SelectedItems[0].SubItems[5].Text + " | " +
                    listView1.SelectedItems[0].SubItems[6].Text + "ms");
            }
        }

        private void viewOnGameTrackerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count == 1)
            {
                Process.Start("http://www.gametracker.com/server_info/" + listView1.SelectedItems[0].SubItems[1].Text + "/");
            }
        }

        private void chatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (f4 == null && f3 == null)
            {
                f3 = new Form3(this);
                f3.Show();
                return;
            }

            if (f3 != null)
            {
                f3.BringToFront();
                return;
            }

            if (f4 != null)
            {
                f4.BringToFront();
            }
        }

        private void forDXSLChatToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (f4 != null)
            {
                server_ s = (server_)listView1.SelectedItems[0].Tag;
                string hport = s.props.hostport;
                if (hport == null) hport = (s.address.Port - 1).ToString();
                
                string[] strArrays = new string[] { "deusex://", s.address.Address.ToString(), ":", hport };
                
                f4.CopyDXSRVLink(string.Concat(strArrays));

                f4.BringToFront();
            }
        }

        private void adminEMailToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.listView1.SelectedItems.Count == 1)
            {
                Clipboard.SetText(((Form1.server_)this.listView1.SelectedItems[0].Tag).props.AdminEMail);
            }
        }
	}
}