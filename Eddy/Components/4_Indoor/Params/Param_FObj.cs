using Eddy.Properties;
using EddyLib;
using EddyLib.Indoor;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Eddy.Components.Indoor.Params
{
    public class FunctionObjectGoo : GH_Goo<FunctionObject>, IGH_PreviewData
    {
        #region constructors

        public FunctionObjectGoo()
        {
            this.Value = new FunctionObject();
        }

        // constructor with initial value
        public FunctionObjectGoo(FunctionObject indoorGeoValue)
        {
            this.Value = indoorGeoValue;
        }

        // copy constructor
        public FunctionObjectGoo(FunctionObjectGoo indoorGeoSource)
        {
            this.Value = indoorGeoSource.Value;
        }

        public override IGH_Goo Duplicate()
        {
            return new FunctionObjectGoo(Value == null ? new FunctionObject() : Value.Duplicate());
        }

        #endregion constructors

        #region properties

        // return a string with the name of this Type.
        public override string TypeName
        {
            get { return "FunctionObject"; }
        }

        // return a string describing what this Type is about.
        public override string TypeDescription
        {
            get { return "FunctionObject"; }
        }

        // return a string representation of the state (value) of this instance.
        public override string ToString()
        {
            string s = "";
            if (Value != null)
            {
                //  s = "Velocity [m/s]: " + Value.Velocity.Length + " Temperature [K]: " + Value.TemperatureK;  //JsonConvert.SerializeObject(this.Value, Formatting.Indented);
            }

            // to string
            return "[FunctionObject] " + Value.Name;
        }

        // serlialize
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            var json = JsonConvert.SerializeObject(this.Value, Formatting.None);
            writer.SetString("FunctionObject", json);

            return true;
        }

        // deserialize
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            var json = reader.GetString("FunctionObject");
            if (!String.IsNullOrWhiteSpace(json))
            {
                this.Value = JsonConvert.DeserializeObject<FunctionObject>(json);
            }

            return true;
        }

        public override bool IsValid
        {
            get
            {
                if (Value == null) { return false; }
                if (Value.Geometry == null || !Value.Geometry.IsValid) { return false; }
                return true;
            }
        }

        public override string IsValidWhyNot
        {
            get
            {
                if (Value == null) { return "No internal instance"; }
                if (Value.Geometry == null || !Value.Geometry.IsValid) { return "No valid geometry mesh in internal instance"; }
                return string.Empty;
            }
        }

        #endregion properties

        #region drawing methods

        public BoundingBox ClippingBox
        {
            get { return this.Value.Geometry.GetBoundingBox(true); }
        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            if (Value == null) { return; }
            if (Value.Geometry != null)
            {
                args.Pipeline.DrawMeshShaded(Value.Geometry, args.Material);
            }
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            if (Value == null) { return; }

            //if (Value.Velocity != null && Value.Centroid != null)
            //{
            //    args.Pipeline.DrawArrow(new Line(Value.Centroid, Value.Velocity * 10), args.Color);
            //}
        }

        #endregion drawing methods
    }

    public class Param_FunctionObject : GH_PersistentParam<FunctionObjectGoo>, IGH_PreviewObject
    {
        // we need to supply a constructor without arguments that calls the base class constructor.
        public Param_FunctionObject() :
          base(new GH_InstanceDescription("FunctionObject", "FO", "FunctionObject", EddyVersion.Name, "9 | Indoor"))
        { }

        // unique id
        public override Guid ComponentGuid
        {
            get { return new Guid("{4F45E063-9126-47E2-A4BC-FE0D0E6E4E7A}"); }
        }

        // hidden parameter
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.obscure; }
        }

        // icon
        protected override Bitmap Icon
        {
            get
            {
                return Resources.Eddy_Indoor_Emitter_Param;
            }
        }

        //We do not allow users to pick inlets,
        //therefore the following 4 methods disable all this ui.
        protected override GH_GetterResult Prompt_Plural(ref List<FunctionObjectGoo> values)
        {
            return GH_GetterResult.cancel;
        }

        protected override GH_GetterResult Prompt_Singular(ref FunctionObjectGoo value)
        {
            return GH_GetterResult.cancel;
        }

        protected override System.Windows.Forms.ToolStripMenuItem Menu_CustomSingleValueItem()
        {
            System.Windows.Forms.ToolStripMenuItem item = new System.Windows.Forms.ToolStripMenuItem();
            item.Text = "Not available";
            item.Visible = false;
            return item;
        }

        protected override System.Windows.Forms.ToolStripMenuItem Menu_CustomMultiValueItem()
        {
            System.Windows.Forms.ToolStripMenuItem item = new System.Windows.Forms.ToolStripMenuItem();
            item.Text = "Not available";
            item.Visible = false;
            return item;
        }

        #region preview methods

        public BoundingBox ClippingBox
        {
            get
            {
                return Preview_ComputeClippingBox();
            }
        }

        public void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            Preview_DrawMeshes(args);
        }

        public void DrawViewportWires(IGH_PreviewArgs args)
        {
            //Use a standard method to draw gunk, you don't have to specifically implement this.
            Preview_DrawWires(args);
        }

        private bool m_hidden = false;

        public bool Hidden
        {
            get { return m_hidden; }
            set { m_hidden = value; }
        }

        public bool IsPreviewCapable
        {
            get { return true; }
        }

        #endregion preview methods
    }
}