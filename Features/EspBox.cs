using CS2Cheat.Core.Data;
using CS2Cheat.Data.Entity;
using CS2Cheat.Graphics;
using CS2Cheat.Utils;
using SharpDX;
using SharpDX.Direct3D9;
using Color = SharpDX.Color;
using Direct3D9Font = SharpDX.Direct3D9.Font;

namespace CS2Cheat.Features;

public static class EspBox
{
    private const int OutlineThickness = 1;

    private static readonly Dictionary<string, string> GunIcons = new()
    {
        ["knife_ct"] = "]",
        ["knife_t"] = "[",
        ["deagle"] = "A",
        ["elite"] = "B",
        ["fiveseven"] = "C",
        ["glock"] = "D",
        ["revolver"] = "J",
        ["hkp2000"] = "E",
        ["p250"] = "F",
        ["usp_silencer"] = "G",
        ["tec9"] = "H",
        ["cz75a"] = "I",
        ["mac10"] = "K",
        ["ump45"] = "L",
        ["bizon"] = "M",
        ["mp7"] = "N",
        ["mp9"] = "R",
        ["p90"] = "O",
        ["galilar"] = "Q",
        ["famas"] = "R",
        ["m4a1_silencer"] = "T",
        ["m4a1"] = "S",
        ["aug"] = "U",
        ["sg556"] = "V",
        ["ak47"] = "W",
        ["g3sg1"] = "X",
        ["scar20"] = "Y",
        ["awp"] = "Z",
        ["ssg08"] = "a",
        ["xm1014"] = "b",
        ["sawedoff"] = "c",
        ["mag7"] = "d",
        ["nova"] = "e",
        ["negev"] = "f",
        ["m249"] = "g",
        ["taser"] = "h",
        ["flashbang"] = "i",
        ["hegrenade"] = "j",
        ["smokegrenade"] = "k",
        ["molotov"] = "l",
        ["decoy"] = "m",
        ["incgrenade"] = "n",
        ["c4"] = "o"
    };

    private static ConfigManager? _config;
    private static ConfigManager Config => _config ??= ConfigManager.Load();

    private static Color expBoxColor
    {
        get
        {
            string input = Config.EspBoxColor?.Trim() ?? "";
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
        if (player == null || graphics.GameData.Entities == null) return;

        foreach (var entity in graphics.GameData.Entities)
        {
            if (!entity.IsAlive() || entity.AddressBase == player.AddressBase) continue;
            if (Config.TeamCheck && entity.Team == player.Team) continue;

            var boundingBox = GetEntityBoundingBox(graphics, entity);
            if (boundingBox == null) continue;

            DrawEntityInfo(graphics, entity, expBoxColor, boundingBox.Value);
        }
    }

    private static void DrawEntityInfo(Graphics.Graphics graphics, Entity entity, Color color,
        (Vector2, Vector2) boundingBox)
    {
        var (topLeft, bottomRight) = boundingBox;
        if (topLeft.X > bottomRight.X || topLeft.Y > bottomRight.Y) return;

        var healthPercentage = Math.Clamp(entity.Health / 100f, 0f, 1f);

        if (_config.DrawEspBox)
        {
            graphics.DrawRectangle(color, topLeft, bottomRight);
        }

        // Health bar - dynamic width based on bounding box height
        var healthBarWidth = 1f; // Fixed width for the bar
        var healthBarLeft = topLeft.X - 3f - OutlineThickness;
        var healthBarTopLeft = new Vector2(healthBarLeft, topLeft.Y);
        var healthBarBottomRight = new Vector2(healthBarLeft + healthBarWidth, bottomRight.Y);
        DrawHealthBar(graphics, healthBarTopLeft, healthBarBottomRight, healthPercentage);

        // Health number - moved to left of health bar
        var healthText = entity.Health.ToString();
        var textSize = graphics.FontConsolas32.MeasureText(null, healthText, FontDrawFlags.Center);
        var healthX = (int)(healthBarLeft - textSize.Right - 10f); // Position left of health bar with 10px padding
        var healthY = (int)(topLeft.Y);
        DrawTextWithOutline(graphics.FontConsolas32, healthText, healthX, healthY, Color.LimeGreen, Color.Black);

        // Weapon
        var weaponIcon = GetWeaponIcon(entity.CurrentWeaponName);
        if (!string.IsNullOrEmpty(weaponIcon))
        {
            var weaponTextSize = graphics.Undefeated.MeasureText(null, weaponIcon, FontDrawFlags.Center);
            var weaponX = (int)((topLeft.X + bottomRight.X - weaponTextSize.Right) / 2);
            var weaponY = (int)(bottomRight.Y + 2.5f);
            graphics.Undefeated.DrawText(null, weaponIcon, weaponX, weaponY, Color.White);
        }

        // Enemy name
        if (!Config.TeamCheck || graphics.GameData.Player.Team != entity.Team)
        {
            var name = entity.Name ?? "UNKNOWN";
            var nameTextWidth = graphics.FontConsolas32.MeasureText(null, name, FontDrawFlags.Center).Right + 10f;
            var nameX = (int)((topLeft.X + bottomRight.X) / 2 - nameTextWidth / 2);
            var nameY = (int)(topLeft.Y - 15f);
            DrawTextWithOutline(graphics.FontConsolas32, name, nameX, nameY, Color.LimeGreen, Color.Black);
        }


        // Status flags
        var flagX = (int)(bottomRight.X + 5f);
        var flagY = (int)topLeft.Y;
        var spacing = 15;

        if (entity.IsInScope == 1)
            graphics.FontConsolas32.DrawText(default, "Scoped", flagX, flagY, Color.White);

        if (entity.FlashAlpha > 7)
            graphics.FontConsolas32.DrawText(default, "Flashed", flagX, flagY + spacing, Color.White);

        if (entity.IsInScope == 256)
            graphics.FontConsolas32.DrawText(default, "Shifting", flagX, flagY + spacing * 2, Color.White);
        else if (entity.IsInScope == 257)
            graphics.FontConsolas32.DrawText(default, "Shifting in scope", flagX, flagY + spacing * 3, Color.White);
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

    private static void DrawHealthBar(Graphics.Graphics graphics, Vector2 topLeft, Vector2 bottomRight,
        float healthPercentage)
    {
        var barHeight = bottomRight.Y - topLeft.Y;
        var filledHeight = barHeight * healthPercentage;

        // Calculate the filled portion (from bottom to top)
        var filledTop = new Vector2(topLeft.X, bottomRight.Y - filledHeight);
        var filledBottom = bottomRight;

        Color healthColor = CalculateHealthColor(healthPercentage);

        // Draw health portion
        if (filledHeight > 0)
        {
            graphics.DrawRectangle(healthColor, filledTop, filledBottom);
        }

        // Draw outline
        graphics.DrawRectangle(Color.Black,
            new Vector2(topLeft.X - OutlineThickness, topLeft.Y - OutlineThickness),
            new Vector2(bottomRight.X + OutlineThickness, bottomRight.Y + OutlineThickness));
    }

    private static Color CalculateHealthColor(float healthPercentage)
    {
        // This creates a smooth transition from:
        // - Lime green (0, 255, 0) at 100% health
        // - Yellow (255, 255, 0) at 50% health  
        // - Red (255, 0, 0) at 0% health

        int r, g;

        if (healthPercentage > 0.5f)
        {
            // From 100% to 50%: Green to Yellow
            // healthPercentage goes from 1.0 to 0.5, we map to 0.0 to 1.0
            float factor = (healthPercentage - 0.5f) * 2f;
            r = (int)(255 * (1f - factor));
            g = 255;
        }
        else
        {
            // From 50% to 0%: Yellow to Red
            // healthPercentage goes from 0.5 to 0.0, we map to 1.0 to 0.0
            float factor = healthPercentage * 2f;
            r = 255;
            g = (int)(255 * factor);
        }

        return new Color(r, g, 0, 255);
    }

    private static string GetWeaponIcon(string weapon)
    {
        return GunIcons.TryGetValue(weapon?.ToLower() ?? string.Empty, out var icon) ? icon : string.Empty;
    }

    private static (Vector2, Vector2)? GetEntityBoundingBox(Graphics.Graphics graphics, Entity entity)
    {
        const float padding = 5.0f;
        var minPos = new Vector2(float.MaxValue, float.MaxValue);
        var maxPos = new Vector2(float.MinValue, float.MinValue);

        var matrix = graphics.GameData.Player?.MatrixViewProjectionViewport;
        if (matrix == null || entity.BonePos == null || entity.BonePos.Count == 0) return null;

        var anyValid = false;
        foreach (var bone in entity.BonePos.Values)
        {
            var transformed = matrix.Value.Transform(bone);
            if (transformed.Z >= 1 || transformed.X < 0 || transformed.Y < 0) continue;

            anyValid = true;
            minPos.X = Math.Min(minPos.X, transformed.X);
            minPos.Y = Math.Min(minPos.Y, transformed.Y);
            maxPos.X = Math.Max(maxPos.X, transformed.X);
            maxPos.Y = Math.Max(maxPos.Y, transformed.Y);
        }

        if (!anyValid) return null;

        var sizeMultiplier = 2f - entity.Health / 100f;
        var paddingVector = new Vector2(padding * sizeMultiplier);
        return (minPos - paddingVector, maxPos + paddingVector);
    }
}