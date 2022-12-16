using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Microsoft.UI.Input;
using Windows.Foundation;
using Microsoft.UI.Xaml.Documents;
using ZyunUI.Utilities;
using Microsoft.UI.Xaml.Automation.Peers;
using static ZyunUI.DataGridInternals.DataGridError;
using System.Security;
using System.Text;
using DiagnosticsDebug = System.Diagnostics.Debug;
using ZyunUI.DataGridInternals;
using Windows.Devices.Display.Core;
using System;

namespace ZyunUI
{
    public partial class DataGrid
    {
        protected virtual void OnBeginningEdit(DataGridBeginningEditEventArgs e)
        {
            EventHandler<DataGridBeginningEditEventArgs> handler = this.BeginningEdit;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        protected virtual void OnCellEditEnded(DataGridCellEditEndedEventArgs e)
        {
            EventHandler<DataGridCellEditEndedEventArgs> handler = this.CellEditEnded;
            if (handler != null)
            {
                handler(this, e);
            }

            // Raise the automation invoke event for the cell that just ended edit
            //DataGridAutomationPeer peer = DataGridAutomationPeer.FromElement(this) as DataGridAutomationPeer;
            //if (peer != null && AutomationPeer.ListenerExists(AutomationEvents.InvokePatternOnInvoked))
            //{
            //    peer.RaiseAutomationInvokeEvents(DataGridEditingUnit.Cell, e.Column, e.Row);
            //}
        }

        /// <summary>
        /// Raises the CellEditEnding event.
        /// </summary>
        protected virtual void OnCellEditEnding(DataGridCellEditEndingEventArgs e)
        {
            EventHandler<DataGridCellEditEndingEventArgs> handler = this.CellEditEnding;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void HookDataGridEvents()
        {
            this.IsEnabledChanged += new DependencyPropertyChangedEventHandler(DataGrid_IsEnabledChanged);
            this.KeyDown += new KeyEventHandler(DataGrid_KeyDown);
            this.KeyUp += new KeyEventHandler(DataGrid_KeyUp);
            this.GettingFocus += new TypedEventHandler<UIElement, GettingFocusEventArgs>(DataGrid_GettingFocus);
            this.GotFocus += new RoutedEventHandler(DataGrid_GotFocus);
            this.LostFocus += new RoutedEventHandler(DataGrid_LostFocus);
            this.PointerEntered += new PointerEventHandler(DataGrid_PointerEntered);
            this.PointerExited += new PointerEventHandler(DataGrid_PointerExited);
            this.PointerMoved += new PointerEventHandler(DataGrid_PointerMoved);
            this.PointerPressed += new PointerEventHandler(DataGrid_PointerPressed);
            this.PointerReleased += new PointerEventHandler(DataGrid_PointerReleased);
            this.Unloaded += new RoutedEventHandler(DataGrid_Unloaded);
        }

        private bool IsPointerPressed
        {
            get;
            set;
        }

        private void DataGrid_GettingFocus(UIElement sender, GettingFocusEventArgs e)
        {
            _focusInputDevice = e.InputDevice;
        }

        private void DataGrid_GotFocus(object sender, RoutedEventArgs e)
        {
            if (!this.ContainsFocus)
            {
                this.ContainsFocus = true;
                //ApplyDisplayedRowsState(this.DisplayData.FirstScrollingSlot, this.DisplayData.LastScrollingSlot);
                //if (this.CurrentColumnIndex != -1 && this.IsSlotVisible(this.CurrentSlot))
                //{
                //    UpdateCurrentState(this.DisplayData.GetDisplayedElement(this.CurrentSlot), this.CurrentColumnIndex, true /*applyCellState*/);
                //}
            }

            //DependencyObject focusedElement = e.OriginalSource as DependencyObject;
            //_focusedObject = focusedElement;
            //while (focusedElement != null)
            //{
            //    // Keep track of which row contains the newly focused element
            //    var focusedRow = focusedElement as DataGridRow;
            //    if (focusedRow != null && focusedRow.OwningGrid == this && _focusedRow != focusedRow)
            //    {
            //        ResetFocusedRow();
            //        _focusedRow = focusedRow.Visibility == Visibility.Visible ? focusedRow : null;
            //        break;
            //    }

            //    focusedElement = VisualTreeHelper.GetParent(focusedElement);
            //}

            _preferMouseIndicators = _focusInputDevice == FocusInputDeviceKind.Mouse || _focusInputDevice == FocusInputDeviceKind.Pen;

            ShowScrollBars();

            // If the DataGrid itself got focus, we actually want the automation focus to be on the current element
            //if (e.OriginalSource as Control == this && AutomationPeer.ListenerExists(AutomationEvents.AutomationFocusChanged))
            //{
            //    DataGridAutomationPeer peer = DataGridAutomationPeer.FromElement(this) as DataGridAutomationPeer;
            //    if (peer != null)
            //    {
            //        peer.RaiseAutomationFocusChangedEvent(this.CurrentSlot, this.CurrentColumnIndex);
            //    }
            //}
        }

        private void DataGrid_LostFocus(object sender, RoutedEventArgs e)
        {
            _focusedObject = null;
            if (this.ContainsFocus)
            {
                bool focusLeftDataGrid = true;
                bool dataGridWillReceiveRoutedEvent = true;
                DataGridColumn editingColumn = null;

                // Walk up the visual tree of the newly focused element
                // to determine if focus is still within DataGrid.
                object focusedObject = GetFocusedElement();
                DependencyObject focusedDependencyObject = focusedObject as DependencyObject;

                while (focusedDependencyObject != null)
                {
                    if (focusedDependencyObject == this)
                    {
                        focusLeftDataGrid = false;
                        break;
                    }

                    // Walk up the visual tree. Try using the framework element's
                    // parent.  We do this because Popups behave differently with respect to the visual tree,
                    // and it could have a parent even if the VisualTreeHelper doesn't find it.
                    DependencyObject parent = null;
                    FrameworkElement element = focusedDependencyObject as FrameworkElement;
                    if (element == null)
                    {
                        parent = VisualTreeHelper.GetParent(focusedDependencyObject);
                    }
                    else
                    {
                        parent = element.Parent;
                        if (parent == null)
                        {
                            parent = VisualTreeHelper.GetParent(focusedDependencyObject);
                        }
                        else
                        {
                            dataGridWillReceiveRoutedEvent = false;
                        }
                    }

                    focusedDependencyObject = parent;
                }

                //if (this.EditingRow != null && this.EditingColumnIndex != -1)
                //{
                //    editingColumn = this.ColumnsItemsInternal[this.EditingColumnIndex];

                //    if (focusLeftDataGrid && editingColumn is DataGridTemplateColumn)
                //    {
                //        dataGridWillReceiveRoutedEvent = false;
                //    }
                //}

                //if (focusLeftDataGrid && !(editingColumn is DataGridTemplateColumn))
                //{
                //    this.ContainsFocus = false;
                //    if (this.EditingRow != null)
                //    {
                //        CommitEdit(DataGridEditingUnit.Row, true /*exitEditingMode*/);
                //    }

                //    ResetFocusedRow();
                //    ApplyDisplayedRowsState(this.DisplayData.FirstScrollingSlot, this.DisplayData.LastScrollingSlot);
                //    if (this.ColumnHeaderHasFocus)
                //    {
                //        this.ColumnHeaderHasFocus = false;
                //    }
                //    else if (this.CurrentColumnIndex != -1 && this.IsSlotVisible(this.CurrentSlot))
                //    {
                //        UpdateCurrentState(this.DisplayData.GetDisplayedElement(this.CurrentSlot), this.CurrentColumnIndex, true /*applyCellState*/);
                //    }
                //}
                //else if (!dataGridWillReceiveRoutedEvent)
                //{
                //    FrameworkElement focusedElement = focusedObject as FrameworkElement;
                //    if (focusedElement != null)
                //    {
                //        focusedElement.LostFocus += new RoutedEventHandler(ExternalEditingElement_LostFocus);
                //    }
                //}
            }
        }

        private void DataGrid_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateDisabledVisual();

            if (!this.IsEnabled)
            {
                HideScrollBars(true /*useTransitions*/);
            }
        }
        private void UpdateDisabledVisual()
        {
            if (this.IsEnabled)
            {
                VisualStates.GoToState(this, true, VisualStates.StateNormal);
            }
            else
            {
                VisualStates.GoToState(this, true, VisualStates.StateDisabled, VisualStates.StateNormal);
            }
        }

        private void DataGrid_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (!e.Handled)
            {
                // e.Handled = ProcessDataGridKey(e);
                this.LastHandledKeyDown = e.Handled ? e.Key : VirtualKey.None;
            }
        }

        private void DataGrid_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Tab && e.OriginalSource as Control == this)
            {
                if (this.CurrentColumnIndex == -1)
                {
                    if (this.ColumnHeaders != null && this.AreColumnHeadersVisible && !this.ColumnHeaderHasFocus)
                    {
                        this.ColumnHeaderHasFocus = true;
                    }
                }
                else
                {
                    if (this.ColumnHeaders != null && this.AreColumnHeadersVisible)
                    {
                        KeyboardHelper.GetMetaKeyState(out _, out var shift);

                        if (shift && this.LastHandledKeyDown != VirtualKey.Tab)
                        {
                            DiagnosticsDebug.Assert(!this.ColumnHeaderHasFocus, "Expected ColumnHeaderHasFocus is false.");

                            // Show currency on the current column's header as focus is entering the DataGrid backwards.
                            this.ColumnHeaderHasFocus = true;
                        }
                    }

                    //bool success = ScrollSlotIntoView(this.CurrentColumnIndex, this.CurrentSlot, false /*forCurrentCellChange*/, true /*forceHorizontalScroll*/);
                    //DiagnosticsDebug.Assert(success, "Expected ScrollSlotIntoView returns true.");
                    //if (this.CurrentColumnIndex != -1 && this.SelectedItem == null)
                    //{
                    //    SetRowSelection(this.CurrentSlot, true /*isSelected*/, true /*setAnchorSlot*/);
                    //}
                }
            }
        }

        private object GetFocusedElement()
        {
            if (XamlRoot != null)
            {
                return FocusManager.GetFocusedElement(XamlRoot);
            }
            else
            {
                return FocusManager.GetFocusedElement();
            }
        }

        private void DataGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType != PointerDeviceType.Touch)
            {
                // Mouse/Pen inputs dominate. If touch panning indicators are shown, switch to mouse indicators.
                _preferMouseIndicators = true;
                ShowScrollBars();
            }
        }

        private void DataGrid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (e.Pointer.PointerDeviceType != PointerDeviceType.Touch)
            {
                // Mouse/Pen inputs dominate. If touch panning indicators are shown, switch to mouse indicators.
                _isPointerOverHorizontalScrollBar = false;
                _isPointerOverVerticalScrollBar = false;
                _preferMouseIndicators = true;
                ShowScrollBars();
                HideScrollBarsAfterDelay();
            }
        }

        private void DataGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            // Don't process if this is a generated replay of the event.
            if (e.IsGenerated)
            {
                return;
            }

            //Select
            PointerPoint expPointer = e.GetCurrentPoint(_cellsPresenter);
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse && !expPointer.Properties.IsLeftButtonPressed)
            {
                return;
            }

            if (CanSelectCells && CurrentCell.IsValid && IsPointerPressed)
            {
                GridCellRef cellRef = GetGridCellRef(expPointer.Position);
                if (cellRef.IsValid)
                {
                    SelectRange(cellRef);
                }
            }

            if (e.Pointer.PointerDeviceType != PointerDeviceType.Touch)
            {
                // Mouse/Pen inputs dominate. If touch panning indicators are shown, switch to mouse indicators.
                _preferMouseIndicators = true;
                ShowScrollBars();

                if (!UISettingsHelper.AreSettingsEnablingAnimations &&
                    _hideScrollBarsTimer != null &&
                    (_isPointerOverHorizontalScrollBar || _isPointerOverVerticalScrollBar))
                {
                    StopHideScrollBarsTimer();
                }
            }
        }

        private void DataGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (e.Handled) return;

            PointerPoint expPointer = e.GetCurrentPoint(_cellsPresenter);
            if (e.Pointer.PointerDeviceType == PointerDeviceType.Mouse && !expPointer.Properties.IsLeftButtonPressed)
            {
                return;
            }
            this.CapturePointer(e.Pointer);

            IsPointerPressed = true;
            GridCellRef cellRef = GetGridCellRef(expPointer.Position);
            if (cellRef.IsValid)
            {
                CurrentCell = cellRef;
                e.Handled = true;
            }
            else
            {
                ClearSelection();
            }

            // Show the scroll bars as soon as a pointer is pressed on the DataGrid.
            ShowScrollBars();
        }

        private void DataGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            ReleasePointerCaptures();

            if (this.CurrentColumnIndex != -1 && this.CurrentRowIndex != -1)
            {
                e.Handled = true;
            }
        }

        protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            if (!e.Handled)
            {
                var pointerPoint = e.GetCurrentPoint(this);

                // A horizontal scroll happens if the mouse has a horizontal wheel OR if the horizontal scrollbar is not disabled AND the vertical scrollbar IS disabled
                bool isForHorizontalScroll = pointerPoint.Properties.IsHorizontalMouseWheel ||
                    (this.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled && this.VerticalScrollBarVisibility == ScrollBarVisibility.Disabled);

                if ((isForHorizontalScroll && this.HorizontalScrollBarVisibility == ScrollBarVisibility.Disabled) ||
                    (!isForHorizontalScroll && this.VerticalScrollBarVisibility == ScrollBarVisibility.Disabled))
                {
                    return;
                }

                double offsetDelta = -pointerPoint.Properties.MouseWheelDelta / DATAGRID_mouseWheelDeltaDivider;
                if (isForHorizontalScroll && pointerPoint.Properties.IsHorizontalMouseWheel)
                {
                    offsetDelta *= -1.0;
                }

                e.Handled = ProcessScrollOffsetDelta(offsetDelta, isForHorizontalScroll);
            }
        }

        private void DataGrid_Unloaded(object sender, RoutedEventArgs e)
        {
            _showingMouseIndicators = false;
            _keepScrollBarsShowing = false;
        }

       
        internal GridCellRef GetGridCellRef(Point pos)
        {
            double cellLeftEdge;
            double frozenLeftEdge = 0;
            double scrollingLeftEdge = -this.HorizontalOffset;

            int columnIndex = -1;
            var columns = this.ColumnsInternal.GetVisibleColumns();
            foreach (DataGridColumn column in columns)
            {
                if (column.IsFrozen)
                {
                    cellLeftEdge = frozenLeftEdge;
                    frozenLeftEdge += column.ActualWidth;
                    if(pos.X > cellLeftEdge && pos.X <= frozenLeftEdge && cellLeftEdge < this.CellsViewWidth)
                    {
                        columnIndex = column.Index;
                        break;
                    }
                }
                else
                {
                    cellLeftEdge = scrollingLeftEdge;
                    scrollingLeftEdge += column.ActualWidth;
                    if (cellLeftEdge < frozenLeftEdge) cellLeftEdge = frozenLeftEdge;

                    if (pos.X > cellLeftEdge && pos.X <= scrollingLeftEdge &&
                        cellLeftEdge < this.CellsViewWidth && scrollingLeftEdge > frozenLeftEdge)
                    {
                        columnIndex = column.Index;
                        break;
                    }
                }
            }

            if (columnIndex >= 0)
            {
                DataGridDisplayData displayData = this.DisplayData;
                DataGridRowVisuals rowVisuals = null;
                double y0 = -NegVerticalOffset;
                for (int i = 0; i < displayData.NumDisplayedRows; i++)
                {
                    rowVisuals = displayData.GetDisplayedRow(i);
                    if (pos.Y > y0 && pos.Y <= y0 + rowVisuals.DisplayHeight)
                    {
                        if (columnIndex < rowVisuals.CellCount)
                            return new GridCellRef(rowVisuals.DataIndex, columnIndex);

                        break;
                    }
                    y0 += rowVisuals.DisplayHeight;
                }
            }
            return new GridCellRef();
        }

        internal Rect GetGridCellRect(GridCellRef cellRef, ref bool hideLeft)
        {
            double cellLeftEdge;
            double frozenLeftEdge = 0;
            double scrollingLeftEdge = -this.HorizontalOffset;

            Rect rc = new Rect();

            var columns = this.ColumnsInternal.GetVisibleColumns();
            foreach (DataGridColumn column in columns)
            {
                if (column.IsFrozen)
                {
                    cellLeftEdge = frozenLeftEdge;
                    frozenLeftEdge += column.ActualWidth;
                    if (cellRef.Column == column.Index)
                    {
                        rc.X = cellLeftEdge;
                        rc.Width = column.ActualWidth;
                        break;
                    }
                }
                else
                {
                    cellLeftEdge = scrollingLeftEdge;
                    scrollingLeftEdge += column.ActualWidth;
                    if (cellRef.Column == column.Index)
                    {
                        if (cellLeftEdge < frozenLeftEdge)
                        {
                            hideLeft = true; 
                            rc.X = frozenLeftEdge;
                            rc.Width = scrollingLeftEdge - frozenLeftEdge;
                        }
                        else
                        {
                            rc.X = cellLeftEdge;
                            rc.Width = column.ActualWidth;
                        }
                        break;
                    }
                }
            }

            rc.Y = -NegVerticalOffset;
            if(cellRef.Row >= DisplayData.FirstDisplayedRow && cellRef.Row <= DisplayData.LastDisplayedRow)
            {
                for(int i = DisplayData.FirstDisplayedRow; i < cellRef.Row; i++)
                {
                    rc.Y += Rows[i].ActualHeight;
                }
                rc.Height = Rows[cellRef.Row].ActualHeight;
            }
            else
            {
                rc.Height = 0;
            }
            
            return rc;
        }

        internal Rect GetSelectionRect(GridCellRange selection, CellsSelectionMode selectionMode, ref bool hideLeft)
        {
            if(selectionMode == CellsSelectionMode.No) return new Rect();

            GridCellRange cellRange = null;
            if (selectionMode == CellsSelectionMode.Rows)
            {
                cellRange =  new GridCellRange(selection.TopRow, selection.BottomRow, 0, this.ColumnsInternal.VisibleColumnCount - 1);
            }
            else if (selectionMode == CellsSelectionMode.Columns)
            {
                cellRange = new GridCellRange(0, RowCount - 1, selection.LeftColumn, selection.RightColumn);
            }
            else
            {
                cellRange = new GridCellRange(selection.TopRow, selection.BottomRow, selection.LeftColumn, selection.RightColumn);
            }

            return GetSelectionRect(cellRange, ref hideLeft);
        }

        internal Rect GetSelectionRect(GridCellRange selection, ref bool hideLeft)
        {
            Rect rc = new Rect();
            if (selection.TopRow > DisplayData.LastDisplayedRow || selection.BottomRow < DisplayData.FirstDisplayedRow)
                return rc;

            double cellLeftEdge;
            double frozenLeftEdge = 0;
            double scrollingLeftEdge = -this.HorizontalOffset;
            
            var columns = this.ColumnsInternal.GetVisibleColumns();
            foreach (DataGridColumn column in columns)
            {
                bool hasFrozenLeft = false;
                double frozenWidth = 0;
                if (column.IsFrozen)
                {
                    cellLeftEdge = frozenLeftEdge;
                    frozenLeftEdge += column.ActualWidth;
                    if(column.Index < selection.LeftColumn)
                        continue;
                    else if (column.Index == selection.LeftColumn)
                    {
                        rc.X = cellLeftEdge;
                        rc.Width = column.ActualWidth;
                        frozenWidth = column.ActualWidth;

                        hasFrozenLeft = true;
                    }
                    else if (column.Index <= selection.RightColumn)
                    {
                        rc.Width += column.ActualWidth;
                        frozenWidth += column.ActualWidth;
                        break;
                    }
                }
                else
                {
                    cellLeftEdge = scrollingLeftEdge;
                    scrollingLeftEdge += column.ActualWidth;

                    if (column.Index < selection.LeftColumn)
                        continue;
                    else if (column.Index == selection.LeftColumn)
                    {
                        if (cellLeftEdge < frozenLeftEdge)
                        {
                            hideLeft = true; //because no clip
                            rc.X = frozenLeftEdge;
                            if (scrollingLeftEdge > frozenLeftEdge)
                                rc.Width = scrollingLeftEdge - frozenLeftEdge;
                        }
                        else
                        {
                            rc.X = cellLeftEdge;
                            rc.Width = column.ActualWidth;
                        }
                    }
                    else if (column.Index <= selection.RightColumn)
                    {
                        if (scrollingLeftEdge > frozenLeftEdge)
                        {
                            if (cellLeftEdge < frozenLeftEdge)
                            {
                                if(scrollingLeftEdge > frozenLeftEdge)
                                    rc.Width += scrollingLeftEdge - frozenLeftEdge;
                            }
                            else
                            {
                                rc.Width += column.ActualWidth;
                            }
                        }
                    }

                    if(hasFrozenLeft && scrollingLeftEdge <= frozenLeftEdge)
                    {
                        rc.Width = frozenWidth;
                    }
                }
            }

            int startRow = 0;
            rc.Y = -NegVerticalOffset;
            if (selection.TopRow < DisplayData.FirstDisplayedRow)
            {
                if (rc.Y < 0)
                {
                    startRow = DisplayData.FirstDisplayedRow;
                }
                else 
                {
                    startRow = DisplayData.FirstDisplayedRow - 1;
                    rc.Y = -Rows[startRow].ActualHeight;
                }
            }
            else
            {
                for (int i = DisplayData.FirstDisplayedRow; i < selection.TopRow; i++)
                {
                    rc.Y += Rows[i].ActualHeight;
                }
                startRow = selection.TopRow;
            }
            for (int i = startRow; i <= selection.BottomRow; i++)
            {
                rc.Height += Rows[i].ActualHeight;
                if(rc.Height > this.CellsViewHeight)
                {
                    break;
                }
            }
            return rc;
        }
    }
}