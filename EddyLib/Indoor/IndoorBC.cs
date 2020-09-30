using EddyLib.BCs;
using EddyLib.Indoor;
using Newtonsoft.Json;
using Rhino.Geometry;
using Rhino.Geometry.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EddyLib.Indoor
{
    public class IndoorBC
    {
        public MeshFaceNormalList Normals { get; set; }

        public enum BCType
        {
            inlet,

            outlet,

            wall,

            emitter
        }

        public BCType bcType { get; set; }

        public Mesh Geometry { get; set; }

        public string Name { get; set; }

        public string OFGeometryType { get; set; }

        public int refinementLevel { get; set; }

        public string Id { get; set; }

        public class Wall : IndoorBC
        {
            public double TemperatureK { get; set; }

            public double internalFieldTempK { get; set; }

            public Wall()
            {
            }

            public Wall(Mesh m, double TemperatureC, int refinementLevel, double internalFieldTempC)
            {
                this.TemperatureK = TemperatureC + 273.15;

                // this needs to be passed differently
                this.internalFieldTempK = internalFieldTempC + 273.15;
                this.Geometry = m;
                this.Normals = m.FaceNormals;
                this.Name = "Wall";
                this.refinementLevel = refinementLevel;
            }
        }

        public class Inlet : IndoorBC
        {
            public double TemperatureK { get; set; }

            public Vector3d velocity { get; set; }

            public Inlet()
            {
            }

            public Inlet(Mesh m, double TemperatureC, int refinementLevel, Vector3d velocity)
            {
                this.TemperatureK = TemperatureC + 273.15;
                this.velocity = velocity;
                this.Geometry = m;
                this.Normals = m.FaceNormals;
                this.Name = "Inlet";
                this.OFGeometryType = "triSurfaceMesh";
                this.bcType = BCType.inlet;
                this.refinementLevel = refinementLevel;
            }
        }

        public class Outlet : IndoorBC
        {
            public Outlet(int refinementLevel)
            {
            }

            public Outlet(Mesh m)
            {
                this.Geometry = m;
                this.Normals = m.FaceNormals;
                this.Name = "Outlet";
                this.bcType = BCType.outlet;
                this.refinementLevel = refinementLevel;
            }
        }

        internal class Emitter : IndoorBC
        {
            public Emitter()
            {
            }

            public Emitter(Mesh m, int refinementLevel)
            {
                this.Geometry = m;
                this.Normals = m.FaceNormals;
                this.Name = "Emitter";
                this.bcType = BCType.emitter;
                this.refinementLevel = refinementLevel;
            }
        }
    }
}