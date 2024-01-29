using Charting.Converters;
using Charting.Extensions;
using Charting.Models;

using Microsoft.Win32;

using Newtonsoft.Json;

using ScottPlot;
using ScottPlot.Control;
using ScottPlot.Drawing;
using ScottPlot.Plottable;
using ScottPlot.Renderable;
using ScottPlot.SnapLogic;
using ScottPlot.Ticks.DateTimeTickUnits;

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
using System.Windows.Shapes;
using System.Windows.Threading;

using Cursor = ScottPlot.Cursor;
using Image = System.Windows.Controls.Image;
using Polygon = ScottPlot.Plottable.Polygon;

namespace Charting
{
    [TemplatePart(Name = OscillogramChartingCoreName, Type = typeof(OscillogramChartingCore))]
    [TemplatePart(Name = DragXName, Type = typeof(TextBox))]
    [TemplatePart(Name = DragYName, Type = typeof(TextBox))]
    [TemplatePart(Name = EditName, Type = typeof(RadioButton))]
    public class OscillogramCharting : UserControl, IDisposable
    {
        public const string OscillogramChartingCoreName = "Part_OscillogramChartingCore";
        public const string EditName = "Part_Edit";
        public const string DragXName = "Part_DragX";
        public const string DragYName = "Part_DragY";
        /// <summary>
        /// 总点数  单副图最大点数 
        /// 超过点数将不在刷新新增图
        /// </summary>
        public const int AllNumConst = 86400;
        private const double hitTestStep = 10;


        private System.Windows.Threading.DispatcherTimer _updateDataTimer;
        private System.Windows.Threading.DispatcherTimer _updateGradientDataTimer;
        private System.Windows.Threading.DispatcherTimer _updateRealGradientDataTimer;
        private System.Windows.Threading.DispatcherTimer _updateSpeedDataTimer;
        private System.Windows.Threading.DispatcherTimer _updateRealSpeedDataTimer;
        private System.Windows.Threading.DispatcherTimer _updatePressureDataTimer;
        private System.Windows.Threading.DispatcherTimer _renderTimer;

        private OscillogramChartingCore Oscill;

        #region dp

        #region Configuration settings


        public System.Windows.Media.Brush ForeGround
        {
            get { return (System.Windows.Media.Brush)GetValue(ForeGroundProperty); }
            set { SetValue(ForeGroundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ForeGround.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ForeGroundProperty =
            DependencyProperty.Register("ForeGround", typeof(System.Windows.Media.Brush), typeof(OscillogramCharting), new PropertyMetadata(default));



        public System.Windows.Media.Brush BackGround
        {
            get { return (System.Windows.Media.Brush)GetValue(BackGroundProperty); }
            set { SetValue(BackGroundProperty, value); }
        }

        // Using a DependencyProperty as the backing store for BackGround.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty BackGroundProperty =
            DependencyProperty.Register("BackGround", typeof(System.Windows.Media.Brush), typeof(OscillogramCharting), new PropertyMetadata(default));




        public ThemesStyle Theme
        {
            get { return (ThemesStyle)GetValue(ThemeProperty); }
            set { SetValue(ThemeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Theme.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ThemeProperty =
            DependencyProperty.Register("Theme", typeof(ThemesStyle), typeof(OscillogramCharting), new PropertyMetadata(ThemesStyle.Default, (d, e) =>
           {
               if (((OscillogramCharting)d).Oscill != null)
                   ((OscillogramCharting)d).Oscill.Theme = (ThemesStyle)e.NewValue;

               var theme = ((ThemesStyle)e.NewValue).ToString();
               var style = ScottPlot.Style.GetStyles().Where(_ => _.GetType().Name == theme).FirstOrDefault() ?? ScottPlot.Style.Default;

               var mColor = System.Windows.Media.Color.FromArgb(style.FigureBackgroundColor.A, style.FigureBackgroundColor.R, style.FigureBackgroundColor.G, style.FigureBackgroundColor.B);
               var solidBrush = new SolidColorBrush(mColor);
               //((OscillogramCharting)d).BackGround = solidBrush;


               var mFColor = System.Windows.Media.Color.FromArgb(style.TitleFontColor.A, style.TitleFontColor.R, style.TitleFontColor.G, style.TitleFontColor.B);
               var solidBrushM = new SolidColorBrush(mFColor);
               //((OscillogramCharting)d).BackGround = solidBrushM;


               var btnForegroundColor = System.Windows.Media.Color.FromArgb(style.TitleFontColor.A, style.TitleFontColor.R, style.TickLabelColor.G, style.TickLabelColor.B);;
               var btnForegroundBrush = new SolidColorBrush(btnForegroundColor);
               //((OscillogramCharting)d).BackGround = solidBrushM;


               Application.Current.Resources["ToolBar_Background"] = solidBrush;

               Application.Current.Resources["ToolBar_Foreground"] = solidBrushM;

               Application.Current.Resources["ToggleBtnForeground"] = btnForegroundBrush;

               var r=ToggleButtonHelper.DefaultForeGroundProperty;

           }));


        public double ConfigurationSpeedMax
        {
            get { return (double)GetValue(ConfigurationSpeedMaxProperty); }
            set { SetValue(ConfigurationSpeedMaxProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ConfigurationSpeedMax.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConfigurationSpeedMaxProperty =
            DependencyProperty.Register("ConfigurationSpeedMax", typeof(double), typeof(OscillogramCharting), new PropertyMetadata(50d));


        public double ConfigurationSpeedMin
        {
            get { return (double)GetValue(ConfigurationSpeedMinProperty); }
            set { SetValue(ConfigurationSpeedMinProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ConfigurationSpeedＭin.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConfigurationSpeedMinProperty =
            DependencyProperty.Register("ConfigurationSpeedMin", typeof(double), typeof(OscillogramCharting), new PropertyMetadata(0d));


        public double ConfigurationGradientMin
        {
            get { return (double)GetValue(ConfigurationGradientMinProperty); }
            set { SetValue(ConfigurationGradientMinProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ConfigurationGradientMin.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConfigurationGradientMinProperty =
            DependencyProperty.Register("ConfigurationGradientMin", typeof(double), typeof(OscillogramCharting), new PropertyMetadata(0d));



        public double ConfigurationGradientMax
        {
            get { return (double)GetValue(ConfigurationGradientMaxProperty); }
            set { SetValue(ConfigurationGradientMaxProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ConfigurationGradientMax.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ConfigurationGradientMaxProperty =
            DependencyProperty.Register("ConfigurationGradientMax", typeof(double), typeof(OscillogramCharting), new PropertyMetadata(0d));
        #endregion

        #region base data


        public bool CanDraggable
        {
            get { return (bool)GetValue(CanDraggableProperty); }
            set { SetValue(CanDraggableProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CanDraggable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CanDraggableProperty =
            DependencyProperty.Register("CanDraggable", typeof(bool), typeof(OscillogramCharting), new PropertyMetadata(false, (sender, e) =>
            {
                if (!(bool)e.NewValue)
                {
                    ((OscillogramCharting)sender).EnableEditDrag = false;
                }
            }));


        public bool AutoRightAdjustEnable
        {
            get { return (bool)GetValue(AutoRightAdjustEnableProperty); }
            set { SetValue(AutoRightAdjustEnableProperty, value); }
        }

        // Using a DependencyProperty as the backing store for AutoRightAdjustEnable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty AutoRightAdjustEnableProperty =
            DependencyProperty.Register("AutoRightAdjustEnable", typeof(bool), typeof(OscillogramCharting), new PropertyMetadata(false));

        public bool IntervalAdjustEnable
        {
            get { return (bool)GetValue(IntervalAdjustEnableProperty); }
            set { SetValue(IntervalAdjustEnableProperty, value); }
        }

        // Using a DependencyProperty as the backing store for IntervalAdjustEnable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IntervalAdjustEnableProperty =
            DependencyProperty.Register("IntervalAdjustEnable", typeof(bool), typeof(OscillogramCharting), new PropertyMetadata(false));


        public GraphType DragType
        {
            get { return (GraphType)GetValue(DragTypeProperty); }
            set { SetValue(DragTypeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DragType.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DragTypeProperty =
            DependencyProperty.Register("DragType", typeof(GraphType), typeof(OscillogramCharting), new PropertyMetadata(GraphType.GraphType, OnDragTypeChanged));

        private static void OnDragTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
        }

        public double DragX
        {
            get { return (double)GetValue(DragXProperty); }
            set { SetValue(DragXProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DragX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DragXProperty =
            DependencyProperty.Register("DragX", typeof(double), typeof(OscillogramCharting), new PropertyMetadata(0d, OnDragXChanged));

        private static void OnDragXChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dr = ((OscillogramCharting)d).Oscill.CurrentDraggableGraph.CurrentDraggableGraph;
            if (dr != null && dr is OscillogramDraggable tb)
            {
                int leftIndex = Math.Max(tb.CurrentIndex - 1, 0);
                int rightIndex = Math.Min(tb.CurrentIndex + 1, tb.Xs.Length - 1);
                double newX = (double)e.NewValue;
                newX = Math.Max(newX, tb.Xs[leftIndex]);
                newX = Math.Min(newX, tb.Xs[rightIndex]);
                tb.Xs[tb.CurrentIndex] = newX;
            }
        }

        public double DragY
        {
            get { return (double)GetValue(DragYProperty); }
            set { SetValue(DragYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DragY.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DragYProperty =
            DependencyProperty.Register("DragY", typeof(double), typeof(OscillogramCharting), new PropertyMetadata(0d, OnDragYChanged));

        private static void OnDragYChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dr = ((OscillogramCharting)d).Oscill.CurrentDraggableGraph.CurrentDraggableGraph;
            if (dr != null && dr is OscillogramDraggable tb)
            {
                tb.Ys[tb.CurrentIndex] = (double)e.NewValue;
            }
        }

        public bool EnableEditDrag
        {
            get { return (bool)GetValue(EnableEditDragProperty); }
            set { SetValue(EnableEditDragProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EnableEditDrag.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnableEditDragProperty =
            DependencyProperty.Register("EnableEditDrag", typeof(bool), typeof(OscillogramCharting), new PropertyMetadata(false, OnEnableEditDrag));
        private static void EnableEditDragEv(OscillogramCharting oscillogramCharting, bool valueEv)
        {
            var settings = oscillogramCharting.Oscill.Plot.GetSettings();
            if (!valueEv)
            {
                //((OscillogramCharting)d).scatterrEndTimeLine.IsVisible = false;
                //((OscillogramCharting)d).scatterrEndTimeLine.DragEnabled = false;

                oscillogramCharting.Oscill.DefaultMenus();

                IDraggable[] enabledDraggables = settings.Plottables
                            .Where(x => x is IDraggable)
                            .Select(x => (IDraggable)x)
                            .Where(x => x.DragEnabled)
                            .Where(x => x is IPlottable p && p.IsVisible)
                            .ToArray();
                foreach (var dr in enabledDraggables)
                {
                    if (dr is ScatterPlot Item)
                        Item.MarkerShape = valueEv ? MarkerShape.filledCircle : MarkerShape.none;
                    if (dr is IGraphType graphType1 && graphType1.GraphType == GraphType.EndClockLine && dr is OscillogramVLine l)
                        l.LineWidth = 1;
                    dr.DragEnabled = valueEv;
                }
                oscillogramCharting.DragType = GraphType.GraphType;
                oscillogramCharting.Oscill.CurrentDraggableGraph.DraggableGraph.Clear();
                oscillogramCharting.Oscill.CurrentDraggableGraph.CurrentDraggableGraph = null!;
                oscillogramCharting.Oscill.CurrentDraggableGraph.CurrentDraggableGraphType = GraphType.GraphType;

            }
            else
            {
                //((OscillogramCharting)d).scatterrEndTimeLine.DragEnabled = false;
                oscillogramCharting.OscillMenus();
                if (!oscillogramCharting.Oscill.CurrentDraggableGraph.HasDraggable)
                {
                    IDraggable[] enabledDraggables = settings.Plottables
                             .Where(x => x is IDraggable)
                             .Select(x => (IDraggable)x)
                             //.Where(x => x.DragEnabled)
                             .Where(x => x is IPlottable p && p.IsVisible)
                             .ToArray();
                    foreach (var dr in enabledDraggables)
                    {
                        if (dr is ScatterPlot Item)
                            Item.MarkerShape = valueEv ? MarkerShape.filledCircle : MarkerShape.none;
                        dr.DragEnabled = valueEv;
                        if (dr is IGraphType graphType1 && graphType1.GraphType == GraphType.EndClockLine && dr is OscillogramVLine l)
                        {
                            l.LineWidth = 3;
                        }
                        if (!oscillogramCharting.Oscill.CurrentDraggableGraph.DraggableGraph.Contains(dr))
                        {
                            oscillogramCharting.Oscill.CurrentDraggableGraph.DraggableGraph.Add(dr);
                        }
                        if (oscillogramCharting.Oscill.CurrentDraggableGraph.CurrentDraggableGraph != dr)
                        {
                            oscillogramCharting.Oscill.CurrentDraggableGraph.CurrentDraggableGraph = dr!;
                            if (dr is IGraphType graphType)
                                oscillogramCharting.Oscill.CurrentDraggableGraph.CurrentDraggableGraphType = graphType.GraphType;
                            else
                                oscillogramCharting.Oscill.CurrentDraggableGraph.CurrentDraggableGraphType = GraphType.GraphType;
                        }
                    }

                }
                oscillogramCharting.DragType = oscillogramCharting.Oscill.CurrentDraggableGraph.CurrentDraggableGraphType;
            }

        }
        private static void OnEnableEditDrag(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            EnableEditDragEv(((OscillogramCharting)d), (bool)e.NewValue);
        }
        private void OscillMenus()
        {
            var cm = new ContextMenu();

            MenuItem SaveImageMenuItem = new() { Header = "Add", Name = "Drag_Add" };
            SaveImageMenuItem.Click += (sender, e) =>
            {
                var pixelX = Mouse.GetPosition(this).X;
                var pixelY = Mouse.GetPosition(this).Y;

                if (DragType == GraphType.Gradient)
                {
                    (double coordinateX, double coordinateY) = Oscill.GetMouseCoordinates(0, scatterGradient?.YAxisIndex ?? 0);
                    bool test = false;
                    var cIndex = 0;
                    for (int i = 0; i < GradientX.Length - 1; i++)
                    {
                        if (coordinateX - GradientX[i] >= 0 && GradientX[i + 1] > coordinateX)
                        {
                            cIndex = i;
                            test = true;
                            break;
                        }
                    }
                    if (test)
                    {
                        var newArryX = new double[GradientX.Length + 1];
                        var newArryY = new double[GradientX.Length + 1];
                        Array.Copy(GradientX, newArryX, cIndex + 1);
                        newArryX[cIndex + 1] = coordinateX;
                        Array.Copy(GradientX, cIndex + 1, newArryX, cIndex + 2, GradientX.Length - cIndex - 1);

                        Array.Copy(GradientY, newArryY, cIndex + 1);
                        newArryY[cIndex + 1] = coordinateY;
                        Array.Copy(GradientY, cIndex + 1, newArryY, cIndex + 2, GradientY.Length - cIndex - 1);

                        GradientX = newArryX;
                        GradientY = newArryY;
                        ResetGradient();

                    }

                }

                if (DragType == GraphType.Speed)
                {
                    (double coordinateX, double coordinateY) = Oscill.GetMouseCoordinates(0, scatterSpeed?.YAxisIndex ?? 0);
                    bool test = false;
                    var cIndex = 0;
                    for (int i = 0; i < SpeedX.Length - 1; i++)
                    {
                        if (coordinateX - SpeedX[i] >= 0 && SpeedX[i + 1] > coordinateX)
                        {
                            cIndex = i;
                            test = true;
                            break;
                        }
                    }
                    if (test)
                    {
                        var newArryX = new double[SpeedX.Length + 1];
                        var newArryY = new double[SpeedX.Length + 1];
                        Array.Copy(SpeedX, newArryX, cIndex + 1);
                        newArryX[cIndex + 1] = coordinateX;
                        Array.Copy(SpeedX, cIndex + 1, newArryX, cIndex + 2, SpeedX.Length - cIndex - 1);

                        Array.Copy(SpeedY, newArryY, cIndex + 1);
                        newArryY[cIndex + 1] = coordinateY;
                        Array.Copy(SpeedY, cIndex + 1, newArryY, cIndex + 2, SpeedY.Length - cIndex - 1);

                        SpeedX = newArryX;
                        SpeedY = newArryY;
                        ResetSpeed();

                    }

                }

            };
            cm.Items.Add(SaveImageMenuItem);

            MenuItem CopyImageMenuItem = new()
            {
                Header = "Remove",
                Name = "Drag_Remove"
                //IsEnabled = GraphType.Gradient == DragType || GraphType.Speed == DragType ? true : false
            };


            CopyImageMenuItem.Click += (sender, e) =>
            {

                var scatter = GraphType.Gradient == DragType ? scatterGradient : GraphType.Speed == DragType ? scatterSpeed : null;
                var xs = GraphType.Gradient == DragType ? GradientX : GraphType.Speed == DragType ? SpeedX : null;
                var ys = GraphType.Gradient == DragType ? GradientY : GraphType.Speed == DragType ? GradientY : null;
                if (scatter != null && scatter is OscillogramDraggable dr)
                {
                    (double coordinateX, double coordinateY) = Oscill.GetMouseCoordinates(0, scatter.YAxisIndex);
                    var cIndex = dr.CurrentIndex + 1;
                    var newArryX = new double[xs.Length - 1];
                    var newArryY = new double[xs.Length - 1];

                    if (GraphType.Gradient == DragType)
                    {
                        Array.Copy(GradientX, newArryX, cIndex - 1);
                        Array.Copy(GradientX, cIndex, newArryX, cIndex - 1, GradientX.Length - cIndex);

                        Array.Copy(GradientY, newArryY, cIndex - 1);
                        Array.Copy(GradientY, cIndex, newArryY, cIndex - 1, GradientY.Length - cIndex);

                        GradientX = newArryX;
                        GradientY = newArryY;
                        ResetGradient();
                    }
                    if (GraphType.Speed == DragType)
                    {
                        Array.Copy(SpeedX, newArryX, cIndex - 1);
                        Array.Copy(SpeedX, cIndex, newArryX, cIndex - 1, SpeedX.Length - cIndex);

                        Array.Copy(SpeedY, newArryY, cIndex - 1);
                        Array.Copy(SpeedY, cIndex, newArryY, cIndex - 1, SpeedY.Length - cIndex);

                        SpeedX = newArryX;
                        SpeedY = newArryY;
                        ResetSpeed();
                    }
                }
            };
            cm.Items.Add(CopyImageMenuItem);

            MenuItem AutoAxisMenuItem = new() { Header = "Zoom to Fit Data" };
            AutoAxisMenuItem.Click += (sender, e) =>
            {
                Oscill.AutoZoom = true;
                Oscill.Plot.AxisAuto();
                Oscill.Refresh(true);
            };
            cm.Items.Add(AutoAxisMenuItem);

            //MenuItem HelpMenuItem = new() { Header = "Help" };
            //HelpMenuItem.Click += RightClickMenu_Help_Click;
            //cm.Items.Add(HelpMenuItem);

            //MenuItem OpenInNewWindowMenuItem = new() { Header = "Open in New Window" };
            //OpenInNewWindowMenuItem.Click += RightClickMenu_OpenInNewWindow_Click;
            //cm.Items.Add(OpenInNewWindowMenuItem);

            //cm.IsOpen = true;

            Oscill.Menus = cm!;
        }
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
            DependencyProperty.Register("CurrentTimeIndex", typeof(int), typeof(OscillogramCharting), new PropertyMetadata(0, (d, e) =>
            {
                if (((OscillogramCharting)d).scatterCurrentTimeLine != null)
                    ((OscillogramCharting)d).scatterCurrentTimeLine!.X = (int)e.NewValue;
            }));

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
            //((OscillogramCharting)d).Initilize();
            ((OscillogramCharting)d).ResetSpeed();
            ((OscillogramCharting)d).ResetRealSpeed();
            ((OscillogramCharting)d).ResetGradient();
            ((OscillogramCharting)d).ResetRealGradient();
            ((OscillogramCharting)d).ResetPressure();
            ((OscillogramCharting)d).ResetEndClockLine();
            ((OscillogramCharting)d).ResetCurrentLine();
            ((OscillogramCharting)d).ResetSpectrum();

            ((OscillogramCharting)d).ResetPeaks();
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

        public ObservableCollection<OscillogramWave> Waves
        {
            get { return (ObservableCollection<OscillogramWave>)GetValue(WavesProperty); }
            set { SetValue(WavesProperty, value); }
        }
        // Using a DependencyProperty as the backing store for Waves.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty WavesProperty =
            DependencyProperty.Register("Waves", typeof(ObservableCollection<OscillogramWave>), typeof(OscillogramCharting), new PropertyMetadata(new ObservableCollection<OscillogramWave>()));
        public List<string> ColorShowSource
        {
            get { return (List<string>)GetValue(ColorShowSourceProperty); }
            set { SetValue(ColorShowSourceProperty, value); }
        }
        // Using a DependencyProperty as the backing store for ColorShowSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ColorShowSourceProperty =
            DependencyProperty.Register("ColorShowSource", typeof(List<string>), typeof(OscillogramCharting), new PropertyMetadata(new List<string>()));
        #region Show DP
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
                charting.yAixsGradient.IsVisible = (bool)e.NewValue || charting.GradientShow;


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
                charting.yAixsGradient.IsVisible = (bool)e.NewValue || charting.RealGradientShow;

                var settings = ((OscillogramCharting)d).Oscill.Plot.GetSettings();
                IDraggable[] enabledDraggables = settings.Plottables
                               .Where(x => x is IDraggable)
                               .Select(x => (IDraggable)x)
                               //.Where(x => x.DragEnabled)
                               .Where(x => x is IGraphType graph && graph.GraphType == GraphType.Gradient)
                               .Where(x => x is IPlottable p/* && p.IsVisible*/)
                               .ToArray();
                foreach (var dr in enabledDraggables)
                {
                    if (dr is ScatterPlot Item)
                    {
                        Item.IsVisible = (bool)e.NewValue;
                    }
                    if (!(bool)e.NewValue)
                    {
                        if (((OscillogramCharting)d).Oscill.CurrentDraggableGraph.CurrentDraggableGraphType == GraphType.Gradient)
                        {
                            ((OscillogramCharting)d).Oscill.CurrentDraggableGraph.CurrentDraggableGraph = null!;
                            ((OscillogramCharting)d).Oscill.CurrentDraggableGraph.CurrentDraggableGraphType = GraphType.GraphType;
                            if (((OscillogramCharting)d).Oscill.CurrentDraggableGraph.DraggableGraph.Contains(dr))
                            {
                                ((OscillogramCharting)d).Oscill.CurrentDraggableGraph.DraggableGraph.Remove(dr);
                            }
                        }
                        if (charting.DragType == GraphType.Gradient)
                        {
                            charting.DragType = GraphType.GraphType;
                        }
                    }
                }


                charting.EnableEditDrag = settings.Plottables
                               .Where(x => x is IDraggable)
                               .Select(x => (IDraggable)x)
                               .Where(x => x.DragEnabled)
                               //.Where(x => x is IGraphType graph && graph.GraphType == GraphType.Gradient)
                               .Any(x => x is IPlottable p && p.IsVisible);


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
                charting.yAixsSpeed.IsVisible = (bool)e.NewValue || charting.RealSpeedShow;


                var settings = ((OscillogramCharting)d).Oscill.Plot.GetSettings();
                IDraggable[] enabledDraggables = settings.Plottables
                               .Where(x => x is IDraggable)
                               .Select(x => (IDraggable)x)
                               //.Where(x => x.DragEnabled)
                               .Where(x => x is IGraphType graph && graph.GraphType == GraphType.Speed)
                               .Where(x => x is IPlottable p/* && p.IsVisible*/)
                               .ToArray();
                foreach (var dr in enabledDraggables)
                {
                    if (dr is ScatterPlot Item)
                    {
                        Item.IsVisible = (bool)e.NewValue;
                    }
                    if (!(bool)e.NewValue)
                    {
                        if (((OscillogramCharting)d).Oscill.CurrentDraggableGraph.CurrentDraggableGraphType == GraphType.Speed)
                        {
                            ((OscillogramCharting)d).Oscill.CurrentDraggableGraph.CurrentDraggableGraph = null!;
                            ((OscillogramCharting)d).Oscill.CurrentDraggableGraph.CurrentDraggableGraphType = GraphType.GraphType;
                            if (((OscillogramCharting)d).Oscill.CurrentDraggableGraph.DraggableGraph.Contains(dr))
                            {
                                ((OscillogramCharting)d).Oscill.CurrentDraggableGraph.DraggableGraph.Remove(dr);
                            }
                        }
                        if (charting.DragType == GraphType.Speed)
                        {
                            charting.DragType = GraphType.GraphType;
                        }
                    }
                }

                charting.EnableEditDrag = settings.Plottables
                               .Where(x => x is IDraggable)
                               .Select(x => (IDraggable)x)
                               .Where(x => x.DragEnabled)
                               //.Where(x => x is IGraphType graph && graph.GraphType == GraphType.Gradient)
                               .Any(x => x is IPlottable p && p.IsVisible);
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
                charting.yAixsSpeed.IsVisible = (bool)e.NewValue || charting.SpeedShow;
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
                charting.yAixsPressure.IsVisible = (bool)e.NewValue;
            }
        }

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
            DependencyProperty.Register("SpeedRealX", typeof(double[]), typeof(OscillogramCharting), new PropertyMetadata(new double[OscillogramCharting.AllNumConst], OnSpeedRealChanged));

        public double[] SpeedRealY
        {
            get { return (double[])GetValue(SpeedRealYProperty); }
            set { SetValue(SpeedRealYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SpeedRealX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SpeedRealYProperty =
            DependencyProperty.Register("SpeedRealY", typeof(double[]), typeof(OscillogramCharting), new PropertyMetadata(new double[OscillogramCharting.AllNumConst], OnSpeedRealChanged));

        private static void OnSpeedRealChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

            if (e.NewValue is double[] ex)
            {
                if (ex.Length < OscillogramCharting.AllNumConst)
                {
                    throw new ArgumentException("The array length does not match 'OscillogramCharting.AllNumConst' ");
                }
            }
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
            DependencyProperty.Register("GradientRealX", typeof(double[]), typeof(OscillogramCharting), new PropertyMetadata(new double[OscillogramCharting.AllNumConst], OnGradientRealChanged));

        public double[] GradientRealY
        {
            get { return (double[])GetValue(GradientRealYProperty); }
            set { SetValue(GradientRealYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SpeedRealX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty GradientRealYProperty =
            DependencyProperty.Register("GradientRealY", typeof(double[]), typeof(OscillogramCharting), new PropertyMetadata(new double[OscillogramCharting.AllNumConst], OnGradientRealChanged));

        private static void OnGradientRealChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is double[] ex)
            {
                if (ex.Length < OscillogramCharting.AllNumConst)
                {
                    throw new ArgumentException("The array length does not match 'OscillogramCharting.AllNumConst' ");
                }
            }
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
            DependencyProperty.Register("PressureX", typeof(double[]), typeof(OscillogramCharting), new PropertyMetadata(new double[OscillogramCharting.AllNumConst], OnPressureChanged));

        public double[] PressureY
        {
            get { return (double[])GetValue(PressureYProperty); }
            set { SetValue(PressureYProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SpeedRealX.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PressureYProperty =
            DependencyProperty.Register("PressureY", typeof(double[]), typeof(OscillogramCharting), new PropertyMetadata(new double[OscillogramCharting.AllNumConst], OnPressureChanged));

        private static void OnPressureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is double[] ex)
            {
                if (ex.Length < OscillogramCharting.AllNumConst)
                {
                    throw new ArgumentException("The array length does not match 'OscillogramCharting.AllNumConst' ");
                }
            }
            ((OscillogramCharting)d).ResetPressure();
        }
        #endregion

        #region XY Real

        public ObservableCollection<OscillogramWave> Xs
        {
            get { return (ObservableCollection<OscillogramWave>)GetValue(XsProperty); }
            set { SetValue(XsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Xs.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty XsProperty =
            DependencyProperty.Register("Xs", typeof(ObservableCollection<OscillogramWave>), typeof(OscillogramCharting), new PropertyMetadata(new ObservableCollection<OscillogramWave>(), OnOscillogramChanged));



        public ObservableCollection<OscillogramWave> Ys
        {
            get { return (ObservableCollection<OscillogramWave>)GetValue(YsProperty); }
            set { SetValue(YsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Ys.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty YsProperty =
            DependencyProperty.Register("Ys", typeof(ObservableCollection<OscillogramWave>), typeof(OscillogramCharting), new PropertyMetadata(new ObservableCollection<OscillogramWave>(), OnOscillogramChanged));


        private static void OnOscillogramChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is ObservableCollection<OscillogramWave> ex)
            {
                ex.CollectionChanged += (object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
                    =>
                {
                    if (e.NewItems != null && e.NewItems.OfType<OscillogramWave>().Any(_ => _.V.Count() < OscillogramCharting.AllNumConst))
                    {
                        throw new ArgumentException("The array length does not match 'OscillogramCharting.AllNumConst' ");
                    }
                    ((OscillogramCharting)d).ResetSpectrum();
                };
            }
            ((OscillogramCharting)d).ResetSpectrum();
        }

        #endregion

        #endregion

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
        /// <summary>
        /// EndTimeLine =VLine
        /// </summary>
        private OscillogramVLine? scatterrEndTimeLine = null!;
        private VLine? scatterCurrentTimeLine = null!;

        private ConcurrentDictionary<OscillogramWave, Polygon[]> scatterPolygons = new();
        #endregion

        #region Ctor
        static OscillogramCharting()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(OscillogramCharting), new FrameworkPropertyMetadata(typeof(OscillogramCharting)));
        }

        public OscillogramCharting()
        {

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
            _renderTimer.Tick += (sender, e) => Oscill?.AutoRender(Oscill.isHighRefresh);
            _renderTimer.Start();



        }
        #endregion

        #region OnApplyTemplate
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            if (GetTemplateChild(OscillogramChartingCoreName) is OscillogramChartingCore oscillogramChartingCore)
            {
                this.Oscill = oscillogramChartingCore;
                Initilize();
            }
            if (GetTemplateChild(OscillogramChartingCore.GradientName) is ToggleButton toggleButton)
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
            if (GetTemplateChild(OscillogramChartingCore.RealGradientName) is ToggleButton toggleButton1)
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
            if (GetTemplateChild(OscillogramChartingCore.PressureName) is ToggleButton toggleButton3)
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
            if (GetTemplateChild(OscillogramChartingCore.SpeedName) is ToggleButton toggleButton4)
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
            if (GetTemplateChild(OscillogramChartingCore.RealSpeedName) is ToggleButton toggleButton5)
            {
                toggleButton5.Checked += (sender, e) =>
                {
                    if (!RealSpeedShow)
                        RealSpeedShow = true;
                };
                toggleButton5.Unchecked += (sender, e) =>
                {
                    if (RealSpeedShow)
                        RealSpeedShow = false;
                };
            }
            if (GetTemplateChild(EditName) is ToggleButton tb)
            {
                tb.Checked += (sender, e) =>
                {
                    if (!EnableEditDrag)
                        EnableEditDrag = true;
                };
                tb.Unchecked += (sender, e) =>
                {
                    if (EnableEditDrag)
                        EnableEditDrag = false;
                };
            }
            if (GetTemplateChild(OscillogramChartingCore.GraphName) is ListView itemsControl)
            {
                itemsControl.PreviewMouseWheel += (sender, e) =>
                {
                    var ic = sender as ItemsControl;
                    if (ic == null) return;
                    var data = e.GetPosition(this);
                    var count = e.Delta > 0 ? 2 : -2;
                    var scroll = ic.FindVisualChildren<ScrollViewer>().FirstOrDefault();
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
                                if (scatterSpectrum.Keys.Contains(wave))
                                {
                                    scatterSpectrum[wave].IsVisible = wave.IsSelected;
                                }
                            }
                        }
                        foreach (var item in arg.RemovedItems)
                        {
                            if (item is OscillogramWave wave)
                            {
                                if (scatterSpectrum.Keys.Contains(wave))
                                {
                                    scatterSpectrum[wave].IsVisible = wave.IsSelected;
                                }
                            }
                        }
                    }
                };
            }
        }
        private ContextMenu ContextMenuPreviousExcute(ContextMenu contextMenu)
        {
            foreach (MenuItem menuItem in contextMenu.Items)
            {
                if (menuItem.Name == "Drag_Remove" || menuItem.Name == "Drag_Add")
                {
                    menuItem.IsEnabled = GraphType.Gradient == DragType || GraphType.Speed == DragType ? true : false;

                    var scatter = GraphType.Gradient == DragType ? scatterGradient : GraphType.Speed == DragType ? scatterSpeed : null;
                    var xs = GraphType.Gradient == DragType ? GradientX : GraphType.Speed == DragType ? SpeedX : null;
                    var ys = GraphType.Gradient == DragType ? GradientY : GraphType.Speed == DragType ? SpeedY : null;
                    if (scatter != null && menuItem.Name == "Drag_Remove")
                    {
                        (double coordinateX, double coordinateY) = Oscill.GetMouseCoordinates(0, scatter.YAxisIndex);
                        bool test = false;
                        var cIndex = 0;
                        for (int i = 0; i < xs.Length - 1; i++)
                        {
                            test = Math.Abs(ys[i] - coordinateY) <= hitTestStep && Math.Abs(xs[i] - coordinateX) <= hitTestStep;
                            if (test)
                            {
                                cIndex = i;
                                break;
                            }
                        }
                        menuItem.IsEnabled = test;
                        if (test && scatter is IDraggable dr && scatter is OscillogramDraggable plotLimitDraggable)
                        {
                            if (!Oscill.CurrentDraggableGraph.DraggableGraph.Contains(dr))
                            {
                                Oscill.CurrentDraggableGraph.DraggableGraph.Add(dr);
                            }
                            if (Oscill.CurrentDraggableGraph.CurrentDraggableGraph != dr)
                            {
                                Oscill.CurrentDraggableGraph.CurrentDraggableGraph = dr!;
                                if (dr is IGraphType graphType)
                                    Oscill.CurrentDraggableGraph.CurrentDraggableGraphType = graphType.GraphType;
                                else
                                    Oscill.CurrentDraggableGraph.CurrentDraggableGraphType = GraphType.GraphType;
                            }
                        }
                    }
                }
            }
            return contextMenu;
        }
        private void Initilize()
        {
            ColorShowSource = OscillogramExtension.ColorHtmls;
            Oscill.ContextMenuPreviousExcute = ContextMenuPreviousExcute;
            Oscill.Reset();

            Oscill.Theme = Theme;
            Oscill.Crosshair = Oscill.Plot.AddCrosshair(0, 0);
            Oscill.Plot.XLabel("TimeSpan (max)");
            Oscill.Plot.XAxis.TickLabelFormat(_ => (Math.Round(_ / 60, 1)).ToString());
            Oscill.Plot.YAxis.IsVisible = true;
            Oscill.Plot.Grid(false);

            xAxis = Oscill.Plot.XAxis;
            yAixs = Oscill.Plot.YAxis;
            yAixs.Label("Spectrum");
            yAixs.LabelStyle(fontSize: 13, rotation: 0);
            //yAixs.Color(ColorTranslator.FromHtml("#252526"));
            yAixs.IsVisible = true;
            //yAixs.SetZoomOutLimit(3000);
            //yAixs.SetZoomInLimit(0);

            yAixsGradient = Oscill.Plot.AddAxis(ScottPlot.Renderable.Edge.Left);
            yAixsGradient.Label("Mobile Phase");
            yAixsGradient.LabelStyle(fontSize: 13, rotation: 0);
            yAixsGradient.Color(ColorTranslator.FromHtml("#B060B0"));
            yAixsGradient.IsVisible = GradientShow || RealGradientShow;
            //yAixsGradient.SetSizeLimit(max: 0, max: 110);

            yAixsSpeed = Oscill.Plot.AddAxis(ScottPlot.Renderable.Edge.Right);
            yAixsSpeed.Label("Speed");
            yAixsSpeed.LabelStyle(fontSize: 13, rotation: 0);
            yAixsSpeed.Color(ColorTranslator.FromHtml("#006400"));
            yAixsSpeed.IsVisible = SpeedShow || RealSpeedShow;
            //yAixsSpeed.SetSizeLimit(max: 0, max: 200);

            yAixsPressure = Oscill.Plot.AddAxis(ScottPlot.Renderable.Edge.Right);
            yAixsPressure.Label("Pressure");
            yAixsPressure.LabelStyle(fontSize: 13, rotation: 0);
            yAixsPressure.Color(ColorTranslator.FromHtml("#0076F6"));
            yAixsPressure.IsVisible = PressureShow;
            //yAixsPressure.SetSizeLimit(max: 0, max: 50);
            //yAixsPressure.SetZoomOutLimit(50);
            //yAixsPressure.SetZoomInLimit(0);

            Oscill.Crosshair.VerticalLine.PositionFormatter = _ => $"{_:f1} s";
            Oscill.Crosshair.LineColor = System.Drawing.Color.Green;
      

            //Oscill.RightClicked -= Oscill.DefaultRightClickEvent!;
            //Oscill.RightClicked += Oscill_RightClicked;

            Oscill.DraggableUpdatedHandler += (sneder, e) =>
            {
                if (!CanDraggable)
                {
                    EnableEditDragEv(this, false);
                    return;
                }
                DragType = e.CurrentDraggableGraphType;
                if (EnableEditDrag != e.HasDraggable)
                {
                    EnableEditDrag = e.HasDraggable;
                }
                else
                {
                    EnableEditDragEv(this, e.HasDraggable);
                }
            };

            #region ResetBaseGraph
            var x = new double[3600];
            var y = new double[3600];
            for (int i = 0; i < 3600; i++)
                x[i] = i;
            y[0] = 3000;
            y[59] = -3000;
            //spectrum
            var scatterFlag = Oscill.Plot.AddScatter(x, y, label: "ZoomSpectrum", color: System.Drawing.Color.Transparent);
            scatterFlag.YAxisIndex = yAixs.AxisIndex;
            scatterFlag.MaxRenderIndex = 59;
            //speed
            var scatterSpeedFlag = Oscill.Plot.AddScatter(new double[] { 0, 1 }, new double[] { -200, 200 }, label: "ZoomSpeed", color: System.Drawing.Color.Transparent);
            scatterSpeedFlag.YAxisIndex = yAixsSpeed.AxisIndex;
            //pressure
            var scatterPressureFlag = Oscill.Plot.AddScatter(new double[] { 0, 1 }, new double[] { -200, 200 }, label: "ZoomPressure", color: System.Drawing.Color.Transparent);
            scatterPressureFlag.YAxisIndex = yAixsPressure.AxisIndex;
            //gradient
            var scatterGradientFlag1 = Oscill.Plot.AddScatter(new double[] { 0, 1 }, new double[] { 100, -100 }, label: "ZoomGradient", color: System.Drawing.Color.Transparent);
            scatterGradientFlag1.YAxisIndex = yAixsGradient.AxisIndex;
            #endregion
        }
        internal void Dragged(object? sender, EventArgs e)
        {
            if (sender is OscillogramDraggable ps)
            {
                var xs = ps.Xs;
                var ys = ps.Ys;
                var index = ps.CurrentIndex;

                int leftIndex = Math.Max(index - 1, 0);
                int rightIndex = Math.Min(index + 1, xs.Count() - 1);

                var point = Mouse.GetPosition(this);
                (double coordinateX, double coordinateY) = Oscill.GetMouseCoordinates(0, 0);


                if (ps is IDraggable dr)
                {
                    if (!Oscill.CurrentDraggableGraph.DraggableGraph.Contains(dr))
                    {
                        Oscill. CurrentDraggableGraph.DraggableGraph.Add(dr);
                    }
                    if (Oscill.CurrentDraggableGraph.CurrentDraggableGraph != dr)
                    {
                        Oscill.CurrentDraggableGraph.CurrentDraggableGraph = dr!;
                        if (dr is IGraphType graphType)
                            Oscill. CurrentDraggableGraph.CurrentDraggableGraphType = graphType.GraphType;
                        else
                            Oscill.CurrentDraggableGraph.CurrentDraggableGraphType = GraphType.GraphType;
                    }
                }

                DragX = Math.Round(ps.Xs[ps.CurrentIndex], 1);
                DragY = Math.Round(ps.Ys[ps.CurrentIndex], 1);
                if (Oscill.CurrentDraggableGraph.CurrentDraggableGraph is IGraphType graphType2)
                {
                    DragType = graphType2.GraphType;
                }
                else
                {
                    DragType = GraphType.GraphType;
                }
            }

        }
        #endregion

        #region reset lines
        private void ResetEndClockLine()
        {
            if (scatterrEndTimeLine != null)
                Oscill.Plot.Remove(scatterrEndTimeLine);
            double CustomSnapFunction(double value)
            {
                return value;
            }
            var SnapDisabled = new ScottPlot.SnapLogic.NoSnap1D();
            var SnapCustom = new Custom1D(CustomSnapFunction);
            scatterrEndTimeLine = Oscill.Plot.AddOscillogramVerticalLine(graphType: GraphType.EndClockLine, LastTimeIndex, ColorTranslator.FromHtml("#000000"), 1.0f);
            scatterrEndTimeLine.LineWidth = EnableEditDrag ? 3 : 1;
            scatterrEndTimeLine.DragEnabled = EnableEditDrag;
            scatterrEndTimeLine.LineStyle = LineStyle.Dot;
            scatterrEndTimeLine.PositionLabel = true;
            scatterrEndTimeLine.PositionLabelBackground = System.Drawing.Color.LightGray;
            scatterrEndTimeLine.PositionLabelOppositeAxis = true;
            scatterrEndTimeLine.PositionFormatter = (x) =>
            {
                return $"TotalTime={(x / 60):F1}min";
            };
            scatterrEndTimeLine.DragSnap = new ScottPlot.SnapLogic.Independent2D(SnapCustom, SnapDisabled);
            scatterrEndTimeLine.DragLimitMin = 0;
            scatterrEndTimeLine.DragLimitMax = 2 * 3600;
            scatterrEndTimeLine.Dragged += ScatterrEndTimeLine_Dragged;
            scatterrEndTimeLine.MovePointFunc = MoveBetweenAdjacentOscillogramVerticalLine;


        }
        Coordinate MoveBetweenAdjacentOscillogramVerticalLine(Coordinate requested)
        {

            var speedCount = SpeedX.Count(_ => _ > CurrentTimeIndex + hitTestStep);
            var gradientCount = GradientX.Count(_ => _ > CurrentTimeIndex + hitTestStep);
            var max = speedCount < gradientCount ? gradientCount : speedCount;

            if (speedCount <= 1 || gradientCount <= 1)
            {
                requested.X = LastTimeIndex;
                return requested;
            }

            Range(SpeedX, hitTestStep, CurrentTimeIndex, max, ref requested);
            Range(GradientX, hitTestStep, CurrentTimeIndex, max, ref requested);

            LastTimeIndex = (int)requested.X;
            return requested;
        }
        void Range(double[] source, double step, double current, double max, ref Coordinate target)
        {
            var list = source.ToList();
            if (!list.Any(_ => _ > current + step))
                return;
            var item = list.First(_ => _ > current + step);
            var index = list.IndexOf(item) + 1;
            if (index <= 0 || index >= list.Count)
                return;

            for (var i = index; i < source.Length; i++)
                list[i] = list[i - 1] + step;


            if (target.X <= (max * step + current))
                target.X = max * step + current;

            if (target.X < list[list.Count - 1])
            {
                target.X = list[list.Count - 1];
                return;
            }

            source[source.Length - 1] = target.X;

            for (int i = list.Count - 2; i >= index; i--)
            {
                if (source[i] > list[i])
                {
                    if (source[i] < source[i + 1] - step)
                    { }
                    else
                    {
                        if (source[i + 1] - step > list[i])
                        {
                            source[i] = source[i + 1] - step;
                        }
                        else
                        {
                            source[i] = list[i];
                        }
                    }
                }
                else
                {
                    source[i] = list[i];
                }
            }
        }
        private void ScatterrEndTimeLine_Dragged(object? sender, EventArgs e)
        {
            var vline = sender as OscillogramVLine;
            if (vline == null) return;
        }
        /// <summary>
        /// 谱图线
        /// </summary>
        private void ResetSpectrum()
        {
            Waves.Clear();
            //Xs.Clear();
            //Ys.Clear();
            if (Xs.Count() != Ys.Count())
                return;
            for (int i = 0; i < Xs.Count; i++)
            {
                if (scatterSpectrum.Keys.Any(_ => _ == Xs[i]) && scatterSpectrum[Xs[i]] != null)
                    Oscill.Plot.Remove(scatterSpectrum[Xs[i]]);

                if (scatterPolygons.Keys.Any(_ => _ == Xs[i]) && scatterPolygons[Xs[i]] != null)
                {
                    if (scatterPolygons[Xs[i]] != null && scatterPolygons[Xs[i]].Length != 0)
                    {
                        for (int v = 0; i < scatterPolygons[Xs[i]].Length; v++)
                            Oscill.Plot.Remove(scatterPolygons[Xs[i]][v]);
                    }
                }

                if (Xs[i] != Ys[i] || Xs[i].V.Count() != Ys[i].V.Count())
                    continue;
                Waves.Add((OscillogramWave)Xs[i]);
                var scatter = Oscill.Plot.AddScatter(Xs[i].V, Ys[i].V);
                scatter.YAxisIndex = yAixs.AxisIndex;
                scatter.MaxRenderIndex = CurrentTimeIndex;
                scatter.MarkerShape = MarkerShape.none;
                //scatter.IsHighlighted = false;
                scatter.Color = ColorTranslator.FromHtml(Xs[i].Color!);
                scatter.IsVisible = Xs[i].IsSelected;
                scatter.Label = Xs[i].Decription;
                //404 750

                scatterSpectrum[Xs[i]] = scatter;


            }
        }
        private void ResetCurrentLine()
        {
            if (scatterCurrentTimeLine != null)
                Oscill.Plot.Remove(scatterCurrentTimeLine);
            double CustomSnapFunction(double value)
            {
                return value;
            }
            var SnapDisabled = new ScottPlot.SnapLogic.NoSnap1D();
            var SnapCustom = new Custom1D(CustomSnapFunction);
            scatterCurrentTimeLine = Oscill.Plot.AddVerticalLine(CurrentTimeIndex, ColorTranslator.FromHtml("#B060B0"), 1.0f);
            scatterCurrentTimeLine.LineWidth = 1;
            scatterCurrentTimeLine.DragEnabled = false;
            scatterCurrentTimeLine.LineStyle = LineStyle.Dot;
            scatterCurrentTimeLine.PositionLabel = true;
            scatterCurrentTimeLine.PositionLabelBackground = System.Drawing.Color.Gray;
            scatterCurrentTimeLine.PositionLabelOppositeAxis = true;
            scatterCurrentTimeLine.PositionFormatter = (x) =>
            {
                return $"CurrentTime={(x / 60):F1}min";
            };
            scatterCurrentTimeLine.DragSnap = new ScottPlot.SnapLogic.Independent2D(SnapCustom, SnapDisabled);
            scatterCurrentTimeLine.DragLimitMin = 0;
            scatterCurrentTimeLine.DragLimitMax = 2 * 3600;
        }
        private void ResetPeaks()
        {
            //if (scatterPolygons != null && scatterPolygons.Length != 0)
            //{
            //    for (int i = 0; i < scatterPolygons.Length; i++)
            //    {
            //        Oscill.Plot.Remove(scatterPolygons[i]);
            //    }
            //}
            #region MyRegion
            var xx = "[-2.0,-1.0,0.0,1.0,2.0,3.0,4.0,5.0,6.0,7.0,8.0,9.0,10.0,11.0,12.0,13.0,14.0,15.0,16.0,17.0,18.0,19.0,20.0,21.0,22.0,23.0,24.0,25.0,26.0,27.0,28.0,29.0,30.0,31.0,32.0,33.0,34.0,35.0,36.0,37.0,38.0,39.0,40.0,41.0,42.0,43.0,44.0,45.0,46.0,47.0,48.0,49.0,50.0,51.0,52.0,53.0,54.0,55.0,56.0,57.0,58.0,59.0,60.0,61.0,62.0,63.0,64.0,65.0,66.0,67.0,68.0,69.0,70.0,71.0,72.0,73.0,74.0,75.0,76.0,77.0,78.0,79.0,80.0,81.0,82.0,83.0,84.0,85.0,86.0,87.0,88.0,89.0,90.0,91.0,92.0,93.0,94.0,95.0,96.0,97.0,98.0,99.0,100.0,101.0,102.0,103.0,104.0,105.0,106.0,107.0,108.0,109.0,110.0,111.0,112.0,113.0,114.0,115.0,116.0,117.0,118.0,119.0,120.0,121.0,122.0,123.0,124.0,125.0,126.0,127.0,128.0,129.0,130.0,131.0,132.0,133.0,134.0,135.0,136.0,137.0,138.0,139.0,140.0,141.0,142.0,143.0,144.0,145.0,146.0,147.0,148.0,149.0,150.0,151.0,152.0,153.0,154.0,155.0,156.0,157.0,158.0,159.0,160.0,161.0,162.0,163.0,164.0,165.0,166.0,167.0,168.0,169.0,170.0,171.0,172.0,173.0,174.0,175.0,176.0,177.0,178.0,179.0,180.0,181.0,182.0,183.0,184.0,185.0,186.0,187.0,188.0,189.0,190.0,191.0,192.0,193.0,194.0,195.0,196.0,197.0,198.0,199.0,200.0,201.0,202.0,203.0,204.0,205.0,206.0,207.0,208.0,209.0,210.0,211.0,212.0,213.0,214.0,215.0,216.0,217.0,218.0,219.0,220.0,221.0,222.0,223.0,224.0,225.0,226.0,227.0,228.0,229.0,230.0,231.0,232.0,233.0,234.0,235.0,236.0,237.0,238.0,239.0,240.0,241.0,242.0,243.0,244.0,245.0,246.0,247.0,248.0,249.0,250.0,251.0,252.0,253.0,254.0,255.0,256.0,257.0,258.0,259.0,260.0,261.0,262.0,263.0,264.0,265.0,266.0,267.0,268.0,269.0,270.0,271.0,272.0,273.0,274.0,275.0,276.0,277.0,278.0,279.0,280.0,281.0,282.0,283.0,284.0,285.0,286.0,287.0,288.0,289.0,290.0,291.0,292.0,293.0,294.0,295.0,296.0,297.0,298.0,299.0,300.0,301.0,302.0,303.0,304.0,305.0,306.0,307.0,308.0,309.0,310.0,311.0,312.0,313.0,314.0,315.0,316.0,317.0,318.0,319.0,320.0,321.0,322.0,323.0,324.0,325.0,326.0,327.0,328.0,329.0,330.0,331.0,332.0,333.0,334.0,335.0,336.0,337.0,338.0,339.0,340.0,341.0,342.0,343.0,344.0,345.0,346.0,347.0,348.0,349.0,350.0,351.0,352.0,353.0,354.0,355.0,356.0,357.0,358.0,359.0,360.0,361.0,362.0,363.0,364.0,365.0,366.0,367.0,368.0,369.0,370.0,371.0,372.0,373.0,374.0,375.0,376.0,377.0,378.0,379.0,380.0,381.0,382.0,383.0,384.0,385.0,386.0,387.0,388.0,389.0,390.0,391.0,392.0,393.0,394.0,395.0,396.0,397.0,398.0,399.0,400.0,401.0,402.0,403.0,404.0,405.0,406.0,407.0,408.0,409.0,410.0,411.0,412.0,413.0,414.0,415.0,416.0,417.0,418.0,419.0,420.0,421.0,422.0,423.0,424.0,425.0,426.0,427.0,428.0,429.0,430.0,431.0,432.0,433.0,434.0,435.0,436.0,437.0,438.0,439.0,440.0,441.0,442.0,443.0,444.0,445.0,446.0,447.0,448.0,449.0,450.0,451.0,452.0,453.0,454.0,455.0,456.0,457.0,458.0,459.0,460.0,461.0,462.0,463.0,464.0,465.0,466.0,467.0,468.0,469.0,470.0,471.0,472.0,473.0,474.0,475.0,476.0,477.0,478.0,479.0,480.0,481.0,482.0,483.0,484.0,485.0,486.0,487.0,488.0,489.0,490.0,491.0,492.0,493.0,494.0,495.0,496.0,497.0,498.0,499.0,500.0,501.0,502.0,503.0,504.0,505.0,506.0,507.0,508.0,509.0,510.0,511.0,512.0,513.0,514.0,515.0,516.0,517.0,518.0,519.0,520.0,521.0,522.0,523.0,524.0,525.0,526.0,527.0,528.0,529.0,530.0,531.0,532.0,533.0,534.0,535.0,536.0,537.0,538.0,539.0,540.0,541.0,542.0,543.0,544.0,545.0,546.0,547.0,548.0,549.0,550.0,551.0,552.0,553.0,554.0,555.0,556.0,557.0,558.0,559.0,560.0,561.0,562.0,563.0,564.0,565.0,566.0,567.0,568.0,569.0,570.0,571.0,572.0,573.0,574.0,575.0,576.0,577.0,578.0,579.0,580.0,581.0,582.0,583.0,584.0,585.0,586.0,587.0,588.0,589.0,590.0,591.0,592.0,593.0,594.0,595.0,596.0,597.0,598.0,599.0,600.0,601.0,602.0,603.0,604.0,605.0,606.0,607.0,608.0,609.0,610.0,611.0,612.0,613.0,614.0,615.0,616.0,617.0,618.0,619.0,620.0,621.0,622.0,623.0,624.0,625.0,626.0,627.0,628.0,629.0,630.0,631.0,632.0,633.0,634.0,635.0,636.0,637.0,638.0,639.0,640.0,641.0,642.0,643.0,644.0,645.0,646.0,647.0,648.0,649.0,650.0,651.0,652.0,653.0,654.0,655.0,656.0,657.0,658.0,659.0,660.0,661.0,662.0,663.0,664.0,665.0,666.0,667.0,668.0,669.0,670.0,671.0,672.0,673.0,674.0,675.0,676.0,677.0,678.0,679.0,680.0,681.0,682.0,683.0,684.0,685.0,686.0,687.0,688.0,689.0,690.0,691.0,692.0,693.0,694.0,695.0,696.0,697.0,698.0,699.0,700.0,701.0,702.0,703.0,704.0,705.0,706.0,707.0,708.0,709.0,710.0,711.0,712.0,713.0,714.0,715.0,716.0,717.0,718.0,719.0,720.0,721.0,722.0,723.0,724.0,725.0,726.0,727.0,728.0,729.0,730.0,731.0,732.0,733.0,734.0,735.0,736.0,737.0,738.0,739.0,740.0,741.0,742.0,743.0,744.0,745.0,746.0,747.0,748.0,749.0,750.0,751.0,752.0,753.0,754.0,755.0,756.0,757.0,758.0,759.0,760.0,761.0,762.0,763.0,764.0,765.0,766.0,767.0,768.0,769.0,770.0,771.0,772.0,773.0,774.0,775.0,776.0,777.0,778.0,779.0,780.0,781.0,782.0,783.0,784.0,785.0,786.0,787.0,788.0,789.0,790.0,791.0,792.0,793.0,794.0,795.0,796.0,797.0,798.0,799.0,800.0,801.0,802.0,803.0,804.0,805.0,806.0,807.0,808.0,809.0,810.0,811.0,812.0,813.0,814.0,815.0,816.0,817.0,818.0,819.0,820.0,821.0,822.0,823.0,824.0,825.0,826.0,827.0,828.0,829.0,830.0,831.0,832.0,833.0,834.0,835.0,836.0,837.0,838.0,839.0,840.0,841.0,842.0,843.0,844.0,845.0,846.0,847.0,848.0,849.0,850.0,851.0,852.0,853.0,854.0,855.0,856.0,857.0,858.0,859.0,860.0,861.0,862.0,863.0,864.0,865.0,866.0,867.0,868.0,869.0,870.0,871.0,872.0,873.0,874.0,875.0,876.0,877.0,878.0,879.0,880.0,881.0,882.0,883.0,884.0,885.0,886.0,887.0,888.0,889.0,890.0,891.0,892.0,893.0,894.0,895.0,896.0]";
            var xy = "[-0.0,0.0012223860024796573,0.0009735737471288208,0.0014138435357620846,0.002000890540979688,0.0014285828918896518,0.0016222158200704703,0.0013119691514081914,0.0009309442582468281,0.0017311785464637413,0.001169564950974877,0.001272882035953425,0.000981132623370921,0.000196913999521108,0.0008548998116186693,0.0004948640313468979,0.0004480283735983798,0.0005773842266966012,0.0006210092994818058,1.2033078227231169E-06,0.00036384977610671114,0.00028026598602733625,-0.0007425924548164844,-0.0001822951631814517,0.010560440716512643,0.010403751469254789,0.010953411657267766,0.011201962275545827,0.0003996555342703319,0.00020211498893179467,-0.0002576461155670102,-0.0005462323095788545,-0.0005439417268682912,9.45063651881048E-05,0.0008954961847510527,0.0007754779580284359,0.0004906712768514485,0.0001458091476879311,-7.073207895221441E-05,-1.816899403652378E-05,-3.7586409500063215E-05,0.0120436302354365,0.01209285829143165,0.012009454808502634,0.011783554419601164,-9.041798705443002E-05,-0.0002951973078151772,-0.00011345539770538972,0.00020956681345743976,1.1474190524742256E-05,0.010932050419022154,0.02220782896452008,0.022313047051459603,0.022319911269227798,0.01131364833398966,-0.00022492916440687737,-6.161133951162144E-06,7.509841154869224E-05,-4.814881931299459E-06,0.000590248504460272,0.00019769678928534373,-0.00019886847609531093,-0.0010061110086449342,-0.0015943268444707803,-0.001359882069268012,-0.0010971909319967249,0.0003716761151411616,0.0005944569307502379,0.0006242275973683067,0.0013286278171782755,0.0005176091339087663,0.0008337131056650369,0.0008016527258477583,-4.9019246947306624E-05,6.500353211885961E-05,-0.00015499227044528,-0.0006541401968127396,-0.0005650293033355123,0.00020547460865244743,-0.0001476478111625668,-0.00026610088429572596,-0.00034036504022704644,-0.0012020967530897656,-0.001121060160640307,-0.000949032982742336,-0.000769569578090761,-0.00035157409932048506,0.00010495718533005391,0.00037436794119524854,0.012075274189528017,0.011117186698918053,0.011336408007910245,0.010943930194952406,-0.0011110456113030142,-0.0007030979040925192,-0.0010838692282436216,-0.0009037794074635123,-0.0005588616577394484,0.0001263854682599309,-0.0014489519056029745,0.009808432823342645,0.009878234852980792,0.009707661076961888,0.011395255960051079,0.0008451564562590163,0.0010605667366045965,0.0020638049530776192,0.0023863312076584835,0.0025104087938978004,0.025515021894350155,0.04372072114913619,0.050225895728432945,0.05331804824753984,0.04548330758541574,0.030166180827715137,0.02740192749858064,0.02875748639260482,0.017811454401637613,0.019830279107232545,0.022184013430978942,0.022747141554734177,0.0336667983189287,0.042499546397429915,0.03959988683300195,0.03836340956123653,0.026900172862666067,0.016080617026643478,0.01546912590882821,0.0138284156199476,0.013467140877561581,0.02396223681677214,0.024331316613831075,0.025382109472598457,0.025763556252879308,0.014272072630829961,0.013920651770949217,0.011422336704625361,0.009787565348038445,0.008650119799606665,0.006646028821147501,0.00721435087203926,0.005722524869725483,0.004730460044488621,0.0046287950168234235,0.0029235178278184107,0.001994713143143074,0.0014097561278303165,0.0009419394923523439,0.0012044065295177043,0.0016339525174441797,0.0017417429461694315,0.0011969669890021719,0.00024411973069691627,-0.0006781658784999229,0.010176906165149427,0.02247193972518117,0.034795966716421316,0.03488590727739486,0.024009039175866706,0.011684265059249055,0.0004968456629405604,0.0010689692657796003,0.0008443807334774034,0.0009290483449424584,0.00010798745305875022,2.4436263854740995E-05,-0.00026592272493162973,0.0002561247601772207,0.0005668706346737886,0.00022076162038140611,0.0003589853982112678,-0.00047851385100372784,-0.0004739374164954392,-0.00011066559720441834,0.0003670451273851522,0.011979904417114929,0.01219521472004918,0.012273053481341525,0.011475753213803997,-0.0002466139446104951,-0.0002454675346590675,-0.0002649324572585076,0.0004405575831749834,0.012773022884591284,0.011974620853131654,0.011792735330224487,0.011717050293874928,-0.000384863754733264,0.0004628453592331844,0.0006516004563581141,0.0003928354568601752,0.0008542644491654139,0.0004364375353448335,0.0010438199017030162,0.0026765366914424382,0.003127260209350004,0.004802501344160671,0.005057256817379456,0.004193478230818708,0.0043214472386708305,0.004100056404982003,0.0038372415123088146,0.0033763722668575607,0.0031205849027577904,0.0024694667865781655,0.002194653479147527,0.002222181706084278,0.013918495270103125,0.013680497245716915,0.013822991380805701,0.01412971457558608,0.002556610686426332,0.0024808368150492518,0.0023234146037640346,0.0025491274968962225,0.002287904875293443,0.0022500377614412884,0.0023189537055341666,0.002054134249241607,0.002119668524903689,0.0020703463361396177,0.0015431052795833347,0.0010852277393596618,0.0007350214336789087,0.011851486391394262,0.012204225276454789,0.012946521544038032,0.01310139156944331,0.001882912275817835,0.0016262613085283274,0.0011063510382661283,0.012404738326008701,0.012176985026398704,0.012703544055569956,0.012879252792562041,0.0011683686667673101,0.0014729103556493446,0.0007919657772770801,0.0002908567522329495,0.014487338739791255,0.019595197693537285,0.02471964267602111,0.029381986652012547,0.020323521459788486,0.01962038947319001,0.031189224789189854,0.031195196245759955,0.03130916793533446,0.031755434457126266,0.020431269489426925,0.021757875008043097,0.022258364853434402,0.022764273188360603,0.023045605105115514,0.022587151907326124,0.022549776129108006,0.02262809599501354,0.023682945269380598,0.02497030603146851,0.026586750293344494,0.028060002455015463,0.02900741799643744,0.02979483359859977,0.030396031559616253,0.03197877618992896,0.03316611260656889,0.03470101838951943,0.03594086920826443,0.03645036625057464,0.037479236241451316,0.03835523277508855,0.03885291445217067,0.03908609111337574,0.03825700006532172,0.037142300854192095,0.036082333632640934,0.03483747867280603,0.0338520856145731,0.03235726768694179,0.031001448532296385,0.029678014259272353,0.02843590062720293,0.02689856656422338,0.025949043481706378,0.025428521515097502,0.025871114919925924,0.02768194513007792,0.02936263951222389,0.03091821288567386,0.033493426848611646,0.03574165482136644,0.03774929784799924,0.04034374476608224,0.041872008826806285,0.04258265813866302,0.044179283498218806,0.04479997676372658,0.04533291536391176,0.046765764985107545,0.04672250906890074,0.0470360985600689,0.04631427118544484,0.045544685073788574,0.045450599373010006,0.045608371059321876,0.046247351370532395,0.045678772512443885,0.045462937981774754,0.045306437731216676,0.04397304850298382,0.04343256358654135,0.04224179140109857,0.041044362096973666,0.03995492270650999,0.03916355350691277,0.038564295609761254,0.03745085140100654,0.03770782459928891,0.037921376495571056,0.039053857776451775,0.04065883710357719,0.04256508998376876,0.04456984697808858,0.04587363894938419,0.04689786013044457,0.07166179477534532,0.07265059628235487,0.07325322662026693,0.07367869291293479,0.05030910853022801,0.05034123869827712,0.06123129943006678,0.061616035261511985,0.06206241618311159,0.061658269276015636,0.06235408765849787,0.06234252572506311,0.06132600775019839,0.060672301623361145,0.04871114481521553,0.04820235622467679,0.047129304290688755,0.04628103389500924,0.04434847038472285,0.043154468322369285,0.04267743222600362,0.05334030034992152,0.052764052439345684,0.05129695481953297,0.04960323713526732,0.03796966415859632,0.04925491544459137,0.04883154826638976,0.04937611538486346,0.04944749061728546,0.06281819206312836,0.06310066867644591,0.07436126401769677,0.08524423634484239,0.060312517961056356,0.0601431926313447,0.06037486897471339,0.049392985624869885,0.04953764405818688,0.05022905503127849,0.03862690238318015,0.03851807921082312,0.038214021035378895,0.03854882512657688,0.038968970262453384,0.038714225607862904,0.050766461801262736,0.06202708806391677,0.061850579138569305,0.06283162179822781,0.05165648541125131,0.04016627541382328,0.0406376050534863,0.04069299783208348,0.052147983028698025,0.051672796830732545,0.05137922897810689,0.05093687603444813,0.0393815779739733,0.04009388346783109,0.0407397754565882,0.04075858900599543,0.04144129002057688,0.04168270453962677,0.041717999916491305,0.042435341249175196,0.04249842143946679,0.04277948979582459,0.042811013223839796,0.04252224839418834,0.04259294174428134,0.04255887230606159,0.054097358168147945,0.05469211754223031,0.05460501981670371,0.054717363263772946,0.04297602666472684,0.04245811216919853,0.04208652375328253,0.041786359524450346,0.042210130901819784,0.04212447123625167,0.042858795691147145,0.04355878398498794,0.0438840387228054,0.044927883974855004,0.04636669204085417,0.04992025275773943,0.058754627950734266,0.07470676578589758,0.10224370289372274,0.1672703781179128,0.26576242731839417,0.37490495130627244,0.5027634289891437,0.6178764957567214,0.7186273723015567,0.8359576032427575,0.9290234122434105,0.9994680892273191,1.047989343003525,1.0609985332416345,1.0672997057614455,1.0638139592963007,1.0533446346144344,1.0380360143366647,0.9829847706599635,0.9170353843265061,0.8376911538110461,0.7043383610609805,0.5782751233479366,0.4571715823512055,0.3373195283881526,0.2719314766553774,0.23421453518317342,0.20369206267820922,0.1993377792724098,0.1858136373400142,0.1729166072929034,0.1635352407375144,0.14312164220237467,0.1356083804236971,0.1312938762451288,0.12537805198205126,0.12077307096383115,0.11500687713789523,0.10969714100776341,0.10537938401907318,0.10094605989727679,0.09789075797045425,0.09457154284034792,0.09104082986505327,0.0876361417086953,0.09662030149813308,0.09404518035375647,0.09198548810176052,0.10137820166646264,0.08758373584218472,0.08526189340878583,0.0840839365226861,0.07132687951865932,0.0704526444188218,0.08073206974310508,0.07826610816414398,0.07689593260865957,0.07500336884897772,0.062420409124072665,0.06182371206003034,0.05973522583828953,0.05895225539486345,0.05840686619001611,0.058207821480753014,0.0584567465075465,0.05815348629726017,0.05755083902896294,0.05682974621242354,0.0567397545581189,0.05669538232998277,0.056403759062863955,0.05688830652676269,0.05635770109208554,0.05621687418215532,0.06790514808807088,0.06706637607538267,0.06705599792444632,0.06694919565001102,0.05486769145064632,0.05521931674108718,0.05531665087226078,0.055329669916412284,0.05597151189228418,0.055569377303353776,0.06741155823616495,0.0672502323231841,0.06705233805164,0.07875487559926964,0.07854222859389595,0.078741318565401,0.07931385482450615,0.06839751637095036,0.057419116179186096,0.05825589107684406,0.058221898083305315,0.07135865123478732,0.07323742621505977,0.08075664228656805,0.09471794399591835,0.11619273733283443,0.16896024807815846,0.2358747811603218,0.3209998591220477,0.43459605678850943,0.5478376949849385,0.6593233430034412,0.7606720690947356,0.8241007059775889,0.8747438913373968,0.9111682965153725,0.9340461216763833,0.9416429737155421,0.9585084548328646,0.9553501278470362,0.9343601513116679,0.9114531169169244,0.8630625269542878,0.8182356661074156,0.7653985926671817,0.703679607492892,0.6472797151226797,0.575987980558452,0.47442425378047026,0.3818407460455922,0.28810835834564585,0.22910564515920184,0.21142186699828786,0.19518789234573003,0.1827678798900169,0.1610036145778082,0.152825898581948,0.15946686261904736,0.16444683329104462,0.18385183196434593,0.17904260646815756,0.16264098980059585,0.14882107826256405,0.12049902594799498,0.11610110263347445,0.11368378764226486,0.11118797235637046,0.10961672124480046,0.10838472920473446,0.10701551257040816,0.10460316825830251,0.10276187269217313,0.10090251454855158,0.09842865858395626,0.09757002410453906,0.10636202975789619,0.10506969988507882,0.11498890600577816,0.11314681634187022,0.10162977849110127,0.10053158164288792,0.09936368569712564,0.0983267179088479,0.097165581206141,0.09614904507781401,0.08460701432804102,0.0846460184773574,0.085049333316077,0.08492604784907848,0.08479539198441632,0.08401519229799301,0.0836479756441863,0.08374354431469101,0.09459951330739216,0.09491952204092571,0.09525887572835787,0.09522285446766135,0.08461860777507524,0.08453401161359654,0.08414884304941345,0.08433600672354294,0.08434157624381171,0.08532333048726626,0.0859832614288342,0.08683860403975494,0.09914238565350883,0.09925715397418344,0.10053687701429054,0.10169121592341039,0.09046492334048915,0.10224511629082202,0.10168276553804112,0.11310470620677024,0.11243736111358013,0.10108793012486264,0.10156976650949272,0.10147088127962829,0.10298672129840143,0.10359012893839872,0.10415924560464149,0.09294791543849477,0.09331847180705641,0.09434831350376788,0.09432702414106986,0.09518647479017255,0.09529746167335776,0.09554322306274488,0.09545952538094622,0.09570225338873604,0.09650816791049464,0.09673871716122269,0.09818623460685513,0.09898822898837513,0.09900974476334218,0.09880208534348188,0.11087385952002665,0.11050297200533363,0.11085787963989904,0.11109994861650489,0.09915183461332314,0.09913604970518179,0.09908856569761693,0.09864208982609214,0.09850118593674716,0.0987238215815198,0.09905376058191896,0.10032068505099574,0.10076702853907733,0.10112275531766032,0.10119348402923335,0.10153210306700507,0.1019368352563317,0.10219825149188867,0.10219680758813937,0.10264267191565218,0.10248204396665979,0.102554369486911,0.10296376239996219,0.10160575190705579,0.10175479511719986,0.10151052839728593,0.10169765556263427,0.11385256312803055,0.11389311512302384,0.12566618327183454,0.13681348221162212,0.1252610023331314,0.12509454605300582,0.1143441159402622,0.10280118393848804,0.10364994674444802,0.10401939050229403,0.11535711638452306,0.11590675460775748,0.11589218996033998,0.12724726061289834,0.11490855496288341,0.11452539965332251,0.11388213836656781,0.10217932117790537,0.10239035430817257,0.10245701901757212,0.10291984080936345,0.10269007057546309,0.10286817766081574,0.12709205677876154,0.1264845550171632,0.12679826199532188,0.12603720014880418,0.10160187409883928,0.10187983013122338,0.10206529978731213,0.10213295811642475,0.10285735109681604,0.10253169295176677,0.1024693513932,0.10243479719138043,0.10166989976213724,0.11212188610493017,0.11135641544036473,0.11134346044934101,0.11001664944142545,0.09966442779337482,0.11096476634774391,0.12213387641358,0.12223965547421754,0.1218714155907155,0.11003844063749464,0.10965465000290298,0.11007449269361849,0.10868930848219119,0.11991232214472322,0.10796591426295686,0.10793720437438453,0.10865838920796157,0.09694810342359471,0.09689827321289118,0.09571487833867398,0.13289675956294225,0.1319909211187048,0.13095537448752415,0.1300983996695912,0.10329363700659211,0.10357111628990728,0.1038138872492107,0.10370548045354974,0.09197279371121517,0.09130577679016734,0.09052015066116942,0.09014087586767526,0.0896044787162267,0.08917984068081854,0.08892342205586788,0.0891409918760379,0.09952211747978243,0.11079041746708912,0.11058348267217398,0.1211347378475398,0.1101078151135068,0.0985130282464571,0.11002288841989429,0.09866530662219325,0.09819600973819849,0.097282040411964,0.08496915945916311,0.08432117265927791,0.08433511058074251,0.0840635205008553,0.08449699862011036,0.08456634017777892,0.08348314000243486,0.08284288702044108,0.09406576738750178,0.0942393152791549,0.09509415918233524,0.09585903754586789,0.08418188230417306,0.08359294779950531,0.08342755043358091,0.08302787127234024,0.0824733762623502,0.08288598836260733,0.08229733184110617,0.08214007670004222,0.09426371958946832,0.09385801759279323,0.10536018198840522,0.10487468239198272,0.09290424491386579,0.09202741980997864,0.08073265230232071,0.08157298827195453,0.08119657305465541,0.08186766555995088,0.09324692915063348,0.09234622953353513,0.09269226253195513,0.09274331412119666,0.08026659382780162,0.08093796695238746,0.080856529314106,0.08038555906439845,0.08045819593822494,0.08048027861499667,0.08038925121006353,0.0793158874477326,0.07978852686188728,0.07958167787673032,0.09059070122668662,0.09205777729385672,0.09141253348568351,0.09090809837614575,0.07940115621225799,0.07904042788118447,0.09112183150154082,0.09119590667283949,0.09141844997810626,0.09154360531777758,0.08006201577182608,0.0803436880765236,0.07985113430288518,0.07973835053719788,0.07927413109420739,0.07928238617851649,0.07945500581816305,0.09153166090029212,0.0917813785537721,0.09190665767530823,0.09208092547808369,0.08027542414367547,0.0801093390918825,0.0911255770681711,0.09117090507363311,0.09068962604693172,0.09111151186062673,0.08020277116272909,0.08023987179339019,0.09197188291763245,0.09199942429581165,0.09165113379280732,0.09211665464652946,0.08091003183210224,0.08069264511804995,0.08082467127293912,0.08037014563377638,0.08033844027540547,0.081203683261943,0.08095351985304755,0.09233594322720842,0.11618885326325992,0.11508334078415687,0.11533075201002362,0.10394557972783221,0.07970743202004575,0.07996557133769783,0.08031841903049564,0.080659815488804,0.08101749080750817,0.08116193192464041,0.08095780203777984,0.08011999607306149,0.07944278668762557,0.0794414101512231,0.07933398241771356,0.0804990484412471,0.08085704482332697,0.08132054606061387,0.0814955148489217,0.08134626133767017,0.08167787653402586,0.09319115681279534,0.0927892866977752,0.09271335674158508,0.09263758767743258,0.08058649910028087,0.08100767437920957,0.10524794395556378,0.11723957424864734,0.11715294690392637,0.11695729636294884,0.09197982198609561,0.08011496243975136,0.08054741201037566,0.09254004754146783,0.09292177888818232,0.10380373249665965,0.10373061848027337,0.091773779930603,0.09173389627631992,0.0810244230538562,0.08075446605102073,0.08067737952057906,0.08029989887066477,0.08000340205038366,0.0794285414834531,0.07943679813718976,0.07960821757422493,0.07927819795649527,0.079719705236519,0.07905157465660309,0.07889659974680163,0.09021887340088135,0.10179664338794395,0.10211716221874442,0.10189810638013855,0.08989968344325967,0.07822166270066375,0.08910021609083989,0.08904005313607416,0.11369924490480755,0.11337030428794852,0.10197855478397289,0.1024360824511693,0.07804192110497436,0.07788328967710582,0.0776691498706874,0.07646102713877603,0.07587173750402934,0.07536186044871673,0.07586172227271429,0.07596902121772428,0.07638337282941662,0.07603302991096089,0.08649953788188114,0.08626470477256991,0.08549451180253079,0.08525887071364213,0.07431157904572402,0.07474771375771012,0.0879547926704351,0.08962748563184282,0.09361527910852145]";
            #endregion
            var xs = JsonConvert.DeserializeObject<double[]>(xx);
            var ys = JsonConvert.DeserializeObject<double[]>(xy);
            for (int i = 0; i < ys.Length; i++)
            {
                ys[i] *= 1000;
            }

            //scatterPolygons = Oscill.Plot.DrawPeakArea(xs, ys, new List<(double startTime, double endTime)>() { (0, 20), (80, 100), (120, 200), (250, 300), (400, 500), (600, 700) }, 10, true);
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

            var scatter = new OscillogramDraggable(SpeedX, SpeedY, GraphType.Speed)
            {
                DragCursor = ScottPlot.Cursor.All,
                DragEnabled = EnableEditDrag,
                MarkerShape = EnableEditDrag ? MarkerShape.filledCircle : MarkerShape.none,
                Label = $"speed",
                Color = ColorTranslator.FromHtml("#FF7F50"),
                LineWidth = 1,
                YAxisIndex = yAixsSpeed.AxisIndex,
                MaxRenderIndex = Math.Min(LastTimeIndex, SpeedX.Length - 1),
                //IsHighlighted = false,
                //scatter.MarkerShape = MarkerShape.none;
                IsVisible = yAixsSpeed.IsVisible && SpeedShow,

            };
            scatter.Dragged += Dragged;
            scatter.MovePointFunc = MoveBetweenAdjacent;
            Oscill.Plot.Add(scatter);
            scatterSpeed = scatter;
        }
        Coordinate MoveBetweenAdjacent(List<double> xs, List<double> ys, int index, Coordinate requested)
        {
            double newX = requested.X;
            double newY = requested.Y;
            if (DragType == GraphType.Speed)
            {
                if (newY > ConfigurationSpeedMax)
                {
                    newY = ConfigurationSpeedMax;
                }
                if (newY < ConfigurationSpeedMin)
                {
                    newY = ConfigurationSpeedMin;
                    if (newY < 0)
                        newY = 0;
                }
            }
            else if (DragType == GraphType.Gradient)
            {
                if (newY > ConfigurationGradientMax)
                {
                    newY = ConfigurationGradientMax;
                }
                if (newY < ConfigurationGradientMin)
                {
                    newY = ConfigurationGradientMin;
                    if (newY < 0)
                        newY = 0;
                }
            }
            if (index == xs.Count - 1)
            {
                return new Coordinate(xs.Last(), newY);
            }
            int leftIndex = Math.Max(index - 1, 0);
            int rightIndex = Math.Min(index + 1, xs.Count - 1);



            if (IntervalAdjustEnable)
            {
                newX = Math.Max(newX, xs[leftIndex]);
                newX = Math.Min(newX, xs[rightIndex]);
            }
            else if (AutoRightAdjustEnable)
            {
                newX = Math.Max(newX, xs[leftIndex]);
                newX = Math.Min(newX, xs[rightIndex]);

                var current = xs[index];
                var r = newX - current;
                if (DragType == GraphType.Speed)
                {
                    for (int i = index + 1; i < SpeedX.Length - 1; i++)
                    {
                        if (SpeedX[i] + r > LastTimeIndex)
                            SpeedX[i] = LastTimeIndex;
                        else
                            SpeedX[i] += r;
                    }
                }
                else if (DragType == GraphType.Gradient)
                {
                    for (int i = index + 1; i < GradientX.Length - 1; i++)
                    {
                        if (GradientX[i] + r > LastTimeIndex)
                            GradientX[i] = LastTimeIndex;
                        else
                            GradientX[i] += r;
                    }
                }
            }

            return new Coordinate(newX, newY);
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
            var scatterReal = Oscill.Plot.AddScatter(SpeedRealX, SpeedRealY, label: $"speed(real)", color: ColorTranslator.FromHtml("#2F4F4F"), lineWidth: 1f);
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
            var scatterPressure = Oscill.Plot.AddScatter(PressureX, PressureY, label: $"pressure", color: ColorTranslator.FromHtml("#0076F6"), lineWidth: 1);
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
            var kks = Oscill.Plot.GetPlottables();
            if (scatterGradient != null)
                Oscill.Plot.Remove(scatterGradient);
            if (GradientX.Count() != GradientY.Count())
                return;
            var scatter = new OscillogramDraggable(GradientX, GradientY, GraphType.Gradient)
            {
                DragCursor = ScottPlot.Cursor.All,
                DragEnabled = EnableEditDrag,
                DragEnabledX = true,
                DragEnabledY = true,
                MarkerShape = EnableEditDrag ? MarkerShape.filledCircle : MarkerShape.none,
                Label = $"gradient",
                Color = ColorTranslator.FromHtml("#206864"),
                LineWidth = 1,
                YAxisIndex = yAixsGradient.AxisIndex,
                XAxisIndex = xAxis.AxisIndex,
                MaxRenderIndex = Math.Min(LastTimeIndex, GradientX.Length - 1),
                //IsHighlighted = true,
                IsVisible = yAixsGradient.IsVisible && GradientShow,

            };

            scatter.Dragged += Dragged;
            scatter.MovePointFunc = MoveBetweenAdjacent;
            Oscill.Plot.Add(scatter);
            scatterGradient = scatter;
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
            var scatterReal = Oscill.Plot.AddScatter(GradientRealX, GradientRealY, label: $"gradient(real)", color: ColorTranslator.FromHtml("#B060B0"), lineWidth: 1f);
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
            if (CurrentTimeIndex >= AllNumConst /*|| CurrentTimeIndex > LastTimeIndex*/)
                return;
            if (scatterSpectrum.Keys.Any())
            {
                for (int i = 0; i < scatterSpectrum.Keys.Count; i++)
                {
                    (scatterSpectrum[Xs[i]] as ScatterPlot)!.MaxRenderIndex = CurrentTimeIndex <= 0 ? 0 : CurrentTimeIndex - 1;
                }
            }
        }
        private void UpdatePressureData(object? sender, EventArgs e)
        {
            if (CurrentTimeIndex >= AllNumConst || CurrentTimeIndex > LastTimeIndex)
                return;
            if (scatterPressure != null)
                (scatterPressure as ScatterPlot)!.MaxRenderIndex = CurrentTimeIndex <= 0 ? 0 : CurrentTimeIndex - 1;
        }
        private void UpdateGradinetData(object? sender, EventArgs e)
        {
            //if (CurrentTimeIndex >= AllNumConst || CurrentTimeIndex > LastTimeIndex)
            //    return;
            //if (scatterGradient != null)
            //    (scatterGradient as ScatterPlot)!.MaxRenderIndex = LastTimeIndex;
        }

        private void UpdateRealGradientData(object? sender, EventArgs e)
        {
            if (CurrentTimeIndex >= AllNumConst || CurrentTimeIndex > LastTimeIndex)
                return;
            if (scatterRealGradient != null)
                (scatterRealGradient as ScatterPlot)!.MaxRenderIndex = CurrentTimeIndex <= 0 ? 0 : CurrentTimeIndex - 1;
        }

        private void UpdateSpeedData(object? sender, EventArgs e)
        {
            //if (CurrentTimeIndex >= AllNumConst || CurrentTimeIndex > LastTimeIndex)
            //    return;
            //if (scatterSpeed != null)
            //    (scatterSpeed as ScatterPlot)!.MaxRenderIndex = LastTimeIndex;
        }

        private void UpdateRealSpeedData(object? sender, EventArgs e)
        {
            if (CurrentTimeIndex >= AllNumConst || CurrentTimeIndex > LastTimeIndex)
                return;
            if (scatterRealSpeed != null)
                (scatterRealSpeed as ScatterPlot)!.MaxRenderIndex = CurrentTimeIndex <= 0 ? 0 : CurrentTimeIndex - 1;
        }
        #endregion

        public void Dispose()
        {
            _renderTimer.Stop();
            _updateDataTimer.Stop();
            _updateGradientDataTimer.Stop();
            _updateRealGradientDataTimer.Stop();
            _updateSpeedDataTimer.Stop();
            _updateRealSpeedDataTimer.Stop();
            _updatePressureDataTimer.Stop();

            _renderTimer = null!;
            _updateDataTimer = null!;
            _updateGradientDataTimer = null!;
            _updateRealGradientDataTimer = null!;
            _updateSpeedDataTimer = null!;
            _updateRealSpeedDataTimer = null!;
            _updatePressureDataTimer = null!;
        }

    }

}
