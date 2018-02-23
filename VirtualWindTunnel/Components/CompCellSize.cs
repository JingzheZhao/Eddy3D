using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Text;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;
using System.Threading;
using Grasshopper.Kernel.Types;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace WindTunnel
{
    public class CellSize : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public CellSize()
          : base("CellSize", "CellSize",
              "CellSize",
              "Eddy", "Mesh")
        {
        }

        

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Domain", "Domain", "Domain", GH_ParamAccess.item);
            pManager.AddNumberParameter("blockMeshCellSize", "blockMeshCellSize", "Cell size in meters", GH_ParamAccess.item);
            pManager.AddNumberParameter("desiredCellSize", "desiredCellSize", "Cell size in meters", GH_ParamAccess.item);
            //pManager.AddBooleanParameter("", "Run", "Clean the directory", GH_ParamAccess.item, false);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddIntegerParameter("Acc", "Acc", "Accuracy needed", GH_ParamAccess.item);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {


            double desiredCellSize = 1;
            double blockMeshCellSize = 1;
            double acc = 1;

            OFCylDomain CylDom;
            OFBoxDomain BoxDom;
            OFBaseDomain DOM;

            GH_ObjectWrapper gobj = null;
            if (!DA.GetData(0, ref gobj)) { }

            if ((gobj.Value is OFCylDomain))
            {
                CylDom = (OFCylDomain)gobj.Value;
                DOM = (OFBaseDomain)gobj.Value;

                //blockMeshCellSize = Math.Abs(CylDom.ListOfAllPointsInMagicOrder[145].X - CylDom.ListOfAllPointsInMagicOrder[136].X);

            }
            else if ((gobj.Value is OFBoxDomain))
            {
                BoxDom = (OFBoxDomain)gobj.Value;
                DOM = (OFBaseDomain)gobj.Value;
            }

            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide a valid domain object");
                return;
            }

            DA.GetData(1, ref blockMeshCellSize);
            DA.GetData(2, ref desiredCellSize);

            

            acc = Math.Round(((Math.Log(blockMeshCellSize) - Math.Log(desiredCellSize)) / Math.Log(2)));





            DA.SetData(0, acc);

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
            get { return new Guid("{BFAD64ED-FACE-4D30-8A3A-64877F1609F2}"); }
        }
    }
}
