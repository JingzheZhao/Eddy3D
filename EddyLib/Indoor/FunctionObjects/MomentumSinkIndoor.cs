using Rhino.Geometry;

namespace EddyLib.Indoor
{
    /// <summary>
    /// Represents a momentum sink region for indoor simulations.
    /// </summary>
    public class MomentumSinkIndoor : FunctionObject
    {
        /// <summary>
        /// Creates a momentum sink with specified geometry and name.
        /// </summary>
        public MomentumSinkIndoor(Mesh Geometry, string Name)
        {
            this.Name = Name;
            this.Geometry = Geometry;
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MomentumSinkIndoor()
        {
        }
    }
}