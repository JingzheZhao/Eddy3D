using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Rhino;


namespace EddyLib
{

    public class DaysimSettings
    {
        public DaysimSettings() { }


        public int START = 0;
        public int STOP = 8760;
        public int AB = 0;
        public int AD = 1024;
        public int AS = 512;
        public int AR = 256;
        public double AA = 0.2;

        public double MESHRES = 2;
        public double RADRES = 0;
        public double SENOFF = 0.5;

        public string PROJNAME = "default";
        public string PROJDIR = @"C:\UD\temp";
        //public string WEANAME = @"TUR_ISTANBUL.170600_IWEC";
        public string EPWPATH = @"C:\UD\LIB\TUR_ISTANBUL.170600_IWEC.EPW";

    }


    public class Daysim
    {
        public static void Epw2Wea(string weatherFilePath, string targetPath)
        {
            try
            {
                if (Directory.Exists(targetPath) == false) Directory.CreateDirectory(targetPath);

                if (Directory.GetFiles(targetPath, "*.wea").Length > 0)
                {
                    Array.ForEach(Directory.GetFiles(targetPath, "*.wea"), delegate (string path) { File.Delete(path); });
                }
                string epwdatname = Path.GetFileNameWithoutExtension(weatherFilePath);






                string arguments = "\"" + Path.GetFullPath(weatherFilePath) + "\" \"" +
                                   Path.GetFullPath(Path.Combine(targetPath, epwdatname + @".wea")) + "\"";


                Debug.WriteLine(arguments);






                ProcessStartInfo processInfo = new ProcessStartInfo();
                processInfo.Arguments = arguments;
                processInfo.FileName = @"C:\UD\bin\DAYSIM\bin_windows\epw2wea";
                processInfo.WorkingDirectory = @"C:\UD\bin\DAYSIM\bin_windows";
                processInfo.UseShellExecute = false;
                processInfo.RedirectStandardOutput = true;
                processInfo.RedirectStandardError = true;
                processInfo.CreateNoWindow = true;

                Process p = new Process();
                p.StartInfo = processInfo;
                // p.OutputDataReceived += DebugLog.CaptureOutput;
                // p.ErrorDataReceived += DebugLog.CaptureError;

                p.Start();
                p.WaitForExit();

                Debug.WriteLine("WEA FILE EXSISTS? " + File.Exists(Path.GetFullPath(Path.Combine(targetPath, epwdatname + @".wea"))).ToString());


            }

            catch
            {
                RhinoApp.WriteLine("SetWeather failed");
            }
        }

        public static void RunDaysim(string workingDir, string varNameBase, DaysimSettings setCon)
            {

                Regex re = new Regex(@"\@(\w+)\@", RegexOptions.Compiled);
                try
                {
                    string AB = setCon.AB.ToString();
                    string AD = setCon.AD.ToString();
                    string AS = setCon.AS.ToString();
                    string AR = setCon.AR.ToString();
                    string AA = setCon.AA.ToString();




                    // PARSING PARAMS AND WEATHER
                    //---------------------------
                    string wetterdatei = null;
                    string wetterkopf = null;
                    string[] weaPaths = null;

                    try
                    {
                        //ParamHandling.LoadParameterSettings();
                        weaPaths = Directory.GetFiles(workingDir, "*.wea", SearchOption.TopDirectoryOnly);

                        if (weaPaths.Length > 0 && weaPaths.Length < 2)
                        {
                            wetterdatei = Path.GetFileName(weaPaths[0]);
                            using (var sr = new StreamReader(weaPaths[0]))
                            {
                                for (int i = 0; i < 6; i++)
                                {
                                    string st = sr.ReadLine();
                                    wetterkopf += st + "\n";
                                }
                            }
                        }
                        else { Rhino.RhinoApp.WriteLine("Check your weather!"); }


                    }
                    catch
                    {
                        Rhino.RhinoApp.WriteLine("MULTIPLE OR NO WEATHER FILE FOUND");
                    }
                    // HEA GENERATION AND RUNNING
                    //---------------------------



                    //string HEAPATH = @"C:\UD\hea_grundlage.set";
                    //string HEACONTENT = File.ReadAllText(HEAPATH);

                    string HEACONTENT = HEAtemplate;


                    string varianten_name = (varNameBase);

                    string projekt_ordner = workingDir;
                    string wetterpfad = (workingDir + @"\" + wetterdatei);
                    //string UDI_lower_limit = "500";
                    //string UDI_upper_limit = "2000";
                    string zeitplan = "weekdays9to5withDST.60min.occ.csv";
                    string minimum_illuminance_level = "500";
                    string verschattung = "shading 1";
                    // FIXED ----------------------------------------------------------------------------------------------------
                    string material_datei = "materials.rad";
                    string geometrie_datei = "scene.rad";
                    string radiance_quelldateien = @"2, "+ workingDir + @"\materials.rad" + @", " + workingDir + @"\scene.rad";
                    string sensor_punkte = "sensors.pts";
                    string hea_dateiname = (workingDir + @"\input.hea");

                string viewpoint = ((varianten_name) + ".vf");

                string dgp_out_file = ((varianten_name) + "_dgp.out");
                string static_system = ((varianten_name) + ".dc " + (varianten_name) + ".ill");
                    string daylight_autonomy_active_RGB = ((varianten_name) + "_autonomy.DA");
                    string daylight_availability_active_RGB = ((varianten_name) + "_availability.DA");
                    string continuous_daylight_autonomy_active_RGB = ((varianten_name) + "_continuous_daylight_autonomy.CDA");
                    string electric_lighting = ((varianten_name) + "_el.htm");
                    string direct_sunlight_file = ((varianten_name) + ".dir");
                    string thermal_simulation = ((varianten_name) + "_intgain.csv");
                    string DDS_sensor_file = ((varianten_name) + ".dds");
                    string DDS_file = ((varianten_name) + ".sen");


                    var args = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
             {
             {"projekt_name" , varianten_name                                                            },
             {"projekt_ordner", projekt_ordner                                                           },
             {"tmp", workingDir                                                                          },
             {"wetterkopf", wetterkopf                                                                   },
             {"wetterdatei", wetterdatei                                                                 },
             {"wetterdateikurz", wetterdatei                                                             },
             {"material_datei", material_datei                                                           },
             {"geometrie_datei", geometrie_datei                                                         },
             {"sensor_punkte", sensor_punkte                                                             },
             {"radiance_quelldateien", radiance_quelldateien                                             },
             {"viewpoint", viewpoint                                                                     },
             {"dgp_out_file", dgp_out_file                                                               },
             {"static_system", static_system                                                             },
             {"verschattung", verschattung                                                               },
             {"sensor_unit", ("2")                                                                       },
              {"output_units", "1"                                                         },
             {"electric_lighting" , electric_lighting                                                    },
             {"direct_sunlight_file" , direct_sunlight_file                                              },
             {"thermal_simulation" , thermal_simulation                                                  },
             {"daylight_autonomy_active_RGB" , daylight_autonomy_active_RGB                              },
             {"daylight_availability_active_RGB" , daylight_availability_active_RGB                      },
             {"continuous_daylight_autonomy_active_RGB" , continuous_daylight_autonomy_active_RGB        },
             {"UDI_100_active_RGB" , "scene_UDI_100.DA"                                                  },
             {"UDI_100_2000_active_RGB" , "scene_UDI_100_2000.DA"                                        },
             {"UDI_2000_active_RGB" , "scene_UDI_2000.DA"                                                },
             {"DDS_sensor_file" , DDS_sensor_file                                                        },
             {"DDS_file" , DDS_file                                                                      },
             {"zeitplan" , zeitplan                                                                      },
             {"minimum_illuminance_level" , minimum_illuminance_level                                    },
             {"aa" , AA                                                        },
             {"ar" , AR                                                        },
             {"as" , AS                                                        },
             {"ad" , AD                                                        },
             {"ab" , AB                                                        },
             {"nutzungsplan" , "" },
             };


                    string output = "";
                    try
                    {

                        output = re.Replace(HEACONTENT, match => args[match.Groups[1].Value]);
                        //Console.Write(output);
                    }
                    catch (Exception e) { Rhino.RhinoApp.WriteLine(e.Message); }
                    try
                    {
                        System.IO.File.WriteAllText(workingDir + @"\" + varianten_name + @".hea", output);
                    }
                    catch (Exception e) { Rhino.RhinoApp.WriteLine("hea file error " + e.Message); }



                    Stopwatch oneSimTime = new Stopwatch();
                    oneSimTime.Start();

                    //RhinoApp.WriteLine("# Exterior Raytrace " + index.ToString());


                    string pathvar = System.Environment.GetEnvironmentVariable("PATH");
                    System.Environment.SetEnvironmentVariable("PATH", pathvar + @";C:\UD\bin\DAYSIM\bin_windows\;C:\UD\bin\Radiance\bin\;C:\UD\bin\DAYSIM;");
                    System.Environment.SetEnvironmentVariable("RAYPATH", @"C:\UD\bin\DAYSIM\lib\;C:\UD\bin\Radiance\lib\");



                    //run the daysim radiance executables
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.WorkingDirectory = "C:\\UD\\bin\\DAYSIM\\bin_windows";


                    string pathvar2 = startInfo.EnvironmentVariables["PATH"];
                    startInfo.EnvironmentVariables["PATH"] = pathvar2 + @";C:\UD\bin\DAYSIM\bin_windows\;C:\UD\bin\Radiance\bin\;C:\UD\bin\DAYSIM;";
                    startInfo.EnvironmentVariables["RAYPATH"] = @"C:\UD\bin\DAYSIM\lib\;C:\UD\bin\Radiance\lib\";


                    startInfo.UseShellExecute = false;
                    startInfo.RedirectStandardOutput = true;
                    startInfo.CreateNoWindow = true;
                    Process p;

                    try
                    {

                        startInfo.FileName = "radfiles2daysim";
                        startInfo.Arguments = workingDir + @"/" + varianten_name + @".hea -m -g";
                        p = Process.Start(startInfo);
                        p.WaitForExit();
                    }
                    catch { Rhino.RhinoApp.WriteLine("radfiles2daysim error"); }

                    try
                    {

                        startInfo.FileName = "gen_dc";
                        startInfo.Arguments = workingDir + @"/" + (varianten_name) + @".hea -dif -af test_dif.amb";
                        p = Process.Start(startInfo);
                        p.WaitForExit();
                    }
                    catch { Rhino.RhinoApp.WriteLine("gen_dc dif error"); }

                    try
                    {

                        startInfo.FileName = "gen_dc";
                        startInfo.Arguments = workingDir + @"/" + (varianten_name) + @".hea -dir -af test_dif.amb";
                        p = Process.Start(startInfo);
                        p.WaitForExit();
                    }
                    catch { Rhino.RhinoApp.WriteLine("gen_dc dir error"); }

                    try
                    {

                        startInfo.Arguments = workingDir + "/" + (varianten_name) + ".hea -paste";
                        p = Process.Start(startInfo);
                        p.WaitForExit();
                    }
                    catch { Rhino.RhinoApp.WriteLine("hea -paste error"); }

                    try
                    {

                        startInfo.FileName = "ds_illum";
                        startInfo.Arguments = workingDir + @"/" + (varianten_name) + @".hea";
                        p = Process.Start(startInfo);
                        p.WaitForExit();
                    }
                    catch { Rhino.RhinoApp.WriteLine("ds_illum error"); }

                    //try
                    //{

                    //    startInfo.FileName = "gen_directsunlight";
                    //    startInfo.Arguments = workingDir + @"/" + (varianten_name) + @".hea";
                    //    p = Process.Start(startInfo);
                    //    p.WaitForExit();
                    //}
                    //catch { Rhino.RhinoApp.WriteLine("gen_directsunlight error"); }

                    try
                    {

                        startInfo.FileName = "gen_dc";
                        startInfo.Arguments = workingDir + @"/" + varianten_name + @".hea -paste";
                        p = Process.Start(startInfo);
                        p.WaitForExit();
                    }
                    catch { Rhino.RhinoApp.WriteLine("gen_dc error"); }

                    oneSimTime.Stop();
                    int oneSimTook = Convert.ToInt32(oneSimTime.ElapsedMilliseconds);
    
                }

                catch
                {
                    Rhino.RhinoApp.WriteLine("runDAYSIM failed");
                }
            }

        private const string HEAtemplate = @"
# @projekt_name@.hea
# DAYSIM Input File generated by Eddy
# Timur Dogan, Patrick Kastner
# The file consist of keywords followed by variable assignment.
# You can add comment lines into the file that start with #.

project_name		@projekt_name@
project_directory	@projekt_ordner@\
bin_directory		C:\UD\bin\DAYSIM\bin_windows\
tmp_directory		@tmp@\
material_directory	C:\UD\bin\DAYSIM\materials\

##################
# site information
##################

@wetterkopf@
first_weekday 1
time_step 60
wea_data_short_file @wetterdateikurz@
wea_data_short_file_units 1
lower_direct_threshold 2
lower_diffuse_threshold 2
output_units @output_units@

######################
# building information
######################
material_file @material_datei@
geometry_file @geometrie_datei@
scene_rotation_angle 00
sensor_file @sensor_punkte@
radiance_source_files @radiance_quelldateien@
#viewpoint_file Untitled.vf
dgp_out_file @dgp_out_file@
@verschattung@
static_system @static_system@

# sensor_file_unit @sensor_unit@


######################
# RADIANCE parameters
######################

ab @ab@
ad @ad@
as @as@
ar @ar@
aa @aa@

lr 6
st 0.1500
sj 1.0000
lw 0.0040000
dj 0.0000
ds 0.200
dr 2
dp 512
# ms 0.57
# dt .05
# dc .75
# lr 12
# lw .002
# ps 2
# pt .05
# af test.amb

 
######################
# Analysis information
######################

#######################
# daylighting results 
#######################

daylight_autonomy_active_RGB @daylight_autonomy_active_RGB@
daylight_availability_active_RGB @daylight_availability_active_RGB@
continuous_daylight_autonomy_active_RGB @continuous_daylight_autonomy_active_RGB@
UDI_100_active_RGB @UDI_100_active_RGB@
UDI_100_2000_active_RGB @UDI_100_2000_active_RGB@
UDI_2000_active_RGB @UDI_2000_active_RGB@
electric_lighting @electric_lighting@
direct_sunlight_file @direct_sunlight_file@
thermal_simulation @thermal_simulation@
DDS_sensor_file @DDS_sensor_file@
DDS_file @DDS_file@

";


        






    }

}