using System.Runtime.InteropServices;
using System.IO;

namespace Eddy
{
    class SymlinkCreator
    {
       

        public static void Create(string simDir, string meshDir)
        {
            string strCmdText;
            
            strCmdText = "/c MKLINK /J "+ "\"" + simDir + "\"" + " "  +"\""+ meshDir + "\"";
            System.Diagnostics.Process.Start("CMD.exe", strCmdText);
            
           
            
        }
    }
}