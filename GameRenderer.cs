using Godot;
using System;

public partial class GameRenderer : Node3D
{
    private const float BoardWorldWidth = TetrisBoard.Width;
    private const float BoardWorldHeight = TetrisBoard.Height;
    private const float ViewHeight = 22.5f;
    private const float HudWidthPixels = 190f;
    private const float HudGapPixels = 32f;
    private const float LayoutMarginPixels = 24f;

    private readonly Color[] pieceColors =
    {
        new Color(0f, 0f, 0f, 0f),
        new Color(0.18f, 0.88f, 0.99f),
        new Color(0.97f, 0.94f, 0.32f),
        new Color(0.32f, 0.52f, 0.97f),
        new Color(0.98f, 0.62f, 0.23f),
        new Color(0.72f, 0.42f, 0.97f),
        new Color(0.29f, 0.84f, 0.48f),
        new Color(0.96f, 0.24f, 0.34f)
    };

    private readonly MeshInstance3D[,] settledBlocks = new MeshInstance3D[TetrisBoard.Width, TetrisBoard.Height];
    private readonly MeshInstance3D[] activeBlocks = new MeshInstance3D[4];
    private readonly MeshInstance3D[] ghostBlocks = new MeshInstance3D[4];
    private readonly MeshInstance3D[] nextBlocks = new MeshInstance3D[4];
    private readonly StandardMaterial3D[] pieceMaterials = new StandardMaterial3D[8];

    private TetrisBoard board;
    private Camera3D camera;
    private ColorRect dimOverlay;
    private StandardMaterial3D ghostMaterial;
    private StandardMaterial3D landingMaterial;
    private MeshInstance3D landingPulse;
    private Vector2 lastViewportSize = new(-1, -1);
    private Vector2 cameraTarget;
    private float flashTimer;
    private float landingEffectTimer;
    private int[] flashingRows = Array.Empty<int>();
    private bool showDimOverlay = true;

    public float CellSize { get; private set; }
    public Vector2 BoardOffset { get; private set; }

    public override void _Ready()
    {
        CreateWorld();
        CreateBlockMeshes();
        CreateDimOverlay();
    }

    public void Initialize(TetrisBoard gameBoard)
    {
        board = gameBoard;
        UpdateLayout();
        UpdateBoardMeshes();
    }

    public override void _Process(double delta)
    {
        UpdateLayout();
        flashTimer = Math.Max(0f, flashTimer - (float)delta);
        landingEffectTimer = Math.Max(0f, landingEffectTimer - (float)delta);
        UpdateBoardMeshes();
    }

    public void ShowLineClear(int[] rows)
    {
        flashingRows = rows;
        flashTimer = 0.24f;
    }

    public void ShowPieceLanding(Vector2I position, int[,] shape)
    {
        float lowestRow = position.Y;
        for (int y = 0; y < shape.GetLength(0); y++)
        {
            for (int x = 0; x < shape.GetLength(1); x++)
            {
                if (shape[y, x] != 0)
                {
                    lowestRow = Math.Max(lowestRow, position.Y + y);
                }
            }
        }

        landingPulse.Position = new Vector3(0f, BoardWorldHeight / 2f - lowestRow - 0.5f, 0.22f);
        landingPulse.Visible = true;
        landingEffectTimer = 0.22f;
    }

    public void SetDimOverlay(bool visible)
    {
        showDimOverlay = visible;
        if (dimOverlay != null)
        {
            dimOverlay.Visible = visible;
        }
    }

    private void CreateWorld()
    {
        camera = new Camera3D
        {
            Projection = Camera3D.ProjectionType.Orthogonal,
            Size = ViewHeight,
            Current = true
        };
        AddChild(camera);

        var environment = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.035f, 0.045f, 0.09f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.55f, 0.62f, 0.82f),
            AmbientLightEnergy = 0.65f,
            TonemapMode = Godot.Environment.ToneMapper.Filmic
        };
        camera.Environment = environment;

        var keyLight = new DirectionalLight3D
        {
            RotationDegrees = new Vector3(-25f, -28f, 0f),
            LightColor = new Color(0.72f, 0.84f, 1f),
            LightEnergy = 1.05f,
            ShadowEnabled = true
        };
        AddChild(keyLight);

        var fillLight = new OmniLight3D
        {
            Position = new Vector3(3f, 7f, 8f),
            LightColor = new Color(0.44f, 0.28f, 1f),
            LightEnergy = 1.4f,
            OmniRange = 22f
        };
        AddChild(fillLight);

        AddBoardBacking();
    }

    private void CreateDimOverlay()
    {
        var canvasLayer = new CanvasLayer { Layer = -1 };
        AddChild(canvasLayer);
        dimOverlay = new ColorRect
        {
            Color = new Color(0.015f, 0.025f, 0.08f, 0.58f),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Visible = showDimOverlay
        };
        dimOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        canvasLayer.AddChild(dimOverlay);
    }

    private void AddBoardBacking()
    {
        var backing = new MeshInstance3D
        {
            Name = "BoardBacking",
            Mesh = new BoxMesh { Size = new Vector3(BoardWorldWidth + 0.35f, BoardWorldHeight + 0.35f, 0.28f) },
            Position = new Vector3(0f, 0f, -0.35f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0.035f, 0.055f, 0.11f),
                Metallic = 0.25f,
                Roughness = 0.38f,
                EmissionEnabled = true,
                Emission = new Color(0.025f, 0.06f, 0.16f),
                EmissionEnergyMultiplier = 0.7f
            }
        };
        AddChild(backing);

        var gridMesh = new ImmediateMesh();
        gridMesh.SurfaceBegin(Mesh.PrimitiveType.Lines);
        for (int x = 0; x <= TetrisBoard.Width; x++)
        {
            float worldX = x - BoardWorldWidth / 2f;
            gridMesh.SurfaceAddVertex(new Vector3(worldX, -BoardWorldHeight / 2f, -0.19f));
            gridMesh.SurfaceAddVertex(new Vector3(worldX, BoardWorldHeight / 2f, -0.19f));
        }
        for (int y = 0; y <= TetrisBoard.Height; y++)
        {
            float worldY = BoardWorldHeight / 2f - y;
            gridMesh.SurfaceAddVertex(new Vector3(-BoardWorldWidth / 2f, worldY, -0.19f));
            gridMesh.SurfaceAddVertex(new Vector3(BoardWorldWidth / 2f, worldY, -0.19f));
        }
        gridMesh.SurfaceEnd();

        var grid = new MeshInstance3D
        {
            Name = "BoardGrid",
            Mesh = gridMesh,
            MaterialOverride = new StandardMaterial3D
            {
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                AlbedoColor = new Color(0.3f, 0.52f, 0.82f, 0.35f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha
            }
        };
        AddChild(grid);
    }

    private void CreateBlockMeshes()
    {
        for (int type = 1; type < pieceMaterials.Length; type++)
        {
            pieceMaterials[type] = new StandardMaterial3D
            {
                AlbedoColor = pieceColors[type],
                Metallic = 0.28f,
                Roughness = 0.3f,
                EmissionEnabled = true,
                Emission = pieceColors[type] * 0.12f,
                EmissionEnergyMultiplier = 0.7f
            };
        }

        var blockMesh = new BoxMesh { Size = new Vector3(0.9f, 0.9f, 0.62f) };
        for (int x = 0; x < TetrisBoard.Width; x++)
        {
            for (int y = 0; y < TetrisBoard.Height; y++)
            {
                var block = new MeshInstance3D { Mesh = blockMesh, Visible = false };
                AddChild(block);
                settledBlocks[x, y] = block;
            }
        }

        for (int i = 0; i < activeBlocks.Length; i++)
        {
            activeBlocks[i] = new MeshInstance3D { Mesh = blockMesh, Visible = false };
            ghostBlocks[i] = new MeshInstance3D { Mesh = blockMesh, Visible = false };
            nextBlocks[i] = new MeshInstance3D { Mesh = blockMesh, Visible = false };
            AddChild(ghostBlocks[i]);
            AddChild(activeBlocks[i]);
            AddChild(nextBlocks[i]);
        }

        ghostMaterial = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(0.52f, 0.78f, 1f, 0.2f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            EmissionEnabled = true,
            Emission = new Color(0.18f, 0.5f, 0.9f),
            EmissionEnergyMultiplier = 0.4f
        };

        var pulseMesh = new BoxMesh { Size = new Vector3(BoardWorldWidth * 0.92f, 0.09f, 0.035f) };
        landingMaterial = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            AlbedoColor = new Color(0.55f, 0.9f, 1f, 0.52f),
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            EmissionEnabled = true,
            Emission = new Color(0.3f, 0.8f, 1f),
            EmissionEnergyMultiplier = 0.75f
        };
        landingPulse = new MeshInstance3D
        {
            Mesh = pulseMesh,
            MaterialOverride = landingMaterial,
            Visible = false
        };
        AddChild(landingPulse);
    }

    private void UpdateLayout()
    {
        Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
        if (viewportSize == lastViewportSize)
        {
            return;
        }

        lastViewportSize = viewportSize;
        float pixelsPerWorldUnit = viewportSize.Y / ViewHeight;
        float desiredCellSize = Math.Min(
            pixelsPerWorldUnit,
            Math.Max(1f, (viewportSize.Y - LayoutMarginPixels * 2f) / TetrisBoard.Height));
        CellSize = desiredCellSize;

        float hudAndBoardWidth = TetrisBoard.Width * CellSize + HudGapPixels + HudWidthPixels;
        BoardOffset = new Vector2(
            Math.Max(LayoutMarginPixels, (viewportSize.X - hudAndBoardWidth) / 2f),
            (viewportSize.Y - TetrisBoard.Height * CellSize) / 2f);

        float boardCenterScreenX = BoardOffset.X + TetrisBoard.Width * CellSize / 2f;
        float boardCenterScreenY = BoardOffset.Y + TetrisBoard.Height * CellSize / 2f;
        float targetX = (viewportSize.X / 2f - boardCenterScreenX) / pixelsPerWorldUnit;
        float targetY = (boardCenterScreenY - viewportSize.Y / 2f) / pixelsPerWorldUnit;
        cameraTarget = new Vector2(targetX, targetY);

        if (camera != null)
        {
            var target = new Vector3(targetX, targetY, 0f);
            camera.Position = target + new Vector3(0f, 0f, 32f);
            camera.LookAt(target, Vector3.Up);
        }
    }

    private void UpdateBoardMeshes()
    {
        if (board == null)
        {
            return;
        }

        for (int x = 0; x < TetrisBoard.Width; x++)
        {
            for (int y = 0; y < TetrisBoard.Height; y++)
            {
                MeshInstance3D block = settledBlocks[x, y];
                int type = board.Cells[x, y];
                block.Visible = type != 0;
                if (type == 0)
                {
                    continue;
                }

                block.Position = CellCenter(x, y, 0.08f);
                block.MaterialOverride = pieceMaterials[type];
            }
        }

        UpdatePieceMeshes(board.CurrentShape, board.CurrentPosition, board.CurrentType, activeBlocks, null, 0.18f);
        if (board.CurrentShape != null)
        {
            UpdatePieceMeshes(board.CurrentShape, board.GetGhostPosition(), board.CurrentType, ghostBlocks, ghostMaterial, 0.18f);
        }
        else
        {
            HideMeshes(ghostBlocks);
        }

        UpdatePreviewMeshes();
        UpdateLandingPulse();
    }

    private void UpdatePieceMeshes(int[,] shape, Vector2I origin, int type, MeshInstance3D[] meshes, Material material, float z)
    {
        int index = 0;
        if (shape != null)
        {
            for (int y = 0; y < shape.GetLength(0); y++)
            {
                for (int x = 0; x < shape.GetLength(1); x++)
                {
                    int boardY = origin.Y + y;
                    if (shape[y, x] == 0 || boardY < 0 || index >= meshes.Length)
                    {
                        continue;
                    }

                    MeshInstance3D block = meshes[index++];
                    block.Visible = true;
                    block.Position = CellCenter(origin.X + x, boardY, z);
                    block.MaterialOverride = material ?? pieceMaterials[type];
                }
            }
        }

        for (; index < meshes.Length; index++)
        {
            meshes[index].Visible = false;
        }
    }

    private void UpdatePreviewMeshes()
    {
        if (board == null)
        {
            HideMeshes(nextBlocks);
            return;
        }

        int[,] shape = TetrisBoard.Shapes[board.NextType];
        Vector2 viewport = GetViewport().GetVisibleRect().Size;
        float pixelsPerWorldUnit = viewport.Y / ViewHeight;
        float previewCell = Math.Min(20f, CellSize * 0.72f) / pixelsPerWorldUnit;
        float screenStartX = BoardOffset.X + TetrisBoard.Width * CellSize + HudGapPixels;
        float screenStartY = BoardOffset.Y + 274f;
        Vector2 topLeft = ScreenToWorld(screenStartX, screenStartY, viewport, pixelsPerWorldUnit);

        int index = 0;
        for (int y = 0; y < shape.GetLength(0); y++)
        {
            for (int x = 0; x < shape.GetLength(1); x++)
            {
                if (shape[y, x] == 0 || index >= nextBlocks.Length)
                {
                    continue;
                }

                MeshInstance3D block = nextBlocks[index++];
                block.Visible = true;
                block.Position = new Vector3(
                    topLeft.X + x * previewCell + previewCell / 2f,
                    topLeft.Y - y * previewCell - previewCell / 2f,
                    0.1f);
                block.Scale = new Vector3(previewCell * 0.82f, previewCell * 0.82f, 0.62f);
                block.MaterialOverride = pieceMaterials[board.NextType];
            }
        }

        for (; index < nextBlocks.Length; index++)
        {
            nextBlocks[index].Visible = false;
            nextBlocks[index].Scale = Vector3.One;
        }
    }

    private void UpdateLandingPulse()
    {
        landingPulse.Visible = landingEffectTimer > 0f;
        if (!landingPulse.Visible)
        {
            return;
        }

        float progress = 1f - landingEffectTimer / 0.22f;
        landingPulse.Scale = new Vector3(0.75f + progress * 0.35f, 1f, 1f);
        landingMaterial.AlbedoColor = new Color(0.55f, 0.9f, 1f, (1f - progress) * 0.4f);
    }

    private Vector3 CellCenter(int x, int y, float z)
    {
        return new Vector3(x - BoardWorldWidth / 2f + 0.5f, BoardWorldHeight / 2f - y - 0.5f, z);
    }

    private Vector2 ScreenToWorld(float x, float y, Vector2 viewport, float pixelsPerWorldUnit)
    {
        return new Vector2(
            cameraTarget.X + (x - viewport.X / 2f) / pixelsPerWorldUnit,
            cameraTarget.Y + (viewport.Y / 2f - y) / pixelsPerWorldUnit);
    }

    private static void HideMeshes(MeshInstance3D[] meshes)
    {
        foreach (MeshInstance3D mesh in meshes)
        {
            mesh.Visible = false;
        }
    }
}
