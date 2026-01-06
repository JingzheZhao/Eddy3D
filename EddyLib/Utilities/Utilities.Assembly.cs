using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace EddyLib
{
    public static partial class Utilities
    {
        public static string AssemblyVersion
        {
            get
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                return fvi.FileVersion;
            }
        }

        public static string GetDirectoryPath(this Assembly assembly)
        {
            string filePath = new Uri(assembly.CodeBase).LocalPath;
            return Path.GetDirectoryName(filePath);
        }

        public static string AssemblyDirectory
        {
            get
            {
                string dir = AppDomain.CurrentDomain.BaseDirectory;
                string localDir = Assembly.GetExecutingAssembly().GetDirectoryPath();
                string dir1 = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);

                Assembly bla1 = Assembly.GetEntryAssembly();    //gives you the entrypoint assembly for the process.
                Assembly bla2 = Assembly.GetCallingAssembly();   // gives you the assembly from which the current method was called.
                Assembly bla3 = Assembly.GetExecutingAssembly(); // gives you the assembly in which the currently executing code is defined
                Assembly bla4 = Assembly.GetAssembly(typeof(OFBaseDomain));  // gives you the assembly in which the specified type is defined.
                string loc = bla4.Location;
                string path2 = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }
    }
}
