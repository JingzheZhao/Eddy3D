using Rhino.Geometry;

namespace EddyLib.Indoor
{
    public class MomentumSource : FunctionObject
    {
        public double Relaxation { get; set; }

        public Vector3d Ubar { get; set; }

        // only if selectionModel is cellZone

        public MomentumSource(Mesh Geometry, Vector3d Ubar, string Name)
        {
            this.Name = Name;
            this.Geometry = Geometry;
            this.Ubar = Ubar;
        }

        //public MomentumSource(GeometryBase Geometries, Vector3d Ubar, string Name)
        //{
        //    this.Name = Name;
        //}

        public MomentumSource()
        {
        }

        //public MomentumSource Duplicate()
        //{
        //    MomentumSource dup = new MomentumSource(Geometry, Ubar, Name);
        //    return dup;
        //}
    }
}