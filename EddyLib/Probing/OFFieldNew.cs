namespace EddyLib
{
    public class OFFieldNew
    {
        public string InterpolationScheme { get; set; }

        public string FieldName { get; set; }

        public string ProbeName { get; set; }

        public field Field { get; set; }

        public fieldType FieldType { get; set; }

        public OFFieldNew(string probeName, field field, int interpolationScheme)
        {
            Field = field;
            FieldType = ProbeFieldHelpers.GetFieldType(field);

            InterpolationScheme = Probing.ReformatIS(interpolationScheme);

            // User given name
            ProbeName = probeName;

            // OF internal name

            FieldName = ProbeFieldHelpers.GetFieldName(field);
        }
    }
}
