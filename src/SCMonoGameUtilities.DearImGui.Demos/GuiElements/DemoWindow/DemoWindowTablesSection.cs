using ImGuiNET;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static ImGuiNET.ImGui;
using static SCMonoGameUtilities.DearImGui.Demos.GuiElements.GuiElementHelpers;
using Vec2 = System.Numerics.Vector2;

namespace SCMonoGameUtilities.DearImGui.Demos.GuiElements.DemoWindow;

// https://github.com/ocornut/imgui/blob/master/imgui_demo.cpp#L5638
class DemoWindowTablesSection
{
    private static readonly TableSizingPolicyDesc[] tableSizingPolicyDescriptions =
[
        new(ImGuiTableFlags.None,               "Default",                            "Use default sizing policy:\n- ImGuiTableFlags_SizingFixedFit if ScrollX is on or if host window has ImGuiWindowFlags_AlwaysAutoResize.\n- ImGuiTableFlags_SizingStretchSame otherwise."),
        new(ImGuiTableFlags.SizingFixedFit,     "ImGuiTableFlags.SizingFixedFit",     "Columns default to _WidthFixed (if resizable) or _WidthAuto (if not resizable), matching contents width."),
        new(ImGuiTableFlags.SizingFixedSame,    "ImGuiTableFlags.SizingFixedSame",    "Columns are all the same width, matching the maximum contents width.\nImplicitly disable ImGuiTableFlags_Resizable and enable ImGuiTableFlags_NoKeepColumnsVisible."),
        new(ImGuiTableFlags.SizingStretchProp,  "ImGuiTableFlags.SizingStretchProp",  "Columns default to _WidthStretch with weights proportional to their widths."),
        new(ImGuiTableFlags.SizingStretchSame,  "ImGuiTableFlags.SizingStretchSame",  "Columns default to _WidthStretch with same weights.")
    ];

    private bool disable_indent = false;

    private readonly BordersAndBackgroundsSubsection bordersAndBackgroundsSubsection = new();
    private readonly ResizableStretchSubsection resizableStretchSubsection = new();
    private readonly ResizableFixedSubsection resizableFixedSubsection = new();
    private readonly ResizableMixedSubsection resizableMixedSubsection = new();
    private readonly ReorderableAndHideableSubsection reorderableAndHideableSubsection = new();
    private readonly PaddingSubsection paddingSubsection = new();
    private readonly SizingPoliciesSubsection sizingPoliciesSubsection = new();
    private readonly VerticalScrollingSubsection verticalScrollingSubsection = new();
    private readonly HorizontalScrollingSubsection horizontalScrollingSubsection = new();
    private readonly ColumnFlagsSubsection columnFlagsSubsection = new();
    private readonly ColumnWidthsSubsection columnWidthsSubsection = new();
    private readonly OuterSizeSubsection outerSizeSubsection = new();
    private readonly BackgroundColorSubsection backgroundColorSubsection = new();
    private readonly TreeViewSubsection treeViewSubsection = new();
    private readonly ItemWidthSubsection itemWidthSubsection = new();
    private readonly CustomHeadersSubsection customHeadersSubsection = new();
    private readonly AngledHeadersSubsection angledHeadersSubsection = new();
    private readonly ContextMenusSubsection contextMenusSubsection = new();
    private readonly SyncedInstancesSubsection syncedInstancesSubsection = new();
    private readonly SortingSubsection sortingSubsection = new();
    private readonly AdvancedSubsection advancedSubsection = new();

    public void Update()
    {
        if (!CollapsingHeader("Tables & Columns")) return;

        // Using those as a base value to create width/height that are factor of the size of our font.
        // NB: Essentially constant, so presumably could be a field and set before first update method.
        // Fonts would need to be loaded already, though.
        // Initialise method (after content load), maybe? Might also need to be in frame though. not sure..
        var TEXT_BASE_WIDTH = CalcTextSize("A").X;
        var TEXT_BASE_HEIGHT = GetTextLineHeightWithSpacing();

        PushID("Tables");

        // Options
        int open_action = -1;
        if (Button("Expand all")) open_action = 1;
        SameLine();
        if (Button("Collapse all")) open_action = 0;
        SameLine();
        Checkbox("Disable tree indentation", ref disable_indent);
        SameLine();
        HelpMarker("Disables the indenting of subsection tree nodes - so that demo tables can use the full window width.");

        Separator();

        if (disable_indent) PushStyleVar(ImGuiStyleVar.IndentSpacing, 0.0f);

        // About Styling of tables
        // Most settings are configured on a per-table basis via the flags passed to BeginTable() and TableSetupColumns APIs.
        // There are however a few settings that a shared and part of the ImGuiStyle structure:
        //   style.CellPadding                          // Padding within each cell
        //   style.Colors[ImGuiCol_TableHeaderBg]       // Table header background
        //   style.Colors[ImGuiCol_TableBorderStrong]   // Table outer and header borders
        //   style.Colors[ImGuiCol_TableBorderLight]    // Table inner borders
        //   style.Colors[ImGuiCol_TableRowBg]          // Table row background when ImGuiTableFlags_RowBg is enabled (even rows)
        //   style.Colors[ImGuiCol_TableRowBgAlt]       // Table row background when ImGuiTableFlags_RowBg is enabled (odds rows)

        // Demos
        UpdateBasicSubsection(open_action);
        bordersAndBackgroundsSubsection.Update(open_action);
        resizableStretchSubsection.Update(open_action);
        resizableFixedSubsection.Update(open_action);
        resizableMixedSubsection.Update(open_action);
        reorderableAndHideableSubsection.Update(open_action);
        paddingSubsection.Update(open_action);
        sizingPoliciesSubsection.Update(TEXT_BASE_HEIGHT, TEXT_BASE_WIDTH, open_action);
        verticalScrollingSubsection.Update(TEXT_BASE_HEIGHT, open_action);
        horizontalScrollingSubsection.Update(TEXT_BASE_HEIGHT, TEXT_BASE_WIDTH, open_action);
        columnFlagsSubsection.Update(TEXT_BASE_HEIGHT, TEXT_BASE_WIDTH, open_action);
        columnWidthsSubsection.Update(TEXT_BASE_WIDTH, open_action);
        UpdateNestedTablesSubsection(TEXT_BASE_HEIGHT, open_action);
        UpdateRowHeightSubsection(TEXT_BASE_HEIGHT, open_action);
        outerSizeSubsection.Update(TEXT_BASE_HEIGHT, TEXT_BASE_WIDTH, open_action);
        backgroundColorSubsection.Update(open_action);
        treeViewSubsection.Update(TEXT_BASE_WIDTH, open_action);
        itemWidthSubsection.Update(TEXT_BASE_WIDTH, open_action);
        customHeadersSubsection.Update(open_action);
        angledHeadersSubsection.Update(TEXT_BASE_HEIGHT, open_action);
        contextMenusSubsection.Update(open_action);
        syncedInstancesSubsection.Update(open_action);
        sortingSubsection.Update(TEXT_BASE_HEIGHT, open_action);
        advancedSubsection.Update(TEXT_BASE_HEIGHT, TEXT_BASE_WIDTH, open_action);

        if (disable_indent) PopStyleVar();

        PopID();
    }

    private static void CheckboxesTableColumnFlags(ref ImGuiTableColumnFlags flags)
    {
        CheckboxFlags(ref flags, ImGuiTableColumnFlags.Disabled); 
        SameLine();
        HelpMarker("Master disable flag (also hide from context menu)");

        CheckboxFlags(ref flags, ImGuiTableColumnFlags.DefaultHide);
        CheckboxFlags(ref flags, ImGuiTableColumnFlags.DefaultSort);
        if (CheckboxFlags(ref flags, ImGuiTableColumnFlags.WidthStretch))
            flags &= ~(ImGuiTableColumnFlags.WidthMask ^ ImGuiTableColumnFlags.WidthStretch);
        if (CheckboxFlags(ref flags, ImGuiTableColumnFlags.WidthFixed))
            flags &= ~(ImGuiTableColumnFlags.WidthMask ^ ImGuiTableColumnFlags.WidthFixed);
        CheckboxFlags(ref flags, ImGuiTableColumnFlags.NoResize);
        CheckboxFlags(ref flags, ImGuiTableColumnFlags.NoReorder);
        CheckboxFlags(ref flags, ImGuiTableColumnFlags.NoHide);
        CheckboxFlags(ref flags, ImGuiTableColumnFlags.NoClip);
        CheckboxFlags(ref flags, ImGuiTableColumnFlags.NoSort);
        CheckboxFlags(ref flags, ImGuiTableColumnFlags.NoSortAscending);
        CheckboxFlags(ref flags, ImGuiTableColumnFlags.NoSortDescending);
        CheckboxFlags(ref flags, ImGuiTableColumnFlags.NoHeaderLabel);
        CheckboxFlags(ref flags, ImGuiTableColumnFlags.NoHeaderWidth);
        CheckboxFlags(ref flags, ImGuiTableColumnFlags.PreferSortAscending);
        CheckboxFlags(ref flags, ImGuiTableColumnFlags.PreferSortDescending);
        
        CheckboxFlags(ref flags, ImGuiTableColumnFlags.IndentEnable);
        SameLine();
        HelpMarker("Default for column 0");
        
        CheckboxFlags(ref flags, ImGuiTableColumnFlags.IndentDisable);
        SameLine();
        HelpMarker("Default for column >0");
        
        CheckboxFlags(ref flags, ImGuiTableColumnFlags.AngledHeader);
    }

    private static void CheckboxesReadOnlyTableColumnStatus(ImGuiTableColumnFlags flags)
    {
        BeginDisabled();
        CheckboxFlags(ref flags, ImGuiTableColumnFlags.IsEnabled);
        CheckboxFlags(ref flags, ImGuiTableColumnFlags.IsVisible);
        CheckboxFlags(ref flags, ImGuiTableColumnFlags.IsSorted);
        CheckboxFlags(ref flags, ImGuiTableColumnFlags.IsHovered);
        EndDisabled();
    }

    private static void ComboBoxTableSizingPolicy(ref ImGuiTableFlags flags)
    {
        int idx;
        for (idx = 0; idx < tableSizingPolicyDescriptions.Length; idx++)
            if (tableSizingPolicyDescriptions[idx].Value == (flags & ImGuiTableFlags.SizingMask))
                break;

        string preview_text = (idx < tableSizingPolicyDescriptions.Length) ? tableSizingPolicyDescriptions[idx].Name : "";
        if (BeginCombo("Sizing Policy", preview_text))
        {
            for (int n = 0; n < tableSizingPolicyDescriptions.Length; n++)
                if (Selectable(tableSizingPolicyDescriptions[n].Name, idx == n))
                    flags = (flags & ~ImGuiTableFlags.SizingMask) | tableSizingPolicyDescriptions[n].Value;
            EndCombo();
        }
        SameLine();
        TextDisabled("(?)");
        if (BeginItemTooltip())
        {
            PushTextWrapPos(GetFontSize() * 50.0f);
            for (int m = 0; m < tableSizingPolicyDescriptions.Length; m++)
            {
                Separator();
                Text($"{tableSizingPolicyDescriptions[m].Name}:");
                Separator();
                SetCursorPosX(GetCursorPosX() + GetStyle().IndentSpacing * 0.5f);
                TextUnformatted(tableSizingPolicyDescriptions[m].Tooltip);
            }
            PopTextWrapPos();
            EndTooltip();
        }
    }

    private static void UpdateBasicSubsection(int open_action)
    {
        if (open_action != -1) SetNextItemOpen(open_action != 0);
        if (!TreeNode("Basic")) return;

        // Here we will showcase three different ways to output a table.
        // They are very simple variations of a same thing!

        // [Method 1] Using TableNextRow() to create a new row, and TableSetColumnIndex() to select the column.
        // In many situations, this is the most flexible and easy to use pattern.
        HelpMarker("Using TableNextRow() + calling TableSetColumnIndex() _before_ each cell, in a loop.");
        if (BeginTable("table1", 3))
        {
            for (int row = 0; row < 4; row++)
            {
                TableNextRow();
                for (int column = 0; column < 3; column++)
                {
                    TableSetColumnIndex(column);
                    Text($"Row {row} Column {column}");
                }
            }
            EndTable();
        }

        // [Method 2] Using TableNextColumn() called multiple times, instead of using a for loop + TableSetColumnIndex().
        // This is generally more convenient when you have code manually submitting the contents of each column.
        HelpMarker("Using TableNextRow() + calling TableNextColumn() _before_ each cell, manually.");
        if (BeginTable("table2", 3))
        {
            for (int row = 0; row < 4; row++)
            {
                TableNextRow();
                TableNextColumn();
                Text($"Row {row}");
                TableNextColumn();
                Text("Some contents");
                TableNextColumn();
                Text("123.456");
            }
            EndTable();
        }

        // [Method 3] We call TableNextColumn() _before_ each cell. We never call TableNextRow(),
        // as TableNextColumn() will automatically wrap around and create new rows as needed.
        // This is generally more convenient when your cells all contains the same type of data.
        HelpMarker(
            "Only using TableNextColumn(), which tends to be convenient for tables where every cell contains "
            + "the same type of contents.\n This is also more similar to the old NextColumn() function of the "
            + "Columns API, and provided to facilitate the Columns->Tables API transition.");
        if (BeginTable("table3", 3))
        {
            for (int item = 0; item < 14; item++)
            {
                TableNextColumn();
                Text($"Item {item}");
            }
            EndTable();
        }

        TreePop();
    }

    private static void UpdateNestedTablesSubsection(float TEXT_BASE_HEIGHT, int open_action)
    {
        if (open_action != -1) SetNextItemOpen(open_action != 0);
        if (!TreeNode("Nested tables")) return;

        HelpMarker("This demonstrates embedding a table into another table cell.");

        if (BeginTable("table_nested1", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable))
        {
            TableSetupColumn("A0");
            TableSetupColumn("A1");
            TableHeadersRow();

            TableNextColumn();
            Text("A0 Row 0");
            {
                float rows_height = TEXT_BASE_HEIGHT * 2;
                if (BeginTable("table_nested2", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable))
                {
                    TableSetupColumn("B0");
                    TableSetupColumn("B1");
                    TableHeadersRow();

                    TableNextRow(ImGuiTableRowFlags.None, rows_height);
                    TableNextColumn();
                    Text("B0 Row 0");
                    TableNextColumn();
                    Text("B1 Row 0");
                    TableNextRow(ImGuiTableRowFlags.None, rows_height);
                    TableNextColumn();
                    Text("B0 Row 1");
                    TableNextColumn();
                    Text("B1 Row 1");

                    EndTable();
                }
            }
            TableNextColumn(); Text("A1 Row 0");
            TableNextColumn(); Text("A0 Row 1");
            TableNextColumn(); Text("A1 Row 1");
            EndTable();
        }
        TreePop();
    }

    private static void UpdateRowHeightSubsection(float TEXT_BASE_HEIGHT, int open_action)
    {
        if (open_action != -1) SetNextItemOpen(open_action != 0);
        if (!TreeNode("Row height")) return;

        HelpMarker(
            "You can pass a 'min_row_height' to TableNextRow().\n\nRows are padded with 'style.CellPadding.y' on top and bottom, "
            + "so effectively the minimum row height will always be >= 'style.CellPadding.y * 2.0f'.\n\n"
            + "We cannot honor a _maximum_ row height as that would require a unique clipping rectangle per row.");
        if (BeginTable("table_row_height", 1, ImGuiTableFlags.Borders))
        {
            for (int row = 0; row < 8; row++)
            {
                float min_row_height = (float)(int)(TEXT_BASE_HEIGHT * 0.30f * row);
                TableNextRow(ImGuiTableRowFlags.None, min_row_height);
                TableNextColumn();
                Text($"min_row_height = {min_row_height:F2}");
            }
            EndTable();
        }

        HelpMarker(
            "Showcase using SameLine(0,0) to share Current Line Height between cells.\n\n"
            + "Please note that Tables Row Height is not the same thing as Current Line Height, "
            + "as a table cell may contains multiple lines.");
        if (BeginTable("table_share_lineheight", 2, ImGuiTableFlags.Borders))
        {
            TableNextRow();
            TableNextColumn();
            ColorButton("##1", new(0.13f, 0.26f, 0.40f, 1.0f), ImGuiColorEditFlags.None, new(40, 40));
            TableNextColumn();
            Text("Line 1");
            Text("Line 2");

            TableNextRow();
            TableNextColumn();
            ColorButton("##2", new(0.13f, 0.26f, 0.40f, 1.0f), ImGuiColorEditFlags.None, new(40, 40));
            TableNextColumn();
            SameLine(0.0f, 0.0f); // Reuse line height from previous column
            Text("Line 1, with SameLine(0,0)");
            Text("Line 2");

            EndTable();
        }

        HelpMarker("Showcase altering CellPadding.y between rows. Note that CellPadding.x is locked for the entire table.");
        if (BeginTable("table_changing_cellpadding_y", 1, ImGuiTableFlags.Borders))
        {
            var style = GetStyle();
            for (int row = 0; row < 8; row++)
            {
                if ((row % 3) == 2)
                    PushStyleVarY(ImGuiStyleVar.CellPadding, 20.0f);
                TableNextRow(ImGuiTableRowFlags.None);
                TableNextColumn();
                Text($"CellPadding.y = {style.CellPadding.Y:F2}");
                if ((row % 3) == 2)
                    PopStyleVar();
            }
            EndTable();
        }

        TreePop();
    }

    private class BordersAndBackgroundsSubsection
    {
        enum ContentsType { Text, FillButton };
        private ImGuiTableFlags flags = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg;
        private bool display_headers = false;
        private int contents_type = (int)ContentsType.Text;

        public void Update(int open_action)
        {
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Borders, background")) return;

            // Expose a few Borders related flags interactively
            PushStyleCompact();

            CheckboxFlags(ref flags, ImGuiTableFlags.RowBg);

            CheckboxFlags(ref flags, ImGuiTableFlags.Borders);
            SameLine();
            HelpMarker("ImGuiTableFlags_Borders\n = ImGuiTableFlags_BordersInnerV\n | ImGuiTableFlags_BordersOuterV\n | ImGuiTableFlags_BordersInnerH\n | ImGuiTableFlags_BordersOuterH");

            Indent();

            CheckboxFlags(ref flags, ImGuiTableFlags.BordersH);

            Indent();

            CheckboxFlags(ref flags, ImGuiTableFlags.BordersOuterH);
            CheckboxFlags(ref flags, ImGuiTableFlags.BordersInnerH);

            Unindent();

            CheckboxFlags(ref flags, ImGuiTableFlags.BordersV);

            Indent();

            CheckboxFlags(ref flags, ImGuiTableFlags.BordersOuterV);
            CheckboxFlags(ref flags, ImGuiTableFlags.BordersInnerV);

            Unindent();

            CheckboxFlags(ref flags, ImGuiTableFlags.BordersOuter);
            CheckboxFlags(ref flags, ImGuiTableFlags.BordersInner);

            Unindent();

            AlignTextToFramePadding(); Text("Cell contents:");
            SameLine();
            RadioButton("Text", ref contents_type, (int)ContentsType.Text);
            SameLine();
            RadioButton("FillButton", ref contents_type, (int)ContentsType.FillButton);

            Checkbox("Display headers", ref display_headers);

            CheckboxFlags(ref flags, ImGuiTableFlags.NoBordersInBody);
            SameLine();
            HelpMarker("Disable vertical borders in columns Body (borders will always appear in Headers)");

            PopStyleCompact();

            if (BeginTable("table1", 3, flags))
            {
                // Display headers so we can inspect their interaction with borders
                // (Headers are not the main purpose of this section of the demo, so we are not elaborating on them now. See other sections for details)
                if (display_headers)
                {
                    TableSetupColumn("One");
                    TableSetupColumn("Two");
                    TableSetupColumn("Three");
                    TableHeadersRow();
                }

                for (int row = 0; row < 5; row++)
                {
                    TableNextRow();
                    for (int column = 0; column < 3; column++)
                    {
                        TableSetColumnIndex(column);
                        if (contents_type == (int)ContentsType.Text)
                            TextUnformatted($"Hello {column},{row}");
                        else if (contents_type == (int)ContentsType.FillButton)
                            Button("hello", new Vec2(-float.Epsilon, 0.0f));
                    }
                }
                EndTable();
            }

            TreePop();
        }
    }

    private class ResizableStretchSubsection
    {
        private ImGuiTableFlags flags = ImGuiTableFlags.SizingStretchSame
            | ImGuiTableFlags.Resizable
            | ImGuiTableFlags.BordersOuter 
            | ImGuiTableFlags.BordersV
            | ImGuiTableFlags.ContextMenuInBody;

        public void Update(int open_action)
        {
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Resizable, stretch")) return;

            // By default, if we don't enable ScrollX the sizing policy for each column is "Stretch"
            // All columns maintain a sizing weight, and they will occupy all available width.
            PushStyleCompact();
            CheckboxFlags(ref flags, ImGuiTableFlags.Resizable);
            CheckboxFlags(ref flags, ImGuiTableFlags.BordersV);
            SameLine(); HelpMarker(
                "Using the _Resizable flag automatically enables the _BordersInnerV flag as well, "
                + "this is why the resize borders are still showing when unchecking this.");
            PopStyleCompact();

            if (BeginTable("table1", 3, flags))
            {
                for (int row = 0; row < 5; row++)
                {
                    TableNextRow();
                    for (int column = 0; column < 3; column++)
                    {
                        TableSetColumnIndex(column);
                        Text($"Hello {column},{row}");
                    }
                }
                EndTable();
            }

            TreePop();
        }
    }

    private class ResizableFixedSubsection
    {
        private ImGuiTableFlags flags = ImGuiTableFlags.SizingFixedFit
            | ImGuiTableFlags.Resizable
            | ImGuiTableFlags.BordersOuter
            | ImGuiTableFlags.BordersV
            | ImGuiTableFlags.ContextMenuInBody;

        public void Update(int open_action)
        {
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Resizable, fixed")) return;

            // Here we use ImGuiTableFlags_SizingFixedFit (even though _ScrollX is not set)
            // So columns will adopt the "Fixed" policy and will maintain a fixed width regardless of the whole available width (unless table is small)
            // If there is not enough available width to fit all columns, they will however be resized down.
            // FIXME-TABLE: Providing a stretch-on-init would make sense especially for tables which don't have saved settings
            HelpMarker(
                "Using _Resizable + _SizingFixedFit flags.\n"
                + "Fixed-width columns generally makes more sense if you want to use horizontal scrolling.\n\n"
                + "Double-click a column border to auto-fit the column to its contents.");
            PushStyleCompact();
            CheckboxFlags(ref flags, ImGuiTableFlags.NoHostExtendX);
            PopStyleCompact();

            if (BeginTable("table1", 3, flags))
            {
                for (int row = 0; row < 5; row++)
                {
                    TableNextRow();
                    for (int column = 0; column < 3; column++)
                    {
                        TableSetColumnIndex(column);
                        Text($"Hello {column},{row}");
                    }
                }
                EndTable();
            }

            TreePop();
        }
    }

    private class ResizableMixedSubsection
    {
        private readonly ImGuiTableFlags flags = ImGuiTableFlags.SizingFixedFit
            | ImGuiTableFlags.RowBg
            | ImGuiTableFlags.Borders
            | ImGuiTableFlags.Resizable
            | ImGuiTableFlags.Reorderable
            | ImGuiTableFlags.Hideable;

        public void Update(int open_action)
        {
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Resizable, mixed")) return;

            HelpMarker(
                "Using TableSetupColumn() to alter resizing policy on a per-column basis.\n\n"
                + "When combining Fixed and Stretch columns, generally you only want one, maybe two trailing columns to use _WidthStretch.");

            if (BeginTable("table1", 3, flags))
            {
                TableSetupColumn("AAA", ImGuiTableColumnFlags.WidthFixed);
                TableSetupColumn("BBB", ImGuiTableColumnFlags.WidthFixed);
                TableSetupColumn("CCC", ImGuiTableColumnFlags.WidthStretch);
                TableHeadersRow();
                for (int row = 0; row < 5; row++)
                {
                    TableNextRow();
                    for (int column = 0; column < 3; column++)
                    {
                        TableSetColumnIndex(column);
                        Text($"{(column == 2 ? "Stretch" : "Fixed")} {column},{row}");
                    }
                }
                EndTable();
            }
            if (BeginTable("table2", 6, flags))
            {
                TableSetupColumn("AAA", ImGuiTableColumnFlags.WidthFixed);
                TableSetupColumn("BBB", ImGuiTableColumnFlags.WidthFixed);
                TableSetupColumn("CCC", ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.DefaultHide);
                TableSetupColumn("DDD", ImGuiTableColumnFlags.WidthStretch);
                TableSetupColumn("EEE", ImGuiTableColumnFlags.WidthStretch);
                TableSetupColumn("FFF", ImGuiTableColumnFlags.WidthStretch | ImGuiTableColumnFlags.DefaultHide);
                TableHeadersRow();
                for (int row = 0; row < 5; row++)
                {
                    TableNextRow();
                    for (int column = 0; column < 6; column++)
                    {
                        TableSetColumnIndex(column);
                        Text($"{(column >= 3 ? "Stretch" : "Fixed")} {column},{row}");
                    }
                }
                EndTable();
            }
            TreePop();
        }
    }

    private class ReorderableAndHideableSubsection
    {
        private ImGuiTableFlags flags = ImGuiTableFlags.Resizable
            | ImGuiTableFlags.Reorderable
            | ImGuiTableFlags.Hideable
            | ImGuiTableFlags.BordersOuter
            | ImGuiTableFlags.BordersV;

        public void Update(int open_action)
        {
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Reorderable, hideable, with headers")) return;

            HelpMarker(
                "Click and drag column headers to reorder columns.\n\n"
                + "Right-click on a header to open a context menu.");
            PushStyleCompact();
            CheckboxFlags(ref flags, ImGuiTableFlags.Resizable);
            CheckboxFlags(ref flags, ImGuiTableFlags.Reorderable);
            CheckboxFlags(ref flags, ImGuiTableFlags.Hideable);
            CheckboxFlags(ref flags, ImGuiTableFlags.NoBordersInBody);
            CheckboxFlags(ref flags, ImGuiTableFlags.NoBordersInBodyUntilResize); SameLine(); HelpMarker("Disable vertical borders in columns Body until hovered for resize (borders will always appear in Headers)");
            CheckboxFlags(ref flags, ImGuiTableFlags.HighlightHoveredColumn);
            PopStyleCompact();

            if (BeginTable("table1", 3, flags))
            {
                // Submit columns name with TableSetupColumn() and call TableHeadersRow() to create a row with a header in each column.
                // (Later we will show how TableSetupColumn() has other uses, optional flags, sizing weight etc.)
                TableSetupColumn("One");
                TableSetupColumn("Two");
                TableSetupColumn("Three");
                TableHeadersRow();
                for (int row = 0; row < 6; row++)
                {
                    TableNextRow();
                    for (int column = 0; column < 3; column++)
                    {
                        TableSetColumnIndex(column);
                        Text($"Hello {column},{row}");
                    }
                }
                EndTable();
            }

            // Use outer_size.x == 0.0f instead of default to make the table as tight as possible
            // (only valid when no scrolling and no stretch column)
            if (BeginTable("table2", 3, flags | ImGuiTableFlags.SizingFixedFit, Vec2.Zero))
            {
                TableSetupColumn("One");
                TableSetupColumn("Two");
                TableSetupColumn("Three");
                TableHeadersRow();
                for (int row = 0; row < 6; row++)
                {
                    TableNextRow();
                    for (int column = 0; column < 3; column++)
                    {
                        TableSetColumnIndex(column);
                        Text($"Fixed {column},{row}");
                    }
                }
                EndTable();
            }

            TreePop();
        }
    }

    private class PaddingSubsection
    {
        private readonly string[] text_bufs = [.. Enumerable.Range(0, 3*5).Select(i => "edit me")]; // Mini text storage for 3x5 cells
        private ImGuiTableFlags flags1 = ImGuiTableFlags.BordersV;
        private bool show_headers = false;
        private ImGuiTableFlags flags2 = ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg;
        private Vec2 cell_padding = new(0.0f, 0.0f);
        private bool show_widget_frame_bg = true;

        public void Update(int open_action)
        {
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Padding")) return;

            // First example: showcase use of padding flags and effect of BorderOuterV/BorderInnerV on X padding.
            // We don't expose BorderOuterH/BorderInnerH here because they have no effect on X padding.
            HelpMarker(
                "We often want outer padding activated when any using features which makes the edges of a column visible:\n"
                + "e.g.:\n"
                + "- BorderOuterV\n"
                + "- any form of row selection\n"
                + "Because of this, activating BorderOuterV sets the default to PadOuterX. "
                + "Using PadOuterX or NoPadOuterX you can override the default.\n\n"
                + "Actual padding values are using style.CellPadding.\n\n"
                + "In this demo we don't show horizontal borders to emphasize how they don't affect default horizontal padding.");

            PushStyleCompact();
            CheckboxFlags(ref flags1, ImGuiTableFlags.PadOuterX);
            SameLine(); HelpMarker("Enable outer-most padding (default if ImGuiTableFlags_BordersOuterV is set)");
            CheckboxFlags(ref flags1, ImGuiTableFlags.NoPadOuterX);
            SameLine(); HelpMarker("Disable outer-most padding (default if ImGuiTableFlags_BordersOuterV is not set)");
            CheckboxFlags(ref flags1, ImGuiTableFlags.NoPadInnerX);
            SameLine(); HelpMarker("Disable inner padding between columns (double inner padding if BordersOuterV is on, single inner padding if BordersOuterV is off)");
            CheckboxFlags(ref flags1, ImGuiTableFlags.BordersOuterV);
            CheckboxFlags(ref flags1, ImGuiTableFlags.BordersInnerV);
            Checkbox("show_headers", ref show_headers);
            PopStyleCompact();

            if (BeginTable("table_padding", 3, flags1))
            {
                if (show_headers)
                {
                    TableSetupColumn("One");
                    TableSetupColumn("Two");
                    TableSetupColumn("Three");
                    TableHeadersRow();
                }

                for (int row = 0; row < 5; row++)
                {
                    TableNextRow();
                    for (int column = 0; column < 3; column++)
                    {
                        TableSetColumnIndex(column);
                        if (row == 0)
                        {
                            Text($"Avail {GetContentRegionAvail().X}"); // todo: %.2f format string equiv
                        }
                        else
                        {
                            Button($"Hello {column},{row}", new Vec2(-float.Epsilon, 0.0f));
                        }
                        //if (ImGui::TableGetColumnFlags() & ImGuiTableColumnFlags_IsHovered)
                        //    ImGui::TableSetBgColor(ImGuiTableBgTarget_CellBg, IM_COL32(0, 100, 0, 255));
                    }
                }
                EndTable();
            }

            // Second example: set style.CellPadding to (0.0) or a custom value.
            // FIXME-TABLE: Vertical border effectively not displayed the same way as horizontal one...
            HelpMarker("Setting style.CellPadding to (0,0) or a custom value.");

            PushStyleCompact();
            CheckboxFlags(ref flags2, ImGuiTableFlags.Borders);
            CheckboxFlags(ref flags2, ImGuiTableFlags.BordersH);
            CheckboxFlags(ref flags2, ImGuiTableFlags.BordersV);
            CheckboxFlags(ref flags2, ImGuiTableFlags.BordersInner);
            CheckboxFlags(ref flags2, ImGuiTableFlags.BordersOuter);
            CheckboxFlags(ref flags2, ImGuiTableFlags.RowBg);
            CheckboxFlags(ref flags2, ImGuiTableFlags.Resizable);
            Checkbox("show_widget_frame_bg", ref show_widget_frame_bg);
            SliderFloat2("CellPadding", ref cell_padding, 0.0f, 10.0f, "%.0f");
            PopStyleCompact();

            PushStyleVar(ImGuiStyleVar.CellPadding, cell_padding);
            if (BeginTable("table_padding_2", 3, flags2))
            {
                if (!show_widget_frame_bg)
                    PushStyleColor(ImGuiCol.FrameBg, 0);
                for (int cell = 0; cell < 3 * 5; cell++)
                {
                    TableNextColumn();
                    SetNextItemWidth(-float.Epsilon);
                    PushID(cell);
                    InputText("##cell", ref text_bufs[cell], 16);
                    PopID();
                }
                if (!show_widget_frame_bg)
                    PopStyleColor();

                EndTable();
            }
            PopStyleVar();

            TreePop();
        }
    }

    private class SizingPoliciesSubsection()
    {
        private ImGuiTableFlags flags1 = ImGuiTableFlags.BordersV | ImGuiTableFlags.BordersOuterH | ImGuiTableFlags.RowBg | ImGuiTableFlags.ContextMenuInBody;
        private readonly ImGuiTableFlags[] flags = [ImGuiTableFlags.SizingFixedFit, ImGuiTableFlags.SizingFixedSame, ImGuiTableFlags.SizingStretchProp, ImGuiTableFlags.SizingStretchSame];
        private ImGuiTableFlags flags2 = ImGuiTableFlags.ScrollY | ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.Resizable;
        private int contents_type = (int)ContentsType.ShowWidth;
        private int column_count = 3;
        private string text_buf = "";

        enum ContentsType { ShowWidth, ShortText, LongText, Button, FillButton, InputText };

        public void Update(float TEXT_BASE_HEIGHT, float TEXT_BASE_WIDTH, int open_action)
        {
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Sizing policies")) return;

            PushStyleCompact();
            CheckboxFlags(ref flags1, ImGuiTableFlags.Resizable);
            CheckboxFlags(ref flags1, ImGuiTableFlags.NoHostExtendX);
            PopStyleCompact();

            for (int table_n = 0; table_n < 4; table_n++)
            {
                PushID(table_n);
                SetNextItemWidth(TEXT_BASE_WIDTH * 30);
                ComboBoxTableSizingPolicy(ref flags[table_n]);

                // To make it easier to understand the different sizing policy,
                // For each policy: we display one table where the columns have equal contents width,
                // and one where the columns have different contents width.
                if (BeginTable("table1", 3, flags[table_n] | flags1))
                {
                    for (int row = 0; row < 3; row++)
                    {
                        TableNextRow();
                        TableNextColumn(); Text("Oh dear");
                        TableNextColumn(); Text("Oh dear");
                        TableNextColumn(); Text("Oh dear");
                    }
                    EndTable();
                }
                if (BeginTable("table2", 3, flags[table_n] | flags1))
                {
                    for (int row = 0; row < 3; row++)
                    {
                        TableNextRow();
                        TableNextColumn(); Text("AAAA");
                        TableNextColumn(); Text("BBBBBBBB");
                        TableNextColumn(); Text("CCCCCCCCCCCC");
                    }
                    EndTable();
                }
                PopID();
            }

            Spacing();
            TextUnformatted("Advanced");
            SameLine();
            HelpMarker(
                "This section allows you to interact and see the effect of various sizing policies "
                + "depending on whether Scroll is enabled and the contents of your columns.");

            PushStyleCompact();
            PushID("Advanced");
            PushItemWidth(TEXT_BASE_WIDTH * 30);
            ComboBoxTableSizingPolicy(ref flags2);
            Combo("Contents", ref contents_type, "Show width\0Short Text\0Long Text\0Button\0Fill Button\0InputText\0");
            if (contents_type == (int)ContentsType.FillButton)
            {
                SameLine();
                HelpMarker(
                    "Be mindful that using right-alignment (e.g. size.x = -FLT_MIN) creates a feedback loop "
                    + "where contents width can feed into auto-column width can feed into contents width.");
            }

            DragInt("Columns", ref column_count, 0.1f, 1, 64, "%d", ImGuiSliderFlags.AlwaysClamp);
            CheckboxFlags(ref flags2, ImGuiTableFlags.Resizable);
            CheckboxFlags(ref flags2, ImGuiTableFlags.PreciseWidths);
            SameLine(); HelpMarker("Disable distributing remainder width to stretched columns (width allocation on a 100-wide table with 3 columns: Without this flag: 33,33,34. With this flag: 33,33,33). With larger number of columns, resizing will appear to be less smooth.");
            CheckboxFlags(ref flags2, ImGuiTableFlags.ScrollX);
            CheckboxFlags(ref flags2, ImGuiTableFlags.ScrollY);
            CheckboxFlags(ref flags2, ImGuiTableFlags.NoClip);
            PopItemWidth();
            PopID();
            PopStyleCompact();

            if (BeginTable("table2", column_count, flags2, new Vec2(0.0f, TEXT_BASE_HEIGHT * 7)))
            {
                for (int cell = 0; cell < 10 * column_count; cell++)
                {
                    TableNextColumn();
                    int column = TableGetColumnIndex();
                    int row = TableGetRowIndex();

                    PushID(cell);
                    var label = $"Hello {column},{row}";
                    switch (contents_type)
                    {
                        case (int)ContentsType.ShortText: TextUnformatted(label); break;
                        case (int)ContentsType.LongText: Text($"Some {(column == 0 ? "long" : "longeeer")} text {column},{row}\nOver two lines.."); break;
                        case (int)ContentsType.ShowWidth: Text($"W: {GetContentRegionAvail().X}"); break; // TODO: format string %.1f equiv...
                        case (int)ContentsType.Button: Button(label); break;
                        case (int)ContentsType.FillButton: Button(label, new Vec2(-float.Epsilon, 0.0f)); break;
                        case (int)ContentsType.InputText: SetNextItemWidth(-float.Epsilon); InputText("##", ref text_buf, 32); break;
                    }
                    PopID();
                }
                EndTable();
            }

            TreePop();
        }
    }

    private class VerticalScrollingSubsection
    {
        private ImGuiTableFlags flags = ImGuiTableFlags.ScrollY
            | ImGuiTableFlags.RowBg
            | ImGuiTableFlags.BordersOuter
            | ImGuiTableFlags.BordersV
            | ImGuiTableFlags.Resizable
            | ImGuiTableFlags.Reorderable
            | ImGuiTableFlags.Hideable;

        public void Update(float textBaseHeight, int open_action)
        {
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Vertical scrolling, with clipping")) return;

            HelpMarker(
                "Here we activate ScrollY, which will create a child window container to allow hosting scrollable contents.\n\n"
                + "We also demonstrate using ImGuiListClipper to virtualize the submission of many items.");

            PushStyleCompact();
            CheckboxFlags(ref flags, ImGuiTableFlags.ScrollY);
            PopStyleCompact();

            // When using ScrollX or ScrollY we need to specify a size for our table container!
            // Otherwise by default the table will fit all available space, like a BeginChild() call.
            var outer_size = new Vec2(0.0f, textBaseHeight * 8);
            if (BeginTable("table_scrolly", 3, flags, outer_size))
            {
                TableSetupScrollFreeze(0, 1); // Make top row always visible
                TableSetupColumn("One", ImGuiTableColumnFlags.None);
                TableSetupColumn("Two", ImGuiTableColumnFlags.None);
                TableSetupColumn("Three", ImGuiTableColumnFlags.None);
                TableHeadersRow();

                // Demonstrate using clipper for large vertical lists
                unsafe
                {
                    ImGuiListClipperPtr clipper = new(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
                    clipper.Begin(1000);
                    while (clipper.Step())
                    {
                        for (int row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
                        {
                            TableNextRow();
                            for (int column = 0; column < 3; column++)
                            {
                                TableSetColumnIndex(column);
                                Text($"Hello {column},{row}");
                            }
                        }
                    }
                    clipper.Destroy(); // should probably be in a finally block, really..
                }

                EndTable();
            }

            TreePop();
        }
    }

    private class HorizontalScrollingSubsection
    {
        private ImGuiTableFlags flags = ImGuiTableFlags.ScrollX
            | ImGuiTableFlags.ScrollY 
            | ImGuiTableFlags.RowBg 
            | ImGuiTableFlags.BordersOuter 
            | ImGuiTableFlags.BordersV
            | ImGuiTableFlags.Resizable
            | ImGuiTableFlags.Reorderable
            | ImGuiTableFlags.Hideable;

        private int freeze_cols = 1;
        private int freeze_rows = 1;

        private ImGuiTableFlags flags2 = ImGuiTableFlags.SizingStretchSame
            | ImGuiTableFlags.ScrollX
            | ImGuiTableFlags.ScrollY
            | ImGuiTableFlags.BordersOuter
            | ImGuiTableFlags.RowBg
            | ImGuiTableFlags.ContextMenuInBody;

        private float inner_width = 1000.0f;

        public void Update(float TEXT_BASE_HEIGHT, float TEXT_BASE_WIDTH, int open_action)
        {
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Horizontal scrolling")) return;

            HelpMarker(
                "When ScrollX is enabled, the default sizing policy becomes ImGuiTableFlags_SizingFixedFit, "
                + "as automatically stretching columns doesn't make much sense with horizontal scrolling.\n\n"
                + "Also note that as of the current version, you will almost always want to enable ScrollY along with ScrollX, "
                + "because the container window won't automatically extend vertically to fix contents "
                + "(this may be improved in future versions).");

            PushStyleCompact();
            CheckboxFlags(ref flags, ImGuiTableFlags.Resizable);
            CheckboxFlags(ref flags, ImGuiTableFlags.ScrollX);
            CheckboxFlags(ref flags, ImGuiTableFlags.ScrollY);
            SetNextItemWidth(GetFrameHeight());
            DragInt("freeze_cols", ref freeze_cols, 0.2f, 0, 9, null, ImGuiSliderFlags.NoInput);
            SetNextItemWidth(GetFrameHeight());
            DragInt("freeze_rows", ref freeze_rows, 0.2f, 0, 9, null, ImGuiSliderFlags.NoInput);
            PopStyleCompact();

            // When using ScrollX or ScrollY we need to specify a size for our table container!
            // Otherwise by default the table will fit all available space, like a BeginChild() call.
            Vec2 outer_size = new(0.0f, TEXT_BASE_HEIGHT * 8);
            if (BeginTable("table_scrollx", 7, flags, outer_size))
            {
                TableSetupScrollFreeze(freeze_cols, freeze_rows);
                TableSetupColumn("Line #", ImGuiTableColumnFlags.NoHide); // Make the first column not hideable to match our use of TableSetupScrollFreeze()
                TableSetupColumn("One");
                TableSetupColumn("Two");
                TableSetupColumn("Three");
                TableSetupColumn("Four");
                TableSetupColumn("Five");
                TableSetupColumn("Six");
                TableHeadersRow();
                for (int row = 0; row < 20; row++)
                {
                    TableNextRow();
                    for (int column = 0; column < 7; column++)
                    {
                        // Both TableNextColumn() and TableSetColumnIndex() return true when a column is visible or performing width measurement.
                        // Because here we know that:
                        // - A) all our columns are contributing the same to row height
                        // - B) column 0 is always visible,
                        // We only always submit this one column and can skip others.
                        // More advanced per-column clipping behaviors may benefit from polling the status flags via TableGetColumnFlags().
                        if (!TableSetColumnIndex(column) && column > 0)
                            continue;
                        if (column == 0)
                            Text($"Line {row}");
                        else
                            Text($"Hello world {column},{row}");
                    }
                }
                EndTable();
            }

            Spacing();
            TextUnformatted("Stretch + ScrollX");
            SameLine();
            HelpMarker(
                "Showcase using Stretch columns + ScrollX together: "
                + "this is rather unusual and only makes sense when specifying an 'inner_width' for the table!\n"
                + "Without an explicit value, inner_width is == outer_size.x and therefore using Stretch columns "
                + "along with ScrollX doesn't make sense.");

            PushStyleCompact();
            PushID("flags3");
            PushItemWidth(TEXT_BASE_WIDTH * 30);
            CheckboxFlags(ref flags2, ImGuiTableFlags.ScrollX);
            DragFloat("inner_width", ref inner_width, 1.0f, 0.0f, float.MaxValue, "%.1f");
            PopItemWidth();
            PopID();
            PopStyleCompact();
            if (BeginTable("table2", 7, (ImGuiTableFlags)flags2, outer_size, inner_width))
            {
                for (int cell = 0; cell < 20 * 7; cell++)
                {
                    TableNextColumn();
                    Text($"Hello world {TableGetColumnIndex()},{TableGetRowIndex()}");
                }
                EndTable();
            }
            TreePop();
        }
    }

    private class ColumnFlagsSubsection
    {
        private readonly int column_count = 3;
        private readonly string[] column_names = ["One", "Two", "Three"];
        private readonly ImGuiTableColumnFlags[] column_flags = [ImGuiTableColumnFlags.DefaultSort, ImGuiTableColumnFlags.None, ImGuiTableColumnFlags.DefaultHide];
        private readonly ImGuiTableColumnFlags[] column_flags_out = [0, 0, 0]; // Output from TableGetColumnFlags()

        public void Update(float TEXT_BASE_HEIGHT, float TEXT_BASE_WIDTH, int open_action)
        {
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Columns flags")) return;

            // Create a first table just to show all the options/flags we want to make visible in our example!
            if (BeginTable("table_columns_flags_checkboxes", column_count, ImGuiTableFlags.None))
            {
                PushStyleCompact();
                for (int column = 0; column < column_count; column++)
                {
                    TableNextColumn();
                    PushID(column);
                    AlignTextToFramePadding(); // FIXME-TABLE: Workaround for wrong text baseline propagation across columns
                    Text($"'{column_names[column]}'");
                    Spacing();
                    Text("Input flags:");
                    CheckboxesTableColumnFlags(ref column_flags[column]);
                    Spacing();
                    Text("Output flags:");
                    CheckboxesReadOnlyTableColumnStatus(column_flags_out[column]);
                    PopID();
                }
                PopStyleCompact();
                EndTable();
            }

            // Create the real table we care about for the example!
            // We use a scrolling table to be able to showcase the difference between the _IsEnabled and _IsVisible flags above,
            // otherwise in a non-scrolling table columns are always visible (unless using ImGuiTableFlags_NoKeepColumnsVisible
            // + resizing the parent window down).
            const ImGuiTableFlags flags
                = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY
                | ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersV
                | ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Sortable;
            Vec2 outer_size = new(0.0f, TEXT_BASE_HEIGHT * 9);
            if (BeginTable("table_columns_flags", column_count, flags, outer_size))
            {
                bool has_angled_header = false;
                for (int column = 0; column < column_count; column++)
                {
                    has_angled_header |= column_flags[column].HasFlag(ImGuiTableColumnFlags.AngledHeader);
                    TableSetupColumn(column_names[column], column_flags[column]);
                }
                if (has_angled_header)
                    TableAngledHeadersRow();
                TableHeadersRow();
                for (int column = 0; column < column_count; column++)
                    column_flags_out[column] = TableGetColumnFlags(column);
                float indent_step = (float)((int)TEXT_BASE_WIDTH / 2);
                for (int row = 0; row < 8; row++)
                {
                    // Add some indentation to demonstrate usage of per-column IndentEnable/IndentDisable flags.
                    Indent(indent_step);
                    TableNextRow();
                    for (int column = 0; column < column_count; column++)
                    {
                        TableSetColumnIndex(column);
                        Text($"{(column == 0 ? "Indented" : "Hello")} {TableGetColumnName(column)}");
                    }
                }
                Unindent(indent_step * 8.0f);

                EndTable();
            }
            TreePop();
        }
    }

    private class ColumnWidthsSubsection
    {
        private ImGuiTableFlags flags1 = ImGuiTableFlags.Borders | ImGuiTableFlags.NoBordersInBodyUntilResize;
        private ImGuiTableFlags flags2 = ImGuiTableFlags.None;

        public void Update(float TEXT_BASE_WIDTH, int open_action)
        {
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Columns widths")) return;

            HelpMarker("Using TableSetupColumn() to setup default width.");

            PushStyleCompact();
            CheckboxFlags(ref flags1, ImGuiTableFlags.Resizable);
            CheckboxFlags(ref flags1, ImGuiTableFlags.NoBordersInBodyUntilResize);
            PopStyleCompact();
            if (BeginTable("table1", 3, flags1))
            {
                // We could also set ImGuiTableFlags_SizingFixedFit on the table and all columns will default to ImGuiTableColumnFlags_WidthFixed.
                TableSetupColumn("one", ImGuiTableColumnFlags.WidthFixed, 100.0f); // Default to 100.0f
                TableSetupColumn("two", ImGuiTableColumnFlags.WidthFixed, 200.0f); // Default to 200.0f
                TableSetupColumn("three", ImGuiTableColumnFlags.WidthFixed);       // Default to auto
                TableHeadersRow();
                for (int row = 0; row < 4; row++)
                {
                    TableNextRow();
                    for (int column = 0; column < 3; column++)
                    {
                        TableSetColumnIndex(column);
                        if (row == 0)
                            Text($"(w: {GetContentRegionAvail().X:00000.0}");
                        else
                            Text($"Hello {column},{row}");
                    }
                }
                EndTable();
            }

            HelpMarker(
                "Using TableSetupColumn() to setup explicit width.\n\nUnless _NoKeepColumnsVisible is set, "
                + "fixed columns with set width may still be shrunk down if there's not enough space in the host.");

            PushStyleCompact();
            CheckboxFlags(ref flags2, ImGuiTableFlags.NoKeepColumnsVisible);
            CheckboxFlags(ref flags2, ImGuiTableFlags.BordersInnerV);
            CheckboxFlags(ref flags2, ImGuiTableFlags.BordersOuterV);
            PopStyleCompact();
            if (BeginTable("table2", 4, (ImGuiTableFlags)flags2))
            {
                // We could also set ImGuiTableFlags_SizingFixedFit on the table and then all columns
                // will default to ImGuiTableColumnFlags_WidthFixed.
                TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 100.0f);
                TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, TEXT_BASE_WIDTH * 15.0f);
                TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, TEXT_BASE_WIDTH * 30.0f);
                TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, TEXT_BASE_WIDTH * 15.0f);
                for (int row = 0; row < 5; row++)
                {
                    TableNextRow();
                    for (int column = 0; column < 4; column++)
                    {
                        TableSetColumnIndex(column);
                        if (row == 0)
                            Text($"(w: {GetContentRegionAvail().X:00000.0})");
                        else
                            Text($"Hello {column},{row}");
                    }
                }
                EndTable();
            }
            TreePop();
        }
    }

    private class OuterSizeSubsection
    {
        private ImGuiTableFlags flags = ImGuiTableFlags.Borders
            | ImGuiTableFlags.Resizable
            | ImGuiTableFlags.ContextMenuInBody
            | ImGuiTableFlags.RowBg
            | ImGuiTableFlags.SizingFixedFit
            | ImGuiTableFlags.NoHostExtendX;

        public void Update(float TEXT_BASE_HEIGHT, float TEXT_BASE_WIDTH, int open_action)
        {
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Outer size")) return;

            // Showcasing use of ImGuiTableFlags_NoHostExtendX and ImGuiTableFlags_NoHostExtendY
            // Important to that note how the two flags have slightly different behaviors!
            Text("Using NoHostExtendX and NoHostExtendY:");
            PushStyleCompact();

            CheckboxFlags(ref flags, ImGuiTableFlags.NoHostExtendX);
            SameLine(); HelpMarker("Make outer width auto-fit to columns, overriding outer_size.x value.\n\nOnly available when ScrollX/ScrollY are disabled and Stretch columns are not used.");
            CheckboxFlags(ref flags, ImGuiTableFlags.NoHostExtendY);
            SameLine(); HelpMarker("Make outer height stop exactly at outer_size.y (prevent auto-extending table past the limit).\n\nOnly available when ScrollX/ScrollY are disabled. Data below the limit will be clipped and not visible.");
            PopStyleCompact();

            Vec2 outer_size = new(0.0f, TEXT_BASE_HEIGHT * 5.5f);
            if (BeginTable("table1", 3, flags, outer_size))
            {
                for (int row = 0; row < 10; row++)
                {
                    TableNextRow();
                    for (int column = 0; column < 3; column++)
                    {
                        TableNextColumn();
                        Text($"Cell {column},{row}");
                    }
                }
                EndTable();
            }
            SameLine();
            Text("Hello!");

            Spacing();

            Text("Using explicit size:");
            if (BeginTable("table2", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg, new(TEXT_BASE_WIDTH * 30, 0.0f)))
            {
                for (int row = 0; row < 5; row++)
                {
                    TableNextRow();
                    for (int column = 0; column < 3; column++)
                    {
                        TableNextColumn();
                        Text($"Cell {column},{row}");
                    }
                }
                EndTable();
            }
            SameLine();
            if (BeginTable("table3", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg, new(TEXT_BASE_WIDTH * 30, 0.0f)))
            {
                for (int row = 0; row < 3; row++)
                {
                    TableNextRow(0, TEXT_BASE_HEIGHT * 1.5f);
                    for (int column = 0; column < 3; column++)
                    {
                        TableNextColumn();
                        Text($"Cell {column},{row}");
                    }
                }
                EndTable();
            }

            TreePop();
        }
    }

    private class BackgroundColorSubsection
    {
        private ImGuiTableFlags flags = ImGuiTableFlags.RowBg;
        private int row_bg_type = 1;
        private int row_bg_target = 1;
        private int cell_bg_type = 1;

        public void Update(int open_action)
        {
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Background color")) return;

            PushStyleCompact();
            CheckboxFlags(ref flags, ImGuiTableFlags.Borders);
            CheckboxFlags(ref flags, ImGuiTableFlags.RowBg);
            SameLine(); HelpMarker("ImGuiTableFlags_RowBg automatically sets RowBg0 to alternative colors pulled from the Style.");
            Combo("row bg type", ref row_bg_type, "None\0Red\0Gradient\0");
            Combo("row bg target", ref row_bg_target, "RowBg0\0RowBg1\0"); SameLine(); HelpMarker("Target RowBg0 to override the alternating odd/even colors,\nTarget RowBg1 to blend with them.");
            Combo("cell bg type", ref cell_bg_type, "None\0Blue\0"); SameLine(); HelpMarker("We are colorizing cells to B1->C2 here.");
            Debug.Assert(row_bg_type >= 0 && row_bg_type <= 2);
            Debug.Assert(row_bg_target >= 0 && row_bg_target <= 1);
            Debug.Assert(cell_bg_type >= 0 && cell_bg_type <= 1);
            PopStyleCompact();

            if (BeginTable("table1", 5, flags))
            {
                for (int row = 0; row < 6; row++)
                {
                    TableNextRow();

                    // Demonstrate setting a row background color with 'ImGui::TableSetBgColor(ImGuiTableBgTarget_RowBgX, ...)'
                    // We use a transparent color so we can see the one behind in case our target is RowBg1 and RowBg0 was already targeted by the ImGuiTableFlags_RowBg flag.
                    if (row_bg_type != 0)
                    {
                        Color row_bg_color = row_bg_type == 1 ? new(0.7f, 0.3f, 0.3f, 0.65f) : new(0.2f + row * 0.1f, 0.2f, 0.2f, 0.65f); // Flat or Gradient?
                        TableSetBgColor(ImGuiTableBgTarget.RowBg0 + row_bg_target, row_bg_color.PackedValue);
                    }

                    // Fill cells
                    for (int column = 0; column < 5; column++)
                    {
                        TableSetColumnIndex(column);
                        Text($"{'A' + row}{'0' + column}");

                        // Change background of Cells B1->C2
                        // Demonstrate setting a cell background color with 'ImGui::TableSetBgColor(ImGuiTableBgTarget_CellBg, ...)'
                        // (the CellBg color will be blended over the RowBg and ColumnBg colors)
                        // We can also pass a column number as a third parameter to TableSetBgColor() and do this outside the column loop.
                        if (row >= 1 && row <= 2 && column >= 1 && column <= 2 && cell_bg_type == 1)
                        {
                            Color cell_bg_color = new(0.3f, 0.3f, 0.7f, 0.65f);
                            TableSetBgColor(ImGuiTableBgTarget.CellBg, cell_bg_color.PackedValue);
                        }
                    }
                }
                EndTable();
            }
            TreePop();
        }
    }

    private class TreeViewSubsection
    {
        private readonly MyTreeNode[] nodes =
        [
            new("Root with Long Name",           "Folder",      -1,      1,  3), // 0
            new("Music",                         "Folder",      -1,      4,  2), // 1
            new("Textures",                      "Folder",      -1,      6,  3), // 2
            new("desktop.ini",                   "System file", 1024,   -1, -1), // 3
            new("File1_a.wav",                   "Audio file",  123000, -1, -1), // 4
            new("File1_b.wav",                   "Audio file",  456000, -1, -1), // 5
            new("Image001.png",                  "Image file",  203128, -1, -1), // 6
            new("Copy of Image001.png",          "Image file",  203256, -1, -1), // 7
            new("Copy of Image001 (Final2).png", "Image file",  203512, -1, -1), // 8
        ];

        private readonly ImGuiTableFlags table_flags = ImGuiTableFlags.BordersV
            | ImGuiTableFlags.BordersOuterH
            | ImGuiTableFlags.Resizable
            | ImGuiTableFlags.RowBg
            | ImGuiTableFlags.NoBordersInBody;

        private ImGuiTreeNodeFlags tree_node_flags =
            ImGuiTreeNodeFlags.SpanAllColumns
            // | ImGuiTreeNodeFlags.DrawLinesFull;
            | ImGuiTreeNodeFlags.DefaultOpen;

        public void Update(float TEXT_BASE_WIDTH, int open_action)
        {
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Tree view")) return;

            CheckboxFlags(ref tree_node_flags, ImGuiTreeNodeFlags.SpanFullWidth);
            CheckboxFlags(ref tree_node_flags, ImGuiTreeNodeFlags.SpanTextWidth);
            CheckboxFlags(ref tree_node_flags, ImGuiTreeNodeFlags.SpanAllColumns);
            CheckboxFlags(ref tree_node_flags, ImGuiTreeNodeFlags.SpanAvailWidth);
            SameLine(); HelpMarker("Useful if you know that you aren't displaying contents in other columns");

            HelpMarker("See \"Columns flags\" section to configure how indentation is applied to individual columns.");
            if (BeginTable("3ways", 3, table_flags))
            {
                // The first column will use the default _WidthStretch when ScrollX is Off and _WidthFixed when ScrollX is On
                TableSetupColumn("Name", ImGuiTableColumnFlags.NoHide);
                TableSetupColumn("Size", ImGuiTableColumnFlags.WidthFixed, TEXT_BASE_WIDTH * 12.0f);
                TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, TEXT_BASE_WIDTH * 18.0f);
                TableHeadersRow();

                DisplayNode(0);

                EndTable();
            }

            TreePop();
        }

        private void DisplayNode(int nodeIndex)
        {
            var node = nodes[nodeIndex];

            TableNextRow();
            TableNextColumn();
            bool is_folder = node.ChildCount > 0;

            ImGuiTreeNodeFlags node_flags = tree_node_flags;
            if (node != nodes[0])
                node_flags &= ~ImGuiTreeNodeFlags.SpanAvailWidth; // Only demonstrate this on the root node.

            if (is_folder)
            {
                bool open = TreeNodeEx(node.Name, node_flags);
                if ((node_flags & ImGuiTreeNodeFlags.SpanAvailWidth) == 0)
                {
                    TableNextColumn();
                    TextDisabled("--");
                    TableNextColumn();
                    TextUnformatted(node.Type);
                }
                if (open)
                {
                    for (int child_n = 0; child_n < node.ChildCount; child_n++)
                        DisplayNode(node.ChildIdx + child_n);
                    TreePop();
                }
            }
            else
            {
                TreeNodeEx(node.Name, node_flags | ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen);
                TableNextColumn();
                Text(node.Size.ToString());
                TableNextColumn();
                TextUnformatted(node.Type);
            }
        }

        private record MyTreeNode(string Name, string Type, int Size, int ChildIdx, int ChildCount);
    }

    private class ItemWidthSubsection
    {
        private float dummy_f = 0.0f;

        public void Update(float TEXT_BASE_WIDTH, int open_action)
        {
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Item width")) return;

            HelpMarker(
                "Showcase using PushItemWidth() and how it is preserved on a per-column basis.\n\n"
                + "Note that on auto-resizing non-resizable fixed columns, querying the content width for "
                + "e.g. right-alignment doesn't make sense.");
            if (BeginTable("table_item_width", 3, ImGuiTableFlags.Borders))
            {
                TableSetupColumn("small");
                TableSetupColumn("half");
                TableSetupColumn("right-align");
                TableHeadersRow();

                for (int row = 0; row < 3; row++)
                {
                    TableNextRow();
                    if (row == 0)
                    {
                        // Setup ItemWidth once (instead of setting up every time, which is also possible but less efficient)
                        TableSetColumnIndex(0);
                        PushItemWidth(TEXT_BASE_WIDTH * 3.0f); // Small
                        TableSetColumnIndex(1);
                        PushItemWidth(-GetContentRegionAvail().X * 0.5f);
                        TableSetColumnIndex(2);
                        PushItemWidth(-float.Epsilon); // Right-aligned
                    }

                    // Draw our contents
                    PushID(row);
                    TableSetColumnIndex(0);
                    SliderFloat("float0", ref dummy_f, 0.0f, 1.0f);
                    TableSetColumnIndex(1);
                    SliderFloat("float1", ref dummy_f, 0.0f, 1.0f);
                    TableSetColumnIndex(2);
                    SliderFloat("##float2", ref dummy_f, 0.0f, 1.0f); // No visible label since right-aligned
                    PopID();
                }
                EndTable();
            }
            TreePop();
        }
    }

    private class CustomHeadersSubsection
    {
        // Dummy entire-column selection storage
        // FIXME: It would be nice to actually demonstrate full-featured selection using those checkbox.
        private readonly bool[] column_selected = new bool[3];

        public void Update(int open_action)
        {
            // Demonstrate using TableHeader() calls instead of TableHeadersRow()
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Custom headers")) return;

            const int COLUMNS_COUNT = 3;
            if (BeginTable("table_custom_headers", COLUMNS_COUNT, ImGuiTableFlags.Borders | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable))
            {
                TableSetupColumn("Apricot");
                TableSetupColumn("Banana");
                TableSetupColumn("Cherry");

                // Instead of calling TableHeadersRow() we'll submit custom headers ourselves.
                // (A different approach is also possible:
                //    - Specify ImGuiTableColumnFlags_NoHeaderLabel in some TableSetupColumn() call.
                //    - Call TableHeadersRow() normally. This will submit TableHeader() with no name.
                //    - Then call TableSetColumnIndex() to position yourself in the column and submit your stuff e.g. Checkbox().)
                TableNextRow(ImGuiTableRowFlags.Headers);
                for (int column = 0; column < COLUMNS_COUNT; column++)
                {
                    TableSetColumnIndex(column);
                    var column_name = TableGetColumnName(column); // Retrieve name passed to TableSetupColumn()
                    PushID(column);
                    PushStyleVar(ImGuiStyleVar.FramePadding, new Vec2(0, 0));
                    Checkbox("##checkall", ref column_selected[column]);
                    PopStyleVar();
                    SameLine(0.0f, GetStyle().ItemInnerSpacing.X);
                    TableHeader(column_name);
                    PopID();
                }

                // Submit table contents
                for (int row = 0; row < 5; row++)
                {
                    TableNextRow();
                    for (int column = 0; column < 3; column++)
                    {
                        TableSetColumnIndex(column);
                        Selectable($"Cell {column},{row}", column_selected[column]);
                    }
                }
                EndTable();
            }
            TreePop();
        }
    }

    private class AngledHeadersSubsection
    {
        private const int columns_count = 14;
        private const int rows_count = 12;

        private readonly string[] column_names = ["Track", "cabasa", "ride", "smash", "tom-hi", "tom-mid", "tom-low", "hihat-o", "hihat-c", "snare-s", "snare-c", "clap", "rim", "kick"];

        private ImGuiTableFlags table_flags = ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.ScrollX | ImGuiTableFlags.ScrollY | ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.Hideable | ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.HighlightHoveredColumn;
        private ImGuiTableColumnFlags column_flags = ImGuiTableColumnFlags.AngledHeader | ImGuiTableColumnFlags.WidthFixed;
        private readonly bool[] bools = new bool[columns_count * rows_count]; // Dummy storage selection storage
        private int frozen_cols = 1;
        private int frozen_rows = 2;

        public void Update(float TEXT_BASE_HEIGHT, int open_action)
        {
            // Demonstrate using ImGuiTableColumnFlags_AngledHeader flag to create angled headers
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Angled headers")) return;

            CheckboxFlags(ref table_flags, ImGuiTableFlags.ScrollX);
            CheckboxFlags(ref table_flags, ImGuiTableFlags.ScrollY);
            CheckboxFlags(ref table_flags, ImGuiTableFlags.Resizable);
            CheckboxFlags(ref table_flags, ImGuiTableFlags.Sortable);
            CheckboxFlags(ref table_flags, ImGuiTableFlags.NoBordersInBody);
            CheckboxFlags(ref table_flags, ImGuiTableFlags.HighlightHoveredColumn);
            SetNextItemWidth(GetFontSize() * 8);
            SliderInt("Frozen columns", ref frozen_cols, 0, 2);
            SetNextItemWidth(GetFontSize() * 8);
            SliderInt("Frozen rows", ref frozen_rows, 0, 2);
            CheckboxFlags("Disable header contributing to column width", ref column_flags, ImGuiTableColumnFlags.NoHeaderWidth);

            if (TreeNode("Style settings"))
            {
                SameLine();
                HelpMarker("Giving access to some ImGuiStyle value in this demo for convenience.");
                SetNextItemWidth(GetFontSize() * 8);
                SliderAngle("style.TableAngledHeadersAngle", ref GetStyle().TableAngledHeadersAngle, -50.0f, +50.0f);
                SetNextItemWidth(GetFontSize() * 8);
                SliderFloat2("style.TableAngledHeadersTextAlign", ref GetStyle().TableAngledHeadersTextAlign, 0.0f, 1.0f, "%.2f");
                TreePop();
            }

            if (BeginTable("table_angled_headers", columns_count, table_flags, new Vec2(0.0f, TEXT_BASE_HEIGHT * 12)))
            {
                TableSetupColumn(column_names[0], ImGuiTableColumnFlags.NoHide | ImGuiTableColumnFlags.NoReorder);
                for (int n = 1; n < columns_count; n++)
                    TableSetupColumn(column_names[n], column_flags);
                TableSetupScrollFreeze(frozen_cols, frozen_rows);

                TableAngledHeadersRow(); // Draw angled headers for all columns with the ImGuiTableColumnFlags_AngledHeader flag.
                TableHeadersRow();       // Draw remaining headers and allow access to context-menu and other functions.
                for (int row = 0; row < rows_count; row++)
                {
                    PushID(row);
                    TableNextRow();
                    TableSetColumnIndex(0);
                    AlignTextToFramePadding();
                    Text($"Track {row}");
                    for (int column = 1; column < columns_count; column++)
                        if (TableSetColumnIndex(column))
                        {
                            PushID(column);
                            Checkbox("", ref bools[row * columns_count + column]);
                            PopID();
                        }
                    PopID();
                }
                EndTable();
            }
            TreePop();
        }
    }

    private class ContextMenusSubsection
    {
        private ImGuiTableFlags flags1 = ImGuiTableFlags.Resizable | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Borders | ImGuiTableFlags.ContextMenuInBody;
        private readonly ImGuiTableFlags flags2 = ImGuiTableFlags.Resizable | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Reorderable | ImGuiTableFlags.Hideable | ImGuiTableFlags.Borders;

        public void Update(int open_action)
        {
            // Demonstrate creating custom context menus inside columns,
            // while playing it nice with context menus provided by TableHeadersRow()/TableHeader()
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Context menus")) return;

            HelpMarker(
                "By default, right-clicking over a TableHeadersRow()/TableHeader() line will open the default context-menu.\n"
                + "Using ImGuiTableFlags_ContextMenuInBody we also allow right-clicking over columns body.");

            PushStyleCompact();
            CheckboxFlags(ref flags1, ImGuiTableFlags.ContextMenuInBody);
            PopStyleCompact();

            // Context Menus: first example
            // [1.1] Right-click on the TableHeadersRow() line to open the default table context menu.
            // [1.2] Right-click in columns also open the default table context menu (if ImGuiTableFlags_ContextMenuInBody is set)
            const int COLUMNS_COUNT = 3;
            if (BeginTable("table_context_menu", COLUMNS_COUNT, flags1))
            {
                TableSetupColumn("One");
                TableSetupColumn("Two");
                TableSetupColumn("Three");

                // [1.1]] Right-click on the TableHeadersRow() line to open the default table context menu.
                TableHeadersRow();

                // Submit dummy contents
                for (int row = 0; row < 4; row++)
                {
                    TableNextRow();
                    for (int column = 0; column < COLUMNS_COUNT; column++)
                    {
                        TableSetColumnIndex(column);
                        Text($"Cell {column},{row}");
                    }
                }
                EndTable();
            }

            // Context Menus: second example
            // [2.1] Right-click on the TableHeadersRow() line to open the default table context menu.
            // [2.2] Right-click on the ".." to open a custom popup
            // [2.3] Right-click in columns to open another custom popup
            HelpMarker(
                "Demonstrate mixing table context menu (over header), item context button (over button) "
                + "and custom per-colunm context menu (over column body).");

            if (BeginTable("table_context_menu_2", COLUMNS_COUNT, flags2))
            {
                TableSetupColumn("One");
                TableSetupColumn("Two");
                TableSetupColumn("Three");

                // [2.1] Right-click on the TableHeadersRow() line to open the default table context menu.
                TableHeadersRow();
                for (int row = 0; row < 4; row++)
                {
                    TableNextRow();
                    for (int column = 0; column < COLUMNS_COUNT; column++)
                    {
                        // Submit dummy contents
                        TableSetColumnIndex(column);
                        Text($"Cell {column},{row}");
                        SameLine();

                        // [2.2] Right-click on the ".." to open a custom popup
                        PushID(row * COLUMNS_COUNT + column);
                        SmallButton("..");
                        if (BeginPopupContextItem())
                        {
                            Text($"This is the popup for Button(\"..\") in Cell {column},{row}");
                            if (Button("Close"))
                                CloseCurrentPopup();
                            EndPopup();
                        }
                        PopID();
                    }
                }

                // [2.3] Right-click anywhere in columns to open another custom popup
                // (instead of testing for !IsAnyItemHovered() we could also call OpenPopup() with ImGuiPopupFlags_NoOpenOverExistingPopup
                // to manage popup priority as the popups triggers, here "are we hovering a column" are overlapping)
                int hovered_column = -1;
                for (int column = 0; column < COLUMNS_COUNT + 1; column++)
                {
                    PushID(column);
                    if ((TableGetColumnFlags(column) & ImGuiTableColumnFlags.IsHovered) != ImGuiTableColumnFlags.None)
                        hovered_column = column;
                    if (hovered_column == column && !IsAnyItemHovered() && IsMouseReleased(ImGuiMouseButton.Right))
                        OpenPopup("MyPopup");
                    if (BeginPopup("MyPopup"))
                    {
                        if (column == COLUMNS_COUNT)
                            Text("This is a custom popup for unused space after the last column.");
                        else
                            Text($"This is a custom popup for Column {column}");
                        if (Button("Close"))
                            CloseCurrentPopup();
                        EndPopup();
                    }
                    PopID();
                }

                EndTable();
                Text($"Hovered column: {hovered_column}");
            }
            TreePop();
        }
    }

    private class SyncedInstancesSubsection
    {
        private ImGuiTableFlags flags = ImGuiTableFlags.Resizable
            | ImGuiTableFlags.Reorderable
            | ImGuiTableFlags.Hideable
            | ImGuiTableFlags.Borders
            | ImGuiTableFlags.SizingFixedFit
            | ImGuiTableFlags.NoSavedSettings;

        public void Update(int open_action)
        {
            // Demonstrate creating multiple tables with the same ID
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Synced instances")) return;

            HelpMarker("Multiple tables with the same identifier will share their settings, width, visibility, order etc.");

            CheckboxFlags(ref flags, ImGuiTableFlags.Resizable);
            CheckboxFlags(ref flags, ImGuiTableFlags.ScrollY);
            CheckboxFlags(ref flags, ImGuiTableFlags.SizingFixedFit);
            CheckboxFlags(ref flags, ImGuiTableFlags.HighlightHoveredColumn);
            for (int n = 0; n < 3; n++)
            {
                bool open = CollapsingHeader($"Synced Table {n}", ImGuiTreeNodeFlags.DefaultOpen);
                if (open && BeginTable("Table", 3, flags, new Vec2(0.0f, GetTextLineHeightWithSpacing() * 5)))
                {
                    TableSetupColumn("One");
                    TableSetupColumn("Two");
                    TableSetupColumn("Three");
                    TableHeadersRow();
                    int cell_count = (n == 1) ? 27 : 9; // Make second table have a scrollbar to verify that additional decoration is not affecting column positions.
                    for (int cell = 0; cell < cell_count; cell++)
                    {
                        TableNextColumn();
                        Text($"this cell {cell}");
                    }
                    EndTable();
                }
            }
            TreePop();
        }
    }

    private class SortingSubsection
    {
        private static readonly string[] template_items_names =
        [
            "Banana", "Apple", "Cherry", "Watermelon", "Grapefruit", "Strawberry", "Mango",
            "Kiwi", "Orange", "Pineapple", "Blueberry", "Plum", "Coconut", "Pear", "Apricot"
        ];

        private ImGuiTableFlags flags = 
            ImGuiTableFlags.Resizable
            | ImGuiTableFlags.Reorderable
            | ImGuiTableFlags.Hideable
            | ImGuiTableFlags.Sortable
            | ImGuiTableFlags.SortMulti
            | ImGuiTableFlags.RowBg
            | ImGuiTableFlags.BordersOuter
            | ImGuiTableFlags.BordersV
            | ImGuiTableFlags.NoBordersInBody
            | ImGuiTableFlags.ScrollY;

        private readonly List<MyItem> items =
        [
            .. Enumerable.Range(0, 50).Select(n => new MyItem(
                n,
                template_items_names[n % template_items_names.Length],
                (n * n - n) % 20))
        ];

        public void Update(float TEXT_BASE_HEIGHT, int open_action)
        {
            // Demonstrate using Sorting facilities
            // This is a simplified version of the "Advanced" example, where we mostly focus on the code necessary to handle sorting.
            // Note that the "Advanced" example also showcase manually triggering a sort (e.g. if item quantities have been modified)
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Sorting")) return;

            // Options
            PushStyleCompact();
            CheckboxFlags(ref flags, ImGuiTableFlags.SortMulti);
            SameLine(); HelpMarker("When sorting is enabled: hold shift when clicking headers to sort on multiple column. TableGetSortSpecs() may return specs where (SpecsCount > 1).");
            CheckboxFlags(ref flags, ImGuiTableFlags.SortTristate);
            SameLine(); HelpMarker("When sorting is enabled: allow no sorting, disable default sorting. TableGetSortSpecs() may return specs where (SpecsCount == 0).");
            PopStyleCompact();

            if (BeginTable("table_sorting", 4, flags, new(0.0f, TEXT_BASE_HEIGHT * 15), 0.0f))
            {
                // Declare columns
                // We use the "user_id" parameter of TableSetupColumn() to specify a user id that will be stored in the sort specifications.
                // This is so our sort function can identify a column given our own identifier. We could also identify them based on their index!
                // Demonstrate using a mixture of flags among available sort-related flags:
                // - ImGuiTableColumnFlags_DefaultSort
                // - ImGuiTableColumnFlags_NoSort / ImGuiTableColumnFlags_NoSortAscending / ImGuiTableColumnFlags_NoSortDescending
                // - ImGuiTableColumnFlags_PreferSortAscending / ImGuiTableColumnFlags_PreferSortDescending
                TableSetupColumn("ID", ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed, 0.0f, (uint)MyItemColumnID.ID);
                TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 0.0f, (uint)MyItemColumnID.Name);
                TableSetupColumn("Action", ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthFixed, 0.0f, (uint)MyItemColumnID.Action);
                TableSetupColumn("Quantity", ImGuiTableColumnFlags.PreferSortDescending | ImGuiTableColumnFlags.WidthStretch, 0.0f, (uint)MyItemColumnID.Quantity);
                TableSetupScrollFreeze(0, 1); // Make row always visible
                TableHeadersRow();

                // Sort our data if sort specs have been changed!
                ImGuiTableSortSpecsPtr sort_specs = TableGetSortSpecs();
                unsafe
                {
                    if (sort_specs.NativePtr != null && sort_specs.SpecsDirty)
                    {
                        items.Sort(new MyItemComparer(sort_specs));
                        sort_specs.SpecsDirty = false;
                    }
                }

                // Demonstrate using clipper for large vertical lists
                unsafe
                {
                    ImGuiListClipperPtr clipper = new(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
                    clipper.Begin(items.Count);
                    while (clipper.Step())
                        for (int row_n = clipper.DisplayStart; row_n < clipper.DisplayEnd; row_n++)
                        {
                            // Display a data item
                            var item = items[row_n];
                            PushID(item.Id);
                            TableNextRow();
                            TableNextColumn();
                            Text($"{item.Id:0000}");
                            TableNextColumn();
                            TextUnformatted(item.Name);
                            TableNextColumn();
                            SmallButton("None");
                            TableNextColumn();
                            Text($"{item.Quantity}");
                            PopID();
                        }
                    clipper.Destroy(); // should probably be in a finally block, really..
                }
                EndTable();
            }
            TreePop();
        }
    }

    private class AdvancedSubsection
    {
        private ImGuiTableFlags flags =
             ImGuiTableFlags.Resizable
             | ImGuiTableFlags.Reorderable
             | ImGuiTableFlags.Hideable
             | ImGuiTableFlags.Sortable
             | ImGuiTableFlags.SortMulti
             | ImGuiTableFlags.RowBg
             | ImGuiTableFlags.Borders
             | ImGuiTableFlags.NoBordersInBody
             | ImGuiTableFlags.ScrollX
             | ImGuiTableFlags.ScrollY
             | ImGuiTableFlags.SizingFixedFit;

        private ImGuiTableColumnFlags columns_base_flags = ImGuiTableColumnFlags.None;

        private enum ContentsType { Text, Button, SmallButton, FillButton, Selectable, SelectableSpanRow };

        private int contents_type = (int)ContentsType.SelectableSpanRow;
        private static readonly string[] contents_type_names = ["Text", "Button", "SmallButton", "FillButton", "Selectable", "Selectable (span row)"];
        private int freeze_cols = 1;
        private int freeze_rows = 1;
        private static readonly string[] template_items_names =
        [
            "Banana", "Apple", "Cherry", "Watermelon", "Grapefruit", "Strawberry", "Mango",
            "Kiwi", "Orange", "Pineapple", "Blueberry", "Plum", "Coconut", "Pear", "Apricot"
        ];
        private int items_count = template_items_names.Length * 2;
        private float row_min_height = 0.0f; // Auto
        private float inner_width_with_scroll = 0.0f; // Auto-extend
        private bool outer_size_enabled = true;
        private bool show_headers = true;
        private bool show_wrapped_text = false;
        static readonly List<MyItem> items = [];
        static readonly List<int> selection = [];
        static bool items_need_sort = false;
        private bool show_debug_details = false;
        //private ImGuiTextFilter filter;

        public void Update(float TEXT_BASE_HEIGHT, float TEXT_BASE_WIDTH, int open_action)
        {
            // In this example we'll expose most table flags and settings.
            // For specific flags and settings refer to the corresponding section for more detailed explanation.
            // This section is mostly useful to experiment with combining certain flags or settings with each others.
            //ImGui::SetNextItemOpen(true, ImGuiCond_Once); // [DEBUG]
            if (open_action != -1) SetNextItemOpen(open_action != 0);
            if (!TreeNode("Advanced")) return;

            Vec2 advanced_outer_size_value = new(0.0f, TEXT_BASE_HEIGHT * 12);

            //ImGui::SetNextItemOpen(true, ImGuiCond_Once); // FIXME-TABLE: Enabling this results in initial clipped first pass on table which tend to affect column sizing

            if (TreeNode("Options"))
            {
                // Make the UI compact because there are so many fields
                PushStyleCompact();
                PushItemWidth(TEXT_BASE_WIDTH * 28.0f);

                if (TreeNodeEx("Features:", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    CheckboxFlags(ref flags, ImGuiTableFlags.Resizable);
                    CheckboxFlags(ref flags, ImGuiTableFlags.Reorderable);
                    CheckboxFlags(ref flags, ImGuiTableFlags.Hideable);
                    CheckboxFlags(ref flags, ImGuiTableFlags.Sortable);
                    CheckboxFlags(ref flags, ImGuiTableFlags.NoSavedSettings);
                    CheckboxFlags(ref flags, ImGuiTableFlags.ContextMenuInBody);
                    TreePop();
                }

                if (TreeNodeEx("Decorations:", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    CheckboxFlags(ref flags, ImGuiTableFlags.RowBg);
                    CheckboxFlags(ref flags, ImGuiTableFlags.BordersV);
                    CheckboxFlags(ref flags, ImGuiTableFlags.BordersOuterV);
                    CheckboxFlags(ref flags, ImGuiTableFlags.BordersInnerV);
                    CheckboxFlags(ref flags, ImGuiTableFlags.BordersH);
                    CheckboxFlags(ref flags, ImGuiTableFlags.BordersOuterH);
                    CheckboxFlags(ref flags, ImGuiTableFlags.BordersInnerH);
                    CheckboxFlags(ref flags, ImGuiTableFlags.NoBordersInBody); SameLine(); HelpMarker("Disable vertical borders in columns Body (borders will always appear in Headers)");
                    CheckboxFlags(ref flags, ImGuiTableFlags.NoBordersInBodyUntilResize); SameLine(); HelpMarker("Disable vertical borders in columns Body until hovered for resize (borders will always appear in Headers)");
                    TreePop();
                }

                if (TreeNodeEx("Sizing:", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    ComboBoxTableSizingPolicy(ref flags);
                    SameLine(); HelpMarker("In the Advanced demo we override the policy of each column so those table-wide settings have less effect that typical.");
                    CheckboxFlags(ref flags, ImGuiTableFlags.NoHostExtendX);
                    SameLine(); HelpMarker("Make outer width auto-fit to columns, overriding outer_size.x value.\n\nOnly available when ScrollX/ScrollY are disabled and Stretch columns are not used.");
                    CheckboxFlags(ref flags, ImGuiTableFlags.NoHostExtendY);
                    SameLine(); HelpMarker("Make outer height stop exactly at outer_size.y (prevent auto-extending table past the limit).\n\nOnly available when ScrollX/ScrollY are disabled. Data below the limit will be clipped and not visible.");
                    CheckboxFlags(ref flags, ImGuiTableFlags.NoKeepColumnsVisible);
                    SameLine(); HelpMarker("Only available if ScrollX is disabled.");
                    CheckboxFlags(ref flags, ImGuiTableFlags.PreciseWidths);
                    SameLine(); HelpMarker("Disable distributing remainder width to stretched columns (width allocation on a 100-wide table with 3 columns: Without this flag: 33,33,34. With this flag: 33,33,33). With larger number of columns, resizing will appear to be less smooth.");
                    CheckboxFlags(ref flags, ImGuiTableFlags.NoClip);
                    SameLine(); HelpMarker("Disable clipping rectangle for every individual columns (reduce draw command count, items will be able to overflow into other columns). Generally incompatible with ScrollFreeze options.");
                    TreePop();
                }

                if (TreeNodeEx("Padding:", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    CheckboxFlags(ref flags, ImGuiTableFlags.PadOuterX);
                    CheckboxFlags(ref flags, ImGuiTableFlags.NoPadOuterX);
                    CheckboxFlags(ref flags, ImGuiTableFlags.NoPadInnerX);
                    TreePop();
                }

                if (TreeNodeEx("Scrolling:", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    CheckboxFlags(ref flags, ImGuiTableFlags.ScrollX);
                    SameLine();
                    SetNextItemWidth(GetFrameHeight());
                    DragInt("freeze_cols", ref freeze_cols, 0.2f, 0, 9, null, ImGuiSliderFlags.NoInput);
                    CheckboxFlags(ref flags, ImGuiTableFlags.ScrollY);
                    SameLine();
                    SetNextItemWidth(GetFrameHeight());
                    DragInt("freeze_rows", ref freeze_rows, 0.2f, 0, 9, null, ImGuiSliderFlags.NoInput);
                    TreePop();
                }

                if (TreeNodeEx("Sorting:", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    CheckboxFlags(ref flags, ImGuiTableFlags.SortMulti);
                    SameLine(); HelpMarker("When sorting is enabled: hold shift when clicking headers to sort on multiple column. TableGetSortSpecs() may return specs where (SpecsCount > 1).");
                    CheckboxFlags(ref flags, ImGuiTableFlags.SortTristate);
                    SameLine(); HelpMarker("When sorting is enabled: allow no sorting, disable default sorting. TableGetSortSpecs() may return specs where (SpecsCount == 0).");
                    TreePop();
                }

                if (TreeNodeEx("Headers:", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    Checkbox("Show headers", ref show_headers);
                    CheckboxFlags(ref flags, ImGuiTableFlags.HighlightHoveredColumn);
                    CheckboxFlags(ref columns_base_flags, ImGuiTableColumnFlags.AngledHeader);
                    SameLine(); HelpMarker("Enable AngledHeader on all columns. Best enabled on selected narrow columns (see \"Angled headers\" section of the demo).");
                    TreePop();
                }

                if (TreeNodeEx("Other:", ImGuiTreeNodeFlags.DefaultOpen))
                {
                    Checkbox("Show wrapped text", ref show_wrapped_text);

                    DragFloat2("##OuterSize", ref advanced_outer_size_value);
                    SameLine(0.0f, GetStyle().ItemInnerSpacing.X);
                    Checkbox("outer_size", ref outer_size_enabled);
                    SameLine();
                    HelpMarker("If scrolling is disabled (ScrollX and ScrollY not set):\n"
                        + "- The table is output directly in the parent window.\n"
                        + "- OuterSize.x < 0.0f will right-align the table.\n"
                        + "- OuterSize.x = 0.0f will narrow fit the table unless there are any Stretch columns.\n"
                        + "- OuterSize.y then becomes the minimum size for the table, which will extend vertically if there are more rows (unless NoHostExtendY is set).");

                    // From a user point of view we will tend to use 'inner_width' differently depending on whether our table is embedding scrolling.
                    // To facilitate toying with this demo we will actually pass 0.0f to the BeginTable() when ScrollX is disabled.
                    DragFloat("inner_width (when ScrollX active)", ref inner_width_with_scroll, 1.0f, 0.0f, float.MaxValue);

                    DragFloat("row_min_height", ref row_min_height, 1.0f, 0.0f, float.MaxValue);
                    SameLine(); HelpMarker("Specify height of the Selectable item.");

                    DragInt("items_count", ref items_count, 0.1f, 0, 9999);
                    Combo("items_type (first column)", ref contents_type, contents_type_names, contents_type_names.Length);
                    //filter.Draw("filter");
                    TreePop();
                }

                PopItemWidth();
                PopStyleCompact();
                Spacing();
                TreePop();
            }

            // Update item list if we changed the number of items
            if (items.Count != items_count)
            {
                while (items_count < items.Count)
                {
                    items.RemoveAt(items.Count - 1);
                }

                while (items_count > items.Count)
                {
                    int template_n = items.Count % template_items_names.Length;
                    items.Add(new MyItem(
                        items.Count,
                        template_items_names[template_n],
                        (template_n == 3) ? 10 : (template_n == 4) ? 20 : 0));
                }
            }

            ImDrawListPtr parent_draw_list = GetWindowDrawList();
            int parent_draw_list_draw_cmd_count = parent_draw_list.CmdBuffer.Size;
            Vec2 table_scroll_cur = Vec2.Zero, table_scroll_max = Vec2.Zero; // For debug display
            ImDrawListPtr table_draw_list = null;  // For debug display

            // Submit table
            float inner_width_to_use = flags.HasFlag(ImGuiTableFlags.ScrollX) ? inner_width_with_scroll : 0.0f;
            if (BeginTable("table_advanced", 6, flags, outer_size_enabled ? advanced_outer_size_value : new(0, 0), inner_width_to_use))
            {
                // Declare columns
                // We use the "user_id" parameter of TableSetupColumn() to specify a user id that will be stored in the sort specifications.
                // This is so our sort function can identify a column given our own identifier. We could also identify them based on their index!
                TableSetupColumn("ID", columns_base_flags | ImGuiTableColumnFlags.DefaultSort | ImGuiTableColumnFlags.WidthFixed | ImGuiTableColumnFlags.NoHide, 0.0f, (uint)MyItemColumnID.ID);
                TableSetupColumn("Name", columns_base_flags | ImGuiTableColumnFlags.WidthFixed, 0.0f, (uint)MyItemColumnID.Name);
                TableSetupColumn("Action", columns_base_flags | ImGuiTableColumnFlags.NoSort | ImGuiTableColumnFlags.WidthFixed, 0.0f, (uint)MyItemColumnID.Action);
                TableSetupColumn("Quantity", columns_base_flags | ImGuiTableColumnFlags.PreferSortDescending, 0.0f, (uint)MyItemColumnID.Quantity);
                TableSetupColumn("Description", columns_base_flags | (flags.HasFlag(ImGuiTableFlags.NoHostExtendX) ? 0 : ImGuiTableColumnFlags.WidthStretch), 0.0f, (uint)MyItemColumnID.Description);
                TableSetupColumn("Hidden", columns_base_flags | ImGuiTableColumnFlags.DefaultHide | ImGuiTableColumnFlags.NoSort);
                TableSetupScrollFreeze(freeze_cols, freeze_rows);

                // Sort our data if sort specs have been changed!
                ImGuiTableSortSpecsPtr sort_specs = TableGetSortSpecs();
                unsafe
                {
                    if (sort_specs.NativePtr != null && sort_specs.SpecsDirty)
                        items_need_sort = true;
                    if (sort_specs.NativePtr != null && items_need_sort && items.Count > 1)
                    {
                        items.Sort(new MyItemComparer(sort_specs));
                        sort_specs.SpecsDirty = false;
                    }

                    items_need_sort = false;
                }

                // Take note of whether we are currently sorting based on the Quantity field,
                // we will use this to trigger sorting when we know the data of this column has been modified.
                bool sorts_specs_using_quantity = TableGetColumnFlags(3).HasFlag(ImGuiTableColumnFlags.IsSorted);

                // Show headers
                if (show_headers && (columns_base_flags & ImGuiTableColumnFlags.AngledHeader) != 0)
                    TableAngledHeadersRow();
                if (show_headers)
                    TableHeadersRow();

                // Show data
                // FIXME-TABLE FIXME-NAV: How we can get decent up/down even though we have the buttons here?
#if true
                // Demonstrate using clipper for large vertical lists
                ImGuiListClipperPtr clipper;
                unsafe
                {
                    clipper = new(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
                }
                clipper.Begin(items.Count);
                while (clipper.Step())
                {
                    for (int row_n = clipper.DisplayStart; row_n < clipper.DisplayEnd; row_n++)
#else
                // Without clipper
                {
                    for (int row_n = 0; row_n < items.Size; row_n++)
#endif
                    {
                        MyItem item = items[row_n];
                        //if (!filter.PassFilter(item->Name))
                        //    continue;

                        bool item_is_selected = selection.Contains(item.Id);
                        PushID(item.Id);
                        TableNextRow(ImGuiTableRowFlags.None, row_min_height);

                        // For the demo purpose we can select among different type of items submitted in the first column
                        TableSetColumnIndex(0);
                        var label = $"{item.Id:0000}";
                        if (contents_type == (int)ContentsType.Text)
                            TextUnformatted(label);
                        else if (contents_type == (int)ContentsType.Button)
                            Button(label);
                        else if (contents_type == (int)ContentsType.SmallButton)
                            SmallButton(label);
                        else if (contents_type == (int)ContentsType.FillButton)
                            Button(label, new Vec2(-float.Epsilon, 0.0f));
                        else if (contents_type == (int)ContentsType.Selectable || contents_type == (int)ContentsType.SelectableSpanRow)
                        {
                            ImGuiSelectableFlags selectable_flags = (contents_type == (int)ContentsType.SelectableSpanRow) ? ImGuiSelectableFlags.SpanAllColumns | ImGuiSelectableFlags.AllowOverlap : ImGuiSelectableFlags.None;
                            if (Selectable(label, item_is_selected, selectable_flags, new Vec2(0, row_min_height)))
                            {
                                if (GetIO().KeyCtrl)
                                {
                                    if (item_is_selected)
                                        selection.Remove(item.Id);
                                    else
                                        selection.Add(item.Id);
                                }
                                else
                                {
                                    selection.Clear();
                                    selection.Add(item.Id);
                                }
                            }
                        }

                        if (TableSetColumnIndex(1))
                            TextUnformatted(item.Name);

                        // Here we demonstrate marking our data set as needing to be sorted again if we modified a quantity,
                        // and we are currently sorting on the column showing the Quantity.
                        // To avoid triggering a sort while holding the button, we only trigger it when the button has been released.
                        // You will probably need some extra logic if you want to automatically sort when a specific entry changes.
                        if (TableSetColumnIndex(2))
                        {
                            if (SmallButton("Chop")) { item.Quantity += 1; }
                            if (sorts_specs_using_quantity && IsItemDeactivated()) { items_need_sort = true; }
                            SameLine();
                            if (SmallButton("Eat")) { item.Quantity -= 1; }
                            if (sorts_specs_using_quantity && IsItemDeactivated()) { items_need_sort = true; }
                        }

                        if (TableSetColumnIndex(3))
                            Text($"{item.Quantity}");

                        TableSetColumnIndex(4);
                        if (show_wrapped_text)
                            TextWrapped("Lorem ipsum dolor sit amet");
                        else
                            Text("Lorem ipsum dolor sit amet");

                        if (TableSetColumnIndex(5))
                            Text("1234");

                        PopID();
                    }
                }

                clipper.Destroy(); // should probably be in a finally block, really..

                // Store some info to display debug details below
                table_scroll_cur = new Vec2(GetScrollX(), GetScrollY());
                table_scroll_max = new Vec2(GetScrollMaxX(), GetScrollMaxY());
                table_draw_list = GetWindowDrawList();
                EndTable();
            }

            Checkbox("Debug details", ref show_debug_details);
            unsafe
            {
                if (show_debug_details && table_draw_list.NativePtr != null)
                {
                    SameLine(0.0f, 0.0f);
                    int table_draw_list_draw_cmd_count = table_draw_list.CmdBuffer.Size;
                    if (table_draw_list.NativePtr == parent_draw_list.NativePtr)
                        Text($": DrawCmd: +{table_draw_list_draw_cmd_count - parent_draw_list_draw_cmd_count} (in same window)");
                    else
                        Text($": DrawCmd: +{table_draw_list_draw_cmd_count - 1} (in child window), Scroll: ({table_scroll_cur.X}/{table_scroll_max.X}) ({table_scroll_cur.Y}/{table_scroll_max.Y})");
                }
            }
            TreePop();
        }
    }

    private class MyItem(int id, string name, int quantity)
    {
        public int Id { get; set; } = id;

        public string Name { get; set; } = name;

        public int Quantity { get; set; } = quantity;
    }

    private class MyItemComparer(ImGuiTableSortSpecsPtr sortSpecs) : IComparer<MyItem>
    {
        public unsafe int Compare(MyItem x, MyItem y)
        {
            for (int n = 0; n < sortSpecs.SpecsCount; n++)
            {
                // Here we identify columns using the ColumnUserID value that we ourselves passed to TableSetupColumn()
                // We could also choose to identify columns based on their index (sort_spec->ColumnIndex), which is simpler!
                ImGuiTableColumnSortSpecsPtr sort_spec = new(&sortSpecs.Specs.NativePtr[n]);

                int delta = sort_spec.ColumnUserID switch
                {
                    (uint)MyItemColumnID.ID => x.Id - y.Id,
                    (uint)MyItemColumnID.Name => string.Compare(x.Name, y.Name),
                    (uint)MyItemColumnID.Quantity => x.Quantity - y.Quantity,
                    (uint)MyItemColumnID.Description => string.Compare(x.Name, y.Name),
                    _ => throw new InvalidOperationException("unexpected sort spec"),
                };

                if (delta > 0)
                    return (sort_spec.SortDirection == ImGuiSortDirection.Ascending) ? +1 : -1;
                if (delta < 0)
                    return (sort_spec.SortDirection == ImGuiSortDirection.Ascending) ? -1 : +1;
            }

            // fall back on ID, which is "guaranteed" to differ - for predictability in case of unstable sort algs.
            return x.Id - y.Id;
        }
    }

    // We are passing our own identifier to TableSetupColumn() to facilitate identifying columns in the sorting code.
    // This identifier will be passed down into ImGuiTableSortSpec::ColumnUserID.
    // But it is possible to omit the user id parameter of TableSetupColumn() and just use the column index instead! (ImGuiTableSortSpec::ColumnIndex)
    // If you don't use sorting, you will generally never care about giving column an ID!
    private enum MyItemColumnID
    {
        ID,
        Name,
        Action,
        Quantity,
        Description
    };

    private record TableSizingPolicyDesc(ImGuiTableFlags Value, string Name, string Tooltip);
}
