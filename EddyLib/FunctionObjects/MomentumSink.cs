using EddyLib.Indoor.Dicts;
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
    public class MomentumSinkDict : FunctionObjectDictInternal
    {
        public string fullExportString { get; set; }

        private string topoSetDictPath { get; set; }

        // Trees
        public MomentumSinkDict(OFBaseDomain DOM, OFMeshSettings MeshSettings)
        {
            //this.fvObjectPath = MeshSettings.meshSystemDir + @"\fvOptions";
            this.topoSetDictPath = MeshSettings.meshSystemDir + @"\topoSetDict";
            //this.MomentumSinks = DOM.Trees;

            ExportfvOptionsDict(DOM.Trees, DOM, MeshSettings);

            ExportTopoSetDict(DOM.Trees, this.topoSetDictPath);

            ExportMomentumSinkGeometry(DOM.Trees, MeshSettings.meshStlDir);
        }

        // Trees
        public MomentumSinkDict(Indoor.IndoorDomain IndoorDomain)
        {
            //this.fvObjectPath = MeshSettings.meshSystemDir + @"\fvOptions";
            this.topoSetDictPath = IndoorDomain.WorkingDir + @"\system" + @"\topoSetDict";
            //this.MomentumSinks = IndoorDomain.MomentumSinks;

            ExportfvOptionsDict(IndoorDomain.WorkingDir);

            ExportTopoSetDict(IndoorDomain.MomentumSinks, this.topoSetDictPath);

            ExportMomentumSinkGeometry(IndoorDomain.MomentumSinks, IndoorDomain.WorkingDir + @"\system\");
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

        public void ExportMomentumSinkGeometry(List<MomentumSink> trees, string meshStlDir)

        {
            for (int i = 0; i < trees.Count; i++)
            {
                STLExport.ExportBinary(meshStlDir + "MS_" + i + ".stl", trees[i].Geometry);
            }
        }

        public void ExportfvOptionsDict(List<MomentumSink> trees, OFBaseDomain DOM, OFMeshSettings Meshsettings)
        {
            for (int i = 0; i < DOM.BCond.windDirs.Count; i++)
            {
                string simSystemDir = Meshsettings.baseWorkingDir + "\\" + DOM.BCond.windDirs[i] + @"\system\";

                StringBuilder sb = new StringBuilder();
                sb.AppendLine(TreeStringHeader());
                foreach (MomentumSink ms in trees)
                {
                    sb.AppendLine(TreeStringBody(ms));
                }
                this.fullExportString = sb.ToString();

                if (Directory.Exists(simSystemDir))
                {
                    File.WriteAllText(simSystemDir + @"\fvOptions", fullExportString);
                }
            }
        }

        public void ExportfvOptionsDict(string simSystemDir)
        {
            if (Directory.Exists(simSystemDir))
            {
                File.WriteAllText(simSystemDir + @"\fvOptions", fullExportString);
            }
        }

        public void ExportTopoSetDict(List<MomentumSink> momentumSink, string topoSetDictPath)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(TopoSetDictStringHeader());
            sb.AppendLine(@"actions
  (");
            foreach (MomentumSink ms in momentumSink)
            {
                sb.AppendLine(TopoSetDictStringBody(ms));
            }
            sb.AppendLine(");");

            File.WriteAllText(topoSetDictPath, sb.ToString());
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

        public static string TopoSetDictStringBody(MomentumSink ms)

        {
            var bb = ms.Geometry.GetBoundingBox(true);
            var pointOutside = new Point3d(bb.Max.X, bb.Max.Y, bb.Max.Z + 0.4);

            StringBuilder sb = new StringBuilder();

            string content = string.Format(@"{{
        name MS_{0};
        type cellZoneSet;
        action new;
        source surfaceToCell;
        sourceInfo
        {{
                surface triSurfaceMesh;
                file ""./constant/triSurface/MS_{0}.stl"";
            outsidePoints (({1}));
            includeCut yes;
            includeInside yes;
            includeOutside no;
            nearDistance 0.08;
            curvature -100;
        }}
}}
", ms.Name, String.Join(" ", EddyLib.Utilities.FormatPV(pointOutside)));

            string content2 = string.Format(@"{{
                name MS_{0};
                type faceZoneSet;
                action new;
                source surfaceToCell;
                sourceInfo
        {{
                    surface triSurfaceMesh;
                    file ""./constant/triSurface/MS_0.stl"";
                    outsidePoints (({1}));
                    includeCut yes;
                    includeInside yes;
                    includeOutside no;
                    nearDistance 0.08;
                    curvature -100;
                }}
            }}", ms.Name, String.Join(" ", Utilities.FormatPV(pointOutside)));

            sb.AppendLine(content);

            //sb.AppendLine(content2);

            return sb.ToString();
        }

        public static string TreeStringBody(MomentumSink ms)

        {
            //var PorosityCoeffs_D = d;
            //var PorosityCoeffs_F = f;

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
    cellZone MS_{0};

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
    ", ms.Name, String.Join(" ", ms.F.Select(p => p.ToString())), String.Join(" ", ms.D.Select(p => p.ToString())));

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

    public class MomentumSink

    {
        public string Name;

        public Mesh Geometry;

        // dp = A *u + B*u^2

        private double[] A = new double[3]; // U

        private double[] B = new double[3]; // U^2

        public double[] D = new double[3]; // u

        public double[] F = new double[3]; // u^2

        public double DimX { get; set; }

        public double DimY { get; set; }

        public double DimZ { get; set; }

        public double[] DimXYZ = new double[3];

        //private double nu = 1.5e-05; // kinematic viscosity

        private double rho = 1.2041;  //At 20 °C and 101.325 kPa, dry air has a density of 1.2041 kg/m³

        private double mu = 0.0000181; // dynamic viscosity

        public string AllProperties { get; set; }

        public MomentumSink(GeometryBase Geometries, double[] B, double[] A, string Name)
        {
            MeshGeo(Geometries);
            GetDims();

            for (int unitVec = 0; unitVec < 3; unitVec++)
            {
                this.D[unitVec] = A[unitVec] / DimXYZ[unitVec] / this.mu;
                this.F[unitVec] = B[unitVec] / DimXYZ[unitVec] * 2 / this.rho;
            }

            ExportSettings();
        }

        public MomentumSink()
        {
        }

        public MomentumSink Duplicate()
        {
            MomentumSink dup = new MomentumSink(Geometry, B, A, Name);
            return dup;
        }

        public class Tree : MomentumSink

        {
            public enum TreeType
            {
                coarse,

                medium,

                dense
            }

            // https://www.simscale.com/docs/analysis-types/pedestrian-wind-comfort-analysis/advanced-modelling/;
            // https://openfoamwiki.net/index.php/DarcyForchheimer

            public TreeType treeType;

            private double Cd = 0.2;

            public double LAI;

            public double LAD;

            public Tree(GeometryBase Geometries, double[] B, double[] A, string Name) : base(Geometries, B, A, Name)
            {
                MeshGeo(Geometries);
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

            public Tree(GeometryBase Geometries, double LAI, string Name)
            {
                MeshGeo(Geometries);
                GetDims();

                //this.treeType = treeType;
                this.LAD = LAI / DimZ;
                this.A = new double[] { 0, 0, 0 };
                this.B = new double[] { this.rho * this.LAD * this.Cd, this.rho * this.LAD * this.Cd, this.rho * this.LAD * this.Cd };

                this.D = new double[] { 0, 0, 0 };
                this.F = this.B.Select(x => x * 2 / this.rho).ToArray();

                ExportSettings();
            }
        }

        private void GetDims()
        {
            BoundingBox BBox = Geometry.GetBoundingBox(true);

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
                this.Geometry = allTogether;
            }
            else if (b.ObjectType == Rhino.DocObjects.ObjectType.Brep || b.ObjectType == Rhino.DocObjects.ObjectType.Extrusion || b.ObjectType == Rhino.DocObjects.ObjectType.Surface)
            {
                Brep obj = (Brep)b;
                var m = Mesh.CreateFromBrep(obj, mp);
                foreach (Mesh mm in m) allTogether.Append(mm);

                this.Geometry = allTogether;
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