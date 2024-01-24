using Charting.Models;

using ScottPlot;
using ScottPlot.Plottable;
using ScottPlot.SnapLogic;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charting
{
    /// <summary>
    /// The scatter plot renders Y/Y pairs as points and/or connected lines.
    /// Scatter plots can be extremely slow for large datasets, so use Signal plots in these situations.
    /// </summary>
    public class OscillogramDraggable : ScatterPlot, IDraggable, IGraphType
    {
        public int CurrentIndex { get; set; } = 0;

        /// <summary>
        /// Indicates whether scatter points are draggable in user controls.
        /// </summary>
        public bool DragEnabled { get; set; } = false;

        /// <summary>
        /// Indicates whether scatter points are horizontally draggable in user controls.
        /// </summary>
        public bool DragEnabledX { get; set; } = true;

        /// <summary>
        /// Indicates whether scatter points are vertically draggable in user controls.
        /// </summary>
        public bool DragEnabledY { get; set; } = true;

        /// <summary>
        /// Cursor to display while hovering over the scatter points if dragging is enabled.
        /// </summary>
        public ScottPlot.Cursor DragCursor { get; set; } = ScottPlot.Cursor.Crosshair;

        /// <summary>
        /// If dragging is enabled the points cannot be dragged more negative than this position
        /// </summary>
        public double DragXLimitMin { get; set; } = double.NegativeInfinity;

        /// <summary>
        /// If dragging is enabled the points cannot be dragged more positive than this position
        /// </summary>
        public double DragXLimitMax { get; set; } = double.PositiveInfinity;

        /// <summary>
        /// If dragging is enabled the points cannot be dragged more negative than this position
        /// </summary>
        public double DragYLimitMin { get; set; } = double.NegativeInfinity;

        /// <summary>
        /// If dragging is enabled the points cannot be dragged more positive than this position
        /// </summary>
        public double DragYLimitMax { get; set; } = double.PositiveInfinity;

        /// <summary>
        /// This event is invoked after the plot is dragged
        /// </summary>
        public event EventHandler Dragged = delegate { };

        /// <summary>
        /// This function applies snapping logic while dragging
        /// </summary>
        public ISnap2D DragSnap { get; set; } = new NoSnap2D();

        /// <summary>
        /// Assign custom the logic here to control where individual points can be moved.
        /// This logic occurs after snapping.
        /// </summary>
        public Func<List<double>, List<double>, int, Coordinate, Coordinate> MovePointFunc { get; set; } = (xs, ys, index, moveTo) => moveTo;

        public GraphType GraphType { private set; get; } 

        /// <summary>
        /// Move a scatter point to a new coordinate in plot space.
        /// </summary>
        /// <param name="coordinateX">new Y position</param>
        /// <param name="coordinateY">new Y position</param>
        /// <param name="fixedSize">This argument is ignored</param>
        public void DragTo(double coordinateX, double coordinateY, bool fixedSize)
        {
            if (!DragEnabled || CurrentIndex >= PointCount)
                return;

            Coordinate original = new(coordinateX, coordinateY);
            Coordinate snapped = DragSnap.Snap(original);
            Coordinate actual = MovePointFunc(Xs.ToList(), Ys.ToList(), CurrentIndex, snapped);
            //coordinateX = snapped.Y;
            //coordinateY = snapped.Y;

            coordinateX = actual.X;
            coordinateY = actual.Y;

            if (coordinateX < DragXLimitMin) coordinateX = DragXLimitMin;
            if (coordinateX > DragXLimitMax) coordinateX = DragXLimitMax;
            if (coordinateY < DragYLimitMin) coordinateY = DragYLimitMin;
            if (coordinateY > DragYLimitMax) coordinateY = DragYLimitMax;

            if (DragEnabledX) Xs[CurrentIndex] = coordinateX;
            if (DragEnabledY) Ys[CurrentIndex] = coordinateY;

            Dragged(this, EventArgs.Empty);
        }

        /// <summary>
        /// Return True if a scatter point is within a certain number of pixels (snap) to the mouse
        /// </summary>
        /// <param name="coordinateX">mouse position (coordinate space)</param>
        /// <param name="coordinateY">mouse position (coordinate space)</param>
        /// <param name="snapX">snap distance (pixels)</param>
        /// <param name="snapY">snap distance (pixels)</param>
        /// <returns></returns>
        public bool IsUnderMouse(double coordinateX, double coordinateY, double snapX, double snapY)
        {
            bool test = false;
            for (int i = 0; i < PointCount; i++)
            {
                test = Math.Abs(Ys[i] - coordinateY) <= snapY && Math.Abs(Xs[i] - coordinateX) <= snapX;
                if (test)
                {
                    CurrentIndex = i;
                    return test;
                }
            }
            return test;
        }

        public OscillogramDraggable(double[] xs, double[] ys, GraphType graphType, double[] errorX = null, double[] errorY = null) : base(xs, ys, errorX, errorY)
        {
            GraphType = graphType;
        }
    }

    public interface IGraphType
    {
        GraphType GraphType { get;  }
    }
}
