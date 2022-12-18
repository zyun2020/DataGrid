using System.Numerics;
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
using Windows.System;
using ZyunUI.DataGridInternals;
using ZyunUI.Utilities;
using static ZyunUI.DataGridInternals.DataGridError;
using DiagnosticsDebug = System.Diagnostics.Debug;
using System.Xml.Linq;

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

        internal enum CellsSelectionMode
        {
            No,
            Columns,
            Rows,
            Range
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

        private const string DATAGRID_elementCellsOverlayCanvas = "CellsOverlayCanvas";
        private const string DATAGRID_elementCellsSelectionRange = "CellsSelectionRange";
        private const string DATAGRID_elementCurrentCellContainer = "CurrentCellContainer";
     
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

        private Queue<Action> _lostFocusActions;
        private bool _hasEditing = false;
        private FrameworkElement _editingElement = null;
        private object _uneditedValue; // Represents the original current cell value at the time it enters editing mode.
        private string _updateSourcePath;
        private RoutedEventArgs _editingEventArgs;
        private bool _executingLostFocusActions;

        // the number of pixels of DisplayData.FirstDisplayedScrollingRow which are not displayed
        private int _noCurrentCellChangeCount;
        private int _noFocusedColumnChangeCount;
        private int _noSelectionChangeCount;
        private bool _focusEditingControl;
        private FocusInputDeviceKind _focusInputDevice;
        private DependencyObject _focusedObject;

        private double _oldEdgedRowsHeightCalculated = 0.0;
        private bool _measured;

        private bool _scrollingByHeight;
        
        private bool _columnHeaderHasFocus;

        private UIElement _bottomRightCorner;
        private DataGridColumnHeadersPresenter _columnHeadersPresenter;
        private DataGridRowHeadersPresenter _rowHeadersPresenter;
        private ScrollBar _hScrollBar;
        private ScrollBar _vScrollBar;
        private DataGridCellsPresenter _cellsPresenter;
        private Panel _rootPanel;

        private Canvas _cellsOverlayCanvas;

        private Border _currentCellContainer;
        private DataGridCurrentCellAction _currentCellAction = DataGridCurrentCellAction.Edit;

        private Border _cellsSelectionRange;

        private FrameworkElement _frozenColumnScrollBarSpacer;
        private ContentControl _topLeftCornerHeader;
       
        private readonly List<DataGridRow> m_rows = new List<DataGridRow>();
        private Size? _datagridAvailableSize;

        private CellsSelectionMode _cellsSelectionMode = CellsSelectionMode.No;
        private GridCellRange _cellsSelection = null;

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
        /// Occurs before a cell or row enters editing mode.
        /// </summary>
        public event EventHandler<DataGridBeginningEditEventArgs> BeginningEdit;

        /// <summary>
        /// Occurs after cell editing has ended.
        /// </summary>
        public event EventHandler<DataGridCellEditEndedEventArgs> CellEditEnded;

        /// <summary>
        /// Occurs immediately before cell editing has ended.
        /// </summary>
        public event EventHandler<DataGridCellEditEndingEventArgs> CellEditEnding;

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

            _lostFocusActions = new Queue<Action>();

            _focusInputDevice = FocusInputDeviceKind.None;
            _proposedScrollBarsState = ScrollBarVisualState.NoIndicator;
            _proposedScrollBarsSeparatorState = ScrollBarsSeparatorVisualState.SeparatorCollapsed;

            this.LastHandledKeyDown = VirtualKey.None;
            HookDataGridEvents();
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see
        /// cref="M:System.Windows.FrameworkElement.ApplyTemplate" /> .
        /// </summary>
        protected override void OnApplyTemplate()
        {
            _hasNoIndicatorStateStoryboardCompletedHandler = false;
            _keepScrollBarsShowing = false;

            _cellsOverlayCanvas = GetTemplateChild(DATAGRID_elementCellsOverlayCanvas) as Canvas;
            _currentCellContainer = GetTemplateChild(DATAGRID_elementCurrentCellContainer) as Border;
            if(null != _currentCellContainer)
            {
                _currentCellContainer.Visibility = Visibility.Collapsed;
            }
         
            _cellsSelectionRange = GetTemplateChild(DATAGRID_elementCellsSelectionRange) as Border;
            if (null != _cellsSelectionRange)
            {
                _cellsSelectionRange.Visibility = Visibility.Collapsed;
            }

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

            if (_rowHeadersPresenter != null)
            {
                _rowHeadersPresenter.Children.Clear();
            }

            _rowHeadersPresenter = GetTemplateChild(DATAGRID_elementRowHeadersPresenterName) as DataGridRowHeadersPresenter;
            if (_rowHeadersPresenter != null)
            {
                _rowHeadersPresenter.OwningGrid = this;
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

            FrameworkElement root = GetTemplateChild(DATAGRID_elementRootName) as FrameworkElement;
            _rootPanel = root as Panel;
            IndicatorStateStoryboardCompleted(root);

            HideScrollBars(false /*useTransitions*/);

            UpdateDisabledVisual();
        }

        private VirtualKey LastHandledKeyDown
        {
            get;
            set;
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
        private void InvalidateCellsArrange()
        {
            if (_cellsPresenter != null)
            {
                _cellsPresenter.InvalidateArrange();
            }
        }

        private void InvalidateCellsMeasure()
        {
            if (_cellsPresenter != null)
            {
                _cellsPresenter.InvalidateMeasure();
            }
        }

        private void InvalidateColumnHeadersArrange()
        {
            if (_columnHeadersPresenter != null)
            {
                _columnHeadersPresenter.InvalidateArrange();
            }
        }

        private void InvalidateColumnHeadersMeasure()
        {
            if (_columnHeadersPresenter != null)
            {
                EnsureColumnHeadersVisibility();
                _columnHeadersPresenter.InvalidateMeasure();
            }
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
                    else if (this.RowCount > 0)
                    {
                        // RowHeaders AutoSize and we have at least 1 row so take the desired width
                        _topLeftCornerHeader.Width = this.ActualRowHeaderWidth;
                    }
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

        internal double CellsViewHeight
        {
            get;
            private set;
        }

        internal double CellsViewWidth
        {
            get;
            private set;
        }

        internal double AvailableSlotElementRoom
        {
            get;
            set;
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

                double widthNotVisible = Math.Max(0, this.ColumnsInternal.VisibleEdgedColumnsWidth - this.CellsViewWidth);
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
            return GridCellRef.ToRowName(j);
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

        private void AddElementForMeasure(FrameworkElement element)
        {
            if(_rootPanel != null)
            {
                _rootPanel.Children.Insert(0, element);
            }
            else if(_cellsPresenter != null)
            {
                _cellsPresenter.Children.Add(element);
            }
        }

        private void RemoveElementForMeasure(FrameworkElement element)
        {
            if (_rootPanel != null)
            {
                _rootPanel.Children.Remove(element);
            }
            else if (_cellsPresenter != null)
            {
                _cellsPresenter.Children.Remove(element);
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
                bool invalidateHeight = this._datagridAvailableSize.HasValue && availableSize.Height != this._datagridAvailableSize.Value.Height;
 
                _datagridAvailableSize = availableSize;
                if (invalidate || invalidateHeight)
                {
                    Refresh(invalidate, invalidateHeight);
                    if (Double.IsFinite(CellsViewWidth) && Double.IsFinite(CellsViewHeight)) 
                    {
                        RectangleGeometry rg = new RectangleGeometry();
                        rg.Rect = new Rect(0, 0, CellsViewWidth, CellsViewHeight);
                        _cellsOverlayCanvas.Clip = rg;
                    }
                }
            }

            return base.MeasureOverride(availableSize);
        }

        internal void Refresh(bool invalidate, bool invalidateHeight)
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

            CellsViewWidth = measureWidth;
            CellsViewHeight = availableSize.Height;
            if (!Double.IsNaN(CellsViewHeight))
            {
                CellsViewHeight -= ActualColumnHeaderHeight;
            }

            ComputeScrollBarsLayout();

            UpdateDisplayedColumns();
            UpdateDisplayedRows(0, CellsViewHeight);

            EnsureTopLeftCornerHeader();
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

            DataGridRowHeader rowHeader = new DataGridRowHeader();
            rowHeader.EnsureStyle(null);

            TextBlock textBlock = RowHeaderColumn.GenerateRowHeader(null);
            rowHeader.Content = textBlock;
           
            AddElementForMeasure(rowHeader);

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
                    if (RowHeaderColumn.Binding == null)
                    {
                        textBlock.Text = GridCellRef.ToRowName(i + 1);
                    }
                    else
                    {
                        rowHeader.DataContext = dataItem;
                    }

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
            RemoveElementForMeasure(rowHeader);
        }

        private double MeasureCells(DataGridColumn column, Size measureSize, bool isMeaseureCol)
        {
            var rows = Rows;
            DataGridRow row;
            object dataItem;
            AdvancedCollectionView collectionView = CollectionView;

            DataGridCell gridCell = column.CreateGridCell(null);
            AddElementForMeasure(gridCell);
            
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

            RemoveElementForMeasure(gridCell); 

            return width;
        }

        internal bool ScrollRowIntoView(int rowIndex, bool scrolledHorizontally)
        {
            DiagnosticsDebug.Assert(rowIndex>=0 && rowIndex < RowCount, "Expected rowIndex>=0 And rowIndex<RowCount .");

            if (scrolledHorizontally && rowIndex >= this.DisplayData.FirstDisplayedRow  && rowIndex <= this.DisplayData.LastDisplayedRow)
            {
                UpdateDisplayedRows(this.DisplayData.FirstDisplayedRow, this.CellsViewHeight);
            }

            if (rowIndex > this.DisplayData.FirstDisplayedRow && rowIndex < this.DisplayData.LastDisplayedRow)
            {
                // The row is already displayed in its entirety
                return true;
            }
            else if (this.DisplayData.FirstDisplayedRow == rowIndex && rowIndex != -1)
            {
                if (!DoubleUtil.IsZero(this.NegVerticalOffset))
                {
                    // First displayed row is partially scrolled of. Let's scroll it so that this.NegVerticalOffset becomes 0.
                    this.DisplayData.PendingVerticalScrollHeight = -this.NegVerticalOffset;
                    InvalidateCellsMeasure();
                }

                return true;
            }

            double deltaY = 0;
            int firstFullRow;
            if (rowIndex < this.DisplayData.FirstDisplayedRow)
            {
                // Scroll up to the new row so it becomes the first displayed row
                firstFullRow = this.DisplayData.FirstDisplayedRow - 1;
                if (DoubleUtil.GreaterThan(this.NegVerticalOffset, 0))
                {
                    deltaY = -this.NegVerticalOffset;
                }

                deltaY -= GetRowsActualHeight(rowIndex, firstFullRow);
                 
                this.NegVerticalOffset = 0;
                UpdateDisplayedRows(rowIndex, this.CellsViewHeight);
            }
            else if (rowIndex >= this.DisplayData.LastDisplayedRow)
            {
                // Scroll down to the new row so it's entirely displayed.  If the height of the row
                // is greater than the height of the DataGrid, then show the top of the row at the top
                // of the grid.
                firstFullRow = this.DisplayData.LastDisplayedRow;

                // Figure out how much of the last row is cut off.
                double rowHeight = GetRowActualHeight(this.DisplayData.LastDisplayedRow);
                double availableHeight = this.AvailableSlotElementRoom + rowHeight;
                if (DoubleUtil.AreClose(rowHeight, availableHeight))
                {
                    if (this.DisplayData.LastDisplayedRow == rowIndex)
                    {
                        // We're already at the very bottom so we don't need to scroll down further.
                        return true;
                    }
                    else
                    {
                        // We're already showing the entire last row so don't count it as part of the delta.
                        firstFullRow++;
                    }
                }
                else if (rowHeight > availableHeight)
                {
                    firstFullRow++;
                    deltaY += rowHeight - availableHeight;
                }

                // sum up the height of the rest of the full rows.
                if (rowIndex >= firstFullRow)
                {
                    deltaY += GetRowsActualHeight(firstFullRow, rowIndex);
                }

                if (DoubleUtil.GreaterThanOrClose(GetRowActualHeight(rowIndex), this.CellsViewHeight))
                {
                    // The entire row won't fit in the DataGrid so we start showing it from the top.
                    this.NegVerticalOffset = 0;
                    UpdateDisplayedRows(rowIndex, this.CellsViewHeight);
                }
                else
                {
                    UpdateDisplayedRowsFromBottom(rowIndex);
                }
            }

            VerticalOffset += deltaY;
            if (_verticalOffset < 0 || this.DisplayData.FirstDisplayedRow == 0)
            {
                // We scrolled too far because a row's height was larger than its approximation.
                VerticalOffset = this.NegVerticalOffset;
            }

            // TODO: in certain cases (eg, variable row height), this may not be true
            DiagnosticsDebug.Assert(DoubleUtil.LessThanOrClose(this.NegVerticalOffset, _verticalOffset), "Expected NegVerticalOffset is less than or close to _verticalOffset.");

            SetVerticalOffset(_verticalOffset);

            InvalidateCellsArrange();

            return true;
        }

        private void ScrollSlotsByHeight(double height)
        {
            DiagnosticsDebug.Assert(this.DisplayData.FirstDisplayedRow >= 0, "Expected positive DisplayData.FirstScrollingSlot.");
            DiagnosticsDebug.Assert(!DoubleUtil.IsZero(height), "DoubleUtil.IsZero(height) is false.");

            _scrollingByHeight = true;
            try
            {
                bool updateFromBottom = false;
                double deltaY = 0;
                int newFirstScrollingRow = this.DisplayData.FirstDisplayedRow;
                double newVerticalOffset = _verticalOffset + height;
                if (height > 0)
                {
                    // Scrolling Down
                    int lastVisibleRow = this.RowCount - 1;
                    if (_vScrollBar != null && DoubleUtil.AreClose(_vScrollBar.Maximum, newVerticalOffset))
                    {
                        // We've scrolled to the bottom of the ScrollBar, automatically place the user at the very bottom
                        // of the DataGrid.  If this produces very odd behavior, evaluate the coping strategy used by
                        // OnRowMeasure(Size).  For most data, this should be unnoticeable.
                        UpdateDisplayedRowsFromBottom(lastVisibleRow);
                        newFirstScrollingRow = this.DisplayData.FirstDisplayedRow;
                        updateFromBottom = true;
                    }
                    else
                    {
                        deltaY = GetRowActualHeight(newFirstScrollingRow) - this.NegVerticalOffset;
                        if (DoubleUtil.LessThan(height, deltaY))
                        {
                            // We've merely covered up more of the same row we're on
                            this.NegVerticalOffset += height;
                        }
                        else
                        {
                            // Figure out what row we've scrolled down to and update the value for this.NegVerticalOffset
                            this.NegVerticalOffset = 0;
                            while (DoubleUtil.LessThanOrClose(deltaY, height))
                            {
                                if (newFirstScrollingRow < lastVisibleRow)
                                {
                                    newFirstScrollingRow++; 
                                }
                                else
                                {
                                    // We're being told to scroll beyond the last row, ignore the extra
                                    this.NegVerticalOffset = 0;
                                    break;
                                }

                                double rowHeight = GetRowActualHeight(newFirstScrollingRow);
                                double remainingHeight = height - deltaY;
                                if (DoubleUtil.LessThanOrClose(rowHeight, remainingHeight))
                                {
                                    deltaY += rowHeight;
                                }
                                else
                                {
                                    this.NegVerticalOffset = remainingHeight;
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    // Scrolling Up
                    if (DoubleUtil.GreaterThanOrClose(0, newVerticalOffset))
                    {
                        // We've scrolled to the top of the ScrollBar, automatically place the user at the very top
                        // of the DataGrid.  If this produces very odd behavior, evaluate the RowHeight estimate.
                        // strategy. For most data, this should be unnoticeable.
                        this.NegVerticalOffset = 0;
                        newFirstScrollingRow = 0;
                    }
                    else
                    {
                        if (DoubleUtil.GreaterThanOrClose(height + this.NegVerticalOffset, 0))
                        {
                            // We've merely exposing more of the row we're on
                            this.NegVerticalOffset += height;
                        }
                        else
                        {
                            // Figure out what row we've scrolled up to and update the value for this.NegVerticalOffset
                            deltaY = -this.NegVerticalOffset;
                            this.NegVerticalOffset = 0;

                            int lastScrollingSlot = this.DisplayData.LastDisplayedRow;
                            while (DoubleUtil.GreaterThan(deltaY, height))
                            {
                                if (newFirstScrollingRow > 0)
                                {
                                    newFirstScrollingRow--;
                                }
                                else
                                {
                                    this.NegVerticalOffset = 0;
                                    break;
                                }

                                double rowHeight = GetRowActualHeight(newFirstScrollingRow);
                                double remainingHeight = height - deltaY;
                                if (DoubleUtil.LessThanOrClose(rowHeight + remainingHeight, 0))
                                {
                                    deltaY -= rowHeight;
                                }
                                else
                                {
                                    this.NegVerticalOffset = rowHeight + remainingHeight;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (!updateFromBottom)
                {
                    UpdateDisplayedRows(newFirstScrollingRow, this.CellsViewHeight);
                }

                DiagnosticsDebug.Assert(this.DisplayData.FirstDisplayedRow >= 0, "Expected positive DisplayData.FirstScrollingSlot.");
                DiagnosticsDebug.Assert(GetRowActualHeight(this.DisplayData.FirstDisplayedRow) > this.NegVerticalOffset, "Expected GetExactSlotElementHeight(DisplayData.FirstScrollingSlot) larger than this.NegVerticalOffset.");

                if (this.DisplayData.FirstDisplayedRow == 0)
                {
                    newVerticalOffset = this.NegVerticalOffset;
                }
                else if (DoubleUtil.GreaterThan(this.NegVerticalOffset, newVerticalOffset))
                {
                    // The scrolled-in row was larger than anticipated. Adjust the DataGrid so the ScrollBar thumb
                    // can stay in the same place
                    this.NegVerticalOffset = newVerticalOffset; 
                }
               
                DiagnosticsDebug.Assert(
                    _verticalOffset != 0 || this.NegVerticalOffset != 0 || this.DisplayData.FirstDisplayedRow <= 0,
                    "Expected _verticalOffset other than 0 or this.NegVerticalOffset other than 0 or this.DisplayData.FirstScrollingSlot smaller than or equal to 0.");

                SetVerticalOffset(newVerticalOffset);

                DiagnosticsDebug.Assert(DoubleUtil.GreaterThanOrClose(this.NegVerticalOffset, 0), "Expected NegVerticalOffset greater than or close to 0.");
                DiagnosticsDebug.Assert(DoubleUtil.GreaterThanOrClose(_verticalOffset, this.NegVerticalOffset), "Expected _verticalOffset greater than or close to NegVerticalOffset.");

                //DataGridAutomationPeer peer = DataGridAutomationPeer.FromElement(this) as DataGridAutomationPeer;
                //if (peer != null)
                //{
                //    peer.RaiseAutomationScrollEvents();
                //}
            }
            finally
            {
                _scrollingByHeight = false;
                //InvalidateCellsArrange();
            }
        }
        private void ComputeScrollBarsLayout()
        {
            double viewWidth = this.CellsViewWidth;
            double viewHeight = this.CellsViewHeight;

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

        internal bool UsesStarSizing
        {
            get
            {
                if (this.ColumnsInternal != null)
                {
                    return this.ColumnsInternal.VisibleStarColumnCount > 0 && !double.IsPositiveInfinity(this.CellsViewWidth);
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
            if (!_hasEditing || _editingElement == null || !CurrentCell.IsValid)
            {
                return false;
            }

            GridCellRef cellRef = new GridCellRef(CurrentCell);
            // We're ready to start ending, so raise the event
            FrameworkElement editingElement = _editingElement;
            if (raiseEvents)
            {
                DataGridCellEditEndingEventArgs e = new DataGridCellEditEndingEventArgs(this.CurrentColumn, this.CurrentRow, editingElement, editAction);
                OnCellEditEnding(e);
                if (e.Cancel)
                {
                    // CellEditEnding has been canceled
                    return false;
                }

                // Ensure that the current cell wasn't changed in the user's CellEditEnding handler
                if (!CurrentCell.IsValid || !cellRef.Equals(CurrentCell))
                {
                    return false;
                }

            }

            //_bindingValidationResults.Clear();

            // If we're canceling, let the editing column repopulate its old value if it wants
            if (editAction == DataGridEditAction.Cancel)
            {
                this.CurrentColumn.CancelCellEditInternal(editingElement, _uneditedValue);
                CurrentColumn.RemoveEditingElement();

                // Ensure that the current cell wasn't changed in the user column's CancelCellEdit
                if (!CurrentCell.IsValid || !cellRef.Equals(CurrentCell))
                {
                    return false;
                }

           
                // Re-validate
                //this.ValidateEditingRow(true /*scrollIntoView*/, false /*wireEvents*/);
            }

            // If we're committing, explicitly update the source but watch out for any validation errors
            if (editAction == DataGridEditAction.Commit)
            {
                Object dataItem = GetRowData(CurrentRowIndex);
                foreach (BindingInfo bindingData in this.CurrentColumn.GetInputBindings(editingElement, dataItem))
                {
                    DiagnosticsDebug.Assert(bindingData.BindingExpression.ParentBinding != null, "Expected non-null bindingData.BindingExpression.ParentBinding.");
                    _updateSourcePath = bindingData.BindingExpression.ParentBinding.Path != null ? bindingData.BindingExpression.ParentBinding.Path.Path : null;
#if FEATURE_VALIDATION
                    bindingData.Element.BindingValidationError += new EventHandler<ValidationErrorEventArgs>(EditingElement_BindingValidationError);
#endif
                    try
                    {
                        bindingData.BindingExpression.UpdateSource();
                    }
                    finally
                    {
#if FEATURE_VALIDATION
                    bindingData.Element.BindingValidationError -= new EventHandler<ValidationErrorEventArgs>(EditingElement_BindingValidationError);
#endif
                    }
                }

                // Re-validate
                //this.ValidateEditingRow(true /*scrollIntoView*/, false /*wireEvents*/);

                //if (_bindingValidationResults.Count > 0)
                //{
                //    ScrollSlotIntoView(this.CurrentColumnIndex, this.CurrentSlot, false /*forCurrentCellChange*/, true /*forceHorizontalScroll*/);
                //    return false;
                //}

                CurrentColumn.RemoveEditingElement();
            }

            if (exitEditingMode)
            {
                DataGridCell editingCell = CurrentDataGridCell;
                if (editingCell != null)
                {
                    PopulateCellContent(!exitEditingMode /*isCellEdited*/, this.CurrentColumn, this.GetRowData(CurrentRowIndex), editingCell);
                    editingCell.ApplyCellState(true /*animate*/);
                }

                // TODO: Figure out if we should restore a cached this.IsTabStop.
                this.IsTabStop = true;
                if (keepFocus && editingElement.ContainsFocusedElement(this))
                {
                    this.Focus(FocusState.Programmatic);
                }
            }

            // We're done, so raise the CellEditEnded event
            if (raiseEvents)
            {
                OnCellEditEnded(new DataGridCellEditEndedEventArgs(this.CurrentColumn, this.CurrentRow, editAction));
            }

            _hasEditing = false;
            _editingElement = null;

            // There's a chance that somebody reopened this cell for edit within the CellEditEnded handler,
            // so we should return false if we were supposed to exit editing mode, but we didn't
            return !(exitEditingMode && cellRef.Equals(CurrentCell));
        }

        internal void BeginCellEdit(RoutedEventArgs editingEventArgs, bool raiseEvents)
        {
            DataGridCell dataGridCell = CurrentDataGridCell;
            if(dataGridCell == null) return;

            if (raiseEvents)
            {
                DataGridBeginningEditEventArgs e = new DataGridBeginningEditEventArgs(this.CurrentColumn, this.CurrentRow, editingEventArgs);
                OnBeginningEdit(e);
            }

            DataGridBoundColumn dataGridColumn = this.CurrentColumn as DataGridBoundColumn;
            Object dataItem = GetRowData(CurrentRowIndex);

            _editingElement = PopulateCellContent(true, dataGridColumn, dataItem, dataGridCell);
            if(_editingElement != null)
                _hasEditing = true;
        }

        private FrameworkElement PopulateCellContent(
           bool isCellEdited,
           DataGridColumn dataGridColumn,
           Object dataContext,
           DataGridCell dataGridCell)
        {
            DiagnosticsDebug.Assert(dataGridColumn != null, "Expected non-null dataGridColumn.");
            DiagnosticsDebug.Assert(dataGridCell != null, "Expected non-null dataGridCell.");

            FrameworkElement element = null;
            DataGridBoundColumn dataGridBoundColumn = dataGridColumn as DataGridBoundColumn;
            if (isCellEdited)
            {
                // Generate EditingElement and apply column style if available
                element = dataGridColumn.GenerateEditingElementInternal(dataGridCell, dataContext);
                if (element != null)
                {
                    if (dataGridBoundColumn != null && dataGridBoundColumn.EditingElementStyle != null)
                    {
                        element.SetStyleWithType(dataGridBoundColumn.EditingElementStyle);
                    }

                    // Subscribe to the new element's events
                    element.Loaded += new RoutedEventHandler(EditingElement_Loaded);
                }
            }
            else
            {
                // Generate Element and apply column style if available
                element = dataGridColumn.GenerateElementInternal(dataGridCell, dataContext);
                if (element != null)
                {
                    if (dataGridBoundColumn != null && dataGridBoundColumn.ElementStyle != null)
                    {
                        element.SetStyleWithType(dataGridBoundColumn.ElementStyle);
                    }
                }
            }
            dataGridCell.Content = element;
            return element;
        }

        private GridCellRef _currentCell = new GridCellRef(-1, -1);

        public GridCellRef CurrentCell
        {
            get { return _currentCell; }
            set
            {
                bool bUpdateCurrent = IsShowSelectionRange;
                ClearSelection();
                if (!_currentCell.Equals(value))
                {
                    GridCellRef oldCell = _currentCell;
                    EndCellEdit(DataGridEditAction.Commit, true, true, true);

                    GridCellRef cell = value;
                    if (cell.Column < 0 || cell.Column >= ColumnCount ||
                        cell.Row < 0 || cell.Row > RowCount)
                    {
                        _currentCell = new GridCellRef();
                    }
                    else
                    {
                        _currentCell = cell;
                    }
                    bUpdateCurrent = true;
                    BeginCellEdit(null, true);
                }

                if (bUpdateCurrent)
                {
                    UpdateCurrentCellState();
                }
            }
        }

        internal int CurrentColumnIndex => CurrentCell.Column;
        internal int CurrentRowIndex => CurrentCell.Row;

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
        }

        public DataGridRow CurrentRow
        {
            get
            {
                if (this.CurrentRowIndex == -1)
                {
                    return null;
                }

                DiagnosticsDebug.Assert(this.CurrentRowIndex < this.Rows.Count, "Expected CurrentColumnIndex smaller than ColumnsItemsInternal.Count.");
                return this.Rows[this.CurrentRowIndex];
            }
        }


        internal DataGridCell CurrentDataGridCell
        {
            get
            {
                if (!CurrentCell.IsValid) return null;
                return DisplayData.GetDataGridCell(CurrentCell);
            }
        }

        private void UpdateCurrentCellState()
        {
            if(_currentCellAction == DataGridCurrentCellAction.None)
            {
                _currentCellContainer.Visibility = Visibility.Collapsed;
                return;
            }

            if (CurrentCell.IsValid)
            {
                bool hideLeft = false;
                Rect rc = GetGridCellRect(CurrentCell, ref hideLeft);
                if (rc.Width > 0 && rc.Height > 0)
                {
                    if (IsShowSelectionRange)
                        _currentCellContainer.BorderThickness = new Thickness(1);
                    else
                        _currentCellContainer.BorderThickness = new Thickness(2);

                    _currentCellContainer.Width = rc.Width + 1;
                    _currentCellContainer.Height = rc.Height + 1;
                    Canvas.SetLeft(_currentCellContainer, rc.X - 1);
                    Canvas.SetTop(_currentCellContainer, rc.Y - 1);

                    if(_hasEditing && _currentCellContainer.Visibility == Visibility.Collapsed)
                    {
                        BeginCellEdit(null, false);
                    }
                    _currentCellContainer.Visibility = Visibility.Visible;
                }
                else
                {
                    _currentCellContainer.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                _currentCellContainer.Visibility = Visibility.Collapsed;
            }
        }

        internal void UpdateSelectedCells()
        { 
            if (IsShowSelectionRange)
            {
                bool hideLeft = false;
                Rect rc = GetSelectionRect(_cellsSelection, _cellsSelectionMode, ref hideLeft);
                if (rc.Width > 0 && rc.Height > 0)
                {
                    Thickness thickness = new Thickness(2);
                    if (hideLeft) thickness.Left = 0;
                    _cellsSelectionRange.BorderThickness = thickness;
                   
                    _cellsSelectionRange.Width = rc.Width + 1;
                    _cellsSelectionRange.Height = rc.Height + 1;
                    Canvas.SetLeft(_cellsSelectionRange, rc.X - 1);
                    Canvas.SetTop(_cellsSelectionRange, rc.Y - 1);

                    _cellsSelectionRange.Visibility = Visibility.Visible;
                }
                else
                {
                    _cellsSelectionRange.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                _cellsSelectionRange.Visibility = Visibility.Collapsed;
            }

            UpdateCurrentCellState();
        }

        private void EditingElement_Loaded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element != null)
            {
                element.Loaded -= new RoutedEventHandler(EditingElement_Loaded);
            }

            PreparingCellForEditPrivate(element);
        }

        private void EditingElement_LostFocus(object sender, RoutedEventArgs e)
        {
            FrameworkElement editingElement = sender as FrameworkElement;
            if (editingElement != null)
            {
                editingElement.LostFocus -= new RoutedEventHandler(EditingElement_LostFocus);
                if (_hasEditing)
                {
                    this.FocusEditingCell(true);
                }

                DiagnosticsDebug.Assert(_lostFocusActions != null, "Expected non-null _lostFocusActions.");
                try
                {
                    _executingLostFocusActions = true;
                    while (_lostFocusActions.Count > 0)
                    {
                        _lostFocusActions.Dequeue()();
                    }
                }
                finally
                {
                    _executingLostFocusActions = false;
                }
            }
        }

        internal bool WaitForLostFocus(Action action)
        {
            DataGridCell editingCell = CurrentDataGridCell;
            if (_hasEditing && editingCell != null && !_executingLostFocusActions)
            {
                FrameworkElement editingElement = editingCell.Content as FrameworkElement;
                if (editingElement != null && editingElement.ContainsChild(_focusedObject))
                {
                    DiagnosticsDebug.Assert(_lostFocusActions != null, "Expected non-null _lostFocusActions.");
                    _lostFocusActions.Enqueue(action);
                    editingElement.LostFocus += new RoutedEventHandler(EditingElement_LostFocus);
                    this.IsTabStop = true;
                    this.Focus(FocusState.Programmatic);
                    return true;
                }
            }

            return false;
        }

        private void PreparingCellForEditPrivate(FrameworkElement editingElement)
        {
            if (!_hasEditing)
            {
                // The current cell has changed since the call to BeginCellEdit, so the fact
                // that this element has loaded is no longer relevant
                return;
            }
 
            FocusEditingCell(this.ContainsFocus || _focusEditingControl /*setFocus*/);

            // Prepare the cell for editing and raise the PreparingCellForEdit event for all columns
            DataGridColumn dataGridColumn = this.CurrentColumn;
            _uneditedValue = dataGridColumn.PrepareCellForEditInternal(editingElement, _editingEventArgs);
            //OnPreparingCellForEdit(new DataGridPreparingCellForEditEventArgs(dataGridColumn, this.EditingRow, _editingEventArgs, editingElement));
        }

        private bool FocusEditingCell(bool setFocus)
        {
            DataGridCell editingCell = CurrentDataGridCell;
            if (editingCell == null) return false;

            DiagnosticsDebug.Assert(this.CurrentColumnIndex >= 0, "Expected positive CurrentColumnIndex.");
            DiagnosticsDebug.Assert(this.CurrentColumnIndex < this.ColumnsItemsInternal.Count, "Expected CurrentColumnIndex smaller than ColumnsItemsInternal.Count.");
          
            // TODO: Figure out if we should cache this.IsTabStop in order to restore
            //       it later instead of setting it back to true unconditionally.
            this.IsTabStop = false;
            _focusEditingControl = false;

            bool success = false;
            DataGridCell dataGridCell = editingCell;
            if (setFocus)
            {
                if (dataGridCell.ContainsFocusedElement(this))
                {
                    success = true;
                }
                else
                {
                    success = dataGridCell.Focus(FocusState.Programmatic);
                }

                _focusEditingControl = !success;
            }

            return success;
        }


        private bool IsShowSelectionRange 
        {
            get
            {
                if (_cellsSelectionMode == CellsSelectionMode.No || _cellsSelection == null ||
                    (_cellsSelectionMode == CellsSelectionMode.Range && _cellsSelection.Columns <= 1 && _cellsSelection.Rows <= 1))
                {
                    return false;
                }
                return true;
            }
        }

        public void ClearSelection()
        {
            if(_cellsPresenter != null && _cellsSelection != null &&
                _cellsSelectionMode != CellsSelectionMode.No)
            {
                _cellsSelection = null;
                _cellsSelectionMode = CellsSelectionMode.No;

                _cellsPresenter.ApplyCellState();
            }

            if (_cellsSelectionRange != null)
            {
                _cellsSelectionRange.Visibility = Visibility.Collapsed;
            }
        }

        public void SelectRange(GridCellRef cellRef)
        {
            SelectRange(cellRef, CurrentCell);
        }

        public void SelectRange(GridCellRef cell1, GridCellRef cell2)
        {
            if (!cell1.IsValid || !cell2.IsValid) return;

            _cellsSelection = new GridCellRange(cell1, cell2);
            _cellsSelectionMode = CellsSelectionMode.Range;

            if (_cellsPresenter != null)
            {
                _cellsPresenter.ApplyCellState();
            }

            UpdateSelectedCells();
        }

        public bool CellIsSelected(GridCellRef cellRef)
        {
            if (CurrentCell.IsValid && CurrentCell.Equals(cellRef)) return false;
            if(_cellsSelectionMode == CellsSelectionMode.Range)
            {
                return _cellsSelection.IsContained(cellRef);
            }
            else if(_cellsSelectionMode == CellsSelectionMode.Columns)
            {
                if(cellRef.Column >= _cellsSelection.LeftColumn && cellRef.Column <= _cellsSelection.RightColumn)
                {
                    return true;
                }
            }
            else if (_cellsSelectionMode == CellsSelectionMode.Rows)
            {
                if (cellRef.Row >= _cellsSelection.TopRow && cellRef.Row <= _cellsSelection.BottomRow)
                {
                    return true;
                }
            }
            return false;
        }

        internal bool CellIsSelectedForState(GridCellRef cellRef)
        {
            if (CurrentCell.IsValid && CurrentCell.Equals(cellRef)) return false;
            return CellIsSelected(cellRef);
        }
 
        internal void UpdateCellsState()
        {
            if (_cellsPresenter != null)
            {
                _cellsPresenter.ApplyCellState();
            }

            UpdateSelectedCells();
        }

        private void ClearRows()
        {
            ResetCurrentCellCore();

            if (_cellsPresenter != null)
            {
                _cellsPresenter.Children.Clear();
            }
            DisplayData.ClearElements(false);

            this.NegVerticalOffset = 0;
            SetVerticalOffset(0);
            ComputeScrollBarsLayout();
        }

        private bool ResetCurrentCellCore()
        {
            CurrentCell = new GridCellRef(-1, -1);
            return !CurrentCell.IsValid;
        }

        private void EnsureHorizontalLayout()
        {
            Refresh(true, false);
        }
    }
}