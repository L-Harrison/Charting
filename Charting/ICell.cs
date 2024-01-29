using ScottPlot;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Charting
{
    public interface ICell
    {
        /// <summary>
        /// Cursor to display when the Plottable is under the mouse
        /// </summary>
        Cursor CellCursor { get; set; }

        /// <summary>
        /// Returns true if the Plottable is at the given coordinate
        /// </summary>
        bool CellTest(Coordinate coord);

        /// <summary>
        /// Controls whether logic inside <see cref="CellTest(Coordinate)"/> will run (or always return false).
        /// </summary>
        bool CellTestEnabled { get; set; }
    }
}
