//using Eddy.Properties;
//using EddyLib;
//using EddyLib.Indoor;
//using Grasshopper.Kernel;
//using Grasshopper.Kernel.Types;
//using Newtonsoft.Json;
//using Rhino.Geometry;
//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Eddy.Components.Indoor.Params
//{
//    public class IndoorResultgGoo : GH_Goo<IndoorResult>, IGH_PreviewData
//    {
//        #region constructors

//        public IndoorResultgGoo()
//        {
//            this.Value = new IndoorResult();
//        }

//        // constructor with initial value
//        public IndoorResultgGoo(IndoorResult indomVal)
//        {
//            this.Value = indomVal;
//        }

//        // copy constructor
//        public IndoorResultgGoo(IndoorResultgGoo indom)
//        {
//            this.Value = indom.Value;
//        }

//        public override IGH_Goo Duplicate()
//        {
//            return new IndoorResultgGoo(Value == null ? new IndoorResult() : Value.Duplicate());
//        }

//        #endregion constructors

//        #region properties

//        // return a string with the name of this Type.
//        public override string TypeName
//        {
//            get { return "IndoorResult"; }
//        }

//        // return a string describing what this Type is about.
//        public override string TypeDescription
//        {
//            get { return "IndoorResult"; }
//        }

//        // return a string representation of the state (value) of this instance.
//        public override string ToString()
//        {
//            string s = "";
//            if (Value != null)
//            {
//                //  s = "Velocity [m/s]: " + Value.Length + " Temperature [K]: " + Value.TemperatureK;  //JsonConvert.SerializeObject(this.Value, Formatting.Indented);
//            }

//            // to string
//            return "[IndoorResult] " + s;
//        }

//        // serlialize
//        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
//        {
//            var json = JsonConvert.SerializeObject(this.Value, Formatting.None);
//            writer.SetString("IndoorResult", json);

//            return true;
//        }

//        // deserialize
//        public override bool Read(GH_IO.Serialization.GH_IReader reader)
//        {
//            var json = reader.GetString("IndoorResult");
//            if (!String.IsNullOrWhiteSpace(json))
//            {
//                this.Value = JsonConvert.DeserializeObject<IndoorResult>(json);
//            }

//            return true;
//        }

//        public override bool IsValid
//        {
//            get
//            {
//                if (Value == null) { return false; }
//                return true;
//            }
//        }

//        public override string IsValidWhyNot
//        {
//            get
//            {
//                if (Value == null) { return "No internal instance"; }
//                if (true) { return string.Empty; }

//                //return "Invalid instance"; //Todo: beef this up to be more informative.
//            }
//        }

//        #endregion properties

//        #region drawing methods

//        //    public BoundingBox ClippingBox
//        //    {
//        //        get { return this.Value.BoundingBox; }
//        //    }

//        //    public void DrawViewportMeshes(GH_PreviewMeshArgs args)
//        //    {
//        //        if (Value == null) { return; }

//        //        //if (Value.BoundingBox != null)
//        //        //{
//        //        //    args.Pipeline.DrawMeshShaded(Value.BoundingBox, args.Material);
//        //        //}
//        //    }

//        //    public void DrawViewportWires(GH_PreviewWireArgs args)
//        //    {
//        //        if (Value == null) { return; }

//        //        if (Value.Edges != null)
//        //        {
//        //            foreach (var pt in Value.Edges)
//        //            {
//        //                args.Pipeline.DrawPoint(pt, args.Color);
//        //            }
//        //        }
//        //    }

//        #endregion drawing methods
//    }

//    public class Param_IndoorResult : GH_PersistentParam<IndoorResultgGoo>, IGH_PreviewObject
//    {
//        // we need to supply a constructor without arguments that calls the base class constructor.
//        public Param_IndoorResult() :
//          base(new GH_InstanceDescription("Result", "Res", "Result for indoor simulations", EddyVersion.Name, "9 | Indoor"))

//        { }

//        // unique id
//        public override Guid ComponentGuid
//        {
//            get { return new Guid("{D72C2A1D-F5A6-4640-A91B-5934C4282805}"); }
//        }

//        // hidden parameter
//        public override GH_Exposure Exposure
//        {
//            get { return GH_Exposure.obscure; }
//        }

//        // icon
//        protected override Bitmap Icon
//        {
//            get
//            {
//                return Resources.Eddy_Indoor_Domain_Param;
//            }
//        }

//        //We do not allow users to pick inlets,
//        //therefore the following 4 methods disable all this ui.
//        protected override GH_GetterResult Prompt_Plural(ref List<IndoorResultgGoo> values)
//        {
//            return GH_GetterResult.cancel;
//        }

//        protected override GH_GetterResult Prompt_Singular(ref IndoorResultgGoo value)
//        {
//            return GH_GetterResult.cancel;
//        }

//        protected override System.Windows.Forms.ToolStripMenuItem Menu_CustomSingleValueItem()
//        {
//            System.Windows.Forms.ToolStripMenuItem item = new System.Windows.Forms.ToolStripMenuItem();
//            item.Text = "Not available";
//            item.Visible = false;
//            return item;
//        }

//        protected override System.Windows.Forms.ToolStripMenuItem Menu_CustomMultiValueItem()
//        {
//            System.Windows.Forms.ToolStripMenuItem item = new System.Windows.Forms.ToolStripMenuItem();
//            item.Text = "Not available";
//            item.Visible = false;
//            return item;
//        }

//        #region preview methods

//        public BoundingBox ClippingBox
//        {
//            get
//            {
//                return Preview_ComputeClippingBox();
//            }
//        }

//        public void DrawViewportMeshes(IGH_PreviewArgs args)
//        {
//            Preview_DrawMeshes(args);
//        }

//        public void DrawViewportWires(IGH_PreviewArgs args)
//        {
//            //Use a standard method to draw gunk, you don't have to specifically implement this.
//            Preview_DrawWires(args);
//        }

//        private bool m_hidden = false;

//        public bool Hidden
//        {
//            get { return m_hidden; }
//            set { m_hidden = value; }
//        }

//        public bool IsPreviewCapable
//        {
//            get { return true; }
//        }

//        #endregion preview methods
//    }
//}