using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static ImGuiNET.ImGui;

namespace SCMonoGameUtilities.DearImGui.Demos.GuiElements.MiniApps;

// This example implements a console with basic coloring
// A more elaborate implementation may want to store entries along with extra data such as timestamp and emitter,
// and or offer auto-completion (e.g. with tab) and history (e.g. with up/down keys)
//
// TODO: separation of concerns - separate the console from the window itself. Create ExampleConsole type and inject.
// TODO: max entries and ring buffer, like log window.
class ConsoleWindow(bool isOpen = false)
{
    public bool IsOpen = isOpen;

    private readonly unsafe ImGuiTextFilterPtr filter = new(ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null));
    private readonly List<string> log = [];
    private readonly List<string> history = [];

    private bool autoScroll = true;
    private bool showDebugOptions = false;

    ~ConsoleWindow() => filter.Destroy();

    public void ClearContent() => log.Clear();

    public void AppendContent(string text) => log.Add(text);

    public void Update()
    {
        if (!IsOpen) return;

        SetNextWindowSize(new Vector2(520, 600), ImGuiCond.FirstUseEver);

        if (Begin("Example: Console", ref IsOpen))
        {
            UpdateContextMenu();
            UpdateTopBar(out var copyToClipboard);
            UpdateDebugBar();

            Separator();

            UpdateOutputPane(copyToClipboard);
            UpdateInputTextBox();
        }

        End();
    }

    private void UpdateContextMenu()
    {
        if (BeginPopupContextItem())
        {
            if (MenuItem("Close Console"))
            {
                IsOpen = false;
            }
            EndPopup();
        }
    }

    private void UpdateTopBar(out bool copyToClipboard)
    {
        if (BeginPopup("Options"))
        {
            Checkbox("Auto-scroll", ref autoScroll);
            Checkbox("Show debug options", ref showDebugOptions);
            EndPopup();
        }

        if (Button("Options"))
        {
            OpenPopup("Options");
        }

        SameLine();

        if (Button("Clear"))
        {
            ClearContent();
        }

        SameLine();
        copyToClipboard = Button("Copy");
        SameLine();
        Text("Filter:");
        SameLine();
        filter.Draw("##Filter", -float.Epsilon);
    }

    private void UpdateDebugBar()
    {
        if (!showDebugOptions) return;

        Separator();

        if (Button("Add Debug Text"))
        {
            AppendContent(log.Count + " some text");
            AppendContent("some more text");
            AppendContent("display very important message here!");
        }

        SameLine();

        if (Button("Add Debug Error"))
        {
            AppendContent("[error] something went wrong");
        }
    }

    private void UpdateOutputPane(bool copyToClipboard)
    {
        float footerHeightToReserve = GetStyle().ItemSpacing.Y + GetFrameHeightWithSpacing();
        BeginChild("ScrollingRegion", new(0, -footerHeightToReserve), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);

        if (BeginPopupContextWindow())
        {
            if (Selectable("Clear"))
            {
                ClearContent();
            }
            EndPopup();
        }

        if (copyToClipboard)
        {
            LogToClipboard();
        }

        PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 1));
        var toDisplay = filter.IsActive() ? log.Where(a => filter.PassFilter(a)) : log;
        foreach (string str in toDisplay)
        {
            Vector4 color;
            bool has_color = false;
            if (str.Contains("[error]", System.StringComparison.CurrentCulture))
            {
                color = new Vector4(1.0f, 0.4f, 0.4f, 1.0f);
                PushStyleColor(ImGuiCol.Text, color);
                has_color = true;
            }
            if (str.StartsWith("# "))
            {
                color = new Vector4(1.0f, 0.8f, 0.6f, 1.0f);
                PushStyleColor(ImGuiCol.Text, color);
                has_color = true;
            }
            TextUnformatted(str);
            if (has_color)
            {
                PopStyleColor();
            }
        }

        PopStyleVar();

        if (autoScroll && GetScrollY() >= GetScrollMaxY())
        {
            SetScrollHereY(1.0f);
        }

        EndChild();
    }

    private void UpdateInputTextBox()
    {
        string input_buf = "";
        bool reclaim_focus = false;

        PushItemWidth(-float.Epsilon);
        if (InputText("##Input", ref input_buf, 250, ImGuiInputTextFlags.EnterReturnsTrue /*| ImGuiInputTextFlags.CallbackCompletion | ImGuiInputTextFlags.CallbackHistory*/))
        {
            ExecCommand(input_buf);
            reclaim_focus = true;
        }
        PopItemWidth();

        SetItemDefaultFocus();
        if (reclaim_focus)
        {
            SetKeyboardFocusHere(-1);
        }
    }

    private void ExecCommand(string command)
    {
        // History
        if (history.Count == 10)
        {
            history.RemoveAt(0);
        }
        history.Add(command);

        // Show Input
        AppendContent("# " + command);

        // Commands
        if (command == "CLEAR")
        {
            ClearContent();
        }
        else if (command == "HELP")
        {
            AppendContent("Commands:");
            AppendContent("- CLEAR");
            AppendContent("- HELP");
            AppendContent("- HISTORY");
        }
        else if (command == "HISTORY")
        {
            int history_pos = 0;
            foreach (string item in history)
            {
                AppendContent("  " + history_pos.ToString() + ": " + item);
                history_pos++;
            }
        }
        else
        {
            AppendContent("Unknown command: '" + command + "'. Enter 'HELP' for assistance.");
        }
    }
}
