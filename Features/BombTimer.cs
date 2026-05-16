using System.Numerics;
using CS2Cheat.Data.Game;
using CS2Cheat.Graphics;
using CS2Cheat.Utils;
using ImGuiNET;

namespace CS2Cheat.Features;

internal class BombTimer : ThreadedServiceBase
{
    private readonly GameProcess _gameProcess;

    private static string _bombPlanted = string.Empty;
    private static string _bombSite = string.Empty;
    private static bool _isBombPlanted;
    private static float _defuseLeft;
    private static float _timeLeft;
    private static float _defuseCountDown;
    private static float _c4Blow;
    private static bool _beingDefused;
    private static bool _bombDefused;
    private float _currentTime;
    private IntPtr _globalVars;
    private IntPtr _plantedC4;
    private IntPtr _tempC4;

    public BombTimer(GameProcess gameProcess)
    {
        _gameProcess = gameProcess;
    }

    protected override void FrameAction()
    {
        if (!_gameProcess.IsValid || _gameProcess.ModuleClient == null || _gameProcess.Process == null) return;

        _globalVars = _gameProcess.ModuleClient.Read<IntPtr>(Offsets.dwGlobalVars);
        _currentTime = _gameProcess.Process.Read<float>(_globalVars + 0x30);

        _tempC4 = _gameProcess.ModuleClient.Read<IntPtr>(Offsets.dwPlantedC4);
        _plantedC4 = _gameProcess.Process.Read<IntPtr>(_tempC4);
        _isBombPlanted = _gameProcess.ModuleClient.Read<bool>(Offsets.dwPlantedC4 - 0x8);

        if (!_isBombPlanted || _plantedC4 == IntPtr.Zero)
        {
            ResetBombState();
            return;
        }

        _bombDefused = _gameProcess.Process.Read<bool>(_plantedC4 + Offsets.m_bBombDefused);
        if (_bombDefused)
        {
            ResetBombState();
            _bombDefused = true;
            return;
        }

        _defuseCountDown = _gameProcess.Process.Read<float>(_plantedC4 + Offsets.m_flDefuseCountDown);
        _c4Blow = _gameProcess.Process.Read<float>(_plantedC4 + Offsets.m_flC4Blow);
        _beingDefused = _gameProcess.Process.Read<bool>(_plantedC4 + Offsets.m_bBeingDefused);

        _timeLeft = _c4Blow - _currentTime;
        _defuseLeft = _defuseCountDown - _currentTime;

        _timeLeft = Math.Max(_timeLeft, 0);
        _defuseLeft = Math.Max(_defuseLeft, 0);

        if (!_beingDefused)
            _defuseLeft = 0;

        if (_isBombPlanted)
            _bombSite = _gameProcess.Process.Read<int>(_plantedC4 + Offsets.m_nBombSite) == 1 ? "B" : "A";

        _bombPlanted = $"Bomb is planted on site: {_bombSite}";
    }

    private static void ResetBombState()
    {
        _isBombPlanted = false;
        _bombPlanted = string.Empty;
        _bombSite = string.Empty;
        _timeLeft = 0;
        _defuseLeft = 0;
        _defuseCountDown = 0;
        _c4Blow = 0;
        _beingDefused = false;
    }

    public static void Draw(ImDrawListPtr drawList)
    {
        if (!_isBombPlanted) return;

        var timerText = $"Time left: {_timeLeft:0.00} seconds";
        var defuseText = _beingDefused ? $"Defuse time: {_defuseLeft:0.00} seconds" : string.Empty;
        var defusedText = _bombDefused ? "BOMB DEFUSED!" : string.Empty;

        uint color;
        if (_bombDefused)
        {
            color = OverlayRenderer.Colors.LimeGreen;
        }
        else if (_beingDefused)
        {
            if (_defuseLeft < _timeLeft)
            {
                color = OverlayRenderer.Colors.LimeGreen; // Enough time to defuse
            }
            else
            {
                // Not enough time! Blink Red/Yellow
                float time = (float)ImGui.GetTime();
                color = (time % 0.4f < 0.2f) ? OverlayRenderer.Colors.Red : OverlayRenderer.Colors.Yellow;
            }
        }
        else
        {
            color = _timeLeft < 5 ? OverlayRenderer.Colors.Red :
                    _timeLeft < 15 ? OverlayRenderer.Colors.Yellow : OverlayRenderer.Colors.WhiteSmoke;
        }

        var y = 500f;
        var fontSize = ImGui.GetFontSize() * 1.5f;
        var font = ImGui.GetFont();

        DrawOutlinedText(drawList, font, fontSize, new Vector2(10, y), color, _bombPlanted);
        y += fontSize + 4;
        DrawOutlinedText(drawList, font, fontSize, new Vector2(10, y), color, timerText);

        if (!string.IsNullOrEmpty(defuseText))
        {
            y += fontSize + 4;
            DrawOutlinedText(drawList, font, fontSize, new Vector2(10, y), color, defuseText);
        }

        if (!string.IsNullOrEmpty(defusedText))
        {
            y += fontSize + 4;
            DrawOutlinedText(drawList, font, fontSize, new Vector2(10, y), OverlayRenderer.Colors.LimeGreen, defusedText);
        }
    }

    private static void DrawOutlinedText(ImDrawListPtr drawList, ImFontPtr font, float fontSize, Vector2 pos, uint color, string text)
    {
        var black = OverlayRenderer.Colors.Black;
        drawList.AddText(font, fontSize, pos + new Vector2(-1, -1), black, text);
        drawList.AddText(font, fontSize, pos + new Vector2( 1, -1), black, text);
        drawList.AddText(font, fontSize, pos + new Vector2(-1,  1), black, text);
        drawList.AddText(font, fontSize, pos + new Vector2( 1,  1), black, text);
        drawList.AddText(font, fontSize, pos + new Vector2(-1,  0), black, text);
        drawList.AddText(font, fontSize, pos + new Vector2( 1,  0), black, text);
        drawList.AddText(font, fontSize, pos + new Vector2( 0, -1), black, text);
        drawList.AddText(font, fontSize, pos + new Vector2( 0,  1), black, text);
        drawList.AddText(font, fontSize, pos, color, text);
    }
}