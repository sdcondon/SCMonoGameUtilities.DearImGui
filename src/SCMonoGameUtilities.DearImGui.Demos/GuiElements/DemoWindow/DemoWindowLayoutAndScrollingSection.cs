using ImGuiNET;
using static ImGuiNET.ImGui;
using static SCMonoGameUtilities.DearImGui.Demos.GuiElements.GuiElementHelpers;
using Vec2 = System.Numerics.Vector2;
using Vec4 = System.Numerics.Vector4;

namespace SCMonoGameUtilities.DearImGui.Demos.GuiElements.DemoWindow;

class DemoWindowLayoutAndScrollingSection
{
    private readonly ChildWindowsSubsection childWindowsSubsection = new();
    private readonly WidgetsWidthSubsection widgetsWidthSubsection = new();
    private readonly BasicHorizontalLayoutSubsection basicHorizontalLayoutSubsection = new();
    private readonly ScrollingSubsection scrollingSubsection = new();

    public void Update()
    {
        if (!CollapsingHeader("Layout & Scrolling")) return;

        childWindowsSubsection.Update();
        widgetsWidthSubsection.Update();
        basicHorizontalLayoutSubsection.Update();
        UpdateGroupsSubsection();
        UpdateTextBaselineAlignmentSubsection();
        scrollingSubsection.Update();
    }

    private static void UpdateGroupsSubsection()
    {
        if (!TreeNode("Groups")) return;

        HelpMarker("BeginGroup() basically locks the horizontal position for new line. " +
            "EndGroup() bundles the whole group so that you can use \"item\" functions such as " +
            "IsItemHovered()/IsItemActive() or SameLine() etc. on the whole group.");

        BeginGroup();
        {
            BeginGroup();
            Button("AAA");
            SameLine();
            Button("BBB");
            SameLine();
            BeginGroup();
            Button("CCC");
            Button("DDD");
            EndGroup();
            SameLine();
            Button("EEE");
            EndGroup();
            if (IsItemHovered())
            {
                SetTooltip("First group hovered");
            }
        }

        // Capture the group size and create widgets using the same size
        Vec2 size = GetItemRectSize();
        float[] values = [0.5f, 0.20f, 0.80f, 0.60f, 0.25f];
        PlotHistogram("##values", ref values[0], values.Length, 0, null, 0.0f, 1.0f, size);

        Button("ACTION", new Vec2((size.X - GetStyle().ItemSpacing.X) * 0.5f, size.Y));
        SameLine();
        Button("REACTION", new Vec2((size.X - GetStyle().ItemSpacing.X) * 0.5f, size.Y));
        EndGroup();

        // This breaks tree node
        //ImGui.SameLine();
        //ImGui.Button("LEVERAGE\nBUZZWORD", size);
        //ImGui.SameLine();

        TreePop();
    }

    private static void UpdateTextBaselineAlignmentSubsection()
    {
        if (!TreeNode("Text Baseline Alignment")) return;

        BulletText("Text baseline:");
        SameLine();
        HelpMarker("This is testing the vertical alignment that gets applied on text to keep it aligned with widgets. " +
            "Lines only composed of text or \"small\" widgets use less vertical space than lines with framed widgets.");
        Indent();

        Text("KO Blahblah"); SameLine();
        Button("Some framed item"); SameLine();
        HelpMarker("Baseline of button will look misaligned with text..");

        AlignTextToFramePadding();
        Text("OK Blahblah"); SameLine();
        Button("Some framed item"); SameLine();
        HelpMarker("We call AlignTextToFramePadding() to vertically align the text baseline by +FramePadding.y");

        // SmallButton() uses the same vertical padding as Text
        Button("TEST##1"); SameLine();
        Text("TEST"); SameLine();
        SmallButton("TEST##2");

        // If your line starts with text, call AlignTextToFramePadding() to align text to upcoming widgets.
        AlignTextToFramePadding();
        Text("Text aligned to framed item"); SameLine();
        Button("Item##1"); SameLine();
        Text("Item"); SameLine();
        SmallButton("Item##2"); SameLine();
        Button("Item##3");

        Unindent();

        Spacing();

        BulletText("Multi-line text:");
        Indent();
        Text("One\nTwo\nThree"); SameLine();
        Text("Hello\nWorld"); SameLine();
        Text("Banana");

        Text("Banana"); SameLine();
        Text("Hello\nWorld"); SameLine();
        Text("One\nTwo\nThree");

        Button("HOP##1"); SameLine();
        Text("Banana"); SameLine();
        Text("Hello\nWorld"); SameLine();
        Text("Banana");

        Button("HOP##2"); SameLine();
        Text("Hello\nWorld"); SameLine();
        Text("Banana");
        Unindent();

        Spacing();

        BulletText("Misc items:");
        Indent();

        // SmallButton() sets FramePadding to zero. Text baseline is aligned to match baseline of previous Button.
        Button("80x80", new Vec2(80, 80));
        SameLine();
        Button("50x50", new Vec2(50, 50));
        SameLine();
        Button("Button()");
        SameLine();
        SmallButton("SmallButton()");

        // Tree
        float spacing = GetStyle().ItemInnerSpacing.X;
        Button("Button##1");
        SameLine(0.0f, spacing);
        if (TreeNode("Node##1"))
        {
            // Placeholder tree data
            for (int i = 0; i < 6; i++)
                BulletText(string.Format("Item {0}..", i));
            TreePop();
        }

        // Vertically align text node a bit lower so it'll be vertically centered with upcoming widget.
        // Otherwise you can use SmallButton() (smaller fit).
        AlignTextToFramePadding();

        // Common mistake to avoid: if we want to SameLine after TreeNode we need to do it before we add
        // other contents below the node.
        bool node_open = TreeNode("Node##2");
        SameLine(0.0f, spacing); Button("Button##2");
        if (node_open)
        {
            // Placeholder tree data
            for (int i = 0; i < 6; i++)
                BulletText(string.Format("Item {0}..", i));
            TreePop();
        }

        // Bullet
        Button("Button##3");
        SameLine(0.0f, spacing);
        BulletText("Bullet text");

        AlignTextToFramePadding();
        BulletText("Node");
        SameLine(0.0f, spacing); Button("Button##4");
        Unindent();

        TreePop();
    }

    private class ChildWindowsSubsection
    {
        private bool disable_mouse_wheel = false;
        private bool disable_menu = false;
        private int offset_x = 0;

        public void Update()
        {
            if (!TreeNode("Child windows")) return;

            HelpMarker("Use child windows to begin into a self-contained independent scrolling/clipping regions within a host window.");

            Checkbox("Disable Mouse Wheel", ref disable_mouse_wheel);
            Checkbox("Disable Menu", ref disable_menu);

            // child 1
            ImGuiWindowFlags window_flags = ImGuiWindowFlags.HorizontalScrollbar;
            if (disable_mouse_wheel)
            {
                window_flags |= ImGuiWindowFlags.NoScrollWithMouse;
            }

            BeginChild("ChildL", new Vec2(GetContentRegionAvail().X * 0.5f, 260), ImGuiChildFlags.Borders, window_flags);
            for (int i = 0; i < 100; i++)
            {
                Text(string.Format("{0}: scrollable region", i.ToString("D4")));
            }
            EndChild();

            // child 2
            SameLine();
            ImGuiWindowFlags window_flags_child2 = ImGuiWindowFlags.None;
            if (disable_mouse_wheel)
            {
                window_flags_child2 |= ImGuiWindowFlags.NoScrollWithMouse;
            }
            if (!disable_menu)
            {
                window_flags_child2 |= ImGuiWindowFlags.MenuBar;
            }
            PushStyleVar(ImGuiStyleVar.ChildRounding, 5.0f);
            BeginChild("ChildR", new Vec2(0, 260), ImGuiChildFlags.Borders, window_flags_child2);
            if (!disable_menu && BeginMenuBar())
            {
                if (BeginMenu("Menu"))
                {
                    ExampleFileMenu();
                    EndMenu();
                }
                EndMenuBar();
            }
            for (int i = 0; i < 100; i++)
            {
                Button(i.ToString("D3"));
                if ((i % 2) == 0)
                {
                    SameLine();
                }
            }
            EndChild();
            PopStyleVar();

            Separator();

            SetNextItemWidth(100);
            DragInt("Offset X", ref offset_x, 1.0f, -1000, 1000);

            SetCursorPosX(GetCursorPosX() + offset_x);
            Vec4 colv4 = new(255.0f, 0.0f, 0.0f, 100.0f);
            PushStyleColor(ImGuiCol.ChildBg, ColorConvertFloat4ToU32(colv4));
            BeginChild("Red", new Vec2(200, 100), ImGuiChildFlags.Borders, ImGuiWindowFlags.None);
            for (int n = 0; n < 50; n++)
            {
                Text("Some test " + n.ToString());
            }
            EndChild();
            bool child_is_hovered = IsItemHovered();
            Vec2 child_rect_min = GetItemRectMin();
            Vec2 child_rect_max = GetItemRectMax();
            PopStyleColor();
            Text("Hovered: " + child_is_hovered.ToString());
            Text(string.Format("Rect of child window is: ({0},{1}) ({2},{3})", child_rect_min.X, child_rect_min.Y, child_rect_max.X, child_rect_max.Y));

            TreePop();
        }
    }

    private class WidgetsWidthSubsection
    {
        float f = 0.0f;
        bool show_indented_items = true;

        public void Update()
        {
            if (!TreeNode("Widgets Width")) return;

            Checkbox("Show intended items", ref show_indented_items);

            Text("SetNextItemWidth/PushItemWidth(100)");
            SameLine(); HelpMarker("Fixed width.");
            PushItemWidth(100);
            DragFloat("float##1b", ref f);
            if (show_indented_items)
            {
                Indent();
                DragFloat("float (intended)##1b", ref f);
                Unindent();
            }
            PopItemWidth();

            Text("SetNextItemWidth/PushItemWidth(-100)");
            SameLine(); HelpMarker("Align to right edge minus 100");
            PushItemWidth(-100);
            DragFloat("float##2a", ref f);
            if (show_indented_items)
            {
                Indent();
                DragFloat("float (indented)##2b", ref f);
                Unindent();
            }
            PopItemWidth();

            Text("SetNextItemWidth/PushItemWidth(GetContentRegionAvail().x * 0.5f)");
            SameLine(); HelpMarker("Half of available width.\n(~ right-cursor_pos)\n(works within a column set)");
            PushItemWidth(GetContentRegionAvail().X * 0.5f);
            DragFloat("float##3a", ref f);
            if (show_indented_items)
            {
                Indent();
                DragFloat("float (indented)##3b", ref f);
                Unindent();
            }
            PopItemWidth();

            Text("SetNextItemWidth/PushItemWidth(-GetContentRegionAvail().x * 0.5f)");
            SameLine(); HelpMarker("Align to right edge minus half");
            PushItemWidth(-GetContentRegionAvail().X * 0.5f);
            DragFloat("float##4a", ref f);
            if (show_indented_items)
            {
                Indent();
                DragFloat("float (indented)##4b", ref f);
                Unindent();
            }
            PopItemWidth();

            TreePop();
        }
    }

    private class BasicHorizontalLayoutSubsection
    {
        bool c1 = false;
        bool c2 = false;
        bool c3 = false;
        bool c4 = false;
        float bf0 = 1.0f;
        float bf1 = 2.0f;
        float bf2 = 3.0f;
        readonly string[] items = ["AAAA", "BBBB", "CCCC", "DDDD"];
        int item = -1;
        readonly int[] bselection = [0, 1, 2, 3];

        public void Update()
        {
            if (!TreeNode("Basic Horizontal Layout")) return;

            TextWrapped("(Use ImGui.SameLine() to keep adding items to the right of the preceding item)");

            // Text
            Text("Two items: Hello"); SameLine();
            TextColored(new Vec4(1, 1, 0, 1), "Sailor");

            // Adjust spacing
            Text("More spacing: Hello"); SameLine(0, 20);
            TextColored(new Vec4(1, 1, 0, 1), "Sailor");

            // Button
            AlignTextToFramePadding();
            Text("Normal buttons"); SameLine();
            Button("Banana"); SameLine();
            Button("Apple"); SameLine();
            Button("Corniflower");

            // Button
            Text("Small buttons"); SameLine();
            SmallButton("Like this one"); SameLine();
            Text("can fit within a text block.");

            // Aligned to arbitrary position. Easy/cheap column.
            Text("Aligned");
            SameLine(150); Text("x=150");
            SameLine(300); Text("x=300");
            Text("Aligned");
            SameLine(150); SmallButton("x=150");
            SameLine(300); SmallButton("x=300");

            // Checkbox
            Checkbox("My", ref c1); SameLine();
            Checkbox("Tailor", ref c2); SameLine();
            Checkbox("Is", ref c3); SameLine();
            Checkbox("Rich", ref c4);

            // Various
            PushItemWidth(80);
            Combo("Combo", ref item, items, items.Length); SameLine();
            SliderFloat("X", ref bf0, 0.0f, 5.0f); SameLine();
            SliderFloat("Y", ref bf1, 0.0f, 5.0f); SameLine();
            SliderFloat("Z", ref bf2, 0.0f, 5.0f);
            PopItemWidth();

            PushItemWidth(80);
            Text("Lists:");
            for (int i = 0; i < 4; i++)
            {
                if (i > 0) SameLine();
                PushID(i);
                ListBox("", ref bselection[i], items, items.Length);
                PopID();
            }
            PopItemWidth();

            // Dummy
            Vec2 button_sz = new(40, 40);
            Button("A", button_sz); SameLine();
            Dummy(button_sz); SameLine();
            Button("B", button_sz);

            // Manually wrapping
            // (we should eventually provide this as an automatic layout feature, but for now you can do it manually)
            Text("Manually wrapping:");
            int buttons_count = 20;
            float window_visible_x2 = GetWindowPos().X + GetContentRegionAvail().X;
            for (int n = 0; n < buttons_count; n++)
            {
                PushID(n);
                Button("Box", button_sz);
                float last_button_x2 = GetItemRectMax().X;
                float next_button_x2 = last_button_x2 + 1.0f + button_sz.X; // Expected position if next button was on same line
                if (n + 1 < buttons_count && next_button_x2 < window_visible_x2)
                {
                    SameLine();
                }
                PopID();
            }

            TreePop();
        }
    }

    private class ScrollingSubsection
    {
        int track_item = 50;
        bool enable_track = true;
        bool enable_extra_decorations = false;
        float scroll_to_off_px = 0.0f;
        float scroll_to_pos_px = 200.0f;

        public void Update()
        {
            if (!TreeNode("Scrolling")) return;

            //vertical
            HelpMarker("Use SetScrollHereY() or SetScrollFromPosY() to scroll to a given vertical position.");

            Checkbox("Decoration", ref enable_extra_decorations);

            Checkbox("Track", ref enable_track);
            PushItemWidth(100);

            SameLine(140); enable_track |= DragInt("##item", ref track_item, 0.25f, 0, 99, "Item = %d");

            bool scroll_to_off = Button("Scroll Offset");
            SameLine(140); scroll_to_off |= DragFloat("##off", ref scroll_to_off_px, 1.00f, 0, 100.0f, "+%.0f px");

            bool scroll_to_pos = Button("Scroll To Pos");
            SameLine(140); scroll_to_pos |= DragFloat("##pos", ref scroll_to_pos_px, 1.00f, -10, 100.0f, "X/Y = %.0f px");
            PopItemWidth();

            if (scroll_to_off || scroll_to_pos)
            {
                enable_track = false;
            }

            float child_w = (GetContentRegionAvail().X - 4 * 1.0f) / 5;
            if (child_w < 1.0f)
            {
                child_w = 1.0f;
            }
            PushID("##VerticalScrolling");
            for (int i = 0; i < 5; i++)
            {
                if (i > 0) { SameLine(); }
                BeginGroup();
                string[] names = ["Top", "25%", "Center", "75%", "Bottom"];
                TextUnformatted(names[i]);

                ImGuiWindowFlags child_flags = enable_extra_decorations ? ImGuiWindowFlags.MenuBar : 0;
                string child_id = names[i];
                bool child_is_visible = BeginChild(child_id, new Vec2(child_w, 200.0f), ImGuiChildFlags.Borders, child_flags);
                if (BeginMenuBar())
                {
                    TextUnformatted("abc");
                    EndMenuBar();
                }
                if (scroll_to_off)
                {
                    SetScrollY(scroll_to_off_px);
                }
                if (scroll_to_pos)
                {
                    SetScrollFromPosY(GetCursorStartPos().Y + scroll_to_pos_px, i * 0.25f);
                }
                if (child_is_visible)
                {
                    for (int item = 0; item < 100; item++)
                    {
                        if (enable_track && item == track_item)
                        {
                            TextColored(new Vec4(1, 1, 0, 1), "Item " + item);
                            SetScrollHereY(i * 0.25f);
                        }
                        else
                        {
                            Text("Item " + item);
                        }
                    }
                }
                float scroll_y = GetScrollY();
                float scroll_max_y = GetScrollMaxY();
                EndChild();
                Text(scroll_y + "/" + scroll_max_y);
                EndGroup();

            }
            PopID();

            //horizontal
            Spacing();
            HelpMarker("Use SetScrollHereX() or SetScrollFromPosX() to scroll to a given horizontal position.\n\n" +
                "Because the clipping rectangle of most window hides half worth of WindowPadding on the " +
                "left/right, using SetScrollFromPosX(+1) will usually result in clipped text whereas the " +
                "equivalent SetScrollFromPosY(+1) wouldn't.");
            PushID("##HorizontalScrolling");
            for (int i = 0; i < 5; i++)
            {
                float child_height = GetTextLineHeight() + 30.0f;
                ImGuiWindowFlags child_flags = ImGuiWindowFlags.HorizontalScrollbar | (enable_extra_decorations ? ImGuiWindowFlags.AlwaysVerticalScrollbar : 0);
                string[] names = ["Left", "25%", "Center", "75%", "Right"];
                string child_id = names[i];
                bool child_is_visible = BeginChild(child_id, new Vec2(-100, child_height), ImGuiChildFlags.Borders, child_flags);
                if (scroll_to_off)
                {
                    SetScrollX(scroll_to_off_px);
                }
                if (scroll_to_pos)
                {
                    SetScrollFromPosX(GetCursorStartPos().X + scroll_to_pos_px, i * 0.25f);
                }
                if (child_is_visible)
                {
                    for (int item = 0; item < 100; item++)
                    {
                        if (enable_track && item == track_item)
                        {
                            TextColored(new Vec4(1, 1, 0, 1), "Item " + item);
                            SetScrollHereX(i * 0.25f);
                        }
                        else
                        {
                            Text("Item " + item);
                        }
                        SameLine();
                    }
                }
                float scroll_x = GetScrollX();
                float scroll_max_x = GetScrollMaxX();
                EndChild();
                SameLine();
                Text(string.Format("{0}\n{1}/{2}", names[i], scroll_x, scroll_max_x));
                Spacing();
            }
            PopID();

            TreePop();
        }
    }
}
