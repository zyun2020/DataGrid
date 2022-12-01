using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DataGridDemo
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

           
            var windowType = typeof(Window);
            this.Examples.AddRange(
                typeof(MainWindow).Assembly.GetTypes()
                .Where(t => t.Name.EndsWith("Example") && windowType.IsAssignableFrom(t))
                .Select(t => new ExampleWindow(t))
                .OrderBy(e => e.Title));
        }

        public List<ExampleWindow> Examples { get; } = new List<ExampleWindow>();

        public class ExampleWindow
        {
            public ExampleWindow(Type type)
            {
                this.Type = type;
                var instance = this.CreateInstance();
                this.Title = instance.Title ?? type.Name;
                instance.Close();
            }

            public string Title { get; }

            public Type Type { get; }

            public Window CreateInstance() => (Window)Activator.CreateInstance(this.Type);

            public void Show()
            {
                this.CreateInstance().Activate();
            }

            public override string ToString()
            {
                return this.Title;
            }
        }
        private void OpenExample(object sender, DoubleTappedRoutedEventArgs e)
        {
            var listBox = (ListBox)sender;
            var example = (ExampleWindow)listBox?.SelectedItem;
            example?.Show();
        }
    }
}
