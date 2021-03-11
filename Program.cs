using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;

namespace gesoLauncher
{
    static class Program
    {
        public static string AppPath { get; set; }
        public static string AppDir { get; set; }
        public static string AppName { get; set; }
        public static string AppNameWoExt { get; set; }
        public static InifileUtils Ini { get; set; }
        public static IList<Client> Clientlist { get; set; }

        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length == 0) return;
            AppPath = Assembly.GetEntryAssembly().Location;
            //ディレクトリ
            AppDir = Path.GetDirectoryName(AppPath);
            //ファイル名
            AppName = Path.GetFileName(AppPath);
            //ファイル名(拡張子含まず)
            AppNameWoExt = System.IO.Path.ChangeExtension(AppName, null);
            Ini = new InifileUtils(Path.Combine(AppDir, AppNameWoExt + ".ini"));
            Clientlist = new List<Client>();

            Client Clientl;
            int i = 0;
            while (true)
            {
                Clientl = new Client
                {
                    Str = Ini.GetValueString("Default", "str" + i),
                    Exe = Ini.GetValueString("Default", "exe" + i),
                    Env = Ini.GetValueString("Default", "env" + i),
                    Del = Ini.GetValueString("Default", "Del" + i) != ""
                };
                Clientl.Chkexe();
                Clientl.Chkenv();
                //Clientl.Addexe(Ini.GetValueString("Default", "exe"+i));
                //Ini.SetValue("Default", "str" + i, Clientl.Str);

                if (Clientl.Str == "")
                {
                    if (i == 0) return;
                    break;
                }
                Clientlist.Add(Clientl);
                i++;
            }
            Directory.SetCurrentDirectory(AppDir);
            string url;
            int s = Clientlist.Count;
            //if (s <= 1) return;
            for (i = 1; i < s; i++)
            {
                Clientl = Clientlist[i];
                url = Clientl.StartsWith(args[0]);
                Clientl.Exe = Clientl.Exe == "" ? Clientlist[0].Exe : Clientl.Exe;
                if (url != "" && Clientl.Exe != "")
                {
                    Clientl.Startexe(Clientl.Del ? url : args[0]);
                    break;
                }
            }
        }
        public class InifileUtils
        {
            private String FilePath { get; set; }
            public InifileUtils(String filePath)
            {
                this.FilePath = filePath;
            }
            public String GetValueString(String section, String key)
            {
                StringBuilder sb = new StringBuilder(1024);

                GetPrivateProfileString(
                    section,
                    key,
                    "",
                    sb,
                    Convert.ToUInt32(sb.Capacity),
                    FilePath);

                return sb.ToString();
            }
            [DllImport("kernel32.dll")]
            public static extern uint GetPrivateProfileString(string lpAppName, string lpKeyName, string lpDefault, StringBuilder lpReturnedString, uint nSize, string lpFileName);

        }
        public class Client
        {
            public string Str { get; set; }
            public string Exe { get; set; }
            public string Env { get; set; }
            public bool Del { get; set; }
            public Client()
            {
                //Str = Exe = "";
            }
            private bool IndexOfAny(string x)
            {
                string[] c = new string[] {
                    "Windows", "cmd.exe", "cmd ", "diskpart.exe", "diskpart ", "format"
                    //, null
                };
                foreach (string s in c)
                {
                    //System.Globalization.CultureInfo.CurrentCulture.CompareInfo.IndexOf(this, value, startIndex, count, CompareOptions.IgnoreCase);
                    if (x.IndexOf(s, StringComparison.CurrentCultureIgnoreCase) != -1)
                        return true;
                }
                return false;
            }
            public string Chkexe() => Addexe(this.Exe);
            public string Chkenv() => Addenv(this.Env);
            private string Addenv(string x)
            {
                while (true)
                {
                    if (string.IsNullOrEmpty(x)) { x = ""; break; }
                    if (x.Length != 0)
                    {
                        if (x.IndexOf("-profile", StringComparison.CurrentCultureIgnoreCase) == -1)
                        { x = ""; break; }
                        x += " ";
                    }
                    break;
                }
                Env = x;
                return x;
            }
            private string Addexe(string x)
            {
                while (true){
                    if (string.IsNullOrEmpty(x)) { x = ""; break; }
                    if (x.Length != 0)
                    {
                        if (IndexOfAny(x))
                        { x = ""; break; }
                        char[] array = x.ToCharArray();
                        int s = 0;
                        if (array[0] == '"') { s = 1; }
                        if (array[s] == '%' || array[s] == '.' || array[s] == '/' || array[s] == '\\' || array[s] == ' ')
                        {
                            x = "";
                        }
                    }
                    break;
                }
                Exe = x;
                return x;
            }
            public string StartsWith(string url)
            {
                if (url.StartsWith(Str))
                {
                    return url.Substring(Str.Length);
                }
                return "";
            }
            public void Startexe(string url)=>Startexep(this.Env + "\"" + url + "\"");
            
            private void Startexep(string url)
            {
                //ProcessStartInfo startInfo=new ProcessStartInfo(exe,url);
                //Process.Start(startInfo);
                using (Process process = new Process()
                {
                    StartInfo = new ProcessStartInfo(Exe, url)
                })
                {
                    //process.StartInfo = startInfo;
                    //process.StartInfo = new ProcessStartInfo(Exe, EncodeCommandLineValue(url));
                    //process.StartInfo = new ProcessStartInfo(Exe, "\""+url+"\"");
                    process.StartInfo.UseShellExecute = false;
                    process.Start();
                }
            }
        }
    }
}
