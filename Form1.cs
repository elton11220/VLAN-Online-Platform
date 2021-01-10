using MetroFramework.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using MetroFramework;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Principal;

namespace VLAN_Online_Platform
{
    public partial class Form1 : MetroForm
    {
        #region 外部DLL调用
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, int nSize, string lpFileName);
        [DllImport("user32.dll", EntryPoint = "SetWindowText")]
        public static extern int SetWindowText(IntPtr hwnd,string lpString);
        [DllImport("kernel32")]
        private static extern int WritePrivateProfileString(string lpApplicationName, string lpKeyName, string lpString, string lpFileName);
        [DllImport("user32", EntryPoint = "HideCaret")]
        private static extern bool HideCaret(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("User32.Dll", EntryPoint = "PostMessageA")]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, int wParam, int lParam);
        //ShowWindow参数
        private const int SW_SHOWNORMAL = 1;
        private const int SW_RESTORE = 9;
        private const int SW_SHOWNOACTIVATE = 4;
        private const int SW_MINIMIZE = 6;
        //SendMessage参数
        private const int WM_KEYDOWN = 0X0100;
        private const int WM_KEYUP = 0X0101;
        private const int WM_CHAR = 0X102;
        private const int WM_CLOSE = 0x0010;
        #endregion

        #region 变量常量
        //
        private static string sys_path = System.Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        private static string me_path = System.IO.Directory.GetCurrentDirectory(); //程序路径
        private static string data_path = System.IO.Directory.GetCurrentDirectory() + "\\data\\config.dat"; //数据路径
        private static string log_path = System.IO.Directory.GetCurrentDirectory() + "\\data\\log\\";  //日志路径
        //
        private const string serverLink = "https://lanplay-1302938064.cos-website.ap-beijing.myqcloud.com/";
        private const string configLink = "https://lanplay-1302938064.cos-website.ap-beijing.myqcloud.com/config.html";
        private const string serverListLink = "https://lanplay-1302938064.cos.ap-beijing.myqcloud.com/serverList.html";
        //
        private const string activityLink = "https://lanplay-1302938064.cos-website.ap-beijing.myqcloud.com/activity.html";
        private static string activityTitleLink = null;
        private string[] activityDate = new string[5]; //YYYY MM DD HH mm
        private static string activityServer = null;
        private static string activityTitle = null;
        private static Boolean IfInActivity = false;
        private static Boolean IfHaveUpdate = false;
        //
        private static double LocalVersion; //从程序读取到的版本号 x.x
        private static int ServerItems = 0; //已保存服务器数量
        private static IntPtr hwnd; //LanPlay窗口句柄
        private string ConnectedIP = null;
        //settings
        private static string settingIfShowUpdateContent = null;
        private static string settingIfCheckActivity = null;
        private static string settingIfHideLanPlay = null;
        private static string settingIfFirstCheckServerList = null;
        private static Boolean ifApplicationDead = true;
        //
        string[] args = null;
        //
        #endregion

        public Form1(string[] args)
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            this.args = args;
            this.Width = 840;
            this.Height = 520;
        }

        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void notifyIcon1_MouseClick(object sender, MouseEventArgs e)
        {
            try
            {
                if(e.Button == MouseButtons.Left)
                {
                    if (this.WindowState == FormWindowState.Minimized)//当程序是最小化的状态时显示程序页面
                    {
                        this.WindowState = FormWindowState.Normal;
                    }
                    this.Activate();
                    this.Visible = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("错误：" + ex.Message);
            }
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                try
                {
                    ShowWindow(hwnd, 0);
                }
                catch
                {
                    //
                }
            }
            if (this.WindowState == FormWindowState.Normal)
            {
                this.ShowInTaskbar = true;
                try
                {
                    if(!IfInActivity)
                    {
                        if(settingIfHideLanPlay != "true")
                            ShowWindow(hwnd, 6);
                    }
                }
                catch
                {
                    //
                }
            }
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            string MeVersion = Application.ProductVersion.ToString();
            MeVersion = MeVersion.Substring(0, 3);
            LocalVersion = Convert.ToDouble(MeVersion); //加载本地版本号
            metroLabel1.Text = metroLabel1.Text + " | Ver" + MeVersion;
            ApplicationDead();
            if(!ifApplicationDead)
            {
                if (CheckRuntime() == true) //检查运行环境是否安装
                {
                    LoadSettings();
                    if (settingIfFirstCheckServerList == "true")
                        FirstCheckServerList();
                    else
                    {
                        LoadSavedServer();
                        GetAllServerInfoEx();
                    }
                    CheckUpdateSilentEx();
                    //if (settingIfCheckActivity == "true")
                        //CheckActivityEx();
                    LanPlayInjectFromArgs();
                    ShowUpdateLog();
                }
                else
                {
                    FixRuntime();
                }
            }
        }

        private static Boolean IfHaveFile(string file_address)
        {
            string MyfileNname = file_address;
            if (MyfileNname.Length < 1)
                return false;
            string ShortName = MyfileNname.Substring(MyfileNname.LastIndexOf("\\") + 1);
            if (File.Exists(MyfileNname))
            {
                return true;
            }
            else
            {
                return false;
            }
        } //检测文件是否存在

        private void FixRuntime() //修复运行环境函数
        {
            if (IfHaveFile(me_path + "\\data\\winpcap413.exe"))
            {
                if (MessageBox.Show(this, "检查到您的运行环境缺失\r\n点击确定开始安装WinPcap\r\n\r\n安装过程依次点击Next-I Agree-Install-Finish\r\n\r\n安装后请重新打开程序", "错误", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.OK)
                {
                    System.Diagnostics.Process.Start(me_path + "\\data\\winpcap413.exe");
                    Application.Exit();
                }
                else
                {
                    MessageBox.Show(this, "由于必要运行环境缺失，程序将退出", "错误");
                    Application.Exit();
                }
            }
            else
            {
                MessageBox.Show(this, "无法完成修复，应用程序数据可能被破坏\r\n请手动安装WinPcap", "错误");
                PrintLog("无法完成修复，可能缺少/data/winpcap413.exe");
                Application.Exit();
            }
        }

        private Boolean CheckRuntime() //检查运行环境
        {
            if (IfHaveFile(sys_path + "\\System32\\wpcap.dll"))
            {
                PrintLog("运行环境检查成功");
                return true;
            }
            else
            {
                PrintLog("运行环境缺失");
                return false;
            }
        }

        private static string ReadINI(string section, string key, string def, string filePath)
        {
            StringBuilder sb = new StringBuilder(1024);
            GetPrivateProfileString(section, key, def, sb, 1024, filePath);
            return sb.ToString();
        }

        private void WriteINI(string section, string key, string value, string filePath)
        {
            if (Form1.IfHaveFile(filePath))
            {
                WritePrivateProfileString(section, key, value, filePath);
            }
        }

        private string Execute(string command) //执行命令行
        {
            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;    //是否使用操作系统shell启动
            p.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
            p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
            p.StartInfo.RedirectStandardError = true;//重定向标准错误输出
            p.StartInfo.CreateNoWindow = true;//不显示程序窗口
            p.Start();
            p.StandardInput.WriteLine(command + "&exit");
            p.StandardInput.AutoFlush = true;
            string output = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            p.Close();
            return output;
        }
        
        private void LoadSavedServer()
        {
            metroLabel9.Text = "";
            if (IfHaveFile(data_path))
            {
                for (ServerItems = 0; ServerItems <= 14; ServerItems++)
                {
                    if(ReadINI("servers",Convert.ToString(ServerItems),"",data_path) == "")
                    {
                        ServerItems++;
                        continue;
                    }
                     listView1.Items.Add(ReadINI("servers",Convert.ToString(ServerItems),"",data_path)); //服务器地址加载
                     listView1.Items[ServerItems].SubItems.Add(""); //服务器状态加载
                     listView1.Items[ServerItems].SubItems.Add(""); //服务器人数
                     listView1.Items[ServerItems].SubItems.Add(""); //服务器版本
                     listView1.Items[ServerItems].SubItems.Add(""); //服务器延迟
                     listView1.Items[ServerItems].SubItems.Add(ReadINI("remarks", Convert.ToString(ServerItems), "", data_path)); //备注 ANSI Code Only
                }
                PrintLog("成功读取" + Convert.ToString(listView1.Items.Count) + "条记录");
            }
            else
            {
                MessageBox.Show(this,"程序数据遭到破坏，请重新安装", "错误");
                PrintLog("程序数据异常:缺少/data/config.dat(编码ANSI)");
                Application.Exit();
            }
        }//读取保存的服务器配置文件

        private void LoadSettings()
        {
            if (IfHaveFile(data_path))
            {
                settingIfShowUpdateContent = ReadINI("settings", "ifShowUpdateLog", "", data_path);
                settingIfCheckActivity = ReadINI("settings", "ifCheckActivity", "", data_path);
                settingIfHideLanPlay = ReadINI("settings", "ifHideLanPlay", "", data_path);
                settingIfFirstCheckServerList = ReadINI("settings", "ifFirstCheckServerList", "", data_path);
                //
                /*
                if (settingIfCheckActivity == "true")
                    metroCheckBox1.Checked = true;
                else
                    metroCheckBox1.Checked = false;
                */
                //
                if (settingIfHideLanPlay == "true")
                    metroCheckBox2.Checked = true;
                else
                    metroCheckBox2.Checked = false;
                //
            }
            else
            {
                MessageBox.Show(this, "程序数据遭到破坏，请重新安装", "错误");
                PrintLog("程序数据异常:缺少/data/config.dat(编码ANSI)");
                Application.Exit();
            }
        }

        private static string GetServerJson(string urladdress)
        {
            try
            {
                WebClient MyWebClient = new WebClient();
                MyWebClient.Credentials = CredentialCache.DefaultCredentials;
                Byte[] pageData = MyWebClient.DownloadData(urladdress);
                //string pageHtml = Encoding.Default.GetString(pageData);  //GB2312 Page           
                string pageHtml = Encoding.UTF8.GetString(pageData); //UTF-8 Page
                return pageHtml;
            }
            catch
            {
                return "Error";
            }
        }

        private static string FormatUrl(string Input,int mode) //mode1:http://xxx.xxx.xxx.xxx:port  mode2:xxx.xxx.xxx.xxx:port  mode3:xxx.xxx.xxx.xxx  Default:Error
        {
            switch(mode)
            {
                case 1 : 
                        if (Input.Contains("ttp:"))
                        {
                            return Input;
                        }
                        else
                        {
                            Input = "http://" + Input;
                            return Input;
                        }
                case 2:
                    if (Input.Contains("http://"))
                    {
                        Input = Input.Substring(7);
                        return Input;
                    }
                    else
                    {
                        if (Input.Contains("https://"))
                        {
                            Input = Input.Substring(8);
                            return Input;
                        }
                        else
                        {
                            return Input;
                        }
                    }
                case 3:
                    string str = Input;
                    string cache = "";
                    char c = ':';
                    if (str.Contains("/info"))
                    {
                        cache = str.Substring(0, str.Length - 5);
                    }
                    else
                    {
                        cache = str;
                    }
                    if (cache.Contains("http://"))
                    {
                        cache = str.Substring(7);
                    }
                    else
                    {
                        if (str.Contains("https://"))
                        {
                            cache = str.Substring(8);
                        }
                        else
                        {
                            cache = str;
                        }
                    }
                    if (cache.Contains(":"))
                    {
                        cache = cache.Substring(0, cache.IndexOf(c));
                    }
                    return cache;
                default:
                    return "Error:wrong mode";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int Itemindex;
            if (listView1.SelectedItems.Count > 0 && listView1.SelectedItems[0].Index >= 0)
            {
                Itemindex = listView1.SelectedItems[0].Index;
                PrintLog("删除了服务器" + listView1.Items[Itemindex].Text);
                listView1.Items[Itemindex].Remove();
                SaveServer();
            }
            else
            {
                MessageBox.Show("请选择一个服务器","警告");
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(listView1.Items.Count < 15)
            {
                groupBox1.Visible = true;
                metroTextBox1.TabStop = true;
                metroTextBox2.TabStop = true;
                metroButton2.TabStop = true;
                metroButton3.TabStop = true;
                metroButton5.Enabled = false;
                metroButton9.Enabled = false;
                button2.Enabled = false;
                button1.Enabled = false;
                metroButton6.Enabled = false;
            }
            else
            {
                MessageBox.Show("只能保存15条记录，请删除部分服务器","警告");
            }
        }

        private void metroButton3_Click(object sender, EventArgs e)
        {
            metroTextBox1.Text = "";
            metroTextBox1.TabStop = false;
            metroButton2.TabStop = false;
            metroButton3.TabStop = false;
            groupBox1.Visible = false;
            button2.Enabled = true;
            button1.Enabled = true;
            metroButton5.Enabled = true;
            metroButton9.Enabled = true;
            metroButton6.Enabled = true;
        }

        private void metroButton2_Click(object sender, EventArgs e)
        {
            if(metroTextBox1.Text == "")
            {
                MessageBox.Show("服务器地址不能为空", "警告");
                return;
            }
            string AddIp;
            AddIp = FormatUrl(metroTextBox1.Text, 2);
            listView1.Items.Add(AddIp);
            listView1.Items[listView1.Items.Count - 1].SubItems.Add("");
            listView1.Items[listView1.Items.Count - 1].SubItems.Add("");
            listView1.Items[listView1.Items.Count - 1].SubItems.Add("");
            listView1.Items[listView1.Items.Count - 1].SubItems.Add("");
            listView1.Items[listView1.Items.Count - 1].SubItems.Add(metroTextBox2.Text);
            metroLabel3.Text = "正在获取服务器信息";
            SaveServer();
            GetSingleServerInfo(true);
            PrintLog("添加了服务器" + metroTextBox1.Text + " Ping:" + listView1.Items[listView1.Items.Count -1].SubItems[4].Text + "ms");
            //
            metroTextBox1.Text = "";
            metroTextBox2.Text = "";
            metroTextBox1.TabStop = false;
            metroTextBox2.TabStop = false;
            metroButton2.TabStop = false;
            metroButton3.TabStop = false;
            groupBox1.Visible = false;
            button2.Enabled = true;
            button1.Enabled = true;
            metroButton5.Enabled = true;
            metroButton6.Enabled = true;
            metroButton9.Enabled = true;
            metroLabel3.Text = "例：127.0.0.1:11451";
        }

        private void PrintLog(string content)
        {
            textBox1.Text = textBox1.Text + "\r\n[" + DateTime.Now.ToString("hh:mm:ss") + "]" + content;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            textBox1.SelectionStart = textBox1.Text.Length;
            textBox1.ScrollToCaret();
        }

        private void textBox1_MouseEnter(object sender, EventArgs e)
        {
            HideCaret(this.textBox1.Handle);
        }

        private void textBox1_MouseDown(object sender, MouseEventArgs e)
        {
            HideCaret(this.textBox1.Handle);
        }

        private void SaveLog()
        {
            PrintLog("保存日志成功");
            textBox1.Text = textBox1.Text + "\r\n\r\n";
            string filePath = log_path + DateTime.Now.ToString("yyyy-MM-dd") + "-Log.txt";
            try
            {
                if (!File.Exists(filePath))
                {
                    File.Create(filePath).Close();
                }
                File.AppendAllText(filePath, textBox1.Text);
            }
            catch
            {
                //
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                SendMessage(hwnd, WM_CLOSE, 0, 0);
            }
            catch
            {
                //
            }
            notifyIcon1.Visible = false;
            SaveLog();
        }

        private void SaveServer()
        {
            int i;
            for (i=0;i<listView1.Items.Count;i++)
            {
                WriteINI("servers", Convert.ToString(i),listView1.Items[i].SubItems[0].Text,data_path);
                WriteINI("remarks", Convert.ToString(i), listView1.Items[i].SubItems[5].Text, data_path); //ANSI Code Only
            }
            for(i=1;i<=15-listView1.Items.Count;i++)
            {
                WriteINI("servers", Convert.ToString(15 - i), "", data_path);
                WriteINI("remarks", Convert.ToString(15 - i), "", data_path);
            }
        }

        private void GetAllServerInfo()
        {
            if(listView1.Items.Count > 0)
            {
                int i;
                for(i=0;i<listView1.Items.Count;i++)
                {
                    try
                    {
                        string gotServerJson = GetServerJson("http://" + listView1.Items[i].Text + "/info"); //连接服务器初步获取信息
                        JObject obj = JObject.Parse(gotServerJson);
                        int serverOnline2 = (int)obj["online"];
                        string serverVersion = (string)obj["version"];
                        string serverOnline = Convert.ToString(serverOnline2);
                        string serverPing = GetPing(FormatUrl(listView1.Items[i].SubItems[0].Text, 3), 1);
                        if(serverPing != "999")
                        {
                            listView1.Items[i].SubItems[1].Text = "正常";
                            listView1.Items[i].SubItems[2].Text = serverOnline;
                            listView1.Items[i].SubItems[3].Text = serverVersion;
                            listView1.Items[i].SubItems[4].Text = serverPing;
                        }
                        else
                        {
                            listView1.Items[i].SubItems[1].Text = "正常";
                            listView1.Items[i].SubItems[2].Text = serverOnline;
                            listView1.Items[i].SubItems[3].Text = serverVersion;
                            listView1.Items[i].SubItems[4].Text = "-";
                        }
                    }
                    catch
                     {
                        listView1.Items[i].SubItems[1].Text = "错误";
                     }
                }
            }
        }

        private void GetSingleServerInfo(bool IfAutoRefresh)
        {
            if(IfAutoRefresh)
            {
                try
                {
                    string gotServerJson = GetServerJson("http://" + listView1.Items[listView1.Items.Count - 1].SubItems[0].Text + "/info"); //连接服务器初步获取信息
                    JObject obj = JObject.Parse(gotServerJson);
                    int serverOnline2 = (int)obj["online"];
                    string serverVersion = (string)obj["version"];
                    string serverOnline = Convert.ToString(serverOnline2);
                    string serverPing = GetPing(FormatUrl(listView1.Items[listView1.Items.Count - 1].SubItems[0].Text, 3), 1);
                    if (serverPing != "999")
                    {
                        listView1.Items[listView1.Items.Count - 1].SubItems[1].Text = "正常";
                        listView1.Items[listView1.Items.Count - 1].SubItems[2].Text = serverOnline;
                        listView1.Items[listView1.Items.Count - 1].SubItems[3].Text = serverVersion;
                        listView1.Items[listView1.Items.Count - 1].SubItems[4].Text = serverPing;
                    }
                    else
                    {
                        listView1.Items[listView1.Items.Count - 1].SubItems[1].Text = "正常";
                        listView1.Items[listView1.Items.Count - 1].SubItems[2].Text = serverOnline;
                        listView1.Items[listView1.Items.Count - 1].SubItems[3].Text = serverVersion;
                        listView1.Items[listView1.Items.Count - 1].SubItems[4].Text = "-";
                    }
                }
                catch
                {
                    listView1.Items[listView1.Items.Count - 1].SubItems[1].Text = "错误";
                }
            }
            else
            {
                try
                {
                    string gotServerJson = GetServerJson("http://" + listView1.SelectedItems[0].SubItems[0].Text + "/info"); //连接服务器初步获取信息
                    JObject obj = JObject.Parse(gotServerJson);
                    int serverOnline2 = (int)obj["online"];
                    string serverVersion = (string)obj["version"];
                    string serverOnline = Convert.ToString(serverOnline2);
                    string serverPing = GetPing(FormatUrl(listView1.SelectedItems[0].SubItems[0].Text, 3), 1);
                    if (serverPing != "999")
                    {
                        listView1.SelectedItems[0].SubItems[1].Text = "正常";
                        listView1.SelectedItems[0].SubItems[2].Text = serverOnline;
                        listView1.SelectedItems[0].SubItems[3].Text = serverVersion;
                        listView1.SelectedItems[0].SubItems[4].Text = serverPing;
                    }
                    else
                    {
                        listView1.SelectedItems[0].SubItems[1].Text = "禁Ping";
                        listView1.SelectedItems[0].SubItems[2].Text = serverOnline;
                        listView1.SelectedItems[0].SubItems[3].Text = serverVersion;
                        listView1.SelectedItems[0].SubItems[4].Text = "-";
                    }
                }
                catch
                {
                    listView1.SelectedItems[0].SubItems[1].Text = "错误";
                }
            }
        }

        private void GetAllServerInfoEx() 
        {
            ThreadStart thStart = new ThreadStart(GetAllServerInfo);//threadStart委托 
            Thread thread = new Thread(thStart);
            thread.Priority = ThreadPriority.Highest;
            thread.IsBackground = false; //关闭窗体继续执行
            thread.Start();
        }

        public static string GetPing(string URL, int Mode)
        {
            try
            {
                Ping ping = new Ping();
                PingReply reply = ping.Send(URL, 1000);
                if (reply != null)
                {
                    switch (Mode)
                    {
                        case 0:
                            return Convert.ToString(reply.Status); //状态
                        case 1:
                            return reply.RoundtripTime.ToString(); //时间
                        case 2:
                            return Convert.ToString(reply.Address); //地址
                        case 3:
                            return reply.ToString(); //响应
                        default:
                            return "0";
                    }
                }
                else
                {
                    return "0";
                }
            }
            catch
            {
                return "0";
            }
        }

        private void 刷新ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(listView1.SelectedItems.Count >0)
            {
                GetSingleServerInfo(false);
            }
            else
            {
                GetAllServerInfoEx();
            }
        }

        private void 删除ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(listView1.SelectedItems.Count >0)
            {
                int Itemindex;
                Itemindex = listView1.SelectedItems[0].Index;
                PrintLog("删除了服务器" + listView1.Items[Itemindex].Text);
                listView1.Items[Itemindex].Remove();
                SaveServer();
            }
        }

        private void listView1_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                if(listView1.SelectedItems.Count == 0)
                {
                    刷新ToolStripMenuItem.Text = "刷新全部";
                    删除ToolStripMenuItem.Enabled = false;
                }
                else
                {
                    刷新ToolStripMenuItem.Text = "刷新";
                    删除ToolStripMenuItem.Enabled = true;
                }
            }
        }

        private void listView1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if(listView1.SelectedItems.Count > 0)
            {
                if(listView1.SelectedItems[0].SubItems[1].Text == "错误")
                {
                    if(MessageBox.Show("服务器可能关闭了信息接口或服务器已关闭\r\n是否仍要继续连接？","注意",MessageBoxButtons.YesNo,MessageBoxIcon.Question) == DialogResult.No)
                    {
                        return;
                    }
                }
                if (Environment.Is64BitOperatingSystem)
                {
                    if (System.IO.File.Exists(me_path + "\\data\\lan-play-win64.exe"))
                    {
                        System.Diagnostics.Process p = new System.Diagnostics.Process();
                        p.StartInfo.FileName = me_path + "\\data\\lan-play-win64.exe";
                        p.StartInfo.Arguments = "--relay-server-addr " + listView1.SelectedItems[0].SubItems[0].Text;
                        p.StartInfo.UseShellExecute = true;
                        if(settingIfHideLanPlay == "true")
                        {
                            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        }
                        else
                            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
                        p.Start();
                        PrintLog("连接服务器:" + listView1.SelectedItems[0].SubItems[0].Text + " Mode:Win64");
                        ConnectedIP = listView1.SelectedItems[0].SubItems[0].Text;
                        LanPlayInject("win64", listView1.SelectedItems[0].SubItems[0].Text,0);
                    }
                    else
                    {
                        MessageBox.Show("程序数据被破坏，请重新安装", "错误");
                        PrintLog("可能缺少lan-play-win64.exe 程序异常退出");
                        Application.Exit();
                    }
                }
                else
                {
                    if (System.IO.File.Exists(me_path + "\\data\\lan-play-win32.exe"))
                    {
                        System.Diagnostics.Process p = new System.Diagnostics.Process();
                        p.StartInfo.FileName = me_path + "\\data\\lan-play-win32.exe";
                        p.StartInfo.Arguments = "--relay-server-addr " + listView1.SelectedItems[0].SubItems[0].Text;
                        p.StartInfo.UseShellExecute = true;
                        if (settingIfHideLanPlay == "true")
                        {
                            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        }
                        else
                            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
                        p.Start();
                        PrintLog("连接服务器:" + listView1.SelectedItems[0].SubItems[0].Text + " Mode:Win32");
                        ConnectedIP = listView1.SelectedItems[0].SubItems[0].Text;
                        LanPlayInject("win32", listView1.SelectedItems[0].SubItems[0].Text,0);
                    }
                    else
                    {
                        MessageBox.Show("程序数据被破坏，请重新安装", "错误");
                        PrintLog("可能缺少lan-play-win32.exe 程序异常退出");
                        Application.Exit();
                    }
                }
            }
        }

        public static int Asc(string character)
        {
            if (character.Length == 1)
            {
                System.Text.ASCIIEncoding asciiEncoding = new System.Text.ASCIIEncoding();
                int intAsciiCode = (int)asciiEncoding.GetBytes(character)[0];
                return (intAsciiCode);
            }
            else
            {
                throw new Exception("Character is not valid.");
            }

        }

        private void LanPlayInject(string SysType, string Url,int mode) //0 正常模式 1 传参启动模式 2活动模式
        {
            groupBox2.Visible = true;
            metroButton4.Enabled = true;
            button1.Enabled = false;
            button2.Enabled = false;
            metroButton1.Enabled = false;
            metroButton6.Enabled = false;
            metroButton9.Enabled = false;
            //
            Thread.Sleep(300);
            string exe_path = me_path + "\\data\\lan-play-" + SysType + ".exe";
            IntPtr myIntPtr = FindWindow(null, exe_path);
            hwnd = myIntPtr;
            //
            switch(mode)
            {
                case 0:
                    {
                        metroLabel4.Text = "已连接到服务器：" + Url;
                        metroLabel5.Text = "服务器版本：" + listView1.SelectedItems[0].SubItems[3].Text;
                        metroLabel6.Text = "在线人数：" + listView1.SelectedItems[0].SubItems[2].Text;
                        metroLabel7.Text = "Ping:" + GetPing(FormatUrl(ConnectedIP, 3), 1);
                        if (listView1.SelectedItems[0].SubItems[5].Text != "")
                        {
                            notifyIcon1.Text = "VLAN Online Platform\r\n状态：已连接到" + listView1.SelectedItems[0].SubItems[5].Text;
                            metroLabel9.Text = listView1.SelectedItems[0].SubItems[5].Text;
                        }
                        else
                        {
                            notifyIcon1.Text = "VLAN Online Platform\r\n状态：已连接到" + Url;
                        }
                        SetWindowText(hwnd, "LanPlay 后台程序勿动 | 已连接到服务器：" + Url);
                        timer1.Enabled = true;
                        break;
                    }
                case 1:
                    {
                        try
                        {
                            string gotServerJson = GetServerJson("http://" + Url + "/info"); //连接服务器初步获取信息
                            JObject obj = JObject.Parse(gotServerJson);
                            int serverOnline2 = (int)obj["online"];
                            metroLabel5.Text = "服务器版本：" + (string)obj["version"];
                            metroLabel6.Text = "在线人数：" + Convert.ToString(serverOnline2);
                        }
                        catch
                        {
                            metroLabel5.Text = "服务器版本：获取失败";
                            metroLabel6.Text = "在线人数：获取失败";
                        }
                        SetWindowText(hwnd, "LanPlay 后台程序勿动 | 已连接到服务器：" + args[0]);
                        metroLabel4.Text = "已连接到服务器：" + Url;
                        metroLabel7.Text = "Ping:" + GetPing(FormatUrl(ConnectedIP, 3), 1);
                        notifyIcon1.Text = "VLAN Online Platform\r\n状态：已连接到" + Url;
                        timer2.Enabled = true;
                        break;
                    }
                case 2:
                    {
                        try
                        {
                            string gotServerJson = GetServerJson("http://" + Url + "/info"); //连接服务器初步获取信息
                            JObject obj = JObject.Parse(gotServerJson);
                            int serverOnline2 = (int)obj["online"];
                            metroLabel5.Text = "服务器版本：" + (string)obj["version"];
                            metroLabel6.Text = "活动人数：" + Convert.ToString(serverOnline2);
                        }
                        catch
                        {
                            metroLabel5.Text = "服务器版本：获取失败";
                            metroLabel6.Text = "活动人数：获取失败";
                        }
                        SetWindowText(hwnd, "LanPlay 后台程序勿动 | 正在参加活动：" + activityTitle);
                        metroLabel4.TextAlign = ContentAlignment.TopCenter;
                        metroLabel4.Text = activityTitle;
                        metroLabel7.Text = "Ping:" + GetPing(FormatUrl(ConnectedIP, 3), 1);
                        notifyIcon1.Text = "VLAN Online Platform\r\n正在参加活动";
                        timer3.Enabled = true;
                        break;
                    }
            }
        }

        private void LanPlayInjectFromArgs()
        {
            if (args == null)
                return;
            if (!args[0].Contains(".") && !args[0].Contains(":"))
                return;
            else
            {
                if (args[0].Contains("vop://") || args[0].Contains("vop"))
                {
                    args[0] = args[0].Substring(6, args[0].Length - 7);
                }
                if (Environment.Is64BitOperatingSystem)
                {
                    if (System.IO.File.Exists(System.IO.Directory.GetCurrentDirectory() + "\\data\\lan-play-win64.exe"))
                    {
                        System.Diagnostics.Process p = new System.Diagnostics.Process();
                        p.StartInfo.FileName = me_path + "\\data\\lan-play-win64.exe";
                        p.StartInfo.Arguments = "--relay-server-addr " + args[0];
                        p.StartInfo.UseShellExecute = true;
                        if (settingIfHideLanPlay == "true")
                        {
                            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        }
                        else
                            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
                        p.Start();
                        PrintLog("连接服务器:" + args[0] + " Mode:Win64");
                        ConnectedIP = args[0];
                        LanPlayInject("win64", args[0], 1);
                    }
                    else
                    {
                        MessageBox.Show("程序数据被破坏，请重新安装", "错误");
                        PrintLog("可能缺少lan-play-win64.exe 程序异常退出");
                        Application.Exit();
                    }
                }
                else
                {
                    if (System.IO.File.Exists(System.IO.Directory.GetCurrentDirectory() + "\\data\\lan-play-win32.exe"))
                    {
                        System.Diagnostics.Process p = new System.Diagnostics.Process();
                        p.StartInfo.FileName = me_path + "\\data\\lan-play-win32.exe";
                        p.StartInfo.Arguments = "--relay-server-addr " + args[0];
                        p.StartInfo.UseShellExecute = true;
                        if (settingIfHideLanPlay == "true")
                        {
                            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        }
                        else
                            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
                        p.Start();
                        PrintLog("连接服务器:" + args[0] + " Mode:Win32");
                        ConnectedIP = args[0];
                        LanPlayInject("win32", args[0], 1);
                    }
                    else
                    {
                        MessageBox.Show("程序数据被破坏，请重新安装", "错误");
                        PrintLog("可能缺少lan-play-win32.exe 程序异常退出");
                        Application.Exit();
                    }
                }
            }
        }

        private void metroButton4_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("确定要断开与服务器的连接吗？","警告",MessageBoxButtons.OKCancel ,MessageBoxIcon.Question) == DialogResult.OK)
            {
                SendMessage(hwnd, WM_CLOSE, 0, 0);
                groupBox2.Visible = false;
                button1.Enabled = true;
                button2.Enabled = true;
                metroButton5.Enabled = true;
                metroButton6.Enabled = true;
                metroButton1.Enabled = true;
                metroButton9.Enabled = true;
                if (timer1.Enabled == true)
                    timer1.Enabled = false;
                if (timer2.Enabled == true)
                    timer2.Enabled = false;
                if (timer3.Enabled == true)
                    timer3.Enabled = false;
                metroLabel4.TextAlign = ContentAlignment.TopLeft;
                metroLabel9.Text = "";
                notifyIcon1.Text = "VLAN Online Platform\r\n状态：未连接";
                IfInActivity = false;
                PrintLog("与服务器的连接断开");
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            GetSingleServerInfo(false);
            metroLabel6.Text = "在线人数：" + listView1.SelectedItems[0].SubItems[2].Text;
            metroLabel7.Text = "Ping:" + GetPing(FormatUrl(ConnectedIP,3),1);
        }

        private void metroButton1_Click(object sender, EventArgs e)
        {
            CheckUpdate();
        }

        private void 退出ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show("是否要退出程序？\r\n这将断开与服务器的连接","退出",MessageBoxButtons.YesNo,MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void CheckUpdate()
        {
            try
            {
                string statusPage2 = GetServerJson(configLink);
                string statusPage = statusPage2.Substring(statusPage2.IndexOf("<body>") + "<body>".Length, statusPage2.IndexOf("</body>") - (statusPage2.IndexOf("<body>") + "<body>".Length));
                JObject obj = JObject.Parse(statusPage);
                double serverVersion = (double)obj["Version"];
                string updateLink = (string)obj["Link"];
                string updateContent1 = (string)obj["Content"];
                string updateContent = "暂无";
                if (updateContent1 != "")
                {
                    if (updateContent1.Contains("|"))
                    {
                        String[] contents = updateContent1.Split('|');
                        String outText = "";
                        String outText1 = "";
                        int i;
                        for (i = 0; i < contents.Length; i++)
                        {
                            outText1 = Convert.ToString(i + 1) + "." + contents[i];
                            outText = outText + outText1 + "\r\n";
                        }
                        updateContent = outText;
                    }
                    else
                    {
                        updateContent = updateContent1;
                    }
                }
                if (LocalVersion < serverVersion)
                {
                    PrintLog("检测到新版本");
                    IfHaveUpdate = true;
                    if (MessageBox.Show(this,"检测到新版本，是否更新？\r\n\r\n更新内容：\r\n" + updateContent, "检测到新版本", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        System.Diagnostics.Process.Start(updateLink);
                    }
                }
                else
                {
                    MessageBox.Show(this,"当前程序为最新版本，无需更新", "更新");
                    PrintLog("暂无更新");
                }
            }
            catch
            {
                MessageBox.Show(this,"检查更新失败", "失败");
                PrintLog("检查更新失败");
            }
        }

        private void CheckUpdateSilent()
        {
            string originalTitle = metroLabel1.Text;
            Boolean IfEnabled = false;
            int Level = 0;//是否强制更新 0否/其他 是
            try
            {
                string statusPage2 = GetServerJson(configLink);
                string statusPage = statusPage2.Substring(statusPage2.IndexOf("<body>") + "<body>".Length, statusPage2.IndexOf("</body>") - (statusPage2.IndexOf("<body>") + "<body>".Length));
                JObject obj = JObject.Parse(statusPage);
                double serverVersion = (double)obj["Version"];
                string updateLink = (string)obj["Link"];
                string updateContent1 = (string)obj["Content"];
                string updateContent = "暂无";
                string MeVersion = Application.ProductVersion.ToString();
                MeVersion = MeVersion.Substring(0, 3);
                metroLabel1.Text = (string)obj["Title"] + " | Ver" + MeVersion;
                int Enable = (int)obj["Enable"];
                int UpdLevel = (int)obj["UpdLevel"];
                Level = UpdLevel;
                if (Enable == 0)
                {
                    Application.Exit();
                }
                else
                {
                    IfEnabled = true;
                }
                if (updateContent1 != "")
                {
                    if (updateContent1.Contains("|"))
                    {
                        String[] contents = updateContent1.Split('|');
                        String outText = "";
                        String outText1 = "";
                        int i;
                        for (i = 0; i < contents.Length; i++)
                        {
                            outText1 = Convert.ToString(i + 1) + "." + contents[i];
                            outText = outText + outText1 + "\r\n";
                        }
                        updateContent = outText;
                    }
                    else
                    {
                        updateContent = updateContent1;
                    }
                }
                if (LocalVersion < serverVersion)
                {
                    PrintLog("检测到新版本");
                    IfHaveUpdate = true;
                    if (MessageBox.Show(this,"检测到新版本，是否更新？\r\n\r\n更新内容：\r\n" + updateContent, "检测到新版本", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        System.Diagnostics.Process.Start(updateLink);
                    }
                    else
                    {
                        if(Level != 0)
                        {
                            MessageBox.Show(this,"此更新为必要更新，拒绝更新将无法正常运行程序", "警告");
                            PrintLog("此更新为必要更新，不更新将无法正常运行");
                            Application.Exit();
                        }
                    }
                }
                else
                {
                    PrintLog("暂无更新");
                }
            }
            catch
            {
                PrintLog("检查更新失败");
                IfEnabled = false;
                metroLabel1.Text = originalTitle;
            }
            if(Enabled == false)
            {
                PrintLog("Error");
                Application.Exit();
            }
        }

        private void CheckUpdateSilentEx()
        {
            ThreadStart thStart = new ThreadStart(CheckUpdateSilent);//threadStart委托 
            Thread thread = new Thread(thStart);
            thread.Priority = ThreadPriority.Highest;
            thread.IsBackground = false; //关闭窗体继续执行
            thread.Start();
        }

        private void ShowUpdateLog()
        {
            if(IfHaveFile(data_path))
            {
                if(settingIfShowUpdateContent == "true")
                {
                    String content = ReadINI("settings", "updateContent", "", data_path);
                    if(content != "")
                    {
                        if (content.Contains("|"))
                        {
                            String[] contents = content.Split('|');
                            String outText = "";
                            String outText1 = "";
                            int i;
                            for (i = 0; i < contents.Length; i++)
                            {
                                outText1 = Convert.ToString(i + 1) + "." + contents[i];
                                outText = outText + outText1 + "\r\n";
                            }
                            WriteINI("settings", "ifShowUpdateLog", "false", data_path);
                            MessageBox.Show(this, outText, "更新内容");
                        }
                        else
                        {
                            WriteINI("settings", "ifShowUpdateLog", "false", data_path);
                            MessageBox.Show(this, content, "更新内容");
                        }
                    }
                }
            }
        }

        private void ApplicationDead()
        {
            String date = DateTime.Now.ToString("yyyy");
            String date2 = DateTime.Now.ToString("MM");
            int year = Convert.ToInt32(date);
            int month = Convert.ToInt32(date2);
            if (year != 2021)
            {
                PrintLog("当前版本已失效，请下载最新版本");
                MessageBox.Show(this, "当前版本已经失效\n将为您检查更新", "版本失效");
                CheckUpdateSilent();
                Application.Exit();
            }
            else if (year == 2021 && month >= 4)
            {
                PrintLog("当前版本已失效，请下载最新版本");
                MessageBox.Show(this, "当前版本已经失效\n将为您检查更新", "版本失效");
                CheckUpdateSilent();
                Application.Exit();
            }
            else
                ifApplicationDead = false;
        }

        private void timer2_Tick(object sender, EventArgs e)
        {
            try
            {
                string gotServerJson = GetServerJson("http://" + args[0] + "/info"); //连接服务器初步获取信息
                JObject obj = JObject.Parse(gotServerJson);
                int serverOnline2 = (int)obj["online"];
                metroLabel6.Text = "在线人数：" + Convert.ToString(serverOnline2);
            }
            catch
            {
                metroLabel6.Text = "在线人数：获取失败";
            }
            metroLabel7.Text = "Ping:" + GetPing(FormatUrl(ConnectedIP, 3), 1);
        }

        private void CheckActivity()
        {
            try
            {
                string statusPage2 = GetServerJson(activityLink);
                string statusPage = statusPage2.Substring(statusPage2.IndexOf("<body>") + "<body>".Length, statusPage2.IndexOf("</body>") - (statusPage2.IndexOf("<body>") + "<body>".Length));
                JObject obj = JObject.Parse(statusPage);
                string activityEnable = (string)obj["Enabled"];
                activityTitle = (string)obj["Title"];
                activityTitleLink = (string)obj["TitleLink"];
                string activityTime = (string)obj["Time"];
                activityServer = (string)obj["Server"];
                string activityMessage = (string)obj["Message"];
                //
                if (activityEnable == "false" || activityEnable == "False")
                {
                    PrintLog("当前服务器暂无活动");
                    return;
                }
                //
                this.Height = this.metroLink1.Location.Y + this.metroLink1.Height + 15;
                int mbt5Y = pictureBox2.Location.Y + pictureBox2.Height - metroButton5.Height;
                metroButton5.Location = new System.Drawing.Point(metroButton5.Location.X, mbt5Y);
                this.pictureBox2.Image = null;
                this.pictureBox2.WaitOnLoad = false; //设置为异步加载图片
                this.pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
                this.pictureBox2.LoadAsync(serverLink + "activity.png");
                //
                metroLink1.Text = activityTitle + " [点击查看活动详情]";
                metroLabel10.Visible = true;
                //
                activityDate = activityTime.Split('.');
                metroLabel10.Text = "开始时间：" + activityDate[0] + "." + activityDate[1] + "." + activityDate[2]  + " " + activityDate[3] + "." + activityDate[4];
                PrintLog("接收到" + activityDate[0] + "." + activityDate[1] + "." + activityDate[2] + " " + activityDate[3] + ":" + activityDate[4] + "的活动信息 快来参加吧");
                //
                label3.Text = activityMessage;
                if (Convert.ToInt32(DateTime.Now.Year.ToString()) == Convert.ToInt32(activityDate[0]) && Convert.ToInt32(DateTime.Now.Month.ToString()) == Convert.ToInt32(activityDate[1]) && Convert.ToInt32(DateTime.Now.Day.ToString()) == Convert.ToInt32(activityDate[2]) && Convert.ToInt32(DateTime.Now.Hour.ToString()) >= Convert.ToInt32(activityDate[3]) && Convert.ToInt32(DateTime.Now.Minute.ToString()) >= Convert.ToInt32(activityDate[4]))
                {
                    metroButton5.Enabled = true;
                }
            }
            catch
            {
                PrintLog("获取活动信息失败或当前客户端非最新版本");
            }
        }

        private void CheckActivityEx()
        {
            ThreadStart thStart = new ThreadStart(CheckActivity);//threadStart委托 
            Thread thread = new Thread(thStart);
            thread.Priority = ThreadPriority.Highest;
            thread.IsBackground = false; //关闭窗体继续执行
            thread.Start();
        }

        private void metroLink1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(activityTitleLink);
        }

        private void metroButton5_Click(object sender, EventArgs e)
        {
            if (IfHaveUpdate)
            {
                PrintLog("当前客户端非最新版本，无法参与服务器活动");
                return;
            }
            if (Convert.ToInt32(DateTime.Now.Year.ToString()) == Convert.ToInt32(activityDate[0]) && Convert.ToInt32(DateTime.Now.Month.ToString()) == Convert.ToInt32(activityDate[1]) && Convert.ToInt32(DateTime.Now.Day.ToString()) == Convert.ToInt32(activityDate[2]) && Convert.ToInt32(DateTime.Now.Hour.ToString()) >= Convert.ToInt32(activityDate[3]) && Convert.ToInt32(DateTime.Now.Minute.ToString()) >= Convert.ToInt32(activityDate[4]))
            {
                //
            }
            else
            {
                PrintLog("活动时间未到，请勿采取其他手段进入服务器哦");
                return;
            }
            if (Environment.Is64BitOperatingSystem)
            {
                if (System.IO.File.Exists(System.IO.Directory.GetCurrentDirectory() + "\\data\\lan-play-win64.exe"))
                {
                    System.Diagnostics.Process p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = me_path + "\\data\\lan-play-win64.exe";
                    p.StartInfo.Arguments = "--relay-server-addr " + activityServer;
                    p.StartInfo.UseShellExecute = true;
                    p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    p.Start();
                    PrintLog("进入活动房间:" + activityTitle + " Mode:Win64");
                    ConnectedIP = activityServer;
                    LanPlayInject("win64", activityServer, 2);
                    metroButton5.Enabled = false;
                    IfInActivity = true;
                }
                else
                {
                    MessageBox.Show("程序数据被破坏，请重新安装", "错误");
                    PrintLog("可能缺少lan-play-win64.exe 程序异常退出");
                    Application.Exit();
                }
            }
            else
            {
                if (System.IO.File.Exists(System.IO.Directory.GetCurrentDirectory() + "\\data\\lan-play-win32.exe"))
                {
                    System.Diagnostics.Process p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = me_path + "\\data\\lan-play-win32.exe";
                    p.StartInfo.Arguments = "--relay-server-addr " + activityServer;
                    p.StartInfo.UseShellExecute = true;
                    p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    p.Start();
                    PrintLog("进入活动房间:" + activityTitle + " Mode:Win32");
                    ConnectedIP = activityServer;
                    LanPlayInject("win32", activityServer, 2);
                    metroButton5.Enabled = false;
                    IfInActivity = true;
                }
                else
                {
                    MessageBox.Show("程序数据被破坏，请重新安装", "错误");
                    PrintLog("可能缺少lan-play-win32.exe 程序异常退出");
                    Application.Exit();
                }
            }
        }

        private void timer3_Tick(object sender, EventArgs e)
        {
            try
            {
                string gotServerJson = GetServerJson("http://" + activityServer + "/info"); //Socket获取服务端Json
                JObject obj = JObject.Parse(gotServerJson);
                int serverOnline2 = (int)obj["online"];
                metroLabel6.Text = "在线人数：" + Convert.ToString(serverOnline2);
            }
            catch
            {
                metroLabel6.Text = "在线人数：获取失败";
            }
            metroLabel7.Text = "Ping:" + GetPing(FormatUrl(ConnectedIP, 3), 1);
        }

        private void metroButton6_Click(object sender, EventArgs e)
        {
            groupBox3.Visible = true;
            metroButton6.Enabled = false;
            metroButton5.Enabled = false;
            metroButton9.Enabled = false;
            button1.Enabled = false;
            button2.Enabled = false;
        }

        private void metroButton8_Click(object sender, EventArgs e)
        {
            LoadSettings();
            groupBox3.Visible = false;
            metroButton6.Enabled = true;
            metroButton5.Enabled = true;
            metroButton9.Enabled = true;
            button1.Enabled = true;
            button2.Enabled = true;
        }

        private void metroButton7_Click(object sender, EventArgs e)
        {
            //
            /*
            if(metroCheckBox1.Checked == true)
                WriteINI("settings", "ifCheckActivity", "true", data_path);
            else
                WriteINI("settings", "ifCheckActivity", "false", data_path);
            */
            //
            if (metroCheckBox2.Checked == true)
                WriteINI("settings", "ifHideLanPlay", "true", data_path);
            else
                WriteINI("settings", "ifHideLanPlay", "false", data_path);
            //
            LoadSettings();
            groupBox3.Visible = false;
            metroButton6.Enabled = true;
            metroButton5.Enabled = true;
            metroButton9.Enabled = true;
            button1.Enabled = true;
            button2.Enabled = true;
            PrintLog("保存设置成功");
        }
        private void getServerList()
        {
            try
            {
                string statusPage2 = GetServerJson(serverListLink);
                string statusPage = statusPage2.Substring(statusPage2.IndexOf("<body>") + "<body>".Length, statusPage2.IndexOf("</body>") - (statusPage2.IndexOf("<body>") + "<body>".Length));
                JObject obj = JObject.Parse(statusPage);
                string[] roomName = new string[15];
                string[] roomURL = new string[15];
                int count = 0;
                for (int i = 0; i < 15; i++)
                {
                    roomName[i] = (string)obj["name" + Convert.ToString(i)];
                    roomURL[i] = (string)obj["url" + Convert.ToString(i)];
                    if (roomName[i] != "")
                        count++;
                }
                if (MessageBox.Show(this,  "成功获取到最新的服务器列表\n是否保存？\n此操作将会覆盖您本地保存的服务器列表","是否保存", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    listView1.Items.Clear();
                    int i;
                    for (i = 0; i < count; i++)
                    {
                        WriteINI("servers", Convert.ToString(i), roomURL[i], data_path);
                        WriteINI("remarks", Convert.ToString(i), roomName[i], data_path); //ANSI Code Only
                    }
                    for (i = 1; i <= 15 - count; i++)
                    {
                        WriteINI("servers", Convert.ToString(15 - i), "", data_path);
                        WriteINI("remarks", Convert.ToString(15 - i), "", data_path);
                    }
                    LoadSavedServer();
                    GetAllServerInfoEx();
                    PrintLog("成功获取服务器列表");
                }
                else
                    return;
            }
            catch
            {
                MessageBox.Show(this, "获取失败", "无法获取服务器列表\n可能是服务器错误或者您的程序发生了问题\n请重新安装软件或者联系管理员");
                PrintLog("获取服务器列表失败");
            }
        }

        private void getServerListSilent()
        {
            try
            {
                string statusPage2 = GetServerJson(serverListLink);
                string statusPage = statusPage2.Substring(statusPage2.IndexOf("<body>") + "<body>".Length, statusPage2.IndexOf("</body>") - (statusPage2.IndexOf("<body>") + "<body>".Length));
                JObject obj = JObject.Parse(statusPage);
                string[] roomName = new string[15];
                string[] roomURL = new string[15];
                int count = 0;
                for (int i = 0; i < 15; i++)
                {
                    roomName[i] = (string)obj["name" + Convert.ToString(i)];
                    roomURL[i] = (string)obj["url" + Convert.ToString(i)];
                    if (roomName[i] != "")
                        count++;
                }
                listView1.Items.Clear();
                int j;
                for (j = 0; j < count; j++)
                {
                    WriteINI("servers", Convert.ToString(j), roomURL[j], data_path);
                    WriteINI("remarks", Convert.ToString(j), roomName[j], data_path); //ANSI Code Only
                }
                for (j = 1; j <= 15 - count; j++)
                {
                    WriteINI("servers", Convert.ToString(15 - j), "", data_path);
                    WriteINI("remarks", Convert.ToString(15 - j), "", data_path);
                }
                LoadSavedServer();
                GetAllServerInfoEx();
                PrintLog("成功更新服务器列表");
            }
            catch
            {
                PrintLog("更新服务器列表失败");
            }
        }

        private void getServerListSilentEx()
        {
            ThreadStart thStart = new ThreadStart(getServerListSilent);//threadStart委托 
            Thread thread = new Thread(thStart);
            thread.Priority = ThreadPriority.Highest;
            thread.IsBackground = false; //关闭窗体继续执行
            thread.Start();
        }

        private void metroButton9_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show(this,"是否要获取我们提供的服务器列表？\n","获取服务器列表",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes)
                getServerList();
        }

        private void FirstCheckServerList()
        {
            if (IfHaveFile(data_path))
            {
                if (settingIfFirstCheckServerList == "true")
                {
                    WriteINI("settings", "ifFirstCheckServerList", "false", data_path);
                    getServerListSilentEx();
                }
            }
        }

        private void metroButton10_Click(object sender, EventArgs e)
        {
            MessageBox.Show(this, "VLAN Online Platform v" +  
                LocalVersion + 
                "\n\n" +
                "感谢818LanPlay Hub提供的服务器\n" +
                "软件GUI由绿胡子大叔制作\n\n" +
                "核心技术来自GitHub spacemeowx2的开源项目switch-lan-play\n\n" +
                "更多信息请访问论坛http://bbs.818lanplay.com/",
                "关于");
        }
    }
}