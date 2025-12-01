using System.Windows.Threading;
using CS2Cheat.Core.Data;
using CS2Cheat.Data.Game;
using CS2Cheat.Features;
using CS2Cheat.Utils;
using SharpDX;
using SharpDX.Direct3D9;
using static System.Windows.Application;
using Color = SharpDX.Color;
using Font = SharpDX.Direct3D9.Font;
using FontWeight = SharpDX.Direct3D9.FontWeight;

namespace CS2Cheat.Graphics;

public class Graphics : ThreadedServiceBase
{
    private static readonly VertexElement[] VertexElements =
    {
        new(0, 0, DeclarationType.Float4, DeclarationMethod.Default, DeclarationUsage.PositionTransformed, 0),
        new(0, 16, DeclarationType.Color, DeclarationMethod.Default, DeclarationUsage.Color, 0),
        VertexElement.VertexDeclarationEnd
    };

    private readonly object _deviceLock = new();

    private readonly List<Vertex> _vertices = [];

    private Vector2 _currentResolution;
    private Device? _device;
    private bool _isDisposed;

    private VertexDeclaration? _vertexDecl;
    private VertexBuffer? _vertexBuffer;
    private int _vertexBufferSize = 20000; // default size (will auto-expand)

    public Graphics(GameProcess gameProcess, GameData gameData, WindowOverlay windowOverlay)
    {
        WindowOverlay = windowOverlay ?? throw new ArgumentNullException(nameof(windowOverlay));
        GameProcess = gameProcess ?? throw new ArgumentNullException(nameof(gameProcess));
        GameData = gameData ?? throw new ArgumentNullException(nameof(gameData));

        _currentResolution = new Vector2(WindowOverlay.Window.Width, WindowOverlay.Window.Height);
        InitializeDevice();
    }

    protected override string ThreadName => nameof(Graphics);

    private WindowOverlay WindowOverlay { get; }
    public GameProcess GameProcess { get; }
    public GameData GameData { get; }
    public Font? FontAzonix64 { get; private set; }
    public Font? FontConsolas32 { get; private set; }
    public Font? Undefeated { get; private set; }

    public override void Dispose()
    {
        if (_isDisposed) return;

        base.Dispose();

        lock (_deviceLock)
        {
            DisposeResources();
            _isDisposed = true;
        }
    }

    private void InitializeDevice()
    {
        var parameters = CreatePresentParameters();
        _device = new Device(new Direct3D(), 0, DeviceType.Hardware, WindowOverlay.Window.Handle,
            CreateFlags.HardwareVertexProcessing, parameters);

        // Create persistent structures
        _vertexDecl = new VertexDeclaration(_device, VertexElements);
        _vertexBuffer = new VertexBuffer(_device, _vertexBufferSize * 20, Usage.WriteOnly,
            VertexFormat.None, Pool.Managed);

        InitializeFonts();
    }

    private PresentParameters CreatePresentParameters()
    {
        return new PresentParameters
        {
            Windowed = true,
            SwapEffect = SwapEffect.Discard,
            DeviceWindowHandle = WindowOverlay.Window.Handle,
            MultiSampleQuality = 0,
            BackBufferFormat = Format.A8R8G8B8,
            BackBufferWidth = WindowOverlay.Window.Width,
            BackBufferHeight = WindowOverlay.Window.Height,
            EnableAutoDepthStencil = true,
            AutoDepthStencilFormat = Format.D16,
            PresentationInterval = PresentInterval.Immediate,
            MultiSampleType = MultisampleType.None
        };
    }

    private void InitializeFonts()
    {
        FontAzonix64 = new Font(_device, CreateFontDescription("Tahoma", 32));
        FontConsolas32 = new Font(_device, CreateFontDescription("Verdana", 12));
        Undefeated = new Font(_device, CreateFontDescription("undefeated", 12, FontCharacterSet.Default));
    }

    private static FontDescription CreateFontDescription(string faceName, int height,
        FontCharacterSet characterSet = FontCharacterSet.Ansi)
    {
        return new FontDescription
        {
            Height = height,
            Italic = false,
            CharacterSet = characterSet,
            FaceName = faceName,
            MipLevels = 0,
            OutputPrecision = FontPrecision.TrueType,
            PitchAndFamily = FontPitchAndFamily.Default,
            Quality = FontQuality.ClearType,
            Weight = FontWeight.Regular
        };
    }

    protected override void FrameAction()
    {
        if (!GameProcess.IsValid) return;

        var newResolution = new Vector2(WindowOverlay.Window.Width, WindowOverlay.Window.Height);
        if (!_currentResolution.Equals(newResolution))
        {
            Current.Dispatcher.Invoke(RecreateDevice, DispatcherPriority.Render);
            _currentResolution = newResolution;
        }

        Current.Dispatcher.Invoke(RenderFrame, DispatcherPriority.Normal);
    }

    private void RecreateDevice()
    {
        lock (_deviceLock)
        {
            DisposeResources();
            _vertices.Clear();
            InitializeDevice();
        }
    }

    private void RenderFrame()
    {
        lock (_deviceLock)
        {
            if (_device == null) return;

            ConfigureRenderState();
            _device.Clear(ClearFlags.Target | ClearFlags.ZBuffer, Color.FromAbgr(0), 1, 0);
            _device.BeginScene();

            RenderScene();

            _device.EndScene();
            _device.Present();
        }
    }

    private void ConfigureRenderState()
    {
        if (_device == null) return;

        _device.SetRenderState(RenderState.AlphaBlendEnable, true);
        _device.SetRenderState(RenderState.AlphaTestEnable, false);
        _device.SetRenderState(RenderState.SourceBlend, Blend.SourceAlpha);
        _device.SetRenderState(RenderState.DestinationBlend, Blend.InverseSourceAlpha);
        _device.SetRenderState(RenderState.Lighting, false);
        _device.SetRenderState(RenderState.CullMode, Cull.None);
        _device.SetRenderState(RenderState.ZEnable, true);
        _device.SetRenderState(RenderState.ZFunc, Compare.Always);
    }

    private void RenderScene()
    {
        _vertices.Clear();
        DrawFeatures();
        RenderVertices();
    }

    private void DrawFeatures()
    {
        WindowOverlay.Draw(GameProcess, this);
        var features = ConfigManager.Load();
        if (features.EspAimCrosshair) EspAimCrosshair.Draw(this);
        if (features.Esp) EspBox.Draw(this);
        if (features.SkeletonEsp) SkeletonEsp.Draw(this);
        if (features.BombTimer) BombTimer.Draw(this);
        if (features.SpectatorList) SpectatorList.Draw(this);
    }

    private void EnsureVertexBufferSize(int required)
    {
        if (required <= _vertexBufferSize) return;

        _vertexBufferSize = (int)(required * 1.5f);
        _vertexBuffer?.Dispose();

        _vertexBuffer = new VertexBuffer(_device, _vertexBufferSize * 20, Usage.WriteOnly,
            VertexFormat.None, Pool.Managed);
    }

    private void RenderVertices()
    {
        if (_vertices.Count == 0 || _device == null || _vertexBuffer == null) return;

        EnsureVertexBufferSize(_vertices.Count);

        DataStream stream = _vertexBuffer.Lock(0, _vertices.Count * 20, LockFlags.None);
        stream.WriteRange(_vertices.ToArray());
        _vertexBuffer.Unlock();

        _device.SetStreamSource(0, _vertexBuffer, 0, 20);
        _device.VertexDeclaration = _vertexDecl;

        int primitiveCount = _vertices.Count / 2;
        _device.DrawPrimitives(PrimitiveType.LineList, 0, primitiveCount);
    }

    private void DisposeResources()
    {
        FontAzonix64?.Dispose();
        FontConsolas32?.Dispose();
        Undefeated?.Dispose();
        _vertexBuffer?.Dispose();
        _vertexDecl?.Dispose();
        _device?.Dispose();
    }

    public void DrawLine(Color color, params Vector2[] verts)
    {
        if (verts.Length < 2 || verts.Length % 2 != 0) return;

        foreach (var vertex in verts)
            _vertices.Add(new Vertex
            {
                Color = color,
                Position = new Vector4(vertex.X, vertex.Y, 0.5f, 1.0f)
            });
    }

    public void DrawLineWorld(Color color, params Vector3[] verticesWorld)
    {
        if (GameData.Player == null) return;

        for (int i = 0; i < verticesWorld.Length - 1; i++)
        {
            var s1 = GameData.Player.MatrixViewProjectionViewport.Transform(verticesWorld[i]);
            if (s1.Z >= 1) continue;

            var s2 = GameData.Player.MatrixViewProjectionViewport.Transform(verticesWorld[i + 1]);
            if (s2.Z >= 1) continue;

            DrawLine(color, new Vector2(s1.X, s1.Y), new Vector2(s2.X, s2.Y));
        }
    }

    public void DrawCircleWorld(Color color, Vector3 centerWorld, float radius, int segments = 16)
    {
        if (GameData.Player == null) return;

        // Forward-Vector from Matrix (Camera Direction)
        Matrix m = GameData.Player.MatrixViewProjectionViewport;
        Vector3 forward = new Vector3(-m.M13, -m.M23, -m.M33);
        forward.Normalize();

        // Wrold-Up-Vector (fallback)
        Vector3 worldUp = new(0, 0, 1);

        // If forward fast is parallel -> use other Up-Vector
        if (Math.Abs(Vector3.Dot(worldUp, forward)) > 0.9f)
            worldUp = new Vector3(0, 1, 0);

        // Create an orthogonal coordinate system
        Vector3 right = Vector3.Normalize(Vector3.Cross(worldUp, forward));
        Vector3 up = Vector3.Normalize(Vector3.Cross(forward, right));

        float angleStep = (float)(2 * Math.PI / segments);
        Vector2? lastPoint = null;

        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep;

            Vector3 worldPoint =
                centerWorld +
                right * (float)Math.Cos(angle) * radius +
                up * (float)Math.Sin(angle) * radius;

            var screen = GameData.Player.MatrixViewProjectionViewport.Transform(worldPoint);
            if (screen.Z >= 1) continue;

            Vector2 nextPoint = new(screen.X, screen.Y);

            if (lastPoint != null)
                DrawLine(color, lastPoint.Value, nextPoint);

            lastPoint = nextPoint;
        }
    }

    // Efficient triangle-fan circle fill
    public void DrawFilledCircleWorld(Color color, Vector3 centerWorld, float radius, int segments = 48)
    {
        if (GameData.Player == null) return;

        var center = GameData.Player.MatrixViewProjectionViewport.Transform(centerWorld);
        if (center.Z >= 1) return;

        var center2 = new Vector2(center.X, center.Y);

        Matrix m = GameData.Player.MatrixViewProjectionViewport;
        Vector3 forward = new Vector3(-m.M13, -m.M23, -m.M33);
        forward.Normalize();

        Vector3 up = new(0, 0, 1);
        if (Math.Abs(Vector3.Dot(up, forward)) > 0.9f)
            up = new(0, 1, 0);

        Vector3 right = Vector3.Normalize(Vector3.Cross(up, forward));
        up = Vector3.Normalize(Vector3.Cross(forward, right));

        List<Vector2> pts = new();
        float step = (float)(2 * Math.PI / segments);

        for (int i = 0; i <= segments; i++)
        {
            float a = i * step;

            var worldPoint = centerWorld +
                             right * (float)Math.Cos(a) * radius +
                             up * (float)Math.Sin(a) * radius;

            var p = GameData.Player.MatrixViewProjectionViewport.Transform(worldPoint);
            if (p.Z >= 1) continue;

            pts.Add(new Vector2(p.X, p.Y));
        }

        // Filled: connect center → p[i] → p[i+1]
        for (int i = 0; i < pts.Count - 1; i++)
        {
            DrawLine(color, center2, pts[i]);
            DrawLine(color, pts[i], pts[i + 1]);
        }
    }

    public void DrawRectangle(Color color, Vector2 topLeft, Vector2 bottomRight)
    {
        DrawLine(color, topLeft, new Vector2(bottomRight.X, topLeft.Y));
        DrawLine(color, new Vector2(bottomRight.X, topLeft.Y), bottomRight);
        DrawLine(color, bottomRight, new Vector2(topLeft.X, bottomRight.Y));
        DrawLine(color, new Vector2(topLeft.X, bottomRight.Y), topLeft);
    }
}