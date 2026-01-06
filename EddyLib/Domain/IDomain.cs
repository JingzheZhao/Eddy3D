using Rhino.Geometry;

namespace EddyLib.Domain
{
    /// <summary>
    /// Core interface for CFD simulation domains.
    /// Defines the essential geometry and properties required for any domain type.
    /// </summary>
    public interface IDomain
    {
        /// <summary>
        /// The mesh representing building geometry within the domain.
        /// </summary>
        Mesh BuildingGeometry { get; }

        /// <summary>
        /// The mesh representing the computational domain boundary.
        /// </summary>
        Mesh DomainMesh { get; }

        /// <summary>
        /// The terrain mesh, if present.
        /// </summary>
        Mesh TerrainMesh { get; }

        /// <summary>
        /// The center point at ground level.
        /// </summary>
        Point3d CenterGround { get; }

        /// <summary>
        /// A point inside the mesh domain, used for mesh generation.
        /// </summary>
        Point3d LocationInMesh { get; }

        /// <summary>
        /// Maximum height of buildings in the domain.
        /// </summary>
        double MaxHeightBuilding { get; }

        /// <summary>
        /// Indicates whether terrain geometry is present.
        /// </summary>
        bool HasTerrain { get; }

        /// <summary>
        /// The bounding box of the domain.
        /// </summary>
        Box BoundingBox { get; }
    }
}
