using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Windows.Foundation;
using ZyunUI.DataGridInternals;

namespace ZyunUI
{
    internal sealed class DataGridCellsPresenter : Panel
    {
        private double _preManipulationHorizontalOffset;
        private double _preManipulationVerticalOffset;
        public DataGridCellsPresenter()
        {
            this.ManipulationStarting += new ManipulationStartingEventHandler(DataGridRowsPresenter_ManipulationStarting);
            this.ManipulationStarted += new ManipulationStartedEventHandler(DataGridRowsPresenter_ManipulationStarted);
            this.ManipulationDelta += new ManipulationDeltaEventHandler(DataGridRowsPresenter_ManipulationDelta);
        }

        internal DataGrid OwningGrid
        {
            get;
            set;
        }

      
        protected override Size MeasureOverride(Size availableSize)
        {
            OwningGrid.OnPendingVerticalScroll();

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

        private void DataGridRowsPresenter_ManipulationStarting(object sender, ManipulationStartingRoutedEventArgs e)
        {
            if (this.OwningGrid != null)
            { 
                _preManipulationHorizontalOffset = this.OwningGrid.HorizontalOffset;
                _preManipulationVerticalOffset = this.OwningGrid.VerticalOffset;
            }
        }

        private void DataGridRowsPresenter_ManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            if (e.PointerDeviceType != Microsoft.UI.Input.PointerDeviceType.Touch)
            {
                e.Complete();
            }
        }

        private void DataGridRowsPresenter_ManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            if (this.OwningGrid != null)
            {
                e.Handled =
                    this.OwningGrid.ProcessScrollOffsetDelta(_preManipulationHorizontalOffset - e.Cumulative.Translation.X - this.OwningGrid.HorizontalOffset, true /*isForHorizontalScroll*/) ||
                    this.OwningGrid.ProcessScrollOffsetDelta(_preManipulationVerticalOffset - e.Cumulative.Translation.Y - this.OwningGrid.VerticalOffset, false /*isForHorizontalScroll*/);
            }
        }

    }
}
