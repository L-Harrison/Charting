using Microsoft.Win32;
using ScottPlot.Control;
using ScottPlot.Plottable;
using ScottPlot;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows;

namespace Charting
{
    public partial class TrayCore
    {
        #region Draggable

        internal IDraggable GetDraggable(double xPixel, double yPixel, int snapDistancePixels = 5)
        {
            var settings = Plot.GetSettings();
            IDraggable[] enabledDraggables = settings.Plottables
                                  .Where(x => x is IDraggable)
                                  .Select(x => (IDraggable)x)
                                  //.Where(x => x.DragEnabled)
                                  .Where(x => x is IPlottable p && p.IsVisible)
                                  .ToArray();

            foreach (IDraggable draggable in enabledDraggables)
            {
                int xAxisIndex = ((IPlottable)draggable).XAxisIndex;
                int yAxisIndex = ((IPlottable)draggable).YAxisIndex;
                double xUnitsPerPx = settings.GetXAxis(xAxisIndex).Dims.UnitsPerPx;
                double yUnitsPerPx = settings.GetYAxis(yAxisIndex).Dims.UnitsPerPx;

                double snapWidth = xUnitsPerPx * snapDistancePixels;
                double snapHeight = yUnitsPerPx * snapDistancePixels;
                //double xCoords = GetCoordinateX((float)xPixel, xAxisIndex);
                //double yCoords = GetCoordinateY((float)yPixel, yAxisIndex);

                double xCoords = settings.GetXAxis(xAxisIndex).Dims.GetUnit((float)xPixel);
                double yCoords = settings.GetYAxis(yAxisIndex).Dims.GetUnit((float)yPixel);
                if (draggable.IsUnderMouse(xCoords, yCoords, snapWidth, snapHeight))
                    return draggable;
            }

            return null!;
        }
        #endregion

        #region 菜单
        public ContextMenu Menus
        {
            get { return (ContextMenu)GetValue(MenusProperty); }
            set { SetValue(MenusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Menus.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MenusProperty =
            DependencyProperty.Register("Menus", typeof(ContextMenu), typeof(TrayCore), new PropertyMetadata(null!));

        #endregion

        #region Plot
        /// <summary>
        /// This is the plot displayed by the user control. After modifying it you may need to call Render() to request the plot be redrawn on the screen.
        /// </summary>
        public Plot Plot => Backend.Plot;

        /// <summary>
        /// This object can be used to modify advanced behaior and customization of this user control.
        /// </summary>
        public readonly Configuration Configuration;

        /// <summary>
        /// This event is invoked any time the axis limits are modified.
        /// </summary>
        public event EventHandler AxesChanged;

        /// <summary>
        /// This event is invoked any time the plot is right-clicked.
        /// By default it contains DefaultRightClickEvent(), but you can remove this and add your own method.
        /// </summary>
        public event EventHandler RightClicked;

        /// <summary>
        /// This event is invoked any time the plot is left-clicked.
        /// It is typically used to interact with custom plot types.
        /// </summary>
        public event EventHandler LeftClicked;

        /// <summary>
        /// This event is invoked when a <seealso cref="Plottable.IHittable"/> plottable is left-clicked.
        /// </summary>
        public event EventHandler LeftClickedPlottable;

        /// <summary>
        /// This event is invoked after the mouse moves while dragging a draggable plottable.
        /// The object passed is the plottable being dragged.
        /// </summary>
        public event EventHandler PlottableDragged;

        [Obsolete("use 'PlottableDragged' instead", error: true)]
        public event EventHandler MouseDragPlottable;

        /// <summary>
        /// This event is invoked right after a draggable plottable was dropped.
        /// The object passed is the plottable that was just dropped.
        /// </summary>
        public event EventHandler PlottableDropped;
        #endregion


        #region Plot method

        /// <summary>
        /// Return the mouse position on the plot (in coordinate space) for the latest Y and Y coordinates
        /// </summary>
        public (double x, double y) GetMouseCoordinates(int xAxisIndex = 0, int yAxisIndex = 0) => Backend.GetMouseCoordinates(xAxisIndex, yAxisIndex);

        /// <summary>
        /// Return the mouse position (in pixel space) for the last observed mouse position
        /// </summary>
        public (float x, float y) GetMousePixel() => Backend.GetMousePixel();

        /// <summary>
        /// Reset this control by replacing the current plot with a new empty plot
        /// </summary>
        public void Reset() => Backend.Reset((float)ActualWidth, (float)ActualHeight);

        /// <summary>
        /// Reset this control by replacing the current plot with an existing plot
        /// </summary>
        public void Reset(Plot newPlot) => Backend.Reset((float)ActualWidth, (float)ActualHeight, newPlot);

        /// <summary>
        /// Re-render the plot and update the image displayed by this control.
        /// </summary>
        /// <param name="lowQuality">disable anti-aliasing to produce faster (but lower quality) plots</param>
        public void Refresh(bool lowQuality = false)
        {
            Backend.WasManuallyRendered = true;
            Backend.Render(lowQuality);
        }
        public virtual void AutoRender(bool lowQuality = false)
        {
            if (AutoZoom)
                Plot.AxisAuto();
            Refresh(lowQuality);
        }

        // TODO: mark this obsolete in ScottPlot 5.0 (favor Refresh)
        /// <summary>
        /// Re-render the plot and update the image displayed by this control.
        /// </summary>
        /// <param name="lowQuality">disable anti-aliasing to produce faster (but lower quality) plots</param>
        public void Render(bool lowQuality = false) => Refresh(lowQuality);

        /// <summary>
        /// Request the control to refresh the next time it is available.
        /// This method does not block the calling thread.
        /// </summary>
        public void RefreshRequest(RenderType renderType = RenderType.LowQualityThenHighQualityDelayed)
        {
            Backend.WasManuallyRendered = true;
            Backend.RenderRequest(renderType);
        }

        // TODO: mark this obsolete in ScottPlot 5.0 (favor Refresh)
        /// <summary>
        /// Request the control to refresh the next time it is available.
        /// This method does not block the calling thread.
        /// </summary>
        public void RenderRequest(RenderType renderType = RenderType.LowQualityThenHighQualityDelayed) => RefreshRequest(renderType);

        /// <summary>
        /// This object stores the bitmap that is displayed in the PlotImage.
        /// When this control is created or resized this bitmap is replaced by a new one.
        /// When new renders are requested (without resizing) they are drawn onto this existing bitmap.
        /// </summary>

        private InputState GetInputState(MouseEventArgs e, double? delta = null) =>
           new()
           {
               X = (float)e.GetPosition(this).X * Configuration.DpiStretchRatio,
               Y = (float)e.GetPosition(this).Y * Configuration.DpiStretchRatio,
               LeftWasJustPressed = e.LeftButton == MouseButtonState.Pressed,
               RightWasJustPressed = e.RightButton == MouseButtonState.Pressed,
               MiddleWasJustPressed = e.MiddleButton == MouseButtonState.Pressed,
               ShiftDown = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift),
               CtrlDown = Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl),
               AltDown = Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt),
               WheelScrolledUp = delta.HasValue && delta > 0,
               WheelScrolledDown = delta.HasValue && delta < 0,
           };
        private static BitmapImage BmpImageFromBmp(System.Drawing.Bitmap bmp)
        {
            using var memory = new System.IO.MemoryStream();
            bmp.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
            memory.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memory;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        /// <summary>
        /// Replace the existing PlotBitmap with a new one.
        /// </summary>
        public void ReplacePlotBitmap(System.Drawing.Bitmap bmp)
        {
            PlotBitmap = new WriteableBitmap(BmpImageFromBmp(bmp));
            if (PlotImage != null)
                PlotImage.Source = PlotBitmap;
        }

        /// <summary>
        /// Update the PlotBitmap with pixel data from the latest render.
        /// If a PlotBitmap does not exist one will be created.
        /// </summary>
        private void UpdatePlotBitmap(System.Drawing.Bitmap bmp)
        {
            if (PlotBitmap is null)
            {
                ReplacePlotBitmap(Backend.GetLatestBitmap());
                return;
            }

            var rect1 = new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height);
            var flags = System.Drawing.Imaging.ImageLockMode.ReadOnly;
            System.Drawing.Imaging.BitmapData bmpData = bmp.LockBits(rect1, flags, bmp.PixelFormat);

            try
            {
                var rect2 = new System.Windows.Int32Rect(0, 0, bmpData.Width, bmpData.Height);
                PlotBitmap.WritePixels(
                    sourceRect: rect2,
                    buffer: bmpData.Scan0,
                    bufferSize: bmpData.Stride * bmpData.Height,
                    stride: bmpData.Stride);
            }
            finally
            {
                bmp.UnlockBits(bmpData);
            }
        }
        internal void DefaultMenus()
        {
            var cm = new ContextMenu();
            MenuItem SaveImageMenuItem = new() { Header = "Save Image" };
            SaveImageMenuItem.Click += RightClickMenu_SaveImage_Click;
            cm.Items.Add(SaveImageMenuItem);

            MenuItem CopyImageMenuItem = new() { Header = "Copy Image" };
            CopyImageMenuItem.Click += RightClickMenu_Copy_Click;
            cm.Items.Add(CopyImageMenuItem);

            MenuItem AutoAxisMenuItem = new() { Header = "Zoom to Fit Data" };
            AutoAxisMenuItem.Click += RightClickMenu_AutoAxis_Click;
            cm.Items.Add(AutoAxisMenuItem);
            //MenuItem HelpMenuItem = new() { Header = "Help" };
            //HelpMenuItem.Click += RightClickMenu_Help_Click;
            //cm.Items.Add(HelpMenuItem);

            //MenuItem OpenInNewWindowMenuItem = new() { Header = "Open in New Window" };
            //OpenInNewWindowMenuItem.Click += RightClickMenu_OpenInNewWindow_Click;
            //cm.Items.Add(OpenInNewWindowMenuItem);

            Menus = cm;

        }

        /// <summary>
        /// Launch the default right-click menu.
        /// </summary>
        public void DefaultRightClickEvent(object sender, EventArgs e)
        {
            if (Menus != null)
            {
                Menus = ContextMenuPreviousExcute(Menus);
                Menus.IsOpen = true;
            }
        }
        public Func<ContextMenu, ContextMenu> ContextMenuPreviousExcute { get; set; } = (moveTo) => moveTo;
        private void RightClickMenu_Copy_Click(object sender, EventArgs e) => System.Windows.Clipboard.SetImage(BmpImageFromBmp(Plot.Render()));
        private void RightClickMenu_AutoAxis_Click(object sender, EventArgs e)
        {
            AutoZoom = true;
            Plot.AxisAuto();
            Refresh(isHighRefresh);
        }
        private void RightClickMenu_SaveImage_Click(object sender, EventArgs e)
        {
            var sfd = new SaveFileDialog
            {
                FileName = "ScottPlot.png",
                Filter = "PNG Files (*.png)|*.png;*.png" +
                         "|JPG Files (*.jpg, *.jpeg)|*.jpg;*.jpeg" +
                         "|BMP Files (*.bmp)|*.bmp;*.bmp" +
                         "|All files (*.*)|*.*"
            };

            if (sfd.ShowDialog() is true)
                Plot.SaveFig(sfd.FileName);
        }
        #endregion


    }
}
