using ScottPlot.Plottable;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charting.Models
{
    internal class DraggableGraphContext
    {
        public DraggableGraphContext()
        {
            DraggableGraph = new();
            CurrentDraggableGraphType = GraphType.Null;
        }
        public bool HasDraggable
        {
            get => DraggableGraph.Any(_ => _.DragEnabled);
        }
        public GraphType CurrentDraggableGraphType { get; set; }
        public IDraggable CurrentDraggableGraph { get; set; }
        public List<IDraggable> DraggableGraph { get; set; }
    }
}
