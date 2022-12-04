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
using ZyunUI.Utilities;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ZyunUI
{
    public sealed class DataGridCell : ContentControl
    {
        public DataGridCell()
        {
            this.DefaultStyleKey = typeof(DataGridCell);
        }

        internal void SetContentDataContext(object dataContext)
        {
            FrameworkElement element = this.Content as FrameworkElement;
            if (element != null) element.DataContext = dataContext;
            else this.DataContext = dataContext;
        }

        internal void EnsureStyle(Style previousStyle)
        {
            if (this.Style != null &&
                (this.OwningColumn == null || this.Style != this.OwningColumn.CellStyle) &&
                (this.OwningGrid == null || this.Style != this.OwningGrid.CellStyle) &&
                this.Style != previousStyle)
            {
                return;
            }

            Style style = null;
            if (this.OwningColumn != null)
            {
                style = this.OwningColumn.CellStyle;
            }

            if (style == null && this.OwningGrid != null)
            {
                style = this.OwningGrid.CellStyle;
            }

            this.SetStyleWithType(style);
        }

        internal int ColumnIndex
        {
            get
            {
                if (this.OwningColumn == null)
                {
                    return -1;
                }

                return this.OwningColumn.Index;
            }
        }

        internal DataGridColumn OwningColumn
        {
            get;
            set;
        }

        internal DataGrid OwningGrid
        {
            get
            {
                if (this.OwningColumn != null)
                {
                    return this.OwningColumn.OwningGrid;
                }

                return null;
            }
        }

      
        internal int RowIndex
        {
            get;
            set;
        }

        private DataGridInteractionInfo InteractionInfo
        {
            get;
            set;
        }

    }
}
