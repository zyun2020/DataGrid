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

            if (this.OwningGrid.DisplayData.NumDisplayedRows == 0)
            {
                return new Size(0.0, 0.0);
            }

            return new Size(this.OwningGrid.ColumnsInternal.VisibleEdgedColumnsWidth, this.OwningGrid.ActualColumnHeaderHeight);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            return finalSize;
        }
    }
}
