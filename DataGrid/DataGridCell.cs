using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Shapes;
using ZyunUI.DataGridInternals;
using ZyunUI.Utilities;

namespace ZyunUI
{
    [TemplatePart(Name = DATAGRIDCELL_elementRightGridLine, Type = typeof(Rectangle))]
    [TemplatePart(Name = DATAGRIDCELL_elementBottomGridLine, Type = typeof(Rectangle))]

    [TemplateVisualState(Name = VisualStates.StateNormal, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = VisualStates.StateSelected, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = VisualStates.StateInvalid, GroupName = VisualStates.GroupValidation)]
    [TemplateVisualState(Name = VisualStates.StateValid, GroupName = VisualStates.GroupValidation)]
    public sealed class DataGridCell : ContentControl
    {
        private const string DATAGRIDCELL_elementRightGridLine = "RightGridLine";
        private const string DATAGRIDCELL_elementBottomGridLine = "BottomGridLine";

        private Rectangle _rightGridLine;
        private Rectangle _bottomGridLine;

        private bool _isValid = true;
      
        public DataGridCell()
        {
            this.DefaultStyleKey = typeof(DataGridCell);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            ApplyCellState(false /*animate*/);

            _rightGridLine = GetTemplateChild(DATAGRIDCELL_elementRightGridLine) as Rectangle;
            _bottomGridLine = GetTemplateChild(DATAGRIDCELL_elementBottomGridLine) as Rectangle;

            EnsureGridLines();
        }

        internal bool IsCurrent
        {
            get
            {
                if(OwningColumn == null || OwningRow == null) return false;

                return this.OwningGrid.CurrentColumnIndex == this.OwningColumn.Index &&
                       this.OwningGrid.CurrentRowIndex == this.OwningRow.DataIndex;
            }
        }

        internal bool IsSelected
        {
            get
            {
                if (OwningColumn == null || OwningRow == null) return false;
                return OwningGrid.CellIsSelected(new GridCellRef(OwningRow.DataIndex, OwningColumn.Index));
            }
        }

        public bool IsValid
        {
            get { return _isValid; }
            set
            {
                if (_isValid != value)
                {
                    _isValid = value; 
                    ApplyCellState(true);
                }
            }
        }

        internal void ClearCellState()
        {
            VisualStates.GoToState(this, false, VisualStates.StateNormal);
            VisualStates.GoToState(this, false, VisualStates.StateValid);
        }

        internal void ApplyCellState(bool animate)
        {
            if (this.OwningGrid == null || this.OwningColumn == null)
            {
                return;
            }

            // CommonStates
            if (this.IsSelected)
            {
                VisualStates.GoToState(this, animate, VisualStates.StateSelected, VisualStates.StateNormal);
            }
            else
            {
                VisualStates.GoToState(this, animate, VisualStates.StateNormal);
            }
 
            // Validation states
            if (this.IsValid)
            {
                VisualStates.GoToState(this, animate, VisualStates.StateValid);
            }
            else
            {
                VisualStates.GoToState(this, animate, VisualStates.StateInvalid, VisualStates.StateValid);
            }
        }

        // Makes sure the right gridline has the proper stroke and visibility. If lastVisibleColumn is specified, the
        // right gridline will be collapsed if this cell belongs to the lastVisibileColumn and there is no filler column
        internal void EnsureGridLines()
        {
            if (this.OwningGrid != null && _rightGridLine != null)
            {
                if (this.OwningGrid.VerticalGridLinesBrush != null && this.OwningGrid.VerticalGridLinesBrush != _rightGridLine.Fill)
                {
                    _rightGridLine.Fill = this.OwningGrid.VerticalGridLinesBrush;
                }

                if (this.OwningGrid.HorizontalGridLinesBrush != null && this.OwningGrid.HorizontalGridLinesBrush != _bottomGridLine.Fill)
                {
                    _bottomGridLine.Fill = this.OwningGrid.VerticalGridLinesBrush;
                }

                Visibility rightVisibility =
                    (this.OwningGrid.GridLinesVisibility == DataGridGridLinesVisibility.Vertical || this.OwningGrid.GridLinesVisibility == DataGridGridLinesVisibility.All) 
                    ? Visibility.Visible : Visibility.Collapsed;

                if (rightVisibility != _rightGridLine.Visibility)
                {
                    _rightGridLine.Visibility = rightVisibility;
                }

                Visibility bottomVisibility =
                    (this.OwningGrid.GridLinesVisibility == DataGridGridLinesVisibility.Horizontal || this.OwningGrid.GridLinesVisibility == DataGridGridLinesVisibility.All)
                    ? Visibility.Visible : Visibility.Collapsed;

                if (bottomVisibility != _bottomGridLine.Visibility)
                {
                    _bottomGridLine.Visibility = bottomVisibility;
                }
            }
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

        internal int RowIndex
        {
            get
            {
                if (this.OwningRow == null)
                {
                    return -1;
                }

                return this.OwningRow.DataIndex;
            }
        }

        internal DataGridColumn OwningColumn
        {
            get;
            set;
        }

        internal DataGridRowVisuals OwningRow
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
    }
}
