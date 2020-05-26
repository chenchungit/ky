using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using KF.Remoting;
using System.Security.AccessControl;
using System.IO;
using Server.Tools;
using System.Runtime.Remoting;

namespace KF.Hosting.HuanYingSiYuan
{
    class Program
    {
        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx")]   //找子窗体   
        private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);

        [DllImport("User32.dll", EntryPoint = "SendMessage")]   //用于发送信息给窗体   
        private static extern int SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, string lParam);

        [DllImport("User32.dll", EntryPoint = "ShowWindow")]   //
        private static extern bool ShowWindow(IntPtr hWnd, int type);

        public static void SetWindowMin()
        {
            Console.Title = "KF.Server.Hosting";
            IntPtr ParenthWnd = new IntPtr(0);
            IntPtr et = new IntPtr(0);
            ParenthWnd = FindWindow(null, "KF.Server.Hosting");

            ShowWindow(ParenthWnd, 2);//隐藏本dos窗体, 0: 后台执行；1:正常启动；2:最小化到任务栏；3:最大化
        }

        #region 控制台关闭控制 windows
        public delegate bool ControlCtrlDelegate(int CtrlType);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ControlCtrlDelegate HandlerRoutine, bool Add);

        static ControlCtrlDelegate newDelegate = new ControlCtrlDelegate(HandlerRoutine);

        public static bool HandlerRoutine(int CtrlType)
        {
            switch (CtrlType)
            {
                case 0:
                    //Console.WriteLine("工具被强制关闭(ctrl + c)"); //Ctrl+C关闭   
                    break;
                case 2:
                    //Console.WriteLine("工具被强制关闭(界面关闭按钮)");//按控制台关闭按钮关闭   
                    break;
            }

            //关闭事件被捕获之后，不需要进行关闭处理
            return true;
        }

        //[DllImport("user32.dll", EntryPoint = "FindWindow")]
        //extern static IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll", EntryPoint = "GetSystemMenu")]
        extern static IntPtr GetSystemMenu(IntPtr hWnd, IntPtr bRevert);
        [DllImport("user32.dll", EntryPoint = "RemoveMenu")]
        extern static IntPtr RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);

        /// <summary>
        /// 禁用关闭按钮
        /// </summary>
        static void HideCloseBtn()
        {
            IntPtr windowHandle = FindWindow(null, Console.Title);
            IntPtr closeMenu = GetSystemMenu(windowHandle, IntPtr.Zero);
            uint SC_CLOSE = 0xF060;
            RemoveMenu(closeMenu, SC_CLOSE, 0x0);
        }

        #endregion 控制台关闭控制

        static bool NeedExitServer = false;

        private static CmdHandlerDict CmdDict = new CmdHandlerDict();

        static void Main(string[] args)
        {
            try
            {
                FileStream fs = File.Open("Pid.txt", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
                if (fs != null)
                {
                    byte[] bytes = Encoding.UTF8.GetBytes(Process.GetCurrentProcess().Id.ToString());
                    fs.Write(bytes, 0, bytes.Length);
                    fs.Flush();
                }
            }
            catch (System.Exception ex)
            {
                Console.WriteLine("本程序已经启动了一个进程,按任意键退出!");
                Console.ReadKey();
                return;
            }

            //Console.TreatControlCAsInput = true;

            #region 控制台关闭控制

            HideCloseBtn();

            SetConsoleCtrlHandler(newDelegate, true);

            if (Console.WindowWidth < 88)
            {
                Console.BufferWidth = 88;
                Console.WindowWidth = 88;
            }

            #endregion 控制台关闭控制

            Console.WriteLine("跨服中心服务器启动!");
            LogManager.WriteLog(LogTypes.Info, "跨服中心服务器启动!");
            SetWindowMin();

            //new MoRiJudgeService();
            if (!KuaFuServerManager.CheckConfig())
            {
                Console.WriteLine("服务器无法启动!");
            }

            if (!KuaFuServerManager.LoadConfig())
            {
                Console.ReadLine();
                return;
            }

            KuaFuServerManager.StartServerConfigThread();
            
            RemotingConfiguration.Configure(Process.GetCurrentProcess().MainModule.FileName + ".config", false);
            InitCmdDict();

            //YongZheZhanChangService s = new YongZheZhanChangService();

            do 
            {
                try
                {
                    ShowCmdHelp();
                    string cmd = Console.ReadLine();
                    if (null != cmd)
                    {
                        CmdDict.ExcuteCmd(cmd);
                    }

                    //判断是否需要退出
                    if (NeedExitServer)
                    {
                        KuaFuServerManager.OnStopServer();

                        Console.WriteLine("Press any key to Stop!");
                        Console.ReadKey();
                        break;
                    }
                }
                catch (System.Exception ex)
                {
                    LogManager.WriteException(ex.ToString());
                }
            } while (true);
        }

        /// <summary>
        /// 初始化命令行命令处理器
        /// </summary>
        private static void InitCmdDict()
        {
            CmdDict.AddCmdHandler("exit", ExitCmdHandler);
            CmdDict.AddCmdHandler("reload", ReloadCmdHandler);
            CmdDict.AddCmdHandler("clear", ClearCmdHandler);
            CmdDict.AddCmdHandler("load", ReloadCmdHandler);
        }

        private static void ShowCmdHelp()
        {
            Console.WriteLine("\n命令列表:");
            Console.WriteLine("exit : 退出");
            Console.WriteLine("reload : 重新加载配置文件");
            Console.WriteLine("clear : 清空控制台输出");
        }

        public static void ExitCmdHandler(object obj)
        {
            Console.WriteLine("确定要退出?请输入'y'");
            if (Console.ReadLine() == "y")
            {
                NeedExitServer = true;
                Console.WriteLine("退出程序!");
            }
        }

        public static void ReloadCmdHandler(object obj)
        {
            try
            {
                string[] args = obj as string[];
                if (args.Length == 1 && args[0] == "reload")
                {
                    KuaFuServerManager.LoadConfig();
                }
                else
                {
                    TianTiService.Instance.ExecCommand(args);
                }
            }
            catch
            {            	
            }

            Console.WriteLine("重新加载配置成功!");
        }

        public static void ReloadPaiHangCmdHandler(object obj)
        {
            try
            {
                TianTiService.Instance.ExecCommand(new string[] { "reload", "paihang" });
            }
            catch
            {
            }

            Console.WriteLine("重新加载配置成功!");
        }

        public static void ClearCmdHandler(object obj)
        {
            Console.Clear();
        }
    }

    public class CmdHandlerDict
    {
        /// <summary>
        /// 命令词典
        /// </summary>
        private Dictionary<String, ParameterizedThreadStart> CmdDict = new Dictionary<string, ParameterizedThreadStart>();

        public void AddCmdHandler(string cmd, ParameterizedThreadStart handler)
        {
            CmdDict.Add(cmd, handler);
        }

        public string[] ParseConsoleCmd(string cmd)
        {
            List<string> argsList = new List<string>();
            string arg = "";
            Stack<char> quoteStack = new Stack<char>();
            foreach (var c in cmd)
            {
                if (char.IsWhiteSpace(c))
                {
                    if (quoteStack.Count == 0)
                    {
                        if (arg != "")
                        {
                            argsList.Add(arg);
                            arg = "";
                        }

                        continue;
                    }
                }
                else if (c == '\"')
                {
                    if (quoteStack.Count > 0 && quoteStack.Peek() == '\"')
                    {
                        quoteStack.Pop();
                    }
                    else
                    {
                        quoteStack.Push(c);
                    }
                }
                else if (c == '\'')
                {
                    if (quoteStack.Count > 0 && quoteStack.Peek() == '\'')
                    {
                        quoteStack.Pop();
                    }
                    else
                    {
                        quoteStack.Push(c);
                    }
                }

                arg += c;
            }

            if (arg != "")
            {
                argsList.Add(arg);
            }

            return argsList.ToArray();
        }

        public void ExcuteCmd(string cmd)
        {
            if (!string.IsNullOrEmpty(cmd))
            {
                string[] args = ParseConsoleCmd(cmd);
                if (args == null || args.Length == 0)
                {
                    return;
                }

                ParameterizedThreadStart proc;
                if (CmdDict.TryGetValue(args[0].ToLower(), out proc))
                {
                    proc(args);
                }
            }
        }
    }
}
