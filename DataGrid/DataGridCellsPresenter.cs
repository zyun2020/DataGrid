using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Windows.Foundation;

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
            //var rows = OwningGrid.Rows;
            //DataGridRow row;
            //var columns = OwningGrid.Columns;
            //DataGridColumn column;

            //FrameworkElement element;
            //AdvancedCollectionView collectionView = OwningGrid.CollectionView;
            //Children.Clear();

            //Rect rcChild = new Rect();

            //rcChild.X = m_offsetX;
            //rcChild.Y = m_offsetY;

            //for (int i = m_nStartRow; i < rows.Count; i++)
            //{
            //    row = rows[i];
            //    rcChild.Height = row.ActualHeight;

            //    for (int j = 0; j < columns.Count; j++)
            //    {
            //        column = columns[i];
            //        element = OwningGrid.CreateDisplayControl(column);
            //        element.DataContext = collectionView[i];

            //        DataGridCell gridCell = new DataGridCell();
            //        gridCell.Content = element;

            //        rcChild.Width = column.ActualWidth;
            //        gridCell.Arrange(rcChild);
            //        Children.Add(gridCell);

            //        rcChild.X += rcChild.Width;
            //    }
            //    rcChild.Y += rcChild.Height;
            //}

            return base.ArrangeOverride(finalSize);
        }

       
    }
}
