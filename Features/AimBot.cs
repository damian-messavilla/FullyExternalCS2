using System.Runtime.InteropServices;
using CS2Cheat.Core;
using CS2Cheat.Core.Data;
using CS2Cheat.Data.Entity;
using CS2Cheat.Data.Game;
using CS2Cheat.Graphics;
using CS2Cheat.Utils;
using Process.NET.Native.Types;
using SharpDX;
using Keys = Process.NET.Native.Types.Keys;
using Point = System.Drawing.Point;

namespace CS2Cheat.Features;

public class AimBot : ThreadedServiceBase
{
    private static double _anglePerPixel;
    private static ConfigManager? _config;
    private static readonly string[] AimBonePriority = { "head", "neck", "chest", "pelvis" };

    private readonly object _stateLock = new();
    private readonly Keys AimBotHotKey;

    // Add the missing fields
    private int _lastMouseX;
    private int _lastMouseY;

    public AimBot(GameProcess gameProcess, GameData gameData)
    {
        GameProcess = gameProcess;
        GameData = gameData;
        MouseHook = new GlobalHook(HookType.WH_MOUSE_LL, MouseHookCallback);
        AimBotHotKey = Config.AimBotKey;
    }

    private static ConfigManager Config => _config ??= ConfigManager.Load();

    private static MouseMoveMethod MouseMoveMethod => MouseMoveMethod.TryMouseMoveNew;

    private bool IsCalibrated { get; set; }
    protected override string ThreadName => nameof(AimBot);
    private GameProcess? GameProcess { get; set; }
    private GameData? GameData { get; set; }
    private GlobalHook? MouseHook { get; set; }
    private AimBotState State { get; set; }

    public override void Dispose()
    {
        base.Dispose();

        if (MouseHook != null)
        {
            MouseHook.Dispose();
            MouseHook = null;
        }

        GameData = null;
        GameProcess = null;
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (MouseMessages)wParam == MouseMessages.WmMouseMove)
        {
            var mouseInput = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            _lastMouseX = mouseInput.Point.X;
            _lastMouseY = mouseInput.Point.Y;
        }

        return nCode < 0 || ProcessMouseMessage((MouseMessages)wParam)
            ? User32.CallNextHookEx(MouseHook != null ? MouseHook.HookHandle : IntPtr.Zero, nCode, wParam, lParam)
            : new IntPtr(1);
    }

    private bool ProcessMouseMessage(MouseMessages mouseMessage)
    {
        if (mouseMessage == MouseMessages.WmLButtonUp)
        {
            lock (_stateLock)
            {
                State = AimBotState.Up;
            }
            return true;
        }

        if (mouseMessage != MouseMessages.WmLButtonDown) return true;

        if (GameProcess == null || !GameProcess.IsValid ||
            GameData == null || GameData.Player == null || !GameData.Player.IsAlive() ||
            TriggerBot.IsHotKeyDown() ||
            GameData.Player.IsGrenade())
            return true;

        lock (_stateLock)
        {
            if (State == AimBotState.Up) State = AimBotState.DownSuppressed;
        }

        return true;
    }

    protected override void FrameAction()
    {
        try
        {
            if (GameProcess == null || !GameProcess.IsValid || GameData?.Player == null ||
                !GameData.Player.IsAlive()) return;

            if (!IsCalibrated)
            {
                Calibrate();
                IsCalibrated = true;
            }

            lock (_stateLock)
            {
                if (State == AimBotState.Up) return;
            }

            var aimPixels = Point.Empty;
            Vector2 aimAngles;
            var aimResult = GetAimTargetWithPrediction(out aimAngles, 30f.DegreeToRadian()); // Large FOV for maximum target acquisition

            if (aimResult)
            {
                if (!float.IsNaN(aimAngles.X) && !float.IsNaN(aimAngles.Y))
                    GetAimPixels(aimAngles, out aimPixels);
            }

            var shouldWait = TryMouseDown();
            if (MouseMoveMethod == MouseMoveMethod.TryMouseMoveOld)
                shouldWait |= TryMouseMoveOld(aimPixels);
            else
                shouldWait |= TryMouseMoveNew(aimPixels);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AimBot ERROR] {ex.Message}\n{ex.StackTrace}");
        }
    }

    private bool GetAimTargetWithPrediction(out Vector2 aimAngles, double customFov)
    {
        var minAngleSize = float.MaxValue;
        aimAngles = new Vector2((float)Math.PI, (float)Math.PI);
        var targetFound = false;

        if (GameData != null && GameData.Entities != null)
        {
            foreach (var entity in GameData.Entities.Where(entity =>
                         GameData.Player != null &&
                         entity.IsAlive() && entity.AddressBase != GameData.Player.AddressBase &&
                         entity.Team != GameData.Player.Team))
            {
                Vector3? bestBonePos = null;
                var bestAngles = Vector2.Zero;
                var bestAngleSize = float.MaxValue;

                foreach (var bone in AimBonePriority)
                {
                    if (!entity.BonePos.TryGetValue(bone, out var bonePos)) continue;

                    // Remove prediction for instant snapping
                    var predictedPos = bonePos;

                    GetAimAngles(predictedPos, out var angleToBoneSize, out var anglesToBone);
                    if (angleToBoneSize > customFov) continue;

                    if (angleToBoneSize < bestAngleSize)
                    {
                        bestAngleSize = angleToBoneSize;
                        bestAngles = anglesToBone;
                        bestBonePos = predictedPos;
                    }
                }

                if (bestBonePos != null && bestAngleSize < minAngleSize)
                {
                    minAngleSize = bestAngleSize;
                    aimAngles = bestAngles; // No smoothing applied
                    targetFound = true;
                }
            }
        }

        return targetFound;
    }

    private void GetAimAngles(Vector3 pointWorld, out float angleSize, out Vector2 aimAngles)
    {
        aimAngles = Vector2.Zero;
        angleSize = 0f;

        if (GameData == null || GameData.Player == null) return;

        var aimDirection = GameData.Player.AimDirection;
        var aimDirectionDesired = (pointWorld - GameData.Player.EyePosition).GetNormalized();

        var horizontalAngle = aimDirectionDesired.GetSignedAngleTo(aimDirection, new Vector3(0, 0, 1));
        var verticalAngle = aimDirectionDesired.GetSignedAngleTo(aimDirection,
            Vector3.Cross(aimDirectionDesired, new Vector3(0, 0, 1)).GetNormalized());

        aimAngles = new Vector2(horizontalAngle, verticalAngle);
        angleSize = aimDirection.GetAngleTo(aimDirectionDesired);
    }

    private static void GetAimPixels(Vector2 aimAngles, out Point aimPixels)
    {
        var fovRatio = 90.0 / Player.Fov;
        aimPixels = new Point(
            (int)Math.Round(aimAngles.X / _anglePerPixel * fovRatio),
            (int)Math.Round(aimAngles.Y / _anglePerPixel * fovRatio)
        );
    }

    private static bool TryMouseMoveOld(Point aimPixels)
    {
        if (aimPixels.X == 0 && aimPixels.Y == 0) return false;
        Utility.MouseMove(aimPixels.X, aimPixels.Y);
        return true;
    }

    private static bool TryMouseMoveNew(Point aimPixels)
    {
        if (aimPixels.X == 0 && aimPixels.Y == 0) return false;
        Utility.MouseMove(aimPixels.X, aimPixels.Y); // Direct movement instead of wind mouse smoothing
        return true;
    }

    private bool TryMouseDown()
    {
        var mouseDown = false;
        lock (_stateLock)
        {
            if (State == AimBotState.DownSuppressed)
            {
                mouseDown = true;
                State = AimBotState.Down;
            }
        }

        if (mouseDown) Utility.MouseLeftDown();
        return mouseDown;
    }

    private void Calibrate()
    {
        _anglePerPixel = new[]
        {
            CalibrationMeasureAnglePerPixel(100),
            CalibrationMeasureAnglePerPixel(-200),
            CalibrationMeasureAnglePerPixel(300),
            CalibrationMeasureAnglePerPixel(-400),
            CalibrationMeasureAnglePerPixel(200)
        }.Average();
    }

    private double CalibrationMeasureAnglePerPixel(int deltaPixels)
    {
        Thread.Sleep(100);

        if (GameData == null || GameData.Player == null) return 0.0;

        var eyeDirectionStart = GameData.Player.EyeDirection;
        eyeDirectionStart.Z = 0;

        Utility.MouseMove(deltaPixels, 0);

        Thread.Sleep(100);

        if (GameData == null || GameData.Player == null) return 0.0;

        var eyeDirectionEnd = GameData.Player.EyeDirection;
        eyeDirectionEnd.Z = 0;

        return eyeDirectionEnd.GetAngleTo(eyeDirectionStart) / Math.Abs(deltaPixels);
    }
}