//Author 0xyg3n
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;
using System.Configuration;
using System.Security.Principal;

namespace FivemAntiCrashDump
{

    #region Config
    public static class Config
    {
        public static string DefaultPath = Environment.GetEnvironmentVariable("LocalAppData")+ "\\FiveM\\FiveM.app\\crashes";
        
        public static void Store()
        {
            try
            {
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings["FiveMpath"].Value = DefaultPath;
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception ex)
            {
                MessageBox.Show("An Error Occured Writing Config File!\n"+ex.ToString(), "FiveM Anti-CrashDump", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
    #endregion

    #region HostBlocker

    public static class HostBlocker
    {
        // based on fivem's source code https://github.com/citizenfx/fivem/blob/3ae804625b61be2ac850f3079837ff40dec347d9/code/client/launcher/MiniDump.cpp

        public static string Host = "\n127.0.0.1 crash-ingress.fivem.net\n";

        public static void AppendHost()
        {
            try
            {
                var HostsFilePath = Environment.SystemDirectory + "\\drivers\\etc\\hosts";
                var HostAlreadyBlocked = false;

                if (File.Exists(HostsFilePath))
                {
                    using (StreamReader sr = new StreamReader(HostsFilePath))
                    {
                        string contents = sr.ReadToEnd();
                        if (!contents.Contains("crash-ingress.fivem.net"))
                        {
                            HostAlreadyBlocked = false;
                            sr.Close();
                        }
                        else
                        {
                            HostAlreadyBlocked = true;
                        }
                    }

                    if (!HostAlreadyBlocked)
                    {
                        using (StreamWriter w = File.AppendText(HostsFilePath))
                        {
                            w.WriteLine(Host);
                            w.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An Error Occured Blocking The Dump Server.\n"+ex.ToString(), "FiveM Anti-CrashDump", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
           
        }
    }

    #endregion

    public static class CheckAdmin
    {
        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
    }

    #region Core
    public class Program
    {
        //this is for the window to be hidden just dont touch
        #region HideWindow
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0; // hide window
        const int SW_SHOW = 5; // show window
        #endregion

        static async Task Main()
        {
            // Call the hidden window thingy
            #region HideWindow2
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_SHOW);
            #endregion

            #region Admin_Check
            if (!CheckAdmin.IsAdministrator())
            {
                MessageBox.Show("FiveM-AntiCrashDump needs to run as elevated process.", "FiveM Anti-CrashDump", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
            #endregion

            // Save Path Upon Launch
            #region ChecksBeforeDaemonLaunches
            try
            {
                //check if needs to be saved to config or not
                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (config.AppSettings.Settings["FiveMpath"].Value.Contains("ChangeMe"))
                {
                    Config.Store(); // save path to config file
                }

                //check for if host is blocked or not
                HostBlocker.AppendHost();
            }
            catch (Exception)
            {
                MessageBox.Show("An Error Occured Saving The Config File.", "FiveM Anti-CrashDump", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(0);
            }
            #endregion

            // Dump Clean Loop
            #region Scan4DumpsFunc
            void Dscan()
            {
                try
                {
                    // if directory exists delete crash
                    if (Directory.Exists(Config.DefaultPath))
                    {
                        foreach (FileInfo fileInfo in new DirectoryInfo(Config.DefaultPath).GetFiles())
                        {
                            if (fileInfo.Name.EndsWith(".dmp"))
                            {
                                File.Delete(fileInfo.FullName); // delete the file xd.
                                Console.Beep(); //notify with beep upon file deletion.
                            }
                        }
                    }
                    //exit ifnot
                    else
                    {
                        MessageBox.Show("FiveM crash dump path not valid, looks like it's not the default one!\nChange the crash dump path to your's on the config file.", "FiveM Anti-CrashDump", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Environment.Exit(0);
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show("An Error Occured!\n" + ex.ToString());
                    Environment.Exit(0);
                }
            }
            #endregion

            //the async loop
            #region Async_Loop
            async Task IFloop()
            {
                try
                {
                    while (true)
                    {
                        await Task.Delay(8000);
                        Dscan();
                    }
                }
                catch { }
            }

            try
            {
                await IFloop();
            }
            catch { }
            #endregion
        }
    }
    #endregion
}