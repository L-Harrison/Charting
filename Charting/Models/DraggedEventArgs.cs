using ScottPlot;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charting.Models
{
    public class DraggedEventArgs : System.EventArgs
    {
        public CoordinateRect CoordinateRect { get; set; }
        public double CoordinateX { get; set; }
        public double CoordinateY { get; set; }
    }
}
