using Godot;

public partial class Main : Node3D
{
    private readonly TetrisBoard board = new();
    private GameRenderer renderer;
    private GameHud hud;
    private AmbientMusic music;

    private float dropTimer;
    private float moveRepeatTimer;
    private bool leftHeld;
    private bool rightHeld;
    private bool rotateHeld;
    private bool hardDropHeld;
    private bool pauseHeld;
    private bool restartHeld;
    private bool isStarted;
    private bool isPaused;

    public override void _Ready()
    {
        renderer = new GameRenderer();
        AddChild(renderer);
        renderer.Initialize(board);

        hud = new GameHud();
        AddChild(hud);
        hud.StartRequested += StartGame;
        hud.PauseRequested += TogglePause;

        music = new AmbientMusic();
        AddChild(music);

        board.LinesClearedEvent += OnLinesCleared;
        board.RotationEvent += music.PlayRotationSound;
        board.PieceLandedEvent += OnPieceLanded;
        board.GameOverEvent += OnGameOver;
        UpdateHudLayout();
        board.Reset();
        hud.UpdateScore(board.Score, board.LinesCleared);
        hud.ShowMenu();
        renderer.SetDimOverlay(true);
    }

    public override void _Process(double delta)
    {
        UpdateHudLayout();
        float dt = (float)delta;
        bool startPressed = Input.IsKeyPressed(Key.Enter) || Input.IsKeyPressed(Key.Space);

        if (!isStarted && startPressed)
        {
            StartGame();
            return;
        }

        bool pausePressed = Input.IsKeyPressed(Key.P);
        if (pausePressed && !pauseHeld && isStarted && !board.IsGameOver)
        {
            TogglePause();
        }
        pauseHeld = pausePressed;

        if (!isStarted)
        {
            return;
        }

        if (board.IsGameOver)
        {
            bool restartPressed = Input.IsKeyPressed(Key.R);
            if (restartPressed && !restartHeld)
            {
                StartGame();
            }
            restartHeld = restartPressed;
            return;
        }

        if (isPaused)
        {
            return;
        }

        HandleInput(dt);
        float fallDelay = Input.IsKeyPressed(Key.Down) ? 0.045f : 0.42f;
        dropTimer += dt;
        if (dropTimer >= fallDelay)
        {
            board.StepDown();
            dropTimer = 0f;
            RefreshHud();
        }
    }

    private void HandleInput(float delta)
    {
        moveRepeatTimer -= delta;

        bool rotatePressed = Input.IsKeyPressed(Key.Up) || Input.IsKeyPressed(Key.X) || Input.IsKeyPressed(Key.C);
        if (rotatePressed && !rotateHeld)
        {
            board.Rotate();
        }
        rotateHeld = rotatePressed;

        bool hardDropPressed = Input.IsKeyPressed(Key.Space);
        if (hardDropPressed && !hardDropHeld)
        {
            board.HardDrop();
            RefreshHud();
        }
        hardDropHeld = hardDropPressed;

        bool leftPressed = Input.IsKeyPressed(Key.Left);
        if (leftPressed && (!leftHeld || moveRepeatTimer <= 0f))
        {
            if (board.Move(-1, 0))
            {
                moveRepeatTimer = leftHeld ? 0.08f : 0.12f;
            }
        }
        leftHeld = leftPressed;

        bool rightPressed = Input.IsKeyPressed(Key.Right);
        if (rightPressed && (!rightHeld || moveRepeatTimer <= 0f))
        {
            if (board.Move(1, 0))
            {
                moveRepeatTimer = rightHeld ? 0.08f : 0.12f;
            }
        }
        rightHeld = rightPressed;
    }

    private void StartGame()
    {
        board.Start();
        isStarted = true;
        isPaused = false;
        dropTimer = 0f;
        moveRepeatTimer = 0f;
        leftHeld = false;
        rightHeld = false;
        rotateHeld = false;
        hardDropHeld = false;
        restartHeld = false;
        pauseHeld = false;
        hud.UpdateScore(board.Score, board.LinesCleared);
        hud.ShowPlaying();
        renderer.SetDimOverlay(false);
        RefreshHud();
    }

    private void TogglePause()
    {
        if (!isStarted || board.IsGameOver)
        {
            return;
        }

        isPaused = !isPaused;
        hud.SetPaused(isPaused);
        renderer.SetDimOverlay(isPaused);
    }

    private void OnLinesCleared(int[] rows)
    {
        renderer.ShowLineClear(rows);
        music.PlayLineClearAccent();
        music.SetLevel(board.LinesCleared);
        RefreshHud();
    }

    private void OnPieceLanded(Vector2I position, int[,] shape, int type)
    {
        renderer.ShowPieceLanding(position, shape);
        music.PlayLandingSound();
    }

    private void OnGameOver()
    {
        isPaused = false;
        hud.ShowGameOver();
        renderer.SetDimOverlay(true);
    }

    private void RefreshHud()
    {
        hud.UpdateScore(board.Score, board.LinesCleared);
    }

    private void UpdateHudLayout()
    {
        if (hud == null || renderer == null)
        {
            return;
        }

        hud.UpdateLayout(GetViewport().GetVisibleRect().Size, renderer.BoardOffset, renderer.CellSize);
    }
}
