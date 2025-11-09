using ImGuiNET;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using static ImGuiNET.ImGui;
using static SCMonoGameUtilities.DearImGui.Demos.GuiElements.GuiElementHelpers;
using Vec2 = System.Numerics.Vector2;
using Vec4 = System.Numerics.Vector4;

namespace SCMonoGameUtilities.DearImGui.Demos.GuiElements.Concepts;

// https://github.com/ocornut/imgui/blob/master/imgui_demo.cpp#L9832
class CustomRenderingWindow(bool isOpen = false)
{
    public bool IsOpen = isOpen;

    private readonly PrimitivesTab primitivesTab = new();
    private readonly CanvasTab canvasTab = new();
    private readonly BgFgDrawListsTab bgFgDrawListsTab = new();

    public void Update()
    {
        if (!IsOpen) return;

        if (Begin("Concept Demo: Custom Rendering", ref IsOpen))
        {
            if (BeginTabBar("##TabBar"))
            {
                primitivesTab.Update();
                canvasTab.Update();
                bgFgDrawListsTab.Update();
                UpdateDrawChannelTab();

                EndTabBar();
            }
        }

        End();
    }

    private static void PathConcaveShape(ImDrawListPtr draw_list, float x, float y, float sz)
    {
        Vec2[] pos_norms = [new(0.0f, 0.0f), new(0.3f, 0.0f), new(0.3f, 0.7f), new(0.7f, 0.7f), new(0.7f, 0.0f), new(1.0f, 0.0f), new(1.0f, 1.0f), new(0.0f, 1.0f)];
        foreach (var p in pos_norms)
            draw_list.PathLineTo(new(x + 0.5f + (int)(sz * p.X), y + 0.5f + (int)(sz * p.Y)));
    }

    private static void UpdateDrawChannelTab()
    {
        // Demonstrate out-of-order rendering via channels splitting
        // We use functions in ImDrawList as each draw list contains a convenience splitter,
        // but you can also instantiate your own ImDrawListSplitter if you need to nest them.
        if (BeginTabItem("Draw Channels"))
        {
            var draw_list = GetWindowDrawList();
            Text("Blue shape is drawn first: appears in back");
            Text("Red shape is drawn after: appears in front");
            var p0 = GetCursorScreenPos();
            draw_list.AddRectFilled(new(p0.X, p0.Y), new(p0.X + 50, p0.Y + 50), Color.Blue.PackedValue);
            draw_list.AddRectFilled(new(p0.X + 25, p0.Y + 25), new(p0.X + 75, p0.Y + 75), Color.Red.PackedValue);
            Dummy(new(75, 75));

            Separator();

            Text("Blue shape is drawn first, into channel 1: appears in front");
            Text("Red shape is drawn after, into channel 0: appears in back");
            var p1 = GetCursorScreenPos();

            // Create 2 channels and draw a Blue shape THEN a Red shape.
            // You can create any number of channels. Tables API use 1 channel per column in order to better batch draw calls.
            draw_list.ChannelsSplit(2);
            draw_list.ChannelsSetCurrent(1);
            draw_list.AddRectFilled(new(p1.X, p1.Y), new(p1.X + 50, p1.Y + 50), Color.Blue.PackedValue);
            draw_list.ChannelsSetCurrent(0);
            draw_list.AddRectFilled(new(p1.X + 25, p1.Y + 25), new(p1.X + 75, p1.Y + 75), Color.Red.PackedValue);

            // Flatten/reorder channels. Red shape is in channel 0 and it appears below the Blue shape in channel 1.
            // This works by copying draw indices only (vertices are not copied).
            draw_list.ChannelsMerge();
            Dummy(new(75, 75));
            Text("After reordering, contents of channel 0 appears below channel 1.");

            EndTabItem();
        }
    }

    private class PrimitivesTab
    {
        private float sz = 36.0f;
        private float thickness = 3.0f;
        private int ngon_sides = 6;
        private bool circle_segments_override = false;
        private int circle_segments_override_v = 12;
        private bool curve_segments_override = false;
        private int curve_segments_override_v = 8;
        private Vec4 colf = new(1.0f, 1.0f, 0.4f, 1.0f);

        public void Update()
        {
            if (!BeginTabItem("Primitives")) return;

            PushItemWidth(-GetFontSize() * 15);
            var draw_list = GetWindowDrawList();

            // Draw gradients
            // (note that those are currently exacerbating our sRGB/Linear issues)
            // Calling ImGui::GetColorU32() multiplies the given colors by the current Style Alpha, but you may pass the IM_COL32() directly as well..
            Text("Gradients");
            var gradient_size = new Vec2(CalcItemWidth(), GetFrameHeight());
            {
                var p0 = GetCursorScreenPos();
                var p1 = new Vec2(p0.X + gradient_size.X, p0.Y + gradient_size.Y);
                var col_a = Color.Black.PackedValue;
                var col_b = Color.White.PackedValue;
                draw_list.AddRectFilledMultiColor(p0, p1, col_a, col_b, col_b, col_a);
                InvisibleButton("##gradient1", gradient_size);
            }
            {
                var p0 = GetCursorScreenPos();
                var p1 = new Vec2(p0.X + gradient_size.X, p0.Y + gradient_size.Y);
                var col_a = Color.Green.PackedValue;
                var col_b = Color.Red.PackedValue;
                draw_list.AddRectFilledMultiColor(p0, p1, col_a, col_b, col_b, col_a);
                InvisibleButton("##gradient2", gradient_size);
            }

            // Draw a bunch of primitives
            Text("All primitives");
            DragFloat("Size", ref sz, 0.2f, 2.0f, 100.0f, "%.0f");
            DragFloat("Thickness", ref thickness, 0.05f, 1.0f, 8.0f, "%.02f");
            SliderInt("N-gon sides", ref ngon_sides, 3, 12);
            Checkbox("##circlesegmentoverride", ref circle_segments_override);
            SameLine(0.0f, GetStyle().ItemInnerSpacing.X);
            circle_segments_override |= SliderInt("Circle segments override", ref circle_segments_override_v, 3, 40);
            Checkbox("##curvessegmentoverride", ref curve_segments_override);
            SameLine(0.0f, GetStyle().ItemInnerSpacing.X);
            curve_segments_override |= SliderInt("Curves segments override", ref curve_segments_override_v, 3, 40);
            ColorEdit4("Color", ref colf);

            var p = GetCursorScreenPos();
            var col = new Color(colf).PackedValue;
            float spacing = 10.0f;
            ImDrawFlags corners_tl_br = ImDrawFlags.RoundCornersTopLeft | ImDrawFlags.RoundCornersBottomRight;
            float rounding = sz / 5.0f;
            int circle_segments = circle_segments_override ? circle_segments_override_v : 0;
            int curve_segments = curve_segments_override ? curve_segments_override_v : 0;
            Vec2[] cp3 = [new(0.0f, sz * 0.6f), new(sz * 0.5f, -sz * 0.4f), new(sz, sz)]; // Control points for curves
            Vec2[] cp4 = [new(0.0f, 0.0f), new(sz * 1.3f, sz * 0.3f), new(sz - sz * 1.3f, sz - sz * 0.3f), new(sz, sz)];

            float x = p.X + 4.0f;
            float y = p.Y + 4.0f;
            for (int n = 0; n < 2; n++)
            {
                // First line uses a thickness of 1.0f, second line uses the configurable thickness
                float th = n == 0 ? 1.0f : thickness;

                draw_list.AddNgon(new(x + sz * 0.5f, y + sz * 0.5f), sz * 0.5f, col, ngon_sides, th); // N-gon
                x += sz + spacing;
                draw_list.AddCircle(new(x + sz * 0.5f, y + sz * 0.5f), sz * 0.5f, col, circle_segments, th); // Circle
                x += sz + spacing;
                draw_list.AddEllipse(new(x + sz * 0.5f, y + sz * 0.5f), new(sz * 0.5f, sz * 0.3f), col, -0.3f, circle_segments, th); // Ellipse
                x += sz + spacing;  
                draw_list.AddRect(new(x, y), new(x + sz, y + sz), col, 0.0f, ImDrawFlags.None, th); // Square
                x += sz + spacing;  
                draw_list.AddRect(new(x, y), new(x + sz, y + sz), col, rounding, ImDrawFlags.None, th); // Square with all rounded corners
                x += sz + spacing;  
                draw_list.AddRect(new(x, y), new(x + sz, y + sz), col, rounding, corners_tl_br, th); // Square with two rounded corners
                x += sz + spacing;  
                draw_list.AddTriangle(new(x + sz * 0.5f, y), new(x + sz, y + sz - 0.5f), new(x, y + sz - 0.5f), col, th); // Triangle
                x += sz + spacing;
                ////draw_list->AddTriangle(ImVec2(x+sz*0.2f,y), ImVec2(x, y+sz-0.5f), ImVec2(x+sz*0.4f, y+sz-0.5f), col, th); // Thin triangle
                ////x+= sz*0.4f + spacing;

                PathConcaveShape(draw_list, x, y, sz); 
                draw_list.PathStroke(col, ImDrawFlags.Closed, th); // Concave Shape
                x += sz + spacing;  
                //draw_list->AddPolyline(concave_shape, IM_ARRAYSIZE(concave_shape), col, ImDrawFlags_Closed, th);

                draw_list.AddLine(new(x, y), new(x + sz, y), col, th); // Horizontal line (note: drawing a filled rectangle will be faster!)
                x += sz + spacing;  
                draw_list.AddLine(new(x, y), new(x, y + sz), col, th); // Vertical line (note: drawing a filled rectangle will be faster!)
                x += spacing;       
                draw_list.AddLine(new(x, y), new(x + sz, y + sz), col, th); // Diagonal line
                x += sz + spacing;  

                // Path
                draw_list.PathArcTo(new(x + sz * 0.5f, y + sz * 0.5f), sz * 0.5f, 3.141592f, 3.141592f * -0.5f);
                draw_list.PathStroke(col, ImDrawFlags.None, th);
                x += sz + spacing;

                // Quadratic Bezier Curve (3 control points)
                draw_list.AddBezierQuadratic(new(x + cp3[0].X, y + cp3[0].Y), new(x + cp3[1].X, y + cp3[1].Y), new(x + cp3[2].X, y + cp3[2].Y), col, th, curve_segments);
                x += sz + spacing;

                // Cubic Bezier Curve (4 control points)
                draw_list.AddBezierCubic(
                    new Vec2(x, y) + cp4[0],
                    new(x + cp4[1].X, y + cp4[1].Y),
                    new(x + cp4[2].X, y + cp4[2].Y),
                    new(x + cp4[3].X, y + cp4[3].Y),
                    col,
                    th,
                    curve_segments);

                x = p.X + 4;
                y += sz + spacing;
            }

            // Filled shapes
            draw_list.AddNgonFilled(new Vec2(x + sz * 0.5f, y + sz * 0.5f), sz * 0.5f, col, ngon_sides); // N-gon
            x += sz + spacing;  
            draw_list.AddCircleFilled(new Vec2(x + sz * 0.5f, y + sz * 0.5f), sz * 0.5f, col, circle_segments); // Circle
            x += sz + spacing;  
            draw_list.AddEllipseFilled(new Vec2(x + sz * 0.5f, y + sz * 0.5f), new Vec2(sz * 0.5f, sz * 0.3f), col, -0.3f, circle_segments); // Ellipse
            x += sz + spacing;
            draw_list.AddRectFilled(new Vec2(x, y), new Vec2(x + sz, y + sz), col); // Square
            x += sz + spacing;  
            draw_list.AddRectFilled(new Vec2(x, y), new Vec2(x + sz, y + sz), col, 10.0f); // Square with all rounded corners
            x += sz + spacing; 
            draw_list.AddRectFilled(new Vec2(x, y), new Vec2(x + sz, y + sz), col, 10.0f, corners_tl_br); // Square with two rounded corners
            x += sz + spacing;  
            draw_list.AddTriangleFilled(new Vec2(x + sz * 0.5f, y), new Vec2(x + sz, y + sz - 0.5f), new Vec2(x, y + sz - 0.5f), col); // Triangle
            x += sz + spacing;
            ////draw_list->AddTriangleFilled(ImVec2(x+sz*0.2f,y), ImVec2(x, y+sz-0.5f), ImVec2(x+sz*0.4f, y+sz-0.5f), col); // Thin triangle
            ////x += sz*0.4f + spacing;

            PathConcaveShape(draw_list, x, y, sz);
            draw_list.PathFillConcave(col); // Concave shape
            x += sz + spacing; 
            draw_list.AddRectFilled(new Vec2(x, y), new Vec2(x + sz, y + thickness), col); // Horizontal line (faster than AddLine, but only handle integer thickness)
            x += sz + spacing;  
            draw_list.AddRectFilled(new Vec2(x, y), new Vec2(x + thickness, y + sz), col); // Vertical line (faster than AddLine, but only handle integer thickness)
            x += spacing * 2.0f;
            draw_list.AddRectFilled(new Vec2(x, y), new Vec2(x + 1, y + 1), col); // Pixel (faster than AddLine)
            x += sz;          

            // Path
            draw_list.PathArcTo(new Vec2(x + sz * 0.5f, y + sz * 0.5f), sz * 0.5f, 3.141592f * -0.5f, 3.141592f);
            draw_list.PathFillConvex(col);
            x += sz + spacing;

            // Quadratic Bezier Curve (3 control points)
            draw_list.PathLineTo(new Vec2(x + cp3[0].X, y + cp3[0].Y));
            draw_list.PathBezierQuadraticCurveTo(new Vec2(x + cp3[1].X, y + cp3[1].Y), new Vec2(x + cp3[2].X, y + cp3[2].Y), curve_segments);
            draw_list.PathFillConvex(col);
            x += sz + spacing;

            draw_list.AddRectFilledMultiColor(
                new Vec2(x, y),
                new Vec2(x + sz, y + sz),
                Color.Black.PackedValue,
                Color.Red.PackedValue,
                Color.Yellow.PackedValue,
                Color.Green.PackedValue);

            Dummy(new Vec2((sz + spacing) * 13.2f, (sz + spacing) * 3.0f));
            PopItemWidth();
            EndTabItem();
        }
    }

    private class CanvasTab
    {
        private readonly List<Vec2> points = [];
        private Vec2 scrolling = new(0.0f, 0.0f);
        private bool opt_enable_grid = true;
        private bool opt_enable_context_menu = true;
        private bool adding_line = false;

        public void Update()
        {
            if (!BeginTabItem("Canvas")) return;

            Checkbox("Enable grid", ref opt_enable_grid);
            Checkbox("Enable context menu", ref opt_enable_context_menu);
            Text("Mouse Left: drag to add lines,\nMouse Right: drag to scroll, click for context menu.");

            // Typically you would use a BeginChild()/EndChild() pair to benefit from a clipping region + own scrolling.
            // Here we demonstrate that this can be replaced by simple offsetting + custom drawing + PushClipRect/PopClipRect() calls.
            // To use a child window instead we could use, e.g:
            //      ImGui::PushStyleVar(ImGuiStyleVar_WindowPadding, ImVec2(0, 0));      // Disable padding
            //      ImGui::PushStyleColor(ImGuiCol_ChildBg, IM_COL32(50, 50, 50, 255));  // Set a background color
            //      ImGui::BeginChild("canvas", ImVec2(0.0f, 0.0f), ImGuiChildFlags_Borders, ImGuiWindowFlags_NoMove);
            //      ImGui::PopStyleColor();
            //      ImGui::PopStyleVar();
            //      [...]
            //      ImGui::EndChild();

            // Using InvisibleButton() as a convenience 1) it will advance the layout cursor and 2) allows us to use IsItemHovered()/IsItemActive()
            var canvas_p0 = GetCursorScreenPos();      // ImDrawList API uses screen coordinates!
            var canvas_sz = GetContentRegionAvail();   // Resize canvas to what's available
            if (canvas_sz.X < 50.0f) canvas_sz.X = 50.0f;
            if (canvas_sz.Y < 50.0f) canvas_sz.Y = 50.0f;
            var canvas_p1 = new Vec2(canvas_p0.X + canvas_sz.X, canvas_p0.Y + canvas_sz.Y);

            // Draw border and background color
            ImGuiIOPtr io = GetIO();
            ImDrawListPtr draw_list = GetWindowDrawList();
            draw_list.AddRectFilled(canvas_p0, canvas_p1, new Color(50, 50, 50, 255).PackedValue);
            draw_list.AddRect(canvas_p0, canvas_p1, Color.White.PackedValue);

            // This will catch our interactions
            InvisibleButton("canvas", canvas_sz, ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight);
            bool is_hovered = IsItemHovered(); // Hovered
            bool is_active = IsItemActive();   // Held
            Vec2 origin = new(canvas_p0.X + scrolling.X, canvas_p0.Y + scrolling.Y); // Lock scrolled origin
            Vec2 mouse_pos_in_canvas = new(io.MousePos.X - origin.X, io.MousePos.Y - origin.Y);

            // Add first and second point
            if (is_hovered && !adding_line && IsMouseClicked(ImGuiMouseButton.Left))
            {
                points.Add(mouse_pos_in_canvas);
                points.Add(mouse_pos_in_canvas);
                adding_line = true;
            }
            if (adding_line)
            {
                points[^1] = mouse_pos_in_canvas;
                if (!IsMouseDown(ImGuiMouseButton.Left))
                    adding_line = false;
            }

            // Pan (we use a zero mouse threshold when there's no context menu)
            // You may decide to make that threshold dynamic based on whether the mouse is hovering something etc.
            float mouse_threshold_for_pan = opt_enable_context_menu ? -1.0f : 0.0f;
            if (is_active && IsMouseDragging(ImGuiMouseButton.Right, mouse_threshold_for_pan))
            {
                scrolling.X += io.MouseDelta.X;
                scrolling.Y += io.MouseDelta.Y;
            }

            // Context menu (under default mouse threshold)
            var drag_delta = GetMouseDragDelta(ImGuiMouseButton.Right);
            if (opt_enable_context_menu && drag_delta.X == 0.0f && drag_delta.Y == 0.0f)
                OpenPopupOnItemClick("context", ImGuiPopupFlags.MouseButtonRight);
            if (BeginPopup("context"))
            {
                if (adding_line)
                    points.RemoveRange(points.Count - 2, 2);
                adding_line = false;
                if (MenuItem("Remove one", null, false, points.Count > 0))
                    points.RemoveRange(points.Count - 2, 2);
                if (MenuItem("Remove all", null, false, points.Count > 0))
                    points.Clear();
                EndPopup();
            }

            // Draw grid + all lines in the canvas
            draw_list.PushClipRect(canvas_p0, canvas_p1, true);
            if (opt_enable_grid)
            {
                const float GRID_STEP = 64.0f;
                for (float x = scrolling.X % GRID_STEP; x < canvas_sz.X; x += GRID_STEP)
                    draw_list.AddLine(new(canvas_p0.X + x, canvas_p0.Y), new(canvas_p0.X + x, canvas_p1.Y), new Color(new Vec4(200, 200, 200, 40)).PackedValue);
                for (float y = scrolling.Y % GRID_STEP; y < canvas_sz.Y; y += GRID_STEP)
                    draw_list.AddLine(new(canvas_p0.X, canvas_p0.Y + y), new(canvas_p1.X, canvas_p0.Y + y), new Color(new Vec4(200, 200, 200, 40)).PackedValue);
            }
            for (int n = 0; n < points.Count; n += 2)
                draw_list.AddLine(new(origin.X + points[n].X, origin.Y + points[n].Y), new(origin.X + points[n + 1].X, origin.Y + points[n + 1].Y), Color.Yellow.PackedValue, 2.0f);
            draw_list.PopClipRect();

            EndTabItem();
        }
    }

    private class BgFgDrawListsTab
    {
        private bool draw_bg = true;
        private bool draw_fg = true;

        public void Update()
        {
            if (!BeginTabItem("BG/FG draw lists")) return;

            Checkbox("Draw in Background draw list", ref draw_bg);
            SameLine();
            HelpMarker("The Background draw list will be rendered below every Dear ImGui windows.");

            Checkbox("Draw in Foreground draw list", ref draw_fg);
            SameLine();
            HelpMarker("The Foreground draw list will be rendered over every Dear ImGui windows.");

            var window_pos = GetWindowPos();
            var window_size = GetWindowSize();
            var window_center = new Vec2(window_pos.X + window_size.X * 0.5f, window_pos.Y + window_size.Y * 0.5f);
            if (draw_bg)
                GetBackgroundDrawList().AddCircle(window_center, window_size.X * 0.6f, Color.Red.PackedValue, 0, 10 + 4);
            if (draw_fg)
                GetForegroundDrawList().AddCircle(window_center, window_size.Y * 0.6f, Color.Green.PackedValue, 0, 10);
            EndTabItem();
        }
    }
}
