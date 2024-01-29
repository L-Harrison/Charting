using ScottPlot.Drawing.Colormaps;
using ScottPlot.Drawing;
using ScottPlot.Plottable;
using ScottPlot.SnapLogic;
using ScottPlot;

using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Charting.Models;
using Font = ScottPlot.Drawing.Font;

namespace Charting
{
    public class Cell : IPlottable, IHasColor, IHasArea, ICell, IDraggable
    {
        public int Index { get; set; }
        public string Name { get; set; }
        public Guid Id { get; set; }
        public int RowValue { get; set; }
        public int ColValue { get; set; }

        // data
        // customization
        public bool IsVisible { get; set; } = true;
        public int XAxisIndex { get; set; } = 0;
        public int YAxisIndex { get; set; } = 0;
        //public bool BackgroundFill = false;
        public Color BackgroundColor;
        Font Font = new Font();

        public event EventHandler Dragged = delegate { };

        /// <summary>
        /// 字体颜色
        /// </summary>
        public Color FontColor { get => Font.Color; set => Font.Color = value; }

        public string FontName { get => Font.Name; set => Font.Name = value; }
        public float FontSize { get => Font.Size; set => Font.Size = value; }
        public bool FontBold { get => Font.Bold; set => Font.Bold = value; }
        public Alignment Alignment { get => Font.Alignment; set => Font.Alignment = value; }
        public float Rotation { get => Font.Rotation; set => Font.Rotation = value; }
        //public float BorderSize { get; set; } = 0;

        public float PixelOffsetX { get; set; } = 0;
        public float PixelOffsetY { get; set; } = 0;
        public RectangleF LastRenderRectangleCoordinates { get; set; }
        private double DeltaCX { get; set; } = 0;
        private double DeltaCY { get; set; } = 0;
        public ISnap2D DragSnap { get; set; } = new NoSnap2D();

        public override string ToString() => $"PlottableText \"{Label}\" at ({X}, {Y})";


        /// <summary>
        /// Horizontal center of the circle (axis units)
        /// </summary>
        public double X { get; set; }

        /// <summary>
        /// Vertical center of the circle (axis units)
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Horizontal radius (axis units)
        /// </summary>
        public double RadiusX { get; set; }

        /// <summary>
        /// Vertical radius (axis units)
        /// </summary>
        public double RadiusY { get; set; }
        /// <summary>
        /// 映射成体积
        /// </summary>
        public double CurrentVol { get; set; } = 0d;
        /// <summary>
        /// 总体积
        /// </summary>
        public double Volumns { get; set; } = 10d;

        /// <summary>
        /// Outline thickness (pixel units)
        /// </summary>
        public float BorderLineWidth { get; set; } = 2;

        /// <summary>
        /// Outline line style
        /// </summary>
        public LineStyle BorderLineStyle { get; set; } = LineStyle.Solid;

        /// <summary>
        /// 边框颜色
        /// </summary>
        public Color BorderColor { get; set; } = System.Drawing.Color.Black;
        /// <summary>
        /// 填充颜色
        /// </summary>
        public Color Color { get; set; }

        /// <summary>
        /// Fill pattern
        /// </summary>
        public ScottPlot.Drawing.HatchStyle HatchStyle { get; set; } = ScottPlot.Drawing.HatchStyle.None;

        /// <summary>
        /// Alternate color for fill pattern
        /// </summary>
        public Color HatchColor { get; set; } = Color.Black;

        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Create an ellipse centered at (x, y) with the given horizontal and vertical radius
        /// </summary>
        public Cell(double x, double y, double xRadius, double yRadius)
        {
            X = x;
            Y = y;
            RadiusX = xRadius;
            RadiusY = yRadius;
        }
        /// <summary>
        /// Create an ellipse centered at (x, y) with the given horizontal and vertical radius
        /// </summary>
        public Cell(double x, double y, double xRadius, double yRadius, Color? color = null, Color? borderColor = null, Color? fontColor = null)
        {
            X = x;
            Y = y;
            RadiusX = xRadius;
            RadiusY = yRadius;
            Color = color ?? default;
            BorderColor = borderColor ?? default;
            FontColor = fontColor ?? default;

        }

        // These default values are fine for most cases

        public Cursor CellCursor { get; set; } = Cursor.All;
        public bool CellTestEnabled { get; set; } = true;
        public bool DragEnabled { get; set; } = true;

        public Cursor DragCursor => Cursor.Hand;

        public void ValidateData(bool deep = false)
        {
            if (double.IsNaN(X) || double.IsNaN(Y))
                throw new InvalidOperationException("X and Y cannot be NaN");

            if (double.IsInfinity(X) || double.IsInfinity(Y))
                throw new InvalidOperationException("X and Y cannot be Infinity");

            if (string.IsNullOrWhiteSpace(Label))
                throw new InvalidOperationException("text cannot be null or whitespace");
        }

        // Return an empty array for plottables that do not appear in the legend
        public LegendItem[] GetLegendItems()
        {
            //if (string.IsNullOrWhiteSpace(Label))
            //    return LegendItem.None;

            //LegendItem item = new(this)
            //{
            //    label = Label,
            //    color = Color,
            //    borderColor = BorderColor,
            //    borderLineStyle = BorderLineStyle,
            //    borderWith = BorderLineWidth,
            //};

            return LegendItem.None;
        }

        // This method returns the bounds of the data
        public AxisLimits GetAxisLimits()
        {
            return new AxisLimits(
                xMin: X - RadiusX,
                xMax: X + RadiusX,
                yMin: Y - RadiusY,
                yMax: Y + RadiusY);
        }

        // This method describes how to plot the data on the cart.
        public void Render(PlotDimensions dims, System.Drawing.Bitmap bmp, bool lowQuality = false)
        {


            #region 绘制圆/椭圆

            // Use ScottPlot's GDI helper functions to create System.Drawing objects
            using var gfxxc = ScottPlot.Drawing.GDI.Graphics(bmp, dims, lowQuality);
            using var pen = ScottPlot.Drawing.GDI.Pen(BorderColor, BorderLineWidth, BorderLineStyle);
            //using var brush = ScottPlot.Drawing.GDI.Brush(Color, HatchColor, HatchStyle);
            //using var brush = ScottPlot.Drawing.GDI.Brush(Color.White, HatchColor, HatchStyle);

            // Use 'dims' methods to convert between axis coordinates and pixel positions
            float xPixel = dims.GetPixelX(X);
            float yPixel = dims.GetPixelY(Y);

            // Use 'dims' to determine how large the radius is in pixel units
            float xRadiusPixels = dims.GetPixelX(X + RadiusX) - xPixel;
            float yRadiusPixels = dims.GetPixelY(Y + RadiusY) - yPixel;

            // Center, rotate, and scale the canvas so the ellipse fits in a radius 1 rectangle at the origin
            gfxxc.TranslateTransform(xPixel, yPixel);
            gfxxc.RotateTransform(Rotation);
            gfxxc.ScaleTransform(xRadiusPixels, yRadiusPixels);
            RectangleF rect = new(-1, -1, 2, 2);

            // Render data by drawing on the Graphics object
            //gfxxc.FillEllipse(brush, rect);  
            //if (Color != Color.Transparent)
            //    gfxxc.FillEllipse(brush, rect);

            // Otherwise the pen width will be scaled as well
            using System.Drawing.Drawing2D.Matrix invertScaleMatrix = new();
            invertScaleMatrix.Scale(1 / xRadiusPixels, 1 / yRadiusPixels);
            pen.Transform = invertScaleMatrix;

            gfxxc.DrawEllipse(pen, rect);
            #endregion

            #region MyRegion
            // Use ScottPlot's GDI helper functions to create System.Drawing objects
            using var gfxx = ScottPlot.Drawing.GDI.Graphics(bmp, dims, lowQuality);
            using var penARC = ScottPlot.Drawing.GDI.Pen(Color, BorderLineWidth, BorderLineStyle);
            using var brushARC = ScottPlot.Drawing.GDI.Brush(Color, HatchColor, HatchStyle);

            GraphicsPath myGraphicsPath = new GraphicsPath();

            // Use 'dims' methods to convert between axis coordinates and pixel positions
            float xPixelARC = dims.GetPixelX(X);
            float yPixelARC = dims.GetPixelY(Y);

            // Use 'dims' to determine how large the radius is in pixel units
            float xRadiusPixelsARC = dims.GetPixelX(X + RadiusX - 2) - xPixelARC;
            float yRadiusPixelsARC = dims.GetPixelY(Y + RadiusY - 2) - yPixelARC;

            // Center, rotate, and scale the canvas so the ellipse fits in a radius 1 rectangle at the origin
            gfxx.TranslateTransform(xPixelARC, yPixelARC);
            gfxx.RotateTransform(Rotation);
            gfxx.ScaleTransform(xRadiusPixelsARC, yRadiusPixelsARC);

            // Otherwise the pen width will be scaled as well
            using System.Drawing.Drawing2D.Matrix invertScaleMatrixARC = new();
            invertScaleMatrixARC.Scale(1 / xRadiusPixelsARC, 1 / yRadiusPixelsARC);
            penARC.Transform = invertScaleMatrixARC;

            //gfxx.DrawArc(penARC, rectARC, startAngle, sweepAngle);
            RectangleF rectARC = new(-1, -1, 2, 2);

            float startAngle = 0f;
            float sweepAngle = 0f;

            var y = Volumns / 2;
            var x = CurrentVol;
            var isMinus = x < y;
            if (isMinus)
            {
                var angle = Math.Acos((y - x) / y) * 180 / Math.PI;
                startAngle = (float)(270 - angle);
                sweepAngle = (90 - (startAngle - 180)) * 2;
            }
            else
            {
                var angle = Math.Asin((x - y) / y) * 180 / Math.PI;
                startAngle = (float)(180 - angle);
                sweepAngle = (180 - startAngle) * 2 + 180;
            }
            myGraphicsPath.AddArc(rectARC, startAngle, sweepAngle);
            gfxx.FillPath(brushARC, myGraphicsPath);
            gfxx.DrawPath(penARC, myGraphicsPath);
            #endregion

            #region 绘制文字

            if (Font.Size <= 5)
                Font.Size = 5;
            if (Font.Size > 33)
                Font.Size = 33;

            using (Graphics gfx = GDI.Graphics(bmp, dims, lowQuality))
            using (var font = GDI.Font(Font))
            using (var fontBrush = new SolidBrush(FontColor))
            using (var frameBrush = new SolidBrush(BackgroundColor))
            //using (var outlinePen = new System.Drawing.Pen(BorderColor, BorderSize))
            //using (var redPen = new System.Drawing.Pen(Color.Red, BorderSize))
            {
                float pixelX = dims.GetPixelX(X) + PixelOffsetX;
                float pixelY = dims.GetPixelY(Y) - PixelOffsetY;
                SizeF stringSize = GDI.MeasureString(gfx, Label, font);

                gfx.TranslateTransform(pixelX, pixelY);
                gfx.RotateTransform(Font.Rotation);

                (float dX, float dY) = GDI.TranslateString(gfx, Label, Font);
                gfx.TranslateTransform(-dX, -dY);

                //if (BackgroundFill)
                //{
                //    RectangleF stringRect = new(0, 0, stringSize.Width, stringSize.Height);
                //    gfx.FillRectangle(frameBrush, stringRect);
                //    if (BorderSize > 0)
                //        gfx.DrawRectangle(outlinePen, stringRect.X, stringRect.Y, stringRect.Width, stringRect.Height);
                //}

                gfx.DrawString(Label, font, fontBrush, new PointF(0, 0));

                GDI.ResetTransformPreservingScale(gfx, dims);

                double degangle = Font.Rotation * Math.PI / 180;
                float xA = pixelX - dX;
                float yA = pixelY - dY;
                float xC = xA + stringSize.Width * (float)Math.Cos(degangle) - stringSize.Height * (float)Math.Sin(degangle);
                float yC = yA + stringSize.Height * (float)Math.Cos(degangle) + stringSize.Width * (float)Math.Sin(degangle);

                PointF pointA = new(xA, yA);
                PointF pointC = new(xC, yC);

                LastRenderRectangleCoordinates = RectangleF.FromLTRB(
                    left: (float)dims.GetCoordinateX(pointA.X),
                    top: (float)dims.GetCoordinateY(pointC.Y),
                    right: (float)dims.GetCoordinateX(pointC.X),
                    bottom: (float)dims.GetCoordinateY(pointA.Y));
            }
            #endregion
        }

        public bool CellTest(Coordinate coord)
            => CellTestEnabled && Math.Abs(coord.X - X) <= RadiusX && Math.Abs(coord.Y - Y) <= RadiusY;

        public bool IsUnderMouse(double coordinateX, double coordinateY, double snapX, double snapY)
        {
            double dX = Math.Abs(X - coordinateX);
            double dY = Math.Abs(Y - coordinateY);
            if (dX - RadiusX <= snapX && dY - RadiusY <= snapY)
            {
                return true;
            }
            return false;
        }

        public void DragTo(double coordinateX, double coordinateY, bool fixedSize)
        {
            if (!DragEnabled)
                return;

            Coordinate requested = new(coordinateX, coordinateY);
            Coordinate snapped = DragSnap.Snap(requested);
            Coordinate actual = MovePointFunc(X, Y, snapped);
            X = actual.X;
            Y = actual.Y;
            Dragged(this, new DraggedEventArgs { CoordinateX = coordinateX, CoordinateY = coordinateY });
        }
        /// <summary>
        /// Assign custom the logic here to control where individual points can be moved.
        /// This logic occurs after snapping.
        /// </summary>
        public Func<double, double, Coordinate, Coordinate> MovePointFunc { get; set; } = (x, y, moveTo) => moveTo;

    }
}
