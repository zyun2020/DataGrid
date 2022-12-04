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
using DiagnosticsDebug = System.Diagnostics.Debug;

namespace ZyunUI
{
    internal sealed class DataGridColumnHeadersPresenter : Panel
    {
        private Control _dragIndicator;
        private Control _dropLocationIndicator;

        /// <summary>
        /// Gets or sets which column is currently being dragged.
        /// </summary>
        internal DataGridColumn DragColumn
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the current drag indicator control.  This value is null if no column is being dragged.
        /// </summary>
        internal Control DragIndicator
        {
            get
            {
                return _dragIndicator;
            }

            set
            {
                if (value != _dragIndicator)
                {
                    if (this.Children.Contains(_dragIndicator))
                    {
                        this.Children.Remove(_dragIndicator);
                    }

                    _dragIndicator = value;
                    if (_dragIndicator != null)
                    {
                        this.Children.Add(_dragIndicator);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the distance, in pixels, that the DragIndicator should be positioned away from the corresponding DragColumn.
        /// </summary>
        internal double DragIndicatorOffset
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the drop location indicator control.  This value is null if no column is being dragged.
        /// </summary>
        internal Control DropLocationIndicator
        {
            get
            {
                return _dropLocationIndicator;
            }

            set
            {
                if (value != _dropLocationIndicator)
                {
                    if (this.Children.Contains(_dropLocationIndicator))
                    {
                        this.Children.Remove(_dropLocationIndicator);
                    }

                    _dropLocationIndicator = value;
                    if (_dropLocationIndicator != null)
                    {
                        this.Children.Add(_dropLocationIndicator);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the distance, in pixels, that the drop location indicator should be positioned away from the left edge
        /// of the ColumnsHeaderPresenter.
        /// </summary>
        internal double DropLocationIndicatorOffset
        {
            get;
            set;
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

            if (!this.OwningGrid.AreColumnHeadersVisible)
            {
                return new Size(0.0, 0.0);
            }

            return new Size(this.OwningGrid.ColumnsInternal.VisibleEdgedColumnsWidth, this.OwningGrid.ActualColumnHeaderHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (this.OwningGrid == null)
            {
                return base.ArrangeOverride(finalSize);
            }

            double dragIndicatorLeftEdge = 0;
            double frozenLeftEdge = 0;
            double scrollingLeftEdge = -this.OwningGrid.HorizontalOffset;
            foreach (DataGridColumn dataGridColumn in this.OwningGrid.ColumnsInternal.GetVisibleColumns())
            {
                DataGridColumnHeader columnHeader = dataGridColumn.HeaderCell;
                DiagnosticsDebug.Assert(columnHeader.OwningColumn == dataGridColumn, "Expected columnHeader owned by dataGridColumn.");

                if (dataGridColumn.IsFrozen)
                {
                    columnHeader.Arrange(new Rect(frozenLeftEdge, 0, dataGridColumn.ActualWidth, finalSize.Height));
                    columnHeader.Clip = null; // The layout system could have clipped this because it's not aware of our render transform
                    if (this.DragColumn == dataGridColumn && this.DragIndicator != null)
                    {
                        dragIndicatorLeftEdge = frozenLeftEdge + this.DragIndicatorOffset;
                    }

                    frozenLeftEdge += dataGridColumn.ActualWidth;
                }
                else
                {
                    columnHeader.Arrange(new Rect(scrollingLeftEdge, 0, dataGridColumn.ActualWidth, finalSize.Height));
                    EnsureColumnHeaderClip(columnHeader, dataGridColumn.ActualWidth, finalSize.Height, frozenLeftEdge, scrollingLeftEdge);
                    if (this.DragColumn == dataGridColumn && this.DragIndicator != null)
                    {
                        dragIndicatorLeftEdge = scrollingLeftEdge + this.DragIndicatorOffset;
                    }
                }

                scrollingLeftEdge += dataGridColumn.ActualWidth;
            }

            // This needs to be updated after the filler column is configured
            DataGridColumn lastVisibleColumn = this.OwningGrid.ColumnsInternal.LastVisibleColumn;
            if (lastVisibleColumn != null)
            {
                lastVisibleColumn.HeaderCell.UpdateSeparatorVisibility(lastVisibleColumn);
            }

            return finalSize;
        }

        private static void EnsureColumnHeaderClip(DataGridColumnHeader columnHeader, double width, double height, double frozenLeftEdge, double columnHeaderLeftEdge)
        {
            // Clip the cell only if it's scrolled under frozen columns.  Unfortunately, we need to clip in this case
            // because cells could be transparent
            if (frozenLeftEdge > columnHeaderLeftEdge)
            {
                RectangleGeometry rg = new RectangleGeometry();
                double xClip = Math.Min(width, frozenLeftEdge - columnHeaderLeftEdge);
                rg.Rect = new Rect(xClip, 0, width - xClip, height);
                columnHeader.Clip = rg;
            }
            else
            {
                columnHeader.Clip = null;
            }
        }

    }
}
