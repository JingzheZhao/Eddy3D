using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class Residuals : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public Residuals()
        : base("Residuals", "Residuals", "Vis" + EddyVersion.toString(),
              EddyVersion.Name, "4 | Residuals")
        {
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Write Residuals", Menu_DoClick, true, !visResiduals);
        }

        // !visResiduals == writeResiduals

        private void Menu_DoClick(object sender, EventArgs e)
        {
            visResiduals = !visResiduals;

            ExpireSolution(true);
        }

        public bool visResiduals = true;

        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            // First add our own field.
            writer.SetBoolean("visResiduals", visResiduals);
            // Then call the base class implementation.
            return base.Write(writer);
        }

        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            // First read our own field.
            visResiduals = reader.GetBoolean("visResiduals");
            // Then call the base class implementation.
            return base.Read(reader);
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "Res", "Eddy Result", GH_ParamAccess.item);

            //pManager.AddIntegerParameter("Mode", "Mode", "Mode", GH_ParamAccess.item, 0);
            //Param_Integer param = pManager[1] as Param_Integer;
            //param.AddNamedValue("Retrieve file from domain.", 0);
            //param.AddNamedValue("Provide custom file.", 1);

            //pManager.AddTextParameter("fP", "fP", "fP", GH_ParamAccess.item, "");
            pManager.AddIntegerParameter("Selection of wind directions", "Sel", @"List of integers for the wind directions to load, e.g. ""0,35"" .""", GH_ParamAccess.list);
            pManager.AddTextParameter("X", "X", @"Bounds for the x-axis, e.g. ""0:5000""", GH_ParamAccess.item, ":");
            pManager.AddTextParameter("Y", "Y", @"Bounds for the y-axis, e.g. ""0.00001:1""", GH_ParamAccess.item, ":");
            pManager.AddIntegerParameter("Version", "Ver", "Version", GH_ParamAccess.item, 1);
            Param_Integer param = pManager[4] as Param_Integer;
            param.AddNamedValue("Windows Gnuplot", 0);
            param.AddNamedValue("BlueCFD Gnuplot", 1);
            pManager.AddBooleanParameter("Run", "Run", "Run the component", GH_ParamAccess.item, false);

            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("Lab", "L", "Labels", GH_ParamAccess.list);
            //pManager.AddGenericParameter("Res", "R", "Residuals", GH_ParamAccess.tree);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">
        /// The DA object can be used to retrieve data from input parameters and to store data in
        /// output parameters.
        /// </param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            // mode to select simulation environment
            if (visResiduals) { Message = "Visualize"; }
            else { Message = "Write"; }

            OFResult RES = null;
            DA.GetData(0, ref RES);

            bool run = false;

            string x0x1 = ":";
            string y0y1 = ":";

            string fullFilePath = "";

            List<int> selectionList = new List<int>();

            DA.GetDataList("Selection of wind directions", selectionList);
            DA.GetData("X", ref x0x1);
            DA.GetData("Y", ref y0y1);
            int version = 1;
            DA.GetData("Version", ref version);
            DA.GetData("Run", ref run);

            List<int> selection = new List<int>();
            if (selectionList.Count != 0)
            {
                selection = RES.Domain.BCond.windDirs.Intersect(selectionList).ToList();
            }
            else
            {
                selection.Add(RES.Domain.BCond.windDirs[0]);
            }

            if (!RES.RunSettings.WindowsGnuplotInstalled && !RES.RunSettings.BlueCFDIsInstalled)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "There was no Gnuplot version found on your system.");
                return;
            }

            if (run != true) { return; }

            if (visResiduals)
            {
                try
                {
                    foreach (double dir in selection)
                    {
                        string p1 = RES.WorkingDirectory + dir + @"\postProcessing\residuals\";
                        fullFilePath = p1 + Utilities.GetLastIterationFromDirectory(p1) + "\\" + @"\\residuals.dat";
                        if (!File.Exists(fullFilePath))
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The residual file for wind direction " + dir + " does not exist.");
                        }

                        string fields = Utilities.FileReader(fullFilePath)[1];

                        string field1 = System.Text.RegularExpressions.Regex.Split(fields, @"\s{2,}")[1];
                        string field2 = System.Text.RegularExpressions.Regex.Split(fields, @"\s{2,}")[2];
                        string field3 = System.Text.RegularExpressions.Regex.Split(fields, @"\s{2,}")[3];
                        string field4 = System.Text.RegularExpressions.Regex.Split(fields, @"\s{2,}")[4];
                        string field5 = System.Text.RegularExpressions.Regex.Split(fields, @"\s{2,}")[5];
                        string field6 = System.Text.RegularExpressions.Regex.Split(fields, @"\s{2,}")[6];

                        string arg = @"
set title 'wind direction: " + dir + @"'
set logscale y
set yrange[" + y0y1 + @"]
set xrange[" + x0x1 + @"]
set ylabel 'Residual'
set xlabel 'Iteration'
set format y ""10 ^{% T}
                        ""
set datafile separator '\t'
plot '" + fullFilePath + @"' u($1):2 with lines title '" + field1 + "','" + fullFilePath + @"' u($1):3 with lines title '" + field2 + "','" + fullFilePath + @"' u($1):4 with lines title '" + field3 + "','" + fullFilePath + @"' u($1):5 with lines title '" + field4 + "','" + fullFilePath + @"' u($1):6 with lines title '" + field5 + "','" + fullFilePath + @"' u($1):7 with lines title '" + field6 + @"'
pause 3600; replot
";

                        Utilities.StartProcess.StartProcessCMDNT(arg, true, false, false, true, Utilities.GetGnuplotPath(version));
                    }
                }
                catch (Exception e)
                {
                    // Let the user know what went wrong.
                    Console.WriteLine("The file(s) could not be read:");
                    Console.WriteLine(e.Message);
                }
            }
            else
            {
                try
                {
                    // Open the file(s) to read from.

                    foreach (double dir in selection)
                    {
                        string p1 = RES.WorkingDirectory + dir + @"\postProcessing\residuals\";
                        var fullDirectoryPath = p1 + Utilities.GetLastIterationFromDirectory(p1) + "\\";
                        var fileName = Path.GetFileName(Utilities.GetFileNameWithHighestEnumerator(fullDirectoryPath));
                        fullFilePath = fullDirectoryPath + fileName;

                        if (!File.Exists(fullFilePath))
                        {
                            AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The residual file for wind direction " + dir + " does not exist.");
                        }

                        string arg = @"
set title 'wind direction: " + dir + @"'
set logscale y
set yrange [" + y0y1 + @"]
set xrange [" + x0x1 + @"]
set ylabel 'Residual'
set xlabel 'Iteration'
set format y ""10^{%T}""
set datafile separator '\t'
plot '" + fullFilePath + @"' u($1):2 with lines title 'Ux', '" + fullFilePath + @"' u($1):3 with lines title 'Uy', '" + fullFilePath + @"' u($1):4 with lines title 'Uz', '" + fullFilePath + @"' u($1):5 with lines title 'p', '" + fullFilePath + @"' u($1):6 with lines title 'omega', '" + fullFilePath + @"' u($1):7 with lines title 'k'
set terminal pdf
set output '" + RES.WorkingDirectory + @"residuals_" + dir + @".pdf'
replot
";

                        Utilities.StartProcess.StartProcessCMDNT(arg, true, false, false, true, Utilities.GetGnuplotPath(RES.RunSettings, version));
                    }
                }
                catch (Exception e)
                {
                    // Let the user know what went wrong.
                    Console.WriteLine("The file(s) could not be read:");
                    Console.WriteLine(e.Message);
                }
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                Resources.Eddy_stability;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{2936A937-4F42-4873-B65D-D02417D73D06}");
    }
}