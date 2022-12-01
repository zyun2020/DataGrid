// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using ZyunUI.Utilities;
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

        internal DataGridRowHeader HeaderCell
        {
            get
            {
                return _headerCell;
            }
        }

        internal DataGridRowHeader CreateHeaderCell(object header, Style style)
        {
            if (_headerCell == null)
            {
                _headerCell = new DataGridRowHeader();
            }
            _headerCell.Content = header;
            _headerCell.SetStyleWithType(style);
            return _headerCell;
        }

        public int Count
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
            _headerCell.DataContext = dataContext;
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
        }

        public int FirstDisplayedScrollingCol
        {
            get;
            set;
        }

        public int LastDisplayedScrollingCol
        {
            get;
            set;
        }


        public int FirstDisplayedRow
        {
            get;
            set;
        }

        public int LastDisplayedRow
        {
            get;
            set;
        }

      
        public int NumDisplayedRows
        {
            get
            {
                return _displayedRows.Count;
            }
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

        internal DataGridRowVisuals GetUsedRow(object dataContext)
        {
            DataGridRowVisuals row = null;
            if (_recyclableRows.Count > 0)
            {
                row = _recyclableRows.Pop();
                row.UpdateDataContext(dataContext);
            }
            return row;
        }

    }
}