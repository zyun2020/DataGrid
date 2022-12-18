using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using ZyunUI.Utilities;
using ZyunUI.DataGridInternals;

using DiagnosticsDebug = System.Diagnostics.Debug;

namespace ZyunUI
{
    /// <summary>
    /// Control to represent data in columns and rows.
    /// </summary>
    public partial class DataGrid
    {
        /// <summary>
        /// OnColumnDisplayIndexChanged
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected virtual void OnColumnDisplayIndexChanged(DataGridColumnEventArgs e)
        {
            this.ColumnDisplayIndexChanged?.Invoke(this, e);
        }

        /// <summary>
        /// OnColumnReordered
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected internal virtual void OnColumnReordered(DataGridColumnEventArgs e)
        {
            this.EnsureVerticalGridLines();

            this.ColumnReordered?.Invoke(this, e);
        }

        /// <summary>
        /// OnColumnReordering
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected internal virtual void OnColumnReordering(DataGridColumnReorderingEventArgs e)
        {
            this.ColumnReordering?.Invoke(this, e);
        }

        /// <summary>
        /// OnColumnSorting
        /// </summary>
        /// <param name="e">Event arguments.</param>
        protected internal virtual void OnColumnSorting(DataGridColumnEventArgs e)
        {
            this.Sorting?.Invoke(this, e);
        }

        private void EnsureColumnHeadersVisibility()
        {
            if (_columnHeadersPresenter != null)
            {
                _columnHeadersPresenter.Visibility = this.AreColumnHeadersVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void EnsureVerticalGridLines()
        {
            if (this.AreColumnHeadersVisible)
            {
                double totalColumnsWidth = 0;
                foreach (DataGridColumn column in this.ColumnsInternal)
                {
                    totalColumnsWidth += column.ActualWidth;

                    column.HeaderCell.SeparatorVisibility = (column != this.ColumnsInternal.LastVisibleColumn || totalColumnsWidth < this.CellsViewWidth) ?
                        Visibility.Visible : Visibility.Collapsed;
                }
            }

            if(_cellsPresenter != null)
            {
                _cellsPresenter.EnsureGridLines();
            }
        }

        // Returns the column's width
        internal static double GetEdgedColumnWidth(DataGridColumn dataGridColumn)
        {
            DiagnosticsDebug.Assert(dataGridColumn != null, "Expected non-null dataGridColumn.");
            return dataGridColumn.ActualWidth;
        }

        internal bool ColumnRequiresRightGridLine(DataGridColumn dataGridColumn, bool includeLastRightGridLineWhenPresent)
        {
            return (this.GridLinesVisibility == DataGridGridLinesVisibility.Vertical || this.GridLinesVisibility == DataGridGridLinesVisibility.All) && this.VerticalGridLinesBrush != null &&
                   (dataGridColumn != this.ColumnsInternal.LastVisibleColumn || includeLastRightGridLineWhenPresent);
        }

        internal DataGridColumnCollection CreateColumnsInstance()
        {
            return new DataGridColumnCollection(this);
        }

        private void InsertDisplayedColumnHeader(DataGridColumn dataGridColumn)
        {
            DiagnosticsDebug.Assert(dataGridColumn != null, "Expected non-null dataGridColumn.");
            if (_columnHeadersPresenter != null)
            {
                dataGridColumn.HeaderCell.Visibility = dataGridColumn.Visibility;
                DiagnosticsDebug.Assert(!_columnHeadersPresenter.Children.Contains(dataGridColumn.HeaderCell), "Expected dataGridColumn.HeaderCell not contained in _columnHeadersPresenter.Children.");
                _columnHeadersPresenter.Children.Insert(dataGridColumn.DisplayIndex, dataGridColumn.HeaderCell);
            }
        }

        private void RemoveDisplayedColumnHeader(DataGridColumn dataGridColumn)
        {
            if (_columnHeadersPresenter != null)
            {
                _columnHeadersPresenter.Children.Remove(dataGridColumn.HeaderCell);
            }
        }

        private void RemoveDisplayedColumnHeaders()
        {
            if (_columnHeadersPresenter != null)
            {
                _columnHeadersPresenter.Children.Clear();
            }
        }

        internal bool GetColumnReadOnlyState(DataGridColumn dataGridColumn, bool isReadOnly)
        {
            DiagnosticsDebug.Assert(dataGridColumn != null, "Expected non-null dataGridColumn.");

            DataGridBoundColumn dataGridBoundColumn = dataGridColumn as DataGridBoundColumn;
            if (dataGridBoundColumn != null && dataGridBoundColumn.Binding != null)
            {
                string path = null;
                if (dataGridBoundColumn.Binding.Path != null)
                {
                    path = dataGridBoundColumn.Binding.Path.Path;
                }

                if (!string.IsNullOrEmpty(path))
                {
                    return this.GetPropertyIsReadOnly(path) || isReadOnly;
                }
            }

            return isReadOnly;
        }

    
        internal void OnClearingColumns()
        {
            // Rows need to be cleared first. There cannot be rows without also having columns.
            ClearRows();

            // Removing all the column header cells
            RemoveDisplayedColumnHeaders();

            _horizontalOffset = _negHorizontalOffset = 0;

            if (_hScrollBar != null && _hScrollBar.Visibility == Visibility.Visible)
            {
                _hScrollBar.Value = 0;
            }
        }

        /// <summary>
        /// Invalidates the widths of all columns because the resizing behavior of an individual column has changed.
        /// </summary>
        /// <param name="column">Column with CanUserResize property that has changed.</param>
        internal void OnColumnCanUserResizeChanged(DataGridColumn column)
        {
            if (column.IsVisible)
            {
                EnsureHorizontalLayout();
            }
        }

        internal void OnColumnCellStyleChanged(DataGridColumn column, Style previousStyle)
        {
            // Set HeaderCell.Style for displayed rows if HeaderCell.Style is not already set
            foreach (DataGridRowVisuals row in DisplayData.GetAllRows())
            {
                row[column.Index].EnsureStyle(previousStyle);
            }
            Refresh(true, false);
        }

        internal void OnColumnCollectionChanged_PostNotification(bool columnsGrew)
        {
            Refresh(true, false);
        }

        internal void OnColumnCollectionChanged_PreNotification(bool columnsGrew)
        {
           
        }

        internal void OnColumnDisplayIndexChanged(DataGridColumn dataGridColumn)
        {
            DiagnosticsDebug.Assert(dataGridColumn != null, "Expected non-null dataGridColumn.");
            DataGridColumnEventArgs e = new DataGridColumnEventArgs(dataGridColumn);

            OnColumnDisplayIndexChanged(e); 
        }

        internal void OnColumnDisplayIndexChanged_PostNotification()
        {
            // Notifications for adjusted display indexes.
            FlushDisplayIndexChanged(true /*raiseEvent*/);

            // Our displayed columns may have changed so recompute them
            UpdateDisplayedColumns();

            // Invalidate layout
            CorrectColumnFrozenStates();
            //EnsureHorizontalLayout();
        }

        internal void OnColumnDisplayIndexChanging(DataGridColumn targetColumn, int newDisplayIndex)
        {
            DiagnosticsDebug.Assert(targetColumn != null, "Expected non-null targetColumn.");
            DiagnosticsDebug.Assert(newDisplayIndex != targetColumn.DisplayIndex, "Expected newDisplayIndex other than targetColumn.DisplayIndexWithFiller.");

            if (InDisplayIndexAdjustments)
            {
                // We are within columns display indexes adjustments. We do not allow changing display indexes while adjusting them.
                throw DataGridError.DataGrid.CannotChangeColumnCollectionWhileAdjustingDisplayIndexes();
            }

            try
            {
                InDisplayIndexAdjustments = true;

                bool trackChange = true;
                DataGridColumn column;

                // Move is legal - let's adjust the affected display indexes.
                if (newDisplayIndex < targetColumn.DisplayIndex)
                {
                    // DisplayIndex decreases. All columns with newDisplayIndex <= DisplayIndex < targetColumn.DisplayIndex
                    // get their DisplayIndex incremented.
                    for (int i = newDisplayIndex; i < targetColumn.DisplayIndex; i++)
                    {
                        column = this.ColumnsInternal.GetColumnAtDisplayIndex(i);
                        column.DisplayIndex = column.DisplayIndex + 1;
                        if (trackChange)
                        {
                            column.DisplayIndexHasChanged = true; // OnColumnDisplayIndexChanged needs to be raised later on
                        }
                    }
                }
                else
                {
                    // DisplayIndex increases. All columns with targetColumn.DisplayIndex < DisplayIndex <= newDisplayIndex
                    // get their DisplayIndex decremented.
                    for (int i = newDisplayIndex; i > targetColumn.DisplayIndex; i--)
                    {
                        column = this.ColumnsInternal.GetColumnAtDisplayIndex(i);
                        column.DisplayIndex = column.DisplayIndex - 1;
                        if (trackChange)
                        {
                            column.DisplayIndexHasChanged = true; // OnColumnDisplayIndexChanged needs to be raised later on
                        }
                    }
                }

                // Now let's actually change the order of the DisplayIndexMap
                if (targetColumn.DisplayIndex != -1)
                {
                    this.ColumnsInternal.DisplayIndexMap.Remove(targetColumn.Index);
                }

                this.ColumnsInternal.DisplayIndexMap.Insert(newDisplayIndex, targetColumn.Index);
            }
            finally
            {
                InDisplayIndexAdjustments = false;
            }

            // Note that displayIndex of moved column is updated by caller.
        }

        internal void OnColumnBindingChanged(DataGridBoundColumn column)
        {
            // Update Binding in Displayed rows by regenerating the affected elements
            if (_cellsPresenter != null)
            {
                //foreach (DataGridRow row in GetAllRows())
                //{
                //    PopulateCellContent(false /*isCellEdited*/, column, row, row.Cells[column.Index]);
                //}
            }
        }

        internal void OnColumnElementStyleChanged(DataGridBoundColumn column)
        {
            // Update Element Style in Displayed rows
            //foreach (DataGridRow row in GetAllRows())
            //{
            //    FrameworkElement element = column.GetCellContent(row);
            //    if (element != null)
            //    {
            //        element.SetStyleWithType(column.ElementStyle);
            //    }
            //}

            //InvalidateRowHeightEstimate();
        }

        internal void OnColumnHeaderDragStarted(DragStartedEventArgs e)
        {
            if (this.ColumnHeaderDragStarted != null)
            {
                this.ColumnHeaderDragStarted(this, e);
            }
        }

        internal void OnColumnHeaderDragDelta(DragDeltaEventArgs e)
        {
            if (this.ColumnHeaderDragDelta != null)
            {
                this.ColumnHeaderDragDelta(this, e);
            }
        }

        internal void OnColumnHeaderDragCompleted(DragCompletedEventArgs e)
        {
            if (this.ColumnHeaderDragCompleted != null)
            {
                this.ColumnHeaderDragCompleted(this, e);
            }
        }

        /// <summary>
        /// Adjusts the specified column's width according to its new maximum value.
        /// </summary>
        /// <param name="column">The column to adjust.</param>
        /// <param name="oldValue">The old ActualMaxWidth of the column.</param>
        internal void OnColumnMaxWidthChanged(DataGridColumn column, double oldValue)
        {
            DiagnosticsDebug.Assert(column != null, "Expected non-null column.");

            if (column.Visibility == Visibility.Visible && oldValue != column.ActualMaxWidth)
            {
                
            }
        }

        /// <summary>
        /// Adjusts the specified column's width according to its new minimum value.
        /// </summary>
        /// <param name="column">The column to adjust.</param>
        /// <param name="oldValue">The old ActualMinWidth of the column.</param>
        internal void OnColumnMinWidthChanged(DataGridColumn column, double oldValue)
        {
            DiagnosticsDebug.Assert(column != null, "Expected non-null column.");

            if (column.Visibility == Visibility.Visible && oldValue != column.ActualMinWidth)
            {
                 
            }
        }

        internal void OnColumnReadOnlyStateChanging(DataGridColumn dataGridColumn, bool isReadOnly)
        {
            DiagnosticsDebug.Assert(dataGridColumn != null, "Expected non-null dataGridColumn.");
            if (isReadOnly && this.CurrentColumnIndex == dataGridColumn.Index)
            {
                // Edited column becomes read-only. Exit editing mode.
                if (!EndCellEdit(DataGridEditAction.Commit, true /*exitEditingMode*/, this.ContainsFocus /*keepFocus*/, true /*raiseEvents*/))
                {
                    EndCellEdit(DataGridEditAction.Cancel, true /*exitEditingMode*/, this.ContainsFocus /*keepFocus*/, false /*raiseEvents*/);
                }
            }
        }

        internal void OnColumnVisibleStateChanged(DataGridColumn updatedColumn)
        {
            DiagnosticsDebug.Assert(updatedColumn != null, "Expected non-null updatedColumn.");

           
        }

        internal void OnColumnVisibleStateChanging(DataGridColumn targetColumn)
        {
            DiagnosticsDebug.Assert(targetColumn != null, "Expected non-null targetColumn.");

            if (targetColumn.IsVisible)
            {
                // Column of the current cell is made invisible. Trying to move the current cell to a neighbor column. May throw an exception.
                DataGridColumn dataGridColumn = this.ColumnsInternal.GetNextVisibleColumn(targetColumn);
                if (dataGridColumn == null)
                {
                    dataGridColumn = this.ColumnsInternal.GetPreviousVisibleColumn(targetColumn);
                }

                if (dataGridColumn == null)
                {
                    //SetCurrentCellCore(-1, -1);
                }
                else
                {
                    //SetCurrentCellCore(dataGridColumn.Index, this.CurrentSlot);
                }
            }
        }

        internal void OnColumnWidthChanged(DataGridColumn updatedColumn)
        {
            DiagnosticsDebug.Assert(updatedColumn != null, "Expected non-null updatedColumn.");
            if (updatedColumn.IsVisible)
            {
                Refresh(true, false);
            }
        }

        internal void OnInsertedColumn_PostNotification(GridCellRef newCurrentCellCoordinates, int newDisplayIndex)
        {
            // Update current cell if needed
            if (newCurrentCellCoordinates.Column != -1)
            {
                DiagnosticsDebug.Assert(this.CurrentColumnIndex == -1, "Expected CurrentColumnIndex equals -1.");
                CurrentCell = newCurrentCellCoordinates;

                if (newDisplayIndex < this.FrozenColumnCount)
                {
                    CorrectColumnFrozenStates();
                }
            }
        }

        internal void OnInsertedColumn_PreNotification(DataGridColumn insertedColumn)
        {
            // Fix the Index of all following columns
            CorrectColumnIndexesAfterInsertion(insertedColumn, 1);

            DiagnosticsDebug.Assert(insertedColumn.Index >= 0, "Expected positive insertedColumn.Index.");
            DiagnosticsDebug.Assert(insertedColumn.Index < this.ColumnsItemsInternal.Count, "insertedColumn.Index smaller than ColumnsItemsInternal.Count.");
            DiagnosticsDebug.Assert(insertedColumn.OwningGrid == this, "Expected insertedColumn.OwningGrid equals this DataGrid.");

            CorrectColumnDisplayIndexesAfterInsertion(insertedColumn);

            InsertDisplayedColumnHeader(insertedColumn);
  
            DataGridBoundColumn boundColumn = insertedColumn as DataGridBoundColumn;
            if (boundColumn != null && !boundColumn.IsAutoGenerated)
            {
                boundColumn.SetHeaderFromBinding();
            }

            ClearRows();
            Refresh(true, false);
        }

        internal GridCellRef OnInsertingColumn(int columnIndexInserted, DataGridColumn insertColumn)
        {
            GridCellRef newCurrentCellCoordinates;
            DiagnosticsDebug.Assert(insertColumn != null, "Expected non-null insertColumn.");

            if (insertColumn.OwningGrid != null)
            {
                throw DataGridError.DataGrid.ColumnCannotBeReassignedToDifferentDataGrid();
            }

            // Reset current cell if there is one, no matter the relative position of the columns involved
            if (this.CurrentColumnIndex != -1)
            {
                newCurrentCellCoordinates = new GridCellRef(
                    columnIndexInserted <= this.CurrentColumnIndex ? this.CurrentColumnIndex + 1 : this.CurrentColumnIndex,
                    this.CurrentRowIndex);
                ResetCurrentCellCore();
            }
            else
            {
                newCurrentCellCoordinates = new GridCellRef(-1, -1);
            }

            return newCurrentCellCoordinates;
        }

        internal void OnRemovedColumn_PostNotification(GridCellRef newCurrentCellCoordinates)
        {
            // Update current cell if needed
            if (newCurrentCellCoordinates.Column != -1)
            {
                DiagnosticsDebug.Assert(this.CurrentColumnIndex == -1, "Expected CurrentColumnIndex equals -1.");
                CurrentCell = newCurrentCellCoordinates;
            }
        }

        internal void OnRemovedColumn_PreNotification(DataGridColumn removedColumn)
        {
            DiagnosticsDebug.Assert(removedColumn.Index >= 0, "Expected positive removedColumn.Index.");
            DiagnosticsDebug.Assert(removedColumn.OwningGrid == null, "Expected null removedColumn.OwningGrid.");

            // Intentionally keep the DisplayIndex intact after detaching the column.
            CorrectColumnIndexesAfterDeletion(removedColumn);

            CorrectColumnDisplayIndexesAfterDeletion(removedColumn);

            // If the detached column was frozen, a new column needs to take its place
            if (removedColumn.IsFrozen)
            {
                removedColumn.IsFrozen = false;
                CorrectColumnFrozenStates();
            }

            UpdateDisplayedColumns();
            RemoveDisplayedColumnHeader(removedColumn);

            ClearRows();
            Refresh(true, false);
        }

        internal GridCellRef OnRemovingColumn(DataGridColumn dataGridColumn)
        {
            DiagnosticsDebug.Assert(dataGridColumn != null, "Expected non-null dataGridColumn.");
            DiagnosticsDebug.Assert(dataGridColumn.Index >= 0, "Expected positive dataGridColumn.Index.");
            DiagnosticsDebug.Assert(dataGridColumn.Index < this.ColumnsItemsInternal.Count, "Expected dataGridColumn.Index smaller than ColumnsItemsInternal.Count.");

            GridCellRef newCurrentCellCoordinates;

            int columnIndex = dataGridColumn.Index;

            // Reset the current cell's address if there is one.
            if (this.CurrentColumnIndex != -1)
            {
                int newCurrentColumnIndex = this.CurrentColumnIndex;
                if (columnIndex == newCurrentColumnIndex)
                {
                    DataGridColumn dataGridColumnNext = this.ColumnsInternal.GetNextVisibleColumn(this.ColumnsItemsInternal[columnIndex]);
                    if (dataGridColumnNext != null)
                    {
                        if (dataGridColumnNext.Index > columnIndex)
                        {
                            newCurrentColumnIndex = dataGridColumnNext.Index - 1;
                        }
                        else
                        {
                            newCurrentColumnIndex = dataGridColumnNext.Index;
                        }
                    }
                    else
                    {
                        DataGridColumn dataGridColumnPrevious = this.ColumnsInternal.GetPreviousVisibleColumn(this.ColumnsItemsInternal[columnIndex]);
                        if (dataGridColumnPrevious != null)
                        {
                            if (dataGridColumnPrevious.Index > columnIndex)
                            {
                                newCurrentColumnIndex = dataGridColumnPrevious.Index - 1;
                            }
                            else
                            {
                                newCurrentColumnIndex = dataGridColumnPrevious.Index;
                            }
                        }
                        else
                        {
                            newCurrentColumnIndex = -1;
                        }
                    }
                }
                else if (columnIndex < newCurrentColumnIndex)
                {
                    newCurrentColumnIndex--;
                }

                newCurrentCellCoordinates = new GridCellRef(newCurrentColumnIndex, (newCurrentColumnIndex == -1) ? -1 : this.CurrentRowIndex);
                if (columnIndex == this.CurrentColumnIndex)
                {
                    // If the commit fails, force a cancel edit
                    if (!this.CommitEdit(false /*exitEditingMode*/))
                    {
                        this.CancelEdit(false /*raiseEvents*/);
                    }
                }
            }
            else
            {
                newCurrentCellCoordinates = new GridCellRef(-1, -1);
            }

            // If the last column is removed, delete all the rows first.
            if (this.ColumnsItemsInternal.Count == 1)
            {
                ClearRows();
            }

            // Is deleted column scrolled off screen?
            if (dataGridColumn.IsVisible &&
                !dataGridColumn.IsFrozen &&
                this.DisplayData.FirstDisplayedCol >= 0)
            {
                // Deleted column is part of scrolling columns.
                if (this.DisplayData.FirstDisplayedCol == dataGridColumn.Index)
                {
                    // Deleted column is first scrolling column
                    _horizontalOffset -= _negHorizontalOffset;
                    _negHorizontalOffset = 0;
                }
                else if (!this.ColumnsInternal.DisplayInOrder(this.DisplayData.FirstDisplayedCol, dataGridColumn.Index))
                {
                    // Deleted column is displayed before first scrolling column
                    DiagnosticsDebug.Assert(_horizontalOffset >= GetEdgedColumnWidth(dataGridColumn), "Expected _horizontalOffset greater than or equal to GetEdgedColumnWidth(dataGridColumn).");
                    _horizontalOffset -= GetEdgedColumnWidth(dataGridColumn);
                }

                if (_hScrollBar != null && _hScrollBar.Visibility == Visibility.Visible)
                {
                    _hScrollBar.Value = _horizontalOffset;
                }
            }

            return newCurrentCellCoordinates;
        }


        /// <summary>
        /// Called when a column property changes, and its cells need to adjust that column change.
        /// </summary>
        internal void RefreshColumnElements(DataGridColumn dataGridColumn, string propertyName)
        {
            DiagnosticsDebug.Assert(dataGridColumn != null, "Expected non-null dataGridColumn.");

            ClearRows();
            Refresh(true, false);
        }


        private static DataGridAutoGeneratingColumnEventArgs GenerateColumn(Type propertyType, string propertyName, string header)
        {
            // Create a new DataBoundColumn for the Property
            DataGridBoundColumn newColumn = GetDataGridColumnFromType(propertyType);
            Binding binding = new Binding();
            binding.Path = new PropertyPath(propertyName);
            newColumn.Binding = binding;
            newColumn.Header = header;
            newColumn.IsAutoGenerated = true;
            return new DataGridAutoGeneratingColumnEventArgs(propertyName, propertyType, newColumn);
        }

        private static DataGridBoundColumn GetDataGridColumnFromType(Type type)
        {
            DiagnosticsDebug.Assert(type != null, "Expected non-null type.");
            if (type == typeof(bool))
            {
                return new DataGridCheckBoxColumn();
            }
            else if (type == typeof(bool?))
            {
                DataGridCheckBoxColumn column = new DataGridCheckBoxColumn();
                column.IsThreeState = true;
                return column;
            }

            return new DataGridTextColumn();
        }


        private bool AddGeneratedColumn(DataGridAutoGeneratingColumnEventArgs e)
        {
            // Raise the AutoGeneratingColumn event in case the user wants to Cancel or Replace the
            // column being generated
            //OnAutoGeneratingColumn(e);
            if (e.Cancel)
            {
                return false;
            }
            else
            {
                if (e.Column != null)
                {
                    // Set the IsAutoGenerated flag here in case the user provides a custom auto-generated column
                    e.Column.IsAutoGenerated = true;
                }

                this.ColumnsInternal.Add(e.Column);
                this.ColumnsInternal.AutogeneratedColumnCount++;
                return true;
            }
        }

        private byte _autoGeneratingColumnOperationCount = 0;
        private void AutoGenerateColumnsPrivate()
        {
            if (!_measured || (_autoGeneratingColumnOperationCount > 0))
            {
                // Reading the DataType when we generate columns could cause the CollectionView to
                // raise a Reset if its Enumeration changed.  In that case, we don't want to generate again.
                return;
            }

            _autoGeneratingColumnOperationCount++;
            try
            {
                // Always remove existing auto-generated columns before generating new ones
                RemoveAutoGeneratedColumns();
                GenerateColumnsFromProperties();
            }
            finally
            {
                _autoGeneratingColumnOperationCount--;
            }
        }

        private void GenerateColumnsFromProperties()
        {
            // Auto-generated Columns are added at the end so the user columns appear first
            if (this.DataProperties != null && this.DataProperties.Length > 0)
            {
                List<KeyValuePair<int, DataGridAutoGeneratingColumnEventArgs>> columnOrderPairs = new List<KeyValuePair<int, DataGridAutoGeneratingColumnEventArgs>>();

                // Generate the columns
                foreach (PropertyInfo propertyInfo in this.DataProperties)
                {
                    string columnHeader = propertyInfo.Name;
                    int columnOrder = DATAGRID_defaultColumnDisplayOrder;

                    // Check if DisplayAttribute is defined on the property
                    DisplayAttribute displayAttribute = propertyInfo.GetCustomAttributes().OfType<DisplayAttribute>().FirstOrDefault();
                    if (displayAttribute != null)
                    {
                        bool? autoGenerateField = displayAttribute.GetAutoGenerateField();
                        if (autoGenerateField.HasValue && autoGenerateField.Value == false)
                        {
                            // Abort column generation because we aren't supposed to auto-generate this field
                            continue;
                        }

                        string header = displayAttribute.GetShortName();
                        if (header != null)
                        {
                            columnHeader = header;
                        }

                        int? order = displayAttribute.GetOrder();
                        if (order.HasValue)
                        {
                            columnOrder = order.Value;
                        }
                    }

                    // Generate a single column and determine its relative order
                    int insertIndex = 0;
                    if (columnOrder == int.MaxValue)
                    {
                        insertIndex = columnOrderPairs.Count;
                    }
                    else
                    {
                        foreach (KeyValuePair<int, DataGridAutoGeneratingColumnEventArgs> columnOrderPair in columnOrderPairs)
                        {
                            if (columnOrderPair.Key > columnOrder)
                            {
                                break;
                            }

                            insertIndex++;
                        }
                    }

                    DataGridAutoGeneratingColumnEventArgs columnArgs = GenerateColumn(propertyInfo.PropertyType, propertyInfo.Name, columnHeader);
                    columnOrderPairs.Insert(insertIndex, new KeyValuePair<int, DataGridAutoGeneratingColumnEventArgs>(columnOrder, columnArgs));
                }

                // Add the columns to the DataGrid in the correct order
                foreach (KeyValuePair<int, DataGridAutoGeneratingColumnEventArgs> columnOrderPair in columnOrderPairs)
                {
                    AddGeneratedColumn(columnOrderPair.Value);
                }
            }
            else if (this.DataIsPrimitive)
            {
                AddGeneratedColumn(GenerateColumn(this.ItemDataType, string.Empty, this.ItemDataType.Name));
            }
        }

        private bool ComputeDisplayedColumns()
        {
            bool invalidate = false;
            int visibleScrollingColumnsTmp = 0;
            double displayWidth = this.CellsViewWidth;
            double cx = 0;
            int firstDisplayedFrozenCol = -1;
            int firstDisplayedScrollingCol = this.DisplayData.FirstDisplayedCol;

            // the same problem with negative numbers:
            // if the width passed in is negative, then return 0
            if (displayWidth <= 0 || this.ColumnsInternal.VisibleColumnCount == 0)
            {
                this.DisplayData.FirstDisplayedCol = -1;
                this.DisplayData.LastDisplayedCol = -1;
                return invalidate;
            }

            foreach (DataGridColumn dataGridColumn in this.ColumnsInternal.GetVisibleFrozenColumns())
            {
                if (firstDisplayedFrozenCol == -1)
                {
                    firstDisplayedFrozenCol = dataGridColumn.Index;
                }

                cx += GetEdgedColumnWidth(dataGridColumn);
                if (cx >= displayWidth)
                {
                    break;
                }
            }

            DiagnosticsDebug.Assert(cx <= this.ColumnsInternal.GetVisibleFrozenEdgedColumnsWidth(), "cx smaller than or equal to ColumnsInternal.GetVisibleFrozenEdgedColumnsWidth().");

            if (cx < displayWidth && firstDisplayedScrollingCol >= 0)
            {
                DataGridColumn dataGridColumn = this.ColumnsItemsInternal[firstDisplayedScrollingCol];
                if (dataGridColumn.IsFrozen)
                {
                    dataGridColumn = this.ColumnsInternal.FirstVisibleScrollingColumn;
                    _negHorizontalOffset = 0;
                    if (dataGridColumn == null)
                    {
                        this.DisplayData.FirstDisplayedCol = this.DisplayData.LastDisplayedCol = -1;
                        return invalidate;
                    }
                    else
                    {
                        firstDisplayedScrollingCol = dataGridColumn.Index;
                    }
                }

                cx -= _negHorizontalOffset;
                while (cx < displayWidth && dataGridColumn != null)
                {
                    cx += GetEdgedColumnWidth(dataGridColumn);
                    visibleScrollingColumnsTmp++;
                    dataGridColumn = this.ColumnsInternal.GetNextVisibleColumn(dataGridColumn);
                }

                var numVisibleScrollingCols = visibleScrollingColumnsTmp;

                // if we inflate the data area then we paint columns to the left of firstDisplayedScrollingCol
                if (cx < displayWidth)
                {
                    DiagnosticsDebug.Assert(firstDisplayedScrollingCol >= 0, "Expected positive firstDisplayedScrollingCol.");

                    // first minimize value of _negHorizontalOffset
                    if (_negHorizontalOffset > 0)
                    {
                        invalidate = true;
                        if (displayWidth - cx > _negHorizontalOffset)
                        {
                            cx += _negHorizontalOffset;
                            _horizontalOffset -= _negHorizontalOffset;
                            if (_horizontalOffset < DATAGRID_roundingDelta)
                            {
                                // Snap to zero to avoid trying to partially scroll in first scrolled off column below
                                _horizontalOffset = 0;
                            }

                            _negHorizontalOffset = 0;
                        }
                        else
                        {
                            _horizontalOffset -= displayWidth - cx;
                            _negHorizontalOffset -= displayWidth - cx;
                            cx = displayWidth;
                        }

                    }

                    // second try to scroll entire columns
                    if (cx < displayWidth && _horizontalOffset > 0)
                    {
                        DiagnosticsDebug.Assert(_negHorizontalOffset == 0, "Expected _negHorizontalOffset equals 0.");
                        dataGridColumn = this.ColumnsInternal.GetPreviousVisibleScrollingColumn(this.ColumnsItemsInternal[firstDisplayedScrollingCol]);
                        while (dataGridColumn != null && cx + GetEdgedColumnWidth(dataGridColumn) <= displayWidth)
                        {
                            cx += GetEdgedColumnWidth(dataGridColumn);
                            visibleScrollingColumnsTmp++;
                            invalidate = true;
                            firstDisplayedScrollingCol = dataGridColumn.Index;
                            _horizontalOffset -= GetEdgedColumnWidth(dataGridColumn);
                            dataGridColumn = this.ColumnsInternal.GetPreviousVisibleScrollingColumn(dataGridColumn);
                        }
                    }

                    // third try to partially scroll in first scrolled off column
                    if (cx < displayWidth && _horizontalOffset > 0)
                    {
                        DiagnosticsDebug.Assert(_negHorizontalOffset == 0, "Expected _negHorizontalOffset equals 0.");
                        dataGridColumn = this.ColumnsInternal.GetPreviousVisibleScrollingColumn(this.ColumnsItemsInternal[firstDisplayedScrollingCol]);
                        DiagnosticsDebug.Assert(dataGridColumn != null, "Expected non-null dataGridColumn.");
                        DiagnosticsDebug.Assert(GetEdgedColumnWidth(dataGridColumn) > displayWidth - cx, "Expected GetEdgedColumnWidth(dataGridColumn) greater than displayWidth - cx.");
                        firstDisplayedScrollingCol = dataGridColumn.Index;
                        _negHorizontalOffset = GetEdgedColumnWidth(dataGridColumn) - displayWidth + cx;
                        _horizontalOffset -= displayWidth - cx;
                        visibleScrollingColumnsTmp++;
                        invalidate = true;
                        cx = displayWidth;
                        DiagnosticsDebug.Assert(_negHorizontalOffset == GetNegHorizontalOffsetFromHorizontalOffset(_horizontalOffset), "Expected _negHorizontalOffset equals GetNegHorizontalOffsetFromHorizontalOffset(_horizontalOffset).");
                    }

                    // update the number of visible columns to the new reality
                    DiagnosticsDebug.Assert(numVisibleScrollingCols <= visibleScrollingColumnsTmp, "Expected numVisibleScrollingCols less than or equal to visibleScrollingColumnsTmp.");
                    numVisibleScrollingCols = visibleScrollingColumnsTmp;
                }

                int jumpFromFirstVisibleScrollingCol = numVisibleScrollingCols - 1;
                if (cx > displayWidth)
                {
                    jumpFromFirstVisibleScrollingCol--;
                }

                DiagnosticsDebug.Assert(jumpFromFirstVisibleScrollingCol >= -1, "Expected jumpFromFirstVisibleScrollingCol greater than or equal to -1.");

                if (jumpFromFirstVisibleScrollingCol < 0)
                {
                    this.DisplayData.LastDisplayedCol = -1; // no totally visible scrolling column at all
                }
                else
                {
                    DiagnosticsDebug.Assert(firstDisplayedScrollingCol >= 0, "Expected positive firstDisplayedScrollingCol.");
                    dataGridColumn = this.ColumnsItemsInternal[firstDisplayedScrollingCol];
                    for (int jump = 0; jump < jumpFromFirstVisibleScrollingCol; jump++)
                    {
                        dataGridColumn = this.ColumnsInternal.GetNextVisibleColumn(dataGridColumn);
                        DiagnosticsDebug.Assert(dataGridColumn != null, "Expected non-null dataGridColumn.");
                    }

                    this.DisplayData.LastDisplayedCol = dataGridColumn.Index;
                }
            }
            else
            {
                this.DisplayData.LastDisplayedCol = -1;
            }

            this.DisplayData.FirstDisplayedCol = firstDisplayedScrollingCol;

            return invalidate;
        }

        private int ComputeFirstVisibleScrollingColumn()
        {
            if (this.ColumnsInternal.GetVisibleFrozenEdgedColumnsWidth() >= this.CellsViewWidth)
            {
                // Not enough room for scrolling columns.
                _negHorizontalOffset = 0;
                return -1;
            }

            DataGridColumn dataGridColumn = this.ColumnsInternal.FirstVisibleScrollingColumn;

            if (_horizontalOffset == 0)
            {
                _negHorizontalOffset = 0;
                return (dataGridColumn == null) ? -1 : dataGridColumn.Index;
            }

            double cx = 0;
            while (dataGridColumn != null)
            {
                cx += GetEdgedColumnWidth(dataGridColumn);
                if (cx > _horizontalOffset)
                {
                    break;
                }

                dataGridColumn = this.ColumnsInternal.GetNextVisibleColumn(dataGridColumn);
            }

            if (dataGridColumn == null)
            {
                DiagnosticsDebug.Assert(cx <= _horizontalOffset, "Expected cx less than or equal to _horizontalOffset.");
                dataGridColumn = this.ColumnsInternal.FirstVisibleScrollingColumn;
                if (dataGridColumn == null)
                {
                    _negHorizontalOffset = 0;
                    return -1;
                }
                else
                {
                    if (_negHorizontalOffset != _horizontalOffset)
                    {
                        _negHorizontalOffset = 0;
                    }

                    return dataGridColumn.Index;
                }
            }
            else
            {
                _negHorizontalOffset = GetEdgedColumnWidth(dataGridColumn) - (cx - _horizontalOffset);
                return dataGridColumn.Index;
            }
        }

        private void RemoveAutoGeneratedColumns()
        {
            int index = 0;
            _autoGeneratingColumnOperationCount++;
            try
            {
                while (index < this.ColumnsInternal.Count)
                {
                    // Skip over the user columns
                    while (index < this.ColumnsInternal.Count && !this.ColumnsInternal[index].IsAutoGenerated)
                    {
                        index++;
                    }

                    // Remove the auto-generated columns
                    while (index < this.ColumnsInternal.Count && this.ColumnsInternal[index].IsAutoGenerated)
                    {
                        this.ColumnsInternal.RemoveAt(index);
                    }
                }

                this.ColumnsInternal.AutogeneratedColumnCount = 0;
            }
            finally
            {
                _autoGeneratingColumnOperationCount--;
            }
        }
 

        private void CorrectColumnDisplayIndexesAfterDeletion(DataGridColumn deletedColumn)
        {
            // Column indexes have already been adjusted.
            // This column has already been detached and has retained its old Index and DisplayIndex
            DiagnosticsDebug.Assert(deletedColumn != null, "Expected non-null deletedColumn.");
            DiagnosticsDebug.Assert(deletedColumn.OwningGrid == null, "Expected null deletedColumn.OwningGrid.");
            DiagnosticsDebug.Assert(deletedColumn.Index >= 0, "Expected positive deletedColumn.Index.");
            DiagnosticsDebug.Assert(deletedColumn.DisplayIndex >= 0, "Expected positive deletedColumn.DisplayIndexWithFiller.");

            try
            {
                InDisplayIndexAdjustments = true;

                // The DisplayIndex of columns greater than the deleted column need to be decremented,
                // as do the DisplayIndexMap values of modified column Indexes
                DataGridColumn column;
                this.ColumnsInternal.DisplayIndexMap.RemoveAt(deletedColumn.DisplayIndex);
                for (int displayIndex = 0; displayIndex < this.ColumnsInternal.DisplayIndexMap.Count; displayIndex++)
                {
                    if (this.ColumnsInternal.DisplayIndexMap[displayIndex] > deletedColumn.Index)
                    {
                        this.ColumnsInternal.DisplayIndexMap[displayIndex]--;
                    }

                    if (displayIndex >= deletedColumn.DisplayIndex)
                    {
                        column = this.ColumnsInternal.GetColumnAtDisplayIndex(displayIndex);
                        column.DisplayIndex = column.DisplayIndex - 1;
                        column.DisplayIndexHasChanged = true; // OnColumnDisplayIndexChanged needs to be raised later on
                    }
                }

#if DEBUG
                DiagnosticsDebug.Assert(this.ColumnsInternal.Debug_VerifyColumnDisplayIndexes(), "Expected ColumnsInternal.Debug_VerifyColumnDisplayIndexes() is true.");
#endif

                // Now raise all the OnColumnDisplayIndexChanged events
                FlushDisplayIndexChanged(true /*raiseEvent*/);
            }
            finally
            {
                InDisplayIndexAdjustments = false;
                FlushDisplayIndexChanged(false /*raiseEvent*/);
            }
        }

        private void CorrectColumnDisplayIndexesAfterInsertion(DataGridColumn insertedColumn)
        {
            DiagnosticsDebug.Assert(insertedColumn != null, "Expected non-null insertedColumn.");
            DiagnosticsDebug.Assert(insertedColumn.OwningGrid == this, "Expected insertedColumn.OwningGrid equals this DataGrid.");
            if (insertedColumn.DisplayIndex == -1 || insertedColumn.DisplayIndex >= this.ColumnsItemsInternal.Count)
            {
                // Developer did not assign a DisplayIndex or picked a large number.
                // Choose the Index as the DisplayIndex.
                insertedColumn.DisplayIndex = insertedColumn.Index;
            }

            try
            {
                InDisplayIndexAdjustments = true;

                // The DisplayIndex of columns greater than the inserted column need to be incremented,
                // as do the DisplayIndexMap values of modified column Indexes
                DataGridColumn column;
                for (int displayIndex = 0; displayIndex < this.ColumnsInternal.DisplayIndexMap.Count; displayIndex++)
                {
                    if (this.ColumnsInternal.DisplayIndexMap[displayIndex] >= insertedColumn.Index)
                    {
                        this.ColumnsInternal.DisplayIndexMap[displayIndex]++;
                    }

                    if (displayIndex >= insertedColumn.DisplayIndex)
                    {
                        column = this.ColumnsInternal.GetColumnAtDisplayIndex(displayIndex);
                        column.DisplayIndex++;
                        column.DisplayIndexHasChanged = true; // OnColumnDisplayIndexChanged needs to be raised later on
                    }
                }

                this.ColumnsInternal.DisplayIndexMap.Insert(insertedColumn.DisplayIndex, insertedColumn.Index);

#if DEBUG
                DiagnosticsDebug.Assert(this.ColumnsInternal.Debug_VerifyColumnDisplayIndexes(), "Expected ColumnsInternal.Debug_VerifyColumnDisplayIndexes() is true.");
#endif

                // Now raise all the OnColumnDisplayIndexChanged events
                FlushDisplayIndexChanged(true /*raiseEvent*/);
            }
            finally
            {
                InDisplayIndexAdjustments = false;
                FlushDisplayIndexChanged(false /*raiseEvent*/);
            }
        }

        private void CorrectColumnFrozenStates()
        {
            int index = 0;
            double frozenColumnWidth = 0;
            double oldFrozenColumnWidth = 0;
            foreach (DataGridColumn column in this.ColumnsInternal.GetDisplayedColumns())
            {
                if (column.IsFrozen)
                {
                    oldFrozenColumnWidth += column.ActualWidth;
                }

                column.IsFrozen = index < this.FrozenColumnCount;
                if (column.IsFrozen)
                {
                    frozenColumnWidth += column.ActualWidth;
                }

                index++;
            }

            if (this.HorizontalOffset > Math.Max(0, frozenColumnWidth - oldFrozenColumnWidth))
            {
                UpdateHorizontalOffset(this.HorizontalOffset - frozenColumnWidth + oldFrozenColumnWidth);
            }
            else
            {
                UpdateHorizontalOffset(0);
            }
        }

        private void CorrectColumnIndexesAfterDeletion(DataGridColumn deletedColumn)
        {
            DiagnosticsDebug.Assert(deletedColumn != null, "Expected non-null deletedColumn.");
            for (int columnIndex = deletedColumn.Index; columnIndex < this.ColumnsItemsInternal.Count; columnIndex++)
            {
                this.ColumnsItemsInternal[columnIndex].Index = this.ColumnsItemsInternal[columnIndex].Index - 1;
                DiagnosticsDebug.Assert(this.ColumnsItemsInternal[columnIndex].Index == columnIndex, "Expected ColumnsItemsInternal[columnIndex].Index equals columnIndex.");
            }
        }

        private void CorrectColumnIndexesAfterInsertion(DataGridColumn insertedColumn, int insertionCount)
        {
            DiagnosticsDebug.Assert(insertedColumn != null, "Expected non-null insertedColumn.");
            DiagnosticsDebug.Assert(insertionCount > 0, "Expected strictly positive insertionCount.");
            for (int columnIndex = insertedColumn.Index + insertionCount; columnIndex < this.ColumnsItemsInternal.Count; columnIndex++)
            {
                this.ColumnsItemsInternal[columnIndex].Index = columnIndex;
            }
        }

        private void FlushDisplayIndexChanged(bool raiseEvent)
        {
            foreach (DataGridColumn column in this.ColumnsItemsInternal)
            {
                if (column.DisplayIndexHasChanged)
                {
                    column.DisplayIndexHasChanged = false;
                    if (raiseEvent)
                    {
                        //DiagnosticsDebug.Assert(column != this.ColumnsInternal.RowGroupSpacerColumn, "Expected column other than ColumnsInternal.RowGroupSpacerColumn.");
                        OnColumnDisplayIndexChanged(column);
                    }
                }
            }
        }

        private bool GetColumnEffectiveReadOnlyState(DataGridColumn dataGridColumn)
        {
            DiagnosticsDebug.Assert(dataGridColumn != null, "Expected non-null dataGridColumn.");

            return this.IsReadOnly || dataGridColumn.IsReadOnly;
        }

        /// <summary>
        ///      Returns the absolute coordinate of the left edge of the given column (including
        ///      the potential gridline - that is the left edge of the gridline is returned). Note that
        ///      the column does not need to be in the display area.
        /// </summary>
        /// <returns>Absolute coordinate of the left edge of the given column.</returns>
        private double GetColumnXFromIndex(int index)
        {
            DiagnosticsDebug.Assert(index < this.ColumnsItemsInternal.Count, "Expected index smaller than this.ColumnsItemsInternal.Count.");
            DiagnosticsDebug.Assert(this.ColumnsItemsInternal[index].IsVisible, "Expected ColumnsItemsInternal[index].IsVisible is true.");

            double x = 0;
            foreach (DataGridColumn column in this.ColumnsInternal.GetVisibleColumns())
            {
                if (index == column.Index)
                {
                    break;
                }

                x += GetEdgedColumnWidth(column);
            }

            return x;
        }

        private double GetNegHorizontalOffsetFromHorizontalOffset(double horizontalOffset)
        {
            foreach (DataGridColumn column in this.ColumnsInternal.GetVisibleScrollingColumns())
            {
                if (GetEdgedColumnWidth(column) > horizontalOffset)
                {
                    break;
                }

                horizontalOffset -= GetEdgedColumnWidth(column);
            }

            return horizontalOffset;
        }

        

        private bool ScrollColumnIntoView(int columnIndex)
        {
           
            return true;
        }

        private void ScrollColumns(int columns)
        {
          
        }

        private void UpdateDisplayedColumns()
        {
            this.DisplayData.FirstDisplayedCol = ComputeFirstVisibleScrollingColumn();
            ComputeDisplayedColumns();
        }
    }
}