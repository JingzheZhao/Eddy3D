namespace EddyLib
{
    public static class EddyVersion
    {
        public const string ProductVersion = "0.7.0.827";
        public const string Name = "Eddy3D";

        public static string toString()
        {
            return @"
" + Name + " " + ProductVersion;
        }
    }
}
