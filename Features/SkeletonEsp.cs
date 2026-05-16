using System.Numerics;
using CS2Cheat.Core.Data;
using CS2Cheat.Data.Entity;
using CS2Cheat.Data.Game;
using CS2Cheat.Graphics;
using CS2Cheat.Utils;
using ImGuiNET;

namespace CS2Cheat.Features;

public static class SkeletonEsp
{
    private static readonly (string Start, string End)[] BoneConnections =
    [
        ("head", "neck_0"),
        ("neck_0", "spine_1"),
        ("spine_1", "spine_2"),
        ("spine_2", "pelvis"),

        ("spine_1", "arm_upper_L"),
        ("arm_upper_L", "arm_lower_L"),
        ("arm_lower_L", "hand_L"),

        ("spine_1", "arm_upper_R"),
        ("arm_upper_R", "arm_lower_R"),
        ("arm_lower_R", "hand_R"),

        ("pelvis", "leg_upper_L"),
        ("leg_upper_L", "leg_lower_L"),
        ("leg_lower_L", "ankle_L"),

        ("pelvis", "leg_upper_R"),
        ("leg_upper_R", "leg_lower_R"),
        ("leg_lower_R", "ankle_R")
    ];

    private static ConfigManager? _config;
    private static ConfigManager Config => _config ??= ConfigManager.Load();

    public static void Draw(ImDrawListPtr drawList, GameData gameData)
    {
        var player = gameData.Player;
        if (player == null || gameData.Entities == null) return;

        _config = ConfigManager.Load();

        var sc = Config.SkeletonEspColor;
        var skeletonColor = OverlayRenderer.ToColor(new Vector4(sc[0], sc[1], sc[2], sc[3]));

        foreach (var entity in gameData.Entities)
        {
            if (!IsValidEntity(entity, player)) continue;
            if (Config.TeamCheck && entity.Team == player.Team) continue;

            DrawSkeleton(drawList, player, entity, skeletonColor);
        }
    }

    private static bool IsValidEntity(Entity entity, Player player)
    {
        return entity.IsAlive() &&
               entity.AddressBase != player.AddressBase;
    }

    private static void DrawSkeleton(ImDrawListPtr drawList, Player player, Entity entity, uint color)
    {
        var bonePositions = entity.BonePos;
        if (bonePositions == null) return;

        var matrix = player.MatrixViewProjectionViewport;

        foreach (var (startBone, endBone) in BoneConnections)
        {
            if (!bonePositions.TryGetValue(startBone, out var startWorld) ||
                !bonePositions.TryGetValue(endBone, out var endWorld))
                continue;

            var startScreen = matrix.Transform(startWorld);
            var endScreen = matrix.Transform(endWorld);

            if (startScreen.Z >= 1 || endScreen.Z >= 1) continue;

            drawList.AddLine(
                new Vector2(startScreen.X, startScreen.Y),
                new Vector2(endScreen.X, endScreen.Y),
                color, 1.5f);
        }

        if (Config.SkeletonHeadCircle)
            DrawHeadCircle(drawList, bonePositions, matrix);
    }

    private static void DrawHeadCircle(ImDrawListPtr drawList,
        IReadOnlyDictionary<string, Vector3> bonePositions,
        Matrix4x4 matrix)
    {
        if (!bonePositions.TryGetValue("head", out var headWorld) ||
            !bonePositions.TryGetValue("neck_0", out var neckWorld))
            return;

        var headScreen = matrix.Transform(headWorld);
        var neckScreen = matrix.Transform(neckWorld);

        if (headScreen.Z >= 1 || neckScreen.Z >= 1) return;

        var headPos = new Vector2(headScreen.X, headScreen.Y);
        var neckPos = new Vector2(neckScreen.X, neckScreen.Y);
        var radius = Vector2.Distance(headPos, neckPos) * 1f; //0.85f
        if (radius < 2f) return;

        var hc = Config.SkeletonHeadCircleColor;
        var headColor = OverlayRenderer.ToColor(new Vector4(hc[0], hc[1], hc[2], hc[3]));

        drawList.AddCircle(headPos, radius, headColor, 24, 1.5f);
    }
}