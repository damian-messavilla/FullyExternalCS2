using CS2Cheat.Data.Game;
using CS2Cheat.Graphics;
using CS2Cheat.Utils;
using SharpDX;
using Color = SharpDX.Color;

namespace CS2Cheat.Features;

public class SpectatorList(GameProcess gameProcess, GameData gameData) : ThreadedServiceBase
{
    private static readonly List<string> _spectators = [];
    private static readonly object _spectatorsLock = new();

    protected override string ThreadName => nameof(SpectatorList);

    protected override void FrameAction()
    {
        if (!gameProcess.IsValid || gameData.Player == null) return;

        var localController = gameProcess.ModuleClient.Read<IntPtr>(Offsets.dwLocalPlayerController);
        if (localController == IntPtr.Zero) return;

        var localPawnHandle = gameProcess.Process.Read<int>(localController + Offsets.m_hPawn);
        var localTeam = gameData.Player.Team;

        var currentSpectators = new List<string>();

        foreach (var entity in gameData.Entities)
        {
            if (entity.AddressBase == IntPtr.Zero) continue;
            if (entity.Team != localTeam) continue;
            if (entity.IsAlive()) continue; // Spectators are usually dead

            var observerServices = gameProcess.Process.Read<IntPtr>(entity.AddressBase + Offsets.m_pObserverServices);
            if (observerServices == IntPtr.Zero) continue;

            var observerTargetHandle = gameProcess.Process.Read<int>(observerServices + Offsets.m_hObserverTarget);
            
            // Check if the handle matches the local player's pawn handle
            // Handles in CS2 might need masking 0x7FFF to compare indices, but usually direct comparison works for equality if both are handles.
            // However, let's try direct comparison first.
            if (observerTargetHandle == localPawnHandle)
            {
                currentSpectators.Add(entity.Name);
            }
        }

        lock (_spectatorsLock)
        {
            _spectators.Clear();
            _spectators.AddRange(currentSpectators);
        }
    }

    public static void Draw(Graphics.Graphics graphics)
    {
        lock (_spectatorsLock)
        {
            var y = 410; // Start position Y
            DrawTextWithOutline(graphics.FontAzonix64, "Spectators:", 90, y, Color.LimeGreen, Color.Black);
            y += 32;
            if (_spectators.Count == 0) return;

            foreach (var spectator in _spectators)
            {
                DrawTextWithOutline(graphics.FontAzonix64, spectator, 90, y, Color.LightGreen, Color.Black);
                y += 32;
            }
        }
    }

    private static void DrawTextWithOutline(SharpDX.Direct3D9.Font font, string text, int x, int y, Color textColor, Color outlineColor)
    {
        // Reduced from 8 to 4 outline positions (good balance of quality/performance)
        font.DrawText(null, text, x - 1, y, outlineColor);     // Left
        font.DrawText(null, text, x + 1, y, outlineColor);     // Right
        font.DrawText(null, text, x, y - 1, outlineColor);     // Top
        font.DrawText(null, text, x, y + 1, outlineColor);     // Bottom

        // Draw the main text on top
        font.DrawText(null, text, x, y, textColor);
    }
}
