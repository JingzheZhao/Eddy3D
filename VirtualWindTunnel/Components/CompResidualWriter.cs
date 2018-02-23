using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Text;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;
using Grasshopper.Kernel.Types;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace WindTunnel
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
          : base("residualWriter", "residualWriter",
              "Write stuff",
              "Eddy", "Simulation")
        {
        }

        

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.AddGenericParameter("Domain", "Domain", "Domain", GH_ParamAccess.item);
            pManager.AddTextParameter("workingDir", "workingDir", "workingDir", GH_ParamAccess.item);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("Out", "Out", "Out", GH_ParamAccess.item);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string workingDir = @"";



             

            DA.GetData(0, ref workingDir);

           
                var iter = new List<int>();
                var Ux = new List<double>();
                var Uy = new List<double>();
                var Uz = new List<double>();
                var p1 = new List<double>();
                var p2 = new List<double>();
                var p3 = new List<double>();
                var p4 = new List<double>();
                var omega = new List<double>();
                var k = new List<double>();


               string fullFilePath = workingDir + "log";
            

                var lines = File.ReadAllLines(fullFilePath);
            //var l = lines[i];
            //if (l.StartsWith("Exec   : simpleFoam")) lines.Parse(l.Split(',')[1].Split('=')[1]));



            int pCnt = 0;

                for (int i = 0; i < lines.Length; i++)
                {
                    var l = lines[i];
                    if (l.StartsWith("Time =")) iter.Add(int.Parse(l.Replace("Time =", "").Trim()));

                    if (l.StartsWith("smoothSolver:  Solving for Ux, Initial residual =")) Ux.Add(double.Parse(l.Split(',')[1].Split('=')[1]));
                    if (l.StartsWith("smoothSolver:  Solving for Uy, Initial residual =")) Uy.Add(double.Parse(l.Split(',')[1].Split('=')[1]));
                    if (l.StartsWith("smoothSolver:  Solving for Uz, Initial residual =")) Uz.Add(double.Parse(l.Split(',')[1].Split('=')[1]));



                    if (l.StartsWith("GAMG:  Solving for p, Initial residual ="))
                    {
                        if (pCnt == 0) { p1.Add(double.Parse(l.Split(',')[1].Split('=')[1])); pCnt++; }
                        else if (pCnt == 1) { p2.Add(double.Parse(l.Split(',')[1].Split('=')[1])); pCnt++; }
                        else if (pCnt == 2) { p3.Add(double.Parse(l.Split(',')[1].Split('=')[1])); pCnt++; }
                        else if (pCnt == 3) { p4.Add(double.Parse(l.Split(',')[1].Split('=')[1])); pCnt = 0; }
                    }

                    if (l.StartsWith("smoothSolver:  Solving for omega, Initial residual =")) omega.Add(double.Parse(l.Split(',')[1].Split('=')[1]));
                    if (l.StartsWith("smoothSolver:  Solving for k, Initial residual =")) k.Add(double.Parse(l.Split(',')[1].Split('=')[1]));
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("iter,Ux,Uy,Uz,p,omega,k");
            for (int i = 0; i < Ux.Count; i++)
            {

                sb.AppendLine(iter[i] + "," + Ux[i] + "," + Uy[i] + "," + Uz[i] + "," + p1[i] + "," + omega[i] + "," + k[i]);
            }


            File.WriteAllText(workingDir+"residuals.csv", sb.ToString());

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
                //return Resources.IconForThisComponent;
                return null;
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
