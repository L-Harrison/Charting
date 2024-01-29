using ScottPlot.Plottable;
using ScottPlot;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charting.Extensions
{
    public static class CellExtension
    {
        public static (int index, string cellName, int currentRow, int currentCol) ToIndexAndName(this int coloums, int currentRow, int currentCol)
        {
            var rowFlag = ASCIIEncoding.UTF8.GetString(new byte[] { (byte)(currentRow + 64) });
            var index = coloums * (currentRow - 1) + currentCol;
            var name = $"{rowFlag}{currentCol}";
            return (index, name, currentRow, currentCol);
        }
        /// <summary>
        /// Add an ellipse to the plot
        /// </summary>
        public static Cell AddCell(this Plot plot, string label, double x, double y, double xRadius, double yRadius, float size = 12, System.Drawing.Color? fontColor = null, System.Drawing.Color? color = null, System.Drawing.Color? borderColor = null, float lineWidth = 2, LineStyle lineStyle = LineStyle.Solid)
        {
            Cell plottable = new(x, y, xRadius, yRadius, color ?? plot.GetNextColor(), borderColor: borderColor ?? plot.GetNextColor())
            {
                BorderLineWidth = lineWidth,
                BorderLineStyle = lineStyle,
                Label = label,
                FontSize = size,
                FontColor = fontColor ?? plot.GetNextColor(),
            };
            plot.Add(plottable);
            return plottable;
        }

        /// <summary>
        /// Return the highest hittable plottable at the given point (or null if no hit)
        /// </summary>
        public static IPlottable GetCelltable(this Plot plot, double xPixel, double yPixel)
        {
            foreach (var plottable in plot.GetPlottables().Where(x => x is ICell).Reverse())
            {
                int xAxisIndex = plottable.XAxisIndex;
                int yAxisIndex = plottable.YAxisIndex;

                double xCoords = plot.GetCoordinateX((float)xPixel, xAxisIndex);
                double yCoords = plot.GetCoordinateY((float)yPixel, yAxisIndex);
                Coordinate c = new(xCoords, yCoords);

                ICell hittable = (ICell)plottable;
                if (hittable.CellTest(c))
                    return plottable;
            }

            return null;
        }
        /// <summary>
        /// Return the highest hittable plottable at the given point (or null if no hit)
        /// </summary>
        public static IPlottable[] GetCelltable(this Plot plot)
            => plot.GetPlottables().Where(x => x is ICell).ToArray();

        public static List<(int index, string cellName, int currentRow, int currentCol)> BuilderCells(this (int rows, int columns) source)
        {
            var res = new List<(int index, string cellName, int currentRow, int currentCol)>();
            for (int row = 1; row <= source.rows; row++)
            {
                for (int col = 0; col < source.columns; col++)
                {
                    var index_name = source.columns.ToIndexAndName(row, col);
                    //for (int i = 0; i < injectionVolumse.Count; i++)
                    //{
                    //    injectionVolumse[i].Name = $"{index_name}-{i}";
                    //}
                    res.Add(index_name);

                }
            }
            return res;
        }
    }
}
