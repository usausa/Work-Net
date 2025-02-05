namespace WorkSync;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Nito.AsyncEx;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        //TestMethod().GetAwaiter().GetResult();
        AsyncContext.Run(TestMethod);
    }

    private static async Task TestMethod()
    {
        //await Task.Delay(1000).ConfigureAwait(false);
        await Task.Delay(1000);
    }
}
