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
    public class IndoorInletGoo : GH_Goo<IndoorBC.Inlet>, IGH_PreviewData
    {
        #region constructors

        public IndoorInletGoo()
        {
            this.Value = new IndoorBC.Inlet();
        }

        // constructor with initial value
        public IndoorInletGoo(IndoorBC.Inlet indoorGeoValue)
        {
            this.Value = indoorGeoValue;
        }

        // copy constructor
        public IndoorInletGoo(IndoorInletGoo indoorGeoSource)
        {
            this.Value = indoorGeoSource.Value;
        }

        public override IGH_Goo Duplicate()
        {
            return new IndoorInletGoo(Value == null ? new IndoorBC.Inlet() : Value.Duplicate());
        }

        #endregion constructors

        #region properties

        // return a string with the name of this Type.
        public override string TypeName
        {
            get { return "IndoorBC Inlet"; }
        }

        // return a string describing what this Type is about.
        public override string TypeDescription
        {
            get { return "IndoorBC Inlet"; }
        }

        // return a string representation of the state (value) of this instance.
        public override string ToString()
        {
            string s = "";
            if (Value != null)
            {
                s = "Velocity [m/s]: " + Value.Velocity.Length + " Temperature [K]: " + Value.TemperatureK;  //JsonConvert.SerializeObject(this.Value, Formatting.Indented);
            }

            // to string
            return "[Inlet] " + s;
        }

        // serlialize
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            var json = JsonConvert.SerializeObject(this.Value, Formatting.None);
            writer.SetString("IndoorBC_Inlet", json);

            return true;
        }

        // deserialize
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            var json = reader.GetString("IndoorBC_Inlet");
            if (!String.IsNullOrWhiteSpace(json))
            {
                this.Value = JsonConvert.DeserializeObject<IndoorBC.Inlet>(json);
            }

            return true;
        }

        public override bool IsValid
        {
            get
            {
                if (Value == null) { return false; }
                return true;
            }
        }

        public override string IsValidWhyNot
        {
            get
            {
                if (Value == null) { return "No internal instance"; }
                if (true) { return string.Empty; }

                //return "Invalid instance"; //Todo: beef this up to be more informative.
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

            if (Value.Velocity != null && Value.Centroid != null)
            {
                args.Pipeline.DrawArrow(new Line(Value.Centroid, Value.Velocity * 10), args.Color);
            }
        }

        #endregion drawing methods
    }

    public class Param_IndoorBC_Inlet : GH_PersistentParam<IndoorInletGoo>, IGH_PreviewObject
    {
        // we need to supply a constructor without arguments that calls the base class constructor.
        public Param_IndoorBC_Inlet() :
          base(new GH_InstanceDescription("Inlet", "Inlet",
              "Inlet (Indoor Boundary Condition)", EddyVersion.Name, "9 | Indoor"))
        { }

        // unique id
        public override Guid ComponentGuid
        {
            get { return new Guid("{EB07F9FB-78B1-43B6-8B9C-718E37DF1D9E}"); }
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
                return Resources.Eddy_Indoor_Inlet_Param;
            }
        }

        //We do not allow users to pick inlets,
        //therefore the following 4 methods disable all this ui.
        protected override GH_GetterResult Prompt_Plural(ref List<IndoorInletGoo> values)
        {
            return GH_GetterResult.cancel;
        }

        protected override GH_GetterResult Prompt_Singular(ref IndoorInletGoo value)
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