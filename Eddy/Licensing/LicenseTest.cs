//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Windows.Forms;

//namespace Eddy
//{
//    public class Test
//    {
//        public static void WriteHardwareId()
//        {
//            string file = Utilities.hardcodedAssemblyDir + "/EddyHardwareId.txt";
//            if (!File.Exists(file))
//            {
//                var GenereateAKey = new Generate();
//                File.WriteAllText(file, GenereateAKey.MachineCode.ToString());

// } } private static string Obscure(string source, Int16 shift) { return source; }

// private static string _powo = "Eddy#D.Ver$ion.0.0.0.1";

// public static bool Validate() { string file = Utilities.AssemblyDirectory + "/Eddy.lic"; string
// key = ""; if (File.Exists(file)) { key = File.ReadAllText(file).Split('\n')[1]; } if
// (!string.IsNullOrWhiteSpace(key)) { Validate ValidateAKey = new Validate(); // create an object
// ValidateAKey.Parola = Obscure(_powo, 40); // the passsword ValidateAKey.Key = key; // enter a
// valid key

// //license without hardware lock if (ValidateAKey.Features[0] == true) return ValidateAKey.IsValid;
// //license with hardware lock else if (ValidateAKey.Features[1] == true) return
// ValidateAKey.IsValid && ValidateAKey.IsOnRightMachine; //license that expires else if
// (ValidateAKey.Features[2] == true) return ValidateAKey.IsValid && !ValidateAKey.IsExpired;
// //license with hardware lock that expires else if (ValidateAKey.Features[3] == true) return
// ValidateAKey.IsValid && ValidateAKey.IsOnRightMachine && !ValidateAKey.IsExpired;

// else { return false; } // check whether tif (ValidateAKey.Features[2] == true) return
// ValidateAKey.IsValid && ValidateAKey.IsOnRightMachine && !ValidateAKey.IsExpired;he key has been
// modified or not } else { return false; } }

// public static bool ValidateTrial() { try { ModifyRegistry mod = new ModifyRegistry(); string val =
// mod.Read("EDY_0_1"); if (string.IsNullOrEmpty(val)) { mod.Write("EDY_0_1",
// (object)System.DateTime.Now.ToString()); return true; } else { var installdate =
// System.DateTime.Parse(val); installdate = installdate.AddDays(30); if
// (System.DateTime.Compare(installdate, System.DateTime.Now) > 0) { return true; } else { return
// false; }

// }

// } catch (Exception ex) { MessageBox.Show("Validation failed: " + ex.Message); return false;

// } }

//    }
//}