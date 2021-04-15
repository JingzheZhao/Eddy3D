using Rhino.Geometry;
using System.Linq;

namespace EddyLib.Indoor.Dicts
{
    internal class BlockMeshDict : GenericDict
    {
        private readonly int divX;

        private readonly int divY;

        private readonly int divZ;

        private readonly Point3d[] Corners;

        public BlockMeshDict(double cellSize, BoundingBox BBox)
        {
            this.DictionaryName = "blockMeshDict";
            this.Location = DictLocation.system;

            this.FC = FieldClass.dictionary;

            this.Header = GetHeader(this);
            this.divX = (int)((BBox.Max.X - BBox.Min.X) / cellSize);
            this.divY = (int)((BBox.Max.Y - BBox.Min.Y) / cellSize);
            this.divZ = (int)((BBox.Max.Z - BBox.Min.Z) / cellSize);
            this.Corners = BBox.GetCorners();

            string[] parts = { this.Header, Vertices(), Blocks(), EdgesAndBoundary };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }

        //private string Serialize(BlockMeshDict dict)
        //{
        //    StringBuilder sb = new StringBuilder();

        //    sb.Append(dict.header);

        //    sb.Append(dict.boundaryFieldString);

        //    return sb.ToString();
        //}

        private string Vertices()
        {
            string res = "";

            res = string.Format(@"
scale           1;

vertices
(
({0} {1} {2})
({3} {4} {5})
({6} {7} {8})
({9} {10} {11})
({12} {13} {14})
({15} {16} {17})
({18} {19} {20})
({21} {22} {23})
);",
Corners[0].X, Corners[0].Y, Corners[0].Z,
Corners[1].X, Corners[1].Y, Corners[1].Z,
Corners[2].X, Corners[2].Y, Corners[2].Z,
Corners[3].X, Corners[3].Y, Corners[3].Z,
Corners[4].X, Corners[4].Y, Corners[4].Z,
Corners[5].X, Corners[5].Y, Corners[5].Z,
Corners[6].X, Corners[6].Y, Corners[6].Z,
Corners[7].X, Corners[7].Y, Corners[7].Z
);

            return res;
        }

        private string Blocks()
        {
            return string.Format(@"

blocks
(
    hex (0 1 2 3 4 5 6 7) ({0} {1} {2}) simpleGrading (1 1 1)
);", this.divX, this.divY, this.divZ);
        }

        private string EdgesAndBoundary = @"

edges
(
);

boundary
(
    Back
    {
        type            wall;
        faces
        (
            (3 7 6 2 )
        );
    }

    Front
    {
        type            wall;
        faces
        (
            (0 1 5 4 )
        );
    }

    Bottom
    {
        type            wall;
        faces
        (
            (4 5 6 7 )
        );
    }

    Top
    {
        type            wall;
        faces
        (
            (0 3 2 1 )
        );
    }

    Right
    {
        type            wall;
        faces
        (
            (1 2 6 5 )
        );
    }

    Left
    {
        type            wall;
        faces
        (
            (0 4 7 3 )
        );
    }

);

mergePatchPairs
(

);
";
    }
}