using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VLAN_Online_Platform
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            bool isAppRunning = false;
            Mutex mutex = new Mutex(true, System.Diagnostics.Process.GetCurrentProcess().ProcessName, out isAppRunning);
            if (!isAppRunning)
            {
                MessageBox.Show("程序已运行，不能再次打开！", "错误");
                Environment.Exit(1);
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length == 0)
            {
                Application.Run(new Form1(null));
            }
            else
            {
                Application.Run(new Form1(args));
            }
            /*
            var surekamKey = Microsoft.Win32.Registry.ClassesRoot.CreateSubKey("VOP");
            var shellKey = surekamKey.CreateSubKey("shell");
            var openKey = shellKey.CreateSubKey("open");
            var commandKey = openKey.CreateSubKey("command");
            surekamKey.SetValue("URL Protocol", "");
            var exePath = Process.GetCurrentProcess().MainModule.FileName;
            commandKey.SetValue("", "\"" + exePath + "\"" + "\"%1\"");
            */
        }
    }
}
