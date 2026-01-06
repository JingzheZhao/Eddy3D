using Rhino.Geometry;
using System.Text;

namespace EddyLib.OpenFOAM
{
    /// <summary>
    /// Serializes cylindrical domain meshes to OpenFOAM blockMeshDict format.
    /// Extracted from OFCylDomain for modularity and portability.
    /// </summary>
    public class BlockMeshDictSerializer
    {
        private readonly Mesh _domainMesh;
        private readonly Mesh _perimBottom;
        private readonly Mesh _coreBottom;
        private readonly Mesh _perimTop;
        private readonly Mesh _coreTop;
        private readonly Mesh _sides;
        private readonly int _divPerim;
        private readonly int _divisionsX;
        private readonly int _divisionsZ;
        private readonly double _gradingPerim;

        /// <summary>
        /// Creates a new BlockMeshDict serializer.
        /// </summary>
        public BlockMeshDictSerializer(
            Mesh domainMesh,
            Mesh perimBottom,
            Mesh coreBottom,
            Mesh perimTop,
            Mesh coreTop,
            Mesh sides,
            int divPerim,
            int divisionsX,
            int divisionsZ,
            double gradingPerim = 1.0)
        {
            _domainMesh = domainMesh;
            _perimBottom = perimBottom;
            _coreBottom = coreBottom;
            _perimTop = perimTop;
            _coreTop = coreTop;
            _sides = sides;
            _divPerim = divPerim;
            _divisionsX = divisionsX;
            _divisionsZ = divisionsZ;
            _gradingPerim = gradingPerim;
        }

        /// <summary>
        /// Generates the complete blockMeshDict content.
        /// </summary>
        public string Serialize()
        {
            var sb = new StringBuilder();

            sb.AppendLine(GetHeader());
            sb.AppendLine("vertices");
            sb.AppendLine("(");
            sb.AppendLine(SerializeVertices());
            sb.AppendLine(");");
            sb.AppendLine();
            sb.AppendLine("blocks");
            sb.AppendLine("(");
            sb.AppendLine(SerializeBlocks());
            sb.AppendLine(");");
            sb.AppendLine();
            sb.AppendLine("edges");
            sb.AppendLine("(");
            sb.AppendLine(");");
            sb.AppendLine();
            sb.AppendLine("boundary");
            sb.AppendLine("(");
            sb.AppendLine(SerializePatches());
            sb.AppendLine(SerializeTop());
            sb.AppendLine(SerializeGround());
            sb.AppendLine(");");
            sb.AppendLine();
            sb.AppendLine("mergePatchPairs");
            sb.AppendLine("(");
            sb.AppendLine(");");

            return sb.ToString();
        }

        private static string GetHeader()
        {
            return @"/*--------------------------------*- C++ -*----------------------------------*\
| =========                 |                                                 |
| \\      /  F ield         | OpenFOAM: The Open Source CFD Toolbox           |
|  \\    /   O peration     | Version:  2.1.0                                 |
|   \\  /    A nd           | Web:      http://www.OpenFOAM.com               |
|    \\/     M anipulation  |                                                 |
\*---------------------------------------------------------------------------*/
FoamFile
{
    version     2.0;
    format      ascii;
    class       dictionary;
    object      blockMeshDict;
}

// * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * //

convertToMeters 1;
";
        }

        private string SerializeVertices()
        {
            var sb = new StringBuilder();

            for (int i = 0; i < _domainMesh.Vertices.Count; i++)
            {
                var v = _domainMesh.Vertices[i];
                sb.AppendLine($"    ({FormatPoint(v)})");
            }

            return sb.ToString();
        }

        private string SerializeBlocks()
        {
            var sb = new StringBuilder();

            int c1 = _perimBottom.Faces.Count;
            int c2 = _perimBottom.Faces.Count + _coreBottom.Faces.Count + _perimTop.Faces.Count;
            int c3 = _perimBottom.Faces.Count + _coreBottom.Faces.Count;

#if DEBUG
            sb.AppendLine("    // perimeter blocks");
#endif
            for (int i = 0; i < _perimBottom.Faces.Count; i++)
            {
                var f = _domainMesh.Faces[i];
                var ft = _domainMesh.Faces[i + c3];

                sb.AppendLine($"    hex ({f.A} {f.D} {f.C} {f.B} " +
                    $"{ft.A} {ft.B} {ft.C} {ft.D}) " +
                    $"({_divPerim} {_divisionsX} {_divisionsZ}) " +
                    $"simpleGrading (1 {_gradingPerim} 1)");
            }

#if DEBUG
            sb.AppendLine("    // core blocks");
#endif
            for (int i = 0; i < _coreBottom.Faces.Count; i++)
            {
                var f = _domainMesh.Faces[i + c1];
                var ft = _domainMesh.Faces[i + c2];

                sb.AppendLine($"    hex ({f.A} {f.D} {f.C} {f.B} " +
                    $"{ft.A} {ft.B} {ft.C} {ft.D}) " +
                    $"({_divisionsX} {_divisionsX} {_divisionsZ}) " +
                    $"simpleGrading (1 1 1)");
            }

            return sb.ToString();
        }

        private string SerializePatches()
        {
            var sb = new StringBuilder();

            int counter = _perimBottom.Faces.Count + _coreBottom.Faces.Count +
                          _perimTop.Faces.Count + _coreTop.Faces.Count;

            for (int i = 0; i < _sides.Faces.Count; i++)
            {
                var f = _domainMesh.Faces[i + counter];

                sb.AppendLine($"    patch{i}");
                sb.AppendLine("    {");
                sb.AppendLine("        type patch;");
                sb.AppendLine("        faces");
                sb.AppendLine("        (");
                sb.AppendLine($"            ({f.A} {f.B} {f.C} {f.D})");
                sb.AppendLine("        );");
                sb.AppendLine("    }");
            }

            return sb.ToString();
        }

        private string SerializeTop()
        {
            var sb = new StringBuilder();

            int c1 = _perimTop.Faces.Count + _coreTop.Faces.Count;
            int c2 = _perimBottom.Faces.Count + _coreBottom.Faces.Count + _perimTop.Faces.Count;

            sb.AppendLine("    frontAndBack");
            sb.AppendLine("    {");
            sb.AppendLine("        type patch;");
            sb.AppendLine("        faces");
            sb.AppendLine("        (");

            for (int i = 0; i < _perimTop.Faces.Count; i++)
            {
                var f = _domainMesh.Faces[i + c1];
                sb.AppendLine($"            ({f.A} {f.B} {f.C} {f.D})");
            }

            for (int i = 0; i < _coreTop.Faces.Count; i++)
            {
                var f = _domainMesh.Faces[i + c2];
                sb.AppendLine($"            ({f.A} {f.B} {f.C} {f.D})");
            }

            sb.AppendLine("        );");
            sb.AppendLine("    }");

            return sb.ToString();
        }

        private string SerializeGround()
        {
            var sb = new StringBuilder();

            int c1 = _perimBottom.Faces.Count + _coreBottom.Faces.Count;

            sb.AppendLine("    ground");
            sb.AppendLine("    {");
            sb.AppendLine("        type wall;");
            sb.AppendLine("        faces");
            sb.AppendLine("        (");

            for (int i = 0; i < c1; i++)
            {
                var f = _domainMesh.Faces[i];
                sb.AppendLine($"            ({f.A} {f.B} {f.C} {f.D})");
            }

            sb.AppendLine("        );");
            sb.AppendLine("    }");

            return sb.ToString();
        }

        private static string FormatPoint(Point3f pt)
        {
            return $"{pt.X:G} {pt.Y:G} {pt.Z:G}";
        }
    }
}
