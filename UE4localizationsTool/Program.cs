using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
namespace UE4localizationsTool
{
    internal static class Program
    {

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;


        public static string commandlines =
         $"{AppDomain.CurrentDomain.FriendlyName}  export     <(Locres/Uasset/Umap) 文件路径>  <选项>\n" +
         $"{AppDomain.CurrentDomain.FriendlyName}  import     <(txt) 文件路径>  <选项>\n" +
         $"{AppDomain.CurrentDomain.FriendlyName} -import     <(txt) 文件路径>  <选项>\n" +
         $"{AppDomain.CurrentDomain.FriendlyName}  exportall  <文件夹> <文本文件> <选项>\n" +
         $"{AppDomain.CurrentDomain.FriendlyName}  importall  <文件夹> <文本文件>  <选项>\n" +
         $"{AppDomain.CurrentDomain.FriendlyName} -importall  <文件夹> <文本文件>  <选项>\n\n" +
          "- 不重命名文件直接导入时，请谨慎使用此命令。\n\n" +

          "选项：\n" +
          "如果要沿用图形界面里上次使用的筛选，请在命令后加上 (-f \\ -filter)\n" +
          "该筛选只会作用于名称表" +
            "\n（导入时请记得使用相同的筛选条件）\n\n" +

          "如果导出时不包含名称表，请使用 (-nn \\ -NoName)" +
          "\n（导入时也请使用同样的参数）\n\n" +

          "如果要使用方法 2，请使用 (-m2 \\ -method2)" +
          "\n（导入时也请使用同样的参数）\n\n" +

          "示例：\n" +
         $"{AppDomain.CurrentDomain.FriendlyName} export Actions.uasset\n" +
         $"{AppDomain.CurrentDomain.FriendlyName} import Actions.uasset.txt\n" +
         $"{AppDomain.CurrentDomain.FriendlyName} exportall Actions text.txt\n" +
         $"{AppDomain.CurrentDomain.FriendlyName} importall Actions text.txt\n";

        public static Args GetArgs(int Index, string[] args)
        {
            Args args1 = new Args();

            for (int n = Index; n < args.Length; n++)
            {
                switch (args[n].ToLower())
                {
                    case "-f":
                    case "-filter":
                        args1 |= Args.filter;
                        break;
                    case "-nn":
                    case "-noname":
                        args1 |= Args.noname;
                        break;
                    case "-m2":
                    case "-method2":
                        args1 |= Args.method2;
                        break;
                    
                    case "-c":
                    case "-csv":
                        args1 |= Args.CSV;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("无效命令：" + args[n]);
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                }
            }
            return args1;
        }


        public static void CheckArges(int Index, string[] args)
        {
            for (int n = 0; n < Index; n++)
            {
                switch (args[n].ToLower())
                {
                    case "-f":
                    case "-filter":
                    case "-nn":
                    case "-noname":
                    case "-method2":
                    case "-m2":
                    case "-c":
                    case "-csv":
                        throw new Exception("参数数量不正确。\n\n" + commandlines);
                }
            }
        }



        [STAThread]

        static void Main(string[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    if (args.Length == 1 && (args[0].EndsWith(".uasset") || args[0].EndsWith(".umap") || args[0].EndsWith(".locres")))
                    {
                        Application.EnableVisualStyles();
                        Application.SetCompatibleTextRenderingDefault(false);
                        var FrmMain = new FrmMain();
                        FrmMain.Show();
                        FrmMain.LoadFile(args[0]);
                        Application.Run(FrmMain);
                        return;
                    }


                    AttachConsole(ATTACH_PARENT_PROCESS);
                    Console.WriteLine("");
                    //  Console.SetCursorPosition(0, Console.CursorTop + 1);

                    if (args.Length < 2)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("参数数量不正确。\n\n" + commandlines);
                        Console.ForegroundColor = ConsoleColor.White;
                        return;
                    }
                    try
                    {

                        if (args[0].ToLower() == "importall" || args[0].ToLower() == "-importall" || args[0].ToLower() == "exportall")
                        {
                            if (args.Length < 3)
                            {
                                throw new Exception("参数数量不正确。\n\n" + commandlines);
                            }

                            CheckArges(3, args);
                            new Commands(args[0], args[1] + "*" + args[2], GetArgs(3, args));
                        }
                        else
                        {
                            CheckArges(2, args);
                            new Commands(args[0], args[1], GetArgs(2, args));
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("\n" + ex.Message);
                        Console.ForegroundColor = ConsoleColor.White;
                    }
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new FrmMain());
            }
            catch (Exception ex)
            {
                var logPath = LogStartupError(ex);
                MessageBox.Show(
                    "程序启动失败，错误详情已写入日志：\n" + logPath + "\n\n" + ex.Message,
                    "UE4 本地化工具",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private static string LogStartupError(Exception ex)
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup-error.log");
            try
            {
                var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]{Environment.NewLine}{ex}{Environment.NewLine}{new string('-', 80)}{Environment.NewLine}";
                File.AppendAllText(logPath, message);
            }
            catch
            {
            }
            return logPath;
        }
    }
}
