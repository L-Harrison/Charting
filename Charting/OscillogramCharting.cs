using Charting.Models;

using Microsoft.Win32;

using ScottPlot;
using ScottPlot.Control;
using ScottPlot.Plottable;
using ScottPlot.Renderable;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
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
    [TemplatePart(Name = PressureName, Type = typeof(ToggleButton))]
    [TemplatePart(Name = GraphName, Type = typeof(ItemsControl))]
    [TemplatePart(Name = PlotImageName, Type = typeof(Image))]
    [TemplatePart(Name = MarinGName, Type = typeof(Grid))]
    [TemplatePart(Name = GraphContainerGName, Type = typeof(ContentControl))]
    public class OscillogramCharting : UserControl, IDisposable
    {
        private readonly ControlBackEnd Backend;
        private readonly Dictionary<Cursor, System.Windows.Input.Cursor> Cursors;

        private System.Windows.Threading.DispatcherTimer _updateDataTimer;
        private System.Windows.Threading.DispatcherTimer _renderTimer;
        private WriteableBitmap PlotBitmap;
        private float ScaledWidth => (float)(ActualWidth * Configuration.DpiStretchRatio);
        private float ScaledHeight => (float)(ActualHeight * Configuration.DpiStretchRatio);

        public const string GradientName = "Part_Gradient";
        public const string RealGradientName = "Part_RealGradient";
        public const string SpeedName = "Part_Speed";
        public const string PressureName = "Part_Pressure";
        public const string GraphName = "Part_Graph";
        public const string PlotImageName = "Part_PlotImage";
        public const string MarinGName = "Part_MarinG";
        public const string GraphContainerGName = "Part_GraphContainer";


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





        static OscillogramCharting()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(OscillogramCharting), new FrameworkPropertyMetadata(typeof(OscillogramCharting)));
        }

        public OscillogramCharting()
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
            //Backend.Plot.Layout(left: -100, right: -100, bottom: -100, top: -50);
            Backend.Plot.Layout(0, 0, 0, 0, 0);

            _updateDataTimer = new DispatcherTimer();
            _updateDataTimer.Interval = TimeSpan.FromMilliseconds(1);
            _updateDataTimer.Tick += UpdateData;
            _updateDataTimer.Start();

            // create a timer to update the GUI
            _renderTimer = new DispatcherTimer();
            _renderTimer.Interval = TimeSpan.FromMilliseconds(20);
            _renderTimer.Tick += Render;
            _renderTimer.Start();


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

            RightClicked += DefaultRightClickEvent;
            //InitializeComponent();
            //ErrorLabel.Visibility = System.Windows.Visibility.Hidden;
            Backend.StartProcessingEvents();


            ResetBase();
        }

        private void Render(object? sender, EventArgs e)
        {
            //if (AutoAxisCheckbox.IsChecked == true)
            //    wpfPlot1.Plot.AxisAuto();
            Backend.Plot.AxisAuto();
            Backend.WasManuallyRendered = true;
            Backend.Plot.Render(false);
        }

        private void UpdateData(object? sender, EventArgs e)
        {

        }

        /// <summary>
        /// Return the mouse position on the plot (in coordinate space) for the latest X and Y coordinates
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

        /// <summary>
        /// Launch the default right-click menu.
        /// </summary>
        public void DefaultRightClickEvent(object sender, EventArgs e)
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

            cm.IsOpen = true;
        }
        private void RightClickMenu_Copy_Click(object sender, EventArgs e) => System.Windows.Clipboard.SetImage(BmpImageFromBmp(Plot.Render()));
        //private void RightClickMenu_Help_Click(object sender, EventArgs e) => new WPF.HelpWindow().Show();
        //private void RightClickMenu_OpenInNewWindow_Click(object sender, EventArgs e) => new WpfPlotViewer(Plot).Show();
        private void RightClickMenu_AutoAxis_Click(object sender, EventArgs e) { Plot.AxisAuto(); Refresh(); }
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
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (GetTemplateChild(GradientName) is ToggleButton toggleButton)
            {
                toggleButton.Checked += (sender, e) =>
                {
                    if (!GradientShow)
                        GradientShow = true;
                };
                toggleButton.Unchecked += (sender, e) =>
                {
                    if (GradientShow)
                        GradientShow = false;
                };
            }
            if (GetTemplateChild(RealGradientName) is ToggleButton toggleButton1)
            {
                toggleButton1.Checked += (sender, e) =>
                {
                    if (!RealGradientShow)
                        RealGradientShow = true;
                };
                toggleButton1.Unchecked += (sender, e) =>
                {
                    if (RealGradientShow)
                        RealGradientShow = false;
                };
            }
            if (GetTemplateChild(PressureName) is ToggleButton toggleButton3)
            {
                toggleButton3.Checked += (sender, e) =>
                {
                    if (!PressureShow)
                        PressureShow = true;
                };
                toggleButton3.Unchecked += (sender, e) =>
                {
                    if (PressureShow)
                        PressureShow = false;
                };
            }
            if (GetTemplateChild(SpeedName) is ToggleButton toggleButton4)
            {
                toggleButton4.Checked += (sender, e) =>
                {
                    if (!SpeedShow)
                        SpeedShow = true;
                };
                toggleButton4.Unchecked += (sender, e) =>
                {
                    if (SpeedShow)
                        SpeedShow = false;
                };
            }
            if (GetTemplateChild(GraphName) is ItemsControl itemsControl)
            {
                itemsControl.PreviewMouseWheel += (sender, e) =>
                {
                    var ic = sender as ItemsControl;
                    if (ic == null) return;
                    var data = e.GetPosition(this);
                    var count = e.Delta > 0 ? 2 : -2;
                    var scroll = FindVisualChildren<ScrollViewer>(ic).FirstOrDefault();
                    if (scroll != null)
                    {
                        var toHorizontalOffset = (scroll.ExtentWidth / ic.Items.Count) * count + scroll.HorizontalOffset;
                        scroll.ScrollToHorizontalOffset(toHorizontalOffset);
                    }
                };
            }
            if (GetTemplateChild(PlotImageName) is Image contentControl)
            {
                PlotImage = contentControl;
                contentControl.MouseDown += (sender, e) =>
                {
                    var ex = e;
                    Debug.WriteLine("MouseDown");
                    //CaptureMouse(); 
                    Backend.MouseDown(GetInputState(e));
                };
                contentControl.MouseMove += (sender, e) =>
                {
                    var ex = e;
                    Debug.WriteLine("MouseMove");
                    Backend.MouseMove(GetInputState(e));
                    base.OnMouseMove(e);
                };
                contentControl.MouseUp += (sender, e) =>
                {
                    Debug.WriteLine("MouseUp");
                    Backend.MouseUp(GetInputState(e));
                    ReleaseMouseCapture();
                };
                contentControl.MouseWheel += (sender, e) =>
                {
                    Debug.WriteLine("MouseWheel");
                    Backend.MouseWheel(GetInputState(e, e.Delta));
                };
                //contentControl.MouseDoubleClick += (sender, e) =>
                //{
                //    Debug.WriteLine("MouseDoubleClick");
                //    Backend.DoubleClick();
                //};
                contentControl.MouseEnter += (sender, e) =>
                {
                    Debug.WriteLine("MouseEnter");
                    base.OnMouseEnter(e);
                };
                contentControl.MouseLeave += (sender, e) =>
                {
                    Debug.WriteLine("MouseLeave");
                    base.OnMouseLeave(e);
                };

            }
            if (GetTemplateChild(MarinGName) is Grid grid)
            {
                grid.SizeChanged += (sender, e) => Backend.Resize(ScaledWidth, ScaledHeight, useDelayedRendering: true);
            }
        }



        public bool RealGradientShow
        {
            get { return (bool)GetValue(RealGradientShowProperty); }
            set { SetValue(RealGradientShowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for RealGradientShow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RealGradientShowProperty =
            DependencyProperty.Register("RealGradientShow", typeof(bool), typeof(OscillogramCharting), new PropertyMetadata(false, OnRealGradientShowChanged));

        private static void OnRealGradientShowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is OscillogramCharting charting)
            {
                charting.Waves.Add(new OscillogramWave(254) { Color = "#FC1944", IsSelected = true });
                charting.Waves.Add(new OscillogramWave(300) { Color = "#00612D", IsSelected = true });
                charting.Waves.Add(new OscillogramWave(9001) { Color = "#0965EA", IsSelected = true });
                charting.Waves.Add(new OscillogramWave(9002) { Color = "#00FF00", IsSelected = true });
                charting.Waves.Add(new OscillogramWave(9003) { Color = "#FF1122", IsSelected = true });
                charting.Waves.Add(new OscillogramWave(9004) { Color = "#CCCC00", IsSelected = true });

            }
            //d.SetValue(WavesProperty, e.)
            //Waves.Add(120);
            //Waves.Add(254);
            //Waves.Add(460);
        }

        public bool GradientShow
        {
            get { return (bool)GetValue(GradientShowProperty); }
            set { SetValue(GradientShowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GradientShow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GradientShowProperty =
            DependencyProperty.Register("GradientShow", typeof(bool), typeof(OscillogramCharting), new PropertyMetadata(false, OnGradientShowChanged));
        private static void OnGradientShowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }


        public bool SpeedShow
        {
            get { return (bool)GetValue(SpeedShowProperty); }
            set { SetValue(SpeedShowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SpeedShow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpeedShowProperty =
            DependencyProperty.Register("SpeedShow", typeof(bool), typeof(OscillogramCharting), new PropertyMetadata(false, OnSpeedShowChanged));

        private static void OnSpeedShowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }


        public bool PressureShow
        {
            get { return (bool)GetValue(PressureShowProperty); }
            set { SetValue(PressureShowProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PressureShow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PressureShowProperty =
            DependencyProperty.Register("PressureShow", typeof(bool), typeof(OscillogramCharting), new PropertyMetadata(false, OnPressureShowChanged));

        private static void OnPressureShowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        public ObservableCollection<OscillogramWave> Waves
        {
            get { return (ObservableCollection<OscillogramWave>)GetValue(WavesProperty); }
            set { SetValue(WavesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Waves.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WavesProperty =
            DependencyProperty.Register("Waves", typeof(ObservableCollection<OscillogramWave>), typeof(OscillogramCharting), new PropertyMetadata(new ObservableCollection<OscillogramWave>(), OnOscillogramWaveChanged));

        private static void OnOscillogramWaveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public System.Windows.Controls.Image PlotImage
        {
            get { return (System.Windows.Controls.Image)GetValue(PlotImageProperty); }
            set { SetValue(PlotImageProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PlotImage.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PlotImageProperty =
            DependencyProperty.Register("PlotImage", typeof(System.Windows.Controls.Image), typeof(OscillogramCharting), new PropertyMetadata(default(System.Windows.Controls.Image)));



        public Crosshair Crosshair
        {
            get { return (Crosshair)GetValue(CrosshairProperty); }
            set { SetValue(CrosshairProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Crosshair.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CrosshairProperty =
            DependencyProperty.Register("Crosshair", typeof(Crosshair), typeof(OscillogramCharting), new PropertyMetadata(new Crosshair()));



        #region Axis
        /// <summary>
        /// wave axis
        /// </summary>
        private Axis xAxis = new Axis(0,new Edge());
        private Axis yAixs = new Axis(0, new Edge());
        /// <summary>
        /// gradient axis
        /// </summary>
        private Axis yAixsGradient = new Axis(1, new Edge());
        private Axis yAixsSpeed = new Axis(2, new Edge());
        private Axis yAixsPressure = new Axis(3, new Edge());
        #endregion


        private void ResetBase()
        {
            Backend.Reset((float)ActualWidth, (float)ActualHeight);
            Crosshair = Backend.Plot.AddCrosshair(0, 0);
            Backend.Plot.XLabel("TimeSpan (min)");
            Backend.Plot.XAxis.TickLabelFormat(_ => (Math.Round(_ / 60, 1)).ToString());
            Backend.Plot.YAxis.IsVisible = true;

            xAxis = Backend.Plot.XAxis;
            yAixs = Backend.Plot.YAxis;
            yAixs.Label("Spectrum");
            yAixs.LabelStyle(fontSize: 13, rotation: 0);
            yAixs.Color(System.Drawing.Color.Green);
            yAixs.IsVisible = true;

            yAixsGradient = Backend.Plot.AddAxis(ScottPlot.Renderable.Edge.Left);
            yAixsGradient.Label("MobilePhase");
            yAixsGradient.LabelStyle(fontSize: 13, rotation: 0);
            yAixsGradient.Color(System.Drawing.Color.Red);
            yAixsGradient.IsVisible = true;
            yAixsGradient.SetSizeLimit(min: 0, max: 110);

            yAixsSpeed = Backend.Plot.AddAxis(ScottPlot.Renderable.Edge.Right);
            yAixsSpeed.Label("Speed");
            yAixsSpeed.LabelStyle(fontSize: 13, rotation: 0);
            yAixsSpeed.Color(System.Drawing.Color.FromArgb(37, 37, 38));
            yAixsSpeed.IsVisible = true;
            yAixsSpeed.SetSizeLimit(min: 0, max: 200);

            yAixsPressure = Backend.Plot.AddAxis(ScottPlot.Renderable.Edge.Right);
            yAixsPressure.Label("Pressure");
            yAixsPressure.LabelStyle(fontSize: 13, rotation: 0);
            yAixsPressure.Color( System.Drawing.Color.FromArgb(35, 170, 242));
            yAixsPressure.IsVisible = true;
            yAixsPressure.SetSizeLimit(min: 0, max: 9999);

            Crosshair.XAxisIndex = Backend.Plot.XAxis.AxisIndex;
            Crosshair.YAxisIndex = Backend.Plot.YAxis.AxisIndex;
            Crosshair.VerticalLine.PositionFormatter = _ => $"{_:f1} s";
            Crosshair.LineColor = System.Drawing.Color.Green;
        }

        private double[] gradientX = new double[1280];
        private double[] gradientY = new double[1280];
        private double[] gradientRealX = new double[3600_00];
        private double[] gradientRealY = new double[3600_00];
        private void ResetGradient()
        {
          

    

         


            //var scatterFlag = chart.Plot.AddScatter(new double[] { 0 }, new double[] { 1000 }, label: null, color: System.Drawing.Color.Transparent);
            //scatterFlag.YAxisIndex = yAixs.AxisIndex;
            //var scatterFlag1 = chart.Plot.AddScatter(new double[] { 0 }, new double[] { 100 }, label: null, color: System.Drawing.Color.Transparent);
            //scatterFlag1.YAxisIndex = yAixsGradient.AxisIndex;
            //chart.Plot.AxisAutoY(yAxisIndex: yAixs.AxisIndex, margin: 0.5);
            //chart.Plot.AxisAutoY(yAxisIndex: yAixsGradient.AxisIndex, margin: 0.1);
            //chart.Plot.AxisAutoX();
            //chart.Refresh(isHightQuality);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="X"></param>
        /// <param name="Y"></param>
        private void AddGradientPoint(double X, double Y)
        {

        }


        #region private
        private IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null)
            {
                yield break;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                T val = child as T;
                if (val != null)
                {
                    yield return val;
                }

                foreach (T item in FindVisualChildren<T>(child))
                {
                    yield return item;
                }
            }
        }
        #endregion



    }
}
