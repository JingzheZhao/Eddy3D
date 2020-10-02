using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EddyLib
{
    //foamToVTK -cellSet Tree_0 -latestTime

    public class TreeObject
    {
        public List<Tree> trees;

        public List<Mesh> treeGeometries;

        public String fullExportString;

        private string topoSetDictPath;

        public TreeObject(OFBaseDomain DOM, OFMeshSettings MeshSettings)
        {
            //this.fvObjectPath = MeshSettings.meshSystemDir + @"\fvOptions";
            this.topoSetDictPath = MeshSettings.meshSystemDir + @"\topoSetDict";
            this.trees = DOM.Trees;

            ExportfvOptionsDict(trees, DOM, MeshSettings);

            ExportTopoSetDict(trees);

            ExportTreeGeometry(trees, MeshSettings);
        }

        public static void RemoveDicts(OFBaseDomain DOM, OFMeshSettings MeshSettings)
        {
            for (int i = 0; i < DOM.BCond.windDirs.Count; i++)
            {
                string simSystemDir = MeshSettings.baseWorkingDir + "\\" + DOM.BCond.windDirs[i] + @"\system\";
                var path = simSystemDir + @"\fvOptions";

                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        public void ExportTreeGeometry(List<Tree> trees, OFMeshSettings MeshSettings)

        {
            for (int i = 0; i < trees.Count; i++)
            {
                STLExport.ExportBinary(MeshSettings.meshStlDir + "Tree_" + i + ".stl", trees[i].treeGeometries);
            }
        }

        public void ExportfvOptionsDict(List<Tree> trees, OFBaseDomain DOM, OFMeshSettings Meshsettings)
        {
            for (int i = 0; i < DOM.BCond.windDirs.Count; i++)
            {
                string simSystemDir = Meshsettings.baseWorkingDir + "\\" + DOM.BCond.windDirs[i] + @"\system\";

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(TreeStringHeader());
                for (int j = 0; j < trees.Count; j++)
                {
                    sb.AppendLine(TreeStringBody(j, trees[j].f, trees[j].d));
                }
                this.fullExportString = sb.ToString();

                if (Directory.Exists(simSystemDir))
                {
                    File.WriteAllText(simSystemDir + @"\fvOptions", fullExportString);
                }
            }
        }

        public void ExportTopoSetDict(List<Tree> trees)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(TopoSetDictStringHeader());
            sb.AppendLine(@"actions
  (");
            for (int i = 0; i < trees.Count; i++)
            {
                sb.AppendLine(TopoSetDictStringBody(i, trees[i].treeGeometries));
            }
            sb.AppendLine(");");
            File.WriteAllText(this.topoSetDictPath, sb.ToString());
        }

        public static string TopoSetDictStringHeader()

        {
            return @"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  3.0.x                                 |
|   \\  /    A nd           | Web:      www.OpenFOAM.org                      |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
            FoamFile
{
                version     2.0;
                format ascii;
    class dictionary;
        object topoSetDict;
    }

    // * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
";
        }

        public static string TopoSetDictStringBody(int id, Mesh tree)

        {
            var pointOutside = new Point3d(tree.GetBoundingBox(false).Max.X - 0.5, tree.GetBoundingBox(false).Max.Y, tree.GetBoundingBox(false).Max.Z);

            StringBuilder sb = new StringBuilder();

            string content = string.Format(@"{{
        name Tree_{0};
        type cellZoneSet;
        action new;
        source surfaceToCell;
        sourceInfo
        {{
                surface triSurfaceMesh;
                file ""./constant/triSurface/Tree_{0}.stl"";
            outsidePoints (({1}));
            includeCut no;
            includeInside yes;
            includeOutside no;
            nearDistance 0.08;
            curvature -100;
        }}
}}
", id, String.Join(" ", EddyLib.Utilities.FormatPV(pointOutside)));

            string content2 = string.Format(@"{{
                name Tree_{0};
                type faceZoneSet;
                action new;
                source surfaceToCell;
                sourceInfo
        {{
                    surface triSurfaceMesh;
                    file ""./constant/triSurface/Tree_0.stl"";
                    outsidePoints (({1}));
                    includeCut no;
                    includeInside yes;
                    includeOutside no;
                    nearDistance 0.08;
                    curvature -100;
                }}
            }}", id, String.Join(" ", Utilities.FormatPV(pointOutside)));

            sb.AppendLine(content);

            //sb.AppendLine(content2);

            return sb.ToString();
        }

        // public static string TreeStringBody(int id, TreeType treeType)
        public static string TreeStringBody(int id, double[] f, double[] d)

        {
            var PorosityCoeffs_D = d;
            var PorosityCoeffs_F = f;

            //var PorosityCoeffs_D = new double[] { 00.0, 00.0, 00.0 };
            //var PorosityCoeffs_F = new double[] { 0.0, 0.0, 0.0 };

            //if (treeType == TreeType.coarse)
            //{
            //    PorosityCoeffs_D = new double[] { 20.0, 20.0, 20.0 };
            //    PorosityCoeffs_F = new double[] { 20.0, 20.0, 20.0 };
            //}
            //else if (treeType == TreeType.medium)
            //{
            //    PorosityCoeffs_D = new double[] { 40.0, 40.0, 40.0 };
            //    PorosityCoeffs_F = new double[] { 40.0, 40.0, 40.0 };
            //}
            //else
            //{
            //    PorosityCoeffs_D = new double[] { 60.0, 60.0, 60.0 };
            //    PorosityCoeffs_F = new double[] { 60.0, 60.0, 60.0 };
            //}

            // https://www.cfd-online.com/Forums/openfoam-solving/78705-darcy-forchheimer-law-specifying-porous-zones.html
            // e1 and e2 are the vectors that are used to specify the porosity.In the porousZones file, you have to specify three components of f and d.The first component is in the direction of e1, the second in the direction of e2 and the third in the direction perpendicular to e1 and e2.An example can be found in tutorials / incompressible / porousSimpleFoam / angledDuctImplicit.

            //Furthermore,
            //d = beta / viscocity[1 / m ^ 2]
            //f = 2 * alpha / density[1 / m]

            StringBuilder sb = new StringBuilder();

            string content = string.Format(@"
    porosity_{0}
    {{
    type explicitPorositySource;

    explicitPorositySourceCoeffs
    {{
    selectionMode cellZone;
    cellZone Tree_{0};

    type DarcyForchheimer;

    f ({1});
    d ({2});

    coordinateSystem
    {{
    type cartesian;
    origin (0 0 0);
    coordinateRotation
    {{
    type axesRotation;
    e1 (1 0 0);
    e2 (0 0 1);
    }}
    }}
    }}
    }}
    ", id, String.Join(" ", PorosityCoeffs_F.Select(p => p.ToString())), String.Join(" ", PorosityCoeffs_D.Select(p => p.ToString())));

            sb.AppendLine(content);
            return sb.ToString();
        }

        public static string TreeStringHeader()

        {
            StringBuilder sb = new StringBuilder();

            string content = @"
/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      / F ield | OpenFOAM: The Open Source CFD Toolbox |
|  \\    / O peration | Version:  dev |
|   \\  / A nd | Web:      www.OpenFOAM.org |
|    \\/ M anipulation |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format ascii;
    class dictionary;
    location    ""constant"";
    object fvOptions;
    }

// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //
";

            sb.AppendLine(content);

            return sb.ToString();
        }
    }

    public enum TreeType
    {
        coarse,

        medium,

        dense
    }

    public class Tree

    {
        public Mesh treeGeometries;

        public TreeType treeType;

        public double[] d;

        public double[] f;

        //public Tree(List<GeometryBase> treeGeometries, TreeType treeType)
        public Tree(List<GeometryBase> treeGeometries, double[] f, double[] d)
        {
            MeshingParameters mp = new MeshingParameters();

            Mesh allTogether = new Mesh();
            List<Mesh> allSeparate = new List<Mesh>();

            foreach (GeometryBase b in treeGeometries)
            {
                if (b.ObjectType == Rhino.DocObjects.ObjectType.Mesh)
                {
                    Mesh obj = (Mesh)b;
                    allTogether.Append(obj);
                    allSeparate.Add(obj);
                }
                else if (b.ObjectType == Rhino.DocObjects.ObjectType.Brep || b.ObjectType == Rhino.DocObjects.ObjectType.Extrusion || b.ObjectType == Rhino.DocObjects.ObjectType.Surface)
                {
                    Brep obj = (Brep)b;
                    var m = Mesh.CreateFromBrep(obj, mp);
                    foreach (Mesh mm in m) allTogether.Append(mm);

                    Mesh meshForMeshList = new Mesh();
                    foreach (Mesh mm in m) meshForMeshList.Append(mm);
                    allSeparate.Add(meshForMeshList);
                }
            }

            this.treeGeometries = allTogether;

            //this.treeType = treeType;

            this.d = d;
            this.f = f;
        }
    }
}