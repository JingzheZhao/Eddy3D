using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class CompComputeFlowRateFromCp : GH_Component
    {




        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CompComputeFlowRateFromCp()
          : base("ComputeFlowRateFromCp", "FlowRateCp", "PostProcessing", "Eddy", "PostProcessing")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Sim", "Sim", "Sim", GH_ParamAccess.item);
            pManager.AddGenericParameter("cp values", "Cp1", "List of cp values.", GH_ParamAccess.list);
            pManager.AddGenericParameter("cp values", "Cp2", "List of cp values.", GH_ParamAccess.list);
            //pManager.AddGenericParameter("Area", "Area", "Area to be evaluated.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh", "Mesh1", "List of mesh surfaces to be evaluated.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh", "Mesh2", "List of mesh surfaces to be evaluated.", GH_ParamAccess.item);

            //pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item, false);


        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("Points", "Points", "Points", GH_ParamAccess.list);
            pManager.AddNumberParameter("Result", "Res", "Result: Volumetric flow rate in m^3/s.", GH_ParamAccess.item);
            //pManager.AddNumberParameter("Min", "Min", "Minimum value in m/s", GH_ParamAccess.item);
            //pManager.AddNumberParameter("Max", "Max", "Maximum value in m/s", GH_ParamAccess.item);
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

            if ((gobj.Value is OFBaseDomain))
            {
                DOM = (OFBaseDomain)gobj.Value;
            }
            if (DOM == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid domain object"); return; }




            var mesh1 = new Mesh();
            var mesh2 = new Mesh();


            DA.GetData(3, ref mesh1);
            DA.GetData(4, ref mesh2);
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


            double AverageCp1 = 0;
            double AverageCp2 = 0;
            double Min = 0;
            double Max = 0;
            //var Magnitudes = new List<double>();
            //double VolumetricFlowRate = 0.0;
            double Area1 = 0;
            double Area2 = 0;
            double VolumetricFlowRate = 0;

            try
            {


                var listOfInputCps1 = new List<double>();
                var listOfInputCps2 = new List<double>();

                DA.GetDataList(1, listOfInputCps1);
                DA.GetDataList(2, listOfInputCps2);





                for (int i = 0; i < mesh1.Faces.Count; i++)
                {
                    Area1 += (Utilities.MeshFaceArea(i, mesh1));
                }

                for (int i = 0; i < mesh2.Faces.Count; i++)
                {
                    Area2 += (Utilities.MeshFaceArea(i, mesh2));
                }



                int cnt1 = 0;


                // Compute average flow rate for all probes



                foreach (double cp in listOfInputCps1)
                {
                    AverageCp1 += cp;                   
                    cnt1++;
                }


                AverageCp1 = AverageCp1 / cnt1; //

               
                //

                int cnt2 = 0;

                // Compute average flow rate for all probes



                foreach (double cp in listOfInputCps2)
                {
                    AverageCp2 += cp;
                    cnt2++;
                }


                AverageCp2 = AverageCp2 / cnt2; //

                var C_D_general = 0.7;
                var C_D_tot_A = ((C_D_general*Area1*C_D_general*Area2)/Math.Sqrt(Math.Pow(C_D_general*Area1,2)+ Math.Pow(C_D_general * Area1, 2)));

               
               double deltaCp = Math.Abs(AverageCp1 - AverageCp2);

                VolumetricFlowRate = C_D_tot_A * DOM.BCInflow.UatBuildingHeight * Math.Sqrt( deltaCp);
               
                                              

            }







            catch (Exception e) { Console.WriteLine(e.Message); };// File.WriteAllText(DOM.baseWorkingDirectory + @"\FlowRate.err", errorLog.ToString()); return; }





            DA.SetData(0, VolumetricFlowRate);
            //DA.SetData(1, Min);
            //DA.SetData(2, Max);


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
        public override Guid ComponentGuid => new Guid("{C5789855-FF0B-4713-A33B-ECD2A594EC25}");
    }
}



