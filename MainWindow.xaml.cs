namespace WpfApp1;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new Board(3, 3, 3, 3);
    }

    private void BoardControl_OnOnWinning(object sender, WinningArgs args)
    {
        BoardControl.IsEnabled = false;
        Title = $"winner is {args.Winner.ToString()}";
    }
}