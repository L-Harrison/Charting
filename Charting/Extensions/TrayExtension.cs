using Charting.Models;
using ScottPlot;

using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charting.Extensions
{
    public static class TrayExtension
    {
        public static Tray AddPanel(this Plot plot, Tray tray, TrayHeaderMode headerMode, bool enablePanelDraggable, bool enableCellDraggable)
        {
            var maxX = (tray.Cols + 1) * tray.CellSize;
            var maxY = -(tray.Rows + 1) * tray.CellSize;

            #region Header
            ScottPlot.Drawing.Font font = new ScottPlot.Drawing.Font();
            font.Family = new FontFamily(GenericFontFamilies.Serif);
            font.Alignment = Alignment.MiddleCenter;
            font.Color = plot.GetNextColor();
            font.Size = 12;

            var xHeader = tray.XPanel + tray.PanelWidth / 2;
            var yHeader = tray.YPanel + (tray.XRadius + tray.YRadius) / 2;
            tray.HeaderTitle = tray.HeaderTitle ?? $"Plate {tray.Index + 1}";
            tray.Header = plot.AddText(tray.HeaderTitle, xHeader, yHeader, font);
            tray.Header.IsVisible = headerMode == TrayHeaderMode.Header;
            #endregion

            #region Header2
            font = new ScottPlot.Drawing.Font();
            font.Alignment = Alignment.MiddleCenter;
            font.Color = /*Color.LightSlateGray;//*/ plot.GetNextColor();
            font.Size = 45;
            font.Family = new FontFamily(GenericFontFamilies.Serif);

            xHeader = tray.XPanel + tray.PanelWidth / 2;
            yHeader = tray.YPanel + maxY / 2;
            tray.Header2Title = tray.Header2Title ?? $"Plate {tray.Index + 1}";
            tray.Header2 = plot.AddText(tray.Header2Title, xHeader, yHeader, font);
            tray.Header2.IsVisible = headerMode == TrayHeaderMode.Header2;
            #endregion

            CoordinateRect rect = new(tray.XPanel, tray.XPanel + maxX, tray.YPanel, tray.YPanel + maxY);
            CPanel plottable = new(rect)
            {
                BorderColor = System.Drawing.Color.LightGray,
                DragEnabled = enablePanelDraggable,
                Color = System.Drawing.Color.Transparent,
            };
            plot.Add(plottable);
            plottable.Dragged += (object? sender, EventArgs e) =>
            {
                if (e is DraggedEventArgs rect)
                {
                    //plot.Remove(tray.Header);
                    tray.Header.X = rect.CoordinateRect.XMin + tray.PanelWidth / 2;
                    tray.Header.Y = rect.CoordinateRect.YMax + (tray.XRadius + tray.YRadius) / 2;
                    //tray.Header = plot.AddText(tray.HeaderTitle, x, y, tray.Header.Font);
                    //tray.Header.IsVisible = headerMode == TrayHeaderMode.Header;


                    //plot.Remove(tray.Header2);
                    tray.Header2.X = rect.CoordinateRect.XMin + tray.PanelWidth / 2;
                    tray.Header2.Y = rect.CoordinateRect.YMax + maxY / 2;
                    //tray.Header2 = plot.AddText(tray.Header2Title, x1, y1, tray.Header2.Font);
                    //tray.Header2.IsVisible = headerMode == TrayHeaderMode.Header2;

                    //var ep = e as DraggedEventArgs;
                    for (int i = 0; i < tray.Cells.Count; i++)
                    {
                        //plot.Remove(tray.Cells[i]);

                        var item = tray.Cells[i];
                        item.X = (item.ColValue + 1) * tray.CellSize + rect.CoordinateRect.XMin;
                        item.Y = item.RowValue * tray.CellSize + rect.CoordinateRect.YMin;

                    }

                }

            };

            tray.Panel = plottable;

            foreach (var item in (tray.Rows, tray.Cols).BuilderCells())
            {
                var cX = (item.currentCol + 1) * tray.CellSize + tray.XPanel;
                var cY = (item.currentRow * tray.CellSize) * -1;

                //var font = new ScottPlot.Drawing.Font() { Size = size, Color = fontColor ?? plot.GetNextColor() };
                //if (plot.GetSettings().DataBackground.Color != ScottPlot.Style.Default.DataBackgroundColor)
                //{
                //    font.Color = ScottPlot.Style.Black.TickLabelColor;
                //}

                var cell = plot.AddCell(
                   label: item.cellName,
                   x: cX,
                   y: cY,
                   xRadius: tray.XRadius,
                   yRadius: tray.YRadius,
                   size: 11,
                   fontColor: plot.GetLabeColor(plot.GetSettings().DataBackground.Color)?.TickLabelColor,
                   color: ColorTranslator.FromHtml("#17BECF"),
                   borderColor: ColorTranslator.FromHtml(htmlColor: "#000000"),
                   lineWidth: 1,
                   default);
                cell.Index = item.index;
                cell.Name = item.cellName;
                cell.Id = Guid.NewGuid();
                cell.RowValue = item.currentRow;
                cell.ColValue = item.currentCol;

                cell.Alignment = Alignment.MiddleCenter;
                cell.DragEnabled = enablePanelDraggable;
                cell.XAxisIndex = 0;
                cell.YAxisIndex = 0;

                tray.Cells.Add(cell);
            }
            return tray;

        }

        public static void Remove(this Plot plot, Tray tray)
        {
            plot.Remove(tray.Header);
            plot.Remove(tray.Header2);
            plot.Remove(tray.Panel);
            foreach (var item in tray.Cells)
            {
                plot.Remove(item);
            }
            tray = null!;
        }

    }
}
