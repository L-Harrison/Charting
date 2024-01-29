using Charting.Models;

using Microsoft.Win32;

using ScottPlot;
using ScottPlot.Control;
using ScottPlot.Drawing;
using ScottPlot.Plottable;
using ScottPlot.Renderable;
using ScottPlot.SnapLogic;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using Cursor = ScottPlot.Cursor;
using Image = System.Windows.Controls.Image;


namespace Charting
{
    [TemplatePart(Name = GradientName, Type = typeof(ToggleButton))]
    [TemplatePart(Name = RealGradientName, Type = typeof(ToggleButton))]
    [TemplatePart(Name = SpeedName, Type = typeof(ToggleButton))]
    [TemplatePart(Name = RealSpeedName, Type = typeof(ToggleButton))]
    [TemplatePart(Name = PressureName, Type = typeof(ToggleButton))]
    [TemplatePart(Name = GraphName, Type = typeof(ItemsControl))]
    [TemplatePart(Name = PlotImageName, Type = typeof(Image))]
    [TemplatePart(Name = MarinGName, Type = typeof(Grid))]
    public class OscillogramChartingCore : UserControl
    {

        public const string GradientName = "Part_Gradient";
        public const string RealGradientName = "Part_RealGradient";
        public const string SpeedName = "Part_Speed";
        public const string RealSpeedName = "Part_RealSpeed";
        public const string PressureName = "Part_Pressure";
        public const string GraphName = "Part_Graph";
        public const string PlotImageName = "Part_PlotImage";
        public const string MarinGName = "Part_MarinG";


        private readonly ControlBackEnd Backend;
        private readonly Dictionary<Cursor, System.Windows.Input.Cursor> Cursors;

        private System.Windows.Controls.Image PlotImage;
        private WriteableBitmap PlotBitmap;
        private float ScaledWidth => (float)(ActualWidth * Configuration.DpiStretchRatio);
        private float ScaledHeight => (float)(ActualHeight * Configuration.DpiStretchRatio);
        internal bool AutoZoom { set; get; } = true;
        internal bool isHighRefresh = true;


        #region Draggable
        internal EventHandler<DraggableGraphContext> DraggableUpdatedHandler { get; set; }
        internal DraggableGraphContext CurrentDraggableGraph
        {
            get { return (DraggableGraphContext)GetValue(CurrentDraggableGraphProperty); }
            set { SetValue(CurrentDraggableGraphProperty, value); }
        }
        // Using a DependencyProperty as the backing store for CurrentDraggableGraph.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentDraggableGraphProperty =
            DependencyProperty.Register("CurrentDraggableGraph", typeof(DraggableGraphContext), typeof(OscillogramChartingCore), new PropertyMetadata(new DraggableGraphContext()));        
    
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

        #region Crosshair
        public Crosshair Crosshair
        {
            get { return (Crosshair)GetValue(CrosshairProperty); }
            set { SetValue(CrosshairProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Crosshair.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CrosshairProperty =
            DependencyProperty.Register("Crosshair", typeof(Crosshair), typeof(OscillogramChartingCore), new PropertyMetadata(new Crosshair()));
        #endregion

        #region 菜单
        /// <summary>
        /// 菜单
        /// </summary>
        public ContextMenu Menus
        {
            get { return (ContextMenu)GetValue(MenusProperty); }
            set { SetValue(MenusProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Menus.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MenusProperty =
            DependencyProperty.Register("Menus", typeof(ContextMenu), typeof(OscillogramChartingCore), new PropertyMetadata(null!));

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

        static OscillogramChartingCore()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(OscillogramChartingCore), new FrameworkPropertyMetadata(typeof(OscillogramChartingCore)));
        }
        public OscillogramChartingCore()
        {
            Backend = new ControlBackEnd(1, 1, GetType().Name);
            Backend.Resize((float)ActualWidth, (float)ActualHeight, useDelayedRendering: false);
            Backend.BitmapChanged += new EventHandler((sender, e) => ReplacePlotBitmap(Backend.GetLatestBitmap()));
            Backend.BitmapUpdated += new EventHandler((sender, e) => UpdatePlotBitmap(Backend.GetLatestBitmap()));
            Backend.CursorChanged += new EventHandler((sender, e) => Cursor = Cursors[Backend.Cursor]);
            Backend.RightClicked += new EventHandler((sender, e) => RightClicked?.Invoke(this, e));
            Backend.LeftClicked += new EventHandler((sender, e) => LeftClicked?.Invoke(this, e));
            Backend.LeftClickedPlottable += new EventHandler((sender, e) => LeftClickedPlottable?.Invoke(this, e));
            Backend.AxesChanged += new EventHandler((sender, e) => AxesChanged?.Invoke(this, e));
            Backend.PlottableDragged += new EventHandler((sender, e) => PlottableDragged?.Invoke(sender, e));
            Backend.PlottableDropped += new EventHandler((sender, e) => PlottableDropped?.Invoke(sender, e));
            Backend.Configuration.ScaleChanged += new EventHandler((sender, e) => Backend.Resize(ScaledWidth, ScaledHeight, useDelayedRendering: true));
            Configuration = Backend.Configuration;

            //Configuration.AltLeftClickDragZoom = false;
            //Configuration.LeftClickDragPan = false;

            //Configuration.Quality = ScottPlot.Control.QualityMode.Low;
            //Configuration.DpiStretch = false;
            //Configuration.DpiStretchRatio =.9f;

            Backend.Plot.Style(ScottPlot.Style.Blue1);

            Backend.Plot.Layout(0, 0, 0, 0, 0);

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                try
                {
                    Configuration.WarnIfRenderNotCalledManually = false;
                    Plot.Title($"ScottPlot {Plot.Version}");
                    Plot.Render();
                }
                catch (Exception e)
                {
                    //InitializeComponent();
                    PlotImage.Visibility = System.Windows.Visibility.Hidden;
                    //ErrorLabel.Text = "ERROR: ScottPlot failed to render in design mode.\n\n" +
                    //    "This may be due to incompatible System.Drawing.Common versions or a 32-bit/64-bit mismatch.\n\n" +
                    //    "Although rendering failed at design time, it may still function normally at runtime.\n\n" +
                    //    $"Exception details:\n{e}";
                    return;
                }
            }
            Cursors = new Dictionary<Cursor, System.Windows.Input.Cursor>()
            {
                [ScottPlot.Cursor.Arrow] = System.Windows.Input.Cursors.Arrow,
                [ScottPlot.Cursor.WE] = System.Windows.Input.Cursors.SizeWE,
                [ScottPlot.Cursor.NS] = System.Windows.Input.Cursors.SizeNS,
                [ScottPlot.Cursor.All] = System.Windows.Input.Cursors.SizeAll,
                [ScottPlot.Cursor.Crosshair] = System.Windows.Input.Cursors.Cross,
                [ScottPlot.Cursor.Hand] = System.Windows.Input.Cursors.Hand,
                [ScottPlot.Cursor.Question] = System.Windows.Input.Cursors.Help,
            };

            DefaultMenus();
            RightClicked += DefaultRightClickEvent!;
            //InitializeComponent();
            //ErrorLabel.Visibility = System.Windows.Visibility.Hidden;
            Backend.StartProcessingEvents();

            Crosshair = Backend.Plot.AddCrosshair(0, 0);


        }

        #region Plot method

        public ThemesStyle Theme
        {
            get { return (ThemesStyle)GetValue(ThemeProperty); }
            set { SetValue(ThemeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Theme.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ThemeProperty =
            DependencyProperty.Register("Theme", typeof(ThemesStyle), typeof(OscillogramChartingCore), new PropertyMetadata(ThemesStyle.Default, (d, e) =>
            {
                var theme = ((ThemesStyle)e.NewValue).ToString();
                var style = ScottPlot.Style.GetStyles().Where(_ => _.GetType().Name == theme).FirstOrDefault() ?? ScottPlot.Style.Default;

                ((OscillogramChartingCore)d).Backend.Plot.Style(style);

             

            }));





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
            {
                Plot.AxisAuto();
            }
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
        //private void RightClickMenu_Help_Click(object sender, EventArgs e) => new WPF.HelpWindow().Show();
        //private void RightClickMenu_OpenInNewWindow_Click(object sender, EventArgs e) => new WpfPlotViewer(Plot).Show();
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

        #region OnApplyTemplate
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (GetTemplateChild(PlotImageName) is Image contentControl)
            {
                PlotImage = contentControl;

                contentControl.MouseDown += (sender, e) =>
                {
                    //Mouse.Capture(contentControl);
                    Backend.MouseDown(GetInputState(e));

                    if (e.ChangedButton == MouseButton.Left)
                        AutoZoom = false;

                };
                contentControl.MouseMove += (sender, e) =>
                {
                    Backend.MouseMove(GetInputState(e));
                    base.OnMouseMove(e);

                    var pixelX = e.MouseDevice.GetPosition(this).X;
                    var pixelY = e.MouseDevice.GetPosition(this).Y;

                    (double coordinateX, double coordinateY) = this.GetMouseCoordinates(0, 0);
                    Crosshair.X = coordinateX;
                    Crosshair.Y = coordinateY;

                    var p = Plot.GetPlottables();

                    var list = new List<(ScatterPlot plot, double pointX, double pointY, int pointIndex, double rx)>();
                    foreach (var item in Plot.GetPlottables().Where(x => x is IPlottable p && p.IsVisible))
                    {
                        if (item is ScatterPlot sp)
                        {
                            (double pointX, double pointY, int pointIndex) = sp.GetPointNearest(coordinateX, coordinateY);
                            if (Math.Abs(pointX - coordinateX) < 10 && Math.Abs(pointY - coordinateY) < 10)
                            {
                                var rt = Math.Abs(pointX - coordinateX) < Math.Abs(pointY - coordinateY) ? Math.Abs(pointX - coordinateX) : Math.Abs(pointY - coordinateY);
                                list.Add((sp, pointX, pointY, pointIndex, rt));
                            }
                        }
                    }

                    var dr = Plot.GetDraggable(pixelX, pixelY, 30);
                    if (dr != null && dr is ScatterPlot scatterPlot)
                    {
                        //scatterPlot.MarkerShape = MarkerShape.filledDiamond;
                        Crosshair.Color = scatterPlot.Color;
                    }
                    else
                    {
                        if (list.Any())
                        {
                            var plt = list.OrderBy(_ => _.rx).First().plot;
                            //plt.IsHighlighted = true;
                            Crosshair.Color = plt.Color;
                            //plt.MarkerShape = MarkerShape.none;
                        }
                        else
                        {
                            Crosshair.Color = System.Drawing.Color.Green;
                        }

                    }
                    this.Refresh(isHighRefresh);
                };
                contentControl.MouseUp += (sender, e) =>
                {
                    Backend.MouseUp(GetInputState(e));
                    ReleaseMouseCapture();
                };
                contentControl.MouseWheel += (sender, e) =>
                {
                    Backend.MouseWheel(GetInputState(e, e.Delta));
                    AutoZoom = false;
                };
                //contentControl.MouseDoubleClick += (sender, e) =>
                //{
                //    Debug.WriteLine("MouseDoubleClick");
                //    Backend.DoubleClick();
                //};
                contentControl.MouseEnter += (sender, e) =>
                {
                    //Mouse.Capture(contentControl);
                    Crosshair.IsVisible = true;
                    //isHighRefresh = true;
                    base.OnMouseEnter(e);
                };
                contentControl.MouseLeave += (sender, e) =>
                {
                    Crosshair.IsVisible = false;
                    //isHighRefresh = false;
                    //if (Mouse.Captured == contentControl)
                    //    Mouse.Capture(null);
                    base.OnMouseLeave(e);
                };
                contentControl.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) =>
                {
                    Trace.WriteLine(e.ClickCount);

                    (double coordinateX, double coordinateY) = GetMouseCoordinates(0, 0);
                    var pixelX = e.MouseDevice.GetPosition(this).X;
                    var pixelY = e.MouseDevice.GetPosition(this).Y;
                    var dr = GetDraggable(pixelX, pixelY, 30);
                    if (dr != null && dr.DragEnabled)
                    {
                        if (dr is IGraphType graphType)
                            CurrentDraggableGraph.CurrentDraggableGraphType = graphType.GraphType;
                        else
                            CurrentDraggableGraph.CurrentDraggableGraphType = GraphType.GraphType;
                    }
                    if (e.ClickCount == 2)
                    {

                        var ht = Plot.GetHittable(pixelX, pixelY);
                        if (ht != null)
                        {
                        }
                        else
                        {
                            UpdateDraggable(dr!);
                        }
                        DraggableUpdatedHandler?.Invoke(sender, CurrentDraggableGraph);
                    }
                    else
                    {
                        DraggableUpdatedHandler?.Invoke(sender, CurrentDraggableGraph);

                    }
                };

            }
            if (GetTemplateChild(MarinGName) is Grid grid)
            {
                grid.SizeChanged += (sender, e) =>
                   Backend.Resize(ScaledWidth, ScaledHeight, useDelayedRendering: true);
            }
        }
        private void UpdateDraggable(IDraggable? dr)
        {
            if (dr == null) return;
            if (dr.DragEnabled)
            {

                if (CurrentDraggableGraph.DraggableGraph.Contains(dr))
                    CurrentDraggableGraph.DraggableGraph.Remove(dr);
                if (CurrentDraggableGraph.CurrentDraggableGraph == dr)
                {
                    CurrentDraggableGraph.CurrentDraggableGraph = null!;
                    CurrentDraggableGraph.CurrentDraggableGraphType = GraphType.GraphType;
                }
            }
            else
            {
                if (!CurrentDraggableGraph.DraggableGraph.Contains(dr))
                    CurrentDraggableGraph.DraggableGraph.Add(dr);
                if (CurrentDraggableGraph.CurrentDraggableGraph != dr)
                {
                    CurrentDraggableGraph.CurrentDraggableGraph = dr!;
                    if (dr is IGraphType graphType)
                        CurrentDraggableGraph.CurrentDraggableGraphType = graphType.GraphType;
                    else
                        CurrentDraggableGraph.CurrentDraggableGraphType = GraphType.GraphType;
                }
            }
            if (dr is ScatterPlot scatterPlot)
            {
                if (dr.DragEnabled)
                {
                    scatterPlot.MarkerShape = MarkerShape.none;
                    dr.DragEnabled = false;
                }
                else
                {
                    scatterPlot.MarkerShape = MarkerShape.filledCircle;
                    dr.DragEnabled = true;
                }
            }
            else if (dr is OscillogramVLine vLine)
            {
                if (dr.DragEnabled)
                {
                    vLine.LineWidth = 1;
                    dr.DragEnabled = false;
                }
                else
                {
                    vLine.LineWidth = 3;
                    dr.DragEnabled = true;
                }
            }
            else
            {

            }

        }

        #endregion



    }
}
