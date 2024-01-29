using ScottPlot.Control;
using ScottPlot;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows;
using Cursor = ScottPlot.Cursor;
using Charting.Models;
using System.Drawing.Text;
using Charting.Extensions;

namespace Charting
{
    [TemplatePart(Name = GraphName, Type = typeof(ItemsControl))]
    [TemplatePart(Name = PlotImageName, Type = typeof(Image))]
    [TemplatePart(Name = MarinGName, Type = typeof(Grid))]
    public partial class TrayCore : UserControl
    {

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
        internal bool isHighRefresh = false;


        private System.Windows.Threading.DispatcherTimer _renderTimer;


        #region Header

        public TrayHeaderMode HeaderMode
        {
            get { return (TrayHeaderMode)GetValue(HeaderModeProperty); }
            set { SetValue(HeaderModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HeaderMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderModeProperty =
            DependencyProperty.Register("HeaderMode", typeof(TrayHeaderMode), typeof(TrayCore), new PropertyMetadata(TrayHeaderMode.None, (d, e) =>
            {
                if (((TrayCore)d).ItemsSource != null)
                {
                    var val = (TrayHeaderMode)e.NewValue;
                    foreach (var item in ((TrayCore)d).ItemsSource)
                    {
                        switch (val)
                        {
                            case TrayHeaderMode.Header:
                                if (item.Header != null)
                                    item.Header.IsVisible = true;
                                if (item.Header2 != null)
                                    item.Header2.IsVisible = false;
                                break;
                            case TrayHeaderMode.Header2:
                                if (item.Header != null)
                                    item.Header.IsVisible = false;
                                if (item.Header2 != null)
                                    item.Header2.IsVisible = true;
                                break;
                            default:
                                if (item.Header != null)
                                    item.Header.IsVisible = false;
                                if (item.Header2 != null)
                                    item.Header2.IsVisible = false;
                                break;
                        }
                    }
                }

            }));


        public ScottPlot.Drawing.Font? HeaderFont
        {
            get { return (ScottPlot.Drawing.Font?)GetValue(HeaderFontProperty); }
            set { SetValue(HeaderFontProperty, value); }
        }

        // Using a DependencyProperty as the backing store for HeadrFont.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HeaderFontProperty =
            DependencyProperty.Register("HeaderFont", typeof(ScottPlot.Drawing.Font), typeof(TrayCore), new PropertyMetadata(null, (d, e) =>
            {
                if (((TrayCore)d).ItemsSource != null)
                {
                    if (e.NewValue is ScottPlot.Drawing.Font val)
                    {
                        foreach (var item in ((TrayCore)d).ItemsSource)
                        {
                            if (item.Header != null)
                                item.Header.Font = val;
                        }
                    }
                    else
                    {
                        ScottPlot.Drawing.Font font = new ScottPlot.Drawing.Font();
                        font.Family = new System.Drawing.FontFamily(GenericFontFamilies.Serif);
                        font.Alignment = Alignment.MiddleCenter;
                        font.Color = ((TrayCore)d).Plot.GetNextColor();
                        font.Size = 20;
                        foreach (var item in ((TrayCore)d).ItemsSource)
                        {
                            if (item.Header != null)
                                item.Header.Font = font;
                        }
                    }
                }

            }));


        public ScottPlot.Drawing.Font? Header2Font
        {
            get { return (ScottPlot.Drawing.Font?)GetValue(Header2FontProperty); }
            set { SetValue(Header2FontProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Header2Font.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty Header2FontProperty =
            DependencyProperty.Register("Header2Font", typeof(ScottPlot.Drawing.Font), typeof(TrayCore), new PropertyMetadata(null, (d, e) =>
            {
                if (((TrayCore)d).ItemsSource != null)
                {
                    if (e.NewValue is ScottPlot.Drawing.Font val)
                    {
                        foreach (var item in ((TrayCore)d).ItemsSource)
                        {
                            if (item.Header2 != null)
                                item.Header2.Font = val;
                        }
                    }
                    else
                    {
                        ScottPlot.Drawing.Font font = new ScottPlot.Drawing.Font();
                        font.Alignment = Alignment.MiddleCenter;
                        font.Color = ((TrayCore)d).Plot.GetNextColor();
                        font.Size = 45;
                        font.Family = new System.Drawing.FontFamily(GenericFontFamilies.Serif);
                        foreach (var item in ((TrayCore)d).ItemsSource)
                        {
                            if (item.Header2 != null)
                                item.Header2.Font = font;
                        }
                    }
                }

            }));


        #endregion

        #region Draggable

        public bool EnablePanelDraggable
        {
            get { return (bool)GetValue(EnablePanelDraggableProperty); }
            set { SetValue(EnablePanelDraggableProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EnablePanelDraggable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnablePanelDraggableProperty =
            DependencyProperty.Register("EnablePanelDraggable", typeof(bool), typeof(TrayCore), new PropertyMetadata(false, OnEnablePanelDraggableChanged));

        private static void OnEnablePanelDraggableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var val = (bool)e.NewValue;
            if (((TrayCore)d).ItemsSource != null)
                foreach (var item in ((TrayCore)d).ItemsSource)
                {
                    if (item.Panel != null)
                        item.Panel.DragEnabled = val;
                }
        }


        public bool EnableCellDraggable
        {
            get { return (bool)GetValue(EnableCellDraggableProperty); }
            set { SetValue(EnableCellDraggableProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EnableCellDraggable.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EnableCellDraggableProperty =
            DependencyProperty.Register("EnableCellDraggable", typeof(bool), typeof(TrayCore), new PropertyMetadata(false, OnEnableCellDraggableChanged));

        private static void OnEnableCellDraggableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var val = (bool)e.NewValue;
            if (((TrayCore)d).ItemsSource != null)
                foreach (var item in ((TrayCore)d).ItemsSource)
                {
                    foreach (var cell in item.Cells)
                    {
                        cell.DragEnabled = val;
                    }
                }
        }

        #endregion

        #region ItemsSource
        public ObservableCollection<Tray> ItemsSource
        {
            get { return (ObservableCollection<Tray>)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ItemsSource.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<Tray>), typeof(TrayCore), new PropertyMetadata(default, (d, e) =>
            {
                ((TrayCore)d).OnItemsSourceChanged(d, e);

                ((TrayCore)d).ItemsSource.CollectionChanged += ((TrayCore)d).ItemsSource_CollectionChanged;
            }));

        void ItemsSource_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (Tray item in e.NewItems)
                    {
                        //ScottPlot.Drawing.Font font = new ScottPlot.Drawing.Font();
                        //font.Family = new FontFamily(GenericFontFamilies.Serif);
                        //font.Alignment = Alignment.MiddleCenter;
                        //font.Color = plot.GetNextColor();
                        //font.Size = 20;

                        Plot.AddPanel(item, HeaderMode, EnablePanelDraggable, EnableCellDraggable);
                        if (item.Header != null)
                        {
                            if (HeaderFont != null)
                            {
                                item.Header.Font = HeaderFont;
                                item.Header.IsVisible = HeaderMode == TrayHeaderMode.Header;
                            }
                        }
                        if (item.Header2 != null && Header2Font != null)
                        {
                            item.Header2.Font = Header2Font;
                            item.Header2.IsVisible = HeaderMode == TrayHeaderMode.Header2;
                        }

                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (Tray item in e.OldItems)
                    {
                        Plot.Remove(item);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Replace:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Move:
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    break;
                default:
                    break;
            }
        }

        public virtual void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is IEnumerable<Tray> tr)
            {
                Plot.Clear();
                foreach (var item in tr)
                {
                    Plot.AddPanel(item, HeaderMode, EnablePanelDraggable, EnableCellDraggable);
                    if (item.Header != null)
                    {
                        if (HeaderFont != null)
                        {
                            item.Header.Font = HeaderFont;
                            item.Header.IsVisible = HeaderMode == TrayHeaderMode.Header;
                        }
                    }
                    if (item.Header2 != null && Header2Font != null)
                    {
                        item.Header2.Font = Header2Font;
                        item.Header2.IsVisible = HeaderMode == TrayHeaderMode.Header2;
                    }
                }
            }
        }
        #endregion

        public event RoutedEventHandler IsCellOver
        {
            add
            {
                AddHandler(IsCellOverEvent, value);
            }
            remove
            {
                RemoveHandler(IsCellOverEvent, value);
            }
        }

        public static readonly RoutedEvent IsCellOverEvent
            = EventManager.RegisterRoutedEvent("IsCellOver", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TrayCore));

        public ThemesStyle Theme
        {
            get { return (ThemesStyle)GetValue(ThemeProperty); }
            set { SetValue(ThemeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Theme.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ThemeProperty =
            DependencyProperty.Register("Theme", typeof(ThemesStyle), typeof(TrayCore), new PropertyMetadata(ThemesStyle.Default, (d, e) =>
            {
                var theme = ((ThemesStyle)e.NewValue).ToString();
                var style = ScottPlot.Style.GetStyles().Where(_ => _.GetType().Name == theme).FirstOrDefault() ?? ScottPlot.Style.Default;

                ((TrayCore)d).Backend.Plot.Style(style);

                if (((TrayCore)d).ItemsSource != null)
                {
                    foreach (var item in ((TrayCore)d).ItemsSource)
                    {
                        foreach (var cell in item.Cells)
                        {
                            cell.FontColor = style.TitleFontColor;
                            //cell.Color = style.GridLineColor;
                        }
                    }
                }

            }));


        static TrayCore()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TrayCore), new FrameworkPropertyMetadata(typeof(TrayCore)));

        }
        public TrayCore()
        {
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
            RightClicked += DefaultRightClickEvent!;

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
            Configuration.Pan = false;
            Configuration.LeftClickDragPan = true;
            Configuration.AltLeftClickDragZoom = false;

            Configuration.RightClickDragZoom = false;
            Configuration.LockHorizontalAxis = false;
            Configuration.LockVerticalAxis = false;
            Configuration.MiddleClickAutoAxis = false;

            Configuration.MiddleClickDragZoom = false;
            Configuration.RightClickDragZoomFromMouseDown = false;

            Configuration.WarnIfRenderNotCalledManually = false;

            //Backend.Plot.Style(ScottPlot.Style.Blue1);
            Backend.Plot.Grid(false);
            Backend.Plot.Frameless(true);
            //Backend.Plot.Layout(-20, -20, 20, 20, 0);

            var padding = new ScottPlot.PixelPadding(
                left: -20,
                right: -20,
                bottom: -20,
                top: -10);

            Backend.Plot.ManualDataArea(padding);

            Backend.StartProcessingEvents();


            AutoZoom = true;
            DefaultMenus();



            _renderTimer = new DispatcherTimer();
            _renderTimer.Interval = TimeSpan.FromMilliseconds(20);
            _renderTimer.Tick += (sender, e) => AutoRender(false);
            _renderTimer.Start();

            Plot.AxisScaleLock(true); // this forces pixels to have 1:1 scale ratio
            Plot.SetAxisLimitsX(0, 550, 0);
            Plot.SetAxisLimitsY(0, 300, 0);

        }


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
                Cell currentCell = null!;
                Cell previousCell = null!;
                contentControl.MouseMove += (sender, e) =>
                {
                    Backend.MouseMove(GetInputState(e));
                    base.OnMouseMove(e);
                    var pixelX = e.MouseDevice.GetPosition(this).X;
                    var pixelY = e.MouseDevice.GetPosition(this).Y;

                    (double coordinateX, double coordinateY) = this.GetMouseCoordinates(0, 0);

                    var isCurrentCell = false;
                    foreach (var item in Plot.GetCelltable())
                    {
                        if (item is Cell plottable)
                        {
                            int xAxisIndex = plottable.XAxisIndex;
                            int yAxisIndex = plottable.YAxisIndex;

                            double xCoords = Plot.GetCoordinateX((float)pixelX, xAxisIndex);
                            double yCoords = Plot.GetCoordinateY((float)pixelY, yAxisIndex);
                            Coordinate c = new(xCoords, yCoords);

                            ICell hittable = (ICell)plottable;
                            if (hittable.CellTest(c))
                            {
                                plottable.BorderLineWidth = 4;
                                plottable.FontBold = true;
                                currentCell = plottable;
                                isCurrentCell = true;
                            }
                            else
                            {
                                plottable.BorderLineWidth = 1;
                                plottable.FontBold = false;
                            }
                        }
                    }
                    if (!isCurrentCell)
                        currentCell = null!;

                    if (previousCell != currentCell)
                    {
                        previousCell = currentCell!;
                        RoutedEventArgs args = new RoutedEventArgs(IsCellOverEvent, currentCell);
                        base.RaiseEvent(args);
                    }
                    this.Refresh(false);
                };
                contentControl.MouseUp += (sender, e) =>
                {
                    Backend.MouseUp(GetInputState(e));
                    ReleaseMouseCapture();
                };
                contentControl.MouseWheel += (sender, e) =>
                {

                    if (Keyboard.IsKeyDown(Key.LeftCtrl))
                        foreach (var item in ItemsSource)
                        {
                            if (item is Tray tray)
                            {
                                //if (Configuration.MiddleClickDragZoom)
                                foreach (var cell in tray.Cells)
                                {
                                    cell.FontSize += (float)e.Delta * 0.01f;
                                }
                            }
                        }
                    else
                    {
                        Backend.MouseWheel(GetInputState(e, e.Delta));
                        AutoZoom = false;
                    }
                };

                contentControl.MouseEnter += (sender, e) =>
                {
                    isHighRefresh = true;
                    base.OnMouseEnter(e);
                };
                contentControl.MouseLeave += (sender, e) =>
                {
                    isHighRefresh = false;
                    //if (Mouse.Captured == contentControl)
                    //    Mouse.Capture(null);
                    base.OnMouseLeave(e);
                };
                contentControl.MouseLeftButtonDown += (object sender, MouseButtonEventArgs e) =>
                {
                    (double coordinateX, double coordinateY) = GetMouseCoordinates(0, 0);
                    var pixelX = e.MouseDevice.GetPosition(this).X;
                    var pixelY = e.MouseDevice.GetPosition(this).Y;
                    var dr = GetDraggable(pixelX, pixelY, 30);
                    if (dr != null && dr.DragEnabled)
                    {
                        //if (dr is IGraphType graphType)
                        //    CurrentDraggableGraph.CurrentDraggableGraphType = graphType.GraphType;
                        //else
                        //    CurrentDraggableGraph.CurrentDraggableGraphType = GraphType.GraphType;
                    }
                };

            }
            if (GetTemplateChild(MarinGName) is Grid grid)
            {
                grid.SizeChanged += (sender, e) =>
                {
                    //var width = Panels.Min(_ => _.Panel.Rectangle.XMax - _.Panel.Rectangle.XMin);
                    //var height = Panels.Min(_ => _.Panel.Rectangle.YMax - _.Panel.Rectangle.YMin);
                    //var red = (float)(width / height);


                    Backend.Resize((float)e.NewSize.Width, (float)e.NewSize.Height, useDelayedRendering: true);
                    //Backend.Resize(ScaledWidth, ScaledHeight, useDelayedRendering: true);
                    Backend.Plot.AxisAuto();
                };

            }
        }
        #endregion

    }
}
