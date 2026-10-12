using Godot;

public partial class GameHud : CanvasLayer
{
    private Label scoreLabel;
    private Label linesLabel;
    private Label statusLabel;
    private Label nextLabel;
    private Label menuTitle;
    private Label menuHint;
    private Button pauseButton;
    private Button startButton;

    public event System.Action StartRequested;
    public event System.Action PauseRequested;

    public override void _Ready()
    {
        var root = new Control();
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        root.MouseFilter = Control.MouseFilterEnum.Ignore;
        AddChild(root);

        scoreLabel = CreateLabel(root, "SCORE: 0", 30, new Color(0.96f, 0.97f, 1f));
        linesLabel = CreateLabel(root, "LINES: 0", 22, new Color(0.76f, 0.86f, 1f));
        statusLabel = CreateLabel(root, "READY", 24, new Color(0.6f, 0.93f, 0.78f));
        nextLabel = CreateLabel(root, "NEXT", 20, new Color(0.76f, 0.86f, 1f));

        pauseButton = new Button { Text = "PAUSE  [P]", Size = new Vector2(170, 44), FocusMode = Control.FocusModeEnum.None };
        pauseButton.AddThemeFontSizeOverride("font_size", 18);
        pauseButton.AddThemeColorOverride("font_color", new Color(0.92f, 0.96f, 1f));
        pauseButton.Pressed += () => PauseRequested?.Invoke();
        root.AddChild(pauseButton);

        menuTitle = CreateLabel(root, "TETRIS\nEFFECT", 48, new Color(0.55f, 0.9f, 1f));
        menuTitle.HorizontalAlignment = HorizontalAlignment.Center;
        menuHint = CreateLabel(root, "Move  ← →     Rotate  ↑ / X     Drop  SPACE", 18, new Color(0.78f, 0.84f, 1f));
        menuHint.HorizontalAlignment = HorizontalAlignment.Center;

        startButton = new Button { Text = "START", Size = new Vector2(220, 54), FocusMode = Control.FocusModeEnum.None };
        startButton.AddThemeFontSizeOverride("font_size", 23);
        startButton.Pressed += () => StartRequested?.Invoke();
        root.AddChild(startButton);

        ShowMenu();
    }

    public void UpdateLayout(Vector2 viewportSize, Vector2 boardOffset, float cellSize)
    {
        float hudX = boardOffset.X + TetrisBoard.Width * cellSize + 32f;
        scoreLabel.Position = new Vector2(hudX, boardOffset.Y + 12f);
        linesLabel.Position = new Vector2(hudX, boardOffset.Y + 60f);
        statusLabel.Position = new Vector2(hudX, boardOffset.Y + 112f);
        pauseButton.Position = new Vector2(hudX, boardOffset.Y + 174f);
        nextLabel.Position = new Vector2(hudX, boardOffset.Y + 240f);

        float centerX = viewportSize.X * 0.5f;
        float centerY = viewportSize.Y * 0.5f;
        menuTitle.Position = new Vector2(centerX - 175f, centerY - 150f);
        menuTitle.Size = new Vector2(350f, 120f);
        menuHint.Position = new Vector2(centerX - 280f, centerY + 75f);
        menuHint.Size = new Vector2(560f, 34f);
        startButton.Position = new Vector2(centerX - 110f, centerY + 125f);
    }

    public void UpdateScore(int score, int lines)
    {
        scoreLabel.Text = "SCORE: " + score;
        linesLabel.Text = "LINES: " + lines;
    }

    public void SetStatus(string text)
    {
        statusLabel.Text = text;
    }

    public void SetPaused(bool paused)
    {
        statusLabel.Text = paused ? "PAUSED" : "PLAYING";
        pauseButton.Text = paused ? "RESUME  [P]" : "PAUSE  [P]";
    }

    public void ShowPlaying()
    {
        pauseButton.Visible = true;
        pauseButton.Disabled = false;
        menuTitle.Visible = false;
        menuHint.Visible = false;
        startButton.Visible = false;
    }

    public void ShowMenu()
    {
        pauseButton.Visible = false;
        pauseButton.Disabled = false;
        menuTitle.Text = "TETRIS\nEFFECT";
        menuHint.Text = "Move  ← →     Rotate  ↑ / X     Drop  SPACE";
        startButton.Text = "START";
        menuTitle.Visible = true;
        menuHint.Visible = true;
        startButton.Visible = true;
        statusLabel.Text = "READY";
        pauseButton.Text = "PAUSE  [P]";
    }

    public void ShowGameOver()
    {
        pauseButton.Disabled = true;
        statusLabel.Text = "GAME OVER\nR to restart";
        menuTitle.Text = "GAME\nOVER";
        menuHint.Text = "Press R or click below to play again";
        startButton.Text = "PLAY AGAIN";
        menuTitle.Visible = true;
        menuHint.Visible = true;
        startButton.Visible = true;
    }

    private static Label CreateLabel(Control parent, string text, int fontSize, Color color)
    {
        var label = new Label { Text = text };
        label.AddThemeColorOverride("font_color", color);
        label.AddThemeFontSizeOverride("font_size", fontSize);
        parent.AddChild(label);
        return label;
    }
}
