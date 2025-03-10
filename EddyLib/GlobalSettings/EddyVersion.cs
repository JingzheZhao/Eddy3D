namespace EddyLib
{
    public static class EddyVersion
    {
        public const string ProductVersion = "0.4.15.2";
        public const string Name = "Eddy3D";

        public static string toString()
        {
            return @"
" + Name + " " + ProductVersion;
        }
    }
}