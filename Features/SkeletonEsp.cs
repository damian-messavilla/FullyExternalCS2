using CS2Cheat.Core.Data;
using CS2Cheat.Data.Entity;
using CS2Cheat.Graphics;
using CS2Cheat.Utils;
using SharpDX;
using Color = SharpDX.Color;

namespace CS2Cheat.Features;

public static class SkeletonEsp
{
    private static readonly (string Start, string End)[] BoneConnections =
    [
        // Spine chain
        ("head", "neck_0"),
        ("neck_0", "spine_1"),
        ("spine_1", "spine_2"),
        ("spine_2", "pelvis"),

        // Left arm chain
        ("spine_1", "arm_upper_L"),
        ("arm_upper_L", "arm_lower_L"),
        ("arm_lower_L", "hand_L"),

        // Right arm chain
        ("spine_1", "arm_upper_R"),
        ("arm_upper_R", "arm_lower_R"),
        ("arm_lower_R", "hand_R"),

        // Left leg chain
        ("pelvis", "leg_upper_L"),
        ("leg_upper_L", "leg_lower_L"),
        ("leg_lower_L", "ankle_L"),

        // Right leg chain
        ("pelvis", "leg_upper_R"),
        ("leg_upper_R", "leg_lower_R"),
        ("leg_lower_R", "ankle_R")
    ];

    private static ConfigManager? _config;
    private static ConfigManager Config => _config ??= ConfigManager.Load();

    // Skeleton color
    private static Color SkeletonColor
    {
        get
        {
            return ConfigManager.ParseColor(Config.SkeletonEspColor);
        }
    }

    // Head circle color
    private static Color HeadColor
    {
        get
        {
            return ConfigManager.ParseColor(Config.HeadCircleColor);
        }
    }

    public static void Draw(Graphics.Graphics graphics)
    {
        var player = graphics.GameData.Player;

        foreach (var entity in graphics.GameData.Entities)
        {
            if (!IsValidEntity(entity, player, graphics))
                continue;

            DrawSkeleton(graphics, entity);
        }
    }

    private static bool IsValidEntity(Entity entity, Player player, Graphics.Graphics graphics)
    {
        if (Config.TeamCheck && entity.Team == player.Team) return false;
        if (Config.RangeCheck && !IsPlayerInRange(entity, player, Config.RangeForRangeCheck)) return false;

        // Updated visibility check
        if (Config.VisibleOnly && !entity.IsVisible) return false;

        return entity.IsAlive() && entity.AddressBase != player.AddressBase;
    }

    private static void DrawSkeleton(Graphics.Graphics graphics, Entity entity)
    {
        var bonePositions = entity.BonePos;
        if (bonePositions == null) return;

        // Draw Head Circle
        if (Config.HeadCircleESP &&
            bonePositions.ContainsKey("head"))
        {
            var headPos = bonePositions["head"];
            float radius = 5f;

            if (Config.HeadCircleFilled)
                graphics.DrawFilledCircleWorld(HeadColor, headPos, radius, 16);
            else
                graphics.DrawCircleWorld(HeadColor, headPos, radius, 16);
        }

        // Draw Skeleton Bones
        foreach (var (startBone, endBone) in BoneConnections)
        {
            if (!bonePositions.ContainsKey(startBone) ||
                !bonePositions.ContainsKey(endBone))
                continue;

            graphics.DrawLineWorld(
                SkeletonColor,
                bonePositions[startBone],
                bonePositions[endBone]
            );
        }
    }

    private static bool IsPlayerInRange(Entity entity, Player player, float maxDistance)
    {
        if (entity.Origin == Vector3.Zero || player.Origin == Vector3.Zero)
            return false;

        float distance = Vector3.Distance(entity.Origin, player.Origin);
        return distance <= maxDistance;
    }
}
