using Newtonsoft.Json;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EddyLib.Indoor
{

   
}

public class IndoorBCs
{
    public MeshFaceNormalList Normals { get; set; }

    public Mesh Geometry { get; set; }

    public string Name { get; set; }

    public string Id { get; set; }

    public class Wall : IndoorBCs
    {
        public double TemperatureK { get; set; }

        public double internalFieldTempK { get; set; }

        public Wall()
        {
        }
        public Wall(Mesh m, double TemperatureC, double internalFieldTempC)
        {
            this.TemperatureK = TemperatureC + 273.15;

            // this needs to be passed differently
            this.internalFieldTempK = internalFieldTempC + 273.15;
            this.Geometry = m;
            this.Normals = m.FaceNormals;
            this.Name = "Wall";
        }
        public Wall Duplicate()
        {
            Wall dup = new Wall(Geometry, TemperatureK, internalFieldTempK);
            return dup;
        }
    }

    public class Inlet : IndoorBCs
    {
        public double TemperatureK { get; set; }


        public Vector3d Velocity { get; set; }
        public Point3d Centroid { get; set; }



        public Inlet()
        {
        }
        public Inlet(Mesh m, double TemperatureC, Vector3d velocity)
        {
            this.TemperatureK = TemperatureC + 273.15;
            this.Velocity = velocity;
            this.Geometry = m;
            this.Centroid = AreaMassProperties.Compute(m).Centroid;
            this.Normals = m.FaceNormals;
            this.Name = "Inlet";
        }

        public Inlet Duplicate()
        {
            Inlet dup = new Inlet(Geometry, TemperatureK, Velocity);
            return dup;
        }
    }

    public class Outlet : IndoorBCs
    {

        public Vector3d Velocity { get; set; }
        public Point3d Centroid { get; set; }

        public Outlet()
        {
        }
        public Outlet(Mesh m,  Vector3d velocity)
        {
            this.Geometry = m;
            this.Normals = m.FaceNormals;
            this.Velocity = velocity;
            this.Centroid = AreaMassProperties.Compute(m).Centroid;
            this.Name = "Outlet";
        }
        public Outlet Duplicate()
        {
            Outlet dup = new Outlet(Geometry,  Velocity);
            return dup;
        }
    }

    internal class Emitter : IndoorBCs
    {
        public Emitter(Mesh m)
        {
            this.Geometry = m;
            this.Normals = m.FaceNormals;
            this.Name = "Emitter";
        }
    }
}
