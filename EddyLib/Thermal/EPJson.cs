using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Thermal
{
   public class EPJson
    {
        [JsonProperty("Shading:Building:Detailed", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, ShadingBuildingDetailed> AllShaders { get; set; } = new Dictionary<string, ShadingBuildingDetailed>();

        [JsonProperty("BuildingSurface:Detailed", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, BuildingSurfaceDetailed> AllThermalSurfaces { get; set; } = new Dictionary<string, BuildingSurfaceDetailed>();

        [JsonProperty("FenestrationSurface:Detailed", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, FenestrationSurfaceDetailed> AllWindows { get; set; } = new Dictionary<string, FenestrationSurfaceDetailed>();
    }





    public  class FenestrationSurfaceDetailed
    {
        [JsonProperty("building_surface_name")]
        public string BuildingSurfaceName { get; set; }

        [JsonProperty("construction_name")]
        public string ConstructionName { get; set; } = "DoublePaneClr";

        //[JsonProperty("frame_and_divider_name", NullValueHandling = NullValueHandling.Ignore)]
        //public string FrameAndDividerName { get; set; }

        //[JsonProperty("multiplier", NullValueHandling = NullValueHandling.Ignore)]
        //public double? Multiplier { get; set; }

        //[JsonProperty("number_of_vertices", NullValueHandling = NullValueHandling.Ignore)]
        //public int NumberOfVertices { get; set; }

        //[JsonProperty("outside_boundary_condition_object", NullValueHandling = NullValueHandling.Ignore)]
        //public string OutsideBoundaryConditionObject { get; set; }

        [JsonProperty("surface_type")]
        public FenestrationSurfaceDetailedSurfaceType SurfaceType { get; set; } = FenestrationSurfaceDetailedSurfaceType.Window;

        [JsonProperty("vertex_1_x_coordinate")]
        public double Vertex1_XCoordinate { get; set; }

        [JsonProperty("vertex_1_y_coordinate")]
        public double Vertex1_YCoordinate { get; set; }

        [JsonProperty("vertex_1_z_coordinate")]
        public double Vertex1_ZCoordinate { get; set; }

        [JsonProperty("vertex_2_x_coordinate")]
        public double Vertex2_XCoordinate { get; set; }

        [JsonProperty("vertex_2_y_coordinate")]
        public double Vertex2_YCoordinate { get; set; }

        [JsonProperty("vertex_2_z_coordinate")]
        public double Vertex2_ZCoordinate { get; set; }

        [JsonProperty("vertex_3_x_coordinate")]
        public double Vertex3_XCoordinate { get; set; }

        [JsonProperty("vertex_3_y_coordinate")]
        public double Vertex3_YCoordinate { get; set; }

        [JsonProperty("vertex_3_z_coordinate")]
        public double Vertex3_ZCoordinate { get; set; }

        [JsonProperty("vertex_4_x_coordinate", NullValueHandling = NullValueHandling.Ignore)]
        public double? Vertex4_XCoordinate { get; set; }

        [JsonProperty("vertex_4_y_coordinate", NullValueHandling = NullValueHandling.Ignore)]
        public double? Vertex4_YCoordinate { get; set; }

        [JsonProperty("vertex_4_z_coordinate", NullValueHandling = NullValueHandling.Ignore)]
        public double? Vertex4_ZCoordinate { get; set; }

        [JsonProperty("view_factor_to_ground", NullValueHandling = NullValueHandling.Ignore)]
        public string ViewFactorToGround { get; set; } = "Autocalculate";

        [JsonProperty("idf_max_extensible_fields", NullValueHandling = NullValueHandling.Ignore)]
        public double? idfMaxExtensibleFields { get; set; } = 0;
        [JsonProperty("idf_max_fields", NullValueHandling = NullValueHandling.Ignore)]
        public double? idfMaxFields { get; set; } = 21;
    }

    public  class BuildingSurfaceDetailed
    {
        [JsonProperty("construction_name")]
        public string ConstructionName { get; set; } = "RedBrick";

        [JsonProperty("number_of_vertices", NullValueHandling = NullValueHandling.Ignore)]
        public int NumberOfVertices { get; set; }

        [JsonProperty("outside_boundary_condition")]
        public BuildingSurfaceDetailedOutsideBoundaryCondition OutsideBoundaryCondition { get; set; } = BuildingSurfaceDetailedOutsideBoundaryCondition.Outdoors;

        //[JsonProperty("outside_boundary_condition_object", NullValueHandling = NullValueHandling.Ignore)]
        //public string OutsideBoundaryConditionObject { get; set; }

        [JsonProperty("sun_exposure", NullValueHandling = NullValueHandling.Ignore)]
        public BuildingSurfaceDetailedSunExposure? SunExposure { get; set; } = BuildingSurfaceDetailedSunExposure.SunExposed;

        [JsonProperty("surface_type")]
        public BuildingSurfaceDetailedSurfaceType SurfaceType { get; set; } = BuildingSurfaceDetailedSurfaceType.Wall;

        [JsonProperty("vertices", NullValueHandling = NullValueHandling.Ignore)]
        public List<DetailedVertex> Vertices { get; set; }

        [JsonProperty("view_factor_to_ground", NullValueHandling = NullValueHandling.Ignore)]
        public string ViewFactorToGround { get; set; } = "Autocalculate";

        [JsonProperty("wind_exposure", NullValueHandling = NullValueHandling.Ignore)]
        public WindExposure? WindExposure { get; set; } = Thermal.WindExposure.WindExposed;

        [JsonProperty("zone_name")]
        public string ZoneName { get; set; } = "UNZ_0";
    }

    public partial class ShadingBuildingDetailed
    {
        [JsonProperty("number_of_vertices", NullValueHandling = NullValueHandling.Ignore)]
        public int NumberOfVertices { get; set; }

        [JsonProperty("transmittance_schedule_name", NullValueHandling = NullValueHandling.Ignore)]
        public string TransmittanceScheduleName { get; set; } = "Off";

        [JsonProperty("vertices", NullValueHandling = NullValueHandling.Ignore)]
        public List<DetailedVertex> Vertices { get; set; }
    }

    public partial class DetailedVertex
    {
        [JsonProperty("vertex_x_coordinate")]
        public double X { get; set; }

        [JsonProperty("vertex_y_coordinate")]
        public double Y { get; set; }

        [JsonProperty("vertex_z_coordinate")]
        public double Z { get; set; }
    }





    [JsonConverter(typeof(StringEnumConverter))]

    public enum FenestrationSurfaceDetailedSurfaceType { Door, GlassDoor, TubularDaylightDiffuser, TubularDaylightDome, Window };
    [JsonConverter(typeof(StringEnumConverter))]

    public enum BuildingSurfaceDetailedOutsideBoundaryCondition { Adiabatic, Foundation, Ground, GroundBasementPreprocessorAverageFloor, GroundBasementPreprocessorAverageWall, GroundBasementPreprocessorLowerWall, GroundBasementPreprocessorUpperWall, GroundFCfactorMethod, GroundSlabPreprocessorAverage, GroundSlabPreprocessorCore, GroundSlabPreprocessorPerimeter, OtherSideCoefficients, OtherSideConditionsModel, Outdoors, Surface, Zone };
    [JsonConverter(typeof(StringEnumConverter))]

    public enum BuildingSurfaceDetailedSunExposure { Empty, NoSun, SunExposed };
    [JsonConverter(typeof(StringEnumConverter))]

    public enum BuildingSurfaceDetailedSurfaceType { Ceiling, Floor, Roof, Wall };
    [JsonConverter(typeof(StringEnumConverter))]

    public enum WindExposure { Empty, NoWind, WindExposed };
    
}
