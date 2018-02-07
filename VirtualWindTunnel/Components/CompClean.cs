using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Text;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;
using System.Threading;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace WindTunnel
{
    public class Clean : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public Clean()
          : base("Clean", "Clean",
              "Clean",
              "CFDTool", "Misc")
        {
        }

        

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Dir", "Dir", "Dir", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "Run", "Clean the directory", GH_ParamAccess.item, false);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
       
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            
            bool Run = false;
            string workingDirectory = "";

            
            
            DA.GetData(0, ref workingDirectory);            
            DA.GetData(1, ref Run);

            
            


            if (Run == true)
            {

                List<String> l = new List<string>();
                l.Add(@"\0");
                l.Add(@".pyFoam");
                l.Add(@"\patchMass*");
                l.Add(@"\postProcessing*");
                l.Add(@"\forces*");
                l.Add(@"\efficiency");
                l.Add(@"\PyFoam*");
                l.Add(@"\constant\extendedFeatureEdgeMesh");
                l.Add(@"\constant\polyMesh ");
                l.Add(@"\constant\triSurface\*.eMesh");
                l.Add(@"\*.pvsm");
                l.Add(@"\Decomposer.*");
                l.Add(@"\processor.*");

                //rm* OpenFOAM
                //rm* foam
                //rm *.png
                //rm Ply *
                //rm* log*
                //Directory.


                //   Directory.GetDirectories(workingDirectory);


                foreach (String element in l)
                {
                    try
                    {
                        String newWorkingDirectory = workingDirectory + element;
                        var newWorkingDirectoryInfo = new DirectoryInfo(newWorkingDirectory);

                        foreach (var file in newWorkingDirectoryInfo.EnumerateFiles("*"))
                        {
                            file.Delete();
                            Directory.Delete(newWorkingDirectory, true);
                        }
                        
                        //File.Delete(newWorkingDirectory, true);

                        //bool directoryExists = Directory.Exists(newWorkingDirectory);

                        //Console.WriteLine("top-level directory exists: " + directoryExists);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("The process failed: {0}", e.Message);
                    }
                }
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
            get { return new Guid("{EE4594E9-FF2E-4E07-8156-957F1F9E08EE}"); }
        }
    }
}
