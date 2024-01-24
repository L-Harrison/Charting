using ScottPlot;
using ScottPlot.Plottable;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charting
{
    public static class OscillogramPolygon
    {
        public static Polygon[] DrawPeakArea(this Plot plot, double[] xs, double[] ys, List<(double startTime, double endTime)> timeRange = default, double levelLine = 0, bool isZero = false)
        {
            if (timeRange == default)
            {
                timeRange = new List<(double startTime, double endTime)>() { (xs[0], xs[xs.Length - 1]) };
            }
            var next = true;
            List<(int startIndex, int endIndex)> xy = new List<(int, int)>();
            int x = int.MinValue;
            int y = int.MinValue;
            (double startTime, double endTime) current = timeRange.First();
            for (int i = 0; i < xs.Length; i++)
            {
                current = timeRange.FirstOrDefault(_ => _.startTime <= i && i < _.endTime);
                if (current == default)
                {
                    next = true;
                    if (int.MinValue != x)
                    {
                        if (int.MinValue != y && y - x > 3)
                            xy.Add((x, y));
                        x = y = int.MinValue;
                    }
                }
                if (ys[i] >= levelLine)
                {
                    if (next)
                    {
                        next = false;
                        x = y = i;
                    }
                    else
                    {
                        if (int.MinValue == x)
                            x = i;
                        else
                            y = i;
                    }
                }
                else
                {
                    next = true;
                    if (int.MinValue != x)
                    {
                        if (int.MinValue != y && y - x > 3)
                            xy.Add((x, y));
                        x = y = int.MinValue;
                    }
                }
            }
            if (int.MinValue != x)
            {
                if (int.MinValue != y && y - x > 3)
                    xy.Add((x, y));
                x = y = int.MinValue;
            }
            var polygons = new List<ScottPlot.Plottable.Polygon>();
            if (xy.Any())
            {
                foreach (var item in xy)
                {
                    var min = isZero ? 0 : Math.Min(ys[item.startIndex], ys[item.endIndex]);

                    var gXS = new double[item.endIndex - item.startIndex];
                    var gYS = new double[gXS.Length];

                    Array.Copy(xs, item.startIndex, gXS, 0, gXS.Length);
                    Array.Copy(ys, item.startIndex, gYS, 0, gXS.Length);
                    gYS[0] = min;
                    gYS[gYS.Length - 1] = min;
                    var rg = plot.AddPolygon(gXS, gYS, plot.GetNextColor(.7), lineWidth: 0);
                    polygons.Add(rg);
                }
            }
            return polygons.ToArray();
        }
    }
}
