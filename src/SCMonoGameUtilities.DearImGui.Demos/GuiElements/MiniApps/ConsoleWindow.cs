using ImGuiNET;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using static ImGuiNET.ImGui;

namespace SCMonoGameUtilities.DearImGui.Demos.GuiElements.MiniApps;

// This example implements a console with basic coloring
// A more elaborate implementation may want to store entries along with extra data such as timestamp and emitter,
// and or offer auto-completion (e.g. with tab) and history (e.g. with up/down keys)
class ConsoleWindow
{
    public bool IsOpen;

    private readonly Dictionary<string, Action> windowCommandActionsByCommandName;
    private readonly ExampleConsole console;
    private readonly RingBuffer<string> outputBuffer = new(100);
    private readonly unsafe ImGuiTextFilterPtr filter = new(ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null));

    private bool autoScroll = true;

    public ConsoleWindow(ExampleConsole console, bool isOpen = false)
    {
        this.console = console;
        this.IsOpen = isOpen;

        windowCommandActionsByCommandName = new(StringComparer.InvariantCultureIgnoreCase)
        {
            ["clear"] = outputBuffer.Clear,
            ["cls"] = outputBuffer.Clear,
            ["close"] = () => IsOpen = false,
            // could also have commands to e.g. resize the buffer, set auto-scroll on or off,
            // set the filter, copy to clipboard (copyToClipboard would need to be added as
            // an instance field), etc. Though of course to support commands with parameters,
            // would need to add a second parameter (the command parameters) to the action, and to
            // tweak the look up against this dictionary so that it only looks for the first word
            // of the submitted command.
        };
    }

    ~ConsoleWindow() => filter.Destroy();

    public void Update()
    {
        // Try to keep our content up to date even when we're not open - else we might have a lot 
        // of catching up to do once we're open again.
        // In a real app, perhaps consider a maximum number of messages to consume per update
        // step. Just in case something goes wrong in your app to the extent that lots of messages
        // are constantly generated (on some thread other than the main game one) - probably don't
        // want to compound the issue by having the log window try too hard to keep up.
        while (console.TryDequeueOutput(out var output))
        {
            outputBuffer.Add(output);
        }

        if (!IsOpen) return;

        SetNextWindowSize(new Vector2(520, 600), ImGuiCond.FirstUseEver);

        if (Begin("Example: Console", ref IsOpen))
        {
            UpdateOutputPane();
            UpdateInputTextBox();
        }

        End();
    }

    private void UpdateOutputPane()
    {
        float footerHeightToReserve = GetStyle().ItemSpacing.Y + GetFrameHeightWithSpacing();
        BeginChild("ScrollingRegion", new(0, -footerHeightToReserve), ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);

        bool copyToClipboard = false;
        if (BeginPopupContextWindow())
        {
            MenuItem("Auto-scroll", null, ref autoScroll);
            if (Selectable("Clear")) outputBuffer.Clear();
            if (Selectable("Copy to clipboard")) copyToClipboard = true;

            Separator();
            filter.Draw();

            EndPopup();
        }

        if (copyToClipboard)
        {
            LogToClipboard();
        }

        PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 1));
        var toDisplay = filter.IsActive() ? outputBuffer.Where(a => filter.PassFilter(a)) : outputBuffer;
        foreach (string str in toDisplay)
        {
            Vector4 color;
            bool has_color = false;
            if (str.Contains("[error]", StringComparison.CurrentCulture))
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
        string input = string.Empty; // Yeah, this InputText method parameter should really be out, not ref..
        bool reclaim_focus = false;
        bool consoleWasBusy = console.IsBusyProcessingACommand;

        PushItemWidth(-float.Epsilon);
        if (consoleWasBusy) BeginDisabled();
        if (InputText("##Input", ref input, 250, ImGuiInputTextFlags.EnterReturnsTrue /*| ImGuiInputTextFlags.CallbackCompletion | ImGuiInputTextFlags.CallbackHistory*/) && !consoleWasBusy)
        {
            if (windowCommandActionsByCommandName.TryGetValue(input, out var windowCommandAction))
            {
                windowCommandAction.Invoke();
            }
            else
            {
                console.SubmitCommand(input);
            }

            reclaim_focus = true;
        }
        if (consoleWasBusy) EndDisabled();
        PopItemWidth();

        SetItemDefaultFocus();
        if (reclaim_focus)
        {
            SetKeyboardFocusHere(-1);
        }
    }
}

// Console class, separate from the window for separation of concerns. While a real app could probably
// use ConsoleWindow more or less unchanged, its equivalent of the code found in this class would probably
// look very different, depending on exactly what kind of console behaviour is desired.
//
// Note that we assume here that console commands can take a non-trivial amount of time to process.
// Long enough that we don't want to wait for them within an update step.  So, execution gets pushed onto
// a background thread via .NET's Task API. We do still limit ourselves to executing one command at a time -
// else output could easily end up getting confusingly interwoven.
//
// Note that, while its common for consoles to log messages from around the application, we don't do that here.
// It would however be easy enough to add in - see the LogWindow example (or rather, the ExampleLogWindowContentSource
// class in there) for code that could be merged in here without much bother.
//
// todo: given the shtick about long-running commands, should probably support cancellation (CTRL+C in the window?)..
class ExampleConsole
{
    private readonly Dictionary<string, Action<string[]>> commandActionsByCommandName;
    private readonly ConcurrentQueue<string> outputQueue = new();
    private readonly RingBuffer<string> commandHistory = new(10);
    private Task currentCommandProcessing;

    public ExampleConsole()
    {
        commandActionsByCommandName = new(StringComparer.InvariantCultureIgnoreCase)
        {
            ["help"] = prms => ProcessHelpCommand(),
            ["history"] = prms => ProcessHistoryCommand(),
            ["error"] = prms => ProcessErrorCommand(),
            ["wait"] = ProcessWaitCommand,
        };
    }

    public bool IsBusyProcessingACommand => currentCommandProcessing != null;

    public bool TryDequeueOutput(out string output) => outputQueue.TryDequeue(out output);

    public void SubmitCommand(string command)
    {
        if (command.Split(' ', StringSplitOptions.RemoveEmptyEntries) is not [var commandName, .. var parameters])
        {
            return;
        }

        if (IsBusyProcessingACommand)
        {
            outputQueue.Enqueue("[error] Console is already processing a command");
            return;
        }

        commandHistory.Add(command);
        outputQueue.Enqueue("# " + command);

        if (!commandActionsByCommandName.TryGetValue(commandName, out var commandAction))
        {
            outputQueue.Enqueue($"[error] Unknown command '{commandName}'. Enter 'help' for assistance.");
            return;
        }

        currentCommandProcessing = Task
            .Run(() => commandAction.Invoke(parameters))
            .ContinueWith(t => currentCommandProcessing = null);
    }

    private void ProcessHelpCommand()
    {
        StringBuilder outputBuilder = new();
        outputBuilder.AppendLine("Commands:");
        outputBuilder.Append(string.Join('\n', commandActionsByCommandName.Keys.Select(a => $"- {a}")));

        outputQueue.Enqueue(outputBuilder.ToString());
    }

    private void ProcessHistoryCommand()
    {
        int history_pos = 0;
        foreach (string item in commandHistory)
        {
            outputQueue.Enqueue($"{history_pos}: {item}");
            history_pos++;
        }
    }

    private void ProcessErrorCommand()
    {
        outputQueue.Enqueue("[error] something went wrong");
    }

    private void ProcessWaitCommand(string[] parameters)
    {
        if (parameters is not [var durationString] || !int.TryParse(durationString, out var durationSeconds))
        {
            outputQueue.Enqueue("[error] wait command requires one integer-valued parameter - wait time in seconds");
            return;
        }

        for (int i = 0; i < durationSeconds; i++)
        {
            outputQueue.Enqueue($"waiting for {durationSeconds - i} seconds..");
            Task.Delay(1000).Wait();
        }

        outputQueue.Enqueue("waiting done.");
    }
}
