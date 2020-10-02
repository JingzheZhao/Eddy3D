using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EddyLib.Indoor.Dicts;

namespace EddyLib.Indoor.Dicts
{
    internal class SurfaceFeatureExtractDict : GenericDict
    {
        public string fullDict;

        public SurfaceFeatureExtractDict()
        {
            this.fullDict = @"
/*---------------------------------------------------------------------------*\
|=========                 |                                                  |
|\\      /   F ield        | OpenFOAM: The Open Source CFD Toolbox            |
| \\    /    O peration    | Version:  1912                                   |
|  \\  /     A nd          | Web:      www.OpenFOAM.org                       |
|   \\/      M anipulation |                                                  |
\*---------------------------------------------------------------------------*/

FoamFile
{
    version         1912;
    format          ascii;
    class           dictionary;
    location        ""system"";
    object
surfaceFeatureExtractDict;

	    extractFromSurfaceCoeffs
    {
        // Mark edges whose adjacent surface normals are at an angle less than includedAngle as features
        // - 0 : selects no edges
        // - 180: selects all edges
        includedAngle   180;
        geometricTestOnly yes;
    }
}

Inlet*.stl
{
    extractionMethod extractFromSurface;
    includedAngle   180.00;
    geometricTestOnly yes;
    intersectionMethod none;
    writeObj        no;
	    extractFromSurfaceCoeffs
    {
        // Mark edges whose adjacent surface normals are at an angle less than includedAngle as features
        // - 0 : selects no edges
        // - 180: selects all edges
        includedAngle   180;
        geometricTestOnly yes;
    }
}

Outlet*.stl
{
    extractionMethod extractFromSurface;
    includedAngle   180.00;
    geometricTestOnly yes;
    intersectionMethod none;
    writeObj        no;
	    extractFromSurfaceCoeffs
    {
        // Mark edges whose adjacent surface normals are at an angle less than includedAngle as features
        // - 0 : selects no edges
        // - 180: selects all edges
        includedAngle   180;
        geometricTestOnly yes;
    }
}

Walls*.stl
{
    extractionMethod extractFromSurface;
    includedAngle   180.00;
    geometricTestOnly yes;
    intersectionMethod none;
    writeObj        no;
	    extractFromSurfaceCoeffs
    {
        // Mark edges whose adjacent surface normals are at an angle less than includedAngle as features
        // - 0 : selects no edges
        // - 180: selects all edges
        includedAngle   180;
        geometricTestOnly yes;
    }
}

";
        }
    }
}