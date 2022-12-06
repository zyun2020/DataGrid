using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Windows.Foundation;
using ZyunUI.DataGridInternals;
using ZyunUI.Utilities;

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

            if (Double.IsFinite(availableSize.Width) && Double.IsFinite(availableSize.Height))
            {
                RectangleGeometry rg = new RectangleGeometry();
                rg.Rect = new Rect(0, 0, availableSize.Width, availableSize.Height);
                this.Clip = rg;
            }

            return base.MeasureOverride(availableSize);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var columns = this.OwningGrid.ColumnsInternal.GetVisibleColumns();
            Rect rcChild = new Rect();
            rcChild.X = 0;
            rcChild.Y = 0;

            double frozenLeftEdge;
            double scrollingLeftEdge;
            double cellLeftEdge;

            DataGridDisplayData displayData = this.OwningGrid.DisplayData;
            DataGridRowVisuals rowVisuals;
            DataGridCell gridCell;
            for (int i = 0; i < displayData.NumDisplayedRows; i++)
            {
                rowVisuals = displayData.GetDisplayedRow(i);

                frozenLeftEdge = 0;
                scrollingLeftEdge = -this.OwningGrid.HorizontalOffset;

                rcChild.Height = rowVisuals.DisplayHeight;
                foreach (DataGridColumn column in columns)
                {
                    gridCell = rowVisuals[column.Index];
                    bool shouldDisplayCell = ShouldDisplayCell(column, frozenLeftEdge, scrollingLeftEdge);
                    EnsureCellDisplay(gridCell, shouldDisplayCell);

                    if (column.IsFrozen)
                    {
                        cellLeftEdge = frozenLeftEdge;
                        // This can happen before or after clipping because frozen cells aren't clipped
                        frozenLeftEdge += column.ActualWidth;
                    }
                    else
                    {
                        cellLeftEdge = scrollingLeftEdge;
                        scrollingLeftEdge += column.ActualWidth;
                    }
                    
                    if (gridCell.Visibility == Visibility.Visible)
                    {
                        rcChild.X = cellLeftEdge;
                        rcChild.Width = gridCell.Width;
                        gridCell.Arrange(rcChild);
                        if (!column.IsFrozen)
                        {
                            EnsureCellClip(gridCell, column.ActualWidth, rcChild.Height, frozenLeftEdge, scrollingLeftEdge);
                        }
                    }
                }

                rcChild.Y += rcChild.Height;
            }

            return base.ArrangeOverride(finalSize);
        }

        private static void EnsureCellClip(DataGridCell cell, double width, double height, double frozenLeftEdge, double cellLeftEdge)
        {
            // Clip the cell only if it's scrolled under frozen columns.  Unfortunately, we need to clip in this case
            // because cells could be transparent
            if (frozenLeftEdge > cellLeftEdge)
            {
                RectangleGeometry rg = new RectangleGeometry();
                double xClip = Math.Round(Math.Min(width, frozenLeftEdge - cellLeftEdge));
                rg.Rect = new Rect(xClip, 0, Math.Max(0, width - xClip), height);
                cell.Clip = rg;
            }
            else
            {
                cell.Clip = null;
            }
        }

        private static void EnsureCellDisplay(DataGridCell cell, bool displayColumn)
        {
           cell.Visibility = displayColumn ? Visibility.Visible : Visibility.Collapsed; 
        }

        private bool ShouldDisplayCell(DataGridColumn column, double frozenLeftEdge, double scrollingLeftEdge)
        {
            if (column.Visibility != Visibility.Visible)
            {
                return false;
            }

            scrollingLeftEdge += this.OwningGrid.HorizontalAdjustment;
            double leftEdge = column.IsFrozen ? frozenLeftEdge : scrollingLeftEdge;
            double rightEdge = leftEdge + column.ActualWidth;
            return DoubleUtil.GreaterThan(rightEdge, 0) &&
                DoubleUtil.LessThanOrClose(leftEdge, this.OwningGrid.CellsViewWidth) &&
                DoubleUtil.GreaterThan(rightEdge, frozenLeftEdge); // scrolling column covered up by frozen column(s)
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
