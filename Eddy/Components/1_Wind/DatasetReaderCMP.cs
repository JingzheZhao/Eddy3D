using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using Eddy.Properties;
using System.IO;
using System.Text;
using System.Linq;

namespace Eddy
{
    public class DatasetReaderCMP : GH_Component
    {
        public DatasetReaderCMP()
            : base("Dataset Reader", "DataReader",
                "Read processed CSV datasets back into Grasshopper. Supports mag_U and all spatial features.",
                "Eddy3D", "4 | ML")
        {
        }

        protected override Bitmap Icon => Resources.Eddy_dataset_reader;

        public override Guid ComponentGuid => new Guid("{9F8E7D6C-5B4A-3210-FEDC-BA9876543210}");

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("CSV Path", "Path", "Path to the .csv file to read.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "Run", "Trigger the reading process.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("SDF", "SDF", "Signed distance from building.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Bldg_height", "Bldg_height", "Building height.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Z_relative", "Z_relative", "Relative height.", GH_ParamAccess.list);
            pManager.AddNumberParameter("U_over_Uref", "U_over_Uref", "Wind speed at height.", GH_ParamAccess.list);
            pManager.AddNumberParameter("mag_U", "mag_U", "Simulated wind speed magnitude.", GH_ParamAccess.list);
            pManager.AddNumberParameter("X", "X", "X coordinate.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Y", "Y", "Y coordinate.", GH_ParamAccess.list);
            pManager.AddNumberParameter("dir_sin", "dir_sin", "Direction Sin component.", GH_ParamAccess.list);
            pManager.AddNumberParameter("dir_cos", "dir_cos", "Direction Cos component.", GH_ParamAccess.list);
            pManager.AddGeometryParameter("brep", "brep", "Reconstructed building geometry (Boxes) from CSV data.", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string path = string.Empty;
            bool run = false;

            DA.GetData(1, ref run);
            if (!run)
            {
                Message = "Toggle 'Run' to read";
                return;
            }

            if (!DA.GetData(0, ref path)) return;

            if (!File.Exists(path))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"File does not exist: {path}");
                return;
            }

            try
            {
                var outputs = new Dictionary<string, List<double>>
                {
                    { "SDF", new List<double>() },
                    { "Bldg_height", new List<double>() },
                    { "Z_relative", new List<double>() },
                    { "U_over_Uref", new List<double>() },
                    { "mag_U", new List<double>() },
                    { "X", new List<double>() },
                    { "Y", new List<double>() },
                    { "dir_sin", new List<double>() },
                    { "dir_cos", new List<double>() }
                };

                using (var reader = new StreamReader(path, Encoding.UTF8))
                {
                    string headerLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(headerLine)) return;

                    string[] headers = headerLine.Split(',');
                    var colMap = new Dictionary<int, string>();

                    for (int i = 0; i < headers.Length; i++)
                    {
                        string h = headers[i].Trim();
                        // Handle potential naming variations
                        if (h.Equals("X_coords", StringComparison.OrdinalIgnoreCase)) h = "X";
                        if (h.Equals("Y_coords", StringComparison.OrdinalIgnoreCase)) h = "Y";

                        if (outputs.ContainsKey(h))
                        {
                            colMap[i] = h;
                        }
                    }

                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        string[] values = line.Split(',');

                        // To ensure all outputs have the same length, we iterate over all possible keys
                        foreach (var key in outputs.Keys.ToList())
                        {
                            // Find if this key is in our column map
                            var mapEntry = colMap.FirstOrDefault(x => x.Value == key);
                            bool found = false;

                            if (mapEntry.Value != null && mapEntry.Key < values.Length)
                            {
                                if (double.TryParse(values[mapEntry.Key], out double val))
                                {
                                    outputs[key].Add(val);
                                    found = true;
                                }
                            }

                            if (!found)
                            {
                                outputs[key].Add(double.NaN);
                            }
                        }
                    }
                }

                DA.SetDataList(0, outputs["SDF"]);
                DA.SetDataList(1, outputs["Bldg_height"]);
                DA.SetDataList(2, outputs["Z_relative"]);
                DA.SetDataList(3, outputs["U_over_Uref"]);
                DA.SetDataList(4, outputs["mag_U"]);
                DA.SetDataList(5, outputs["X"]);
                DA.SetDataList(6, outputs["Y"]);
                DA.SetDataList(7, outputs["dir_sin"]);
                DA.SetDataList(8, outputs["dir_cos"]);

                // --- Geometry Reconstruction ---
                var brepList = new List<Brep>();
                var xList = outputs["X"];
                var yList = outputs["Y"];
                var hList = outputs["Bldg_height"];

                if (xList.Count > 0 && xList.Count == yList.Count && xList.Count == hList.Count)
                {
                    // 1. Infer Grid Size
                    double cellSize = 1.0; // Fallback
                    var uniqueX = xList.Distinct().OrderBy(v => v).ToList();
                    var uniqueY = yList.Distinct().OrderBy(v => v).ToList();

                    double minDx = double.MaxValue;
                    for (int i = 0; i < uniqueX.Count - 1; i++)
                    {
                        double diff = uniqueX[i + 1] - uniqueX[i];
                        if (diff < minDx && diff > 1e-4) minDx = diff;
                    }

                    double minDy = double.MaxValue;
                    for (int i = 0; i < uniqueY.Count - 1; i++)
                    {
                        double diff = uniqueY[i + 1] - uniqueY[i];
                        if (diff < minDy && diff > 1e-4) minDy = diff;
                    }

                    if (minDx < double.MaxValue && minDy < double.MaxValue)
                        cellSize = Math.Min(minDx, minDy);
                    else if (minDx < double.MaxValue) cellSize = minDx;
                    else if (minDy < double.MaxValue) cellSize = minDy;

                    // 2. Generate Boxes
                    for (int i = 0; i < xList.Count; i++)
                    {
                        double h = hList[i];
                        if (h > 0.01)
                        {
                            // Assume X/Y is center
                            double x = xList[i];
                            double y = yList[i];
                            var plane = new Plane(new Point3d(x, y, 0), Vector3d.ZAxis);
                            // Centered base: X and Y interval is [-size/2, size/2]
                            var box = new Box(plane, new Interval(-cellSize / 2, cellSize / 2), new Interval(-cellSize / 2, cellSize / 2), new Interval(0, h));
                            brepList.Add(box.ToBrep());
                        }
                    }
                }
                DA.SetDataList(9, brepList);

                Message = $"Rows: {outputs["SDF"].Count}";
            }
            catch (Exception ex)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Parsing error: {ex.Message}");
            }
        }
    }
}
