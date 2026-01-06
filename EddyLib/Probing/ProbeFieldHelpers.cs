namespace EddyLib
{
    internal static class ProbeFieldHelpers
    {
        internal static fieldType GetFieldType(field field)
        {
            return field == field.U ? fieldType.vector : fieldType.scalar;
        }

        internal static fieldType GetFieldType(string fieldName)
        {
            return fieldName == "U" ? fieldType.vector : fieldType.scalar;
        }

        internal static string GetFieldName(field field)
        {
            return field == field.cp_coeff ? "total(p)_coeff" : field.ToString();
        }
    }
}
