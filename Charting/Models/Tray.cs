using ScottPlot.Plottable;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charting.Models
{
    public class Tray
    {
        public Tray(int index, double xPanel, double yPanel, double panelWidth, int rows, int cols, double xRadius, double yRadius, double cellSize)
        {
            Index = index;
            XPanel = xPanel;
            YPanel = yPanel;
            PanelWidth = panelWidth;
            Rows = rows;
            Cols = cols;
            XRadius = xRadius;
            YRadius = yRadius;
            CellSize = cellSize;
        }

        public List<Cell> Cells { get; set; } = new();
        public CPanel? Panel { get; set; }
        public Text? Header { set; get; }
        public Text? Header2 { set; get; }

        public string? HeaderTitle { get; set; }
        public string? Header2Title { get; set; }



        public int Index { get; set; }
        public double XPanel { get; set; }
        public double YPanel { get; set; }
        public double PanelWidth { get; set; }


        public int Rows { get; set; }
        public int Cols { get; set; }

        public double XRadius { get; set; }
        public double YRadius { get; set; }
        public double CellSize { get; set; }
    }
}
