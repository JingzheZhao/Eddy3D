using EddyLib.FunctionObjects;
using EddyLib.Indoor.Dicts;
using Newtonsoft.Json;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace EddyLib.Indoor
{
    public class MomentumSinkIndoor : FunctionObject
    {
        //TEST CODE
        //public Vector3d Ubar { get; set; }

        // only if selectionModel is cellZone

        public MomentumSinkIndoor(Mesh Geometry, string Name)
        {
            this.Name = Name;
            this.Geometry = Geometry;
            //this.Ubar = Ubar;

        }



        public MomentumSinkIndoor()
        {
        }


    }
}
////COPIED CODE FORM MOMENTUM SINK OUTDOOR
//// dp = A *u + B*u^2

//private double[] A = new double[3]; // U

//private double[] B = new double[3]; // U^2

//public double[] D = new double[3]; // u

//public double[] F = new double[3]; // u^2

//public double DimX { get; set; }

//public double DimY { get; set; }

//private double DimZ { get; set; }

//private double[] DimXYZ = new double[3];

////private double nu = 1.5e-05; // kinematic viscosity

//public double rho = 1.2041;  //At 20 °C and 101.325 kPa, dry air has a density of 1.2041 kg/m³

//public double mu = 0.0000181; // dynamic viscosity

//public string AllProperties { get; set; }

//public MomentumSink(GeometryBase Geometries, double[] B, double[] A, string Name)
//{
//    MeshGeo(Geometries);
//    GetDims();

//    for (int unitVec = 0; unitVec < 3; unitVec++)
//    {
//        this.D[unitVec] = A[unitVec] / DimXYZ[unitVec] / this.mu;
//        this.F[unitVec] = B[unitVec] / DimXYZ[unitVec] * 2 / this.rho;
//    }

//    ExportSettings();
//}

//public MomentumSink()
//{
//}

////public MomentumSink Duplicate()
////{
////    MomentumSink dup = new MomentumSink(Geometry, B, A, Name);
////    return dup;
////}

//public class People : MomentumSink

//{
//    public enum TreeType
//    {
//        coarse,

//        medium,

//        dense
//    }

//    // https://www.simscale.com/docs/analysis-types/pedestrian-wind-comfort-analysis/advanced-modelling/;
//    // https://openfoamwiki.net/index.php/DarcyForchheimer

//    public TreeType treeType;

//    private double Cd = 0.2;

//    public double LAI;

//    public double LAD;

//    public People(GeometryBase Geometries, double[] B, double[] A, string Name) : base(Geometries, B, A, Name)
//    {
//        MeshGeo(Geometries);
//        GetDims();

//        for (int unitVec = 0; unitVec < 3; unitVec++)
//        {
//            this.D[unitVec] = A[unitVec] / DimXYZ[unitVec] / this.mu;
//            this.F[unitVec] = B[unitVec] / DimXYZ[unitVec] * 2 / this.rho;
//        }

//        this.LAD = this.B.Average() / (this.rho * this.Cd);
//        this.LAI = this.LAD * DimZ;

//        ExportSettings();
//    }

//    public People(GeometryBase Geometries, double LAI, string Name)
//    {
//        MeshGeo(Geometries);
//        GetDims();

//        //this.treeType = treeType;
//        this.LAD = LAI / DimZ;
//        this.A = new double[] { 0, 0, 0 };
//        this.B = new double[] { this.rho * this.LAD * this.Cd, this.rho * this.LAD * this.Cd, this.rho * this.LAD * this.Cd };

//        this.D = new double[] { 0, 0, 0 };
//        this.F = this.B.Select(x => x * 2 / this.rho).ToArray();

//        ExportSettings();
//    }
//}

//private void GetDims()
//{
//    BoundingBox BBox = Geometry.GetBoundingBox(true);

//    this.DimX = BBox.Max.X - BBox.Min.X;
//    this.DimY = BBox.Max.Y - BBox.Min.Y;
//    this.DimZ = BBox.Max.Z - BBox.Min.Z;
//    this.DimXYZ = new double[3] { DimX, DimY, DimZ };
//}

//public void MeshGeo(GeometryBase b)
//{
//    MeshingParameters mp = new MeshingParameters();

//    Mesh allTogether = new Mesh();

//    if (b.ObjectType == Rhino.DocObjects.ObjectType.Mesh)
//    {
//        Mesh obj = (Mesh)b;
//        allTogether.Append(obj);
//        this.Geometry = allTogether;
//    }
//    else if (b.ObjectType == Rhino.DocObjects.ObjectType.Brep || b.ObjectType == Rhino.DocObjects.ObjectType.Extrusion || b.ObjectType == Rhino.DocObjects.ObjectType.Surface)
//    {
//        Brep obj = (Brep)b;
//        var m = Mesh.CreateFromBrep(obj, mp);
//        foreach (Mesh mm in m) allTogether.Append(mm);

//        this.Geometry = allTogether;
//    }
//}

//private static Dictionary<string, object> DictionaryFromType(object atype)
//{
//    if (atype == null) return new Dictionary<string, object>();
//    Type t = atype.GetType();
//    PropertyInfo[] props = t.GetProperties();
//    Dictionary<string, object> dict = new Dictionary<string, object>();
//    foreach (PropertyInfo prp in props)
//    {
//        object value = prp.GetValue(atype, new object[] { });
//        dict.Add(prp.Name, value);
//    }
//    return dict;
//}

//private void ExportSettings()
//{
//    this.AllProperties = JsonConvert.SerializeObject(DictionaryFromType(this));
//}