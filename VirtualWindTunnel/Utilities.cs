using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Eddy
{
    public static class Utilities
    {
        static public string hardcodedAssemblyDir = @"C:\Users\pkastner\Documents\GitHub\WindTunnel\VirtualWindTunnel\bin\";

        static public string AssemblyVersion
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

        static public string AssemblyDirectory
        {
            get
            {
                var dir = AppDomain.CurrentDomain.BaseDirectory;
                var localDir = Assembly.GetExecutingAssembly().GetDirectoryPath();
                var dir1 = System.IO.Path.GetDirectoryName(new System.Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);


                var bla1 = Assembly.GetEntryAssembly();    //gives you the entrypoint assembly for the process.
                var bla2 = Assembly.GetCallingAssembly();   // gives you the assembly from which the current method was called.
                var bla3 = Assembly.GetExecutingAssembly(); // gives you the assembly in which the currently executing code is defined
                var bla4 = Assembly.GetAssembly(typeof(OFBaseDomain));  // gives you the assembly in which the specified type is defined.
                var loc = bla4.Location;
                string path2 = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);

                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }


    }
}
