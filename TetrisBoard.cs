using Godot;
using System;

public sealed class TetrisBoard
{
    public const int Width = 10;
    public const int Height = 20;

    public static readonly int[][,] Shapes =
    {
        new int[0, 0],
        new int[,] { { 1, 1, 1, 1 } },
        new int[,] { { 1, 1 }, { 1, 1 } },
        new int[,] { { 1, 0, 0 }, { 1, 1, 1 } },
        new int[,] { { 0, 0, 1 }, { 1, 1, 1 } },
        new int[,] { { 0, 1, 0 }, { 1, 1, 1 } },
        new int[,] { { 1, 1, 0 }, { 0, 1, 1 } },
        new int[,] { { 0, 1, 1 }, { 1, 1, 0 } }
    };

    private readonly Random random = new();

    public int[,] Cells { get; private set; } = new int[Width, Height];
    public int[,] CurrentShape { get; private set; }
    public Vector2I CurrentPosition { get; private set; }
    public int CurrentType { get; private set; }
    public int NextType { get; private set; } = 1;
    public int Score { get; private set; }
    public int LinesCleared { get; private set; }
    public bool IsGameOver { get; private set; }

    public event Action<int[]> LinesClearedEvent;
    public event Action RotationEvent;
    public event Action<Vector2I, int[,], int> PieceLandedEvent;
    public event Action GameOverEvent;

    public void Reset()
    {
        Cells = new int[Width, Height];
        CurrentShape = null;
        CurrentPosition = Vector2I.Zero;
        CurrentType = 0;
        NextType = random.Next(1, Shapes.Length);
        Score = 0;
        LinesCleared = 0;
        IsGameOver = false;
    }

    public void Start()
    {
        Reset();
        SpawnPiece();
    }

    public bool Move(int dx, int dy)
    {
        if (IsGameOver || CurrentShape == null)
        {
            return false;
        }

        Vector2I destination = CurrentPosition + new Vector2I(dx, dy);
        if (Collides(CurrentShape, destination))
        {
            return false;
        }

        CurrentPosition = destination;
        return true;
    }

    public void StepDown()
    {
        if (!Move(0, 1))
        {
            LockPiece();
        }
    }

    public void HardDrop()
    {
        if (IsGameOver || CurrentShape == null)
        {
            return;
        }

        int distance = 0;
        while (!Collides(CurrentShape, CurrentPosition + new Vector2I(0, distance + 1)))
        {
            distance++;
        }

        CurrentPosition += new Vector2I(0, distance);
        Score += distance * 2;
        LockPiece();
    }

    public void Rotate()
    {
        if (IsGameOver || CurrentShape == null)
        {
            return;
        }

        int[,] rotated = RotateMatrix(CurrentShape);
        int[] offsets = { 0, -1, 1, -2, 2 };
        foreach (int offset in offsets)
        {
            Vector2I position = CurrentPosition + new Vector2I(offset, 0);
            if (!Collides(rotated, position))
            {
                CurrentShape = rotated;
                CurrentPosition = position;
                RotationEvent?.Invoke();
                return;
            }
        }
    }

    public Vector2I GetGhostPosition()
    {
        Vector2I ghostPosition = CurrentPosition;
        if (CurrentShape == null)
        {
            return ghostPosition;
        }

        while (!Collides(CurrentShape, ghostPosition + new Vector2I(0, 1)))
        {
            ghostPosition += new Vector2I(0, 1);
        }

        return ghostPosition;
    }

    private void SpawnPiece()
    {
        CurrentType = NextType;
        NextType = random.Next(1, Shapes.Length);
        CurrentShape = Shapes[CurrentType];
        CurrentPosition = new Vector2I(3, -1);

        if (Collides(CurrentShape, CurrentPosition))
        {
            EndGame();
        }
    }

    private void LockPiece()
    {
        if (CurrentShape == null)
        {
            return;
        }

        for (int y = 0; y < CurrentShape.GetLength(0); y++)
        {
            for (int x = 0; x < CurrentShape.GetLength(1); x++)
            {
                if (CurrentShape[y, x] != 0 && CurrentPosition.Y + y < 0)
                {
                    EndGame();
                    return;
                }
            }
        }

        PieceLandedEvent?.Invoke(CurrentPosition, CurrentShape, CurrentType);

        for (int y = 0; y < CurrentShape.GetLength(0); y++)
        {
            for (int x = 0; x < CurrentShape.GetLength(1); x++)
            {
                if (CurrentShape[y, x] == 0)
                {
                    continue;
                }

                int boardX = CurrentPosition.X + x;
                int boardY = CurrentPosition.Y + y;
                Cells[boardX, boardY] = CurrentType;
            }
        }

        ClearFullLines();
        if (!IsGameOver)
        {
            SpawnPiece();
        }
    }

    private void ClearFullLines()
    {
        int cleared = 0;
        var rows = new System.Collections.Generic.List<int>();

        for (int y = 0; y < Height; y++)
        {
            bool full = true;
            for (int x = 0; x < Width; x++)
            {
                if (Cells[x, y] == 0)
                {
                    full = false;
                    break;
                }
            }

            if (!full)
            {
                continue;
            }

            cleared++;
            rows.Add(y);
            for (int row = y; row > 0; row--)
            {
                for (int x = 0; x < Width; x++)
                {
                    Cells[x, row] = Cells[x, row - 1];
                }
            }

            for (int x = 0; x < Width; x++)
            {
                Cells[x, 0] = 0;
            }

            y--;
        }

        if (cleared == 0)
        {
            return;
        }

        LinesCleared += cleared;
        Score += cleared switch
        {
            1 => 100,
            2 => 300,
            3 => 500,
            4 => 800,
            _ => 0
        };
        LinesClearedEvent?.Invoke(rows.ToArray());
    }

    private void EndGame()
    {
        if (IsGameOver)
        {
            return;
        }

        IsGameOver = true;
        GameOverEvent?.Invoke();
    }

    private bool Collides(int[,] shape, Vector2I position)
    {
        for (int y = 0; y < shape.GetLength(0); y++)
        {
            for (int x = 0; x < shape.GetLength(1); x++)
            {
                if (shape[y, x] == 0)
                {
                    continue;
                }

                int boardX = position.X + x;
                int boardY = position.Y + y;
                if (boardX < 0 || boardX >= Width || boardY >= Height)
                {
                    return true;
                }

                if (boardY >= 0 && Cells[boardX, boardY] != 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static int[,] RotateMatrix(int[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int columns = matrix.GetLength(1);
        var rotated = new int[columns, rows];
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                rotated[x, rows - 1 - y] = matrix[y, x];
            }
        }

        return rotated;
    }
}
