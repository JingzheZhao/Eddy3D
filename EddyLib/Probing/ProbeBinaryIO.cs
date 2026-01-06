using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;

namespace EddyLib
{
    internal static class ProbeBinaryIO
    {
        internal static void WriteScalars(string path, GH_Number[] values)
        {
            double[] scalars = Array.ConvertAll(values, new Converter<GH_Number, double>(GrasshopperConversions.GH_NumberToDouble));
            RadianceFiles.writeBinScalars(path, scalars);
        }

        internal static void WriteVectors(string path, GH_Vector[] values)
        {
            Vector3d[] vectors = Array.ConvertAll(values, new Converter<GH_Vector, Vector3d>(GrasshopperConversions.GH_VectorToVector3d));
            RadianceFiles.writeBinVectors(path, vectors);
        }

        internal static GH_Number[] LoadScalars(string path)
        {
            var scalars = RadianceFiles.loadBinScalars(path);
            return Array.ConvertAll(scalars, new Converter<double, GH_Number>(GrasshopperConversions.DoubleToGH_Number));
        }

        internal static GH_Vector[] LoadVectors(string path)
        {
            var vectors = RadianceFiles.loadBinVectors(path);
            return Array.ConvertAll(vectors, new Converter<Vector3d, GH_Vector>(GrasshopperConversions.Vector3dToGH_Vector));
        }
    }
}
