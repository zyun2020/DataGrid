using Microsoft.UI.Xaml;
using System;

namespace ZyunUI
{
    /// <summary>
    /// Defines a row in a <see cref="DataGrid" />.
    /// </summary>
    public class DataGridRow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RowDefinition"/> class.
        /// </summary>
        public DataGridRow()
        { 
        }

        public DataGridRow(String header)
        {
            this.Header = header;    
        }

        public double ActualHeight { get; internal set; } = double.NaN;

        /// <summary>
        /// Gets or sets the row height.
        /// </summary>
        /// <value>The height.</value>
        public double Height { get; set; } = double.NaN;

        public object Header { get; set; }
    }
}