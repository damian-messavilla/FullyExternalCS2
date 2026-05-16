using System.Numerics;
using CS2Cheat.Core.Data;
using CS2Cheat.Data.Game;
using CS2Cheat.Graphics;
using ImGuiNET;
using CS2Cheat.Utils;

namespace CS2Cheat.Features;

public static class SniperCrosshair
{
    private static readonly HashSet<short> SniperIndexes = new()
    {
        (short)WeaponIndexes.Awp,
        (short)WeaponIndexes.Ssg08,
        (short)WeaponIndexes.Scar20,
        (short)WeaponIndexes.G3Sg1
    };

    public static void Draw(ImDrawListPtr drawList, GameData gameData, GameProcess gameProcess)
    {
        var player = gameData.Player;
        if (player == null || !player.IsAlive() || gameProcess.Process == null)
            return;

        var pawn = player.AddressBase;
        if (pawn == IntPtr.Zero) return;

        // Read IsScoped directly as int (CS2 stores bools as 4-byte values)
        var isScoped = gameProcess.Process.Read<int>(pawn + Offsets.m_bIsScoped) != 0;
        if (isScoped)
            return;

        // Read weapon via WeaponServices → ActiveWeapon
        var weaponServices = gameProcess.Process.Read<IntPtr>(pawn + Offsets.m_pWeaponServices);
        if (weaponServices == IntPtr.Zero) return;

        var weaponHandle = gameProcess.Process.Read<int>(weaponServices + Offsets.m_hActiveWeapon);
        if (weaponHandle <= 0 || gameProcess.ModuleClient == null) return;

        var entityList = gameProcess.ModuleClient.Read<IntPtr>(Offsets.dwEntityList);
        if (entityList == IntPtr.Zero) return;

        var entry = gameProcess.Process.Read<IntPtr>(entityList + 0x8 * ((weaponHandle & 0x7FFF) >> 9) + 16);
        if (entry == IntPtr.Zero) return;

        var weaponEntity = gameProcess.Process.Read<IntPtr>(entry + 112 * (weaponHandle & 0x1FF));
        if (weaponEntity == IntPtr.Zero) return;

        var wpnIdx = gameProcess.Process.Read<short>(
            weaponEntity + Offsets.m_AttributeManager + Offsets.m_Item + Offsets.m_iItemDefinitionIndex);

        if (!SniperIndexes.Contains(wpnIdx))
            return;

        // Draw crosshair at screen center
        var io = ImGui.GetIO();
        var center = new Vector2(io.DisplaySize.X / 2f, io.DisplaySize.Y / 2f);
        drawList.AddCircleFilled(center, 3f, OverlayRenderer.Colors.Black);
        drawList.AddCircleFilled(center, 2f, OverlayRenderer.Colors.WhiteSmoke);
    }
}

