
using EddyLib.Geometry;
using EddyLib.Radiation;

namespace EddyLib
{
    public sealed class PrepareProtoBufSingleton
    {
        private PrepareProtoBufSingleton()
        {
        }

        public static PrepareProtoBufSingleton Instance { get { return Nested.instance; } }

        private class Nested
        {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            static Nested()
            {
                ProtoBuf.Serializer.PrepareSerializer<EddyPoint>();
                ProtoBuf.Serializer.PrepareSerializer<EddyVector>();
                ProtoBuf.Serializer.PrepareSerializer<EddyMesh>();

                ProtoBuf.Serializer.PrepareSerializer<RProbe>();
                ProtoBuf.Serializer.PrepareSerializer<MRT_Simulation_ResultProto>();

            }

            internal static readonly PrepareProtoBufSingleton instance = new PrepareProtoBufSingleton();
        }
    }
}
