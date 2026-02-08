using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    public class BuildingGeometryTests
    {
        private readonly struct StlTopology
        {
            public int UniqueVertices { get; }
            public int Faces { get; }

            public StlTopology(int uniqueVertices, int faces)
            {
                UniqueVertices = uniqueVertices;
                Faces = faces;
            }
        }

        private static StlTopology LoadTopology(string resourcePath)
        {
            var solutionRoot = FindSolutionRoot();
            var stlPath = Path.Combine(solutionRoot, resourcePath);

            if (!File.Exists(stlPath))
                throw new FileNotFoundException($"STL not found: {stlPath}");

            return IsBinaryStl(stlPath)
                ? ReadBinaryStl(stlPath)
                : ReadAsciiStl(stlPath);
        }

        private static string FindSolutionRoot()
        {
            var current = new DirectoryInfo(AppContext.BaseDirectory);
            while (current != null && current.Name != TestConstants.SolutionFolderName)
            {
                current = current.Parent;
            }

            if (current != null)
                return current.FullName;

#if DEBUG
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\"));
#else
            return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\..\"));
#endif
        }

        private static bool IsBinaryStl(string path)
        {
            var info = new FileInfo(path);
            if (info.Length < 84)
                return false;

            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs, Encoding.ASCII, leaveOpen: true);
            fs.Seek(80, SeekOrigin.Begin);
            var triangleCount = br.ReadUInt32();
            var expectedSize = 84L + (long)triangleCount * 50L;
            return expectedSize == info.Length;
        }

        private static StlTopology ReadBinaryStl(string path)
        {
            var uniqueVertices = new HashSet<string>();
            int faces = 0;

            using var fs = File.OpenRead(path);
            using var br = new BinaryReader(fs, Encoding.ASCII, leaveOpen: true);
            fs.Seek(80, SeekOrigin.Begin);
            var triangleCount = br.ReadUInt32();

            for (var i = 0; i < triangleCount; i++)
            {
                _ = br.ReadSingle();
                _ = br.ReadSingle();
                _ = br.ReadSingle();

                for (var v = 0; v < 3; v++)
                {
                    var x = br.ReadSingle();
                    var y = br.ReadSingle();
                    var z = br.ReadSingle();
                    uniqueVertices.Add($"{x:R}|{y:R}|{z:R}");
                }

                _ = br.ReadUInt16();
                faces++;
            }

            return new StlTopology(uniqueVertices.Count, faces);
        }

        private static StlTopology ReadAsciiStl(string path)
        {
            var uniqueVertices = new HashSet<string>();
            int verticesInCurrentFacet = 0;
            int faces = 0;

            foreach (var rawLine in File.ReadLines(path))
            {
                var line = rawLine.Trim();
                if (!line.StartsWith("vertex ", StringComparison.OrdinalIgnoreCase))
                    continue;

                var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4)
                    continue;

                uniqueVertices.Add($"{parts[1]}|{parts[2]}|{parts[3]}");
                verticesInCurrentFacet++;

                if (verticesInCurrentFacet == 3)
                {
                    faces++;
                    verticesInCurrentFacet = 0;
                }
            }

            return new StlTopology(uniqueVertices.Count, faces);
        }

        [NotWindowsServerTheory]
        [InlineData(@"RhinoPlugin.Test.Xunit\Resources\BuildingGeo_fine.stl", 11144, 22322)]
        [InlineData(@"RhinoPlugin.Test.Xunit\Resources\BuildingGeo.stl", 92, 172)]
        public void BuildingGeo_HasExpectedTopology(string resourcePath, int expectedVertices, int expectedFaces)
        {
            var topology = LoadTopology(resourcePath);
            Assert.Equal(expectedVertices, topology.UniqueVertices);
            Assert.Equal(expectedFaces, topology.Faces);
        }
    }
}
