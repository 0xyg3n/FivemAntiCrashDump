//Author 0xyg3n
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.IO;

namespace FivemAntiCrashDump
{


    #region Config
    public static class Config
    {

        public static string FivemPath = Properties.Settings.Default.FivemPath;
    }
    #endregion
  
    #region Core
    public class Program
    {
        //this is for the window to be hidden just dont touch
        #region hidewindow
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0; // hide window
        const int SW_SHOW = 5; // show window
        #endregion

        static async Task Main()
        {
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_SHOW); 
            
            
            void Dscan()
            {
                try
                {

                    if (Directory.Exists(Config.FivemPath))
                    {
                        foreach (FileInfo fileInfo in new DirectoryInfo(Config.FivemPath).GetFiles())
                        {
                            if (fileInfo.Name.EndsWith(".dmp"))
                            {
                                File.Delete(fileInfo.FullName); // delete the file xd.
                                Console.Beep(); //notify with beep upon file deletion.
                            }
                        }
                    }
                    else
                    {
                        MessageBox.Show("FiveM Crash Dump Path Not Valid!", "FiveM Anti-CrashDump", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }

                }
                catch { }
            }

            async Task IFloop()
            {
                try
                {
                    while (true) 
                    {
                        await Task.Delay(10000); 
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
        }
    }
    #endregion
}