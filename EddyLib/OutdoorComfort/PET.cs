using MathNet.Numerics.RootFinding;
using System;

namespace EddyLib.OutdoorComfort

{
    public class PET
    {
        // https://github.com/eddes/AREP
        public class PET2017
        {
            public static int age = 35;

            public static double cair = 1.01 * 1000.0;

            public static double cb = 3.64 * 1000.0;

            public static double emcl = 0.95;

            public static double emsk = 0.99;

            public static double eps = 1E-6;

            public static double eta = 0.0;

            public static double HR = 50;

            public static double ht = 1.8;

            public static double icl = 0.5;

            public static double Lvap = 2.42E6;

            public static int M = 80;

            public static int mbody = 75;

            public static double p = 1013.25;

            public static double po = 1013.25;

            public static int pos = 1;

            public static double rob = 1.06;

            public static int sex = 1;

            public static double sigm = 5.67E-8;

            public static double[] T = new double[] { 38, 40, 40 };

            public static double Ta = 30;

            public static double tbody_set = (0.1 * tsk_set) + (0.9 * tc_set);

            public static double tc_set = 36.6;

            public static double Tmax = 60;

            public static double Tmin = -40;

            public static double Tmrt = 20;

            public static double tsk_set = 34;

            public static double[] Tstable = Resolution(Ta, Tmrt, HR, v, age, sex, ht, mbody, pos, M, icl, T).Item1;

            public static double v = 1;

            // PET calculation with dichotomy method
            public static object PET(
              int age,
              int sex,
              double ht,
              int mbody,
              int pos,
              int M,
              object icl,
              double[] Tstable,
              double eps)
            {
                // Definition of a function with the input variables of the PET reference situation

                // Bolt optimization: Precalculate Math.Pow constants
                var mbody_0_75 = Math.Pow(mbody, 0.75);
                var mbody_1_3 = Math.Pow(mbody, 1.0 / 3.0);
                var mbody_0_425 = Math.Pow(mbody, 0.425);
                var ht_0_725 = Math.Pow(ht, 0.725);
                var v_0_67 = Math.Pow(0.1, 0.67);
                var v_0_513 = Math.Pow(0.1, 0.513);
                var p_po_0_55 = Math.Pow(p / po, 0.55);

                Func<double, double[]> f = Tx =>
                {
                    return Syst(Tstable, Tx, Tx, 50, 0.1, age, sex, ht, mbody, pos, M, 0.9, false, mbody_0_75, mbody_1_3, mbody_0_425, ht_0_725, v_0_67, v_0_513, p_po_0_55);
                };
                var Ti = Tmin;
                var Tf = Tmax;
                var pet = 0.0;
                while (Tf - Ti > eps)
                {
                    // Dichotomy loop
                    if (f(Ti)[0] * f(pet)[0] < 0) // check this later
                    {
                        Tf = pet;
                    }
                    else
                    {
                        Ti = pet;
                    }
                    pet = (Ti + Tf) / 2.0;
                }
                return pet;
            }

            // Solving the 3 equation non-linear system
            // Bolt optimization: Replace Tuple with ValueTuple to eliminate heap allocations
            // inside this high-frequency root-finding objective function loop.
            public static (double[], double) Resolution(
              double Ta,
              double Tmrt,
              double HR,
              double v,
              int age,
              int sex,
              double ht,
              int mbody,
              int pos,
              int M,
              double icl,
              double[] Tx)
            {
                // Bolt optimization: Precalculate Math.Pow constants
                var mbody_0_75 = Math.Pow(mbody, 0.75);
                var mbody_1_3 = Math.Pow(mbody, 1.0 / 3.0);
                var mbody_0_425 = Math.Pow(mbody, 0.425);
                var ht_0_725 = Math.Pow(ht, 0.725);
                var v_0_67 = Math.Pow(v, 0.67);
                var v_0_513 = Math.Pow(v, 0.513);
                var p_po_0_55 = Math.Pow(p / po, 0.55);

                Func<double[], double[]> ff = Txx =>
                {
                    return Syst(Txx, Ta, Tmrt, HR, v, age, sex, ht, mbody, pos, M, icl, true, mbody_0_75, mbody_1_3, mbody_0_425, ht_0_725, v_0_67, v_0_513, p_po_0_55);
                };

                var firstGuess = new double[] { 0, 0, 0 };
                var Tn = Broyden.FindRoot(ff, firstGuess, 1.49012e-08, 1000, 0.000001);

                //double Tn = BrentsFun(Syst(Tx, Ta, Tmrt, HR, v, age, sex, ht, mbody, pos, M, icl, true, output), lower: -1.0, upper: 4, tol: 0.002, maxIter: 100);

                //   Tuple<double[],double> res =                    new Tuple<doubl, string, string>(1, "Steve", "Jobs");

                return (Tn, 1.0);
            }

            // Sweating calculation function
            public static double Suda(double tbody, double tsk)
            {
                var sig_body = tbody - tbody_set;
                var sig_skin = tsk - tsk_set;
                if (sig_body < 0)
                {
                    // In this case, Tbody<Tbody_set --> The sweat flow is 0
                    sig_body = 0.0;
                }
                if (sig_skin < 0)
                {
                    // In this case, Tsk<Tsk_set --> the sweat flow is reduced
                    sig_skin = 0.0;
                }

                // qmsw = 170 * sig_body * math.exp((sig_skin) / 10.7)  # [g/m2/h] is the expression from Gagge's model
                var qmsw = 304.94E-3 * sig_body;

                // 500 g/m^2/h is the upper sweat rate limit
                if (qmsw > 500)
                {
                    qmsw = 500;
                }
                return qmsw;
            }

            // Vectorial MEMI balance calculation function
            public static double[] Syst(
              double[] T,
              double Ta,
              double Tmrt,
              double HR,
              double v,
              int age,
              int sex,
              double ht,
              int mbody,
              int pos,
              int M,
              double icl,
              bool mode,
              double mbody_0_75,
              double mbody_1_3,
              double mbody_0_425,
              double ht_0_725,
              double v_0_67,
              double v_0_513,
              double p_po_0_55
              )
            {
                double fec;
                double metab;
                double vpa;

                // Area parameters of the body:
                var Adu = 0.203 * mbody_0_425 * ht_0_725;
                var feff = 0.725;
                if (pos == 1 || pos == 3)
                {
                    feff = 0.725;
                }
                if (pos == 2)
                {
                    feff = 0.696;
                }

                // Calculation of the Burton surface increase coefficient, k = 0.31 for Hoeppe:
                // Increase heat exchange surface depending on clothing level
                var fcl = 1 + 0.31 * icl;
                var facl = (173.51 * icl - 2.36 - 100.76 * icl * icl + 19.28 * (icl * icl * icl)) / 100;
                var Aclo = Adu * facl + Adu * (fcl - 1.0);
                var Aeffr = Adu * feff;

                // Partial pressure of water in the air depending on relative humidity and air temperature:
                if (mode)
                {
                    // mode=True is the calculation of the actual environment
                    vpa = HR / 100.0 * 6.105 * Math.Exp(17.27 * Ta / (237.7 + Ta));
                }
                else
                {
                    // mode=False means we are calculating the PET
                    vpa = 12;
                }

                // Convection coefficient depending on wind velocity and subject position
                var hc = 0.0;
                if (pos == 1)
                {
                    hc = 2.67 + 6.5 * v_0_67;
                }
                if (pos == 2)
                {
                    hc = 2.26 + 7.42 * v_0_67;
                }
                if (pos == 3)
                {
                    hc = 8.6 * v_0_513;

                    // modification of hc with the total pressure
                    hc = hc * p_po_0_55;
                }

                // Base metabolism for men and women in [W]
                var metab_female = 3.19 * mbody_0_75 * (1.0 + 0.004 * (30.0 - age) + 0.018 * (ht * 100.0 / mbody_1_3 - 42.1));
                var metab_male = 3.45 * mbody_0_75 * (1.0 + 0.004 * (30.0 - age) + 0.01 * (ht * 100.0 / mbody_1_3 - 43.4));

                // Source term : metabolic activity
                if (mode == true)
                {
                    // = actual environment
                    metab = (M + metab_male) / Adu;
                    fec = (M + metab_female) / Adu;
                }
                else
                {
                    // False=reference environment
                    metab = (80 + metab_male) / Adu;
                    fec = (80 + metab_female) / Adu;
                }
                var he = 0.0;

                // Attribution of internal energy depending on the sex of the subject
                if (sex == 1)
                {
                    he = metab;
                }
                else if (sex == 2)
                {
                    he = fec;
                }
                var h = he * (1.0 - eta);

                // Respiratory energy losses
                // Expired air temperature calculation:
                var texp = 0.47 * Ta + 21.0;

                // Pulmonary flow rate
                var dventpulm = he * 1.44E-6;

                // Sensible heat energy loss:
                var eres = cair * (Ta - texp) * dventpulm;

                // Latent heat energy loss:
                var vpexp = 6.11 * Math.Exp(2.302585092994046 * (7.45 * texp / (235.0 + texp)));
                var erel = 0.623 * Lvap / p * (vpa - vpexp) * dventpulm;
                var ere = eres + erel;

                // Clothed fraction of the body approximation
                var rcl = icl / 6.45;
                var y = 0.0;
                if (facl > 1.0)
                {
                    facl = 1.0;
                }
                if (icl >= 2.0)
                {
                    y = 1.0;
                }
                if (icl > 0.6 && icl < 2.0)
                {
                    y = (ht - 0.2) / ht;
                }
                if (icl <= 0.6 && icl > 0.3)
                {
                    y = 0.5;
                }
                if (icl <= 0.3 && icl > 0.0)
                {
                    y = 0.1;
                }

                // calculation of the closing radius depending on the clothing level (6.28 = 2* pi !)
                var r2 = Adu * (fcl - 1.0 + facl) / (6.28 * ht * y);
                var r1 = facl * Adu / (6.28 * ht * y);
                var di = r2 - r1;

                // Calculation of the equivalent thermal resistance of body tissues

                // Bolt: optimize redundant VasoC calculations by caching method call
                var vasoC_res = VasoC((double)T[0], (double)T[1]);
                var alpha = vasoC_res.Item2;
                var tbody = alpha * (double)T[1] + (1 - alpha) * (double)T[0];
                var htcl = 6.28 * ht * y * di / (rcl * Math.Log(r2 / r1) * Aclo);

                // Calculation of sweat losses
                var qmsw = Suda(tbody, (double)T[1]);

                // Lvap/1000 = 2400 000[J/kg] divided by 1000 = [J/g] // qwsw/3600 for [g/m2/h] to [g/m2/s]
                var esw = Lvap / 1000 * qmsw / 3600;

                // Saturation vapor pressure at temperature Tsk
                var Pvsk = 6.105 * Math.Exp((17.27 * ((double)T[1] + 273.15) - 4717.03) / (237.7 + (double)T[1]));

                // Calculation of vapour transfer
                var Lw = 1.67;
                var he_diff = hc * Lw;
                var fecl = 1 / (1 + 0.92 * hc * rcl);
                var emax = he_diff * fecl * (Pvsk - vpa);
                var w = esw / emax;
                if (w > 1)
                {
                    w = 1;
                    var delta = esw - emax;
                    if (delta < 0)
                    {
                        esw = emax;
                    }
                }
                if (esw < 0)
                {
                    esw = 0;
                }
                var i_m = 0.38;

                // clothing vapour transfer resistance after Woodcock's method
                var R_ecl = (1 / (fcl * hc) + rcl) / (Lw * i_m);

                // R_ecl=0.79*1e7 # Hoeppe's method for E_diff
                var ediff = (1 - w) * (Pvsk - vpa) / R_ecl;
                var evap = -ediff + esw;

                // Radiation losses
                // For bare skin area:
                double tmrtK = Tmrt + 273.15;
                double t1K = (double)T[1] + 273.15;
                double t2K = (double)T[2] + 273.15;

                double tmrtK2 = tmrtK * tmrtK;
                double tmrtK4 = tmrtK2 * tmrtK2;

                double t1K2 = t1K * t1K;
                double t1K4 = t1K2 * t1K2;

                double t2K2 = t2K * t2K;
                double t2K4 = t2K2 * t2K2;

                var rbare = Aeffr * (1.0 - facl) * emsk * sigm * (tmrtK4 - t1K4) / Adu;

                // For dressed area:
                var rclo = feff * Aclo * emcl * sigm * (tmrtK4 - t2K4) / Adu;
                var rsum = rclo + rbare;

                // Convection losses #
                var cbare = hc * (Ta - (double)T[1]) * Adu * (1.0 - facl) / Adu;
                var cclo = hc * (Ta - (double)T[2]) * Aclo / Adu;
                var csum = cclo + cbare;

                // Balance equations of the 3-nodes model

                double term1 = (vasoC_res.Item1 / 3600 * cb + 5.28);
                double t1t2 = htcl * (T[1] - T[2]);

                double enbal0 = h + ere - (term1) * (T[0] - T[1]); // Core balance [W/m^2]
                double enbal1 = rbare + cbare + evap + (term1) * (T[0] - T[1]) - t1t2; //# Skin balance [W/m^2]
                double enbal2 = cclo + rclo + t1t2; //# Clothes balance [W/m^2]
                var enbal_scal = h + ere + rsum + csum + evap;

                // returning either the calculated core,skin,clo temperatures or the PET

                if (mode)
                {
                    // if we solve for the system we need to return 3 temperatures
                    return new double[] { enbal0, enbal1, enbal2 };
                }
                else
                {
                    // solving for the PET requires the scalar balance only
                    return new double[] { enbal_scal };
                }
            }

            // Skin blood flow calculation function:
            // Bolt optimization: Replace Tuple with ValueTuple to eliminate heap allocations
            // inside this method which is called repeatedly during non-linear root finding.
            public static (double, double) VasoC(double tcore, double tsk)
            {
                // Set value signals
                var sig_skin = tsk_set - tsk;
                var sig_core = tcore - tc_set;
                if (sig_core < 0)
                {
                    // In this case, Tcore<Tc_set --> the blood flow is reduced
                    sig_core = 0.0;
                }
                if (sig_skin < 0)
                {
                    // In this case, Tsk>Tsk_set --> the blood flow is increased
                    sig_skin = 0.0;
                }

                // 6.3 L/m^2/h is the set value of the blood flow
                var qmblood = (6.3 + 75.0 * sig_core) / (1.0 + 0.5 * sig_skin);

                // 90 L/m^2/h is the blood flow upper limit
                if (qmblood > 90)
                {
                    qmblood = 90.0;
                }

                // in the transient model, alpha is used to update tbody
                //alpha = 0.04177 + 0.74518 / (qmblood + 0.585417)
                var alpha = 0.1;
                return (qmblood, alpha);
            }
        }
    }
}