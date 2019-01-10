using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Text;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;
using Grasshopper.Kernel.Types;
using SlavaGu.ConsoleAppLauncher;
using System.Windows.Forms;
using Grasshopper;
using Eddy.Properties;
using System.Linq;
using EddyLib;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class ResidualWriter : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ResidualWriter()
        : base("WriteResiduals", "WriteResiduals",
        "Write",
        "Eddy", "Residuals")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Sim", "Sim", "Sim", GH_ParamAccess.item);

            //pManager.AddIntegerParameter("Mode", "Mode", "Mode", GH_ParamAccess.item, 0);
            //Param_Integer param = pManager[1] as Param_Integer;
            //param.AddNamedValue("Retrieve file from domain.", 0);
            //param.AddNamedValue("Provide custom file.", 1);

            //pManager.AddTextParameter("fP", "fP", "fP", GH_ParamAccess.item, "");
            pManager.AddTextParameter("X", "X", @"Provide bounds for the x-axis, e.g. ""0:5000""", GH_ParamAccess.item, ":");
            pManager.AddTextParameter("Y", "Y", @"Provide bounds for the y-axis, e.g. ""0.00001:1""", GH_ParamAccess.item, ":");

            //pManager.AddBooleanParameter("Live", "Live", "Run the component for a live preview", GH_ParamAccess.item, false);

            //pManager[0].Optional = true;
            //pManager[1].Optional = true;
            //pManager[2].Optional = true;
            //pManager[3].Optional = true;
            //pManager[4].Optional = true;
            //pManager[5].Optional = true;
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
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            OFBaseDomain DOM = null;

            GH_ObjectWrapper gobj = null;
            if (!DA.GetData(0, ref gobj)) { }

            if ((gobj.Value is EddyLib.OFBaseDomain))
            {
                DOM = (OFBaseDomain)gobj.Value;
            }
            if (DOM == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid domain object"); return; }





            string x0x1 = ":";
            string y0y1 = ":";

            //string fullFilePath = "";
            //DA.GetData(1, ref mode);

            string fullFilePath = "";

            DA.GetData(1, ref x0x1);
            DA.GetData(2, ref y0y1);

            foreach (double dir in DOM.BCInflow.windDirs)
            {
                fullFilePath = DOM.baseWorkingDirectory + dir + @"\postProcessing\residuals\0\residuals.dat";
                if (!File.Exists((fullFilePath))) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "The residual file for wind direction " + dir + " does not exist."); }
            }

            try
            {

                // Open the file(s) to read from.

                foreach (double dir in DOM.BCInflow.windDirs)
                {

                    
                    fullFilePath = DOM.baseWorkingDirectory + dir + @"\postProcessing\residuals\0\residuals.dat";


                    Process plotProcessPDF = new Process();
                    plotProcessPDF.StartInfo.FileName = @"""C:\Program Files\gnuplot\bin\gnuplot.exe""";
                    plotProcessPDF.StartInfo.UseShellExecute = false;
                    plotProcessPDF.StartInfo.RedirectStandardInput = true;
                    plotProcessPDF.StartInfo.CreateNoWindow = true;
                    plotProcessPDF.Start();
                    StreamWriter swPDF = plotProcessPDF.StandardInput;
                    //String strInputText = "plot sin(x)\n";
                    String strInputTextPDF = @"
set title 'wind direction: " + dir + @"'
set logscale y
set logscale y
set yrange [" + y0y1 + @"]
set xrange [" + x0x1 + @"]
set ylabel 'Residual'
set xlabel 'Iteration'
set format y ""10^{%T}""
set datafile separator '\t'
plot '" + fullFilePath + @"' u($0):2 with lines title 'Ux', '" + fullFilePath + @"' u($0):3 with lines title 'Uy', '" + fullFilePath + @"' u($0):4 with lines title 'Uz', '" + fullFilePath + @"' u($0):5 with lines title 'p', '" + fullFilePath + @"' u($0):6 with lines title 'omega', '" + fullFilePath + @"' u($0):7 with lines title 'k'
set terminal pdf
set output '" + DOM.baseWorkingDirectory + @"residuals_" + dir + @".pdf'
replot
";
                    swPDF.WriteLine(strInputTextPDF);
                    swPDF.Flush();
                    //MessageBox.Show("Close the gnuplot Window? " );
                    swPDF.Close();
                    //plotProcess.Close();




                    //var labels = new List<string>();
                    //labels.Add("Ux");
                    //labels.Add("Uy");
                    //labels.Add("Uz");
                    //labels.Add("p");
                    //labels.Add("omega");
                    //labels.Add("k");

                    //DA.SetDataList(0, labels);

                    //var resid = new DataTree<double>();

                    //resid.AddRange(Ux, new Grasshopper.Kernel.Data.GH_Path(0));
                    //resid.AddRange(Uy, new Grasshopper.Kernel.Data.GH_Path(1));
                    //resid.AddRange(Uz, new Grasshopper.Kernel.Data.GH_Path(2));
                    //resid.AddRange(p, new Grasshopper.Kernel.Data.GH_Path(3));
                    //resid.AddRange(omega, new Grasshopper.Kernel.Data.GH_Path(4));
                    //resid.AddRange(k, new Grasshopper.Kernel.Data.GH_Path(5));

                    //DA.SetDataTree(1, resid);

                }
            }

            catch (Exception e)
            {
                // Let the user know what went wrong.
                Console.WriteLine("The file(s) could not be read:");
                Console.WriteLine(e.Message);
            }
        }



        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                return Resources.Eddy_residuals;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{26728D9C-4BE0-459D-ABF5-2708ED951CA3}"); }
        }
    }
}

