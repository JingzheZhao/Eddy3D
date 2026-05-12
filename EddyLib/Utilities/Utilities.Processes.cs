using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace EddyLib
{
    public static partial class Utilities
    {
        public class StartProcess
        {
            public static void StartProcessCMDNT(string argument, bool createnowindow, bool waitforexit = true, bool close = false, bool startInNewThread = false, string executable = @"C:\Windows\System32\cmd.exe", EventHandler eh = null)
            {
                if (!File.Exists(executable)) { return; }
                ValidatePathForShell(executable);

                ThreadStart ths = new ThreadStart(() =>
                {
                    try
                    {
                        ProcessStartInfo psi = new ProcessStartInfo
                        {
                            FileName = @"C:\Windows\System32\cmd.exe",
                            UseShellExecute = false,
                            CreateNoWindow = createnowindow,
                        };
                        psi.ArgumentList.Add("/c");
                        psi.ArgumentList.Add(executable);
                        if (!string.IsNullOrEmpty(argument))
                        {
                            psi.ArgumentList.Add(argument);
                        }

                        using (Process p = Process.Start(psi))
                        {
                            p?.WaitForExit();
                            eh?.Invoke(p, EventArgs.Empty);
                        }
                    }
                    catch
                    {
                        // Fail securely
                    }
                });

                Thread th = new Thread(ths) { IsBackground = true };
                th.Start();
            }

            public static void StartBatchScriptCMDNT(string scriptContent, bool createnowindow, bool waitforexit = true, bool close = false, bool startInNewThread = false, EventHandler eh = null)
            {
                if (string.IsNullOrWhiteSpace(scriptContent)) { return; }

                string cmdExe = @"C:\Windows\System32\cmd.exe";
                if (!File.Exists(cmdExe)) { return; }

                string tempBatchFile = Path.Combine(Path.GetTempPath(), $"Eddy3D_{Guid.NewGuid():N}.bat");
                using (FileStream fs = new FileStream(tempBatchFile, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(scriptContent);
                }

                ThreadStart ths = new ThreadStart(() =>
                {
                    Process p = new Process();
                    p.StartInfo.FileName = cmdExe;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = createnowindow;
                    p.StartInfo.ArgumentList.Add("/c");
                    p.StartInfo.ArgumentList.Add(tempBatchFile);

                    try
                    {
                        p.Start();

                        if (waitforexit)
                        {
                            p.WaitForExit();
                        }

                        if (close)
                        {
                            p.Close();
                        }

                        if (eh != null)
                        {
                            eh.Invoke(p, new EventArgs());
                        }
                    }
                    finally
                    {
                        if (waitforexit)
                        {
                            try
                            {
                                File.Delete(tempBatchFile);
                            }
                            catch
                            {
                                // Ignore temp-file cleanup failures.
                            }
                        }
                    }
                });

                if (startInNewThread)
                {
                    Thread th = new Thread(ths);
                    th.Start();
                }
                else
                {
                    ths();
                }
            }
        }
    }
}
