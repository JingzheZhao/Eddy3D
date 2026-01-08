namespace EddyLib
{
    /// <summary>
    /// Represents an OpenFOAM field for probing (new version using enum).
    /// </summary>
    public class OFFieldNew
    {
        /// <summary>
        /// Interpolation scheme for the field.
        /// </summary>
        public string InterpolationScheme { get; set; }

        /// <summary>
        /// Name of the field to probe.
        /// </summary>
        public string FieldName { get; set; }

        /// <summary>
        /// User-given probe name.
        /// </summary>
        public string ProbeName { get; set; }

        /// <summary>
        /// Field enum value.
        /// </summary>
        public field Field { get; set; }

        /// <summary>
        /// Type of the field (vector or scalar).
        /// </summary>
        public fieldType FieldType { get; set; }

        /// <summary>
        /// Creates an OpenFOAM field for probing using field enum.
        /// </summary>
        public OFFieldNew(string probeName, field field, int interpolationScheme)
        {
            Field = field;
            FieldType = ProbeFieldHelpers.GetFieldType(field);
            InterpolationScheme = Probing.ReformatIS(interpolationScheme);
            ProbeName = probeName;
            FieldName = ProbeFieldHelpers.GetFieldName(field);
        }
    }
}
