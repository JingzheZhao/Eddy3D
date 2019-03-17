//using Eddy.Properties;
//using EddyLib;
//using Grasshopper.GUI;
//using Grasshopper.GUI.Canvas;
//using Grasshopper.Kernel;
//using Grasshopper.Kernel.Attributes;
//using Grasshopper.Kernel.Types;
//using Rhino.Geometry;
//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.IO;
//using System.Linq;

//// In order to load the result of this wizard, you will also need to
//// add the output bin/ folder of this project to the list of loaded
//// folder in Grasshopper.
//// You can use the _GrasshopperDeveloperSettings Rhino command for that.

//namespace Eddy
//{
//    public class ReadResults : GH_Component
//    {

//        public string ResultType = "Hours";

//        // override read write so that component remebers last state
//        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
//        {
//            writer.SetString("ResultType", ResultType);
//            return base.Write(writer);
//        }
//        public override bool Read(GH_IO.Serialization.GH_IReader reader)
//        {
//            ResultType = reader.GetString("ResultType");
//            return base.Read(reader);
//        }


//        public override void CreateAttributes()
//        {
//            m_attributes = new CustomAttributes(this);
//        }

//        public class CustomAttributes : GH_ComponentAttributes
//        {


//            public CustomAttributes(ReadResults owner) : base(owner) { }

//            #region Custom layout logic
//            private RectangleF isComfortHours { get; set; }
//            private RectangleF isHumanCondition { get; set; }
//            private RectangleF isUTCIByHour { get; set; }
//            private RectangleF isUTCIByProbe { get; set; }


//            protected override void Layout()
//            {
//                base.Layout();
//                //We'll extend the basic layout by adding three regions to the bottom of this component
//                isComfortHours = new RectangleF(Bounds.X, Bounds.Bottom, Bounds.Width, 20);
//                isHumanCondition = new RectangleF(Bounds.X, Bounds.Bottom + 20, Bounds.Width, 20);
//                isUTCIByHour = new RectangleF(Bounds.X, Bounds.Bottom + 40, Bounds.Width, 20);
//                isUTCIByProbe = new RectangleF(Bounds.X, Bounds.Bottom + 60, Bounds.Width, 20);
//                Bounds = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height + 80);
//            }
//            #endregion

//            #region Custom Mouse handling
//            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
//            {
//                if (e.Button == System.Windows.Forms.MouseButtons.Left)
//                {
//                    ReadResults comp = Owner as ReadResults;

//                    if (isComfortHours.Contains(e.CanvasLocation))
//                    {
//                        if (comp.ResultType == "Hours") return GH_ObjectResponse.Handled;
//                        comp.RecordUndoEvent("Hours");
//                        comp.ResultType = "Hours";
//                        comp.ExpireSolution(true);
//                        return GH_ObjectResponse.Handled;
//                    }

//                    if (isHumanCondition.Contains(e.CanvasLocation))
//                    {
//                        if (comp.ResultType == "Condition") return GH_ObjectResponse.Handled;
//                        comp.RecordUndoEvent("Condition");
//                        comp.ResultType = "Condition";
//                        comp.ExpireSolution(true);
//                        return GH_ObjectResponse.Handled;
//                    }

//                    if (isUTCIByHour.Contains(e.CanvasLocation))
//                    {
//                        if (comp.ResultType == "UTCI/h") return GH_ObjectResponse.Handled;
//                        comp.RecordUndoEvent("UTCI/h");
//                        comp.ResultType = "UTCI/h";
//                        comp.ExpireSolution(true);
//                        return GH_ObjectResponse.Handled;
//                    }

//                    if (isUTCIByProbe.Contains(e.CanvasLocation))
//                    {
//                        if (comp.ResultType == "UTCI/p") return GH_ObjectResponse.Handled;
//                        comp.RecordUndoEvent("UTCI/p");
//                        comp.ResultType = "UTCI/p";
//                        comp.ExpireSolution(true);
//                        return GH_ObjectResponse.Handled;
//                    }
//                }
//                return base.RespondToMouseDown(sender, e);
//            }
//            #endregion

//            #region Custom Render logic
//            protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
//            {
//                switch (channel)
//                {
//                    case GH_CanvasChannel.Objects:
//                        //We need to draw everything outselves.
//                        base.RenderComponentCapsule(canvas, graphics, true, true, false, true, true, true);


//                        ReadResults comp = Owner as ReadResults;

//                        GH_Capsule buttonHours = GH_Capsule.CreateCapsule(isComfortHours, comp.ResultType == "Hours" ? GH_Palette.Black : GH_Palette.White);
//                        buttonHours.Render(graphics, this.Selected, Owner.Locked, Owner.Hidden);
//                        buttonHours.Dispose();

//                        GH_Capsule buttonCondition = GH_Capsule.CreateCapsule(isHumanCondition, comp.ResultType == "Condition" ? GH_Palette.Black : GH_Palette.White);
//                        buttonCondition.Render(graphics, this.Selected, Owner.Locked, Owner.Hidden);
//                        buttonCondition.Dispose();

//                        GH_Capsule buttonUTCIh = GH_Capsule.CreateCapsule(isUTCIByHour, comp.ResultType == "UTCI/h" ? GH_Palette.Black : GH_Palette.White);
//                        buttonUTCIh.Render(graphics, this.Selected, Owner.Locked, Owner.Hidden);
//                        buttonUTCIh.Dispose();

//                        GH_Capsule buttonUTCIp = GH_Capsule.CreateCapsule(isUTCIByProbe, comp.ResultType == "UTCI/p" ? GH_Palette.Black : GH_Palette.White);
//                        buttonUTCIp.Render(graphics, this.Selected, Owner.Locked, Owner.Hidden);
//                        buttonUTCIp.Dispose();

//                        graphics.DrawString("Hours", GH_FontServer.Standard, comp.ResultType == "Hours" ? Brushes.White : Brushes.Black, isComfortHours, GH_TextRenderingConstants.CenterCenter);
//                        graphics.DrawString("Condition", GH_FontServer.Standard, comp.ResultType == "Condition" ? Brushes.White : Brushes.Black, isHumanCondition, GH_TextRenderingConstants.CenterCenter);
//                        graphics.DrawString("UTCI/h", GH_FontServer.Standard, comp.ResultType == "UTCI/h" ? Brushes.White : Brushes.Black, isUTCIByHour, GH_TextRenderingConstants.CenterCenter);
//                        graphics.DrawString("UTCI/p", GH_FontServer.Standard, comp.ResultType == "UTCI/p" ? Brushes.White : Brushes.Black, isUTCIByProbe, GH_TextRenderingConstants.CenterCenter);


//                        break;
//                    default:
//                        base.Render(canvas, graphics, channel);
//                        break;
//                }
//            }
//            #endregion
//        }









//        /// <summary>
//        /// Each implementation of GH_Component must provide a public 
//        /// constructor without any arguments.
//        /// Category represents the Tab in which the component will appear, 
//        /// Subcategory the panel. If you use non-existing tab or panel names, 
//        /// new tabs/panels will automatically be created.
//        /// </summary>
//        public ReadResults()
//          : base("Read Results", "Read", "Read Simulation Results", "Eddy", "6 | Outdoor Comfort")
//        {
//        }



//        /// <summary>
//        /// Registers all the input parameters for this component.
//        /// </summary>
//        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
//        {
//            pManager.AddGenericParameter("Domain", "Dom", "Base Domain", GH_ParamAccess.item);
//            pManager.AddIntervalParameter("Interval", "Int", "Interval to be evaluated [0-8760]", GH_ParamAccess.item, new Interval(0,8760));
//            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item, false);

//        }

//        /// <summary>
//        /// Registers all the output parameters for this component.
//        /// </summary>
//        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
//        {
//            pManager.AddGenericParameter("Results", "Res", "Simulation Results", GH_ParamAccess.list);
//        }



//        /// <summary>
//        /// This is the method that actually does the work.
//        /// </summary>
//        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
//        /// to store data in output parameters.</param>
//        protected override void SolveInstance(IGH_DataAccess DA)
//        {

//            OFBaseDomain DOM = null;
//            GH_ObjectWrapper gobj = null;
//            if (!DA.GetData(0, ref gobj)) { }

//            if ((gobj.Value is OFBaseDomain))
//            {
//                DOM = (OFBaseDomain)gobj.Value;
//            }
//            if (DOM == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid domain object"); return; }

//            Interval interval = new Interval();
//            DA.GetData(1, ref interval);


//            if(interval.T0 < 0 || interval.T1 > 8760) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Analysis interval must be between hour 0 and hour 8760"); return; }



//            bool Run = false;
//            DA.GetData(2, ref Run);



//            if (Run)
//            {



//                if (ResultType == "Hours")
//                {
//                    //TODO: implement logic
//                }
//                else if (ResultType == "Condition")
//                {
//                    //TODO: implement logic
//                }
//                else if (ResultType == "UTCI/h")
//                {
//                    //TODO: implement logic
//                }
//                else if (ResultType == "UTCI/p")
//                {
//                    //TODO: implement logic
//                }




//            }

//        }




//        /// <summary>
//        /// Provides an Icon for every component that will be visible in the User Interface.
//        /// Icons need to be 24x24 pixels.
//        /// </summary>
//        protected override System.Drawing.Bitmap Icon =>
//        // You can add image files to your project resources and access them like this:
//        //null;
//        Resources.Eddy_parseU;

//        /// <summary>
//        /// Each component must have a unique Guid to identify it. 
//        /// It is vital this Guid doesn't change otherwise old ghx files 
//        /// that use the old ID will partially fail during loading.
//        /// </summary>
//        public override Guid ComponentGuid => new Guid("{7D64BCD2-0F68-49E5-8A49-238D98BC347B}");
//    }
//}