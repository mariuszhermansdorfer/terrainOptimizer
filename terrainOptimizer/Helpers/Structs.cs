using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace terrainOptimizer
{
    public class Structs
    {
        public class ControlPoints
        {
            public Anchors[] Anchors;
            public bool[] Ascending;
            public int Length;
            public bool[] Pegged;
            public Rhino.Geometry.Point3d[] Points;
            

            public ControlPoints(int length)
            {
                Anchors = new Anchors[length];
                Ascending = new bool[length];
                Length = length;
                Pegged = new bool[length];
                Points = new Rhino.Geometry.Point3d[length];
            }
        }
        public enum Anchors
        {
            Null,
            LowPoint,
            HighPoint
        }
    }
}
