using Rhino.Geometry;
using Rhino.Geometry.Collections;

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

        public Point3d Centroid { get; set; }

        public class Wall : IndoorBC
        {
            public double TemperatureK { get; set; }

            public Wall()
            {
            }

            public Wall(Mesh m, int refinementLevel, double TemperatureC)
            {
                this.TemperatureK = TemperatureC + 273.15;
                m.Normals.ComputeNormals();
                this.Geometry = m;

                this.Normals = m.FaceNormals;
                this.Name = "Wall";
                this.refinementLevel = refinementLevel;
            }

            public Wall Duplicate()
            {
                Wall dup = new Wall(Geometry, refinementLevel, TemperatureK);
                return dup;
            }
        }

        public class Inlet : IndoorBC
        {
            public double TemperatureK { get; set; }

            public Vector3d Velocity { get; set; }

            public Inlet()
            {
            }

            public Inlet(Mesh m, double TemperatureC, int refinementLevel, Vector3d velocity)
            {
                this.TemperatureK = TemperatureC + 273.15;
                this.Velocity = velocity;
                m.Normals.ComputeNormals();
                this.Geometry = m;
                this.Centroid = AreaMassProperties.Compute(m).Centroid;
                this.Normals = m.FaceNormals;
                this.Name = "Inlet";
                this.OFGeometryType = "triSurfaceMesh";
                this.bcType = BCType.inlet;
                this.refinementLevel = refinementLevel;
            }

            public Inlet Duplicate()
            {
                Inlet dup = new Inlet(Geometry, TemperatureK, refinementLevel, Velocity);
                return dup;
            }
        }

        public class Outlet : IndoorBC
        {
            public Outlet()
            {
            }

            public Outlet(Mesh m, int refinementLevel)
            {
                m.Normals.ComputeNormals();
                this.Geometry = m;
                this.Centroid = AreaMassProperties.Compute(m).Centroid;
                this.Normals = m.FaceNormals;
                this.Name = "Outlet";
                this.bcType = BCType.outlet;
                this.refinementLevel = refinementLevel;
            }

            public Outlet Duplicate()
            {
                Outlet dup = new Outlet(Geometry, refinementLevel);
                return dup;
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
                this.Centroid = AreaMassProperties.Compute(m).Centroid;
                this.Normals = m.FaceNormals;
                this.Name = "Emitter";
                this.bcType = BCType.emitter;
                this.refinementLevel = refinementLevel;
            }
        }
    }
}