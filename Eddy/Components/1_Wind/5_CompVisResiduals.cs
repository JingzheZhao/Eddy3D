using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class Residuals : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.quinary; }
        }

        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public Residuals()
        : base("Plot Residuals", "Residuals", 
@"Convergence Monitor

Visualizes the definition of simulation convergence (residuals) in real-time. Helps verify if the simulation has reached a stable solution.

" + EddyVersion.toString(),
              EddyVersion.Name, "1 | Wind")
        {
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "Export Residuals as PDF", Menu_DoClick, true, !visResiduals);
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
            pManager.AddGenericParameter(
                "Result", "Res", 
                "Simulation result from Wind Simulation component.", 
                GH_ParamAccess.item);

            pManager.AddTextParameter(
                "X Range", "X", 
                "Iteration axis range. Format: 'min:max'. Example: '0:5000'", 
                GH_ParamAccess.item, ":");

            pManager.AddTextParameter(
                "Y Range", "Y", 
                "Residual axis range (log scale). Format: 'min:max'. Example: '0.00001:1'", 
                GH_ParamAccess.item, ":");

            pManager.AddIntegerParameter(
                "Gnuplot Version", "Ver", 
                "0: BlueCFD Gnuplot, 1: Windows Gnuplot", 
                GH_ParamAccess.item, 0);
            Param_Integer param = pManager[3] as Param_Integer;
            param.AddNamedValue("BlueCFD Gnuplot", 0);
            param.AddNamedValue("Windows Gnuplot", 1);

            pManager.AddBooleanParameter(
                "Run", "Run!", 
                "Set True to display residual plot.", 
                GH_ParamAccess.item, false);

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
            else { Message = "Export"; }

            OFResult RES = null;
            DA.GetData(0, ref RES);

            string x0x1 = ":";
            string y0y1 = ":";
            DA.GetData(1, ref x0x1);
            DA.GetData(2, ref y0y1);

            int gnuplotVersion = 0;
            DA.GetData(3, ref gnuplotVersion);

            if (!RES.RunSettings.WindowsGnuplotInstalled && !RES.RunSettings.BlueCFD_GNU_Plot)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "There was no Gnuplot version found on your system.");
                return;
            }

            if (!RES.RunSettings.WindowsGnuplotInstalled && gnuplotVersion == 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "There is no native Gnuplot version installed, please consider selecting the version that comes with BlueCFD.");
                return;
            }

            bool run = false;
            DA.GetData(4, ref run);
            if (run != true) { return; }

            List<int> windDirections = RES.Domain.BCond.WindDirections;

            foreach (int dir in windDirections)
            {
                ProcessWindDirection(dir, RES, visResiduals, x0x1, y0y1, gnuplotVersion);
            }
        }

        private void ProcessWindDirection(int dir, OFResult RES, bool visResiduals, string x0x1, string y0y1, int version)
        {
            try
            {
                // Refactored to a separate method for processing each wind direction
                string lastDir = GetLastDirectoryPath(RES, dir);
                var mostRecentFile = GetMostRecentFile(lastDir, "*.dat");

                var fullFilePath = Path.Combine(lastDir, mostRecentFile.Name);
                ProcessFile(fullFilePath, dir, RES, visResiduals, x0x1, y0y1, version);
            }
            catch (Exception e)
            {
                Console.WriteLine("The file(s) could not be read:");
                Console.WriteLine(e.Message);
            }
        }

        private static string GetLastDirectoryPath(OFResult RES, int dir)
        {
            string p1 = Path.Combine(RES.WorkingDirectory, dir.ToString(), "postProcessing", "residuals") + Path.DirectorySeparatorChar;
            return p1 + Utilities.GetLastIterationFromDirectory(p1);
        }

        private static FileInfo GetMostRecentFile(string directoryPath, string fileExtension)
        {
            var directoryInfo = new DirectoryInfo(directoryPath);
            return directoryInfo.GetFiles(fileExtension)
                                .OrderByDescending(f => f.LastWriteTime)
                                .FirstOrDefault();
        }

        private static void ProcessFile(string residualsPath, int dir, OFResult RES, bool visResiduals, string x0x1, string y0y1, int version)
        {
            // Process the file content, prepare Gnuplot arguments, etc.
            // The actual implementation depends on how you want to process the file
            // and how the Gnuplot arguments are structured in your application.

            string gnuplotArguments = PrepareGnuplotArguments(residualsPath, dir, RES, visResiduals, x0x1, y0y1);
            string gnuplotExecutablePath = Utilities.GetGnuplotPath(RES.RunSettings, version);

            var pdfFilePath = Path.Combine(RES.WorkingDirectory, "residuals_" + dir + ".pdf");

            if (File.Exists(pdfFilePath))
            {
                File.Delete(pdfFilePath);
            }

            Utilities.StartProcess.StartGnuplot(gnuplotArguments, true, false, gnuplotExecutablePath);
        }

        private static string PrepareGnuplotArguments(string fullFilePath, int dir, OFResult RES, bool visResiduals, string x0x1, string y0y1)
        {
            // Prepare the Gnuplot arguments based on the fields and other parameters
            // This needs to be implemented based on how you're using Gnuplot

            string fields = Utilities.FileReader(fullFilePath)[1];
            string field1 = System.Text.RegularExpressions.Regex.Split(fields, @"\s{2,}")[1];
            string field2 = System.Text.RegularExpressions.Regex.Split(fields, @"\s{2,}")[2];
            string field3 = System.Text.RegularExpressions.Regex.Split(fields, @"\s{2,}")[3];
            string field4 = System.Text.RegularExpressions.Regex.Split(fields, @"\s{2,}")[4];
            string field5 = System.Text.RegularExpressions.Regex.Split(fields, @"\s{2,}")[5];
            string field6 = System.Text.RegularExpressions.Regex.Split(fields, @"\s{2,}")[6];

            string template = @"
                set title 'wind direction: " + dir + @"'
                set logscale y
                set yrange[" + y0y1 + @"]
                set xrange[" + x0x1 + @"]
                set ylabel 'Residual'
                set xlabel 'Iteration'
                set format y ""10 ^{% T}""
                set datafile separator '\t'
                plot '" + fullFilePath + @"' u($1):2 with lines title '" + field1 + "','" + fullFilePath + @"' u($1):3 with lines title '" + field2 + "','" + fullFilePath + @"' u($1):4 with lines title '" + field3 + "','" + fullFilePath + @"' u($1):5 with lines title '" + field4 + "','" + fullFilePath + @"' u($1):6 with lines title '" + field5 + "','" + fullFilePath + @"' u($1):7 with lines title '" + field6 + @"'";

            var argVis = template; // + "\npause 60; replot";

            var argWrite = @"set terminal pdf
                set output '" + RES.WorkingDirectory + @"residuals_" + dir + @".pdf'
                " + template;

            var arg = (visResiduals) ? argVis : argWrite;

            return arg;
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