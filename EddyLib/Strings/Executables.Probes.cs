using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Text;

namespace EddyLib.Strings
{
    /// <summary>
    /// OpenFOAM probe and topoSet templates.
    /// </summary>
    public partial class OFExecDicts
    {
        /// <summary>
        /// Generates topoSetDict for evaluation patches.
        /// </summary>
        public static string TopoSetDict(List<Mesh> evaluationTopology)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      / F ield | OpenFOAM: The Open Source CFD Toolbox |
|  \\    / O peration | Version:  5 |
|   \\  / A nd | Web:      www.OpenFOAM.org |
|    \\/ M anipulation |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
            version     2.0;
            format ascii;
    class dictionary;
    location    ""system"";
    object topoSetDict;
}

        // * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

        actions
        (");

            for (int i = 0; i < evaluationTopology.Count; i++)
            {
                sb.Append(@"
{
            name patch" + i + @";
            type faceZoneSet;
            action new;
            source searchableSurfaceToFaceZone;
            sourceInfo
        {
                surface triSurfaceMesh;
                name patch" + i + @".stl;
            }
        }

        {
            name surfaceSlaveCells;
            name surfaceSlaveCells;
            type cellSet;
            action new;
            source faceZoneToCell;
            sourceInfo
                {
                name patch" + i + @";
                option slave;
            }
        }
            ");
            }
            sb.Append(@");

        // ************************************************************************* //");

            return sb.ToString();
        }

        /// <summary>
        /// Generates probe locations for all fields.
        /// </summary>
        public static string SampleProbesAllFields(List<Point3d> listOfPoints, string ProbeName, string InterpolationScheme)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"/*--------------------------------*- C++ -*----------------------------------*\
  | =========                 |                                                 |
  | \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
  |  \\    /   O peration     | Version:  12                                    |
  |   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
  |    \\/     M anipulation  |                                                 |
  \*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       dictionary;
    location    ""system"";
    object      " + ProbeName + @";
}

" + ProbeName + @"
{
    type probes;
    libs ("" + OpenFoamLibraryNames.Name(""sampling"") + @"");
    includeOutOfBounds true;
    verbose false;
    writeControl writeTime;

                interpolationScheme " + InterpolationScheme + @";

                setFormat csv;

                fields (U p total(p)_coeff epsilon omega k nut phi aoa);

                probeLocations
                  (");
            sb.Append(Environment.NewLine);
            for (int i = 0; i < listOfPoints.Count; i++)
            {
                sb.Append(@"(" + Utilities.FormatPV(listOfPoints[i]) + @")");
                sb.Append(Environment.NewLine);
            }

            sb.Append(@");
        }

            // ************************************************************************* //");

            return sb.ToString();
        }

        /// <summary>
        /// Generates probe locations for a specific field.
        /// </summary>
        public static string SampleProbes(List<Point3d> listOfPoints, OFField ofField)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(@"/*--------------------------------*- C++ -*----------------------------------*\
  | =========                 |                                                 |
  | \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
  |  \\    /   O peration     | Version:  12                                    |
  |   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
  |    \\/     M anipulation  |                                                 |
  \*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       dictionary;
    location    ""system"";
    object      " + ofField.ProbeName + @";
}

" + ofField.ProbeName + @"
{
                type probes;
                libs (""" + OpenFoamLibraryNames.Name("libsampling") + @""");
                includeOutOfBounds true;
                verbose false;
                writeControl writeTime;

                interpolationScheme " + ofField.InterpolationScheme + @";

                setFormat csv;

                fields (" + ofField.FieldName + @");

                probeLocations
                  (");
            sb.Append(Environment.NewLine);
            for (int i = 0; i < listOfPoints.Count; i++)
            {
                sb.Append(@"(" + Utilities.FormatPV(listOfPoints[i]) + @")");
                sb.Append(Environment.NewLine);
            }

            sb.Append(@");
        }

            // ************************************************************************* //");

            return sb.ToString();
        }
    }
}
