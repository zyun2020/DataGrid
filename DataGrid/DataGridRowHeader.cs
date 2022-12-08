using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using ZyunUI.DataGridInternals;
using ZyunUI.Utilities;

namespace ZyunUI
{
    /// <summary>
    /// Represents an individual <see cref="DataGrid"/> row header.
    /// </summary>
    [TemplatePart(Name = DATAGRIDROWHEADER_elementRootName, Type = typeof(FrameworkElement))]

    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateNormal, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateNormalCurrentRow, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateNormalEditingRow, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateNormalEditingRowFocused, GroupName = VisualStates.GroupCommon)]

    [TemplateVisualState(Name = DATAGRIDROWHEADER_statePointerOver, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_statePointerOverCurrentRow, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_statePointerOverEditingRow, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_statePointerOverEditingRowFocused, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_statePointerOverSelected, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_statePointerOverSelectedFocused, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_statePointerOverSelectedCurrentRow, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_statePointerOverSelectedCurrentRowFocused, GroupName = VisualStates.GroupCommon)]

    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateSelected, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateSelectedCurrentRow, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateSelectedCurrentRowFocused, GroupName = VisualStates.GroupCommon)]
    [TemplateVisualState(Name = DATAGRIDROWHEADER_stateSelectedFocused, GroupName = VisualStates.GroupCommon)]

    [TemplateVisualState(Name = VisualStates.StateRowInvalid, GroupName = VisualStates.GroupValidation)]
    [TemplateVisualState(Name = VisualStates.StateRowValid, GroupName = VisualStates.GroupValidation)]
    public partial class DataGridRowHeader : ContentControl
    {
        private const string DATAGRIDROWHEADER_elementRootName = "RowHeaderRoot";
        private const double DATAGRIDROWHEADER_separatorThickness = 1;

        private const string DATAGRIDROWHEADER_statePointerOver = "PointerOver";
        private const string DATAGRIDROWHEADER_statePointerOverCurrentRow = "PointerOverCurrentRow";
        private const string DATAGRIDROWHEADER_statePointerOverEditingRow = "PointerOverUnfocusedEditingRow";
        private const string DATAGRIDROWHEADER_statePointerOverEditingRowFocused = "PointerOverEditingRow";
        private const string DATAGRIDROWHEADER_statePointerOverSelected = "PointerOverUnfocusedSelected";
        private const string DATAGRIDROWHEADER_statePointerOverSelectedCurrentRow = "PointerOverUnfocusedCurrentRowSelected";
        private const string DATAGRIDROWHEADER_statePointerOverSelectedCurrentRowFocused = "PointerOverCurrentRowSelected";
        private const string DATAGRIDROWHEADER_statePointerOverSelectedFocused = "PointerOverSelected";
        private const string DATAGRIDROWHEADER_stateNormal = "Normal";
        private const string DATAGRIDROWHEADER_stateNormalCurrentRow = "NormalCurrentRow";
        private const string DATAGRIDROWHEADER_stateNormalEditingRow = "UnfocusedEditingRow";
        private const string DATAGRIDROWHEADER_stateNormalEditingRowFocused = "NormalEditingRow";
        private const string DATAGRIDROWHEADER_stateSelected = "UnfocusedSelected";
        private const string DATAGRIDROWHEADER_stateSelectedCurrentRow = "UnfocusedCurrentRowSelected";
        private const string DATAGRIDROWHEADER_stateSelectedCurrentRowFocused = "NormalCurrentRowSelected";
        private const string DATAGRIDROWHEADER_stateSelectedFocused = "NormalSelected";

        private const byte DATAGRIDROWHEADER_statePointerOverCode = 0;
        private const byte DATAGRIDROWHEADER_statePointerOverCurrentRowCode = 1;
        private const byte DATAGRIDROWHEADER_statePointerOverEditingRowCode = 2;
        private const byte DATAGRIDROWHEADER_statePointerOverEditingRowFocusedCode = 3;
        private const byte DATAGRIDROWHEADER_statePointerOverSelectedCode = 4;
        private const byte DATAGRIDROWHEADER_statePointerOverSelectedCurrentRowCode = 5;
        private const byte DATAGRIDROWHEADER_statePointerOverSelectedCurrentRowFocusedCode = 6;
        private const byte DATAGRIDROWHEADER_statePointerOverSelectedFocusedCode = 7;
        private const byte DATAGRIDROWHEADER_stateNormalCode = 8;
        private const byte DATAGRIDROWHEADER_stateNormalCurrentRowCode = 9;
        private const byte DATAGRIDROWHEADER_stateNormalEditingRowCode = 10;
        private const byte DATAGRIDROWHEADER_stateNormalEditingRowFocusedCode = 11;
        private const byte DATAGRIDROWHEADER_stateSelectedCode = 12;
        private const byte DATAGRIDROWHEADER_stateSelectedCurrentRowCode = 13;
        private const byte DATAGRIDROWHEADER_stateSelectedCurrentRowFocusedCode = 14;
        private const byte DATAGRIDROWHEADER_stateSelectedFocusedCode = 15;
        private const byte DATAGRIDROWHEADER_stateNullCode = 255;

        private static byte[] _fallbackStateMapping = new byte[]
        {
            DATAGRIDROWHEADER_stateNormalCode,
            DATAGRIDROWHEADER_stateNormalCurrentRowCode,
            DATAGRIDROWHEADER_statePointerOverEditingRowFocusedCode,
            DATAGRIDROWHEADER_stateNormalEditingRowFocusedCode,
            DATAGRIDROWHEADER_statePointerOverSelectedFocusedCode,
            DATAGRIDROWHEADER_statePointerOverSelectedCurrentRowFocusedCode,
            DATAGRIDROWHEADER_stateSelectedFocusedCode,
            DATAGRIDROWHEADER_stateSelectedFocusedCode,
            DATAGRIDROWHEADER_stateNullCode,
            DATAGRIDROWHEADER_stateNormalCode,
            DATAGRIDROWHEADER_stateNormalEditingRowFocusedCode,
            DATAGRIDROWHEADER_stateSelectedCurrentRowFocusedCode,
            DATAGRIDROWHEADER_stateSelectedFocusedCode,
            DATAGRIDROWHEADER_stateSelectedCurrentRowFocusedCode,
            DATAGRIDROWHEADER_stateNormalCurrentRowCode,
            DATAGRIDROWHEADER_stateNormalCode,
        };

        private static byte[] _idealStateMapping = new byte[]
        {
            DATAGRIDROWHEADER_stateNormalCode,
            DATAGRIDROWHEADER_stateNormalCode,
            DATAGRIDROWHEADER_statePointerOverCode,
            DATAGRIDROWHEADER_statePointerOverCode,
            DATAGRIDROWHEADER_stateNullCode,
            DATAGRIDROWHEADER_stateNullCode,
            DATAGRIDROWHEADER_stateNullCode,
            DATAGRIDROWHEADER_stateNullCode,
            DATAGRIDROWHEADER_stateSelectedCode,
            DATAGRIDROWHEADER_stateSelectedFocusedCode,
            DATAGRIDROWHEADER_statePointerOverSelectedCode,
            DATAGRIDROWHEADER_statePointerOverSelectedFocusedCode,
            DATAGRIDROWHEADER_stateNormalEditingRowCode,
            DATAGRIDROWHEADER_stateNormalEditingRowFocusedCode,
            DATAGRIDROWHEADER_statePointerOverEditingRowCode,
            DATAGRIDROWHEADER_statePointerOverEditingRowFocusedCode,
            DATAGRIDROWHEADER_stateNormalCurrentRowCode,
            DATAGRIDROWHEADER_stateNormalCurrentRowCode,
            DATAGRIDROWHEADER_statePointerOverCurrentRowCode,
            DATAGRIDROWHEADER_statePointerOverCurrentRowCode,
            DATAGRIDROWHEADER_stateNullCode,
            DATAGRIDROWHEADER_stateNullCode,
            DATAGRIDROWHEADER_stateNullCode,
            DATAGRIDROWHEADER_stateNullCode,
            DATAGRIDROWHEADER_stateSelectedCurrentRowCode,
            DATAGRIDROWHEADER_stateSelectedCurrentRowFocusedCode,
            DATAGRIDROWHEADER_statePointerOverSelectedCurrentRowCode,
            DATAGRIDROWHEADER_statePointerOverSelectedCurrentRowFocusedCode,
            DATAGRIDROWHEADER_stateNormalEditingRowCode,
            DATAGRIDROWHEADER_stateNormalEditingRowFocusedCode,
            DATAGRIDROWHEADER_statePointerOverEditingRowCode,
            DATAGRIDROWHEADER_statePointerOverEditingRowFocusedCode
        };

        private static string[] _stateNames = new string[]
        {
            DATAGRIDROWHEADER_statePointerOver,
            DATAGRIDROWHEADER_statePointerOverCurrentRow,
            DATAGRIDROWHEADER_statePointerOverEditingRow,
            DATAGRIDROWHEADER_statePointerOverEditingRowFocused,
            DATAGRIDROWHEADER_statePointerOverSelected,
            DATAGRIDROWHEADER_statePointerOverSelectedCurrentRow,
            DATAGRIDROWHEADER_statePointerOverSelectedCurrentRowFocused,
            DATAGRIDROWHEADER_statePointerOverSelectedFocused,
            DATAGRIDROWHEADER_stateNormal,
            DATAGRIDROWHEADER_stateNormalCurrentRow,
            DATAGRIDROWHEADER_stateNormalEditingRow,
            DATAGRIDROWHEADER_stateNormalEditingRowFocused,
            DATAGRIDROWHEADER_stateSelected,
            DATAGRIDROWHEADER_stateSelectedCurrentRow,
            DATAGRIDROWHEADER_stateSelectedCurrentRowFocused,
            DATAGRIDROWHEADER_stateSelectedFocused
        };

        private FrameworkElement _rootElement;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataGridRowHeader"/> class.
        /// </summary>
        public DataGridRowHeader()
        {
            this.IsTapEnabled = true;

            this.AddHandler(UIElement.TappedEvent, new TappedEventHandler(DataGridRowHeader_Tapped), true /*handledEventsToo*/);

            DefaultStyleKey = typeof(DataGridRowHeader);
        }

        /// <summary>
        /// Gets or sets the <see cref="T:System.Windows.Media.Brush"/> used to paint the row header separator lines.
        /// </summary>
        public Brush SeparatorBrush
        {
            get { return GetValue(SeparatorBrushProperty) as Brush; }
            set { SetValue(SeparatorBrushProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="CommunityToolkit.WinUI.UI.Controls.Primitives.DataGridRowHeader.SeparatorBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SeparatorBrushProperty =
            DependencyProperty.Register(
                "SeparatorBrush",
                typeof(Brush),
                typeof(DataGridRowHeader),
                null);

        /// <summary>
        /// Gets or sets a value indicating whether the row header separator lines are visible.
        /// </summary>
        public Visibility SeparatorVisibility
        {
            get { return (Visibility)GetValue(SeparatorVisibilityProperty); }
            set { SetValue(SeparatorVisibilityProperty, value); }
        }

        /// <summary>
        /// Identifies the <see cref="CommunityToolkit.WinUI.UI.Controls.Primitives.DataGridRowHeader.SeparatorVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SeparatorVisibilityProperty =
            DependencyProperty.Register(
                "SeparatorVisibility",
                typeof(Visibility),
                typeof(DataGridRowHeader),
                new PropertyMetadata(Visibility.Visible));


        internal DataGrid OwningGrid
        {
            get;
            set;
        }

        internal DataGridColumn OwningColumn
        {
            get
            {
                return OwningGrid == null ? null : OwningGrid.RowHeaderColumn;
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

        internal DataGridRowVisuals OwningRow
        {
            get;
            set;
        }

        /// <summary>
        /// Builds the visual tree for the row header when a new template is applied.
        /// </summary>
        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _rootElement = GetTemplateChild(DATAGRIDROWHEADER_elementRootName) as FrameworkElement;
            if (_rootElement != null)
            {
                ApplyOwnerState(false /*animate*/);
            }
        }

        /// <summary>
        /// Creates AutomationPeer (<see cref="UIElement.OnCreateAutomationPeer"/>)
        /// </summary>
        /// <returns>An automation peer for this <see cref="T:System.Windows.Controls.Primitives.DataGridRowHeader"/>.</returns>
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return null; // new DataGridRowHeaderAutomationPeer(this);
        }

        internal void ApplyOwnerState(bool animate)
        {
            
        }

        /// <summary>
        /// Ensures that the correct Style is applied to this object.
        /// </summary>
        /// <param name="previousStyle">Caller's previous associated Style</param>         
        internal void EnsureStyle(Style previousStyle)
        {
            if (this.Style != null &&
                //this.OwningRow != null &&
                //this.Style != this.OwningRow.HeaderStyle &&
                this.OwningColumn != null &&
                this.Style != this.OwningColumn.CellStyle &&
                this.OwningGrid != null &&
                this.Style != this.OwningGrid.RowHeaderStyle &&
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
                style = this.OwningGrid.RowHeaderStyle;
            }

            this.SetStyleWithType(style);
        }

        private void DataGridRowHeader_Tapped(object sender, TappedRoutedEventArgs e)
        {
           
        }
    }
}
