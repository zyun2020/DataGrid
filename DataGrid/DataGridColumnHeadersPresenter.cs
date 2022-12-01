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

    }
}
