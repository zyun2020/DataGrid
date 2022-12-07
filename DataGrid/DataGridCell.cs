using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using ZyunUI.DataGridInternals;
using ZyunUI.Utilities;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace ZyunUI
{
    [TemplatePart(Name = DATAGRIDCELL_elementRightGridLine, Type = typeof(Rectangle))]
    [TemplatePart(Name = DATAGRIDCELL_elementBottomGridLine, Type = typeof(Rectangle))]

    [TemplateVisualState(Name = VisualStates.StateNormal, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = VisualStates.StateSelected, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = VisualStates.StateCurrent, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = VisualStates.StateInvalid, GroupName = VisualStates.GroupValidation)]
    [TemplateVisualState(Name = VisualStates.StateValid, GroupName = VisualStates.GroupValidation)]
    public sealed class DataGridCell : ContentControl
    {
        private const string DATAGRIDCELL_elementRightGridLine = "RightGridLine";
        private const string DATAGRIDCELL_elementBottomGridLine = "BottomGridLine";

        private Rectangle _rightGridLine;
        private Rectangle _bottomGridLine;

        private bool _isCurrent = false;
        private bool _isValid = true;
        private bool _isSelected = false;
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

            EnsureGridLine();
        }

        public bool IsCurrent
        {
            get { return _isCurrent; }  
            set
            {
                if(_isCurrent != value)
                {
                    _isCurrent = value;
                    if (_isCurrent) _isSelected = false;
                    ApplyCellState(true);
                }
            }
        }
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    ApplyCellState(true);
                }
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

        internal void ApplyCellState(bool animate)
        {
            if (this.OwningGrid == null || this.OwningColumn == null)
            {
                return;
            }

            // CommonStates
            if (this.IsCurrent)
            {
                VisualStates.GoToState(this, animate, VisualStates.StateCurrent, VisualStates.StateNormal);
            }
            else if (this.IsSelected)
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
        internal void EnsureGridLine()
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
            get;
            set;
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
    }
}
