namespace EddyLib
{
    /// <summary>
    /// Represents an OpenFOAM field for probing.
    /// </summary>
    public class OFField
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
        /// Name of the probe.
        /// </summary>
        public string ProbeName { get; set; }

        /// <summary>
        /// Type of the field (vector or scalar).
        /// </summary>
        public fieldType FieldType { get; set; }

        /// <summary>
        /// Creates an OpenFOAM field for probing.
        /// </summary>
        public OFField(string fieldName, string probeName, int InterpolationScheme)
        {
            Setup(fieldName, probeName, InterpolationScheme);
        }

        private void Setup(string fieldName, string probeName, int InterpolationScheme)
        {
            this.InterpolationScheme = Probing.ReformatIS(InterpolationScheme);
            FieldName = fieldName;
            ProbeName = probeName;
            FieldType = ProbeFieldHelpers.GetFieldType(fieldName);
        }

        /// <summary>
        /// Converts field index to field name.
        /// </summary>
        public static string ReformatOFFields(int OFFieldInt)
        {
            switch (OFFieldInt)
            {
                case 0: return "U";
                case 1: return "total(p)_coeff";
                case 2: return "p";
                case 3: return "epsilon";
                case 4: return "omega";
                case 5: return "k";
                case 6: return "nut";
                case 7: return "phi";
                case 8: return "aoa";
                default: return "covid19";
            }
        }
    }
}
