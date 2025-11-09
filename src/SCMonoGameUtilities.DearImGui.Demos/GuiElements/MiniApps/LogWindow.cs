using ImGuiNET;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static ImGuiNET.ImGui;

namespace SCMonoGameUtilities.DearImGui.Demos.GuiElements.MiniApps;

// TODO: separation of concerns - separate out the log itself and inject it - in a real app log messages are
// *never* going to come from someone clicking a button on the log window, which limits the value of this demo.
// perhaps include max entries to consume per update as an option?
// perhaps allow logging of rich objects (not just strings) - make generic with optional (otherwise just ToString) formatter?
class LogWindow(int maxEntries, bool isOpen = false)
{
    public bool IsOpen = isOpen;

    private readonly unsafe ImGuiTextFilterPtr filter = new(ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null));
    private readonly RingBuffer<string> content = new(maxEntries);

    private bool autoScroll = true;
    private bool showDebugOptions = false;

    ~LogWindow() => filter.Destroy();

    public void ClearContent() => content.Clear();

    public void AppendContent(string text) => content.Add(text);

    public void Update()
    {
        if (!IsOpen) return;

        SetNextWindowSize(new Vector2(500, 400), ImGuiCond.FirstUseEver);

        if (Begin("Example: Log", ref IsOpen))
        {
            UpdateTopBar(out var copyContentToClipboard);
            UpdateDebugBar();
            Separator();
            UpdateContentPane(copyContentToClipboard);
        }

        End();
    }

    private void UpdateTopBar(out bool copyContentToClipboard)
    {
        if (BeginPopup("Options"))
        {
            Checkbox("Auto-scroll", ref autoScroll);
            Checkbox("Show Debug Options", ref showDebugOptions);
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
        copyContentToClipboard = Button("Copy");
        SameLine();
        Text("Filter:");
        SameLine();
        filter.Draw("##Filter", -float.Epsilon);
    }

    private void UpdateDebugBar()
    {
        if (!showDebugOptions) return;

        Separator();

        if (Button("[Debug] Add 5 entries"))
        {
            string[] words = { "Bumfuzzled", "Cattywampus", "Snickersnee", "Abibliophobia", "Absquatulate" };
            foreach (string str in words)
            {
                AppendContent("Frame " + GetFrameCount() + " [info] Hello, current time is " + GetTime() + " here's a word: " + str);
            }
        }
    }

    private void UpdateContentPane(bool copyContentToClipboard)
    {
        BeginChild("content", Vector2.Zero, ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);

        if (copyContentToClipboard)
        {
            LogToClipboard();
        }

        PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
        if (filter.IsActive())
        {
            foreach (string str in content.Where(str => filter.PassFilter(str)))
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

        EndChild();
    }

    /// <summary>
    /// <para>
    /// A basic circular buffer type used for storing the log window's content -
    /// on the assumption that we don't want it to just grow forever.
    /// </para>
    /// <para>
    /// NB: Of course, in a real app this wouldn't necessarily be an inner type like this.
    /// Just wanted to keep all the examples as self-contained as possible.
    /// </para>
    /// </summary>
    /// <typeparam name="T">The type of elements to be stored.</typeparam>
    /// <param name="maxSize">The maximum number of elements that the buffer will store. The oldest elements will be dropped once this is size is reached.</param>
    private class RingBuffer<T>(int maxSize) : IEnumerable<T>
    {
        // We *could* use something that automatically resizes itself (e.g. a List<>), so that we require
        // less memory while at less than capacity. However, on the assumption that we will be at capacity
        // most of the time, there isn't much point, and the code is simpler if we just use an array.
        public readonly T[] content = new T[maxSize];
        private int headIndex = 0;
        private int count = 0;

        public void Add(T item)
        {
            content[(headIndex + count) % content.Length] = item;

            if (count < content.Length)
            {
                count++;
            }
            else
            {
                headIndex++;
                headIndex %= content.Length;
            }
        }

        public void Clear()
        {
            Array.Clear(content); // NB: actually clear the array to avoid leaks when T is a reference type
            headIndex = 0;
            count = 0;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < count; i++)
            {
                yield return content[(headIndex + i) % content.Length];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
