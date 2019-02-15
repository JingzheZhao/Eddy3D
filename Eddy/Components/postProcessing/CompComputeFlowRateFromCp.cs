using Eddy.Properties;
using EddyLib;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
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
            pManager.AddNumberParameter("cp values", "Cp", "List cp values per mesh surface.", GH_ParamAccess.tree);
            //pManager.AddNumberParameter("cd values", "Cd", "List of cd values per mesh surface.", GH_ParamAccess.list);
            //pManager.AddGenericParameter("cp values", "Cp2", "List of cp values.", GH_ParamAccess.list);
            //pManager.AddGenericParameter("Area", "Area", "Area to be evaluated.", GH_ParamAccess.item);
            pManager.AddMeshParameter("Mesh surfaces", "Mesh", "List of mesh surfaces to be evaluated.", GH_ParamAccess.list);
            pManager.AddNumberParameter("Volume", "Volume", "Volume to calculate the air change rate of a zone.", GH_ParamAccess.item);

            //pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item, false);
            //pManager[4].Optional = true;

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("Points", "Points", "Points", GH_ParamAccess.list);
            pManager.AddNumberParameter("FR", "FR", "Volumetric flow rate in m^3/s.", GH_ParamAccess.item);
            pManager.AddNumberParameter("vCenter", "vCenter", "Velocity at center node in m/s under the assumption of a pipe flow.", GH_ParamAccess.item);
            pManager.AddNumberParameter("ACR", "ACR", "Air change rate in 1/h", GH_ParamAccess.item);
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

            var cdList = new List<double>();


            var meshes = new List<Mesh>();
            //DA.GetDataList(2, cdList);

            DA.GetDataList(2, meshes);


            //if (cdList.Count == 0)
            //{

                foreach (Mesh m in meshes)
                {
                    cdList.Add(0.7);
                }

            //}

            double volume = 0;

            DA.GetData(3, ref volume);

            //private readonly List<double> cdList = new List<double> { 0.7, 0.7 };


            StringBuilder errorLog = new StringBuilder();

            //var cps = new DataTree<double>();
            GH_Structure<GH_Number> cps_ghnumber;
            DA.GetDataTree(1, out cps_ghnumber);

            int cnt = 0;
            var cps = new DataTree<double>();
            foreach (var b in cps_ghnumber.Branches)
            {
                var path = cps_ghnumber.get_Path(cnt);
                cnt++;
                foreach (var i in b) {

                    cps.Add((double)i.Value, path);

                }
            }



            //double AverageCp1 = 0;
            //double AverageCp2 = 0;


            //double Area1 = 0;
            //double Area2 = 0;
            var VolumetricFlowRate = new DataTree<double>(); 
            var VelocityCenterNode = new DataTree<double>();
            var ACR = new DataTree<double>();
            //Mesh mesh1 = meshes[0];
            //Mesh mesh2 = meshes[1];

            try
            {


               

                List<double> MeshAreas = new List<double>();

                foreach (Mesh m in meshes)
                {
                    double singleArea = 0;
                    for (int i = 0; i < m.Faces.Count; i++)
                    {
                        singleArea += Utilities.MeshFaceArea(i, m);
                    }
                    MeshAreas.Add(singleArea);
                }




                if (cps.Paths.Count != DOM.BCInflow.windDirs.Count) return;
                foreach (var path in cps.Paths)
                { 

                    if (GH_Document.IsEscapeKeyDown())
                    {
                        GH_Document GHDocument = OnPingDocument();
                        GHDocument.RequestAbortSolution();
                    }


                    NVAnalysis nv1 = new NVAnalysis(cps.Branch(path), MeshAreas, DOM.BCInflow.UatBuildingHeight, volume);




                    VolumetricFlowRate.Add(nv1.FlowRate, path);
                    VelocityCenterNode.Add(nv1.vCenter, path);


                    if (volume != 0)
                    {
                        ACR.Add(nv1.ACR, path);
                    }
                }




                //for (int i = 0; i < DOM.BCInflow.windDirs.Count; ++i)
                //{
                //    GH_Path path = new GH_Path(i);
                    

                //    if (GH_Document.IsEscapeKeyDown())
                //    {
                //        GH_Document GHDocument = OnPingDocument();
                //        GHDocument.RequestAbortSolution();
                //    }


                //    NVAnalysis nv1 = new NVAnalysis(cps.Paths, MeshAreas, DOM.BCInflow.UatBuildingHeight, volume);




                //    VolumetricFlowRate.Branches[i].Add(nv1.FlowRate);
                //    VelocityCenterNode.Branches[i].Add(nv1.vCenter);


                //    if (volume != 0)
                //    {
                //        ACR.Add(nv1.ACR);
                //    }

                //}
            }

            catch (Exception e) { Console.WriteLine(e.Message); };// File.WriteAllText(DOM.baseWorkingDirectory + @"\FlowRate.err", errorLog.ToString()); return; }

            DA.SetDataTree(0, VolumetricFlowRate);
            DA.SetDataTree(1, VelocityCenterNode);
            DA.SetDataTree(2, ACR);
           
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



