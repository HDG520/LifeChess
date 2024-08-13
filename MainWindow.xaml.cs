namespace LifeChess;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        int i = 3;
        DataContext = new Board(i,i,i,i);
    }

    private void BoardControl_OnOnWinning(object sender, WinningArgs args)
    {
        BoardControl.IsEnabled = false;
        Title = $"winner is {args.Winner.ToString()}";
    }
}