using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EddyLib.Radiation
{
    public partial class SkyViewFactor
    {
        #region 6. LoadResultFile

        public static int[] LoadResultFile(int HCnt, string Path, bool Run)

        {
            if (!Run) { }

            var lines = File.ReadAllLines(Path);

            var ptCnt = lines.Length / HCnt;
            var result = new int[ptCnt];

            //A = "Lines: " + lines.Length + " Points: " + ptCnt;
            if (lines.Length == 0) { }

            int lindex = 0;
            for (int pt = 0; pt < ptCnt; pt++)
            {
                for (int h = 0; h < HCnt; h++)
                {
                    // var m = Regex.Match(lines[lindex].Trim(), @"^\d");
                    var m = lines[lindex].Trim().StartsWith("*");

                    if (m)
                    {
                        //if(m.Success) {
                        result[pt]++;
                    }

                    lindex++;
                }
            }

            return result;
        }

        #endregion 6. LoadResultFile
    }
}
