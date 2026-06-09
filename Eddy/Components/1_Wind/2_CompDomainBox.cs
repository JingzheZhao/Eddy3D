using EddyLib;
using EddyLib.BCs;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class BlockMeshBox : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }

        /// <summary>
        /// Each implementation of GH_Component must provide a public constructor without any
        /// arguments. Category represents the Tab in which the component will appear, Subcategory
        /// the panel. If you use non-existing tab or panel names, new tabs/panels will automatically
        /// be created.
        /// </summary>
        public BlockMeshBox()
          : base("Box Domain", "DomBox",
@"Rectangular Simulation Domain

Defines a box-shaped computational domain for the wind simulation. Best suited for single-direction wind analysis or wind tunnel comparisons.

" + EddyVersion.toString(),
              EddyVersion.Name, "1 | Wind")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter(
                "Buildings", "Bldg",
                "Building geometry (Breps or Meshes). These create wall boundary conditions in the CFD mesh.",
                GH_ParamAccess.list);

            pManager.AddGeometryParameter(
                "Terrain", "Terr",
                "Optional: Ground surface geometry. Must extend beyond domain bounds. If omitted, a flat ground is assumed.",
                GH_ParamAccess.list);
            pManager[1].Optional = true;

            pManager.AddGenericParameter(
                "Trees", "Tree",
                "Optional: Tree/vegetation objects from Tree component. Creates porous zones for wind resistance.",
                GH_ParamAccess.list);
            pManager[2].Optional = true;

            pManager.AddGenericParameter(
                "Boundary Condition", "BC",
                "Wind inlet conditions from ABL Flow or Uniform Flow component. Defines wind speed, direction, and turbulence.",
                GH_ParamAccess.item);
            pManager[3].Optional = true;

            pManager.AddNumberParameter(
                "Cell Size", "Cell",
                "Base mesh cell size. Units: meters. Smaller = more accurate but slower. Typical: 5-20m. Default: 20m",
                GH_ParamAccess.item, 20);
            pManager[4].Optional = true;

            pManager.AddNumberParameter(
                "Length", "Len",
                "Domain length (wind direction). Units: meters. Recommend: 15-20x building height.",
                GH_ParamAccess.item);

            pManager.AddNumberParameter(
                "Width", "Wid",
                "Domain width (cross-wind). Units: meters. Recommend: 10x building width.",
                GH_ParamAccess.item);

            pManager.AddNumberParameter(
                "Height", "Hgt",
                "Domain height. Units: meters. Recommend: 5-6x tallest building height.",
                GH_ParamAccess.item);
            pManager[5].Optional = true;
            pManager[6].Optional = true;
            pManager[7].Optional = true;
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Domain", "Dom", "CFD domain object for Wind Simulation component", GH_ParamAccess.item);
            pManager.AddGenericParameter("Preview Mesh", "Prev", "Domain boundary mesh for visualization", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">
        /// The DA object can be used to retrieve data from input parameters and to store data in
        /// output parameters.
        /// </param>

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            _previewMeshes = null;

            //DOMAIN GEOMETRY
            List<IGH_GeometricGoo> geoGooDomain = new List<IGH_GeometricGoo>();
            DA.GetDataList("Buildings", geoGooDomain);
            List<GeometryBase> buildings = new List<GeometryBase>();

            foreach (IGH_GeometricGoo g in geoGooDomain)
            {
                if (g != null)
                {
                    if (g.CastTo<GeometryBase>(out GeometryBase gb))
                    {
                        buildings.Add(gb);
                    }
                }
            }

            //TERRAIN GEOMETRY
            List<IGH_GeometricGoo> terrainGoo = new List<IGH_GeometricGoo>();
            List<GeometryBase> terrain = new List<GeometryBase>();
            DA.GetDataList("Terrain", terrainGoo);

            foreach (IGH_GeometricGoo g in terrainGoo)
            {
                if (g != null)
                {
                    if (g.CastTo<GeometryBase>(out GeometryBase gb))
                    {
                        terrain.Add(gb);
                    }
                }
            }

            if (Utilities.CheckForDuplicates(buildings))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"Duplicate Geometries might lead to a crashing simulation. Please find duplicates with ""SelDup"" and remove them.");
            }

            if (Utilities.CheckForDuplicates(terrain))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"Duplicate Geometries might lead to a crashing simulation. Please find duplicates with ""SelDup"" and remove them.");
            }

            #region Trees

            List<Tree> trees = new List<Tree>();

            DA.GetDataList("Trees", trees);

            #endregion Trees

            BCCollection bCond;

            GH_ObjectWrapper gobj = null;
            if (!DA.GetData("Boundary Condition", ref gobj)) { }
            if ((gobj != null && gobj.Value is BCCollection))
            {
                bCond = (BCCollection)gobj.Value;
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide a valid Boundary Condition object"); return;
            }

            if (bCond.BCs.Count > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "For box-shaped domains you can only pass one wind direction per simulation setup."); return;
            }

            double blockDimension = 20;
            DA.GetData("Cell Size", ref blockDimension);

            Mesh buildingGeometry = new Mesh();
            MeshingParameters mp = new MeshingParameters();

            double width = 0;
            double length = 0;
            double height = 0;

            DA.GetData("Width", ref width);
            DA.GetData("Length", ref length);
            DA.GetData("Height", ref height);

            Mesh terrainMeshes = new Mesh();

            if (terrain.Count == 0)
            {
                // AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "If you don't provide a terrain,
                // Eddy will use a standard ground plane."); return;
            }
            else // (terrain.Count > 0)
            {
                var terrainList = new System.Collections.Generic.List<Mesh>();
                foreach (GeometryBase b in terrain)
                {
                    if (b.ObjectType == Rhino.DocObjects.ObjectType.Mesh)
                    {
                        terrainList.Add((Mesh)b);
                    }
                    else if (b.ObjectType == Rhino.DocObjects.ObjectType.Brep || b.ObjectType == Rhino.DocObjects.ObjectType.Extrusion || b.ObjectType == Rhino.DocObjects.ObjectType.Surface)
                    {
                        Mesh[] m = Mesh.CreateFromBrep((Brep)b, mp);
                        if (m != null) terrainList.AddRange(m);
                    }
                }
                terrainMeshes.Append(terrainList);
            }

            if (buildings == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please reference an input geometry."); return;
            }
            else
            {
                var buildingList = new System.Collections.Generic.List<Mesh>();
                foreach (GeometryBase b in buildings)
                {
                    if (b.ObjectType == Rhino.DocObjects.ObjectType.Mesh)
                    {
                        buildingList.Add((Mesh)b);
                    }
                    else if (b.ObjectType == Rhino.DocObjects.ObjectType.Brep || b.ObjectType == Rhino.DocObjects.ObjectType.Extrusion || b.ObjectType == Rhino.DocObjects.ObjectType.Surface)
                    {
                        Mesh[] m = Mesh.CreateFromBrep((Brep)b, mp);
                        if (m != null) buildingList.AddRange(m);
                    }
                }
                buildingGeometry.Append(buildingList);
            }

            // For radiation simulation

            buildingGeometry.UserDictionary.Set("type", "Building");
            terrainMeshes.UserDictionary.Set("type", "Ground");

            // Check if lowest point in Domain is below zGround, then we cannot use an ABL

            var minZDomain = buildingGeometry.GetBoundingBox(true).Min.Z;

            if (bCond.BCs.All(item => item is ABL))
            {
                foreach (BC bcond in bCond.BCs)
                {
                    var bc = (ABL)bcond;
                    double zg = bc.zGround;

                    if (minZDomain < zg)
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Your building geometry extends below zGround (" + zg + "). Please use the Constant U Boundary Condition or adjust zGround accordingly."); return;
                    }
                }
            }

            if (Utilities.CheckLicence() == true)
            {
                OFBoxDomain DOMBOX = new OFBoxDomain(buildingGeometry, terrainMeshes, bCond, blockDimension, length, width, height, trees);

                FillWindDirRenderList(bCond, DOMBOX);

                Message = $"Cell: {blockDimension}m";

                DA.SetData(0, DOMBOX);

                if (DOMBOX.HasTerrain)
                {
                    DA.SetDataList(1, new List<GeometryBase> { DOMBOX.TerrainMesh, DOMBOX.DomainMesh });
                    _previewMeshes = new List<Mesh> { DOMBOX.TerrainMesh, DOMBOX.DomainMesh };
                }
                else
                {
                    DA.SetData(1, DOMBOX.DomainMesh);
                    _previewMeshes = new List<Mesh> { DOMBOX.DomainMesh };
                }
            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Licence expired."); return;
            }
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>

                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                Properties.Resources.Eddy_domainBox;

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{0AD4BDF7-33AC-492D-ABF0-622A5488C8E2}");

        private List<Point3d> _pointWindDirRender;

        private List<Vector3d> _vecsWindDirRender;

        private List<Line> _inletProfileGuides;

        private List<Mesh> _previewMeshes;

        private static readonly Color MeshWireColor = Color.FromArgb(104, 104, 104);
        private static readonly Color InletAirColor = Color.FromArgb(56, 88, 249);

        private void FillWindDirRenderList(BCCollection bCond, OFBoxDomain DOM)
        {
            //clear
            _pointWindDirRender = new List<Point3d>();
            _vecsWindDirRender = new List<Vector3d>();
            _inletProfileGuides = new List<Line>();

            if (DOM?.DomainMesh == null || bCond?.BCs == null || bCond.BCs.Count == 0)
            {
                return;
            }

            var bbox = DOM.DomainMesh.GetBoundingBox(true);
            double zMin = bbox.Min.Z;
            double zMax = bbox.Max.Z;
            if (zMax <= zMin)
            {
                zMax = zMin + 1.0;
            }

            var centerBottom = Utilities.CenterBottomBoundingBox(DOM.DomainMesh);
            var rayOrigin = centerBottom;
            rayOrigin.Z = 0.5 * (zMin + zMax);
            const int sampleCount = 8;
            double maxArrowLength = Math.Max(bbox.Diagonal.Length * 0.08, 2.0);
            const double referenceSpeed = 10.0; // m/s for viewport scaling
            double speedToLength = maxArrowLength / referenceSpeed;
            double maxRenderableArrowLength = maxArrowLength * 1.25;

            foreach (BC bc in bCond.BCs)
            {
                var boundaryPoint = GetBoundaryAnchor(DOM.DomainMesh, rayOrigin, bc.flowDir);
                var basePoint = new Point3d(boundaryPoint.X, boundaryPoint.Y, zMin);

                _inletProfileGuides.Add(new Line(
                    new Point3d(basePoint.X, basePoint.Y, zMin),
                    new Point3d(basePoint.X, basePoint.Y, zMax)));

                for (int i = 0; i < sampleCount; i++)
                {
                    double t = sampleCount == 1 ? 0.0 : (double)i / (sampleCount - 1);
                    double z = zMin + t * (zMax - zMin);

                    // OpenFOAM atmosphericBoundaryLayerInletVelocity profile:
                    // U(z) = Uref * log((z - zGround + z0)/z0) / log((Zref + z0)/z0)
                    double speed = GetPreviewSpeedAtHeight(bc, z);

                    var vec = bc.flowDir;
                    if (vec.IsTiny())
                    {
                        vec = Vector3d.XAxis;
                    }
                    double arrowLength = Math.Min(Math.Max(speed, 0.0) * speedToLength, maxRenderableArrowLength);
                    vec *= arrowLength;

                    _pointWindDirRender.Add(new Point3d(basePoint.X, basePoint.Y, z));
                    _vecsWindDirRender.Add(vec);
                }
            }
        }

        private static double GetPreviewSpeedAtHeight(BC bc, double worldZ)
        {
            if (bc is ABL abl)
            {
                return ComputeOpenFoamAblVelocity(abl, worldZ);
            }

            return Math.Max(bc?.URef ?? 0.0, 0.0);
        }

        private static double ComputeOpenFoamAblVelocity(ABL abl, double worldZ)
        {
            if (abl == null)
            {
                return 0.0;
            }

            double z0 = Math.Max(abl.z0, 1e-6);
            double zRef = Math.Max(abl.zref, z0 + 1e-6);
            double denominator = Math.Log((zRef + z0) / z0);
            if (Math.Abs(denominator) < 1e-12)
            {
                return Math.Max(abl.URef, 0.0);
            }

            double zTerm = Math.Max(worldZ - abl.zGround + z0, z0 * 1.000001);
            double numerator = Math.Log(zTerm / z0);
            double velocity = abl.URef * (numerator / denominator);

            return Math.Max(velocity, 0.0);
        }

        private static Point3d GetBoundaryAnchor(Mesh domainMesh, Point3d rayOrigin, Vector3d flowDir)
        {
            var dir = -flowDir;
            if (!dir.Unitize() || dir.IsTiny())
            {
                dir = Vector3d.XAxis;
            }

            var ray = new Ray3d(rayOrigin, dir);
            double dist = Rhino.Geometry.Intersect.Intersection.MeshRay(domainMesh, ray);
            if (dist > 0)
            {
                return ray.PointAt(dist);
            }

            return rayOrigin + (dir * Math.Max(domainMesh.GetBoundingBox(true).Diagonal.Length * 0.5, 1.0));
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            if (_previewMeshes != null)
            {
                foreach (var mesh in _previewMeshes)
                {
                    if (mesh != null)
                    {
                        args.Display.DrawMeshWires(mesh, MeshWireColor);
                    }
                }
            }

            if (this.Locked || _pointWindDirRender == null || _pointWindDirRender.Count == 0 || _vecsWindDirRender == null || _vecsWindDirRender.Count == 0)
            {
                return;
            }

            if (this.Attributes.Selected)
            {
                if (_inletProfileGuides != null)
                {
                    foreach (var guide in _inletProfileGuides)
                    {
                        if (guide.IsValid)
                        {
                            args.Display.DrawLine(guide, InletAirColor, 1);
                        }
                    }
                }

                // Draw wind dir arrows
                for (int i = 0; i < _pointWindDirRender.Count; i++)
                {
                    var l = new Line(_pointWindDirRender[i], _vecsWindDirRender[i]);
                    args.Display.DrawArrow(l, InletAirColor, 25, 0);
                }

                return;
            }
            else
            {
                if (_inletProfileGuides != null)
                {
                    foreach (var guide in _inletProfileGuides)
                    {
                        if (guide.IsValid)
                        {
                            args.Display.DrawLine(guide, InletAirColor, 1);
                        }
                    }
                }

                // Draw wind dir arrows
                for (int i = 0; i < _pointWindDirRender.Count; i++)
                {
                    var l = new Line(_pointWindDirRender[i], _vecsWindDirRender[i]);
                    args.Display.DrawArrow(l, InletAirColor, 25, 0);
                }

                return;
            }
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            // Show domain preview as wireframe-only (no shaded mesh faces).
        }
    }
}
