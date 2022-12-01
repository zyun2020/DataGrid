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

namespace ZyunUI
{
   

   /* public partial class DataGrid
    {
        /// <summary>
        /// Handles mouse left button down events.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            this.Focus(FocusState.Pointer);
            base.OnPointerPressed(e);
            
            PointerPoint expPointer = e.GetCurrentPoint(this.sheetGrid);
            if (expPointer.Properties.IsLeftButtonPressed)
            {
                Point pos = expPointer.Position;
                var cellRef = this.GetCell(pos);

                if (cellRef.Column == -1 || cellRef.Row == -1)
                {
                    return;
                }

                if (cellRef.Row >= this.Rows && (!this.CanInsertRows || !this.IsEasyInsertByMouseEnabled))
                {
                    return;
                }

                if (cellRef.Column > this.Columns && (!this.CanInsertColumns || !this.IsEasyInsertByMouseEnabled))
                {
                    return;
                }

                if (this.autoFillSelection.Visibility == Visibility.Visible)
                {
                    this.AutoFillCell = cellRef;
                    //this.ProtectedCursor = this.autoFillBox.Cursor;
                    this.autoFillToolTip.IsOpen = true;
                }
                else
                {
                    if (!this.HandleAutoInsert(cellRef))
                    {
                        var shift = (e.KeyModifiers == VirtualKeyModifiers.Shift);
                        if (!shift)
                        {
                            this.CurrentCell = cellRef;
                        }

                        this.SelectionCell = cellRef;
                        this.ScrollIntoView(cellRef);
                    }

                    //this.OverrideCursor = this.sheetGrid.Cursor;
                }

                this.CapturePointer(e.Pointer);
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles mouse left button up events.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            base.OnPointerReleased(e);

            PointerPoint expPointer = e.GetCurrentPoint(this.sheetGrid);
            if (expPointer.Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonReleased)
            {
                this.ReleasePointerCaptures();
                //Mouse.OverrideCursor = null;

                if (this.autoFillSelection.Visibility == Visibility.Visible)
                {
                    this.autoFiller.AutoFill(this.CurrentCell, this.SelectionCell, this.AutoFillCell);

                    this.autoFillSelection.Visibility = Visibility.Collapsed;
                    this.autoFillToolTip.IsOpen = false;
                }
            }
        }

        /// <summary>
        /// Handles mouse move events.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected override void OnPointerMoved(PointerRoutedEventArgs e)
        {
            base.OnPointerMoved(e);

            if (this.PointerCaptures == null || this.PointerCaptures.Count == 0)
            {
                return;
            }

            var isInAutoFillMode = this.autoFillSelection.Visibility == Visibility.Visible;

            PointerPoint expPointer = e.GetCurrentPoint(this.sheetGrid);
            var pos = expPointer.Position;

            var cellRef = this.GetCell(pos, isInAutoFillMode, this.CurrentCell);

            if (cellRef.Column == -1 || cellRef.Row == -1)
            {
                return;
            }

            if (cellRef.Row >= this.Rows && (!this.CanInsertRows || !this.IsEasyInsertByMouseEnabled))
            {
                return;
            }

            if (cellRef.Column > this.Columns && (!this.CanInsertColumns || !this.IsEasyInsertByMouseEnabled))
            {
                return;
            }

            if (isInAutoFillMode)
            {
                this.AutoFillCell = cellRef;
                if (this.autoFiller.TryExtrapolate(
                    cellRef,
                    this.CurrentCell,
                    this.SelectionCell,
                    this.AutoFillCell,
                    out var result))
                {
                    var formatString = this.GetFormatString(cellRef);
                    this.autoFillToolTip.Content = FormatValue(result, formatString);
                }

                this.autoFillToolTip.Placement = PlacementMode.Bottom;
                var p = e.GetCurrentPoint(this.autoFillSelection).Position;
                this.autoFillToolTip.HorizontalOffset = p.X + 8;
                this.autoFillToolTip.VerticalOffset = p.Y + 8;
            }
            else
            {
                this.SelectionCell = cellRef;
            }

            this.ScrollIntoView(cellRef);
        }

        /// <summary>
        /// Handles mouse wheel preview events.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected override void OnPointerWheelChanged(PointerRoutedEventArgs e)
        {
            base.OnPointerWheelChanged(e);

            var control = e.KeyModifiers == VirtualKeyModifiers.Control;
            if (control)
            {
                PointerPoint expPointer = e.GetCurrentPoint(sheetGrid);
                var s = 1 + (expPointer.Properties.MouseWheelDelta * 0.0004);
                var tg = new TransformGroup();
                if (this.RenderTransform != null)
                {
                    tg.Children.Add(this.RenderTransform);
                }

                tg.Children.Add(new ScaleTransform() {  ScaleX = s, ScaleY = s});
                this.RenderTransform = tg;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles KeyDown events on the grid.
        /// </summary>
        /// <param name="e">The event arguments.</param>
        protected override void OnKeyDown(KeyRoutedEventArgs e)
        {
            base.OnKeyDown(e);

            bool control = false, shift = false, alt = false;
            KeyboardHelper.GetMetaKeyState(out control, out shift, out alt);    
            
            var row = shift ? this.SelectionCell.Row : this.CurrentCell.Row;
            var column = shift ? this.SelectionCell.Column : this.CurrentCell.Column;

            switch (e.Key)
            {
                case VirtualKey.Enter:
                    if (this.IsMoveAfterEnterEnabled)
                    {
                        if (this.InputDirection == InputDirection.Vertical)
                        {
                            this.ChangeCurrentCell(shift ? -1 : 1, 0);
                        }
                        else
                        {
                            this.ChangeCurrentCell(0, shift ? -1 : 1);
                        }
                    }

                    e.Handled = true;
                    return;

                case VirtualKey.Up:
                    if (row > 0)
                    {
                        row--;
                    }

                    if (this.endPressed)
                    {
                        row = this.FindNextRow(row, column, -1);
                    }

                    if (control)
                    {
                        row = 0;
                    }

                    break;
                case VirtualKey.Down:
                    if (row < this.Rows - 1 || (this.CanInsertRows && this.IsEasyInsertByKeyboardEnabled))
                    {
                        row++;
                    }

                    if (this.endPressed)
                    {
                        row = this.FindNextRow(row, column, 1);
                    }

                    if (control)
                    {
                        row = this.Rows - 1;
                    }

                    break;
                case VirtualKey.Left:
                    if (column > 0)
                    {
                        column--;
                    }

                    if (this.endPressed)
                    {
                        column = this.FindNextColumn(row, column, -1);
                    }

                    if (control)
                    {
                        column = 0;
                    }

                    break;
                case VirtualKey.Right:
                    if (column < this.Columns - 1 || (this.CanInsertColumns && this.IsEasyInsertByKeyboardEnabled))
                    {
                        column++;
                    }

                    if (this.endPressed)
                    {
                        column = this.FindNextColumn(row, column, 1);
                    }

                    if (control)
                    {
                        column = this.Columns - 1;
                    }

                    break;
                case VirtualKey.End:

                    // Flag that the next key should be handled differently
                    this.endPressed = true;
                    e.Handled = true;
                    return;
                case VirtualKey.Home:
                    column = 0;
                    row = 0;
                    break;
                case VirtualKey.Back:
                case VirtualKey.Delete:
                    if (this.CanClear)
                    {
                        this.Clear();
                        e.Handled = true;
                    }

                    return;
                case VirtualKey.F2:
                    if (this.ShowTextBoxEditControl())
                    {
                        e.Handled = true;
                    }

                    return;
                case VirtualKey.F4:
                    if (this.OpenComboBoxControl())
                    {
                        e.Handled = true;
                    }

                    return;
                case VirtualKey.Space:
                    if (this.ToggleCheck())
                    {
                        e.Handled = true;
                    }

                    return;
                case VirtualKey.A:
                    if (control)
                    {
                        this.SelectAll();
                        e.Handled = true;
                    }

                    return;
                case VirtualKey.C:
                    if (control && alt)
                    {
                        var content = new DataPackage();
                        content.SetText(this.ToCsv(this.GetSelectionRange()));
                        Clipboard.SetContent(content);
                        e.Handled = true;
                    }

                    return;
                default:
                    return;
            }

            if (e.Key != VirtualKey.End)
            {
                // Turn of special handling after the first key after End was pressed.
                this.endPressed = false;
            }

            var cell = new CellRef(row, column);
            if (!this.HandleAutoInsert(cell))
            {
                this.SelectionCell = cell;

                if (!shift)
                {
                    this.CurrentCell = cell;
                }

                this.ScrollIntoView(cell);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Handles mouse down events on the row grid.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void RowGridPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            this.Focus(FocusState.Pointer);
            PointerPoint expPointer = e.GetCurrentPoint(this.rowGrid);
            Point pos = expPointer.Position;

            var row = this.GetCell(pos).Row;
            var isRightButton = expPointer.Properties.IsRightButtonPressed;
            var selectionRange = this.GetSelectionRange();
            if (isRightButton && selectionRange.TopRow <= row && selectionRange.BottomRow >= row)
            {
                // do nothing, just show the context menu
                return;
            }

            if (row >= 0)
            {
                var shift = (e.KeyModifiers == VirtualKeyModifiers.Shift);
                this.SelectionCell = new CellRef(row, this.Columns - 1);
                this.CurrentCell = shift ? new CellRef(this.CurrentCell.Row, 0) : new CellRef(row, 0);
                this.ScrollIntoView(this.SelectionCell);
            }

            this.rowGrid.CapturePointer(e.Pointer);
            e.Handled = true;
        }

        /// <summary>
        /// Handles mouse up events on the row grid.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void RowGridPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            this.rowGrid.ReleasePointerCaptures();
        }

        /// <summary>
        /// Handles mouse move events on the row grid.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void RowGridPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (this.rowGrid.PointerCaptures == null || this.rowGrid.PointerCaptures.Count == 0)
            {
                return;
            }

            PointerPoint expPointer = e.GetCurrentPoint(this.rowGrid);
            var row = this.GetCell(expPointer.Position).Row;
            if (row >= 0)
            {
                this.SelectionCell = new CellRef(row, this.Columns - 1);
                e.Handled = true;
            }
        }
        /// <summary>
        /// Handles mouse down events on the column grid.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ColumnGridPointerPressed(object sender, PointerRoutedEventArgs e)
        {
            this.Focus(FocusState.Pointer);
            PointerPoint expPointer = e.GetCurrentPoint(this.columnGrid);
            Point pos = expPointer.Position;

            var column = this.GetCell(pos).Column;
            if (column >= 0)
            {
                var shift = (e.KeyModifiers == VirtualKeyModifiers.Shift);
                this.SelectionCell = new CellRef(this.Rows - 1, column);
                this.CurrentCell = shift ? new CellRef(0, this.CurrentCell.Column) : new CellRef(0, column);
                this.ScrollIntoView(this.SelectionCell);
            }

            // LMB toggles the sort
            if (expPointer.Properties.IsLeftButtonPressed && this.CanSort())
            {
                this.ToggleSort();
            }

            this.columnGrid.CapturePointer(e.Pointer);
            e.Handled = true;
        }

        /// <summary>
        /// Handles mouse up events on the column grid.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ColumnGridPointerReleased(object sender, PointerRoutedEventArgs e)
        {
            this.columnGrid.ReleasePointerCaptures();
        }

        /// <summary>
        /// Handles mouse move events on the column grid.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ColumnGridPointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (this.columnGrid.PointerCaptures == null || this.columnGrid.PointerCaptures.Count == 0)
            {
                return;
            }

            PointerPoint expPointer = e.GetCurrentPoint(this.columnGrid);
            var column = this.GetCell(expPointer.Position).Column;
            if (column >= 0)
            {
                this.SelectionCell = new CellRef(this.Rows - 1, column);

                // e.Handled = true;
            }
        }

        /// <summary>
        /// The column splitter double click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ColumnSplitterDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            bool control = false, shift = false;
            KeyboardHelper.GetMetaKeyState(out control, out shift);
            if (control)
            {
                this.AutoSizeAllColumns();
            }

            var column = Grid.GetColumn((GridSplitter)sender);
            this.AutoSizeColumn(column);
        }

        /// <summary>
        /// The row splitter double click.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void RowSplitterDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            bool control = false, shift = false;
            KeyboardHelper.GetMetaKeyState(out control, out shift);
            if (control)
            {
                this.AutoSizeAllRows();
            }

            var row = Grid.GetRow((GridSplitter)sender);
            this.AutoSizeRow(row);
        }

        /// <summary>
        /// Handles changes in the row scroll viewer.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void RowScrollViewerScrollChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                this.sheetScrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset);
            }
        }

        /// <summary>
        /// Handles changes in the column scroll viewer.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ColumnScrollViewerScrollChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                this.sheetScrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset);
            }
        }

        /// <summary>
        /// Handles scroll changes in the scroll viewers (both horizontal and vertical).
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void ScrollViewerScrollChanged(object sender, ScrollViewerViewChangedEventArgs e)
        {
            this.columnScrollViewer.ScrollToHorizontalOffset(this.sheetScrollViewer.HorizontalOffset);
            this.rowScrollViewer.ScrollToVerticalOffset(this.sheetScrollViewer.VerticalOffset);
        }

        /// <summary>
        /// Handles mouse down events on the top/left selection control.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void TopLeftTapped(object sender, TappedRoutedEventArgs e)
        {
            this.Focus(FocusState.Pointer);
            this.SelectAll();
            e.Handled = true;
        }

        /// <summary>
        /// Handles mouse left button down events on the add item cell.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void AddItemCellTapped(object sender, TappedRoutedEventArgs e)
        {
            this.Focus(FocusState.Pointer);
            var actualIndex = this.InsertItem(-1);
            //this.CollectionView?.Refresh();
            if (actualIndex != -1)
            {
                var viewIndex = this.Operator.GetCollectionViewIndex(actualIndex);

                var cell = this.ItemsInRows
                               ? new CellRef(viewIndex, 0)
                               : new CellRef(0, viewIndex);
                this.SelectionCell = cell;
                this.CurrentCell = cell;
                this.ScrollIntoView(cell);
            }

            e.Handled = true;
        }

        /// <summary>
        /// Handles mouse down events on the auto fill box.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The event arguments.</param>
        private void AutoFillBoxTapped(object sender, TappedRoutedEventArgs e)
        {
            // Show the auto-fill selection border
            this.autoFillSelection.Visibility = Visibility.Visible;
        }
    }*/
}