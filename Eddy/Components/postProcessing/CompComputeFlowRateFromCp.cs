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
          : base("ComputeFlowRateFromCp", "FlowRateCp", "Compute flow rates from pressure coefficients", "Eddy", "PostProcessing")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Sim", "Sim", "Sim", GH_ParamAccess.item);
            pManager.AddGenericParameter("cp values", "Cp", "List of two averaged cp values.", GH_ParamAccess.list);
            //pManager.AddGenericParameter("cp values", "Cp2", "List of cp values.", GH_ParamAccess.list);
            //pManager.AddGenericParameter("Area", "Area", "Area to be evaluated.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh surfaces", "Mesh", "List of two mesh surfaces to be evaluated.", GH_ParamAccess.list);
            //pManager.AddMeshParameter("Mesh", "Mesh2", "List of mesh surfaces to be evaluated.", GH_ParamAccess.item);

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




            var meshes = new List<Mesh>();
            

            DA.GetDataList(2, meshes);
        


            

            StringBuilder errorLog = new StringBuilder();

           


            double AverageCp1 = 0;
            double AverageCp2 = 0;
            double Min = 0;
            double Max = 0;
            
            double Area1 = 0;
            double Area2 = 0;
            double VolumetricFlowRate = 0;

            Mesh mesh1 = meshes[0];
            Mesh mesh2 = meshes[1];

            try
            {


                var listOfInputCps1 = new List<double>();
                var listOfInputCps2 = new List<double>();

                DA.GetDataList(1, listOfInputCps1);
                //DA.GetDataList(2, listOfInputCps2);





                for (int i = 0; i < mesh1.Faces.Count; i++)
                {
                    Area1 += (Utilities.MeshFaceArea(i, mesh1));
                }

                for (int i = 0; i < mesh2.Faces.Count; i++)
                {
                    Area2 += (Utilities.MeshFaceArea(i, mesh2));
                }



                //int cnt1 = 0;


                // Compute average flow rate for all probes



                //foreach (double cp in listOfInputCps1)
                //{
                //    AverageCp1 += cp;                   
                //    cnt1++;
                //}


                //AverageCp1 = AverageCp1 / cnt1; //
                AverageCp1 = listOfInputCps1[0];

                //

                //int cnt2 = 0;
                //
                //// Compute average flow rate for all probes
                //
                //
                //
                //foreach (double cp in listOfInputCps2)
                //{
                //    AverageCp2 += cp;
                //    cnt2++;
                //}


                //AverageCp2 = AverageCp2 / cnt2; //
                AverageCp2 = listOfInputCps1[1];

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



