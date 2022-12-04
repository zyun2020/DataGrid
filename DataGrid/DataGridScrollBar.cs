using System;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ZyunUI.Utilities;
using DispatcherQueueTimer = Microsoft.UI.Dispatching.DispatcherQueueTimer;
using DiagnosticsDebug = System.Diagnostics.Debug;

namespace ZyunUI
{
    public partial class DataGrid
    {
        // the sum of the widths in pixels of the scrolling columns preceding
        // the first displayed scrolling column
        private double _horizontalOffset;
        private byte _horizontalScrollChangesIgnored;
        private bool _ignoreNextScrollBarsLayout;
        private List<ValidationResult> _indeiValidationResults;
        private bool _initializingNewItem;

        private ScrollBarVisualState _proposedScrollBarsState;
        private ScrollBarsSeparatorVisualState _proposedScrollBarsSeparatorState;
        private DispatcherQueueTimer _hideScrollBarsTimer;
        // Set to True when the mouse scroll bars are currently showing.
        private bool _showingMouseIndicators;
        // Set to True to favor mouse indicators over panning indicators for the scroll bars.
        private bool _preferMouseIndicators;

        private bool _hasNoIndicatorStateStoryboardCompletedHandler;
        private byte _verticalScrollChangesIgnored;

        private bool _isHorizontalScrollBarInteracting;
        private bool _isVerticalScrollBarInteracting;

        // Set to True when the pointer is over the optional scroll bars.
        private bool _isPointerOverHorizontalScrollBar;
        private bool _isPointerOverVerticalScrollBar;

        // Set to True to prevent the normal fade-out of the scroll bars.
        private bool _keepScrollBarsShowing;

        // the number of pixels of the firstDisplayedScrollingCol which are not displayed
        private double _negHorizontalOffset;

        // An approximation of the sum of the heights in pixels of the scrolling rows preceding
        // the first displayed scrolling row.  Since the scrolled off rows are discarded, the grid
        // does not know their actual height. The heights used for the approximation are the ones
        // set as the rows were scrolled off.
        private double _verticalOffset;

       

        private bool IsHorizontalScrollBarInteracting
        {
            get
            {
                return _isHorizontalScrollBarInteracting;
            }

            set
            {
                if (_isHorizontalScrollBarInteracting != value)
                {
                    _isHorizontalScrollBarInteracting = value;

                    if (_hScrollBar != null)
                    {
                        if (_isHorizontalScrollBarInteracting)
                        {
                            // Prevent the vertical scroll bar from fading out while the user is interacting with the horizontal one.
                            _keepScrollBarsShowing = true;

                            ShowScrollBars();
                        }
                        else
                        {
                            // Make the scroll bars fade out, after the normal delay.
                            _keepScrollBarsShowing = false;

                            HideScrollBars(true /*useTransitions*/);
                        }
                    }
                }
            }
        }

        private bool IsVerticalScrollBarInteracting
        {
            get
            {
                return _isVerticalScrollBarInteracting;
            }

            set
            {
                if (_isVerticalScrollBarInteracting != value)
                {
                    _isVerticalScrollBarInteracting = value;

                    if (_vScrollBar != null)
                    {
                        if (_isVerticalScrollBarInteracting)
                        {
                            // Prevent the horizontal scroll bar from fading out while the user is interacting with the vertical one.
                            _keepScrollBarsShowing = true;

                            ShowScrollBars();
                        }
                        else
                        {
                            // Make the scroll bars fade out, after the normal delay.
                            _keepScrollBarsShowing = false;

                            HideScrollBars(true /*useTransitions*/);
                        }
                    }
                }
            }
        }

        // Calculates the amount to scroll for the ScrollLeft button
        // This is a method rather than a property to emphasize a calculation
        private double GetHorizontalSmallScrollDecrease()
        {
            // If the first column is covered up, scroll to the start of it when the user clicks the left button
            if (_negHorizontalOffset > 0)
            {
                return _negHorizontalOffset;
            }
            else
            {
                // The entire first column is displayed, show the entire previous column when the user clicks
                // the left button
                DataGridColumn previousColumn = this.ColumnsInternal.GetPreviousVisibleScrollingColumn(
                    this.ColumnsItemsInternal[DisplayData.FirstDisplayedCol]);
                if (previousColumn != null)
                {
                    return GetEdgedColumnWidth(previousColumn);
                }
                else
                {
                    // There's no previous column so don't move
                    return 0;
                }
            }
        }

        // Calculates the amount to scroll for the ScrollRight button
        // This is a method rather than a property to emphasize a calculation
        private double GetHorizontalSmallScrollIncrease()
        {
            if (this.DisplayData.FirstDisplayedCol >= 0)
            {
                return GetEdgedColumnWidth(this.ColumnsItemsInternal[DisplayData.FirstDisplayedCol]) - _negHorizontalOffset;
            }

            return 0;
        }

        // Calculates the amount the ScrollDown button should scroll
        // This is a method rather than a property to emphasize that calculations are taking place
        private double GetVerticalSmallScrollIncrease()
        {
            if (this.DisplayData.FirstDisplayedRow >= 0)
            {
                return GetRowActualHeight(this.DisplayData.FirstDisplayedRow) - this.NegVerticalOffset;
            }

            return 0;
        }

        private void HideScrollBars(bool useTransitions)
        {
            if (!_keepScrollBarsShowing)
            {
                _proposedScrollBarsState = ScrollBarVisualState.NoIndicator;
                _proposedScrollBarsSeparatorState = UISettingsHelper.AreSettingsEnablingAnimations ? ScrollBarsSeparatorVisualState.SeparatorCollapsed : ScrollBarsSeparatorVisualState.SeparatorCollapsedWithoutAnimation;
                if (UISettingsHelper.AreSettingsAutoHidingScrollBars)
                {
                    SwitchScrollBarsVisualStates(_proposedScrollBarsState, _proposedScrollBarsSeparatorState, useTransitions);
                }
            }
        }

        private void HideScrollBarsAfterDelay()
        {
            if (!_keepScrollBarsShowing)
            {
                DispatcherQueueTimer hideScrollBarsTimer = null;

                if (_hideScrollBarsTimer != null)
                {
                    hideScrollBarsTimer = _hideScrollBarsTimer;
                    if (hideScrollBarsTimer.IsRunning)
                    {
                        hideScrollBarsTimer.Stop();
                    }
                }
                else
                {
                    hideScrollBarsTimer = DispatcherQueue.CreateTimer();
                    hideScrollBarsTimer.Interval = TimeSpan.FromMilliseconds(DATAGRID_noScrollBarCountdownMs);
                    hideScrollBarsTimer.Tick += HideScrollBarsTimerTick;
                    _hideScrollBarsTimer = hideScrollBarsTimer;
                }

                hideScrollBarsTimer.Start();
            }
        }

        private void HideScrollBarsTimerTick(object sender, object e)
        {
            StopHideScrollBarsTimer();
            HideScrollBars(true /*useTransitions*/);
        }
        private void HookHorizontalScrollBarEvents()
        {
            if (_hScrollBar != null)
            {
                _hScrollBar.Scroll += new ScrollEventHandler(HorizontalScrollBar_Scroll);
                _hScrollBar.PointerEntered += new PointerEventHandler(HorizontalScrollBar_PointerEntered);
                _hScrollBar.PointerExited += new PointerEventHandler(HorizontalScrollBar_PointerExited);
            }
        }

        private void HookVerticalScrollBarEvents()
        {
            if (_vScrollBar != null)
            {
                _vScrollBar.Scroll += new ScrollEventHandler(VerticalScrollBar_Scroll);
                _vScrollBar.PointerEntered += new PointerEventHandler(VerticalScrollBar_PointerEntered);
                _vScrollBar.PointerExited += new PointerEventHandler(VerticalScrollBar_PointerExited);
            }
        }

        private void HorizontalScrollBar_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _isPointerOverHorizontalScrollBar = true;

            if (!UISettingsHelper.AreSettingsEnablingAnimations)
            {
                HideScrollBarsAfterDelay();
            }
        }

        private void HorizontalScrollBar_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isPointerOverHorizontalScrollBar = false;
            HideScrollBarsAfterDelay();
        }

        private void VerticalScrollBar_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            _isPointerOverVerticalScrollBar = true;

            if (!UISettingsHelper.AreSettingsEnablingAnimations)
            {
                HideScrollBarsAfterDelay();
            }
        }

        private void VerticalScrollBar_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _isPointerOverVerticalScrollBar = false;
            HideScrollBarsAfterDelay();
        }

        internal void ProcessHorizontalScroll(ScrollEventType scrollEventType)
        {
            if (scrollEventType == ScrollEventType.EndScroll)
            {
                this.IsHorizontalScrollBarInteracting = false;
            }
            else if (scrollEventType == ScrollEventType.ThumbTrack)
            {
                this.IsHorizontalScrollBarInteracting = true;
            }

            if (_horizontalScrollChangesIgnored > 0)
            {
                return;
            }

            // If the user scrolls with the buttons, we need to update the new value of the scroll bar since we delay
            // this calculation.  If they scroll in another other way, the scroll bar's correct value has already been set
            double scrollBarValueDifference = 0;
            if (scrollEventType == ScrollEventType.SmallIncrement)
            {
                scrollBarValueDifference = GetHorizontalSmallScrollIncrease();
            }
            else if (scrollEventType == ScrollEventType.SmallDecrement)
            {
                scrollBarValueDifference = -GetHorizontalSmallScrollDecrease();
            }

            _horizontalScrollChangesIgnored++;
            try
            {
                if (scrollBarValueDifference != 0)
                {
                    DiagnosticsDebug.Assert(_horizontalOffset + scrollBarValueDifference >= 0, "Expected positive _horizontalOffset + scrollBarValueDifference.");
                    SetHorizontalOffset(_horizontalOffset + scrollBarValueDifference);
                }

                UpdateHorizontalOffset(_hScrollBar.Value);
            }
            finally
            {
                _horizontalScrollChangesIgnored--;
            }

            //DataGridAutomationPeer peer = DataGridAutomationPeer.FromElement(this) as DataGridAutomationPeer;
            //if (peer != null)
            //{
            //    peer.RaiseAutomationScrollEvents();
            //}
        }

        internal void ProcessVerticalScroll(ScrollEventType scrollEventType)
        {
            if (scrollEventType == ScrollEventType.EndScroll)
            {
                this.IsVerticalScrollBarInteracting = false;
            }
            else if (scrollEventType == ScrollEventType.ThumbTrack)
            {
                this.IsVerticalScrollBarInteracting = true;
            }

            if (_verticalScrollChangesIgnored > 0)
            {
                return;
            }

            DiagnosticsDebug.Assert(DoubleUtil.LessThanOrClose(_vScrollBar.Value, _vScrollBar.Maximum), "Expected _vScrollBar.Value smaller than or close to _vScrollBar.Maximum.");

            _verticalScrollChangesIgnored++;
            try
            {
                DiagnosticsDebug.Assert(_vScrollBar != null, "Expected non-null _vScrollBar.");
                if (scrollEventType == ScrollEventType.SmallIncrement)
                {
                    this.DisplayData.PendingVerticalScrollHeight = GetVerticalSmallScrollIncrease();
                    double newVerticalOffset = _verticalOffset + this.DisplayData.PendingVerticalScrollHeight;
                    if (newVerticalOffset > _vScrollBar.Maximum)
                    {
                        this.DisplayData.PendingVerticalScrollHeight -= newVerticalOffset - _vScrollBar.Maximum;
                    }
                }
                else if (scrollEventType == ScrollEventType.SmallDecrement)
                {
                    if (DoubleUtil.GreaterThan(this.NegVerticalOffset, 0))
                    {
                        this.DisplayData.PendingVerticalScrollHeight -= this.NegVerticalOffset;
                    }
                    else
                    {
                        //int previousScrollingSlot = this.GetPreviousVisibleSlot(this.DisplayData.FirstScrollingSlot);
                        //if (previousScrollingSlot >= 0)
                        //{
                        //    ScrollSlotIntoView(previousScrollingSlot, false /*scrolledHorizontally*/);
                        //}

                        return;
                    }
                }
                else
                {
                    this.DisplayData.PendingVerticalScrollHeight = _vScrollBar.Value - _verticalOffset;
                }

                if (!DoubleUtil.IsZero(this.DisplayData.PendingVerticalScrollHeight))
                {
                    // Invalidate so the scroll happens on idle
                    //InvalidateRowsMeasure(false /*invalidateIndividualElements*/);
                }
            }
            finally
            {
                _verticalScrollChangesIgnored--;
            }
        }

        private void HorizontalScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            ProcessHorizontalScroll(e.ScrollEventType);
        }

        private void VerticalScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            ProcessVerticalScroll(e.ScrollEventType);
        }

        private void IndicatorStateStoryboard_Completed(object sender, object e)
        {
            //If the cursor is currently directly over either scroll bar then do not automatically hide the indicators.
            if (!_keepScrollBarsShowing &&
                !_isPointerOverVerticalScrollBar &&
                !_isPointerOverHorizontalScrollBar)
            {
                // Go to the NoIndicator state using transitions.
                if (UISettingsHelper.AreSettingsEnablingAnimations)
                {
                    // By default there is a delay before the NoIndicator state actually shows.
                    HideScrollBars(true /*useTransitions*/);
                }
                else
                {
                    // Since OS animations are turned off, use a timer to delay the scroll bars' hiding.
                    HideScrollBarsAfterDelay();
                }
            }
        }
        private void ShowScrollBars()
        {
            if (this.AreAllScrollBarsCollapsed)
            {
                _proposedScrollBarsState = ScrollBarVisualState.NoIndicator;
                _proposedScrollBarsSeparatorState = ScrollBarsSeparatorVisualState.SeparatorCollapsedWithoutAnimation;
                SwitchScrollBarsVisualStates(_proposedScrollBarsState, _proposedScrollBarsSeparatorState, false /*useTransitions*/);
            }
            else
            {
                if (_hideScrollBarsTimer != null && _hideScrollBarsTimer.IsRunning)
                {
                    _hideScrollBarsTimer.Stop();
                    _hideScrollBarsTimer.Start();
                }

                // Mouse indicators dominate if they are already showing or if we have set the flag to prefer them.
                if (_preferMouseIndicators || _showingMouseIndicators)
                {
                    if (this.AreBothScrollBarsVisible && (_isPointerOverHorizontalScrollBar || _isPointerOverVerticalScrollBar))
                    {
                        _proposedScrollBarsState = ScrollBarVisualState.MouseIndicatorFull;
                    }
                    else
                    {
                        _proposedScrollBarsState = ScrollBarVisualState.MouseIndicator;
                    }

                    _showingMouseIndicators = true;
                }
                else
                {
                    _proposedScrollBarsState = ScrollBarVisualState.TouchIndicator;
                }

                // Select the proper state for the scroll bars separator square within the GroupScrollBarsSeparator group:
                if (UISettingsHelper.AreSettingsEnablingAnimations)
                {
                    // When OS animations are turned on, show the square when a scroll bar is shown unless the DataGrid is disabled, using an animation.
                    _proposedScrollBarsSeparatorState =
                        this.IsEnabled &&
                        _proposedScrollBarsState == ScrollBarVisualState.MouseIndicatorFull ?
                        ScrollBarsSeparatorVisualState.SeparatorExpanded : ScrollBarsSeparatorVisualState.SeparatorCollapsed;
                }
                else
                {
                    // OS animations are turned off. Show or hide the square depending on the presence of a scroll bars, without an animation.
                    // When the DataGrid is disabled, hide the square in sync with the scroll bar(s).
                    if (_proposedScrollBarsState == ScrollBarVisualState.MouseIndicatorFull)
                    {
                        _proposedScrollBarsSeparatorState = this.IsEnabled ? ScrollBarsSeparatorVisualState.SeparatorExpandedWithoutAnimation : ScrollBarsSeparatorVisualState.SeparatorCollapsed;
                    }
                    else
                    {
                        _proposedScrollBarsSeparatorState = this.IsEnabled ? ScrollBarsSeparatorVisualState.SeparatorCollapsedWithoutAnimation : ScrollBarsSeparatorVisualState.SeparatorCollapsed;
                    }
                }

                if (!UISettingsHelper.AreSettingsAutoHidingScrollBars)
                {
                    if (this.AreBothScrollBarsVisible)
                    {
                        if (UISettingsHelper.AreSettingsEnablingAnimations)
                        {
                            SwitchScrollBarsVisualStates(ScrollBarVisualState.MouseIndicatorFull, this.IsEnabled ? ScrollBarsSeparatorVisualState.SeparatorExpanded : ScrollBarsSeparatorVisualState.SeparatorCollapsed, true /*useTransitions*/);
                        }
                        else
                        {
                            SwitchScrollBarsVisualStates(ScrollBarVisualState.MouseIndicatorFull, this.IsEnabled ? ScrollBarsSeparatorVisualState.SeparatorExpandedWithoutAnimation : ScrollBarsSeparatorVisualState.SeparatorCollapsed, true /*useTransitions*/);
                        }
                    }
                    else
                    {
                        if (UISettingsHelper.AreSettingsEnablingAnimations)
                        {
                            SwitchScrollBarsVisualStates(ScrollBarVisualState.MouseIndicator, ScrollBarsSeparatorVisualState.SeparatorCollapsed, true /*useTransitions*/);
                        }
                        else
                        {
                            SwitchScrollBarsVisualStates(ScrollBarVisualState.MouseIndicator, this.IsEnabled ? ScrollBarsSeparatorVisualState.SeparatorCollapsedWithoutAnimation : ScrollBarsSeparatorVisualState.SeparatorCollapsed, true /*useTransitions*/);
                        }
                    }
                }
                else
                {
                    SwitchScrollBarsVisualStates(_proposedScrollBarsState, _proposedScrollBarsSeparatorState, true /*useTransitions*/);
                }
            }
        }

        private void StopHideScrollBarsTimer()
        {
            if (_hideScrollBarsTimer != null && _hideScrollBarsTimer.IsRunning)
            {
                _hideScrollBarsTimer.Stop();
            }
        }

        private void SwitchScrollBarsVisualStates(ScrollBarVisualState scrollBarsState, ScrollBarsSeparatorVisualState separatorState, bool useTransitions)
        {
            switch (scrollBarsState)
            {
                case ScrollBarVisualState.NoIndicator:
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateNoIndicator);

                    if (!_hasNoIndicatorStateStoryboardCompletedHandler)
                    {
                        _showingMouseIndicators = false;
                    }

                    break;
                case ScrollBarVisualState.TouchIndicator:
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateTouchIndicator);
                    break;
                case ScrollBarVisualState.MouseIndicator:
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateMouseIndicator);
                    break;
                case ScrollBarVisualState.MouseIndicatorFull:
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateMouseIndicatorFull);
                    break;
            }

            switch (separatorState)
            {
                case ScrollBarsSeparatorVisualState.SeparatorCollapsed:
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateSeparatorCollapsed);
                    break;
                case ScrollBarsSeparatorVisualState.SeparatorExpanded:
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateSeparatorExpanded);
                    break;
                case ScrollBarsSeparatorVisualState.SeparatorExpandedWithoutAnimation:
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateSeparatorExpandedWithoutAnimation);
                    break;
                case ScrollBarsSeparatorVisualState.SeparatorCollapsedWithoutAnimation:
                    VisualStates.GoToState(this, useTransitions, VisualStates.StateSeparatorCollapsedWithoutAnimation);
                    break;
            }
        }

        private void UnhookHorizontalScrollBarEvents()
        {
            if (_hScrollBar != null)
            {
                _hScrollBar.Scroll -= new ScrollEventHandler(HorizontalScrollBar_Scroll);
                _hScrollBar.PointerEntered -= new PointerEventHandler(HorizontalScrollBar_PointerEntered);
                _hScrollBar.PointerExited -= new PointerEventHandler(HorizontalScrollBar_PointerExited);
            }
        }

        private void UnhookVerticalScrollBarEvents()
        {
            if (_vScrollBar != null)
            {
                _vScrollBar.Scroll -= new ScrollEventHandler(VerticalScrollBar_Scroll);
                _vScrollBar.PointerEntered -= new PointerEventHandler(VerticalScrollBar_PointerEntered);
                _vScrollBar.PointerExited -= new PointerEventHandler(VerticalScrollBar_PointerExited);
            }
        }

        private void SetHorizontalOffset(double newHorizontalOffset)
        {
            if (_hScrollBar != null && _hScrollBar.Value != newHorizontalOffset)
            {
                _hScrollBar.Value = newHorizontalOffset;

                // Unless the control is still loading, show the scroll bars when an offset changes. Keep the existing indicator type.
                if (VisualTreeHelper.GetParent(this) != null)
                {
                    ShowScrollBars();
                }
            }
        }

        private void SetVerticalOffset(double newVerticalOffset)
        {
            VerticalOffset = newVerticalOffset;

            if (_vScrollBar != null && !DoubleUtil.AreClose(newVerticalOffset, _vScrollBar.Value))
            {
                _vScrollBar.Value = _verticalOffset;

                // Unless the control is still loading, show the scroll bars when an offset changes. Keep the existing indicator type.
                if (VisualTreeHelper.GetParent(this) != null)
                {
                    ShowScrollBars();
                }
            }
        }

        internal void UpdateHorizontalOffset(double newValue)
        {
            if (this.HorizontalOffset != newValue)
            {
                this.HorizontalOffset = newValue;

                //InvalidateColumnHeadersMeasure();
                //InvalidateRowsMeasure(true);
            }
        }
    }
}