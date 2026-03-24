using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace EddyLib
{
    public static partial class Utilities
    {
        public class StartProcess
        {
            public static void StartProcessCMD(string argument, bool createnowindow, bool waitforexit = false, bool close = false, string executable = @"C:\Windows\System32\cmd.exe")
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo.FileName = executable;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.CreateNoWindow = createnowindow;
                p.Start();
                StreamWriter sw = p.StandardInput;
                string strInputText = argument;
                sw.WriteLine(strInputText);

                sw.Flush();
                if (waitforexit) { p.WaitForExit(); }
                if (close) { p.Close(); }
            }

            public static void StartGnuplot(string argument, bool createnowindow, bool waitforexit = false, string executable = @"C:\Windows\System32\cmd.exe")
            {
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                p.StartInfo.FileName = executable;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.CreateNoWindow = createnowindow;
                p.Start();
                StreamWriter sw = p.StandardInput;
                string strInputText = argument;
                sw.WriteLine(strInputText);

                sw.Flush();
                if (waitforexit)
                {
                    p.WaitForExit(); // Wait for the process to exit if required.
                }
            }

            public static void StartProcessCMDNT(string argument, bool createnowindow, bool waitforexit = true, bool close = false, bool startInNewThread = false)
            {
                StartProcessCMDNT(argument, createnowindow, waitforexit, close, startInNewThread, @"C:\Windows\System32\cmd.exe");
            }

            public static void StartProcessCMDNT(string argument, bool createnowindow, bool waitforexit = true, bool close = false, bool startInNewThread = false, EventHandler eh = null)
            {
                StartProcessCMDNT(argument, createnowindow, waitforexit, close, startInNewThread, @"C:\Windows\System32\cmd.exe", eh);
            }

            public static void StartProcessCMDNT(string argument, bool createnowindow, bool waitforexit = true, bool close = false, bool startInNewThread = false, string executable = @"C:\Windows\System32\cmd.exe", EventHandler eh = null)
            {
                if (!File.Exists(executable)) { return; }

                System.Diagnostics.Process p = new System.Diagnostics.Process();

                // if(eh!=null) p.Exited += eh;
                p.StartInfo.FileName = executable;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardInput = true;

                //p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.CreateNoWindow = createnowindow;

                //p.Start();

                string theArgument = argument + ((close) ? @"
exit
" : "");

                ThreadStart ths = new ThreadStart(() =>
                {
                    p.Start();

                    StreamWriter sw = p.StandardInput;
                    String strInputText = theArgument;
                    sw.WriteLine(strInputText);

                    // Window doesn't close with
                    //sw.Flush();

                    p.WaitForExit();
                    if (close) { p.Close(); }
                    if (eh != null) { eh.Invoke(p, new EventArgs()); }
                });

                Thread th = new Thread(ths);
                th.Start();

                //if (waitforexit)
                //{
                //    //Console.ReadLine();
                //    p.WaitForExit();
                //}
                //if (close) { p.Close(); }
            }

            public static void StartBatchScriptCMDNT(string scriptContent, bool createnowindow, bool waitforexit = true, bool close = false, bool startInNewThread = false, EventHandler eh = null)
            {
                if (string.IsNullOrWhiteSpace(scriptContent)) { return; }

                string cmdExe = @"C:\Windows\System32\cmd.exe";
                if (!File.Exists(cmdExe)) { return; }

                string tempDir = Path.Combine(Path.GetTempPath(), "Eddy3D");
                Directory.CreateDirectory(tempDir);

                string tempBatchFile = Path.Combine(tempDir, $"Eddy3D_{Guid.NewGuid():N}.bat");
                File.WriteAllText(tempBatchFile, scriptContent);

                ThreadStart ths = new ThreadStart(() =>
                {
                    System.Diagnostics.Process p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = cmdExe;
                    p.StartInfo.Arguments = $@"/c """"{tempBatchFile}""""";
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = createnowindow;

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
