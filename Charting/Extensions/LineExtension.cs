using ScottPlot.Plottable;
using ScottPlot;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Charting.Models;

namespace Charting.Extensions
{
    public static class LineExtension
    {
        /// <summary>
        /// Add a vertical axis line at a specific X position
        /// </summary>
        public static OscillogramVLine AddOscillogramVerticalLine(this Plot plot, GraphType graphType, double x, Color? color = null, float width = 1, LineStyle style = LineStyle.Solid, string label = null)
        {
            OscillogramVLine plottable = new OscillogramVLine(graphType)
            {
                X = x,
                Color = color ?? plot.GetSettings().GetNextColor(),
                LineWidth = width,
                LineStyle = style,
                Label = label
            };
            plot.Add(plottable);
            return plottable;
        }
    }
}
