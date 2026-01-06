using EddyLib.BCs;
using System;
using System.Text;

namespace EddyLib.Strings

{
    public partial class BCDicts
    {
        #region Generic

        private static double dotCutoff = 0.0;

        public static string ABL(ABL bcond, int d)
        {
            return @"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  v3.0+                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       IOobject;
    location    ""0"";
    object ABLConditions;
        }

// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
Uref		" + Utilities.FormatDouble(bcond.URef) + @";
Zref		" + Utilities.FormatDouble(bcond.zref) + @";
z0 uniform " + Utilities.FormatDouble(bcond.z0) + @";
flowDir (" + Utilities.FormatDouble(bcond.flowDir.X) + " " + Utilities.FormatDouble(bcond.flowDir.Y) + " " + Utilities.FormatDouble(bcond.flowDir.Z) + @");
zDir (0 0 1);
zGround uniform " + Utilities.FormatDouble(bcond.zGround) + @";

// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
";
        }

        public static string InitialConditions(OFBaseDomain DOM, int d)
        {
            return @"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  v3.0+                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       IOobject;
    location    ""0"";
    object initialConditions;
        }

// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
flowVelocity (0 0 0);
pressure    0;
turbulentKE " + Utilities.FormatDouble(Math.Round(DOM.BCond.BCs[d].k, 4)) + @";
turbulentEpsilon " + Utilities.FormatDouble(Math.Round(DOM.BCond.BCs[d].epsilon, 4)) + @";
turbulentOmega	" + Utilities.FormatDouble(Math.Round(DOM.BCond.BCs[d].omega, 4)) + @";
#inputMode		merge;

// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
";
        }

        #endregion Generic
    }
}
