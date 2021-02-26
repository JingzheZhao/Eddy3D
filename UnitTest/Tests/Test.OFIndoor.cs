//using Rhino.Compute;

using EddyLib.Indoor;
using EddyLib.Indoor.Dicts;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using static EddyLib.Indoor.IndoorBC;

namespace RhinoPlugin.Tests.Xunit
{
    [Collection("Rhino Collection")]
    public class OFIndoor
    {

        private static Dictionary<string, dynamic> NestedDict2()
        {


            Dictionary<string, dynamic> Dict1 = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> Dict2 = new Dictionary<string, dynamic>();



            Dict1.Add("application", "extractFromSurface");


            Dict1.Add("functions", Dict2);


            Dict2.Add("#includeFunc", "volumetricHeatSources");
           

            return Dict1;
        }

        private static Dictionary<string, dynamic> NestedDict3()
        {


            Dictionary<string, dynamic> Dict1 = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> Dict2 = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> Dict3 = new Dictionary<string, dynamic>();

            Dict1.Add("application", "extractFromSurface");


            Dict1.Add("functions", Dict2);


            Dict2.Add("#includeFunc", "volumetricHeatSources");

            Dict2.Add("functions", Dict3);


            Dict3.Add("#includeFunc", "volumetricHeatSources");
            return Dict1;
        }



        //int endTime = 2000;
        //string dir = @"C:\TestingIndoor\";
        // int cellSize = 1;
        //var pointInsideDomain = new Point3d(1,1,1);

        //var point0 = new Rhino.Geometry.Point3d(0, 0, 0);
        //var point1 = new Rhino.Geometry.Point3d(20, 0, 0);
        //var point2 = new Rhino.Geometry.Point3d(0, 20, 0);
        //var point3 = new Rhino.Geometry.Point3d(20, 20, 0);
        //var point4 = new Rhino.Geometry.Point3d(0, 0, 40);
        //var point5 = new Rhino.Geometry.Point3d(20, 0, 40);
        //var point6 = new Rhino.Geometry.Point3d(0, 20, 40);
        //var point7 = new Rhino.Geometry.Point3d(20, 20, 40);
        //Rhino.Geometry.Box box1 = new Rhino.Geometry.Box(Rhino.Geometry.Plane.WorldXY, new List<Rhino.Geometry.Point3d>() { point0, point1, point2, point3, point4, point5, point6, point7 });
        //Rhino.Geometry.MeshingParameters mp = new Rhino.Geometry.MeshingParameters();
        //var m = Mesh.CreateFromBrep(box1.ToBrep(), mp);

        //Rhino.Geometry.Mesh mm = new Rhino.Geometry.Mesh();

        //var w = new Wall(mm, 2, 20);
        //var listWalls = new List<IndoorBC.Wall>() { w };





        //var inlet = new IndoorBC.Inlet(m[0], 20, 2, new Vector3d(1, 1, 1));
        //var inletList = new List<IndoorBC.Inlet>() { inlet };

        //var outlet = new IndoorBC.Outlet(m[0], 2);
        //var outletList = new List<IndoorBC.Outlet>() { outlet };

        //var FO = new FunctionObject();
        //var FOs = new List<FunctionObject>() { FO };


        //var dom = new IndoorDomain(endTime, dir, cellSize, pointInsideDomain, listWalls, inletList,outletList, FOs);





        [Fact]
        public void TestNestedDict2()
        {

       
            var d = CppMapSerializerDyn.Serialize(NestedDict2());

            string[] parts = {
   
         String.Join("\n", d.ToArray())
            };

           var FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");

            Assert.StartsWith("a", FullDictString);


        }

        [Fact]
        public void TestNestedDict3()
        {


            var d = CppMapSerializerDyn.Serialize(NestedDict3());

            string[] parts = {

         String.Join("\n", d.ToArray())
            };

            var FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");

            Assert.StartsWith("a", FullDictString);


        }
        [Fact]
        public void TestFvOptions()
        {

            var dict = EddyLib.Indoor.Dicts.FvSolutionDict.MakeDict_p_rgh();
        
            var d = CppMapSerializerDyn.Serialize(dict);

            string[] parts = {

         String.Join("\n", d.ToArray())
            };

            var FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");

            Assert.StartsWith("a", FullDictString);


        }

     
    }
}