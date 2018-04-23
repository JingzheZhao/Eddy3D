using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SymlinkCreator
{
    class Program
    {
        [DllImport("kernel32.dll")]
        static extern bool CreateSymbolicLink(
                string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }

        static void Main(string[] args)
        {
            //string symbolicLink = @"c:\bar.txt";
            //string fileName = @"c:\temp\foo.txt";

            //using (var writer = File.CreateText(fileName))
            //{
            //    writer.WriteLine("Hello World");
            //}

            string simPath = "";
            string meshPath = "";

            CreateSymbolicLink(simPath, meshPath, SymbolicLink.File);
        }
    }
}

