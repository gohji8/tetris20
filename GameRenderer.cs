using Godot;
using System;

public partial class GameRenderer : Node2D
{
    private const float MaxCellSize = 32f;
    private const float LayoutMargin = 24f;
    private const float HudWidth = 190f;
    private const float HudGap = 32f;

    private readonly Color[] pieceColors =
    {
        new Color(0f, 0f, 0f, 0f),
        new Color(0.18f, 0.88f, 0.99f, 1f),
        new Color(0.97f, 0.94f, 0.32f, 1f),
        new Color(0.32f, 0.52f, 0.97f, 1f),
        new Color(0.98f, 0.62f, 0.23f, 1f),
        new Color(0.72f, 0.42f, 0.97f, 1f),
        new Color(0.29f, 0.84f, 0.48f, 1f),
        new Color(0.96f, 0.24f, 0.34f, 1f)
    };

    private TetrisBoard board;
    private Vector2 lastViewportSize = new(-1, -1);
    private float flashTimer;
    private int[] flashingRows = Array.Empty<int>();
    private bool showDimOverlay = true;
    private Vector2I[] landingCells = Array.Empty<Vector2I>();
    private float landingEffectTimer;

    public float CellSize { get; private set; } = MaxCellSize;
    public Vector2 BoardOffset { get; private set; }

    public void Initialize(TetrisBoard gameBoard)
    {
        board = gameBoard;
        UpdateLayout();
        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        UpdateLayout();
        flashTimer = Math.Max(0f, flashTimer - (float)delta);
        landingEffectTimer = Math.Max(0f, landingEffectTimer - (float)delta);
        QueueRedraw();
    }

    public void ShowLineClear(int[] rows)
    {
        flashingRows = rows;
        flashTimer = 0.24f;
    }

    public void ShowPieceLanding(Vector2I position, int[,] shape)
    {
        var cells = new System.Collections.Generic.List<Vector2I>();
        for (int y = 0; y < shape.GetLength(0); y++)
        {
            for (int x = 0; x < shape.GetLength(1); x++)
            {
                int boardY = position.Y + y;
                if (shape[y, x] != 0 && boardY >= 0)
                {
                    cells.Add(new Vector2I(position.X + x, boardY));
                }
            }
        }

        landingCells = cells.ToArray();
        landingEffectTimer = 0.22f;
    }

    public void SetDimOverlay(bool visible)
    {
        showDimOverlay = visible;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawBackground();
        DrawBoardFrame();
        DrawGrid();

        for (int x = 0; x < TetrisBoard.Width; x++)
        {
            for (int y = 0; y < TetrisBoard.Height; y++)
            {
                int type = board.Cells[x, y];
                if (type != 0)
                {
                    DrawCell(x, y, type, 1f);
                }
            }
        }

        if (board.CurrentShape != null)
        {
            DrawPiece(board.CurrentShape, board.GetGhostPosition(), board.CurrentType, 0.18f);
            DrawPiece(board.CurrentShape, board.CurrentPosition, board.CurrentType, 1f);
        }

        DrawLineFlashes();
        DrawLandingEffect();
        DrawNextPiece();
        if (showDimOverlay)
        {
            DrawRect(new Rect2(Vector2.Zero, GetViewportRect().Size), new Color(0.015f, 0.025f, 0.08f, 0.58f));
        }
    }

    private void UpdateLayout()
    {
        Vector2 viewportSize = GetViewportRect().Size;
        if (viewportSize == lastViewportSize)
        {
            return;
        }

        lastViewportSize = viewportSize;
        float availableHeight = Math.Max(1f, viewportSize.Y - LayoutMargin * 2f);
        float availableWidth = Math.Max(1f, viewportSize.X - LayoutMargin * 2f - HudGap - HudWidth);
        CellSize = Math.Min(MaxCellSize, Math.Min(availableHeight / TetrisBoard.Height, availableWidth / TetrisBoard.Width));

        float contentWidth = TetrisBoard.Width * CellSize + HudGap + HudWidth;
        float contentHeight = TetrisBoard.Height * CellSize;
        BoardOffset = new Vector2(
            Math.Max(LayoutMargin, (viewportSize.X - contentWidth) / 2f),
            Math.Max(0f, (viewportSize.Y - contentHeight) / 2f));
    }

    private void DrawBackground()
    {
        Vector2 viewportSize = GetViewportRect().Size;
        DrawRect(new Rect2(Vector2.Zero, viewportSize), new Color(0.08f, 0.09f, 0.17f, 1f));
        for (int i = 0; i < 16; i++)
        {
            float y = i * 48f;
            float alpha = 0.07f + i * 0.005f;
            DrawRect(new Rect2(0, y, viewportSize.X, 30f), new Color(0.26f, 0.33f, 0.72f, alpha));
        }
        DrawRect(new Rect2(0, 0, viewportSize.X, viewportSize.Y), new Color(0.02f, 0.04f, 0.08f, 1f), false);
    }

    private void DrawBoardFrame()
    {
        Rect2 area = new(BoardOffset.X - 12, BoardOffset.Y - 12, TetrisBoard.Width * CellSize + 24, TetrisBoard.Height * CellSize + 24);
        DrawRect(area, new Color(0.12f, 0.16f, 0.27f, 0.85f));
        DrawRect(area, new Color(0.57f, 0.75f, 1f, 0.18f), false);
    }

    private void DrawGrid()
    {
        for (int x = 0; x <= TetrisBoard.Width; x++)
        {
            float gx = BoardOffset.X + x * CellSize;
            DrawLine(new Vector2(gx, BoardOffset.Y), new Vector2(gx, BoardOffset.Y + TetrisBoard.Height * CellSize), new Color(0.70f, 0.80f, 1f, 0.10f));
        }

        for (int y = 0; y <= TetrisBoard.Height; y++)
        {
            float gy = BoardOffset.Y + y * CellSize;
            DrawLine(new Vector2(BoardOffset.X, gy), new Vector2(BoardOffset.X + TetrisBoard.Width * CellSize, gy), new Color(0.70f, 0.80f, 1f, 0.10f));
        }
    }

    private void DrawPiece(int[,] shape, Vector2I origin, int type, float alpha)
    {
        for (int y = 0; y < shape.GetLength(0); y++)
        {
            for (int x = 0; x < shape.GetLength(1); x++)
            {
                if (shape[y, x] == 0)
                {
                    continue;
                }

                int boardY = origin.Y + y;
                if (boardY >= 0)
                {
                    DrawCell(origin.X + x, boardY, type, alpha);
                }
            }
        }
    }

    private void DrawCell(int boardX, int boardY, int type, float alpha)
    {
        Color color = pieceColors[type];
        float x = BoardOffset.X + boardX * CellSize;
        float y = BoardOffset.Y + boardY * CellSize;
        float inset = Math.Max(1f, CellSize * 0.06f);
        float highlight = CellSize * 0.25f;
        DrawRect(new Rect2(x + inset, y + inset, CellSize - inset * 2f, CellSize - inset * 2f), new Color(color.R, color.G, color.B, alpha));
        DrawRect(new Rect2(x + highlight, y + highlight, CellSize - highlight * 2f, CellSize - highlight * 2f), new Color(1f, 1f, 1f, 0.14f * alpha));
    }

    private void DrawNextPiece()
    {
        int[,] shape = TetrisBoard.Shapes[board.NextType];
        float size = Math.Min(20f, CellSize * 0.72f);
        float startX = BoardOffset.X + TetrisBoard.Width * CellSize + HudGap;
        float startY = BoardOffset.Y + 274f;
        Color color = pieceColors[board.NextType];
        for (int y = 0; y < shape.GetLength(0); y++)
        {
            for (int x = 0; x < shape.GetLength(1); x++)
            {
                if (shape[y, x] != 0)
                {
                    float inset = Math.Max(1f, size * 0.07f);
                    DrawRect(new Rect2(startX + x * size + inset, startY + y * size + inset, size - inset * 2f, size - inset * 2f), color);
                }
            }
        }
    }

    private void DrawLineFlashes()
    {
        if (flashTimer <= 0f)
        {
            return;
        }

        float strength = flashTimer / 0.24f;
        strength *= strength;
        foreach (int row in flashingRows)
        {
            float y = BoardOffset.Y + row * CellSize;
            DrawRect(new Rect2(BoardOffset.X - 4f, y - 4f, TetrisBoard.Width * CellSize + 8f, CellSize + 8f), new Color(0.34f, 0.92f, 1f, strength * 0.28f));
            DrawRect(new Rect2(BoardOffset.X, y + CellSize * 0.42f, TetrisBoard.Width * CellSize, CellSize * 0.16f), new Color(0.86f, 0.98f, 1f, strength * 0.52f));
        }
    }

    private void DrawLandingEffect()
    {
        if (landingEffectTimer <= 0f)
        {
            return;
        }

        float progress = 1f - landingEffectTimer / 0.22f;
        float alpha = (1f - progress) * 0.38f;
        float inset = progress * CellSize * 0.18f;
        foreach (Vector2I cell in landingCells)
        {
            float x = BoardOffset.X + cell.X * CellSize + inset;
            float y = BoardOffset.Y + cell.Y * CellSize + inset;
            float size = CellSize - inset * 2f;
            DrawRect(
                new Rect2(x, y, size, size),
                new Color(0.85f, 0.95f, 1f, alpha),
                false,
                Math.Max(1f, CellSize * 0.06f));
        }
    }
}
