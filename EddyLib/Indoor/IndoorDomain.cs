using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Indoor
{
    internal class IndoorDomain
    {
        private BoundingBox BoundingBox;

        private List<IndoorBC.Wall> Geometry; //Surfaces or Volumes. IE Walls, table, whatever

        private List<IndoorBC.Inlet> Inlets; //Surfaces

        private List<IndoorBC.Outlet> Outlets;//Surfaces

        private List<IndoorBC.Emitter> Emitters; //Volumes

        private Point3d[] Edges;

        private double CellSize;

        public IndoorDomain(string workingDir, List<IndoorBC.Wall> RoomGeometry, List<IndoorBC.Inlet> Inlets, List<IndoorBC.Outlet> Outlets)
        {

            // Give unique index to every object

            int cnt = 0;

            for (int i = 0; i < RoomGeometry.Count; i++) {
                RoomGeometry[i].Id = RoomGeometry[i].Name + cnt;
                cnt++;
            }
            for (int i = 0; i < Inlets.Count; i++)
            {
                Inlets[i].Id = Inlets[i].Name + cnt;
                cnt++;
            }
            for (int i = 0; i < Outlets.Count; i++)
            {
                Outlets[i].Id = Outlets[i].Name + cnt;
                cnt++;
            }



            // Walls

            this.BoundingBox = GetBoundingBox(RoomGeometry);
            this.Edges = this.BoundingBox.GetCorners();

            // Inlets

            this.Inlets = Inlets;

            // Outlets

            this.Outlets = Outlets;

            // Dicts

            List<IndoorBCDict> allDicts = new List<IndoorBCDict>();

            IndoorBCDict.U u = new IndoorBCDict.U(Inlets, Outlets, RoomGeometry);
            //Dicts.U.ToFile(...)


            IndoorBCDict.alphat alphat = new IndoorBCDict.alphat(Inlets, Outlets, RoomGeometry);
            IndoorBCDict.AoA AoA = new IndoorBCDict.AoA(Inlets, Outlets, RoomGeometry);
            IndoorBCDict.nut nut = new IndoorBCDict.nut(Inlets, Outlets, RoomGeometry);
            IndoorBCDict.omega omega = new IndoorBCDict.omega(Inlets, Outlets, RoomGeometry);
            IndoorBCDict.p p = new IndoorBCDict.p(Inlets, Outlets, RoomGeometry);
            IndoorBCDict.p_rgh p_rgh = new IndoorBCDict.p_rgh(Inlets, Outlets, RoomGeometry);
            IndoorBCDict.T T = new IndoorBCDict.T(Inlets, Outlets, RoomGeometry);

            allDicts.Add(u);
            allDicts.Add(alphat);
            allDicts.Add(AoA);
            allDicts.Add(nut);
            allDicts.Add(omega);
            allDicts.Add(p);
            allDicts.Add(p_rgh);
            allDicts.Add(T);

            foreach (IndoorBCDict d in allDicts)
            {
                d.Export(workingDir);
            }
        }

        private BoundingBox GetBoundingBox(List<IndoorBC.Wall> RoomGeometry)
        {
            BoundingBox bb = new BoundingBox();
            Mesh RG = new Mesh();

            foreach (IndoorBC m in RoomGeometry)
            {
                if (m != null)
                {
                    RG.Append(m.Geometry);
                }
            }

            bb = RG.GetBoundingBox(false);

            return bb;
        }
    }
}