using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;

namespace EddyLib.Radiation
{
    public static class RadianceMaterials
    {
        private static readonly CultureInfo radianceCulture = new CultureInfo("en-US");

        public const string DefaultFacade = "void plastic GenericOpaque_r40\n0\n0\n5 0.4 0.4 0.4 0 0\n";
        public const string DefaultGround = "void plastic GenericOpaque_r20\n0\n0\n5 0.2 0.2 0.2 0 0\n";
        public const string DefaultGrass = "void plastic GenericGrass_r9\n0\n0\n5 0.1035 0.0919 0.0353 0 0.4";
        public const string DefaultGlass = "void glass GenericGlazing_t65\n0\n0\n3 0.71 0.71 0.71\n";
        public const string DefaultTree = "void trans GenericTree_t19\n0\n0\n7 0.28 0.7 0.37 0 0 0.8 1\n";

        /// valid Radiance material types and modifiers
        public static readonly string[] Types = { "plastic", "metal", "trans", "plastic2", "metal2", "trans2", "glass" };

        // ⚡ Bolt: Cache Types array in a HashSet for O(1) lookups instead of O(N) LINQ/Array.IndexOf overhead
        private static readonly HashSet<string> TypesSet = new HashSet<string>(Types);

        public static string RadiancePlasticMaterial(string Name, Color Color, double Reflectance, double Specularilty = 0, double Roughness = 0)
        {
            const double LuminousEfficacyRed = 0.3;
            const double LuminousEfficacyGreen = 0.59;
            const double LuminousEfficacyBlue = 0.11;

            double Red = Color.R;
            double Green = Color.G;
            double Blue = Color.B;

            double w = Red * LuminousEfficacyRed + Green * LuminousEfficacyGreen + Blue * LuminousEfficacyBlue;

            return String.Format(radianceCulture, "void plastic {5} 0 0 5 {0} {1} {2} {3} {4}\n", (Red / w * Reflectance), (Green / w * Reflectance), (Blue / w * Reflectance), Specularilty, Roughness, Name);
        }

        public static string RadianceGlassMaterial(string Name, double Transmittance)
        {
            const double refractiveIndex = 1.52;
            double Transmisivity = (Math.Sqrt(0.8402528435 + 0.0072522239 * Transmittance * Transmittance) - 0.9166530661) / 0.0036261119 / Transmittance;
            return String.Format(radianceCulture, "void glass {4} 0 0 4 {0} {1} {2} {3}\n", Transmisivity, Transmisivity, Transmisivity, refractiveIndex, Name);
        }

        public static bool GetID(string description, out string id)
        {
            // assign initial value
            id = string.Empty;

            using (var sr = new StringReader(description))
            {
                // get description as list of arguments
                var args = new List<string>();
                while (sr.Peek() > -1)
                {
                    // get uncommented portion of line
                    string line = sr.ReadLine().Split('#')[0];
                    if (!string.IsNullOrWhiteSpace(line) && line.Length > 0)
                    {
                        // add args to list
                        args.AddRange(line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
                    }
                }

                // get material id from arg following last appearance of a material type
                // (if multiple materials in description, this will take the last one)
                for (int i = 1; i < args.Count - 1; i++)
                {
                    if (TypesSet.Contains(args[i])) id = args[i + 1];
                }
                if (id.Length > 0) return true;
            }
            return false;
        }

        public static string GetType(string description)
        {
            // assign initial value
            string type = string.Empty;

            using (var sr = new StringReader(description))
            {
                if (sr.Peek() < 0) return type;

                // read first line
                string firstline = sr.ReadLine().Split('#')[0];
                string[] word = firstline.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                // check for valid structure
                if ((word.Length > 2) && TypesSet.Contains(word[1]))
                {
                    type = word[1];
                    return type;
                }
            }
            return type;
        }

        public static bool IsValid(string description)
        {
            using (var sr = new StringReader(description))
            {
                // get description as list of arguments
                var args = new List<string>();
                while (sr.Peek() > -1)
                {
                    // get uncommented portion of line
                    string line = sr.ReadLine().Split('#')[0];
                    if (!string.IsNullOrWhiteSpace(line) && line.Length > 0)
                    {
                        // add args to list
                        args.AddRange(line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));
                    }
                }

                // check that second arg is valid type
                if (args.Count > 1 && !TypesSet.Contains(args[1]))
                {
                    Console.Error.WriteLine("Material type " + args[1] + " not recognized.");
                    return false;
                }

                // valid rad description must have at least nine args
                if (args[1] == "electrochromic")
                {
                    if (args.Count < 6)
                    {
                        Console.Error.WriteLine("Electrochromic system description must have at least 6 arguments.");
                        return false;
                    }
                }
                else if (args[1] == "antimatter")
                {
                    if (args.Count < 7)
                    {
                        Console.Error.WriteLine("Antimatter must have at least 7 arguments.");
                        return false;
                    }
                }
                else if (args.Count < 9)
                {
                    Console.Error.WriteLine("Radiance material description must have at least 9 arguments.");
                    return false;
                }

                // success
                return true;
            }
        }

        public static string ReadLine(string str, bool allowComment = false)
        {
            str = str.Trim();
            string[] strArr = str.Split(new char[] { '#' });
            if (str.Contains("void electrochromic") || allowComment)
            {
                if (strArr != null && strArr.Length > 0) return strArr[strArr.Length - 1].Trim();
                else return "";
            }
            else
            {
                if (strArr != null && strArr.Length > 0) return strArr[0].Trim();
                else return "";
            }
        }
    }
}