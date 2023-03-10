// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using ZyunUI.Utilities;
using static ZyunUI.DataGridInternals.DataGridError;
using DiagnosticsDebug = System.Diagnostics.Debug;

namespace ZyunUI.DataGridInternals
{
    internal class DataGridRowVisuals
    {
        private DataGridRowHeader _headerCell = null;
        private List<DataGridCell> _cells;
     
        public DataGridRowVisuals()
        { 
            _cells = new List<DataGridCell>();
        }

        internal void SetRowInfo(int index, object dataItem, double displayHeight, bool isReused)
        {
            DataIndex = index;
            DataItem = dataItem;
            DisplayHeight = displayHeight;

            SetElementInfo(dataItem, displayHeight, isReused);
        }

        private void SetElementInfo(object dataItem, double displayHeight, bool isReused)
        {
            if (_headerCell != null)
            {
                _headerCell.DataContext = dataItem;
                _headerCell.Height = displayHeight;
                _headerCell.OwningRow = this;
                
            }
            for (int i = 0; i < _cells.Count; i++)
            {
                DataGridCell cell = _cells[i];
                cell.DataContext = dataItem;
                cell.Height = displayHeight;
                cell.OwningRow = this;
            }
        }

        internal int DataIndex { get; private set; }
        internal object DataItem { get; private set; }
        internal double DisplayHeight { get; private set; }
       
        internal DataGridRowHeader HeaderCell
        {
            get
            {
                return _headerCell;
            }
        }

        internal DataGridRowHeader CreateHeaderCell()
        {
            if (_headerCell == null)
            {
                _headerCell = new DataGridRowHeader();
                _headerCell.EnsureStyle(null);
            } 
            return _headerCell;
        }

        public int CellCount
        {
            get
            {
                return _cells.Count;
            }
        }

        public IEnumerator GetEnumerator()
        {
            return _cells.GetEnumerator();
        }

        public void Insert(int cellIndex, DataGridCell cell)
        {
            Debug.Assert(cellIndex >= 0 && cellIndex <= _cells.Count, "Expected cellIndex between 0 and _cells.Count inclusive.");
            Debug.Assert(cell != null, "Expected non-null cell.");

            _cells.Insert(cellIndex, cell);
        }

        public void RemoveAt(int cellIndex)
        {
            _cells.RemoveAt(cellIndex);
        }

        public DataGridCell this[int index]
        {
            get
            {
                if (index < 0 || index >= _cells.Count)
                {
                    throw DataGridError.DataGrid.ValueMustBeBetween("index", "Index", 0, true, _cells.Count, false);
                }

                return _cells[index];
            }
        }

        public void UpdateDataContext(object dataContext)
        {
            if (_headerCell != null)
            {
                _headerCell.DataContext = dataContext;
            }
            for(int i = 0; i < _cells.Count; i++)
            {
                DataGridCell cell = _cells[i];
                cell.DataContext = dataContext;
            }
        }

        
    }
    internal class DataGridDisplayData
    {
        private DataGrid _owner;
        private Stack<DataGridRowVisuals> _recyclableRows; // list of Rows which have not been fully recycled (avoids Measure in several cases)
        private List<DataGridRowVisuals> _displayedRows; // circular list of displayed elements
    
        public DataGridDisplayData(DataGrid owner)
        {
            _owner = owner;
            _recyclableRows = new Stack<DataGridRowVisuals>();
            _displayedRows = new List<DataGridRowVisuals>();

            FirstDisplayedCol = -1;
            LastDisplayedCol = -1;

            FirstDisplayedRow = -1;
            LastDisplayedRow = -1;
        }

        internal double PendingVerticalScrollHeight
        {
            get;
            set;
        }

        public int FirstDisplayedCol
        {
            get;
            set;
        }

        public int LastDisplayedCol
        {
            get;
            set;
        }


        public int FirstDisplayedRow
        {
            get;
            private set;
        }

        public int LastDisplayedRow
        {
            get;
            private set;
        }

      
        public int NumDisplayedRows
        {
            get
            {
                return _displayedRows.Count;
            }
        }

        internal DataGridCell GetDataGridCell(GridCellRef cellRef)
        {
            if(cellRef.Row >= FirstDisplayedRow && cellRef.Row <= LastDisplayedRow &&
                cellRef.Column >= 0 && cellRef.Column < _owner.ColumnCount)
            {
                DataGridRowVisuals rowVisuals = GetDisplayedRow(cellRef.Row - FirstDisplayedRow);
                return rowVisuals[cellRef.Column];
            }
            return null;
        }

        internal void AddRecylableRow(DataGridRowVisuals row)
        {
            DiagnosticsDebug.Assert(!_recyclableRows.Contains(row), "Expected row parameter to be non-recyclable.");
            row.UpdateDataContext(null);
            _recyclableRows.Push(row);
        }

        internal void ClearElements(bool recycle)
        {
            if (recycle)
            {
                foreach (var row in _displayedRows)
                {
                    AddRecylableRow(row);
                }
            }
            else
            {
                _recyclableRows.Clear();
            }
            _displayedRows.Clear();
        }

        internal IEnumerable<DataGridRowVisuals> GetAllRows()
        {
            foreach (DataGridRowVisuals element in _displayedRows)
            { 
                if (element != null)
                {
                    yield return element;
                }
            } 
        }

        internal DataGridRowVisuals GetDisplayedRow(int displayIndex)
        {
            DiagnosticsDebug.Assert(displayIndex >= 0, "Expected slot greater than or equal to 0.");
            DiagnosticsDebug.Assert(displayIndex < this.NumDisplayedRows, "Expected slot less than or equal to NumDisplayedRows.");

            return _displayedRows[displayIndex];
        }

        internal DataGridRowVisuals GetUsedRow()
        {
            DataGridRowVisuals row = null;
            if (_recyclableRows.Count > 0)
            {
                row = _recyclableRows.Pop();
            }
            return row;
        }

        private int GetDisplayIndex(int rowIndex)
        {
            return rowIndex - this.FirstDisplayedRow;
        }

        private DataGridRowVisuals RecycleRow(int displayIndex)
        {
            DiagnosticsDebug.Assert(displayIndex >= 0, "Expected slot greater than or equal to 0.");
            DiagnosticsDebug.Assert(displayIndex < this.NumDisplayedRows, "Expected slot less than or equal to NumDisplayedRows.");

            DataGridRowVisuals rowVisuals =  _displayedRows[displayIndex];
            _displayedRows.RemoveAt(displayIndex);

            AddRecylableRow(rowVisuals);
            return rowVisuals;
        }

        internal void UpdateDisplayedRows(int newFirstDisplayedRow, int newLastDisplayedRow)
        {
            _owner.ResetEditingElement(newFirstDisplayedRow, newLastDisplayedRow);

            if (this.NumDisplayedRows == 0 || this.FirstDisplayedRow == -1 || this.LastDisplayedRow == -1 ||
                newLastDisplayedRow < this.FirstDisplayedRow || newFirstDisplayedRow > this.LastDisplayedRow)
            {
                ClearElements(true);
                _owner.RemoveAllDisplayedElement();

                DataGridRowVisuals rowVisuals;
                this.FirstDisplayedRow = newFirstDisplayedRow;
                this.LastDisplayedRow = newLastDisplayedRow;
                for(int i = 0; i <= newLastDisplayedRow - newFirstDisplayedRow; i++)
                {
                    rowVisuals = _owner.GenerateRow(this.FirstDisplayedRow + i);
                    _owner.InsertDisplayedElement(i, rowVisuals);
                    _displayedRows.Insert(i, rowVisuals);
                }
            }
            else
            {
                try
                {
                    //?????????????? ??????????????
                    DataGridRowVisuals rowVisuals;
                    int dist = this.LastDisplayedRow - newLastDisplayedRow;
                    if (dist > 0)
                    {
                        //????????????,??????????????
                        int displayIndex = GetDisplayIndex(LastDisplayedRow);
                        for (int i = 0; i < dist; i++)
                        {
                            rowVisuals = RecycleRow(displayIndex - i);
                            _owner.RemoveDisplayedElement(displayIndex - i, rowVisuals);
                        }
                    }
                    else
                    {
                        dist = -dist;
                        int displayIndex = GetDisplayIndex(this.LastDisplayedRow + 1);
                        for (int i = 0; i < dist; i++)
                        {
                            rowVisuals = _owner.GenerateRow(this.LastDisplayedRow + 1 + i);
                            _owner.InsertDisplayedElement(displayIndex + i, rowVisuals);
                            _displayedRows.Insert(displayIndex + i, rowVisuals);
                        }
                    }
                  
                    dist = this.FirstDisplayedRow - newFirstDisplayedRow;
                    if (dist > 0)
                    {
                        for (int i = 0; i < dist; i++)
                        {
                            //??????????????????????????
                            rowVisuals = _owner.GenerateRow(this.FirstDisplayedRow - 1 - i);
                            _owner.InsertDisplayedElement(0, rowVisuals);
                            _displayedRows.Insert(0, rowVisuals);
                        }
                    }
                    else
                    {
                        dist = -dist;
                        for (int i = 0; i < dist; i++)
                        {
                            //????????????
                            rowVisuals = RecycleRow(0);
                            _owner.RemoveDisplayedElement(0, rowVisuals);
                        }
                    }

                    this.FirstDisplayedRow = newFirstDisplayedRow;
                    this.LastDisplayedRow = newLastDisplayedRow;
                }
                catch(Exception ex)
                {
                    Debug.Write(ex.ToString());
                }
            }

            _owner.UpdateCellsState();
        }
    }
}