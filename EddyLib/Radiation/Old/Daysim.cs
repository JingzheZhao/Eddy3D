using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace EddyLib
{
    public class DaysimSettings
    {
        public DaysimSettings()
        {
        }

        public int AB = 0;

        public int AD = 1024;

        public int AS = 512;

        public int AR = 256;

        public double AA = 0.2;

        public string ProjectName = "CallRay";

        public string WorkDir = @"C:\temp";

        public string Weather = "";
    }

    public class Daysim
    {
        public double[][] dirIll;

        public double[][] difIll;

        public Daysim(string baseWorkingDir, Mesh BuildingGeometry, List<Point3d> probes, Weather weather)
        {
            //export RAD for DAYSIM
            if (!Directory.Exists(baseWorkingDir + @"Rad\"))
            {
                Directory.CreateDirectory(baseWorkingDir + @"Rad\");
            }

            string radMat = @"
void plastic Generic_20
0
0
5 0.2 0.2 0.2 0 0
";
            Mesh daysimMesh = new Mesh();
            daysimMesh.Append(BuildingGeometry);

            // Todo: add ground plane to the above mesh

            File.WriteAllText(baseWorkingDir + @"Rad\materials.rad", radMat);
            RadianceFiles.MeshProc(daysimMesh, baseWorkingDir + @"Rad\scene.rad", "Generic_20");

            // Write Probes

            RadianceFiles.writePTS(baseWorkingDir + @"\Rad\sensors.pts", probes);

            var difillFile = baseWorkingDir + @"\Rad\CallRay.dif.ill";
            var dirillFile = baseWorkingDir + @"\Rad\CallRay.dir.ill";

            RadianceFiles.Epw2Wea(weather.epwFilePath, baseWorkingDir + @"\Rad");

            DaysimSettings set = new DaysimSettings
            {
                AB = 1,
                WorkDir = baseWorkingDir + @"\Rad"
            };
            Daysim.RunDaysim(set);

            this.difIll = RadianceFiles.loadILL(difillFile);
            this.dirIll = RadianceFiles.loadILL(dirillFile);
        }

        public static string DaysimInstallation = @"C:\DIVA\DaysimBinaries";

        public static void RunDaysim(DaysimSettings setCon)
        {
            Regex re = new Regex(@"\@(\w+)\@", RegexOptions.Compiled);
            try
            {
                string workingDir = setCon.WorkDir;

                string varNameBase = setCon.ProjectName;

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
                    else { Debug.WriteLine("Check your weather!"); }
                }
                catch
                {
                    Debug.WriteLine("MULTIPLE OR NO WEATHER FILE FOUND");
                }

                // HEA GENERATION AND RUNNING
                //---------------------------

                string HEACONTENT = HEAtemplate;
                string varianten_name = (varNameBase);

                string projekt_ordner = workingDir;
                string wetterpfad = (workingDir + @"\" + wetterdatei);
                string material_datei = "materials.rad";
                string geometrie_datei = "scene.rad";
                string radiance_quelldateien = @"2, " + workingDir + @"\materials.rad" + @", " + workingDir + @"\scene.rad";
                string sensor_punkte = "sensors.pts";

                //string hea_dateiname = (workingDir + @"\input.hea");

                string static_system_DIR = (varianten_name) + " " + ((varianten_name) + ".dc " + (varianten_name) + ".dir.ill");
                string static_system_DIF = (varianten_name) + " " + ((varianten_name) + ".dc " + (varianten_name) + ".dif.ill");

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
             {"static_system", static_system_DIR                                                             },
             {"sensor_unit", ("2")                                                                       },
             {"output_units", "1"                                                         },
             {"aa" , AA                                                        },
             {"ar" , AR                                                        },
             {"as" , AS                                                        },
             {"ad" , AD                                                        },
             {"ab" , AB                                                        },
             };

                var argsDIF = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
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
             {"static_system", static_system_DIF                                                            },
             {"sensor_unit", ("2")                                                                       },
             {"output_units", "1"                                                         },
             {"aa" , AA                                                        },
             {"ar" , AR                                                        },
             {"as" , AS                                                        },
             {"ad" , AD                                                        },
             {"ab" , AB                                                        },
             };

                try
                {
                    string output = re.Replace(HEACONTENT, match => args[match.Groups[1].Value]);
                    string output_DIF = re.Replace(HEACONTENT, match => argsDIF[match.Groups[1].Value]);

                    System.IO.File.WriteAllText(workingDir + @"\" + varianten_name + @".hea", output);
                    System.IO.File.WriteAllText(workingDir + @"\" + varianten_name + @".dif.hea", output_DIF);
                }
                catch (Exception e) { Console.WriteLine("hea file error " + e.Message + "  " + workingDir + @"\" + varianten_name + @".hea"); }

                Stopwatch oneSimTime = new Stopwatch();
                oneSimTime.Start();

                //RhinoApp.WriteLine("# Exterior Raytrace " + index.ToString());

                string pathvar = System.Environment.GetEnvironmentVariable("PATH");
                System.Environment.SetEnvironmentVariable("PATH", pathvar + @";" + DaysimInstallation);

                //run the daysim radiance executables
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    WorkingDirectory = DaysimInstallation
                };

                string pathvar2 = startInfo.EnvironmentVariables["PATH"];
                startInfo.EnvironmentVariables["PATH"] = pathvar2 + @";" + DaysimInstallation; 


                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardError = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.CreateNoWindow = true;
                Process p;

                try
                {
                    startInfo.FileName = "radfiles2daysim";
                    startInfo.Arguments = workingDir + @"/" + varianten_name + @".hea -m -g";
                    p = Process.Start(startInfo);

                    p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                    Console.WriteLine("output>>" + e.Data); p.BeginOutputReadLine();

                    p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                    Console.WriteLine("error>>" + e.Data); p.BeginErrorReadLine();

                    p.WaitForExit();

                    Console.WriteLine("ExitCode: {0}", p.ExitCode); p.Close();
                }
                catch (Exception e) { Console.WriteLine("radfiles2daysim error" + e.Message); }

                //try
                //{
                //    startInfo.FileName = "oconv";
                //    startInfo.Arguments = workingDir + @"/" + varianten_name + @"scene.rad > scene.oct";
                //    p = Process.Start(startInfo);

                //    p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                //    Console.WriteLine("output>>" + e.Data); p.BeginOutputReadLine();

                //    p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                //    Console.WriteLine("error>>" + e.Data); p.BeginErrorReadLine();

                //    p.WaitForExit();

                //    Console.WriteLine("ExitCode: {0}", p.ExitCode); p.Close();
                //}
                //catch (Exception e) { Console.WriteLine("octree error" + e.Message); }

                try
                {
                    startInfo.FileName = "gen_dc";
                    startInfo.Arguments = workingDir + @"\" + (varianten_name) + @".hea -dif -af test_dif.amb";
                    p = Process.Start(startInfo);
                    p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                           Console.WriteLine("output>>" + e.Data);
                    p.BeginOutputReadLine();

                    p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                        Console.WriteLine("error>>" + e.Data);
                    p.BeginErrorReadLine();

                    p.WaitForExit();

                    Console.WriteLine("ExitCode: {0}", p.ExitCode);
                    p.Close();
                }
                catch (Exception e) { Console.WriteLine("gen_dc dif error" + e.Message); }

                try
                {
                    startInfo.FileName = "gen_dc";
                    startInfo.Arguments = workingDir + @"\" + (varianten_name) + @".hea -dir -af test_dif.amb";
                    p = Process.Start(startInfo);
                    p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                           Console.WriteLine("output>>" + e.Data);
                    p.BeginOutputReadLine();

                    p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                        Console.WriteLine("error>>" + e.Data);
                    p.BeginErrorReadLine();

                    p.WaitForExit();

                    Console.WriteLine("ExitCode: {0}", p.ExitCode);
                    p.Close();
                }
                catch (Exception e) { Console.WriteLine("gen_dc dir error" + e.Message); }

                //load dir and dif coefficients
                var dirDC = EddyLib.RadianceFiles.loadDC(workingDir + @"\" + (varianten_name) + @".dir.dc");
                var difDC = EddyLib.RadianceFiles.loadDC(workingDir + @"\" + (varianten_name) + @".dif.dc");

                //try
                //{
                //    startInfo.FileName = "gen_dc";
                //    startInfo.Arguments = workingDir + @"\" + (varianten_name) + ".hea -paste";
                //    p = Process.Start(startInfo);
                //    p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                //           Console.WriteLine("output>>" + e.Data);
                //    p.BeginOutputReadLine();

                // p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                // Console.WriteLine("error>>" + e.Data); p.BeginErrorReadLine();

                // p.WaitForExit();

                //    Console.WriteLine("ExitCode: {0}", p.ExitCode);
                //    p.Close();
                //}
                //catch (Exception e) { Console.WriteLine("hea -paste error" + e.Message); }

                //DIRECT Rad
                RadianceFiles.writeDC_DIR(workingDir + @"\" + (varianten_name) + @".dc", difDC, dirDC);
                try
                {
                    startInfo.FileName = "ds_illum";
                    startInfo.Arguments = workingDir + @"/" + (varianten_name) + @".hea";
                    p = Process.Start(startInfo);
                    p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                           Console.WriteLine("output>>" + e.Data);
                    p.BeginOutputReadLine();

                    p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                        Console.WriteLine("error>>" + e.Data);
                    p.BeginErrorReadLine();

                    p.WaitForExit();

                    Console.WriteLine("ExitCode: {0}", p.ExitCode);
                    p.Close();
                }
                catch (Exception e) { Console.WriteLine("ds_illum error" + e.Message); }

                //DIFFUSE Rad
                RadianceFiles.writeDC_DIF(workingDir + @"\" + (varianten_name) + @".dc", difDC, dirDC);
                try
                {
                    startInfo.FileName = "ds_illum";
                    startInfo.Arguments = workingDir + @"/" + (varianten_name) + @".dif.hea";
                    p = Process.Start(startInfo);
                    p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                           Console.WriteLine("output>>" + e.Data);
                    p.BeginOutputReadLine();

                    p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                        Console.WriteLine("error>>" + e.Data);
                    p.BeginErrorReadLine();

                    p.WaitForExit();

                    Console.WriteLine("ExitCode: {0}", p.ExitCode);
                    p.Close();
                }
                catch (Exception e) { Console.WriteLine("ds_illum error" + e.Message); }

                //try
                //{
                //    startInfo.FileName = "gen_directsunlight";
                //    startInfo.Arguments = workingDir + @"/" + (varianten_name) + @".hea";
                //    p = Process.Start(startInfo);
                //    p.WaitForExit();
                //}
                //catch { Console.WriteLine("gen_directsunlight error"); }

                //try
                //{
                //    startInfo.FileName = "gen_dc";
                //    startInfo.Arguments = workingDir + @"/" + varianten_name + @".hea -paste";
                //    p = Process.Start(startInfo);
                //    p.OutputDataReceived += (object sender, DataReceivedEventArgs e) =>
                //           Console.WriteLine("output>>" + e.Data);
                //    p.BeginOutputReadLine();

                // p.ErrorDataReceived += (object sender, DataReceivedEventArgs e) =>
                // Console.WriteLine("error>>" + e.Data); p.BeginErrorReadLine();

                // p.WaitForExit();

                //    Console.WriteLine("ExitCode: {0}", p.ExitCode);
                //    p.Close();
                //}
                //catch (Exception e) { Console.WriteLine("gen_dc error" + e.Message); }

                oneSimTime.Stop();
                int oneSimTook = Convert.ToInt32(oneSimTime.ElapsedMilliseconds);
            }
            catch (Exception e)
            {
                Console.WriteLine("runDAYSIM failed" + e.Message);
            }
        }

        private const string HEAtemplate = @"

# DAYSIM Input File generated by Eddy
# Timur Dogan, Patrick Kastner

project_name		@projekt_name@
project_directory	@projekt_ordner@\
bin_directory		C:\DIVA\DaysimBinaries\
tmp_directory		@tmp@\

@wetterkopf@
first_weekday 1
time_step 60
wea_data_short_file @wetterdateikurz@
wea_data_short_file_units 1
lower_direct_threshold 2
lower_diffuse_threshold 2
output_units @output_units@

material_file @material_datei@
geometry_file @geometrie_datei@
scene_rotation_angle 00
sensor_file @sensor_punkte@
radiance_source_files @radiance_quelldateien@

shading 1 @static_system@
# sensor_file_unit @sensor_unit@

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
# af test.amb";
    }
}