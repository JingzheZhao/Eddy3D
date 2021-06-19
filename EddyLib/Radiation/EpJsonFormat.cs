using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace EddyLib.Radiation
{
    public class EpJsonFormat
    {
        [JsonProperty("Shading:Building:Detailed", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, ShadingBuildingDetailed> AllShaders { get; set; } = new Dictionary<string, ShadingBuildingDetailed>();

        [JsonProperty("BuildingSurface:Detailed", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, BuildingSurfaceDetailed> AllThermalSurfaces { get; set; } = new Dictionary<string, BuildingSurfaceDetailed>();

        [JsonProperty("FenestrationSurface:Detailed", NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, FenestrationSurfaceDetailed> AllWindows { get; set; } = new Dictionary<string, FenestrationSurfaceDetailed>();
    }

    public class FenestrationSurfaceDetailed
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

    public class BuildingSurfaceDetailed
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
        public WindExposure? WindExposure { get; set; } = Radiation.WindExposure.WindExposed;

        [JsonProperty("zone_name")]
        public string ZoneName { get; set; } = "UNZ_0";
    }

    public class ShadingBuildingDetailed
    {
        [JsonProperty("number_of_vertices", NullValueHandling = NullValueHandling.Ignore)]
        public int NumberOfVertices { get; set; }

        [JsonProperty("transmittance_schedule_name", NullValueHandling = NullValueHandling.Ignore)]
        public string TransmittanceScheduleName { get; set; } = "Off";

        [JsonProperty("vertices", NullValueHandling = NullValueHandling.Ignore)]
        public List<DetailedVertex> Vertices { get; set; }
    }

    public class DetailedVertex
    {
        [JsonProperty("vertex_x_coordinate")]
        public double X { get; set; }

        [JsonProperty("vertex_y_coordinate")]
        public double Y { get; set; }

        [JsonProperty("vertex_z_coordinate")]
        public double Z { get; set; }
    }

    public class Material
    {
        [JsonProperty("conductivity")]
        public double Conductivity { get; set; }

        [JsonProperty("density")]
        public double Density { get; set; }

        [JsonProperty("roughness")]
        public RoughnessOfCollectorEnum Roughness { get; set; }

        [JsonProperty("solar_absorptance", NullValueHandling = NullValueHandling.Ignore)]
        public double? SolarAbsorptance { get; set; }

        [JsonProperty("specific_heat")]
        public double SpecificHeat { get; set; }

        [JsonProperty("thermal_absorptance", NullValueHandling = NullValueHandling.Ignore)]
        public double? ThermalAbsorptance { get; set; }

        [JsonProperty("thickness")]
        public double Thickness { get; set; }

        [JsonProperty("visible_absorptance", NullValueHandling = NullValueHandling.Ignore)]
        public double? VisibleAbsorptance { get; set; }
    }

    public class Construction
    {
        //[JsonProperty("layer_10", NullValueHandling = NullValueHandling.Ignore)]
        //public string Layer10 { get; set; }

        //[JsonProperty("layer_4", NullValueHandling = NullValueHandling.Ignore)]
        //public string Layer4 { get; set; }

        //[JsonProperty("layer_5", NullValueHandling = NullValueHandling.Ignore)]
        //public string Layer5 { get; set; }

        //[JsonProperty("layer_6", NullValueHandling = NullValueHandling.Ignore)]
        //public string Layer6 { get; set; }

        //[JsonProperty("layer_7", NullValueHandling = NullValueHandling.Ignore)]
        //public string Layer7 { get; set; }

        //[JsonProperty("layer_8", NullValueHandling = NullValueHandling.Ignore)]
        //public string Layer8 { get; set; }

        //[JsonProperty("layer_9", NullValueHandling = NullValueHandling.Ignore)]
        //public string Layer9 { get; set; }

        [JsonProperty("layer_3", NullValueHandling = NullValueHandling.Ignore)]
        public string Layer3 { get; set; }

        [JsonProperty("layer_2", NullValueHandling = NullValueHandling.Ignore)]
        public string Layer2 { get; set; }

        [JsonProperty("outside_layer")]
        public string OutsideLayer { get; set; }

        //[JsonProperty("idf_max_extensible_fields", NullValueHandling = NullValueHandling.Ignore)]
        //public double? idfMaxExtensibleFields { get; set; } = 0;
        //[JsonProperty("idf_max_fields", NullValueHandling = NullValueHandling.Ignore)]
        //public double? idfMaxFields { get; set; }
    }

    public class MaterialRoofVegetation
    {
        [JsonProperty("conductivity_of_dry_soil", NullValueHandling = NullValueHandling.Ignore)]
        public double? ConductivityOfDrySoil { get; set; }

        [JsonProperty("density_of_dry_soil", NullValueHandling = NullValueHandling.Ignore)]
        public double? DensityOfDrySoil { get; set; }

        [JsonProperty("height_of_plants", NullValueHandling = NullValueHandling.Ignore)]
        public double? HeightOfPlants { get; set; }

        [JsonProperty("initial_volumetric_moisture_content_of_the_soil_layer", NullValueHandling = NullValueHandling.Ignore)]
        public double? InitialVolumetricMoistureContentOfTheSoilLayer { get; set; }

        [JsonProperty("leaf_area_index", NullValueHandling = NullValueHandling.Ignore)]
        public double? LeafAreaIndex { get; set; }

        [JsonProperty("leaf_emissivity", NullValueHandling = NullValueHandling.Ignore)]
        public double? LeafEmissivity { get; set; }

        [JsonProperty("leaf_reflectivity", NullValueHandling = NullValueHandling.Ignore)]
        public double? LeafReflectivity { get; set; }

        [JsonProperty("minimum_stomatal_resistance", NullValueHandling = NullValueHandling.Ignore)]
        public double? MinimumStomatalResistance { get; set; }

        [JsonProperty("moisture_diffusion_calculation_method", NullValueHandling = NullValueHandling.Ignore)]
        public MoistureDiffusionCalculationMethod? MoistureDiffusionCalculationMethod { get; set; } = Radiation.MoistureDiffusionCalculationMethod.Simple;

        [JsonProperty("residual_volumetric_moisture_content_of_the_soil_layer", NullValueHandling = NullValueHandling.Ignore)]
        public double? ResidualVolumetricMoistureContentOfTheSoilLayer { get; set; }

        [JsonProperty("roughness", NullValueHandling = NullValueHandling.Ignore)]
        public RoughnessOfCollectorEnum? Roughness { get; set; }

        [JsonProperty("saturation_volumetric_moisture_content_of_the_soil_layer", NullValueHandling = NullValueHandling.Ignore)]
        public double? SaturationVolumetricMoistureContentOfTheSoilLayer { get; set; }

        [JsonProperty("soil_layer_name", NullValueHandling = NullValueHandling.Ignore)]
        public string SoilLayerName { get; set; }

        [JsonProperty("solar_absorptance", NullValueHandling = NullValueHandling.Ignore)]
        public double? SolarAbsorptance { get; set; }

        [JsonProperty("specific_heat_of_dry_soil", NullValueHandling = NullValueHandling.Ignore)]
        public double? SpecificHeatOfDrySoil { get; set; }

        [JsonProperty("thermal_absorptance", NullValueHandling = NullValueHandling.Ignore)]
        public double? ThermalAbsorptance { get; set; }

        [JsonProperty("thickness", NullValueHandling = NullValueHandling.Ignore)]
        public double? Thickness { get; set; }

        [JsonProperty("visible_absorptance", NullValueHandling = NullValueHandling.Ignore)]
        public double? VisibleAbsorptance { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum MoistureDiffusionCalculationMethod { Advanced, Empty, Simple };

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

    [JsonConverter(typeof(StringEnumConverter))]
    public enum RoughnessOfCollectorEnum { MediumRough, MediumSmooth, Rough, Smooth, VeryRough, VerySmooth };
}