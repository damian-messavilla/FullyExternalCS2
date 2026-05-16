using System.Numerics;
using CS2Cheat.Data.Game;
using CS2Cheat.Graphics;
using CS2Cheat.Utils;
using ImGuiNET;

namespace CS2Cheat.Features;

public static class SpectatorList
{
    private static ConfigManager? _config;
    private static ConfigManager Config => _config ??= ConfigManager.Load();

    public static void Draw(ImDrawListPtr drawList, GameData gameData, GameProcess gameProcess)
    {
        _config = ConfigManager.Load();

        var player = gameData.Player;
        if (player == null || gameData.Entities == null || gameProcess.Process == null)
            return;

        var localPawn = player.AddressBase;
        if (localPawn == IntPtr.Zero) return;

        var isDead = !player.IsAlive();
        var watchedPawn = localPawn;

        if (isDead && player.ObserverTarget != IntPtr.Zero)
        {
            watchedPawn = player.ObserverTarget;
        }

        var spectators = new List<string>();

        foreach (var entity in gameData.Entities)
        {
            if (entity.AddressBase == IntPtr.Zero || entity.AddressBase == localPawn)
                continue;

            if (isDead && entity.AddressBase == watchedPawn)
                continue; // The person we are watching isn't spectating themselves

            if (entity.ObserverTarget == watchedPawn)
            {
                var name = entity.Name;
                if (!string.IsNullOrEmpty(name))
                    spectators.Add(name);
            }
        }

        if (spectators.Count == 0) return;

        var sc = Config.SpectatorListColor;
        var textColor = OverlayRenderer.ToColor(new Vector4(sc[0], sc[1], sc[2], sc[3]));
        var fontSize = Math.Clamp(Config.SpectatorListFontSize, 10f, 40f);
        var lineHeight = fontSize + 4f;

        var screenW = gameProcess.WindowRectangleClient.Width;
        var screenH = gameProcess.WindowRectangleClient.Height;
        var startX = screenW * (Config.SpectatorListPosX / 100f);
        var startY = screenH * (Config.SpectatorListPosY / 100f);

        var font = ImGui.GetFont();

        // Header
        DrawScaledText(drawList, font, fontSize, new Vector2(startX, startY), textColor, $"Spectators ({spectators.Count}):");

        // Spectator names
        for (int i = 0; i < spectators.Count; i++)
        {
            var y = startY + lineHeight * (i + 1);
            DrawScaledText(drawList, font, fontSize, new Vector2(startX + 5f, y), textColor, spectators[i]);
        }
    }

    private static void DrawScaledText(ImDrawListPtr drawList, ImFontPtr font, float fontSize, Vector2 position, uint color, string text)
    {
        var black = OverlayRenderer.Colors.Black;
        // 8-directional outline for sharp, thick border
        drawList.AddText(font, fontSize, position + new Vector2(-1, -1), black, text);
        drawList.AddText(font, fontSize, position + new Vector2( 1, -1), black, text);
        drawList.AddText(font, fontSize, position + new Vector2(-1,  1), black, text);
        drawList.AddText(font, fontSize, position + new Vector2( 1,  1), black, text);
        drawList.AddText(font, fontSize, position + new Vector2(-1,  0), black, text);
        drawList.AddText(font, fontSize, position + new Vector2( 1,  0), black, text);
        drawList.AddText(font, fontSize, position + new Vector2( 0, -1), black, text);
        drawList.AddText(font, fontSize, position + new Vector2( 0,  1), black, text);
        drawList.AddText(font, fontSize, position, color, text);
    }
}
