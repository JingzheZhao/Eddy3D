using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using System;
using System.Diagnostics;
using System.IO;

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


            try
            {

                // Open the file(s) to read from.

                foreach (double dir in DOM.BCInflow.windDirs)
                {

                    var p1 = DOM.baseWorkingDir + dir + @"\postProcessing\residuals\";
                    fullFilePath = p1 + Utilities.GetLastIterationFromDirectory(p1) + "\\" + @"\\residuals.dat";
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
set output '" + DOM.baseWorkingDir + @"residuals_" + dir + @".pdf'
replot
";

                    Utilities.StartProcessCMD(arg, true, false, true, @"C:\Program Files\gnuplot\bin\gnuplot.exe");

                                   
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
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                Resources.Eddy_residuals;

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{26728D9C-4BE0-459D-ABF5-2708ED951CA3}");
    }
}

