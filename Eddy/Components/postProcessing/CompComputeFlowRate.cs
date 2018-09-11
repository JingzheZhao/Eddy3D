using Eddy.Properties;
using EddyLib;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompComputeFlowRate : GH_Component
    {
        private readonly DataTree<double> cpTree = new DataTree<double>();
        private readonly DataTree<Vector3d> uTree = new DataTree<Vector3d>();




        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CompComputeFlowRate()
          : base("ComputeFlowRate", "FlowRate", "PostProcessing", "Eddy", "PostProcessing")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            //pManager.AddGenericParameter("Sim", "Sim", "Sim", GH_ParamAccess.item);
            pManager.AddVectorParameter("cp values/velocity vectors", "Inp", "List of cp values or velocity vectors depending on mode.", GH_ParamAccess.list);
            //pManager.AddGenericParameter("Area", "Area", "Area to be evaluated.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh", "Mesh", "Mesh surface to be evaluated.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Mode", "Mode", "Mode", GH_ParamAccess.item, 1);
            Param_Integer param = pManager[2] as Param_Integer;
            //param.AddNamedValue("from cp", 0);
            param.AddNamedValue("from U", 1);


            //pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item, false);


        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("Points", "Points", "Points", GH_ParamAccess.list);
            pManager.AddNumberParameter("Result", "Res", "Result: Volumetric flow rate in m^3/s.", GH_ParamAccess.item);
            pManager.AddNumberParameter("Min", "Min", "Minimum value in m/s", GH_ParamAccess.item);
            pManager.AddNumberParameter("Max", "Max", "Maximum value in m/s", GH_ParamAccess.item);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            //OFBaseDomain DOM = null;



            //GH_ObjectWrapper gobj = null;
            //if (!DA.GetData(0, ref gobj)) { }

            //if ((gobj.Value is OFBaseDomain))
            //{
            //    DOM = (OFBaseDomain)gobj.Value;
            //}
            //if (DOM == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid domain object"); return; }

            var listOfInputVelocities = new List<Vector3d>();

            

            int mode = 1;
            //List<Point3d> listOfPoints = new List<Point3d>();

            bool run = false;
            var mesh = new Mesh();

            DA.GetDataList(0, listOfInputVelocities);
            DA.GetData(1, ref mesh);
            DA.GetData(2, ref mode);
            //DA.GetData(3, ref run);

           
 
                

            // Inclusion check for probes

            //// Filter the list
            //int kept = 0;
            //for (int i = 0; i < listOfPoints.Count; i++)
            //{
            //    // Test whether this is an element that we want to keep.
            //    if (DOM.inputBreps.IsPointInside(listOfPoints[i], 0.01, true) == false)
            //    {
            //        // Add it to the list of kept elements.
            //        listOfPoints[kept] = listOfPoints[i];
            //        kept++;
            //    }
            //}
            //// Unfortunately IList has no Resize method. So instead we
            //// remove the last element of the list until: elements.Count == kept.
            //while (kept < listOfPoints.Count)
            //{
            //    listOfPoints.RemoveAt(listOfPoints.Count - 1);
            //}

            //var numberOfProbes = listOfPoints.Count();


            // Error handling

            StringBuilder errorLog = new StringBuilder();

            //if (numberOfProbes < 1)
            //{
            //    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "You need to pass a list of point to the component.");
            //}


            //var numberOfWindDirs = DOM.BCInflow.windDir.Count;



            //for (int i = 0; i < numberOfWindDirs; i++)

            //{
            //    var fp = DOM.baseWorkingDirectory + @"\" + DOM.BCInflow.windDir[i] + @"\system\U_Probes";
            //    if (!File.Exists(fp))
            //    {
            //        errorLog.AppendLine(@"The wind direction """ + DOM.BCInflow.windDir[i] + @""" misses the probing dictionary. Please connect the ""writeProbes"" component and recompute the solution.");
            //        throw new System.ArgumentException("The wind direction " + DOM.BCInflow.windDir[i] + @" misses the probing dictionary. Please connect the component ""writeProbes"" and recompute the solution.");
            //    }
            //}

            //for (int i = 0; i < numberOfWindDirs; i++)
            //{
            //    var fp = DOM.baseWorkingDirectory + @"\mesh\constant\polyMesh";
            //    if (!Directory.Exists(fp))
            //    {
            //        errorLog.AppendLine(@"The wind direction """ + DOM.BCInflow.windDir[i] + @""" misses the ""\constant\polyMesh"" dictionary. Please make sure that directory exists.");
            //        throw new System.ArgumentException("The wind direction " + DOM.BCInflow.windDir[i] + @" misses the ""\constant\polyMesh"" dictionary. Please make sure that directory exists.");
            //    }
            //}

            //for (int i = 0; i < numberOfWindDirs; i++)
            //{
            //    var ABLfilePath = DOM.baseWorkingDirectory + "\\" + DOM.BCInflow.windDir[i] + @"\0.org\ABLConditions";
            //    if (!File.Exists(ABLfilePath)) { Console.WriteLine(ABLfilePath + " not found. Exiting"); errorLog.AppendLine(ABLfilePath + " not found. Exiting"); }
            //}


            //// Check if U file is in last iteration
            //for (int i = 0; i < DOM.BCInflow.windDir.Count; i++)
            //{
            //    string iter = Utilities.GetLastIterationInSimfolder(DOM.baseWorkingDirectory + @"\" + DOM.BCInflow.windDir[i]).ToString();
            //    string fp = DOM.baseWorkingDirectory + @"\" + DOM.BCInflow.windDir[i] + @"\" + iter + @"\U";


            //    if (!File.Exists(fp))
            //    {
            //        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"The simulation folder of the wind direction """ + DOM.BCInflow.windDir[i] + @""" misses the velocity (U) result file. Please make sure that U is calculated for this particular timestep (change WriteInterval) and recompute the solution.");
            //    }
            //}



            //// export pts file for Daysim
            //if (!Directory.Exists(DOM.baseWorkingDirectory + @"Rad\"))
            //{
            //    Directory.CreateDirectory(DOM.baseWorkingDirectory + @"Rad\");
            //}
            //RadianceFiles.writePTS(DOM.baseWorkingDirectory + @"\Rad\sensors.pts", listOfPoints);

            //if (Utilities.IsDirectoryEmpty(DOM.meshPolyMeshDirectory) == true)
            //{
            //    throw new System.ArgumentException("The mesh folder is empty. Can't retrieve probes from a mesh that does not exist.");
            //}


            double AverageFlowRate = 0;
            double Min = 0;
            double Max = 0;
            var Magnitudes = new List<double>();
            double VolumetricFlowRate = 0.0;
            double Area = 0;

            

            try
            {


                if (mode == 1) // U
                {
                    int cnt = 0;

                    // Compute average flow rate for all probes

                    foreach (Vector3d U in listOfInputVelocities)
                    {
                        AverageFlowRate += U.Length;
                        Magnitudes.Add(U.Length);
                        cnt++;
                    }


                    AverageFlowRate = AverageFlowRate / cnt; // m/s

                    for (int i = 0; i< mesh.Faces.Count; i++)
                    {
                        Area += (Utilities.MeshFaceArea(i, mesh));
                    }

                    

                    VolumetricFlowRate = AverageFlowRate * Area;



                    Min = Magnitudes.Any() ? Magnitudes.Min(x => x) : 0;
                    Max = Magnitudes.Any() ? Magnitudes.Max(x => x) : 0;

                }


            }


            catch (Exception e) { Console.WriteLine(e.Message); };// File.WriteAllText(DOM.baseWorkingDirectory + @"\FlowRate.err", errorLog.ToString()); return; }





            DA.SetData(0, VolumetricFlowRate);
            DA.SetData(1, Min);
            DA.SetData(2, Max);


        }


        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                Resources.Eddy_probes;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{4ABC334E-1FEC-41B2-9852-D005151DD79B}");
    }
}



