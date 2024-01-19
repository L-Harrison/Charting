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
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using Cursor = ScottPlot.Cursor;
using Image = System.Windows.Controls.Image;

namespace Charting
{
    [TemplatePart(Name = OscillogramChartingCoreName, Type = typeof(OscillogramChartingCore))]
    public class OscillogramCharting : UserControl, IDisposable
    {
        public const string OscillogramChartingCoreName = "Part_OscillogramChartingCore";
        /// <summary>
        /// 总点数  单副图最大点数 
        /// 超过点数将不在刷新新增图
        /// </summary>
        public const int AllNumConst = 86400;


        private System.Windows.Threading.DispatcherTimer _updateDataTimer;
        private System.Windows.Threading.DispatcherTimer _updateGradientDataTimer;
        private System.Windows.Threading.DispatcherTimer _updateRealGradientDataTimer;
        private System.Windows.Threading.DispatcherTimer _updateSpeedDataTimer;
        private System.Windows.Threading.DispatcherTimer _updateRealSpeedDataTimer;
        private System.Windows.Threading.DispatcherTimer _updatePressureDataTimer;
        private System.Windows.Threading.DispatcherTimer _renderTimer;

        private OscillogramChartingCore Oscill;

        #region dp
        #region base data

        /// <summary>
        /// 当前时间索引位
        /// </summary>
        public int CurrentTimeIndex
        {
            get { return (int)GetValue(CurrentTimeIndexProperty); }
            set { SetValue(CurrentTimeIndexProperty, value); }
        }
        // Using a DependencyProperty as the backing store for CurrentTimeIndex.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CurrentTimeIndexProperty =
            DependencyProperty.Register("CurrentTimeIndex", typeof(int), typeof(OscillogramCharting), new PropertyMetadata(0));


        /// <summary>
        /// 最后时间索引位
        /// </summary>
        public int LastTimeIndex
        {
            get { return (int)GetValue(LastTimeIndexProperty); }
            set { SetValue(LastTimeIndexProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LastTimeIndex.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LastTimeIndexProperty =
            DependencyProperty.Register("LastTimeIndex", typeof(int), typeof(OscillogramCharting), new PropertyMetadata(59, OnLastTimeIndexChanged));

        private static void OnLastTimeIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((OscillogramCharting)d).Initilize();
            //((OscillogramCharting)d).ResetSpectrum();
        }


        /// <summary>
        /// 梯度点数
        /// </summary>
        public int GradientNum
        {
            get { return (int)GetValue(GradientNumProperty); }
            set { SetValue(GradientNumProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GradientNum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GradientNumProperty =
            DependencyProperty.Register("GradientNum", typeof(int), typeof(OscillogramCharting), new PropertyMetadata(1280));


        /// <summary>
        /// 速度点数
        /// </summary>
        public int SpeedNum
        {
            get { return (int)GetValue(SpeedNumProperty); }
            set { SetValue(SpeedNumProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SpeedNum.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpeedNumProperty =
            DependencyProperty.Register("SpeedNum", typeof(int), typeof(OscillogramCharting), new PropertyMetadata(1280));


       
        #endregion
        #region 
        public List<string> ColorShowSource
        {
            get { return (List<string>)GetValue(ColorShowSourceProperty); }
            set { SetValue(ColorShowSourceProperty, value); }
        }
        // Using a DependencyProperty as the backing store for ColorShowSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColorShowSourceProperty =
            DependencyProperty.Register("ColorShowSource", typeof(List<string>), typeof(OscillogramCharting), new PropertyMetadata(new List<string>()));
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
            if (d is OscillogramCharting charting && charting.scatterRealGradient != null)
            {
                charting.scatterRealGradient.IsVisible = (bool)e.NewValue;
            }
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
            if (d is OscillogramCharting charting && charting.scatterGradient != null)
            {
                charting.scatterGradient.IsVisible = (bool)e.NewValue;
            }
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
            if (d is OscillogramCharting charting && charting.scatterSpeed != null)
            {
                charting.scatterSpeed.IsVisible = (bool)e.NewValue;
            }
        }
        public bool RealSpeedShow
        {
            get { return (bool)GetValue(RealSpeedShowProperty); }
            set { SetValue(RealSpeedShowProperty, value); }
        }
        // Using a DependencyProperty as the backing store for RealSpeedShow.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty RealSpeedShowProperty =
            DependencyProperty.Register("RealSpeedShow", typeof(bool), typeof(OscillogramCharting), new PropertyMetadata(false, OnRealSpeedShowChanged));
        private static void OnRealSpeedShowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is OscillogramCharting charting && charting.scatterRealSpeed != null)
            {
                charting.scatterRealSpeed.IsVisible = (bool)e.NewValue;
            }
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
            if (d is OscillogramCharting charting && charting.scatterPressure != null)
            {
                charting.scatterPressure.IsVisible = (bool)e.NewValue;
            }
        }
        public ObservableCollection<OscillogramWave> Waves
        {
            get { return (ObservableCollection<OscillogramWave>)GetValue(WavesProperty); }
            set { SetValue(WavesProperty, value); }
        }
        // Using a DependencyProperty as the backing store for Waves.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WavesProperty =
            DependencyProperty.Register("Waves", typeof(ObservableCollection<OscillogramWave>), typeof(OscillogramCharting), new PropertyMetadata(new ObservableCollection<OscillogramWave>()));
        #endregion
        #endregion

        #region update data


        #region gradient
        public double[] GradientX
        {
            get { return (double[])GetValue(GradientXProperty); }
            set { SetValue(GradientXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GradientX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GradientXProperty =
            DependencyProperty.Register("GradientX", typeof(double[]), typeof(OscillogramCharting), new PropertyMetadata(new double[2], OnGradientChanged));

        public double[] GradientY
        {
            get { return (double[])GetValue(GradientYProperty); }
            set { SetValue(GradientYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for GradientY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GradientYProperty =
            DependencyProperty.Register("GradientY", typeof(double[]), typeof(OscillogramCharting), new PropertyMetadata(new double[2], OnGradientChanged));
        private static void OnGradientChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((OscillogramCharting)d).ResetGradient();
        }
        #endregion


        #region speed

        public double[] SpeedX
        {
            get { return (double[])GetValue(SpeedXProperty); }
            set { SetValue(SpeedXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SpeedX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpeedXProperty =
            DependencyProperty.Register("SpeedX", typeof(double[]), typeof(OscillogramCharting), new PropertyMetadata(new double[2], OnSpeedChanged));

        public double[] SpeedY
        {
            get { return (double[])GetValue(SpeedYProperty); }
            set { SetValue(SpeedYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SpeedY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpeedYProperty =
            DependencyProperty.Register("SpeedY", typeof(double[]), typeof(OscillogramCharting), new PropertyMetadata(new double[2], OnSpeedChanged));

        private static void OnSpeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((OscillogramCharting)d).ResetSpeed();
        }
        #endregion


        #region speed real

        public double[] SpeedRealX
        {
            get { return (double[])GetValue(SpeedRealXProperty); }
            set { SetValue(SpeedRealXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SpeedRealX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpeedRealXProperty =
            DependencyProperty.Register("SpeedRealX", typeof(double[]), typeof(OscillogramCharting), new PropertyMetadata(new double[2], (d, e) =>
            {
                //if (e.NewValue is double[] ex)
                //{
                //    if (ex.Length < OscillogramCharting.AllNumConst/*((OscillogramCharting)d).LastTimeIndex*/)
                //    {
                //        var db = new double[OscillogramCharting.AllNumConst];
                //        Array.Copy(ex, db, ex.Length);

                //        //((OscillogramCharting)d).SpeedRealX.setn
                //        ((OscillogramCharting)d).SpeedRealX = db;
                //    }
                //}
                ((OscillogramCharting)d).ResetRealSpeed();
            }));

        public double[] SpeedRealY
        {
            get { return (double[])GetValue(SpeedRealYProperty); }
            set { SetValue(SpeedRealYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SpeedRealX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpeedRealYProperty =
            DependencyProperty.Register("SpeedRealY", typeof(double[]), typeof(OscillogramCharting), new PropertyMetadata(new double[2], (d, e) =>
            {
                //if (e.NewValue is double[] ex)
                //{
                //    if (ex.Length < OscillogramCharting.AllNumConst/*((OscillogramCharting)d).LastTimeIndex*/)
                //    {
                //        var db = new double[OscillogramCharting.AllNumConst];
                //        Array.Copy(ex, db, ex.Length);

                //        ((OscillogramCharting)d).SpeedRealY = db;
                //    }
                //}
                 ((OscillogramCharting)d).ResetRealSpeed();
            }));

        private static void OnSpeedRealChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            //if (e.NewValue is double[] ex)
            //{
            //    if (ex.Length < ((OscillogramCharting)d).LastTimeIndex)
            //    {
            //        var db = new double[OscillogramCharting.AllNumConst];
            //        Array.Copy(ex, db, ex.Length); 
            //        var Types = d.GetType();//获得类型  
            //        foreach (var item in Types.GetProperties())
            //        {
            //            if (item.Name == e.Property.Name)
            //            {
            //                item.SetValue(d, db);
            //            }
            //        }
            //    }
            //}
            ((OscillogramCharting)d).ResetRealSpeed();
        }
        #endregion

        #region gradient real
        public double[] GradientRealX
        {
            get { return (double[])GetValue(GradientRealXProperty); }
            set { SetValue(GradientRealXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SpeedRealX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GradientRealXProperty =
            DependencyProperty.Register("GradientRealX", typeof(double[]), typeof(OscillogramCharting), new PropertyMetadata(new double[2], OnGradientRealChanged));

        public double[] GradientRealY
        {
            get { return (double[])GetValue(GradientRealYProperty); }
            set { SetValue(GradientRealYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SpeedRealX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GradientRealYProperty =
            DependencyProperty.Register("GradientRealY", typeof(double[]), typeof(OscillogramCharting), new PropertyMetadata(new double[2], OnGradientRealChanged));

        private static void OnGradientRealChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //if (e.NewValue is double[] ex)
            //{
            //    if (ex.Length < ((OscillogramCharting)d).LastTimeIndex)
            //    {
            //        var db = new double[OscillogramCharting.AllNumConst];
            //        Array.Copy(ex, db, ex.Length);
            //        var Types = d.GetType();//获得类型  
            //        foreach (var item in Types.GetProperties())
            //        {
            //            if (item.Name == e.Property.Name)
            //            {
            //                item.SetValue(d, db);
            //            }
            //        }
            //    }
            //}
            ((OscillogramCharting)d).ResetRealGradient();
        }
        #endregion

        #region pressure real
        public double[] PressureX
        {
            get { return (double[])GetValue(PressureXProperty); }
            set { SetValue(PressureXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SpeedRealX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PressureXProperty =
            DependencyProperty.Register("PressureX", typeof(double[]), typeof(OscillogramCharting), new PropertyMetadata(new double[2], OnPressureChanged));

        public double[] PressureY
        {
            get { return (double[])GetValue(PressureYProperty); }
            set { SetValue(PressureYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SpeedRealX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PressureYProperty =
            DependencyProperty.Register("PressureY", typeof(double[]), typeof(OscillogramCharting), new PropertyMetadata(new double[2], OnPressureChanged));

        private static void OnPressureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is double[] ex)
            {
                //if (ex.Length < ((OscillogramCharting)d).LastTimeIndex)
                //{
                //    var db = new double[OscillogramCharting.AllNumConst];
                //    Array.Copy(ex, db, ex.Length);
                //    var Types = d.GetType();//获得类型  
                //    foreach (var item in Types.GetProperties())
                //    {
                //        if (item.Name == e.Property.Name)
                //        {
                //            item.SetValue(d, db);
                //        }
                //    }
                //}
            }
            ((OscillogramCharting)d).ResetPressure();
        }
        #endregion


        private ConcurrentDictionary<OscillogramWave, double[]> X;
        private ConcurrentDictionary<OscillogramWave, double[]> Y;
        #endregion

        #region Axis
        /// <summary>
        /// wave axis
        /// </summary>
        private Axis xAxis = new Axis(0, new Edge());
        /// <summary>
        /// wave axis
        /// </summary>
        private Axis yAixs = new Axis(0, new Edge());
        /// <summary>
        /// gradient axis
        /// </summary>
        private Axis yAixsGradient = new Axis(1, new Edge());
        /// <summary>
        /// speed axis
        /// </summary>
        private Axis yAixsSpeed = new Axis(2, new Edge());
        /// <summary>
        /// pressure axis
        /// </summary>
        private Axis yAixsPressure = new Axis(3, new Edge());
        #endregion

        #region IPlottable
        /// <summary>
        /// wave ScatterPlot
        /// key wave
        /// </summary>
        private ConcurrentDictionary<OscillogramWave, IPlottable> scatterSpectrum = new();
        /// <summary>
        /// gradient ScatterPlot
        /// </summary>
        private IPlottable? scatterGradient = null;
        /// <summary>
        /// realGradient ScatterPlot
        /// </summary>
        private IPlottable? scatterRealGradient = null;

        /// <summary>
        /// speed ScatterPlot
        /// </summary>
        private IPlottable? scatterSpeed = null;
        /// <summary>
        /// realSpeed = ScatterPlot
        /// </summary>
        private IPlottable? scatterRealSpeed = null;

        /// <summary>
        /// pressure = ScatterPlot
        /// </summary>
        private IPlottable? scatterPressure = null;
        #endregion

        static OscillogramCharting()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(OscillogramCharting), new FrameworkPropertyMetadata(typeof(OscillogramCharting)));
        }

        public OscillogramCharting()
        {

            X = new ConcurrentDictionary<OscillogramWave, double[]>();
            Y = new ConcurrentDictionary<OscillogramWave, double[]>();


            _updateDataTimer = new DispatcherTimer();
            _updateDataTimer.Interval = TimeSpan.FromMilliseconds(1); ;
            _updateDataTimer.Tick += UpdateData;
            _updateDataTimer.Start();

            _updateGradientDataTimer = new DispatcherTimer();
            _updateGradientDataTimer.Interval = TimeSpan.FromMilliseconds(1);
            _updateGradientDataTimer.Tick += UpdateGradinetData;
            _updateGradientDataTimer.Start();

            _updateRealGradientDataTimer = new DispatcherTimer();
            _updateRealGradientDataTimer.Interval = TimeSpan.FromMilliseconds(1);
            _updateRealGradientDataTimer.Tick += UpdateRealGradientData;
            _updateRealGradientDataTimer.Start();

            _updateSpeedDataTimer = new DispatcherTimer();
            _updateSpeedDataTimer.Interval = TimeSpan.FromMilliseconds(1);
            _updateSpeedDataTimer.Tick += UpdateSpeedData;
            _updateSpeedDataTimer.Start();

            _updateRealSpeedDataTimer = new DispatcherTimer();
            _updateRealSpeedDataTimer.Interval = TimeSpan.FromMilliseconds(1);
            _updateRealSpeedDataTimer.Tick += UpdateRealSpeedData;
            _updateRealSpeedDataTimer.Start();

            _updatePressureDataTimer = new DispatcherTimer();
            _updatePressureDataTimer.Interval = TimeSpan.FromMilliseconds(1);
            _updatePressureDataTimer.Tick += UpdatePressureData;
            _updatePressureDataTimer.Start();

            // create a timer to update the GUI
            _renderTimer = new DispatcherTimer();
            _renderTimer.Interval = TimeSpan.FromMilliseconds(20);
            _renderTimer.Tick += (sender, e) => Oscill?.AutoRender();
            _renderTimer.Start();

        }

        #region virtual

        public virtual void GradientEvent(ToggleButton toggleButton)
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
        public virtual void RealGradientEvent(ToggleButton toggleButton)
        {
            toggleButton.Checked += (sender, e) =>
            {
                if (!RealGradientShow)
                    RealGradientShow = true;
            };
            toggleButton.Unchecked += (sender, e) =>
            {
                if (RealGradientShow)
                    RealGradientShow = false;
            };
        }
        public virtual void PressureEvent(ToggleButton toggleButton)
        {
            toggleButton.Checked += (sender, e) =>
            {
                if (!PressureShow)
                    PressureShow = true;
            };
            toggleButton.Unchecked += (sender, e) =>
            {
                if (PressureShow)
                    PressureShow = false;
            };
        }

        public virtual void SpeedEvent(ToggleButton toggleButton)
        {
            toggleButton.Checked += (sender, e) =>
            {
                if (!SpeedShow)
                    SpeedShow = true;
            };
            toggleButton.Unchecked += (sender, e) =>
            {
                if (SpeedShow)
                    SpeedShow = false;
            };
        }

        public virtual void RealSpeedEvent(ToggleButton toggleButton)
        {
            toggleButton.Checked += (sender, e) =>
            {
                if (!RealSpeedShow)
                    RealSpeedShow = true;
            };
            toggleButton.Unchecked += (sender, e) =>
            {
                if (RealSpeedShow)
                    RealSpeedShow = false;
            };
        }

        public virtual void GraphEvent(ListView itemsControl)
        {
            itemsControl.PreviewMouseWheel += (sender, e) =>
            {
                var ic = sender as ItemsControl;
                if (ic == null) return;
                var data = e.GetPosition(this);
                var count = e.Delta > 0 ? 2 : -2;
                var scroll = Oscill.FindVisualChildren<ScrollViewer>(ic).FirstOrDefault();
                if (scroll != null)
                {
                    var toHorizontalOffset = (scroll.ExtentWidth / ic.Items.Count) * count + scroll.HorizontalOffset;
                    scroll.ScrollToHorizontalOffset(toHorizontalOffset);
                }
            };
            itemsControl.SelectionChanged += (sender, e) =>
            {
                if (e is SelectionChangedEventArgs arg)
                {
                    foreach (var item in arg.AddedItems)
                    {
                        if (item is OscillogramWave wave)
                        {
                            scatterSpectrum[wave].IsVisible = wave.IsSelected;
                        }
                    }
                    foreach (var item in arg.RemovedItems)
                    {
                        if (item is OscillogramWave wave)
                        {
                            scatterSpectrum[wave].IsVisible = wave.IsSelected;
                        }
                    }
                }
            };
        }
        #endregion
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (GetTemplateChild(OscillogramChartingCoreName) is OscillogramChartingCore oscillogramChartingCore)
            {
                this.Oscill = oscillogramChartingCore;
                Initilize();
            }
            if (GetTemplateChild(OscillogramChartingCore.GradientName) is ToggleButton toggleButton)
                GradientEvent(toggleButton);
            if (GetTemplateChild(OscillogramChartingCore.RealGradientName) is ToggleButton toggleButton1)
                RealGradientEvent(toggleButton1);
            if (GetTemplateChild(OscillogramChartingCore.PressureName) is ToggleButton toggleButton3)
            {
                PressureEvent(toggleButton3);
            }
            if (GetTemplateChild(OscillogramChartingCore.SpeedName) is ToggleButton toggleButton4)
            {
                SpeedEvent(toggleButton4);
            }
            if (GetTemplateChild(OscillogramChartingCore.RealSpeedName) is ToggleButton toggleButton5)
            {
                RealSpeedEvent(toggleButton5);
            }
            if (GetTemplateChild(OscillogramChartingCore.GraphName) is ListView itemsControl)
            {
                GraphEvent(itemsControl);
            }

        }
        private void Initilize()
        {
            ColorShowSource = Oscill.ColorHtmls;

            InitilizePlot();


            ResetSpectrum(new double[] { 254, 255, 269, 259, 255, 254, 254, 255, 255, 255, 255, 255 });
            ResetSpeed();
            ResetRealSpeed();
            ResetPressure();
            ResetGradient();
            ResetRealGradient();
        }

        private void InitilizePlot()
        {
            Oscill.Reset();
            Oscill.Crosshair = Oscill.Plot.AddCrosshair(0, 0);
            Oscill.Plot.XLabel("TimeSpan (min)");
            Oscill.Plot.XAxis.TickLabelFormat(_ => (Math.Round(_ / 60, 1)).ToString());
            Oscill.Plot.YAxis.IsVisible = true;

            xAxis = Oscill.Plot.XAxis;
            yAixs = Oscill.Plot.YAxis;
            yAixs.Label("Spectrum");
            yAixs.LabelStyle(fontSize: 13, rotation: 0);
            yAixs.Color(ColorTranslator.FromHtml("#252526"));
            yAixs.IsVisible = true;
            //yAixs.SetZoomOutLimit(3000);
            //yAixs.SetZoomInLimit(0);

            yAixsGradient = Oscill.Plot.AddAxis(ScottPlot.Renderable.Edge.Left);
            yAixsGradient.Label("Mobile Phase");
            yAixsGradient.LabelStyle(fontSize: 13, rotation: 0);
            yAixsGradient.Color(ColorTranslator.FromHtml("#B060B0"));
            yAixsGradient.IsVisible = true;
            //yAixsGradient.SetSizeLimit(min: 0, max: 110);

            yAixsSpeed = Oscill.Plot.AddAxis(ScottPlot.Renderable.Edge.Right);
            yAixsSpeed.Label("Speed");
            yAixsSpeed.LabelStyle(fontSize: 13, rotation: 0);
            yAixsSpeed.Color(ColorTranslator.FromHtml("#006400"));
            yAixsSpeed.IsVisible = true;
            //yAixsSpeed.SetSizeLimit(min: 0, max: 200);

            yAixsPressure = Oscill.Plot.AddAxis(ScottPlot.Renderable.Edge.Right);
            yAixsPressure.Label("Pressure");
            yAixsPressure.LabelStyle(fontSize: 13, rotation: 0);
            yAixsPressure.Color(ColorTranslator.FromHtml("#94D5F9"));
            yAixsPressure.IsVisible = true;
            //yAixsPressure.SetSizeLimit(min: 0, max: 50);
            //yAixsPressure.SetZoomOutLimit(50);
            //yAixsPressure.SetZoomInLimit(0);

            Oscill.Crosshair.VerticalLine.PositionFormatter = _ => $"{_:f1} s";
            Oscill.Crosshair.LineColor = System.Drawing.Color.Green;

            #region ResetBaseGraph
            var x = new double[180];
            var y = new double[180];
            for (int i = 0; i < 180; i++)
                x[i] = i;
            y[0] = 3000;
            y[59] = -1000;
            //spectrum
            var scatterFlag = Oscill.Plot.AddScatter(x, y, label: null, color: System.Drawing.Color.Transparent);
            scatterFlag.YAxisIndex = yAixs.AxisIndex;
            //speed
            var scatterSpeedFlag = Oscill.Plot.AddScatter(new double[] { 0, 1 }, new double[] { 0, 200 }, label: null, color: System.Drawing.Color.Transparent);
            scatterSpeedFlag.YAxisIndex = yAixsSpeed.AxisIndex;
            //pressure
            var scatterPressureFlag = Oscill.Plot.AddScatter(new double[] { 0, 1 }, new double[] { 0, 200 }, label: null, color: System.Drawing.Color.Transparent);
            scatterPressureFlag.YAxisIndex = yAixsPressure.AxisIndex;
            //gradient
            var scatterGradientFlag1 = Oscill.Plot.AddScatter(new double[] { 0, 1 }, new double[] { 100, 0 }, label: null, color: System.Drawing.Color.Transparent);
            scatterGradientFlag1.YAxisIndex = yAixsGradient.AxisIndex;
            #endregion
        }


        #region reset lines
        /// <summary>
        /// 谱图线
        /// </summary>
        private void ResetSpectrum(params double[] waves)
        {
            Waves.Clear();
            X.Clear();
            Y.Clear();
            var waveIndex = 0;
            foreach (var wave in waves)
            {
                var dscib = $"{wave}";
                var count = X.Keys.Count(_ => _.Wave == wave);
                if (count > 0)
                    dscib = $"{wave}({count})";
                var key = new OscillogramWave(wave) { Color = Oscill.ColorHtmls[waveIndex], Decription = dscib, IsSelected = false };
                Waves.Add(key);
                X.TryAdd(key, new double[AllNumConst]);
                Y.TryAdd(key, new double[AllNumConst]);
                waveIndex++;
            }

            foreach (var currentWave in X.Keys)
            {
                if (scatterSpectrum.Keys.Any(_ => _ == currentWave) && scatterSpectrum[currentWave] != null)
                    Oscill.Plot.Remove(scatterSpectrum[currentWave]);
                var scatter = Oscill.Plot.AddScatter(X[currentWave], Y[currentWave]);
                scatter.YAxisIndex = yAixs.AxisIndex;
                scatter.MaxRenderIndex = LastTimeIndex;
                scatter.MarkerShape = MarkerShape.none;
                //scatter.IsHighlighted = false;
                scatter.Color = ColorTranslator.FromHtml(currentWave.Color!);// ColorHelper.WaveColors[i++ % 28];// System.Drawing.Color.Green;
                scatter.IsVisible = currentWave.IsSelected/* && WaveSource.FirstOrDefault(_ => _.Wave == item.Wave).IsSelected*/;
                scatter.Label = currentWave.Decription;
                scatterSpectrum[currentWave] = scatter;
            }
        }
        /// <summary>
        /// 速度线
        /// </summary>
        private void ResetSpeed()
        {

            if (scatterSpeed != null)
                Oscill.Plot.Remove(scatterSpeed);
            if (SpeedX.Count() != SpeedY.Count())
                return;

            var scatter = new ScatterPlotLimitDraggable(SpeedX, SpeedY)
            {
                DragCursor = ScottPlot.Cursor.Hand,
                DragEnabled = true,
                Label = $"Speed",
                Color = ColorTranslator.FromHtml("#FFAA25"),
                LineWidth = 1,
                YAxisIndex = yAixsSpeed.AxisIndex,
                MaxRenderIndex = SpeedX.Length - 1,
                //IsHighlighted = false,
                //scatter.MarkerShape = MarkerShape.none;
                IsVisible = yAixsSpeed.IsVisible && SpeedShow,

            };
            scatter.Dragged += (sender, e) =>
            {
                var x = e;
            };
            // use a custom function to limit the movement of points
            static Coordinate MoveBetweenAdjacent(List<double> xs, List<double> ys, int index, Coordinate requested)
            {
                int leftIndex = Math.Max(index - 1, 0);
                int rightIndex = Math.Min(index + 1, xs.Count - 1);

                double newX = requested.X;
                if (xs[leftIndex] > xs[rightIndex])
                {

                }
                //newX = Math.Max(newX, xs[leftIndex]);
                //newX = Math.Min(newX, xs[rightIndex]);

                return new Coordinate(newX, requested.Y);
            }
            scatter.MovePointFunc = MoveBetweenAdjacent;
            Oscill.Plot.Add(scatter);
            scatterSpeed = scatter;
        }
        /// <summary>
        /// 实时速度线
        /// </summary>
        private void ResetRealSpeed()
        {
            if (scatterRealSpeed != null)
                Oscill.Plot.Remove(scatterRealSpeed);
            if (SpeedRealX.Count() != SpeedRealY.Count())
                return;
            var scatterReal = Oscill.Plot.AddScatter(SpeedRealX, SpeedRealY, label: null, color: ColorTranslator.FromHtml("#006400"), lineWidth: 1);
            scatterReal.YAxisIndex = yAixsSpeed.AxisIndex;
            scatterReal.MaxRenderIndex = SpeedRealX.Length - 1;
            scatterReal.MarkerShape = MarkerShape.none;
            //scatterReal.IsHighlighted = false;
            scatterReal.IsVisible = yAixsSpeed.IsVisible && RealSpeedShow;
            scatterRealSpeed = scatterReal;
        }
        /// <summary>
        /// 压力线
        /// </summary>
        private void ResetPressure()
        {
            if (this.scatterPressure != null)
                Oscill.Plot.Remove(this.scatterPressure);
            if (PressureX.Count() != PressureY.Count())
                return;
            var scatterPressure = Oscill.Plot.AddScatter(PressureX, PressureY, label: $"Pressure", color: ColorTranslator.FromHtml("#0076F6"), lineWidth: 1);
            scatterPressure.YAxisIndex = yAixsPressure.AxisIndex;
            scatterPressure.MaxRenderIndex = PressureX.Length - 1;
            scatterPressure.MarkerShape = MarkerShape.none;
            //scatterPressure.IsHighlighted = false;
            scatterPressure.IsVisible = yAixsPressure.IsVisible && PressureShow;
            this.scatterPressure = scatterPressure;
        }
        /// <summary>
        /// 梯度线
        /// </summary>
        private void ResetGradient()
        {
            if (this.scatterGradient != null)
                Oscill.Plot.Remove(this.scatterGradient);
            if (GradientX.Count() != GradientY.Count())
                return;
            var scatter = new ScatterPlotLimitDraggable(GradientX, GradientY)
            {
                DragCursor = ScottPlot.Cursor.Hand,
                DragEnabled = true,
                DragEnabledX = true,
                DragEnabledY = true,
                Label = $"mobilephase",
                Color = ColorTranslator.FromHtml("#B060B0"),
                LineWidth = 1,
                YAxisIndex = yAixsGradient.AxisIndex,
                XAxisIndex = xAxis.AxisIndex,
                MaxRenderIndex = GradientX.Length - 1,
                //IsHighlighted = true,
                IsVisible = yAixsGradient.IsVisible && GradientShow,

            };

            scatter.Dragged += (sender, e) =>
            {
                if (sender is ScottPlot.Plottable.ScatterPlotDraggable ps)
                {
                    var xs = ps.Xs;
                    var ys = ps.Ys;
                    var index = ps.CurrentIndex;

                    int leftIndex = Math.Max(index - 1, 0);
                    int rightIndex = Math.Min(index + 1, xs.Count() - 1);
                }

            };
            scatter.MovePointFunc = MoveBetweenAdjacent;
            scatterGradient = scatter;
            Oscill.Plot.Add(scatterGradient);

            // use a custom function to limit the movement of points
            static Coordinate MoveBetweenAdjacent(List<double> xs, List<double> ys, int index, Coordinate requested)
            {
                int leftIndex = Math.Max(index - 1, 0);
                int rightIndex = Math.Min(index + 1, xs.Count - 1);

                double newX = requested.X;
                //newX = Math.Max(newX, xs[leftIndex]);
                //newX = Math.Min(newX, xs[rightIndex]);

                return new Coordinate(newX, requested.Y);
            }

        }
        /// <summary>
        /// 实时梯度线
        /// </summary>
        private void ResetRealGradient()
        {
            if (this.scatterRealGradient != null)
                Oscill.Plot.Remove(this.scatterRealGradient);
            if (GradientRealX.Count() != GradientRealY.Count())
                return;
            var scatterReal = Oscill.Plot.AddScatter(GradientRealX, GradientRealY, label: null, color: ColorTranslator.FromHtml("#FF0000"), lineWidth: 1);
            scatterReal.YAxisIndex = yAixsGradient.AxisIndex;
            scatterReal.MaxRenderIndex = GradientRealX.Length - 1;
            scatterReal.MarkerShape = MarkerShape.none;
            //scatterReal.IsHighlighted = false;
            scatterReal.IsVisible = yAixsGradient.IsVisible && RealGradientShow;
            scatterRealGradient = scatterReal;
        }
        #endregion

        #region update data
        private void UpdateData(object? sender, EventArgs e)
        {
            if (CurrentTimeIndex >= AllNumConst || CurrentTimeIndex > LastTimeIndex)
                return;
            foreach (var key in X.Keys)
            {
                X[key][CurrentTimeIndex] = CurrentTimeIndex;
                Y[key][CurrentTimeIndex] = CurrentTimeIndex * 100;
                (scatterSpectrum[key] as ScatterPlot)!.MaxRenderIndex = CurrentTimeIndex;
            }


        }
        private void UpdatePressureData(object? sender, EventArgs e)
        {
            if (CurrentTimeIndex >= AllNumConst || CurrentTimeIndex > LastTimeIndex)
                return;
            PressureX[CurrentTimeIndex] = CurrentTimeIndex;
            PressureY[CurrentTimeIndex] = CurrentTimeIndex + 25;
            if (scatterPressure != null)
                (scatterPressure as ScatterPlot)!.MaxRenderIndex = CurrentTimeIndex;
        }
        private void UpdateGradinetData(object? sender, EventArgs e)
        {

        }

        private void UpdateRealGradientData(object? sender, EventArgs e)
        {
            if (CurrentTimeIndex >= AllNumConst || CurrentTimeIndex > LastTimeIndex)
                return;
            GradientRealX[CurrentTimeIndex] = CurrentTimeIndex;
            GradientRealY[CurrentTimeIndex] = CurrentTimeIndex + 20;
            if (scatterRealGradient != null)
                (scatterRealGradient as ScatterPlot)!.MaxRenderIndex = CurrentTimeIndex;
        }

        private void UpdateSpeedData(object? sender, EventArgs e)
        {

        }

        private void UpdateRealSpeedData(object? sender, EventArgs e)
        {
            if (CurrentTimeIndex >= AllNumConst || CurrentTimeIndex > LastTimeIndex)
                return;
            SpeedRealX[CurrentTimeIndex] = CurrentTimeIndex;
            SpeedRealY[CurrentTimeIndex] = CurrentTimeIndex + 8;
            if (scatterRealSpeed != null)
                (scatterRealSpeed as ScatterPlot)!.MaxRenderIndex = CurrentTimeIndex;
        }
        #endregion

        public void Dispose()
        {
            throw new NotImplementedException();
        }

    }

}
