using EddyLib.Geometry;
using ProtoBuf;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Radiation
{

    public enum RadiationSurfaceType
    {
        Building = 0,
        Ground = 1,
        Vegetation = 2,
        Tree = 3,
        Sky = 4
    }

    public enum SimulationType
    {
        Simulated = 0,
        Ambient = 1,
        TemperatureInput = 2,
        Ignore = 3
    }

    [ProtoContract]
    public class RPolygon
    {
        public double rin = 0.0;
        public double rout = 0.0;
        public double refl = 0.0;

        [ProtoMember(1)]
        public EddyVector Normal { get; set; } = new EddyVector();
        [ProtoMember(2)]
        public EddyPoint Centroid { get; set; } = new EddyPoint();
        [ProtoMember(3)]
        public EddyMesh Mesh { get; set; } = new EddyMesh();
        [ProtoMember(4)]
        public double Area { get; set; } = 0.0;
        [ProtoMember(5)]
        public string Name { get; set; }
        [ProtoMember(6)]
        public RadiationSurfaceType Type { get; set; }
        [ProtoMember(7)]
        public SimulationType SimulationType { get; set; }
        [ProtoMember(8)]
        public double[] VFtoPolys { get; set; }
        [ProtoMember(9)]
        public double SeenByProbes { get; set; } = 0;

        [ProtoMember(10)]
        public int ID { get; set; }


        [ProtoMember(99)]
        public float[] TemperatureOverride { get; set; }

        [ProtoMember(100)]
        public double[] SurfaceTemperature { get; set; }

    }
}
