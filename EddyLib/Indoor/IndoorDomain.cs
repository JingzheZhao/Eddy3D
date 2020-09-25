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

        private List<IndoorBCs.Wall> Geometry; //Surfaces or Volumes. IE Walls, table, whatever

        private List<IndoorBCs.Inlet> Inlets; //Surfaces

        private List<IndoorBCs.Outlet> Outlets;//Surfaces

        private List<IndoorBCs.Emitter> Emitters; //Volumes

        private Point3d[] Edges;

        private double CellSize;

        public IndoorDomain(string workingDir, List<IndoorBCs.Wall> RoomGeometry, List<IndoorBCs.Inlet> Inlets, List<IndoorBCs.Outlet> Outlets)
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

            List<Dicts> allDicts = new List<Dicts>();

            Dicts.U u = new Dicts.U(Inlets, Outlets, RoomGeometry);
            //Dicts.U.ToFile(...)


            Dicts.alphat alphat = new Dicts.alphat(Inlets, Outlets, RoomGeometry);
            Dicts.AoA AoA = new Dicts.AoA(Inlets, Outlets, RoomGeometry);
            Dicts.nut nut = new Dicts.nut(Inlets, Outlets, RoomGeometry);
            Dicts.omega omega = new Dicts.omega(Inlets, Outlets, RoomGeometry);
            Dicts.p p = new Dicts.p(Inlets, Outlets, RoomGeometry);
            Dicts.p_rgh p_rgh = new Dicts.p_rgh(Inlets, Outlets, RoomGeometry);
            Dicts.T T = new Dicts.T(Inlets, Outlets, RoomGeometry);

            allDicts.Add(u);
            allDicts.Add(alphat);
            allDicts.Add(AoA);
            allDicts.Add(nut);
            allDicts.Add(omega);
            allDicts.Add(p);
            allDicts.Add(p_rgh);
            allDicts.Add(T);

            foreach (Dicts d in allDicts)
            {
                d.Export(workingDir);
            }
        }

        private BoundingBox GetBoundingBox(List<IndoorBCs.Wall> RoomGeometry)
        {
            BoundingBox bb = new BoundingBox();
            Mesh RG = new Mesh();

            foreach (IndoorBCs m in RoomGeometry)
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