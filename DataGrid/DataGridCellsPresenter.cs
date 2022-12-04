using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Windows.Foundation;
using ZyunUI.DataGridInternals;

namespace ZyunUI
{
    internal sealed class DataGridCellsPresenter : Panel
    {
        public DataGridCellsPresenter()
        {
        }

        internal DataGrid OwningGrid
        {
            get;
            set;
        }

      
        protected override Size MeasureOverride(Size availableSize)
        {
            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var columns = this.OwningGrid.ColumnsInternal.GetVisibleColumns();
            Rect rcChild = new Rect();
            rcChild.X = 0;
            rcChild.Y = 0;

            DataGridDisplayData displayData = this.OwningGrid.DisplayData;
            DataGridRowVisuals rowVisuals;
            DataGridCell gridCell;
            for (int i = 0; i < displayData.NumDisplayedRows; i++)
            {
                rowVisuals = displayData.GetDisplayedRow(i);

                rcChild.Height = rowVisuals.DisplayHeight;
                rcChild.X = 0;

                for (int k = 0; k < rowVisuals.CellCount; k++)
                {
                    gridCell = rowVisuals[k];

                    rcChild.Width = gridCell.Width;
                    gridCell.Arrange(rcChild);

                    rcChild.X += rcChild.Width;
                }

                rcChild.Y += rcChild.Height;
            }

            return base.ArrangeOverride(finalSize);
        }

       
    }
}
