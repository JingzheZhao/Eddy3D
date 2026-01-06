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
        #region 5. RunRayCast

        //if(Run){
        //  RunRayCastMat(Oct, Pts, Path);
        //    // RunRayCastSurf(Oct, Pts, Path);
        //}

        public enum OSType
        {
            Windows = 0,

            Unix = 1,
        }

        public enum RuntimeType
        {
            NET = 0,

            Mono = 1,
        }

        #endregion 5. RunRayCast
    }
}
