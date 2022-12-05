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

namespace ZyunUI
{
    public partial class DataGrid
    {
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
            if (e.Handled)
            {
                return;
            }

            // Show the scroll bars as soon as a pointer is pressed on the DataGrid.
            ShowScrollBars();
        }

        private void DataGrid_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (this.CurrentColumnIndex != -1 && this.CurrentSlot != -1)
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
    }
}