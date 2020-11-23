using Newtonsoft.Json;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
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

            ExportTopoSetDict(trees, DOM.LocationInMesh);

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
                    sb.AppendLine(TreeStringBody(j, trees[j].F, trees[j].D));
                }
                this.fullExportString = sb.ToString();

                if (Directory.Exists(simSystemDir))
                {
                    File.WriteAllText(simSystemDir + @"\fvOptions", fullExportString);
                }
            }
        }

        public void ExportTopoSetDict(List<Tree> trees, Point3d locationInMesh)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(TopoSetDictStringHeader());
            sb.AppendLine(@"actions
  (");
            for (int i = 0; i < trees.Count; i++)
            {
                sb.AppendLine(TopoSetDictStringBody(i, trees[i].treeGeometries, locationInMesh));
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

        public static string TopoSetDictStringBody(int id, Mesh tree, Point3d locationInMesh)

        {
            var pointOutside = locationInMesh;

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
            includeCut yes;
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
                    includeCut yes;
                    includeInside yes;
                    includeOutside no;
                    nearDistance 0.08;
                    curvature -100;
                }}
            }}", id, String.Join(" ", EddyLib.Utilities.FormatPV(pointOutside)));

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
    e2 (0 1 0);
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
        // https://www.simscale.com/docs/analysis-types/pedestrian-wind-comfort-analysis/advanced-modelling/;

        // https://openfoamwiki.net/index.php/DarcyForchheimer

        public Mesh treeGeometries;

        public TreeType treeType;

        // dp = A *u + B*u^2

        private double[] A = new double[3]; // U

        private double[] B = new double[3]; // U^2

        public double[] D = new double[3]; // u

        public double[] F = new double[3]; // u^2

        public double DimX;

        public double DimY;

        public double DimZ;

        public double[] DimXYZ = new double[3];

        //private double nu = 1.5e-05; // kinematic viscosity

        private double rho = 1.2041;  //At 20 °C and 101.325 kPa, dry air has a density of 1.2041 kg/m³

        private double mu = 0.0000181; // dynamic viscosity

        private double Cd = 0.2;

        public double LAI;

        public double LAD;

        public string AllProperties;

        //public Tree(List<GeometryBase> treeGeometries, TreeType treeType)
        public Tree(GeometryBase treeGeometries, double[] B, double[] A)
        {
            MeshGeo(treeGeometries);
            GetDims();

            for (int unitVec = 0; unitVec < 3; unitVec++)
            {
                this.D[unitVec] = A[unitVec] / DimXYZ[unitVec] / this.mu;
                this.F[unitVec] = B[unitVec] / DimXYZ[unitVec] * 2 / this.rho;
            }

            this.LAD = this.B.Average() / (this.rho * this.Cd);
            this.LAI = this.LAD * DimZ;

            ExportSettings();
        }

        public Tree(GeometryBase treeGeometries, double LAI)
        {
            MeshGeo(treeGeometries);
            GetDims();

            //this.treeType = treeType;
            this.LAD = LAI / DimZ;
            this.A = new double[] { 0, 0, 0 };
            this.B = new double[] { this.rho * this.LAD * this.Cd, this.rho * this.LAD * this.Cd, this.rho * this.LAD * this.Cd };

            this.D = new double[] { 0, 0, 0 };
            this.F = this.B.Select(x => x * 2 / this.rho).ToArray();

            ExportSettings();
        }

        private void GetDims()
        {
            BoundingBox BBox = treeGeometries.GetBoundingBox(true);

            this.DimX = BBox.Max.X - BBox.Min.X;
            this.DimY = BBox.Max.Y - BBox.Min.Y;
            this.DimZ = BBox.Max.Z - BBox.Min.Z;
            this.DimXYZ = new double[3] { DimX, DimY, DimZ };
        }

        private void MeshGeo(GeometryBase b)
        {
            MeshingParameters mp = new MeshingParameters();

            Mesh allTogether = new Mesh();

            if (b.ObjectType == Rhino.DocObjects.ObjectType.Mesh)
            {
                Mesh obj = (Mesh)b;
                allTogether.Append(obj);
                this.treeGeometries = allTogether;
            }
            else if (b.ObjectType == Rhino.DocObjects.ObjectType.Brep || b.ObjectType == Rhino.DocObjects.ObjectType.Extrusion || b.ObjectType == Rhino.DocObjects.ObjectType.Surface)
            {
                Brep obj = (Brep)b;
                var m = Mesh.CreateFromBrep(obj, mp);
                foreach (Mesh mm in m) allTogether.Append(mm);

                this.treeGeometries = allTogether;
            }
        }

        private static Dictionary<string, object> DictionaryFromType(object atype)
        {
            if (atype == null) return new Dictionary<string, object>();
            Type t = atype.GetType();
            PropertyInfo[] props = t.GetProperties();
            Dictionary<string, object> dict = new Dictionary<string, object>();
            foreach (PropertyInfo prp in props)
            {
                object value = prp.GetValue(atype, new object[] { });
                dict.Add(prp.Name, value);
            }
            return dict;
        }

        private void ExportSettings()
        {
            this.AllProperties = JsonConvert.SerializeObject(DictionaryFromType(this));
        }

        //public override string ToString()
        //{
        //    StringBuilder sb = new StringBuilder();

        //    sb.AppendLine(AllProperties);

        //    return sb.ToString();
        //}
    }
}