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

    private static Color color
    {
        get
        {
            string input = Config.SkeletonEspColor?.Trim() ?? "";
            if (string.IsNullOrEmpty(input))
                return Color.White;

            try
            {
                // Hex ("0xFFFF0000" or "FF0000FF")
                if (input.StartsWith("0x") || input.All(c => "0123456789ABCDEFabcdef".Contains(c)))
                    return Color.FromBgra(uint.Parse(input.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber));

                // Color names
                switch (input.ToLower())
                {
                    case "red": return Color.Red;
                    case "green": return Color.Green;
                    case "blue": return Color.Blue;
                    case "yellow": return Color.Yellow;
                    case "cyan": return Color.Cyan;
                    case "magenta": return Color.Magenta;
                    case "black": return Color.Black;
                    case "white": return Color.White;
                    case "orange": return new Color(255, 165, 0);
                    case "purple": return new Color(128, 0, 128);
                    case "pink": return new Color(255, 192, 203);
                    case "gray":
                    case "grey": return new Color(128, 128, 128);
                    default: return Color.White;
                }
            }
            catch
            {
                return Color.White;
            }
        }
    }

    public static void Draw(Graphics.Graphics graphics)
    {
        var player = graphics.GameData.Player;
        foreach (var entity in graphics.GameData.Entities)
        {
            if (!IsValidEntity(entity, player, graphics)) continue;

            DrawSkeleton(graphics, entity, color);
        }
    }

    private static bool IsValidEntity(Entity entity, Player player, Graphics.Graphics graphics)
    {
        if (Config.TeamCheck && entity.Team == player.Team) return false;
        if (Config.RangeCheck && !IsPlayerInRange(entity, player, Config.RangeForRangeCheck)) return false;
        if (Config.VisibleOnly && !entity.IsVisible) return false;

        return entity.IsAlive() && entity.AddressBase != player.AddressBase;
    }


    private static void DrawSkeleton(Graphics.Graphics graphics, Entity entity, Color color)
    {
        var bonePositions = entity.BonePos;
        if (bonePositions == null) return;

        //Head Circle
        if (bonePositions.ContainsKey("head") && Config.HeadCircleESP)
        {
            var headPos = bonePositions["head"];

            float radius = 5f;

            graphics.DrawCircleWorld(color, headPos, 5f); //radius is 5f
        }

        //Drad bone connections
        foreach (var (startBone, endBone) in BoneConnections)
        {
            if (!bonePositions.ContainsKey(startBone) || !bonePositions.ContainsKey(endBone))
                continue;

            graphics.DrawLineWorld(color, bonePositions[startBone], bonePositions[endBone]);
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