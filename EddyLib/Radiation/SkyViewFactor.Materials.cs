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
        #region 1. AddMat

        public static Mesh AddMat(string mat, Mesh m)

        {
            string matClean = DeleteComments(mat.Trim());
            string matSingleLine = Regex.Replace(matClean, @"\s+", " ", RegexOptions.Multiline);

            m.UserDictionary.Set("RadMat", matSingleLine);

            return m;
        }

        private static string BuildingGroundTemplate()
        {
            return @"void plastic OutsideGround_10
0
0
5 0.1 0.1 0.1 0 0";
        }

        public static string DeleteComments(string s)
        {
            StringBuilder sb = new StringBuilder();
            string[] lines = s.Split(Environment.NewLine.ToCharArray());
            foreach (var l in lines)
            {
                if (!l.Trim().StartsWith("#")) sb.AppendLine(l);
            }

            return sb.ToString();
        }

        #endregion 1. AddMat
    }
}
