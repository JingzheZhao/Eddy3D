namespace EddyLib
{
    public class KMpt
    {
        #region Properties

        public int Id { get; set; }

        [KMeansValue]
        public double X { get; set; }

        [KMeansValue]
        public double Y { get; set; }

        [KMeansValue]
        public double Z { get; set; }

        #endregion Properties

        #region Constructors

        public KMpt()
        {
            Id = -1;
            X = -1;
            Y = -1;
            Z = -1;
        }

        public KMpt(double _x, double _y, double _z)
        {
            this.Id = -1;
            this.X = _x;
            this.Y = _y;
            this.Z = _z;
        }

        public KMpt(int _id, double _x, double _y, double _z)
        {
            this.Id = _id;
            this.X = _x;
            this.Y = _y;
            this.Z = _z;
        }

        #endregion Constructors
    }
}
