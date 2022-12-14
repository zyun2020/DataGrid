using System;
using Windows.ApplicationModel.Activation;

namespace ZyunUI
{
    /// <summary>
    /// Represents a range of cells.
    /// </summary>
    public class GridCellRange
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridCellRange"/> class.
        /// </summary>
        /// <param name="cell1">The cell1.</param>
        /// <param name="cell2">The cell2.</param>
        public GridCellRange(GridCellRef cell1, GridCellRef cell2)
        {
            this.TopLeft = new GridCellRef(Math.Min(cell1.Row, cell2.Row), Math.Min(cell1.Column, cell2.Column));
            this.BottomRight = new GridCellRef(Math.Max(cell1.Row, cell2.Row), Math.Max(cell1.Column, cell2.Column));
        }

        public GridCellRange(int row1, int row2, int col1, int col2)
        {
            this.TopLeft = new GridCellRef(Math.Min(row1, row2), Math.Min(col1, col2));
            this.BottomRight = new GridCellRef(Math.Max(row1, row2), Math.Max(col1, col2));
        }

        public bool IsContained(GridCellRef cell)
        {
            if(cell.Column >= LeftColumn && cell.Column <= RightColumn &&
                cell.Row >= TopRow && cell.Row <= BottomRow)
            {
                return true;
            }
            return false;
        }

        internal void SetColumn(int minColumn, int maxColumn)
        {
            this.TopLeft = new GridCellRef(this.TopLeft.Row, minColumn);
            this.BottomRight = new GridCellRef(this.BottomRight.Row, maxColumn);
        }

        internal void SetRow(int minRow, int maxRow)
        {
            this.TopLeft = new GridCellRef(minRow, this.TopLeft.Column);
            this.BottomRight = new GridCellRef(maxRow, this.BottomRight.Column);
        }

        /// <summary>
        /// Gets the top left cell.
        /// </summary>
        /// <value>
        /// The top left cell reference.
        /// </value>
        public GridCellRef TopLeft { get; private set; }

        /// <summary>
        /// Gets the bottom right cell.
        /// </summary>
        /// <value>
        /// The bottom right cell reference.
        /// </value>
        public GridCellRef BottomRight { get; private set; }

        /// <summary>
        /// Gets the index of the top row.
        /// </summary>
        /// <value>
        /// The zero-based index.
        /// </value>
        public int TopRow => this.TopLeft.Row;

        /// <summary>
        /// Gets the index of the bottom row.
        /// </summary>
        /// <value>
        /// The zero-based index.
        /// </value>
        public int BottomRow => this.BottomRight.Row;

        /// <summary>
        /// Gets the index of the left column.
        /// </summary>
        /// <value>
        /// The zero-based index.
        /// </value>
        public int LeftColumn => this.TopLeft.Column;

        /// <summary>
        /// Gets the index of the right column.
        /// </summary>
        /// <value>
        /// The zero-based index.
        /// </value>
        public int RightColumn => this.BottomRight.Column;

        /// <summary>
        /// Gets the number of rows.
        /// </summary>
        /// <value>
        /// The number of rows.
        /// </value>
        public int Rows => this.BottomRow - this.TopRow + 1;

        /// <summary>
        /// Gets the number of columns.
        /// </summary>
        /// <value>
        /// The number of columns.
        /// </value>
        public int Columns => this.RightColumn - this.LeftColumn + 1;

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return $"{this.TopLeft}:{this.BottomRight}";
        }
    }
}