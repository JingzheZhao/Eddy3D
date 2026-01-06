namespace EddyLib
{
    public class OFField
    {
        public string InterpolationScheme { get; set; }

        public string FieldName { get; set; }

        public string ProbeName { get; set; }

        public fieldType FieldType { get; set; }

        public OFField(string fieldName, string probeName, int InterpolationScheme)
        {
            Setup(fieldName, probeName, InterpolationScheme);
        }

        private void Setup(string fieldName, string probeName, int InterpolationScheme)
        {
            //param.AddNamedValue("U", 0);
            //param.AddNamedValue("total(p)_coeff", 1);
            //param.AddNamedValue("p", 2);
            //param.AddNamedValue("epsilon", 3);
            //param.AddNamedValue("omega", 4);
            //param.AddNamedValue("k", 5);
            //param.AddNamedValue("nut", 6);
            //param.AddNamedValue("phi", 7);

            this.InterpolationScheme = Probing.ReformatIS(InterpolationScheme);

            FieldName = fieldName;
            ProbeName = probeName;
            FieldType = ProbeFieldHelpers.GetFieldType(fieldName);
        }

        public static string ReformatOFFields(int OFFieldInt)
        {
            switch (OFFieldInt)
            {
                case 0:
                    return "U";
                case 1:
                    return "total(p)_coeff";
                case 2:
                    return "p";
                case 3:
                    return "epsilon";
                case 4:
                    return "omega";
                case 5:
                    return "k";
                case 6:
                    return "nut";
                case 7:
                    return "phi";
                case 8:
                    return "aoa";
                default:
                    return "covid19";
            }

            // Todo Zoe
        }
    }
}
