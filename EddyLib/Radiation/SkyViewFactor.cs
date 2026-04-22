using Rhino;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EddyLib.Radiation
{
    public partial class SkyViewFactor
    {
        public int[] hitCounts;

        private string sunRaysRes = "sunRays.res";

        private string sunRaysFile = "sunRays.pts";

        private string radFile = "geometry.rad";

        private string octreeFile = "geometry.oct";

        private string fileNameExport = "SkyViewFactors.bin";

        private Point3d[] sensors;

        private string workingDir;

        private readonly string subDir = Path.Combine("Rad", "ViewFactors");

        public bool wrongNumberOfProbes;

        public bool resultPrecalculated;

        public double[] Values { get; set; }

        public Mesh BuildingsAndGround { get; set; }

        public SkyViewFactor(string workingDir, Mesh BuildingsAndGround, Point3d[] sensors, bool recalc)
        {
            this.hitCounts = new int[sensors.Length];
            this.Values = new double[sensors.Length];
            this.BuildingsAndGround = BuildingsAndGround;

            this.sensors = sensors;
            this.workingDir = workingDir;
            var subDirPath = Path.Combine(workingDir, subDir);
            var binSVF = Path.Combine(subDirPath, fileNameExport);

            var numberOfProbes = sensors.Length;

            // Add other files here
            if (recalc == false && File.Exists(binSVF))
            {
                // Load radiation datasets [x][] time [][x] points

                var tempValues = RadianceFiles.loadBin1D(binSVF);

                int sensorPointCountExisting = tempValues.Length;

                if (sensorPointCountExisting != numberOfProbes)
                {
                    this.wrongNumberOfProbes = true;
                    return;
                }
                else
                {
                    this.Values = tempValues;
                }
            }
            else if (recalc == true)
            {
                if (File.Exists(binSVF))
                {
                    File.Delete(binSVF);
                }

                Run();

                RadianceFiles.writeBin1D(binSVF, this.Values);
            }
        }

        private void Run()
        {
            var subDirPath = Path.Combine(workingDir, subDir);
            string sunRaysResPath = Path.Combine(subDirPath, this.sunRaysRes);
            string sunRaysFilePath = Path.Combine(subDirPath, this.sunRaysFile);
            string radFilePath = Path.Combine(subDirPath, this.radFile);
            string octreeFilePath = Path.Combine(subDirPath, this.octreeFile);

            // 0. Number of rays

            var numRays = equiSolidAngleVectors4PI().Length;

            // 1. Add Mat

            //foreach (Mesh m in BuildingsAndGround)
            //{
            AddMat(BuildingGroundTemplate(), BuildingsAndGround);

            //this.BuildingsAndGround = BuildingsAndGround;

            //}

            // 2. RadFile

            var matlist = new HashSet<string>();

            StringBuilder radFile = new StringBuilder();

            StringBuilder radFileString = new StringBuilder();

            int id = 0;

            //foreach (GeometryBase g in BuildingsAndGround)
            //{
            string mat = BuildingsAndGround.UserDictionary["RadMat"].ToString().Trim();
            matlist.Add(mat);

            string matName = mat.Split(' ')[2];

            //Print(matName);

            //Mesh m = (Mesh)g;

            radFileString.AppendLine(Mesh2Rad(BuildingsAndGround, matName, id.ToString()));

            //id++;
            //}

            foreach (string s in matlist)
            {
                radFile.AppendLine(s);
            }
            radFile.AppendLine("");
            radFile.AppendLine(radFileString.ToString());

            Directory.CreateDirectory(subDirPath);
            File.WriteAllText(radFilePath, radFile.ToString());

            //A = "Final File Length: " + finalFile.Length;

            // 3. Octree

            RunOconv(radFilePath, octreeFilePath);

            // 4. RaysFile

            StringBuilder sunRaysFile = new StringBuilder();

            foreach (Point3d p in sensors)
            {
                sunRaysFile.AppendLine(Rays(p, equiSolidAngleVectors4PI()));
            }

            File.WriteAllText(sunRaysFilePath, sunRaysFile.ToString());

            //A = "Final File Length: " + finalFile.Length;

            // 5. RayCast

            RunRayCastMat(octreeFilePath, sunRaysFilePath, sunRaysResPath);

            // RunRayCastSurf(Oct, Pts, Path);

            // 6. LoadResultsFile

            var HCnt = equiSolidAngleVectors4PI().Length;

            this.hitCounts = LoadResultFile(HCnt, sunRaysResPath, true);

            var ptCnt = hitCounts.Length;

            for (int pt = 0; pt < ptCnt; pt++)
            {
                this.Values[pt] = (double)hitCounts[pt] / numRays;
            }
        }

    }
}
