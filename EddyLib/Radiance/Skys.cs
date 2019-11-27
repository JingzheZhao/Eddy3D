using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Radiance
{

      public enum SkySubdivision { 
        r1,
        r2,
        r3,
        r4,
        r5,
        r6
        }
    public class Skys
    {
      

        public static void Write(string path, SkySubdivision R)
        {
            string sky = @"
#@rfluxmtx u=+Y h=u
void glow groundglow
0
0
4 1 1 1 0

groundglow source ground
0
0
4 0 0 -1 180


#@rfluxmtx u=+Y h="+R.ToString()+@"
void glow skyglow
0
0
4 1 1 1 0

skyglow source skydome
0
0
4 0 0 1 180
";

            File.WriteAllText(path, sky);

        }

    }
}
