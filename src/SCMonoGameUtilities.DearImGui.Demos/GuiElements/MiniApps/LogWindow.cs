using ImGuiNET;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using static ImGuiNET.ImGui;

namespace SCMonoGameUtilities.DearImGui.Demos.GuiElements.MiniApps;

class LogWindow(ExampleLogWindowContentSource contentSource, int maxEntryCount, bool isOpen = false)
{
    public bool IsOpen = isOpen;

    // This window is for demo purposes only, and wouldn't feature in a real app:
    private readonly LogGeneratorWindow logGeneratorWindow = new(isOpen: true);

    private readonly ExampleLogWindowContentSource contentSource = contentSource;
    private readonly RingBuffer<string> content = new(maxEntryCount);
    private readonly unsafe ImGuiTextFilterPtr filter = new(ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null));

    private bool autoScroll = true;

    ~LogWindow() => filter.Destroy();

    public void Update()
    {
        // Try to keep our content up to date even when we're not open - else we might have a lot 
        // of catching up to do once we're open again.
        // In a real app, perhaps consider a maximum number of messages to consume per update
        // step. Just in case something goes wrong in your app to the extent that lots of messages
        // are constantly generated (on some thread other than the main game one) - probably don't
        // want to compound the issue by having the log window try too hard to keep up.
        while (contentSource.TryDequeueMessage(out var message))
        {
            content.Add(message);
        }

        if (!IsOpen) return;

        // Again, this window is for demo purposes only, and wouldn't exist in a real app:
        logGeneratorWindow.Update();

        SetNextWindowSize(new Vector2(500, 400), ImGuiCond.FirstUseEver);
        if (Begin("Example: Log", ref IsOpen))
        {
            UpdateContextMenu(out var copyContentToClipboard);
            UpdateContentPane(copyContentToClipboard);
        }

        End();
    }

    private void UpdateContextMenu(out bool copyContentToClipboard)
    {
        copyContentToClipboard = false;
        if (BeginPopupContextWindow())
        {
            MenuItem("Auto-scroll", null, ref autoScroll);
            if (Selectable("Clear")) content.Clear();
            if (Selectable("Copy to clipboard")) copyContentToClipboard = true;

            Separator();
            filter.Draw();

            // Once again, demo purposes only, and wouldn't feature in a real app:
            Separator();
            MenuItem("Show Log Generator Window", null, ref logGeneratorWindow.IsOpen);

            EndPopup();
        }
    }

    private void UpdateContentPane(bool copyContentToClipboard)
    {
        if (copyContentToClipboard)
        {
            LogToClipboard();
        }

        PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
        if (filter.IsActive())
        {
            TextUnformatted($"Entries matching active filter:");

            var filteredContent = content.Where(str => filter.PassFilter(str));

            if (!filteredContent.Any())
            {
                BulletText("No matches!");
            }

            foreach (string str in filteredContent)
            {
                BulletText(str);
            }
        }
        else
        {
            foreach (string str in content)
            {
                TextUnformatted(str);
            }
        }
        PopStyleVar();

        if (autoScroll && GetScrollY() >= GetScrollMaxY())
        {
            SetScrollHereY(1.0f);
        }
    }
}

// Example source of content for a log window, separate from the window itself for separation of concerns.
// While a real app could probably use LogWindow more or less unchanged, its equivalent of the code found in
// this class could look very different, depending on exactly what "logs" are being displayed.
//
// This particular implementation just grabs trace (including debug) messages.
// TODO: Should probably demo capture of (structured?) logging too - e.g. MS Logging ILogger, Serilog LogSink
class ExampleLogWindowContentSource
{
    private readonly ConcurrentQueue<string> messageQueue = new();

    public ExampleLogWindowContentSource()
    {
        // In general, its a bad idea to include such a high impact side-effect in a constructor
        // (and at the very least we should make the type disposable so that it can be unhooked),
        // but its fine for this demo app. For now.. Hmm, I'll probably fix this at some point.
        Trace.Listeners.Add(new QueuingTraceListener(messageQueue));
    }

    public bool TryDequeueMessage(out string message) => messageQueue.TryDequeue(out message);

    // This is obviously an absolutely minimal trace listener. By overriding TraceListener's other methods,
    // a more sophisticated implementation could use a queue of complex objects instead of strings - objects
    // that the log window could apply pretty formatting to when displaying them. We could, for example,
    // store the category separate from the message. And/or we could handle the object-accepting methods
    // in a more sophisticated manner than just calling ToString(), as the default method implementations do.
    private class QueuingTraceListener(ConcurrentQueue<string> messageQueue) : TraceListener
    {
        public override void Write(string message) => messageQueue.Enqueue(message);

        public override void WriteLine(string message) => messageQueue.Enqueue(message);
    }
}

// Window that offers buttons to write Debug and Trace messages, for demo purposes. Obviously wouldn't feature
// in a real app.
//
// NB: There's no dependency on the logging window or content source here - messages are going via dotnet's
// tracing infrastructure.
class LogGeneratorWindow(bool isOpen = false)
{
    public bool IsOpen = isOpen;

    private const string TraceMessageCategory = "Log Source Window - Trace";
    private const string DebugMessageCategory = "Log Source Window - Debug";
    private static readonly string[] randomWords = ["Bumfuzzled", "Cattywampus", "Snickersnee", "Abibliophobia", "Absquatulate"];

    public void Update()
    {
        if (!IsOpen) return;

        if (Begin("Example: Log Generator", ref IsOpen))
        {
            if (Button("Debug line"))
            {
                Debug.WriteLine(MakeMessage());
            }

            if (Button("Debug line with category"))
            {
                Debug.WriteLine(MakeMessage(), DebugMessageCategory);
            }

            if (Button("Trace line"))
            {
                Debug.WriteLine(MakeMessage());
            }

            if (Button("Trace line with category"))
            {
                Trace.WriteLine(MakeMessage(), TraceMessageCategory);
            }

            if (Button("Trace event: info"))
            {
                Trace.TraceInformation(MakeMessage());
            }

            if (Button("Trace event: warning"))
            {
                Trace.TraceWarning(MakeMessage());
            }

            if (Button("Trace event: error"))
            {
                Trace.TraceError(MakeMessage());
            }

            // TODO: Add (structured?) log message
        }

        End();
    }

    private static string MakeMessage()
    {
        return $"Hello, elapsed game time {GetTime():F2}s, here's a word: {randomWords[Random.Shared.Next(randomWords.Length)]}";
    }
}
