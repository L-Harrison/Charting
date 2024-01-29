using ScottPlot.Styles;
using ScottPlot;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Charting.Extensions
{
    public static class StyleExtension
    {
        public static IStyle GetLabeColor(this Plot plot, Color bc)
        {
            foreach (var item in ScottPlot.Style.GetStyles())
            {
                if (item.DataBackgroundColor == plot.GetSettings().DataBackground.Color)
                {
                    return item;
                }
            }
            return null!;

        }
    }
}
