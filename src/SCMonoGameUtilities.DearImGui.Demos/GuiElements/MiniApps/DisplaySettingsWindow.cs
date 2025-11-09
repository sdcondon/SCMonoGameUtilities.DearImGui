using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using static ImGuiNET.ImGui;

namespace SCMonoGameUtilities.DearImGui.Demos.GuiElements.MiniApps;

class DisplaySettingsWindow
{
    public bool IsOpen;

    private readonly GameWindow window;
    private readonly GraphicsDeviceManager graphicsDeviceManager;
    private readonly DisplayMode[] displayModes;
    private readonly string[] displayModeDescriptions;

    private int displayModeIndex = 0;
    private bool isFullScreen = true;

    public DisplaySettingsWindow(GameWindow window, GraphicsDeviceManager graphicsDeviceManager, bool isOpen = false)
    {
        this.IsOpen = isOpen;

        this.window = window;
        this.graphicsDeviceManager = graphicsDeviceManager;
        this.displayModes = [.. graphicsDeviceManager.GraphicsDevice.Adapter.SupportedDisplayModes];
        this.displayModeDescriptions = [.. displayModes.Select(a => $"{a.Width}x{a.Height}")];
        this.displayModeIndex = Array.IndexOf(displayModes, graphicsDeviceManager.GraphicsDevice.Adapter.CurrentDisplayMode);
    }

    public void Update()
    {
        if (!IsOpen) return;

        SetNextWindowSize(new(300, 220), ImGuiCond.FirstUseEver);
        SetNextWindowPos(new(10, 140), ImGuiCond.FirstUseEver);

        if (!Begin("Example: Display Settings", ref IsOpen, ImGuiWindowFlags.NoCollapse))
        {
            End();
            return;
        }

        if (Combo("Window Size", ref displayModeIndex, displayModeDescriptions, displayModeDescriptions.Length) && displayModeIndex > -1)
        {
            graphicsDeviceManager.PreferredBackBufferWidth = displayModes[displayModeIndex].Width;
            graphicsDeviceManager.PreferredBackBufferHeight = displayModes[displayModeIndex].Height;
            graphicsDeviceManager.PreferredBackBufferFormat = displayModes[displayModeIndex].Format;
            graphicsDeviceManager.ApplyChanges();
        }

        if (Checkbox("Fullscreen", ref isFullScreen))
        {
            graphicsDeviceManager.IsFullScreen = isFullScreen;
            graphicsDeviceManager.ApplyChanges();
        }

        Separator();

        Text($"Window Screen Device Name: {window.ScreenDeviceName}");
        Text($"Window Position X,Y: {window.Position.X},{window.Position.Y}");
        Text($"Window ClientBounds L - R: {window.ClientBounds.Left} - {window.ClientBounds.Right}");
        Text($"Window ClientBounds T - B: {window.ClientBounds.Top} - {window.ClientBounds.Bottom}");
        Text($"GDM Preferred Back Buffer WxH: {graphicsDeviceManager.PreferredBackBufferWidth}x{graphicsDeviceManager.PreferredBackBufferHeight}");
        Text($"GD Adapter Desc: {graphicsDeviceManager.GraphicsDevice.Adapter.Description}");
        Text($"GD Display Mode WxH: {graphicsDeviceManager.GraphicsDevice.DisplayMode.Width}x{graphicsDeviceManager.GraphicsDevice.DisplayMode.Height}");
        Text($"GD Presentation Parameters Back Buffer WxH: {graphicsDeviceManager.GraphicsDevice.PresentationParameters.BackBufferWidth}x{graphicsDeviceManager.GraphicsDevice.PresentationParameters.BackBufferHeight}");
        Text($"GD Viewport X,Y: {graphicsDeviceManager.GraphicsDevice.Viewport.X},{graphicsDeviceManager.GraphicsDevice.Viewport.Y}");
        Text($"GD Viewport WxH: {graphicsDeviceManager.GraphicsDevice.Viewport.Width}x{graphicsDeviceManager.GraphicsDevice.Viewport.Height}");
        Text($"ImGui Window Size X,Y: {GetWindowViewport().Size.X},{GetWindowViewport().Size.Y}");

        End();
    }
}
