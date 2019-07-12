//using Eddy.Properties;
//using EddyLib;
//using Grasshopper.Kernel;
//using Grasshopper.Kernel.Parameters;
//using Rhino.Geometry;
//using System;
//using System.Collections.Generic;
//// In order to load the result of this wizard, you will also need to
//// add the output bin/ folder of this project to the list of loaded
//// folder in Grasshopper.
//// You can use the _GrasshopperDeveloperSettings Rhino command for that.

//namespace Eddy
//{
//    public class OSSettings : GH_Component
//    {
//        private object windowsVersion;

// //List<double> defaultDir = new List<double>(0); /// <summary> /// Each implementation of
// GH_Component must provide a public /// constructor without any arguments. /// Category represents
// the Tab in which the component will appear, /// Subcategory the panel. If you use non-existing tab
// or panel names, /// new tabs/panels will automatically be created. /// </summary> public
// OSSettings() : base("OS", "OS", "OSSettings", "Eddy", "Misc") { //dirs.Add(0); } /// <summary> ///
// Registers all the input parameters for this component. /// </summary> ///

// protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager) {
// pManager.AddIntegerParameter("Windows Version", "Windows Version", "Windows Version",
// GH_ParamAccess.list, 1); Param_Integer param = pManager[0] as Param_Integer;
// param.AddNamedValue("Windows 10", 0); param.AddNamedValue("Windows 8", 1);
// param.AddNamedValue("Windows 7", 2); pManager[0].Optional = true;

// }

// ///
// <summary>
// /// Registers all the output parameters for this component. ///
// </summary>
// protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager) { }

// ///
// <summary>
// /// This is the method that actually does the work. ///
// </summary>
// ///
// <param name="DA">
// The DA object can be used to retrieve data from input parameters and /// to store data in output parameters.
// </param>
// protected override void SolveInstance(IGH_DataAccess DA) { int windowsVersionInt = 0; string
// windowsVersion = "";

// Environment.OSVersion;

// DA.GetData(0, ref windowsVersionInt);

// if (windowsVersionInt == 0) { windowsVersion = "Windows10"; } else if( windowsVersionInt == 1) {
// windowsVersion = "Windows8"; } else { windowsVersion = "Windows7"; }

// Settings s = new Settings(); s.WindowsVersion = windowsVersion; s.SaveToFile()

// }

// ///
// <summary>
// /// Provides an Icon for every component that will be visible in the User Interface. /// Icons
// need to be 24x24 pixels. ///
// </summary>
// protected override System.Drawing.Bitmap Icon =&gt; // You can add image files to your project
// resources and access them like this: Resources.Eddy_abl;// return null;

//        /// <summary>
//        /// Each component must have a unique Guid to identify it.
//        /// It is vital this Guid doesn't change otherwise old ghx files
//        /// that use the old ID will partially fail during loading.
//        /// </summary>
//        public override Guid ComponentGuid => new Guid("{F00C8EDE-6374-4E0F-BFB3-93E46D674193}");
//    }
//}