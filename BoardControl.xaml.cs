using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace WpfApp1;

public sealed class LatticeData : INotifyPropertyChanged
{
    public readonly int Row;
    public readonly int Col;

    private bool _isWinning;

    public bool IsWinning
    {
        get => _isWinning;
        set
        {
            if (_isWinning == value) return;
            _isWinning = value;
            OnPropertyChanged();
            OnPropertyChanged();
        }
    }

    private LatticeType _type;

    public LatticeType Type
    {
        get => _type;
        set
        {
            if (_type == value) return;
            _type = value;
            OnPropertyChanged();
        }
    }
    
    private int _lastLifeTime = -1;

    public int LastLifeTime
    {
        get => _lastLifeTime;
        set
        {
            if (value == _lastLifeTime) return;
            _lastLifeTime = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public LatticeData(int row, int col, LatticeType type)
    {
        Row = row;
        Col = col;
        Type = type;
    }
}

public enum LatticeType
{
    X = -1,
    O = 1,
    Empty = 0
}

public class Board(int winPieces)
{
    public readonly List<LatticeData> Lattices = [];

    public readonly int WinPieces = winPieces;

    public readonly int Row;

    public readonly int Col;

    public readonly int LatticeLife;

    public LatticeData? this[int row, int col]
    {
        get
        {
            return Lattices.FirstOrDefault(l => l.Row == row && l.Col == col);
        }
        set
        {
            if (this[row, col] != null || this[row, col]!.Type != LatticeType.Empty) return;
            value!.LastLifeTime = this.LatticeLife;
            Lattices.Add(value);
        }
    }

    public Board(int row, int col, int latticeLife, int winPieces) : this(winPieces)
    {
        Row = row;
        Col = col;
        LatticeLife = latticeLife;//ToDo:实现具体规则
        for (int r = 0; r < row; r++)
        {
            for (int c = 0; c < col; c++)
            {
                Lattices.Add(new LatticeData(r,c, LatticeType.Empty));
            }
        }
    }

    public ObservableCollection<ObservableCollection<LatticeData>> LatticeArray => new(Lattices.GroupBy(data => data.Row)
        .Select(group => new ObservableCollection<LatticeData>(group)));
}

public partial class BoardControl
{
    public event Winning? OnWinning;
    public Board Board
    {
        get => (Board)GetValue(BoardProperty);
        set => SetValue(BoardProperty, value);
    }

    private LatticeType CurrentPlayer { get; set; } = LatticeType.O;
    
    public static readonly DependencyProperty BoardProperty = DependencyProperty.Register(nameof(Board), typeof(Board),
        typeof(BoardControl), new PropertyMetadata(default));

    public BoardControl()
    {
        InitializeComponent();
    }

    private void Lattice_OnClick(object sender, RoutedEventArgs e)
    {
        var btn = (Button)sender;
        var latticeData = btn.DataContext as LatticeData;
        CalcLastLife();
        SwitchPlayer();
        latticeData!.Type = CurrentPlayer;
        latticeData.LastLifeTime = Board.LatticeLife;
        if (CheckBoard(latticeData, out var winner, out var winningLattices))
        {
            OnWinning?.Invoke(this, new WinningArgs(winner, winningLattices));
        }
    }

    private void CalcLastLife()
    {
        foreach (var lattice in Board.Lattices.Where(lattice => lattice.Type == CurrentPlayer))
        {
            lattice.LastLifeTime--;
            if (lattice.LastLifeTime != 0) continue;
            lattice.Type = LatticeType.Empty;
            lattice.LastLifeTime = -1;
        }
    }

    private void SwitchPlayer()
    {
        CurrentPlayer = (LatticeType)(-(int)CurrentPlayer);
    }

    private bool CheckBoard(LatticeData lattice, out LatticeType winner, out ObservableCollection<LatticeData> winningLattices)
    {
        var directions = new (int rowInc, int colInc)[]
        {
            (0, -1), // Left
            (0, 1),  // Right
            (-1, 0), // Top
            (1, 0),  // Bottom
            (-1, -1), // LeftTop
            (1, -1),  // LeftBottom
            (-1, 1),  // RightTop
            (1, 1)    // RightBottom
        };

        winningLattices = [];

        foreach (var (rowInc, colInc) in directions)
        {
            List<LatticeData> latticesInDirection = [];

            latticesInDirection.AddRange(CountInDirection(lattice, rowInc, colInc));
            latticesInDirection.AddRange(CountInDirection(lattice, -rowInc, -colInc));

            if (latticesInDirection.Count + 1 < Board.WinPieces) continue;
            winner = lattice.Type;
            winningLattices.Add(lattice);
            foreach (var data in latticesInDirection)
            {
                data.IsWinning = true;
                winningLattices.Add(data);
            }
            return true;
        }

        winner = LatticeType.Empty;
        winningLattices = [];
        return false;
    }

    private List<LatticeData> CountInDirection(LatticeData lattice, int rowInc, int colInc)
    {
        List<LatticeData> lattices = [];
        int row = lattice.Row + rowInc;
        int col = lattice.Col + colInc;

        while (row >= 0 && row < Board.Row && col >= 0 && col < Board.Col)
        {
            var adjacentLattice = Board[row, col];
            if (adjacentLattice!.Type != lattice.Type)
            {
                break;
            }
            lattices.Add(adjacentLattice);
            row += rowInc;
            col += colInc;
        }

        return lattices;
    }
}

public delegate void Winning(object sender, WinningArgs args);

public class WinningArgs(LatticeType winner, ObservableCollection<LatticeData> winningLattices) : EventArgs
{
    public readonly LatticeType Winner = winner;

    public ObservableCollection<LatticeData> WinningLattices = winningLattices;
}

public class LatticeVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is LatticeType.Empty ? Visibility.Collapsed : Visibility.Visible;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class LatticeBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is LatticeType.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}

public class LastLifeTimeVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is int i && i != -1 ? Visibility.Visible : Visibility.Collapsed;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null;
    }
}