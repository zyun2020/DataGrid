using Microsoft.UI.Xaml;
using System;

namespace ZyunUI
{
    /// <summary>
    /// Defines a row in a <see cref="DataGrid" />.
    /// </summary>
    public class DataGridRow
    {
        public DataGridRow(int index, object header = null)
        {
            this.Index = index;
            this.Header = header;
        }
     
        public int Index { get; private set; }
        
        public double ActualHeight { get; internal set; } = double.NaN;

        /// <summary>
        /// Gets or sets the row height.
        /// </summary>
        /// <value>The height.</value>
        public double Height { get; set; } = double.NaN;

        public object Header { get; set; }
    }
}