using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Text;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace WindTunnel
{
    public class SnappyHexMesh : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public SnappyHexMesh()
          : base("Mesh", "Mesh",
              "Mesh",
              "CFDTool", "Meshing")
        {
        }

        

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Domain", "Domain", "Domain", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Accuracy", "acc", "Specify accuracy of mesh", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "Run", "Create the mesh.", GH_ParamAccess.item, false);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Out", "Out", "Out", GH_ParamAccess.item);
            pManager.AddGenericParameter("Domain", "Domain", "Domain", GH_ParamAccess.item);
            //pManager.AddGenericParameter("Cyl", "C", "Domain", GH_ParamAccess.item);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            //string filepath = @"C:\OF\";
            bool Run = false;
            

            //string workingDirectory = "";

            //public Box DomainBoundaryBox;
            //List<Brep> domain = new List<Brep>();

            OFDomainBuilder DOM = null;
            if (!DA.GetData(0, ref DOM)) { return; }
            if (DOM == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid domain object"); return; }


            //string command = "";
            string SingleCPU = "surfaceFeatureExtract;snappyHexMesh"; //-overwrite
            string MultipleCPU = @"surfaceFeatureExtract; pyFoamDecompose.py --clear . " + DOM.CPU + @"; foamJob -parallel -screen snappyHexMesh -overwrite";




            int acc = 3;
            
            
            DA.GetData(1, ref acc);
            
            DA.GetData(2, ref Run);



            if (Run == true)
            {
                
                var stlDir = Path.GetDirectoryName( DOM.workingDirectory + @"\constant\triSurface\");
                var stlFilenameBuildings = DOM.workingDirectory + @"\constant\triSurface\building.stl";
                var stlFilenameGround = DOM.workingDirectory + @"\constant\triSurface\ground.stl";

                if (!Directory.Exists(stlDir)) {
                    Directory.CreateDirectory(stlDir);
                }
                
                                
                string systemDir = DOM.workingDirectory + @"\system\";

                if (!Directory.Exists(systemDir))
                {
                    Directory.CreateDirectory(systemDir);
                }

                Point3d locationInMesh = new Point3d();
                locationInMesh = DOM.locationInMesh;

                File.WriteAllText(Path.Combine(systemDir + "snappyHexMeshDict"), StringTemplates.snappyHexMeshDict(acc, locationInMesh));
                File.WriteAllText(Path.Combine(systemDir + "surfaceFeatureExtractDict"), StringTemplates.surfaceFeatureExtractDict());


                string command = DOM.CPU > 1 ? MultipleCPU: SingleCPU;

                
                ProcessStartInfo psi = new ProcessStartInfo(@"C:\Users\pkastner\Documents\GitHub\WindTunnel\CallOF\bin\CallOF.exe", " -e " + command + " -f " + DOM.workingDirectory);

                psi.UseShellExecute = false;
                psi.WorkingDirectory = DOM.workingDirectory;


                Process p = new Process();
                p.StartInfo = psi;
                p.Start();
                p.WaitForExit();



                string logFile = "";

                using (FileStream stream = File.Open(DOM.workingDirectory + @"\log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        logFile = reader.ReadToEnd();
                        //while (!reader.EndOfStream)
                        //{

                        //}

                    }
                }

                DA.SetData(0, logFile);

                if (logFile.Contains("End")) {      AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Super!!"); }
               // else if (logFile.Contains("End")) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Fast Super!!"); }
                else { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Nicht Super!!"); }


                DA.SetData(1, DOM);

            }
            //else
            //{
            //    return;
            //}


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
            get { return new Guid("{6836B42F-FB09-48AF-BF41-A85D9D8FD913}"); }
        }
    }
}
