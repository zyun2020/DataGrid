using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using ZyunUI.DataGridInternals;
using ZyunUI.Utilities;

using DiagnosticsDebug = System.Diagnostics.Debug;
using DispatcherQueuePriority = Microsoft.UI.Dispatching.DispatcherQueuePriority;

namespace ZyunUI
{
    public partial class DataGrid
    {
        private PropertyInfo[] _dataProperties;
        private Type _itemDataType;

        /// <summary>
        /// Handles changes to the items collection.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        private void CollectionView_VectorChanged(Windows.Foundation.Collections.IObservableVector<object> sender,
            Windows.Foundation.Collections.IVectorChangedEventArgs @event)
        {
            this.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () => {
                if (@event.CollectionChange == Windows.Foundation.Collections.CollectionChange.Reset)
                {
                    Reset();
                }
                else if (@event.CollectionChange == Windows.Foundation.Collections.CollectionChange.ItemInserted)
                {
                    ItemInserted(@event.Index);
                }
                else if (@event.CollectionChange == Windows.Foundation.Collections.CollectionChange.ItemRemoved)
                {
                    ItemRemoved(@event.Index);
                }
                else if (@event.CollectionChange == Windows.Foundation.Collections.CollectionChange.ItemChanged)
                {
                    ItemChanged(@event.Index);
                }
            });
        }

        private void Reset()
        {
            Rows.Clear();
            for (var i = 0; i < CollectionView.Count; i++)
            {
                Rows.Add(new DataGridRow(i));
            }
        }

       

        private void ItemInserted(uint index)
        {

        }

        private void ItemRemoved(uint index)
        {

        }

        private void ItemChanged(uint index)
        {

        }

        internal Type ItemDataType
        {
            get
            {
                // We need to use the raw ItemsSource as opposed to DataSource because DataSource
                // may be the ItemsSource wrapped in a collection view, in which case we wouldn't
                // be able to take T to be the type if we're given IEnumerable<T>
                if (_itemDataType == null && ItemsSource != null)
                {
                    _itemDataType = ItemsSource.GetItemType();
                }

                return _itemDataType;
            }
        }

        public bool DataIsPrimitive
        {
            get
            {
                return DataTypeIsPrimitive(this.ItemDataType);
            }
        }

        internal PropertyInfo[] DataProperties
        {
            get
            {
                if (_dataProperties == null)
                {
                    UpdateDataProperties();
                }

                return _dataProperties;
            }
        }

        internal void UpdateDataProperties()
        {
            Type dataType = this.ItemDataType;

            if (this.ItemsSource != null && dataType != null && !DataTypeIsPrimitive(dataType))
            {
                _dataProperties = dataType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                DiagnosticsDebug.Assert(_dataProperties != null, "Expected non-null _dataProperties.");
            }
            else
            {
                _dataProperties = null;
            }
        }

        internal static bool DataTypeIsPrimitive(Type dataType)
        {
            if (dataType != null)
            {
                Type type = TypeHelper.GetNonNullableType(dataType);  // no-opt if dataType isn't nullable
                return
                    type.GetTypeInfo().IsPrimitive ||
                    type == typeof(string) ||
                    type == typeof(decimal) ||
                    type == typeof(DateTime);
            }
            else
            {
                return false;
            }
        }

        public bool AllowEdit
        {
            get
            {
                if (this.ItemsSource == null)
                {
                    return false;
                }
                else
                {
                    return !this.ItemsSource.IsReadOnly;
                }
            }
        }

        public static bool CanEdit(Type type)
        {
            DiagnosticsDebug.Assert(type != null, "Expected non-null type.");

            type = type.GetNonNullableType();

            return
                type.GetTypeInfo().IsEnum
                || type == typeof(string)
                || type == typeof(char)
                || type == typeof(bool)
                || type == typeof(byte)
                || type == typeof(sbyte)
                || type == typeof(float)
                || type == typeof(double)
                || type == typeof(decimal)
                || type == typeof(short)
                || type == typeof(int)
                || type == typeof(long)
                || type == typeof(ushort)
                || type == typeof(uint)
                || type == typeof(ulong)
                || type == typeof(DateTime);
        }

        public bool GetPropertyIsReadOnly(string propertyName)
        {
            if (this.ItemDataType != null)
            {
                if (!string.IsNullOrEmpty(propertyName))
                {
                    Type propertyType = this.ItemDataType;
                    PropertyInfo propertyInfo = null;
                    List<string> propertyNames = TypeHelper.SplitPropertyPath(propertyName);
                    for (int i = 0; i < propertyNames.Count; i++)
                    {
                        if (propertyType.GetTypeInfo().GetIsReadOnly())
                        {
                            return true;
                        }

                        propertyInfo = propertyType.GetPropertyOrIndexer(propertyNames[i], out _);
                        if (propertyInfo == null || propertyInfo.GetIsReadOnly())
                        {
                            // Either the property doesn't exist or it does exist but is read-only.
                            return true;
                        }

                        // Check if EditableAttribute is defined on the property and if it indicates uneditable
                        var editableAttribute = propertyInfo.GetCustomAttributes().OfType<EditableAttribute>().FirstOrDefault();
                        if (editableAttribute != null && !editableAttribute.AllowEdit)
                        {
                            return true;
                        }

                        propertyType = propertyInfo.PropertyType.GetNonNullableType();
                    }

                    return propertyInfo == null || !propertyInfo.CanWrite || !this.AllowEdit || !CanEdit(propertyType);
                }
                else
                {
                    if (this.ItemDataType.GetTypeInfo().GetIsReadOnly())
                    {
                        return true;
                    }
                }
            }

            return !this.AllowEdit;
        }

        private double GetRowActualHeight(int rowIndex)
        {
            DiagnosticsDebug.Assert(rowIndex >= 0 && rowIndex < RowCount, "Expected positive rowIndex.");
            return Rows[rowIndex].ActualHeight;
        }
        internal DataGridRowVisuals GenerateRow(int rowIndex)
        {
            DiagnosticsDebug.Assert(rowIndex >= 0, "Expected positive rowIndex.");

            object dataItem = null;
            if (rowIndex < CollectionView.Count)
                dataItem = CollectionView[rowIndex];

            DataGridRowVisuals row = DisplayData.GetUsedRow();
            if (row == null)
            {
                row = new DataGridRowVisuals();

                FrameworkElement element;
                var columns = Columns;
                DataGridColumn dataGridColumn;
                for (int i = 0; i < columns.Count; i++)
                {
                    dataGridColumn = columns[i];

                    DataGridCell dataGridCell = new DataGridCell();
                    element = dataGridColumn.GenerateElementInternal(dataGridCell, dataItem);
                    element.SetStyleWithType(dataGridColumn.CellStyle);
                    dataGridCell.Content = element;
                    dataGridCell.Width = dataGridColumn.ActualWidth;
                    row.Insert(i, dataGridCell);
                }
                
            }
            
            row.DisplayHeight = Rows[rowIndex].ActualHeight;

            if (AreRowHeadersVisible)
            {
                DataGridRowHeader headerCell = row.HeaderCell;
                if (headerCell == null)
                {
                    headerCell = row.CreateHeaderCell(RowHeaderColumn.CellStyle);
                }
                TextBlock textBlock = RowHeaderColumn.GenerateRowHeader(dataItem);
                if (RowHeaderColumn.Binding == null)
                {
                    textBlock.Text = CellRef.ToRowName(rowIndex);
                }
                headerCell.Height = Rows[rowIndex].ActualHeight;
            }
            row.UpdateDataContext(dataItem);

            return row;
        }

        internal void InsertDisplayedElement(int displayRow, DataGridRowVisuals rowVisuals)
        {
            if (_rowHeadersPresenter != null && AreRowHeadersVisible)
            {
                rowVisuals.HeaderCell.Visibility = Visibility.Visible;
                _rowHeadersPresenter.Children.Insert(displayRow, rowVisuals.HeaderCell);
            }

            int cellIndex = displayRow * rowVisuals.CellCount;
            if (_cellsPresenter != null)
            { 
                for(int i = 0; i < rowVisuals.CellCount; i++)
                {
                    _cellsPresenter.Children.Insert(cellIndex + i, rowVisuals[i]);
                }
            }
        }

        internal void RemoveDisplayedElement(int displayRow, DataGridRowVisuals rowVisuals)
        {
            if (_rowHeadersPresenter != null)
            {
                DiagnosticsDebug.Assert(_rowHeadersPresenter.Children[displayRow] == rowVisuals.HeaderCell, "RemoveDisplayedElement row header.");
                _rowHeadersPresenter.Children.RemoveAt(displayRow);
            }

            int cellIndex = displayRow * rowVisuals.CellCount;
            if (_cellsPresenter != null)
            {
                for (int i = 0; i < rowVisuals.CellCount; i++)
                {
                    DiagnosticsDebug.Assert(_cellsPresenter.Children[cellIndex + i] == rowVisuals[cellIndex + i], "RemoveDisplayedElement cells.");
                    _cellsPresenter.Children.RemoveAt(cellIndex + i);
                }
            }
        }

        internal void RemoveAllDisplayedElement()
        {
            if (_rowHeadersPresenter != null)
            {
                _rowHeadersPresenter.Children.Clear();
            }
             
            if (_cellsPresenter != null)
            {
                _cellsPresenter.Children.Clear();
            }
        }

        private void UpdateDisplayedRows(int newFirstDisplayedRow, double displayHeight)
        {
            int firstDisplayedScrollingRow = newFirstDisplayedRow;
            int lastDisplayedScrollingRow = -1;
            double deltaY = -this.NegVerticalOffset;
            int visibleScrollingRows = 0;

            if (DoubleUtil.LessThanOrClose(displayHeight, 0) || this.RowCount == 0 || this.ColumnsItemsInternal.Count == 0)
            {
                return;
            }

            if (firstDisplayedScrollingRow == -1)
            {
                // 0 is fine because the element in the first slot cannot be collapsed
                firstDisplayedScrollingRow = 0;
            }

            int rowIndex = firstDisplayedScrollingRow;
            while (rowIndex < this.RowCount && DoubleUtil.LessThan(deltaY, displayHeight))
            {
                deltaY += Rows[rowIndex].ActualHeight;
                visibleScrollingRows++;
                lastDisplayedScrollingRow = rowIndex;
                rowIndex++;
            }

            rowIndex = firstDisplayedScrollingRow - 1;
            while (rowIndex >= 0 && DoubleUtil.LessThan(deltaY, displayHeight))
            {
                deltaY += Rows[rowIndex].ActualHeight;
                visibleScrollingRows++;
                firstDisplayedScrollingRow = rowIndex;
                rowIndex--;
            }

            // If we're up to the first row, and we still have room left, uncover as much of the first row as we can
            if (firstDisplayedScrollingRow == 0 && DoubleUtil.LessThan(deltaY, displayHeight))
            {
                double newNegVerticalOffset = Math.Max(0, this.NegVerticalOffset - displayHeight + deltaY);
                deltaY += this.NegVerticalOffset - newNegVerticalOffset;
                this.NegVerticalOffset = newNegVerticalOffset;
            }

            if (visibleScrollingRows == 0)
            {
                firstDisplayedScrollingRow = -1;
                DiagnosticsDebug.Assert(lastDisplayedScrollingRow == -1, "Expected lastDisplayedScrollingSlot equal to -1.");
            }
 
            DisplayData.UpdateDisplayedRows(firstDisplayedScrollingRow, lastDisplayedScrollingRow);
        }

       
    }
   
}