//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace EddyLib
//{
//    public class ParsingResiduals
//    {
//        public List<List<double>> parseResiduals(string filePath)
//        {
//            List<List<double>> parsedResiduals = new List<List<double>>();

// string[] lines = File.ReadAllLines(filePath);

// var iter = new List<double>(); var Ux = new List<double>(); var Uy = new List<double>(); var Uz =
// new List<double>(); var p = new List<double>(); var omega = new List<double>(); var k = new
// List<double>(); var clocktime = new List<double>();

// foreach (string str in lines) { if (System.Text.RegularExpressions.Regex.IsMatch(str, @"^\d+")) {
// iter.Add(double.Parse(str.Split('\t')[0])); Ux.Add(double.Parse(str.Split('\t')[1]));
// Uy.Add(double.Parse(str.Split('\t')[2])); Uz.Add(double.Parse(str.Split('\t')[3]));
// p.Add(double.Parse(str.Split('\t')[4])); omega.Add(double.Parse(str.Split('\t')[5]));
// k.Add(double.Parse(str.Split('\t')[6])); } }

// parsedResiduals.Add(iter); parsedResiduals.Add(Ux); parsedResiduals.Add(Uy);
// parsedResiduals.Add(Uz); parsedResiduals.Add(p); parsedResiduals.Add(omega); parsedResiduals.Add(k);

//            return parsedResiduals;
//        }
//    }
//}