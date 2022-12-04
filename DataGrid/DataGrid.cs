using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data.Common;
using System.Text;
using Windows.Foundation;
using ZyunUI.DataGridInternals;
using ZyunUI.Utilities;
using static ZyunUI.DataGridInternals.DataGridError;
using DiagnosticsDebug = System.Diagnostics.Debug;

namespace ZyunUI
{
    [TemplatePart(Name = DataGrid.DATAGRID_elementCellsPresenterName, Type = typeof(DataGridCellsPresenter))]
    [TemplatePart(Name = DataGrid.DATAGRID_elementColumnHeadersPresenterName, Type = typeof(DataGridColumnHeadersPresenter))]
    [TemplatePart(Name = DataGrid.DATAGRID_elementRowHeadersPresenterName, Type = typeof(DataGridRowHeadersPresenter))]
    [TemplatePart(Name = DataGrid.DATAGRID_elementFrozenColumnScrollBarSpacerName, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = DataGrid.DATAGRID_elementHorizontalScrollBarName, Type = typeof(ScrollBar))]
    [TemplatePart(Name = DataGrid.DATAGRID_elementVerticalScrollBarName, Type = typeof(ScrollBar))]
    [TemplateVisualState(Name = VisualStates.StateDisabled, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = VisualStates.StateNormal, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = VisualStates.StateTouchIndicator, GroupName = VisualStates.GroupScrollBars)]
    [TemplateVisualState(Name = VisualStates.StateMouseIndicator, GroupName = VisualStates.GroupScrollBars)]
    [TemplateVisualState(Name = VisualStates.StateMouseIndicatorFull, GroupName = VisualStates.GroupScrollBars)]
    [TemplateVisualState(Name = VisualStates.StateNoIndicator, GroupName = VisualStates.GroupScrollBars)]
    [TemplateVisualState(Name = VisualStates.StateSeparatorExpanded, GroupName = VisualStates.GroupScrollBarsSeparator)]
    [TemplateVisualState(Name = VisualStates.StateSeparatorCollapsed, GroupName = VisualStates.GroupScrollBarsSeparator)]
    [TemplateVisualState(Name = VisualStates.StateSeparatorExpandedWithoutAnimation, GroupName = VisualStates.GroupScrollBarsSeparator)]
    [TemplateVisualState(Name = VisualStates.StateSeparatorCollapsedWithoutAnimation, GroupName = VisualStates.GroupScrollBarsSeparator)]
    [TemplateVisualState(Name = VisualStates.StateInvalid, GroupName = VisualStates.GroupValidation)]
    [TemplateVisualState(Name = VisualStates.StateValid, GroupName = VisualStates.GroupValidation)]
    [StyleTypedProperty(Property = "CellStyle", StyleTargetType = typeof(DataGridCell))]
    [StyleTypedProperty(Property = "ColumnHeaderStyle", StyleTargetType = typeof(DataGridColumnHeader))]
    [StyleTypedProperty(Property = "RowHeaderStyle", StyleTargetType = typeof(DataGridRowHeader))]
    public partial class DataGrid : Control
    {
        private enum ScrollBarVisualState
        {
            NoIndicator,
            TouchIndicator,
            MouseIndicator,
            MouseIndicatorFull
        }

        private enum ScrollBarsSeparatorVisualState
        {
            SeparatorCollapsed,
            SeparatorExpanded,
            SeparatorExpandedWithoutAnimation,
            SeparatorCollapsedWithoutAnimation
        }

#if FEATURE_VALIDATION_SUMMARY
        private const string DATAGRID_elementValidationSummary = "ValidationSummary";
#endif
        private const string DATAGRID_elementRootName = "Root";
        private const string DATAGRID_elementCellsPresenterName = "CellsPresenter";
        private const string DATAGRID_elementColumnHeadersPresenterName = "ColumnHeadersPresenter";
        private const string DATAGRID_elementFrozenColumnScrollBarSpacerName = "FrozenColumnScrollBarSpacer";
        private const string DATAGRID_elementHorizontalScrollBarName = "HorizontalScrollBar";
        private const string DATAGRID_elementRowHeadersPresenterName = "RowHeadersPresenter";
        private const string DATAGRID_elementTopLeftCornerHeaderName = "TopLeftCornerHeader";
        private const string DATAGRID_elementTopRightCornerHeaderName = "TopRightCornerHeader";
        private const string DATAGRID_elementBottomRightCornerHeaderName = "BottomRightCorner";
        private const string DATAGRID_elementVerticalScrollBarName = "VerticalScrollBar";

        private const bool DATAGRID_defaultAutoGenerateColumns = true;
        private const bool DATAGRID_defaultCanUserReorderColumns = true;
        private const bool DATAGRID_defaultCanUserResizeColumns = true;
        private const bool DATAGRID_defaultCanUserSortColumns = true;
        private const DataGridGridLinesVisibility DATAGRID_defaultGridLinesVisibility = DataGridGridLinesVisibility.None;
        private const DataGridHeadersVisibility DATAGRID_defaultHeadersVisibility = DataGridHeadersVisibility.Column;
        private const DataGridSelectionMode DATAGRID_defaultSelectionMode = DataGridSelectionMode.Extended;
        private const ScrollBarVisibility DATAGRID_defaultScrollBarVisibility = ScrollBarVisibility.Auto;

        /// <summary>
        /// The default order to use for columns when there is no <see cref="DisplayAttribute.Order"/>
        /// value available for the property.
        /// </summary>
        /// <remarks>
        /// The value of 10,000 comes from the DataAnnotations spec, allowing
        /// some properties to be ordered at the beginning and some at the end.
        /// </remarks>
        private const int DATAGRID_defaultColumnDisplayOrder = 10000;

        private const double DATAGRID_horizontalGridLinesThickness = 1;
        private const double DATAGRID_minimumRowHeaderWidth = 4;
        private const double DATAGRID_minimumColumnHeaderHeight = 4;
        internal const double DATAGRID_maximumStarColumnWidth = 10000;
        internal const double DATAGRID_minimumStarColumnWidth = 0.001;
        private const double DATAGRID_mouseWheelDeltaDivider = 4.0;
        private const double DATAGRID_maxHeadersThickness = 32768;

        private const double DATAGRID_defaultRowHeight = 22;
        internal const double DATAGRID_defaultRowGroupSublevelIndent = 20;
        private const double DATAGRID_defaultMinColumnWidth = 20;
        private const double DATAGRID_defaultMaxColumnWidth = double.PositiveInfinity;

        private const byte DATAGRIDROW_defaultMinHeight = 0;
        internal const int DATAGRIDROW_maximumHeight = 65536;
        internal const double DATAGRIDROW_minimumHeight = 0;

        private const double DATAGRID_defaultIncrementalLoadingThreshold = 3.0;
        private const double DATAGRID_defaultDataFetchSize = 3.0;

        // 2 seconds delay used to hide the scroll bars for example when OS animations are turned off.
        private const int DATAGRID_noScrollBarCountdownMs = 2000;

        // Used to work around double arithmetic rounding.
        private const double DATAGRID_roundingDelta = 0.0001;
 
        // the number of pixels of DisplayData.FirstDisplayedScrollingRow which are not displayed
        private int _noCurrentCellChangeCount;
        private int _noFocusedColumnChangeCount;
        private int _noSelectionChangeCount;

        private double _oldEdgedRowsHeightCalculated = 0.0;
        private bool _measured;

        private int _editingColumnIndex;
        private bool _columnHeaderHasFocus;

        private UIElement _bottomRightCorner;
        private DataGridColumnHeadersPresenter _columnHeadersPresenter;
        private DataGridRowHeadersPresenter _rowHeadersPresenter;
        private ScrollBar _hScrollBar;
        private ScrollBar _vScrollBar;
        private DataGridCellsPresenter _cellsPresenter;

        private FrameworkElement _frozenColumnScrollBarSpacer;
        private ContentControl _topLeftCornerHeader;
       
        private readonly List<DataGridRow> m_rows = new List<DataGridRow>();
        private DataGridCellCoordinates _currentCellCoordinates;
        private Size? _datagridAvailableSize;

        private FocusInputDeviceKind _focusInputDevice;

        /// <summary>
        /// Occurs when the <see cref="ZyunUI.Controls.DataGridColumn.DisplayIndex"/>
        /// property of a column changes.
        /// </summary>
        public event EventHandler<DataGridColumnEventArgs> ColumnDisplayIndexChanged;

        /// <summary>
        /// Occurs when the user drops a column header that was being dragged using the mouse.
        /// </summary>
        public event EventHandler<DragCompletedEventArgs> ColumnHeaderDragCompleted;

        /// <summary>
        /// Occurs one or more times while the user drags a column header using the mouse.
        /// </summary>
        public event EventHandler<DragDeltaEventArgs> ColumnHeaderDragDelta;

        /// <summary>
        /// Occurs when the user begins dragging a column header using the mouse.
        /// </summary>
        public event EventHandler<DragStartedEventArgs> ColumnHeaderDragStarted;

        /// <summary>
        /// Raised when column reordering ends, to allow subscribers to clean up.
        /// </summary>
        public event EventHandler<DataGridColumnEventArgs> ColumnReordered;

        /// <summary>
        /// Raised when starting a column reordering action.  Subscribers to this event can
        /// set tooltip and caret UIElements, constrain tooltip position, indicate that
        /// a preview should be shown, or cancel reordering.
        /// </summary>
        public event EventHandler<DataGridColumnReorderingEventArgs> ColumnReordering;

        /// <summary>
        /// Occurs when a different cell becomes the current cell.
        /// </summary>
        public event EventHandler<EventArgs> CurrentCellChanged;

        /// <summary>
        /// Occurs when the <see cref="ZyunUI.Controls.DataGridColumn"/> sorting request is triggered.
        /// </summary>
        public event EventHandler<DataGridColumnEventArgs> Sorting;

        public DataGrid()
        {
            DefaultStyleKey = typeof(DataGrid);
            CollectionView.VectorChanged += CollectionView_VectorChanged;

            this.ColumnHeaderInteractionInfo = new DataGridColumnHeaderInteractionInfo();
            this.ColumnsInternal = CreateColumnsInstance();
            this.DisplayData = new DataGridDisplayData(this);

            _focusInputDevice = FocusInputDeviceKind.None;
            _proposedScrollBarsState = ScrollBarVisualState.NoIndicator;
            _proposedScrollBarsSeparatorState = ScrollBarsSeparatorVisualState.SeparatorCollapsed;
        }

        internal DataGridDisplayData DisplayData
        {
            get;
            private set;
        }

        /// <summary>
        /// Comparator class so we can sort list by the display index
        /// </summary>
        public class DisplayIndexComparer : IComparer<DataGridColumn>
        {
            // Calls CaseInsensitiveComparer.Compare with the parameters reversed.
            int IComparer<DataGridColumn>.Compare(DataGridColumn x, DataGridColumn y)
            {
                return (x.DisplayIndex < y.DisplayIndex) ? -1 : 1;
            }
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see
        /// cref="M:System.Windows.FrameworkElement.ApplyTemplate" /> .
        /// </summary>
        protected override void OnApplyTemplate()
        {
            _hasNoIndicatorStateStoryboardCompletedHandler = false;
            _keepScrollBarsShowing = false;

            if (_columnHeadersPresenter != null)
            {
                // If we're applying a new template, we want to remove the old column headers first
                _columnHeadersPresenter.Children.Clear();
            }

            _columnHeadersPresenter = GetTemplateChild(DATAGRID_elementColumnHeadersPresenterName) as DataGridColumnHeadersPresenter;
            if (_columnHeadersPresenter != null)
            {
                _columnHeadersPresenter.OwningGrid = this;

                // Columns were added before our Template was applied, add the ColumnHeaders now
                List<DataGridColumn> sortedInternal = new List<DataGridColumn>(this.ColumnsItemsInternal);
                sortedInternal.Sort(new DisplayIndexComparer());
                foreach (DataGridColumn column in sortedInternal)
                {
                    InsertDisplayedColumnHeader(column);
                }
            }

            if (_cellsPresenter != null)
            {
                // If we're applying a new template, we want to remove the old rows first
                this.UnloadElements(false /*recycle*/);
            }

            _cellsPresenter = GetTemplateChild(DATAGRID_elementCellsPresenterName) as DataGridCellsPresenter;
            if (_cellsPresenter != null)
            {
                _cellsPresenter.OwningGrid = this;
                //InvalidateRowHeightEstimate();
                //UpdateRowsPresenterManipulationMode(true /*horizontalMode*/, true /*verticalMode*/);
            }

            _frozenColumnScrollBarSpacer = GetTemplateChild(DATAGRID_elementFrozenColumnScrollBarSpacerName) as FrameworkElement;

            if (_hScrollBar != null)
            {
                _isHorizontalScrollBarInteracting = false;
                _isPointerOverHorizontalScrollBar = false;
                UnhookHorizontalScrollBarEvents();
            }

            _hScrollBar = GetTemplateChild(DATAGRID_elementHorizontalScrollBarName) as ScrollBar;
            if (_hScrollBar != null)
            {
                _hScrollBar.IsTabStop = false;
                _hScrollBar.Maximum = 0.0;
                _hScrollBar.Orientation = Orientation.Horizontal;
                _hScrollBar.Visibility = Visibility.Collapsed;
                HookHorizontalScrollBarEvents();
            }

            if (_vScrollBar != null)
            {
                _isVerticalScrollBarInteracting = false;
                _isPointerOverVerticalScrollBar = false;
                UnhookVerticalScrollBarEvents();
            }

            _vScrollBar = GetTemplateChild(DATAGRID_elementVerticalScrollBarName) as ScrollBar;
            if (_vScrollBar != null)
            {
                _vScrollBar.IsTabStop = false;
                _vScrollBar.Maximum = 0.0;
                _vScrollBar.Orientation = Orientation.Vertical;
                _vScrollBar.Visibility = Visibility.Collapsed;
                HookVerticalScrollBarEvents();
            }

            _topLeftCornerHeader = GetTemplateChild(DATAGRID_elementTopLeftCornerHeaderName) as ContentControl;
            EnsureTopLeftCornerHeader(); // EnsureTopLeftCornerHeader checks for a null _topLeftCornerHeader;
             
            _bottomRightCorner = GetTemplateChild(DATAGRID_elementBottomRightCornerHeaderName) as UIElement;
        }

        private void EnsureTopLeftCornerHeader()
        {
            if (_topLeftCornerHeader != null)
            {
                _topLeftCornerHeader.Visibility = this.HeadersVisibility == DataGridHeadersVisibility.All ? Visibility.Visible : Visibility.Collapsed;

                if (_topLeftCornerHeader.Visibility == Visibility.Visible)
                {
                    if (!double.IsNaN(this.RowHeaderWidth))
                    {
                        // RowHeaderWidth is set explicitly so we should use that
                        _topLeftCornerHeader.Width = this.RowHeaderWidth;
                    }
                    //else if (this.VisibleSlotCount > 0)
                    //{
                    //    // RowHeaders AutoSize and we have at least 1 row so take the desired width
                    //    _topLeftCornerHeader.Width = this.RowHeadersDesiredWidth;
                    //}
                }
            }
        }

        private void UnloadElements(bool recycle)
        {

        }
        internal bool IsMeasured { get { return _measured; } }

        internal bool AreColumnHeadersVisible
        {
            get
            {
                return (this.HeadersVisibility & DataGridHeadersVisibility.Column) == DataGridHeadersVisibility.Column;
            }
        }

        internal bool AreRowHeadersVisible
        {
            get
            {
                return (this.HeadersVisibility & DataGridHeadersVisibility.Row) == DataGridHeadersVisibility.Row;
            }
        }

        internal bool ContainsFocus
        {
            get;
            private set;
        }

        internal double ViewportHeight
        {
            get;
            private set;
        }

        internal double ViewportWidth
        {
            get;
            private set;
        }

        internal double VisibleEdgedRowsHeight
        {
            get;
            private set;
        }

        internal double ActualColumnHeaderHeight
        {
            get;
            private set;
        }

        internal double ActualRowHeaderWidth
        {
            get;
            private set;
        }

        

        internal int NoCurrentCellChangeCount
        {
            get
            {
                return _noCurrentCellChangeCount;
            }

            set
            {
                DiagnosticsDebug.Assert(value >= 0, "Expected positive NoCurrentCellChangeCount.");
                _noCurrentCellChangeCount = value;
                if (value == 0)
                {
                    FlushCurrentCellChanged();
                }
            }
        }

        internal bool InDisplayIndexAdjustments
        {
            get;
            set;
        }

        internal double HorizontalAdjustment
        {
            get;
            private set;
        }

        // the sum of the widths in pixels of the scrolling columns preceding
        // the first displayed scrolling column
        internal double HorizontalOffset
        {
            get
            {
                return _horizontalOffset;
            }

            set
            {
                if (value < 0)
                {
                    value = 0;
                }

                double widthNotVisible = Math.Max(0, this.ColumnsInternal.VisibleEdgedColumnsWidth - this.ViewportWidth);
                if (value > widthNotVisible)
                {
                    value = widthNotVisible;
                }

                if (value == _horizontalOffset)
                {
                    return;
                }

                SetHorizontalOffset(value);

                _horizontalOffset = value;

                this.DisplayData.FirstDisplayedCol = ComputeFirstVisibleScrollingColumn();

                // update the lastTotallyDisplayedScrollingCol
                ComputeDisplayedColumns();
            }
        }

        internal ScrollBar HorizontalScrollBar
        {
            get
            {
                return _hScrollBar;
            }
        }

        internal double VerticalOffset
        {
            get
            {
                return _verticalOffset;
            }

            set
            {
                bool loadMoreDataFromIncrementalItemsSource = _verticalOffset < value;

                _verticalOffset = value;

                if (loadMoreDataFromIncrementalItemsSource)
                {
                    //LoadMoreDataFromIncrementalItemsSource();
                }
            }
        }

        internal ScrollBar VerticalScrollBar
        {
            get
            {
                return _vScrollBar;
            }
        }

        internal bool LoadingOrUnloadingRow
        {
            get;
            private set;
        }

        internal double NegVerticalOffset
        {
            get;
            private set;
        }

        private void FlushCurrentCellChanged()
        {
        }

        internal DataGridColumn FocusedColumn
        {
            get;
            set;
        }

        internal DataGridColumnHeaderInteractionInfo ColumnHeaderInteractionInfo
        {
            get;
            set;
        }

        internal bool ColumnHeaderHasFocus
        {
            get
            {
                return _columnHeaderHasFocus;
            }

            set
            {
                DiagnosticsDebug.Assert(!value || (this.ColumnHeaders != null && this.AreColumnHeadersVisible), "Expected value==False || (non-null ColumnHeaders and AreColumnHeadersVisible==True)");

                if (_columnHeaderHasFocus != value)
                {
                    _columnHeaderHasFocus = value;
                }
            }
        }

        internal DataGridColumnHeadersPresenter ColumnHeaders
        {
            get
            {
                return _columnHeadersPresenter;
            }
        }

        internal DataGridColumnCollection ColumnsInternal
        {
            get;
            private set;
        }

        internal List<DataGridColumn> ColumnsItemsInternal
        {
            get
            {
                return this.ColumnsInternal.ItemsInternal;
            }
        }

       

        /// <summary>
        /// Handles changes in the <see cref="ItemsSource" /> property.
        /// </summary>
        private void ItemsSourceChanged()
        {
            if (this.ItemsSource != null)
            {
                CollectionView.Source = this.ItemsSource;
                if (this.AutoGenerateColumns && this.Columns.Count == 0)
                {
                    
                }
            }
            else
            {
                this.CollectionView.Clear();
            }
        }

        /// <summary>
        /// Gets the column definitions.
        /// </summary>
        /// <value>The column definitions.</value>
        public ObservableCollection<DataGridColumn> Columns
        {
            get
            {
                // we use a backing field here because the field's type
                // is a subclass of the property's
                return this.ColumnsInternal;
            }
        }

        public List<DataGridRow> Rows { get; } = new List<DataGridRow>();

        public DataGridTextColumn RowHeaderColumn { get; } = new DataGridTextColumn();

        public int RowCount => Rows.Count;
        public int ColumnCount => Columns.Count;

        /// <summary>
        /// Gets the collection view.
        /// </summary>
        /// <value>
        /// The collection view.
        /// </value>
        public AdvancedCollectionView CollectionView { get; } = new AdvancedCollectionView();
 
        
        /// <summary>
        /// Gets the default row header.
        /// </summary>
        /// <param name="j">The j.</param>
        /// <returns>
        /// The get row header.
        /// </returns>
        private String DefaultRowHeader(int j)
        { 
            return CellRef.ToRowName(j);
        }

        /// <summary>
        /// Refreshes the collection view and updates the grid content, if the ItemsSource is not implementing INotifyCollectionChanged.
        /// </summary>
        private void RefreshIfRequired()
        {
            if (!(this.ItemsSource is INotifyCollectionChanged))
            {
               this.CollectionView.Refresh();
            }
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            // Delay layout until after the initial measure to avoid invalid calculations when the
            // DataGrid is not part of the visual tree
            if (!_measured)
            {
                _measured = true;
            }

            // This is a shortcut to skip layout if we don't have any columns
            if (this.ColumnsInternal.Count == 0)
            {
                if (_hScrollBar != null && _hScrollBar.Visibility != Visibility.Collapsed)
                {
                    _hScrollBar.Visibility = Visibility.Collapsed;
                }

                if (_vScrollBar != null && _vScrollBar.Visibility != Visibility.Collapsed)
                {
                    _vScrollBar.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                bool invalidate = !this._datagridAvailableSize.HasValue || availableSize.Width != this._datagridAvailableSize.Value.Width;
                _datagridAvailableSize = availableSize;
                if (invalidate)
                {
                    Refresh();
                }
            }

            return base.MeasureOverride(availableSize);
        }

        internal void Refresh()
        {
            if (!_datagridAvailableSize.HasValue) return;
            Size availableSize = _datagridAvailableSize.Value;

            //1. 先计算RowHeaderWidth
            MeasureRowHeaders();
            double measureWidth = availableSize.Width;
            if (!Double.IsNaN(measureWidth))
            {
                measureWidth -= ActualRowHeaderWidth;

            }
            //2. ColumnHeaders
            MeasureColumnHeaders(measureWidth);
            //3. Cells
            MeasureCells(measureWidth);

            ViewportWidth = measureWidth;
            ViewportHeight = availableSize.Height;
            if (!Double.IsNaN(ViewportHeight))
            {
                ViewportHeight -= ActualColumnHeaderHeight;
            }

            UpdateDisplayedColumns();
            UpdateDisplayedRows(0, ViewportHeight);

            ComputeScrollBarsLayout();
            ShowScrollBars();
        }

        internal void MeasureCells(double measureWidth)
        {
            bool bFinityWidth = true;
            //如果宽度是无限，则不计算Star宽度，当作Auto
            if (Double.IsInfinity(measureWidth)) bFinityWidth = false;

            var columns = ColumnsInternal.GetDisplayedColumns();
             
            Size measureSize = new Size(0, double.PositiveInfinity);

            double usedWidth = 0;
            double starsToDistribute = 0;
            foreach (DataGridColumn column in columns)
            {
                var columnWidth = column.Width;
                if (columnWidth.IsAuto)
                {
                    measureSize.Width = column.ActualMaxWidth;
                    column.ActualWidth = this.MeasureCells(column, measureSize, true);
                    usedWidth += column.ActualWidth;
                }
                else if (columnWidth.IsAbsolute)
                {
                    column.ActualWidth = columnWidth.Value;
                    usedWidth += column.ActualWidth;

                    measureSize.Width = column.ActualWidth;
                    this.MeasureCells(column, measureSize, false);
                }
                else if (columnWidth.IsStar)
                {
                    column.ActualWidth = 0;
                    if (bFinityWidth)
                    {
                        starsToDistribute += columnWidth.Value;
                    }
                    else // as auto column
                    {
                        measureSize.Width = column.ActualMaxWidth;
                        column.ActualWidth = this.MeasureCells(column, measureSize, true);
                        usedWidth += column.ActualWidth;
                    }
                }
            }
             
            usedWidth = 0.0;
            //starsize width column
            if (bFinityWidth)
            {
                var widthPerStar = Math.Max((measureWidth - usedWidth) / starsToDistribute, 0);
                foreach (DataGridColumn column in columns)
                {
                    var columnWidth = column.Width;
                    if (columnWidth.IsStar)
                    {
                        column.ActualWidth = widthPerStar * columnWidth.Value;
                        usedWidth += column.ActualWidth;

                        if (column.ActualWidth > 0)
                        {
                            measureSize.Width = column.ActualWidth;
                            this.MeasureCells(column, measureSize, false);
                        }
                    }
                }
            }

            ColumnsInternal.EnsureVisibleEdgedColumnsWidth();
            EnsureVisibleEdgedRowsHeight();
        }

        private void EnsureVisibleEdgedRowsHeight()
        {
            VisibleEdgedRowsHeight = 0;
            var rows = Rows;
            for (int i = 0; i < rows.Count; i++)
            {
                VisibleEdgedRowsHeight += rows[i].ActualHeight;
            }
        }

        private void MeasureColumnHeaders(double measureWidth)
        {
            bool bFinityWidth = true;
            //如果宽度是无限，则不计算Star宽度，当作Auto
            if (Double.IsInfinity(measureWidth)) bFinityWidth = false;

            double height = this.ColumnHeaderHeight;
            Size measureSize = new Size(0, height);
            bool autoSizeHeight = false;
            if (double.IsNaN(height))
            {
                autoSizeHeight = true;
                height = 0;
                measureSize.Height = Double.PositiveInfinity;
            }
             
            var columns = ColumnsInternal.GetDisplayedColumns();           
            
            Size desiredSize = new Size();
            double usedWidth = 0;
            double starsToDistribute = 0;

            foreach (DataGridColumn column in columns)
            {
                DataGridColumnHeader columnHeader = column.HeaderCell;
                var columnWidth = column.Width;
                if (columnWidth.IsAuto)
                {
                    measureSize.Width = column.ActualMaxWidth;

                    columnHeader.Measure(measureSize);
                    desiredSize = columnHeader.DesiredSize;
                    if(desiredSize.Height > height)
                        height = desiredSize.Height;

                    column.ActualWidth = desiredSize.Width;
                    usedWidth += column.ActualWidth;
                }
                else if (columnWidth.IsAbsolute)
                {
                    column.ActualWidth = columnWidth.Value;
                    if (autoSizeHeight) 
                    { 
                        measureSize.Width = columnWidth.Value;
                        columnHeader.Measure(measureSize);
                        desiredSize = columnHeader.DesiredSize;
                        if (desiredSize.Height > height)
                            height = desiredSize.Height;

                        column.ActualWidth = desiredSize.Width;
                    }
                    usedWidth += column.ActualWidth;
                }
                else if (columnWidth.IsStar)
                {
                    column.ActualWidth = 0;
                    if (bFinityWidth)
                    {
                        starsToDistribute += columnWidth.Value;
                    }
                    else // as auto column
                    {
                        columnHeader.Measure(measureSize);
                        desiredSize = columnHeader.DesiredSize;
                        if (desiredSize.Height > height)
                            height = desiredSize.Height;

                        column.ActualWidth = desiredSize.Width;
                    }
                }
            }

            //starsize width column
            if (bFinityWidth)
            {
                var widthPerStar = Math.Max((measureWidth - usedWidth) / starsToDistribute, 0);
                foreach (DataGridColumn column in columns)
                {
                    var columnWidth = column.Width;
                    if (columnWidth.IsStar)
                    {
                        column.ActualWidth = widthPerStar * columnWidth.Value;
                        if (column.ActualWidth > 0)
                        {
                            measureSize.Width = column.ActualWidth;
                            this.MeasureCells(column, measureSize, false);

                            if (desiredSize.Height > height)
                                height = desiredSize.Height;
                        }
                    }
                }
            }

            ActualColumnHeaderHeight = height;
        }

        private void MeasureRowHeaders()
        {
            var columnWidth = RowHeaderColumn.Width;
            var rows = Rows;
            DataGridRow row;
            object dataItem;
            AdvancedCollectionView collectionView = CollectionView;

            DataGridRowHeader rowHeader = new DataGridRowHeader();
            double width = 0;

            Size desiredSize = new Size();
            bool isMeaseureRow = false;
            bool isMeaseureCol = false ;
            Size measureSize = new Size(0, double.PositiveInfinity);

            if (columnWidth.IsAuto || columnWidth.IsStar)
            {
                isMeaseureCol = true;
                measureSize.Width = RowHeaderColumn.MaxWidth;
            }
            else
            {
                isMeaseureCol = false;
                measureSize.Width = columnWidth.Value;
                width = columnWidth.Value;
            }

            for (int i = 0; i < rows.Count; i++)
            {
                row = rows[i];
                isMeaseureRow = false;
                if (RowHeaderColumn.IsAutoCellHeight || (Double.IsInfinity(this.RowHeight) && Double.IsInfinity(row.Height)))
                {
                    isMeaseureRow = true;
                }

                if (isMeaseureCol || isMeaseureRow)
                {
                    dataItem = collectionView[i];
                    rowHeader.DataContext = dataItem;

                    rowHeader.Measure(measureSize);
                    desiredSize = rowHeader.DesiredSize;
                }

                if (isMeaseureRow)
                {
                    if (Double.IsNaN(row.ActualHeight) || row.ActualHeight < desiredSize.Height)
                        row.ActualHeight = desiredSize.Height;
                }
                else if (!Double.IsNaN(row.Height))
                {
                    row.ActualHeight = row.Height;
                }
                else if (!Double.IsNaN(this.RowHeight))
                {
                    row.ActualHeight = this.RowHeight;
                }

                if (isMeaseureCol)
                {
                    if (desiredSize.Width > width) width = desiredSize.Width;
                }
            }

            if (width < RowHeaderColumn.MinWidth) 
                width = RowHeaderColumn.MinWidth;

            ActualRowHeaderWidth = width;
        }

        private double MeasureCells(DataGridColumn column, Size measureSize, bool isMeaseureCol)
        {
            var rows = Rows;
            DataGridRow row;
            object dataItem;
            AdvancedCollectionView collectionView = CollectionView;

            DataGridCell gridCell = column.CreateGridCell(null);
            _cellsPresenter.Children.Add(gridCell);
           
            double width = 0;

            Size desiredSize = new Size();
            bool isMeaseureRow;
            for (int i = 0; i < rows.Count; i++)
            {
                row = rows[i];
                isMeaseureRow = false;
                if (column.IsAutoCellHeight || (Double.IsNaN(this.RowHeight) && Double.IsNaN(row.Height)))
                {
                    isMeaseureRow = true;
                }

                if (isMeaseureCol || isMeaseureRow)
                {
                    dataItem = collectionView[i];
                    gridCell.DataContext = dataItem;
                     
                    gridCell.Measure(measureSize);
                    desiredSize = gridCell.DesiredSize;
                }

                if (isMeaseureRow)
                {
                    if (Double.IsNaN(row.ActualHeight) || row.ActualHeight < desiredSize.Height)
                        row.ActualHeight = desiredSize.Height;
                }
                else if (!Double.IsNaN(row.Height))
                {
                    row.ActualHeight = row.Height;
                }
                else if (!Double.IsNaN(this.RowHeight))
                {
                    row.ActualHeight = this.RowHeight;
                }

                if (isMeaseureCol)
                {
                    if (desiredSize.Width > width) width = desiredSize.Width;
                }
            }

            if (width < column.ActualWidth) width = column.ActualWidth;
            else if (width < column.ActualMinWidth) width = column.ActualMinWidth;

            _cellsPresenter.Children.Remove(gridCell);

            return width;
        }

        private void ComputeScrollBarsLayout()
        {
            double viewWidth = this.ViewportWidth;
            double viewHeight = this.ViewportHeight;

            bool allowHorizScrollBar = false;
            bool forceHorizScrollBar = false;
            if (_hScrollBar != null)
            {
                forceHorizScrollBar = this.HorizontalScrollBarVisibility == ScrollBarVisibility.Visible;
                allowHorizScrollBar = forceHorizScrollBar || (this.ColumnsInternal.VisibleColumnCount > 0 &&
                    this.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled &&
                    this.HorizontalScrollBarVisibility != ScrollBarVisibility.Hidden);
            }

            bool allowVertScrollBar = false;
            bool forceVertScrollBar = false;
            if (_vScrollBar != null)
            {
                forceVertScrollBar = this.VerticalScrollBarVisibility == ScrollBarVisibility.Visible;
                allowVertScrollBar = forceVertScrollBar || (this.ColumnsItemsInternal.Count > 0 &&
                    this.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled &&
                    this.VerticalScrollBarVisibility != ScrollBarVisibility.Hidden);
             }

            // Now cellsWidth is the width potentially available for displaying data cells.
            // Now cellsHeight is the height potentially available for displaying data cells.
            bool needHorizScrollBar = false;
            bool needVertScrollBar = false;

            double totalVisibleWidth = this.ColumnsInternal.VisibleEdgedColumnsWidth;
            double totalVisibleFrozenWidth = this.ColumnsInternal.GetVisibleFrozenEdgedColumnsWidth();
            double totalVisibleHeight = this.VisibleEdgedRowsHeight;

            if (!forceHorizScrollBar && !forceVertScrollBar)
            {
                bool needHorizScrollBarWithoutVertScrollBar = false;

                if (allowHorizScrollBar &&
                    DoubleUtil.GreaterThan(totalVisibleWidth, viewWidth) &&
                    DoubleUtil.LessThan(totalVisibleFrozenWidth, viewWidth))
                {
                    needHorizScrollBarWithoutVertScrollBar = needHorizScrollBar = true;
                }
 
                if (allowVertScrollBar &&
                    DoubleUtil.GreaterThan(totalVisibleHeight, viewHeight))
                { 
                    needVertScrollBar = true;
                }
            }
            else if (forceHorizScrollBar && !forceVertScrollBar)
            {
                if (allowVertScrollBar)
                {
                    if (DoubleUtil.GreaterThan(totalVisibleHeight, viewHeight))
                    { 
                        needVertScrollBar = true;
                    }
                }
                needHorizScrollBar = totalVisibleWidth > viewWidth && totalVisibleFrozenWidth < viewWidth;
            }
            else if (!forceHorizScrollBar && forceVertScrollBar)
            {
                if (allowHorizScrollBar)
                {
                    if (viewWidth > 0 &&
                        DoubleUtil.GreaterThan(totalVisibleWidth, viewWidth) &&
                        DoubleUtil.LessThan(totalVisibleFrozenWidth, viewWidth))
                    { 
                        needHorizScrollBar = true; 
                    } 
                }

                needVertScrollBar = true;
            }
            else
            {
                needVertScrollBar = true;
                needHorizScrollBar = totalVisibleWidth > viewWidth && totalVisibleFrozenWidth < viewWidth;
            }

            UpdateHorizontalScrollBar(needHorizScrollBar, forceHorizScrollBar, totalVisibleWidth, totalVisibleFrozenWidth, viewWidth);
            UpdateVerticalScrollBar(needVertScrollBar, forceVertScrollBar, totalVisibleHeight, viewHeight);

            if (_bottomRightCorner != null)
            {
                // Show the BottomRightCorner when both scrollbars are visible.
                _bottomRightCorner.Visibility =
                    _hScrollBar != null && _hScrollBar.Visibility == Visibility.Visible &&
                    _vScrollBar != null && _vScrollBar.Visibility == Visibility.Visible ?
                        Visibility.Visible : Visibility.Collapsed;
            }
        }

        private bool AreAllScrollBarsCollapsed
        {
            get
            {
                return (_hScrollBar == null || _hScrollBar.Visibility == Visibility.Collapsed) &&
                       (_vScrollBar == null || _vScrollBar.Visibility == Visibility.Collapsed);
            }
        }

        private bool AreBothScrollBarsVisible
        {
            get
            {
                return _hScrollBar != null && _hScrollBar.Visibility == Visibility.Visible &&
                       _vScrollBar != null && _vScrollBar.Visibility == Visibility.Visible;
            }
        }

        private void UpdateHorizontalScrollBar(bool needHorizScrollBar, bool forceHorizScrollBar, double totalVisibleWidth, double totalVisibleFrozenWidth, double cellsWidth)
        {
            if (_hScrollBar != null)
            {
                if (needHorizScrollBar || forceHorizScrollBar)
                {
                    // ..........viewportSize
                    //         v---v
                    // |<|_____|###|>|
                    //   ^     ^
                    //   min   max

                    // we want to make the relative size of the thumb reflect the relative size of the viewing area
                    // viewportSize / (max + viewportSize) = cellsWidth / max
                    // -> viewportSize = max * cellsWidth / (max - cellsWidth)

                    // always zero
                    _hScrollBar.Minimum = 0;
                    if (needHorizScrollBar)
                    {
                        // maximum travel distance -- not the total width
                        _hScrollBar.Maximum = totalVisibleWidth - cellsWidth;
                        DiagnosticsDebug.Assert(totalVisibleFrozenWidth >= 0, "Expected positive totalVisibleFrozenWidth.");
                        if (_frozenColumnScrollBarSpacer != null)
                        {
                            _frozenColumnScrollBarSpacer.Width = totalVisibleFrozenWidth;
                        }

                        DiagnosticsDebug.Assert(_hScrollBar.Maximum >= 0, "Expected positive _hScrollBar.Maximum.");

                        // width of the scrollable viewing area
                        double viewPortSize = Math.Max(0, cellsWidth - totalVisibleFrozenWidth);
                        _hScrollBar.ViewportSize = viewPortSize;
                        _hScrollBar.LargeChange = viewPortSize;

                        // The ScrollBar should be in sync with HorizontalOffset at this point.  There's a resize case
                        // where the ScrollBar will coerce an old value here, but we don't want that.
                        SetHorizontalOffset(_horizontalOffset);

                        _hScrollBar.IsEnabled = true;
                    }
                    else
                    {
                        _hScrollBar.Maximum = 0;
                        _hScrollBar.ViewportSize = 0;
                        _hScrollBar.IsEnabled = false;
                    }

                    if (_hScrollBar.Visibility != Visibility.Visible)
                    {
                        // This will trigger a call to this method via Cells_SizeChanged for which no processing is needed.
                        _hScrollBar.Visibility = Visibility.Visible;
                        _ignoreNextScrollBarsLayout = true;
                    }
                }
                else
                {
                    _hScrollBar.Maximum = 0;
                    if (_hScrollBar.Visibility != Visibility.Collapsed)
                    {
                        // This will trigger a call to this method via Cells_SizeChanged for which no processing is needed.
                        _hScrollBar.Visibility = Visibility.Collapsed;
                        _ignoreNextScrollBarsLayout = true;
                    }
                }

                //DataGridAutomationPeer peer = DataGridAutomationPeer.FromElement(this) as DataGridAutomationPeer;
                //if (peer != null)
                //{
                //    peer.RaiseAutomationScrollEvents();
                //}
            }
        }

        private void UpdateVerticalScrollBar(bool needVertScrollBar, bool forceVertScrollBar, double totalVisibleHeight, double cellsHeight)
        {
            if (_vScrollBar != null)
            {
                if (needVertScrollBar || forceVertScrollBar)
                {
                    // ..........viewportSize
                    //         v---v
                    // |<|_____|###|>|
                    //   ^     ^
                    //   min   max

                    // we want to make the relative size of the thumb reflect the relative size of the viewing area
                    // viewportSize / (max + viewportSize) = cellsWidth / max
                    // -> viewportSize = max * cellsHeight / (totalVisibleHeight - cellsHeight)
                    // ->              = max * cellsHeight / (totalVisibleHeight - cellsHeight)
                    // ->              = max * cellsHeight / max
                    // ->              = cellsHeight

                    // always zero
                    _vScrollBar.Minimum = 0;
                    if (needVertScrollBar && !double.IsInfinity(cellsHeight))
                    {
                        // maximum travel distance -- not the total height
                        _vScrollBar.Maximum = totalVisibleHeight - cellsHeight;
                        DiagnosticsDebug.Assert(_vScrollBar.Maximum >= 0, "Expected positive _vScrollBar.Maximum.");

                        // total height of the display area
                        _vScrollBar.ViewportSize = cellsHeight;
                        _vScrollBar.LargeChange = cellsHeight;
                        _vScrollBar.IsEnabled = true;
                    }
                    else
                    {
                        _vScrollBar.Maximum = 0;
                        _vScrollBar.ViewportSize = 0;
                        _vScrollBar.IsEnabled = false;
                    }

                    if (_vScrollBar.Visibility != Visibility.Visible)
                    {
                        // This will trigger a call to this method via Cells_SizeChanged for which no processing is needed.
                        _vScrollBar.Visibility = Visibility.Visible;
                        _ignoreNextScrollBarsLayout = true;
                    }
                }
                else
                {
                    _vScrollBar.Maximum = 0;
                    if (_vScrollBar.Visibility != Visibility.Collapsed)
                    {
                        // This will trigger a call to this method via Cells_SizeChanged for which no processing is needed.
                        _vScrollBar.Visibility = Visibility.Collapsed;
                        _ignoreNextScrollBarsLayout = true;
                    }
                }

                //DataGridAutomationPeer peer = DataGridAutomationPeer.FromElement(this) as DataGridAutomationPeer;
                //if (peer != null)
                //{
                //    peer.RaiseAutomationScrollEvents();
                //}
            }
        }

        private void UpdateRowsPresenterManipulationMode(bool horizontalMode, bool verticalMode)
        {
            if (_cellsPresenter != null)
            {
                ManipulationModes manipulationMode = _cellsPresenter.ManipulationMode;

                if (horizontalMode)
                {
                    if (this.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled)
                    {
                        manipulationMode |= ManipulationModes.TranslateX | ManipulationModes.TranslateInertia;
                    }
                    else
                    {
                        manipulationMode &= ~(ManipulationModes.TranslateX | ManipulationModes.TranslateRailsX);
                    }
                }

                if (verticalMode)
                {
                    if (this.VerticalScrollBarVisibility != ScrollBarVisibility.Disabled)
                    {
                        manipulationMode |= ManipulationModes.TranslateY | ManipulationModes.TranslateInertia;
                    }
                    else
                    {
                        manipulationMode &= ~(ManipulationModes.TranslateY | ManipulationModes.TranslateRailsY);
                    }
                }

                if ((manipulationMode & (ManipulationModes.TranslateX | ManipulationModes.TranslateY)) == (ManipulationModes.TranslateX | ManipulationModes.TranslateY))
                {
                    manipulationMode |= ManipulationModes.TranslateRailsX | ManipulationModes.TranslateRailsY;
                }

                if ((manipulationMode & (ManipulationModes.TranslateX | ManipulationModes.TranslateRailsX | ManipulationModes.TranslateY | ManipulationModes.TranslateRailsY)) ==
                    ManipulationModes.None)
                {
                    manipulationMode &= ~ManipulationModes.TranslateInertia;
                }

                _cellsPresenter.ManipulationMode = manipulationMode;
            }
        }


        internal bool WaitForLostFocus(Action action)
        {
             
            return false;
        }

        internal bool UsesStarSizing
        {
            get
            {
                if (this.ColumnsInternal != null)
                {
                    return this.ColumnsInternal.VisibleStarColumnCount > 0 && !double.IsPositiveInfinity(this.ViewportWidth);
                }

                return false;
            }
        }

        internal void ResetColumnHeaderInteractionInfo()
        {
            DataGridColumnHeaderInteractionInfo interactionInfo = this.ColumnHeaderInteractionInfo;

            if (interactionInfo != null)
            {
                interactionInfo.CapturedPointer = null;
                interactionInfo.DragMode = DataGridColumnHeader.DragMode.None;
                interactionInfo.DragPointerId = 0;
                interactionInfo.DragColumn = null;
                interactionInfo.DragStart = null;
                interactionInfo.PressedPointerPositionHeaders = null;
                interactionInfo.LastPointerPositionHeaders = null;
            }

            if (this.ColumnHeaders != null)
            {
                this.ColumnHeaders.DragColumn = null;
                this.ColumnHeaders.DragIndicator = null;
                this.ColumnHeaders.DropLocationIndicator = null;
            }
        }

        /// <summary>
        /// Enters editing mode for the current cell and current row (if they're not already in editing mode).
        /// </summary>
        /// <returns>True if operation was successful. False otherwise.</returns>
        public bool BeginEdit()
        {
            return BeginEdit(null);
        }

        /// <summary>
        /// Enters editing mode for the current cell and current row (if they're not already in editing mode).
        /// </summary>
        /// <param name="editingEventArgs">Provides information about the user gesture that caused the call to BeginEdit. Can be null.</param>
        /// <returns>True if operation was successful. False otherwise.</returns>
        public bool BeginEdit(RoutedEventArgs editingEventArgs)
        {
            //if (this.CurrentColumnIndex == -1 || !GetRowSelection(this.CurrentSlot))
            //{
            //    return false;
            //}

            //DiagnosticsDebug.Assert(this.CurrentColumnIndex >= 0, "Expected positive CurrentColumnIndex.");
            //DiagnosticsDebug.Assert(this.CurrentColumnIndex < this.ColumnsItemsInternal.Count, "Expected CurrentColumnIndex smaller than ColumnsItemsInternal.Count.");
            //DiagnosticsDebug.Assert(this.CurrentSlot >= -1, "Expected CurrentSlot greater than or equal to -1.");
            //DiagnosticsDebug.Assert(this.CurrentSlot < this.SlotCount, "Expected CurrentSlot smaller than SlotCount.");
            //DiagnosticsDebug.Assert(this.EditingRow == null || this.EditingRow.Slot == this.CurrentSlot, "Expected null EditingRow or EditingRow.Slot equal to CurrentSlot.");

            //if (GetColumnEffectiveReadOnlyState(this.CurrentColumn))
            //{
            //    // Current column is read-only
            //    return false;
            //}

            return false;
        }

    
        /// <summary>
        /// Cancels editing mode and restores the original value.
        /// </summary>
        /// <returns>True if operation was successful. False otherwise.</returns>
        public bool CancelEdit()
        {
            return CancelEdit(true);
        }

        internal bool CancelEdit(bool raiseEvents)
        {
            if (!EndCellEdit(DataGridEditAction.Cancel, true, this.ContainsFocus /*keepFocus*/, raiseEvents))
            {
                return false;
            }
            return true;
        }


        /// <summary>
        /// Commits editing mode and pushes changes to the backend.
        /// </summary>
        /// <returns>True if operation was successful. False otherwise.</returns>
        public bool CommitEdit()
        {
            return CommitEdit(true);
        }

        /// <summary>
        /// Commits editing mode for the specified DataGridEditingUnit and pushes changes to the backend.
        /// </summary>
        /// <param name="editingUnit">Specifies whether to commit edit for a Cell or Row.</param>
        /// <param name="exitEditingMode">Editing mode is left if True.</param>
        /// <returns>True if operation was successful. False otherwise.</returns>
        public bool CommitEdit(bool exitEditingMode)
        {
            if (!EndCellEdit(DataGridEditAction.Commit, exitEditingMode, this.ContainsFocus /*keepFocus*/, true /*raiseEvents*/))
            {
                return false;
            }
            return true;
        }

        private bool EndCellEdit(DataGridEditAction editAction, bool exitEditingMode, bool keepFocus, bool raiseEvents)
        {
            return false;
        }

        private DataGridCellCoordinates CurrentCellCoordinates
        {
            get
            {
                return _currentCellCoordinates;
            }

            set
            {
                _currentCellCoordinates = value;
            }
        }

        internal int CurrentColumnIndex
        {
            get
            {
                return this.CurrentCellCoordinates.ColumnIndex;
            }

            private set
            {
                this.CurrentCellCoordinates.ColumnIndex = value;
            }
        }

        internal int CurrentSlot
        {
            get
            {
                return this.CurrentCellCoordinates.Slot;
            }

            private set
            {
                this.CurrentCellCoordinates.Slot = value;
            }
        }

        public DataGridColumn CurrentColumn
        {
            get
            {
                if (this.CurrentColumnIndex == -1)
                {
                    return null;
                }

                DiagnosticsDebug.Assert(this.CurrentColumnIndex < this.ColumnsItemsInternal.Count, "Expected CurrentColumnIndex smaller than ColumnsItemsInternal.Count.");
                return this.ColumnsItemsInternal[this.CurrentColumnIndex];
            }

            set
            {
                DataGridColumn dataGridColumn = value;
                if (dataGridColumn == null)
                {
                    throw DataGridError.DataGrid.ValueCannotBeSetToNull("value", "CurrentColumn");
                }

                if (this.CurrentColumn != dataGridColumn)
                {
                    if (dataGridColumn.OwningGrid != this)
                    {
                        // Provided column does not belong to this DataGrid
                        throw DataGridError.DataGrid.ColumnNotInThisDataGrid();
                    }

                    if (dataGridColumn.Visibility == Visibility.Collapsed)
                    {
                        // CurrentColumn cannot be set to an invisible column
                        throw DataGridError.DataGrid.ColumnCannotBeCollapsed();
                    }


                    bool beginEdit = _editingColumnIndex != -1;
                    if (!EndCellEdit(DataGridEditAction.Commit, true /*exitEditingMode*/, this.ContainsFocus /*keepFocus*/, true /*raiseEvents*/))
                    {
                        // Edited value couldn't be committed or aborted
                        return;
                    }

                    if (_noFocusedColumnChangeCount == 0)
                    {
                        this.ColumnHeaderHasFocus = false;
                    }

                    //this.UpdateSelectionAndCurrency(dataGridColumn.Index, this.CurrentSlot, DataGridSelectionAction.None, false /*scrollIntoView*/);
                   // DiagnosticsDebug.Assert(_successfullyUpdatedSelection, "Expected _successfullyUpdatedSelection is true.");
                    if (beginEdit &&
                        _editingColumnIndex == -1 &&
                        this.CurrentSlot != -1 &&
                        this.CurrentColumnIndex != -1 &&
                        this.CurrentColumnIndex == dataGridColumn.Index &&
                        dataGridColumn.OwningGrid == this &&
                        !GetColumnEffectiveReadOnlyState(dataGridColumn))
                    {
                        // Returning to editing mode since the grid was in that mode prior to the EndCellEdit call above.
                        BeginEdit(new RoutedEventArgs());
                    }
                }
            }
        }

        private void ClearRows(bool recycle)
        {
            // Need to clean up recycled rows even if the RowCount is 0
            //SetCurrentCellCore(-1, -1, false /*commitEdit*/, false /*endRowEdit*/);
            
            UnloadElements(recycle);
 
            this.NegVerticalOffset = 0;
            SetVerticalOffset(0);
            ComputeScrollBarsLayout();
        }

        private void RemoveDisplayedColumnHeaders()
        {
            if (_columnHeadersPresenter != null)
            {
                _columnHeadersPresenter.Children.Clear();
            }
        }
    }
}