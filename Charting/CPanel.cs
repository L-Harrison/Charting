using Charting.Models;
using ScottPlot.Drawing;
using ScottPlot.Plottable;
using ScottPlot.SnapLogic;
using ScottPlot;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charting
{
    public class CPanel : IPlottable, IHasColor, IHasArea, IDraggable
    {
        public bool IsVisible { get; set; } = true;
        public int XAxisIndex { get; set; } = 0;
        public int YAxisIndex { get; set; } = 0;
        public Color Color { get; set; } = Color.FromArgb(50, Color.Red);
        public Color BorderColor { get; set; } = Color.Red;
        public float BorderLineWidth { get; set; } = 1;
        public LineStyle BorderLineStyle { get; set; } = LineStyle.Solid;
        public Color HatchColor { get; set; } = Color.Magenta;
        public HatchStyle HatchStyle { get; set; } = HatchStyle.None;
        public string Label { get; set; } = string.Empty;

        public CoordinateRect Rectangle { get; set; }
        public bool DragEnabled { get; set; } = false;

        public Cursor DragCursor => Cursor.Hand;

        public ISnap2D DragSnap { get; set; } = new NoSnap2D();

        public CPanel(CoordinateRect rect)
        {
            Rectangle = rect;
        }

        public event EventHandler Dragged = delegate { };

        public void ValidateData(bool deep = false) { }

        public AxisLimits GetAxisLimits()
        {
            return new AxisLimits(Rectangle.XMin, Rectangle.XMax, Rectangle.YMin, Rectangle.YMax);
        }

        public LegendItem[] GetLegendItems()
        {
            return LegendItem.Single(this, Label, Color);
        }

        public void Render(PlotDimensions dims, Bitmap bmp, bool lowQuality = false)
        {
            using Graphics gfx = GDI.Graphics(bmp, dims, lowQuality);
            using Brush fillBrush = GDI.Brush(Color, HatchColor, HatchStyle);
            using Pen outlinePen = GDI.Pen(BorderColor, (float)BorderLineWidth, BorderLineStyle);

            RectangleF rect = dims.GetRect(Rectangle);
            gfx.FillRectangle(fillBrush, rect);
            gfx.DrawRectangle(outlinePen, rect.X, rect.Y, rect.Width, rect.Height);
        }

        public bool IsUnderMouse(double coordinateX, double coordinateY, double snapX, double snapY)
        {

            (double x, double y) d1 = (Rectangle.XMin, Rectangle.YMin);
            (double x, double y) d2 = (Rectangle.XMax, Rectangle.YMin);
            (double x, double y) d3 = (Rectangle.XMin, Rectangle.YMax);
            (double x, double y) d4 = (Rectangle.XMax, Rectangle.YMax);

            var px = 15;
            if (Math.Abs(d1.x - coordinateX) < snapX && Math.Abs(d1.y - coordinateY) < snapY)
            {
                currentIndex = 0; return true;
            }
            else if (Math.Abs(d2.x - coordinateX) < snapX && Math.Abs(d2.y - coordinateY) < snapY)
            {
                currentIndex = 1; return true;
            }
            else if (Math.Abs(d3.x - coordinateX) < snapX && Math.Abs(d3.y - coordinateY) < snapY)
            {
                currentIndex = 2;
                return true;
            }
            else if (Math.Abs(d4.x - coordinateX) < snapX && Math.Abs(d4.y - coordinateY) < snapY)
            {
                currentIndex = 3;
                return true;
            }
            return false;
        }
        private int currentIndex = -1;

        public void DragTo(double coordinateX, double coordinateY, bool fixedSize)
        {
            if (!DragEnabled)
                return;

            Coordinate requested = new(coordinateX, coordinateY);
            Coordinate snapped = DragSnap.Snap(requested);

            double wdith = Rectangle.XMax - Rectangle.XMin;
            double heigth = Rectangle.YMax - Rectangle.YMin;
            if (currentIndex == 0)
            {
                Rectangle = new CoordinateRect(snapped.X, snapped.X + wdith, snapped.Y, snapped.Y + heigth);
            }
            else if (currentIndex == 1)
            {
                Rectangle = new CoordinateRect(snapped.X - wdith, snapped.X, snapped.Y, snapped.Y + heigth);
            }
            else if (currentIndex == 2)
            {
                Rectangle = new CoordinateRect(snapped.X, snapped.X + wdith, snapped.Y - heigth, snapped.Y);
            }

            else if (currentIndex == 3)
            {
                Rectangle = new CoordinateRect(snapped.X - wdith, snapped.X - wdith, snapped.Y, snapped.Y);
            }

            Dragged(this, new DraggedEventArgs { CoordinateRect = Rectangle, CoordinateX = coordinateX, CoordinateY = coordinateY });
        }
    }
}
