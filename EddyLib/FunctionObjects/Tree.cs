using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EddyLib
{
    /// <summary>
    /// Manages tree objects for OpenFOAM porous zone simulations.
    /// </summary>
    public class TreeObject
    {
        /// <summary>List of trees in the domain.</summary>
        public List<Tree> Trees { get; set; }

        /// <summary>Combined geometries of all trees.</summary>
        public List<Mesh> TreeGeometries { get; set; }

        /// <summary>Full fvOptions export string.</summary>
        public string FullExportString { get; private set; }

        private readonly string topoSetDictPath;

        /// <summary>
        /// Creates a TreeObject and exports all necessary OpenFOAM files.
        /// </summary>
        public TreeObject(OFBaseDomain dom, OFMeshSettings meshSettings)
        {
            topoSetDictPath = Path.Combine(meshSettings.meshSystemDir, "topoSetDict");
            Trees = dom.Trees;

            ExportFvOptionsDict(Trees, dom, meshSettings);
            ExportTopoSetDict(Trees, dom.LocationInMesh);
            ExportTreeGeometry(Trees, meshSettings);
        }

        /// <summary>
        /// Removes fvOptions files from all wind direction folders.
        /// </summary>
        public static void RemoveDicts(OFBaseDomain dom, OFMeshSettings meshSettings)
        {
            foreach (var windDir in dom.BCond.WindDirections)
            {
                string fvOptionsPath = Path.Combine(
                    meshSettings.baseWorkingDir,
                    windDir.ToString(),
                    "system",
                    "fvOptions");

                if (File.Exists(fvOptionsPath))
                {
                    File.Delete(fvOptionsPath);
                }
            }
        }

        /// <summary>
        /// Exports tree geometries as STL files.
        /// </summary>
        public void ExportTreeGeometry(List<Tree> trees, OFMeshSettings meshSettings)
        {
            for (int i = 0; i < trees.Count; i++)
            {
                string stlPath = Path.Combine(meshSettings.meshStlDir, $"Tree_{i}.stl");
                STLExport.ExportBinary(stlPath, trees[i].Geometry);
            }
        }

        /// <summary>
        /// Exports fvOptions dictionary for all wind directions.
        /// </summary>
        public void ExportFvOptionsDict(List<Tree> trees, OFBaseDomain dom, OFMeshSettings meshSettings)
        {
            foreach (var windDir in dom.BCond.WindDirections)
            {
                string systemDir = Path.Combine(meshSettings.baseWorkingDir, windDir.ToString(), "system");

                var sb = new StringBuilder();
                sb.AppendLine(FvOptionsHeader());

                for (int i = 0; i < trees.Count; i++)
                {
                    sb.AppendLine(FvOptionsBody(i, trees[i].F, trees[i].D));
                }

                FullExportString = sb.ToString();

                if (Directory.Exists(systemDir))
                {
                    File.WriteAllText(Path.Combine(systemDir, "fvOptions"), FullExportString);
                }
            }
        }

        /// <summary>
        /// Exports topoSetDict for tree cell zones.
        /// </summary>
        public void ExportTopoSetDict(List<Tree> trees, Point3d locationInMesh)
        {
            var sb = new StringBuilder();
            sb.AppendLine(TopoSetHeader());
            sb.AppendLine("actions\n(");

            for (int i = 0; i < trees.Count; i++)
            {
                sb.AppendLine(TopoSetBody(i, locationInMesh));
            }

            sb.AppendLine(");");
            File.WriteAllText(topoSetDictPath, sb.ToString());
        }

        #region OpenFOAM Dictionary Templates

        private static string TopoSetHeader() => @"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  3.0.x                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       dictionary;
    object      topoSetDict;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
";

        private static string TopoSetBody(int id, Point3d locationInMesh)
        {
            string outsidePoint = Utilities.FormatPV(locationInMesh);
            return $@"{{
    name Tree_{id};
    type cellZoneSet;
    action new;
    source surfaceToCell;
    sourceInfo
    {{
        surface triSurfaceMesh;
        file ""./constant/triSurface/Tree_{id}.stl"";
        outsidePoints (({outsidePoint}));
        includeCut yes;
        includeInside yes;
        includeOutside no;
        nearDistance 0.08;
        curvature -100;
    }}
}}";
        }

        private static string FvOptionsHeader() => @"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  dev                                   |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       dictionary;
    location    ""constant"";
    object      fvOptions;
}
// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
";

        private static string FvOptionsBody(int id, double[] f, double[] d)
        {
            string fStr = string.Join(" ", f.Select(v => v.ToString()));
            string dStr = string.Join(" ", d.Select(v => v.ToString()));

            return $@"porosity_{id}
{{
    type explicitPorositySource;
    explicitPorositySourceCoeffs
    {{
        selectionMode cellZone;
        cellZone Tree_{id};
        type DarcyForchheimer;
        f ({fStr});
        d ({dStr});
        coordinateSystem
        {{
            type cartesian;
            origin (0 0 0);
            coordinateRotation
            {{
                type axesRotation;
                e1 (1 0 0);
                e2 (0 1 0);
            }}
        }}
    }}
}}";
        }

        #endregion
    }

    /// <summary>
    /// Tree density type for predefined LAI values.
    /// </summary>
    public enum TreeType
    {
        coarse,
        medium,
        dense
    }

    /// <summary>
    /// Represents a tree as a porous zone for CFD simulations.
    /// Uses Darcy-Forchheimer model with LAI/LAD parameters.
    /// </summary>
    public class Tree
    {
        #region Constants

        private const double Cd = 0.2;  // Drag coefficient for vegetation

        #endregion

        #region Properties

        /// <summary>Mesh representation of tree geometry.</summary>
        public Mesh Geometry { get; private set; }

        /// <summary>Tree density type.</summary>
        public TreeType TreeType { get; set; }

        /// <summary>Viscous resistance coefficients (d) [1/m²].</summary>
        public double[] D { get; private set; } = new double[3];

        /// <summary>Inertial resistance coefficients (f) [1/m].</summary>
        public double[] F { get; private set; } = new double[3];

        /// <summary>Bounding box dimensions [X, Y, Z].</summary>
        public double[] Dimensions { get; private set; } = new double[3];

        /// <summary>Leaf Area Index (total leaf area / ground area).</summary>
        public double LAI { get; private set; }

        /// <summary>Leaf Area Density (leaf area / volume) [1/m].</summary>
        public double LAD { get; private set; }

        /// <summary>JSON serialization of properties.</summary>
        public string AllProperties { get; private set; }

        #endregion

        /// <summary>
        /// Creates a tree from explicit pressure drop coefficients.
        /// </summary>
        public Tree(GeometryBase geometry, double[] B, double[] A)
        {
            Geometry = GeometryHelpers.ToMesh(geometry);
            Dimensions = GeometryHelpers.GetDimensionsArray(Geometry);

            CalculateCoefficients(A, B);

            LAD = B.Average() / (AirProperties.Rho * Cd);
            LAI = LAD * Dimensions[2];  // Z dimension

            AllProperties = SerializationHelpers.ToJson(this);
        }

        /// <summary>
        /// Creates a tree from LAI (Leaf Area Index).
        /// </summary>
        public Tree(GeometryBase geometry, double lai)
        {
            Geometry = GeometryHelpers.ToMesh(geometry);
            Dimensions = GeometryHelpers.GetDimensionsArray(Geometry);

            LAI = lai;
            LAD = lai / Dimensions[2];  // Z dimension

            // Calculate B from LAI: B = rho * LAD * Cd
            double b = AirProperties.Rho * LAD * Cd;

            // No viscous term for vegetation (D = 0)
            D = new double[] { 0, 0, 0 };
            // f = B * 2 / rho
            F = new double[] { b * 2 / AirProperties.Rho, b * 2 / AirProperties.Rho, b * 2 / AirProperties.Rho };

            AllProperties = SerializationHelpers.ToJson(this);
        }

        private void CalculateCoefficients(double[] A, double[] B)
        {
            for (int i = 0; i < 3; i++)
            {
                D[i] = A[i] / Dimensions[i] / AirProperties.Mu;
                F[i] = B[i] / Dimensions[i] * 2 / AirProperties.Rho;
            }
        }
    }
}