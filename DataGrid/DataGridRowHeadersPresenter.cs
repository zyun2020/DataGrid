using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using ZyunUI.DataGridInternals;

namespace ZyunUI
{
    internal sealed class DataGridRowHeadersPresenter : Panel
    {
        public DataGridRowHeadersPresenter()
        {
        }

        internal DataGrid OwningGrid
        {
            get;
            set;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (this.OwningGrid == null)
            {
                return base.MeasureOverride(availableSize);
            }

            if (!OwningGrid.AreRowHeadersVisible || this.OwningGrid.DisplayData.NumDisplayedRows == 0)
            {
                return new Size(0.0, 0.0);
            }

            Size  size = new Size(this.OwningGrid.ActualRowHeaderWidth, OwningGrid.CellsViewHeight);
            RectangleGeometry rg = new RectangleGeometry();
            rg.Rect = new Rect(0, 0, size.Width, size.Height);
            this.Clip = rg;

            return size;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (OwningGrid.AreRowHeadersVisible)
            {
                DataGridDisplayData displayData = this.OwningGrid.DisplayData;
                DataGridRowVisuals rowVisuals;
                DataGridRowHeader rowHeader;

                Rect child = new Rect(0, -OwningGrid.NegVerticalOffset, finalSize.Width, 0);
                for (int i = 0; i < displayData.NumDisplayedRows; i++)
                {
                    rowVisuals = displayData.GetDisplayedRow(i);
                    rowHeader = rowVisuals.HeaderCell;

                    child.Height = rowVisuals.DisplayHeight;
                    rowHeader.Arrange(child);
                    child.Y += child.Height;
                }
            }
            return finalSize;
        }
    }
}
