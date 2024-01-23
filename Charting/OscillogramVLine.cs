using Charting.Models;

using ScottPlot.Plottable;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charting
{
    public class OscillogramVLine : OscillogramAxisLine, IGraphType
    {

        /// <summary>
        /// X position to render the line
        /// </summary>
        public double X { get => Position; set => Position = value; }
        public override string ToString() => $"Vertical line at X={X}";
 

        public GraphType GraphType { private set; get; }


  
        public OscillogramVLine(GraphType graphType) : base(false)
        {
            GraphType = graphType;
        }

        public OscillogramVLine() : base(false)
        {
            GraphType = GraphType.Null;
        }
    }
}
