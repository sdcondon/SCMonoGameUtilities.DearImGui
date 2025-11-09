using ImGuiNET;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static ImGuiNET.ImGui;
using static SCMonoGameUtilities.DearImGui.Demos.GuiElements.GuiElementHelpers;
using Vector2 = System.Numerics.Vector2;

namespace SCMonoGameUtilities.DearImGui.Demos.GuiElements.MiniApps;

// "Asset browser" mini-app that demonstrates a set of selectable
// (and multi-selectable, and deletable) icons.
//
// This is a port of the original native code found here:
// https://github.com/ocornut/imgui/blob/master/imgui_demo.cpp#L10459
// https://github.com/ocornut/imgui/blob/master/imgui_demo.cpp#L2427
class AssetsBrowserWindow(bool isOpen = false)
{
    public bool IsOpen = isOpen;

    // Options
    private bool showTypeOverlay = true;
    private bool allowSorting = true;
    private bool allowDragUnselected = false;
    private bool allowBoxSelect = true;
    private float iconSize = 32.0f;
    private int iconSpacing = 10;
    private int iconHitSpacing = 4; // Increase hit-spacing if you want to make it possible to clear or box-select from gaps. Some spacing is required to able to amend with Shift+box-select. Value is small in Explorer.
    private bool stretchSpacing = true;

    // State
    private readonly List<ExampleAsset> items = []; // Our items
    private readonly unsafe ImGuiSelectionBasicStoragePtr selection = new(ImGuiNative.ImGuiSelectionBasicStorage_ImGuiSelectionBasicStorage());
    private uint nextItemId = 0; // Unique identifier when creating new items
    private bool requestDelete = false; // Deferred deletion request
    private bool requestSort = false; // Deferred sort request
    private float zoomWheelAccum = 0.0f; // Mouse wheel accumulator to handle smooth wheels better

    // Calculated sizes for layout, output of UpdateLayoutSizes(). Could be locals but our code is simpler this way.
    private Vector2 layoutItemSize;
    private Vector2 layoutItemStep; // == LayoutItemSize + LayoutItemSpacing
    private float layoutItemSpacing = 0.0f;
    private float layoutSelectableSpacing = 0.0f;
    private float layoutOuterPadding = 0.0f;
    private int layoutColumnCount = 0;
    private int layoutLineCount = 0;

    private delegate uint SelectionAdapter(ImGuiSelectionBasicStoragePtr storage, int idx);

    ~AssetsBrowserWindow() => selection.Destroy();

    public void Update()
    {
        if (!IsOpen) return;

        SetNextWindowSize(new(iconSize * 25, iconSize * 15), ImGuiCond.FirstUseEver);
        if (!Begin("Example: Assets Browser", ref IsOpen, ImGuiWindowFlags.MenuBar))
        {
            End();
            return;
        }

        // Menu bar
        if (BeginMenuBar())
        {
            if (BeginMenu("File"))
            {
                if (MenuItem("Add 10000 items"))
                    AddItems(10000);
                if (MenuItem("Clear items"))
                    ClearItems();
                Separator();
                if (MenuItem("Close"))
                    IsOpen = false;
                EndMenu();
            }
            if (BeginMenu("Edit"))
            {
                if (MenuItem("Delete", "Del", false, selection.Size > 0))
                    requestDelete = true;
                EndMenu();
            }
            if (BeginMenu("Options"))
            {
                PushItemWidth(GetFontSize() * 10);

                SeparatorText("Contents");
                Checkbox("Show Type Overlay", ref showTypeOverlay);
                Checkbox("Allow Sorting", ref allowSorting);

                SeparatorText("Selection Behavior");
                Checkbox("Allow dragging unselected item", ref allowDragUnselected);
                Checkbox("Allow box-selection", ref allowBoxSelect);

                SeparatorText("Layout");
                SliderFloat("Icon Size", ref iconSize, 16.0f, 128.0f, "%.0f");
                SameLine(); HelpMarker("Use CTRL+Wheel to zoom");
                SliderInt("Icon Spacing", ref iconSpacing, 0, 32);
                SliderInt("Icon Hit Spacing", ref iconHitSpacing, 0, 32);
                Checkbox("Stretch Spacing", ref stretchSpacing);
                PopItemWidth();
                EndMenu();
            }

            EndMenuBar();
        }

        // Show a table with ONLY one header row to showcase the idea/possibility of using this to provide a sorting UI
        if (allowSorting)
        {
            PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
            ImGuiTableFlags table_flags_for_sort_specs = ImGuiTableFlags.Sortable | ImGuiTableFlags.SortMulti | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.Borders;
            if (BeginTable("for_sort_specs_only", 2, table_flags_for_sort_specs, new(0.0f, GetFrameHeight())))
            {
                TableSetupColumn("Index");
                TableSetupColumn("Type");
                TableHeadersRow();
                ImGuiTableSortSpecsPtr sort_specs = TableGetSortSpecs();
                unsafe
                {
                    if (sort_specs.NativePtr != null && (sort_specs.SpecsDirty || requestSort))
                    {
                        items.Sort(new ExampleAssetComparer(sort_specs));
                        sort_specs.SpecsDirty = requestSort = false;
                    }
                }
                EndTable();
            }
            PopStyleVar();
        }

        ImGuiIOPtr io = GetIO();
        SetNextWindowContentSize(new(0.0f, layoutOuterPadding + layoutLineCount * (layoutItemSize.Y + layoutItemSpacing)));
        if (BeginChild("Assets", new(0.0f, -GetTextLineHeightWithSpacing()), ImGuiChildFlags.Borders, ImGuiWindowFlags.NoMove))
        {
            ImDrawListPtr draw_list = GetWindowDrawList();

            float avail_width = GetContentRegionAvail().X;
            UpdateLayoutSizes(avail_width);

            // Calculate and store start position.
            var start_pos = GetCursorScreenPos();
            start_pos = new(start_pos.X + layoutOuterPadding, start_pos.Y + layoutOuterPadding);
            SetCursorScreenPos(start_pos);

            // Multi-select
            ImGuiMultiSelectFlags ms_flags = ImGuiMultiSelectFlags.ClearOnEscape | ImGuiMultiSelectFlags.ClearOnClickVoid;

            // - Enable box-select (in 2D mode, so that changing box-select rectangle X1/X2 boundaries will affect clipped items)
            if (allowBoxSelect)
                ms_flags |= ImGuiMultiSelectFlags.BoxSelect2d;

            // - This feature allows dragging an unselected item without selecting it (rarely used)
            if (allowDragUnselected)
                ms_flags |= ImGuiMultiSelectFlags.SelectOnClickRelease;

            // - Enable keyboard wrapping on X axis
            // (FIXME-MULTISELECT: We haven't designed/exposed a general nav wrapping api yet, so this flag is provided as a courtesy to avoid doing:
            //    ImGui::NavMoveRequestTryWrapping(ImGui::GetCurrentWindow(), ImGuiNavMoveFlags_WrapX);
            // When we finish implementing a more general API for this, we will obsolete this flag in favor of the new system)
            ms_flags |= ImGuiMultiSelectFlags.NavWrapX;

            ImGuiMultiSelectIOPtr ms_io = BeginMultiSelect(ms_flags, selection.Size, items.Count);

            // Use custom selection adapter: store ID in selection (recommended)
            uint customSelectionAdapter(ImGuiSelectionBasicStoragePtr _, int idx) => items[idx].Id;
            selection.AdapterIndexToStorageId = Marshal.GetFunctionPointerForDelegate((SelectionAdapter)customSelectionAdapter);
            selection.ApplyRequests(ms_io);

            bool want_delete = (Shortcut(ImGuiKey.Delete, ImGuiInputFlags.Repeat) && (selection.Size > 0)) || requestDelete;
            int item_curr_idx_to_focus = want_delete ? ApplyDeletionPreLoop(selection, ms_io, items.Count) : -1;
            requestDelete = false;

            // Push LayoutSelectableSpacing (which is LayoutItemSpacing minus hit-spacing, if we decide to have hit gaps between items)
            // Altering style ItemSpacing may seem unnecessary as we position every items using SetCursorScreenPos()...
            // But it is necessary for two reasons:
            // - Selectables uses it by default to visually fill the space between two items.
            // - The vertical spacing would be measured by Clipper to calculate line height if we didn't provide it explicitly (here we do).
            PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(layoutSelectableSpacing, layoutSelectableSpacing));

            // Rendering parameters
            var icon_type_overlay_colors = new uint[] { 0, new Color(200, 70, 70, 255).PackedValue, new Color(70, 170, 70, 255).PackedValue };
            var icon_bg_color = new Color(35, 35, 35, 220).PackedValue;
            var icon_type_overlay_size = new Vector2(4.0f, 4.0f);
            var display_label = layoutItemSize.X >= CalcTextSize("999").X;

            int column_count = layoutColumnCount;
            ImGuiListClipperPtr clipper;
            unsafe
            {
                clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
            }
            clipper.Begin(layoutLineCount, layoutItemStep.Y);
            if (item_curr_idx_to_focus != -1)
                clipper.IncludeItemByIndex(item_curr_idx_to_focus / column_count); // Ensure focused item line is not clipped.
            if (ms_io.RangeSrcItem != -1)
                clipper.IncludeItemByIndex((int)ms_io.RangeSrcItem / column_count); // Ensure RangeSrc item line is not clipped.
            while (clipper.Step())
            {
                for (int line_idx = clipper.DisplayStart; line_idx < clipper.DisplayEnd; line_idx++)
                {
                    int item_min_idx_for_current_line = line_idx * column_count;
                    int item_max_idx_for_current_line = Math.Min((line_idx + 1) * column_count, items.Count);
                    for (int item_idx = item_min_idx_for_current_line; item_idx < item_max_idx_for_current_line; ++item_idx)
                    {
                        ExampleAsset item_data = items[item_idx];
                        PushID($"##{item_data.Id}");

                        // Position item
                        var pos = new Vector2(start_pos.X + (item_idx % column_count) * layoutItemStep.X, start_pos.Y + line_idx * layoutItemStep.Y);
                        SetCursorScreenPos(pos);

                        SetNextItemSelectionUserData(item_idx);
                        bool item_is_selected = selection.Contains(item_data.Id);
                        bool item_is_visible = IsRectVisible(layoutItemSize);
                        Selectable("", item_is_selected, ImGuiSelectableFlags.None, layoutItemSize);

                        // Update our selection state immediately (without waiting for EndMultiSelect() requests)
                        // because we use this to alter the color of our text/icon.
                        if (IsItemToggledSelection())
                            item_is_selected = !item_is_selected;

                        // Focus (for after deletion)
                        if (item_curr_idx_to_focus == item_idx)
                            SetKeyboardFocusHere(-1);

                        // Drag and drop
                        if (BeginDragDropSource())
                        {
                            // Create payload with full selection OR single unselected item.
                            // (the later is only possible when using ImGuiMultiSelectFlags_SelectOnClickRelease)
                            unsafe
                            {
                                if (GetDragDropPayload().NativePtr == null)
                                {
                                    List<uint> payload_items = [];
                                    void* it = null;
                                    uint id = 0;
                                    if (!item_is_selected)
                                        payload_items.Add((uint)item_data.Id);
                                    else
                                        while (selection.GetNextSelectedItem(ref it, out id))
                                            payload_items.Add(id);
                                    var payload_items_array = payload_items.ToArray();
                                    //// todo: SetDragDropPayload("ASSETS_BROWSER_ITEMS", new nint(&payload_items_array), (uint)(sizeof(uint) * payload_items.Count));
                                }
                            }

                            // Display payload content in tooltip, by extracting it from the payload data
                            // (we could read from selection, but it is more correct and reusable to read from payload)
                            ImGuiPayloadPtr payload = GetDragDropPayload();
                            int payload_count = payload.DataSize / sizeof(uint);
                            Text($"{payload_count} assets");

                            EndDragDropSource();
                        }

                        // Render icon (a real app would likely display an image/thumbnail here)
                        // Because we use ImGuiMultiSelectFlags_BoxSelect2d, clipping vertical may occasionally be larger, so we coarse-clip our rendering as well.
                        if (item_is_visible)
                        {
                            var box_min = new Vector2(pos.X -1, pos.Y - 1);
                            var box_max = new Vector2(box_min.X + layoutItemSize.X + 2, box_min.Y + layoutItemSize.Y + 2); // Dubious
                            draw_list.AddRectFilled(box_min, box_max, icon_bg_color); // Background color
                            if (showTypeOverlay && item_data.Type != 0)
                            {
                                var type_col = icon_type_overlay_colors[item_data.Type % icon_type_overlay_colors.Length];
                                draw_list.AddRectFilled(new Vector2(box_max.X - 2 - icon_type_overlay_size.X, box_min.Y + 2), new Vector2(box_max.X - 2, box_min.Y + 2 + icon_type_overlay_size.Y), type_col);
                            }
                            if (display_label)
                            {
                                var label_col = GetColorU32(item_is_selected ? ImGuiCol.Text : ImGuiCol.TextDisabled);
                                draw_list.AddText(new Vector2(box_min.X, box_max.Y - GetFontSize()), label_col, item_data.Id.ToString());
                            }
                        }

                        PopID();
                    }
                }
            }
            clipper.End();
            clipper.Destroy(); // should probably be in a finally block, really..

            PopStyleVar(); // ImGuiStyleVar_ItemSpacing

            // Context menu
            if (BeginPopupContextWindow())
            {
                Text($"Selection: {selection.Size} items");
                Separator();
                if (MenuItem("Delete", "Del", false, selection.Size > 0))
                    requestDelete = true;
                EndPopup();
            }

            ms_io = EndMultiSelect();
            selection.ApplyRequests(ms_io);
            if (want_delete)
                ApplyDeletionPostLoop(selection, ms_io, items, item_curr_idx_to_focus);

            // Zooming with CTRL+Wheel
            if (IsWindowAppearing())
                zoomWheelAccum = 0.0f;
            if (IsWindowHovered() && io.MouseWheel != 0.0f && IsKeyDown(ImGuiKey.ModCtrl) && IsAnyItemActive() == false)
            {
                zoomWheelAccum += io.MouseWheel;
                if (Math.Abs(zoomWheelAccum) >= 1.0f)
                {
                    // Calculate hovered item index from mouse location
                    // FIXME: Locking aiming on 'hovered_item_idx' (with a cool-down timer) would ensure zoom keeps on it.
                    float hovered_item_nx = (io.MousePos.X - start_pos.X + layoutItemSpacing * 0.5f) / layoutItemStep.X;
                    float hovered_item_ny = (io.MousePos.Y - start_pos.Y + layoutItemSpacing * 0.5f) / layoutItemStep.Y;
                    int hovered_item_idx = ((int)hovered_item_ny * layoutColumnCount) + (int)hovered_item_nx;
                    //ImGui::SetTooltip("%f,%f -> item %d", hovered_item_nx, hovered_item_ny, hovered_item_idx); // Move those 4 lines in block above for easy debugging

                    // Zoom
                    iconSize *= (float)Math.Pow(1.1f, (float)(int)zoomWheelAccum);
                    iconSize = Math.Clamp(iconSize, 16.0f, 128.0f);
                    zoomWheelAccum -= (int)zoomWheelAccum;
                    UpdateLayoutSizes(avail_width);

                    // Manipulate scroll to that we will land at the same Y location of currently hovered item.
                    // - Calculate next frame position of item under mouse
                    // - Set new scroll position to be used in next ImGui::BeginChild() call.
                    float hovered_item_rel_pos_y = ((float)(hovered_item_idx / layoutColumnCount) + hovered_item_ny % 1.0f) * layoutItemStep.Y;
                    hovered_item_rel_pos_y += GetStyle().WindowPadding.Y;
                    float mouse_local_y = io.MousePos.Y - GetWindowPos().Y;
                    SetScrollY(hovered_item_rel_pos_y - mouse_local_y);
                }
            }
        }
        EndChild();

        Text($"Selected: {selection.Size}/{items.Count} items");
        End();
    }

    public void AddItems(int count)
    {
        if (items.Count == 0)
            nextItemId = 0;

        for (int n = 0; n < count; n++, nextItemId++)
            items.Add(new ExampleAsset(nextItemId, (nextItemId % 20) < 15 ? 0u : (nextItemId % 20) < 18 ? 1u : 2u));

        requestSort = true;
    }

    public void ClearItems()
    {
        items.Clear();
        selection.Clear();
    }

    // Logic would be written in the main code BeginChild() and outputting to local variables.
    // We extracted it into a function so we can call it easily from multiple places.
    private void UpdateLayoutSizes(float avail_width)
    {
        // Layout: when not stretching: allow extending into right-most spacing.
        layoutItemSpacing = (float)iconSpacing;
        if (stretchSpacing == false)
            avail_width += (float)Math.Floor(layoutItemSpacing * 0.5f);

        // Layout: calculate number of icon per line and number of lines
        layoutItemSize = new((float)Math.Floor(iconSize), (float)Math.Floor(iconSize));
        layoutColumnCount = Math.Max((int)(avail_width / (layoutItemSize.X + layoutItemSpacing)), 1);
        layoutLineCount = (items.Count + layoutColumnCount - 1) / layoutColumnCount;

        // Layout: when stretching: allocate remaining space to more spacing. Round before division, so item_spacing may be non-integer.
        if (stretchSpacing && layoutColumnCount > 1)
            layoutItemSpacing = (float)Math.Floor(avail_width - layoutItemSize.X * layoutColumnCount) / layoutColumnCount;

        layoutItemStep = new(layoutItemSize.X + layoutItemSpacing, layoutItemSize.Y + layoutItemSpacing);
        layoutSelectableSpacing = Math.Max((float)Math.Floor(layoutItemSpacing) - iconHitSpacing, 0.0f);
        layoutOuterPadding = (float)Math.Floor(layoutItemSpacing * 0.5f);
    }

    // Find which item should be Focused after deletion.
    // Call _before_ item submission. Return an index in the before-deletion item list, your item loop should call SetKeyboardFocusHere() on it.
    // The subsequent ApplyDeletionPostLoop() code will use it to apply Selection.
    // - We cannot provide this logic in core Dear ImGui because we don't have access to selection data.
    // - We don't actually manipulate the ImVector<> here, only in ApplyDeletionPostLoop(), but using similar API for consistency and flexibility.
    // - Important: Deletion only works if the underlying ImGuiID for your items are stable: aka not depend on their index, but on e.g. item id/ptr.
    // FIXME-MULTISELECT: Doesn't take account of the possibility focus target will be moved during deletion. Need refocus or scroll offset.
    private static int ApplyDeletionPreLoop(ImGuiSelectionBasicStoragePtr storage, ImGuiMultiSelectIOPtr ms_io, int items_count)
    {
        if (storage.Size == 0)
            return -1;

        // If focused item is not selected...
        int focused_idx = (int)ms_io.NavIdItem;  // Index of currently focused item
        if (ms_io.NavIdSelected == false)  // This is merely a shortcut, == Contains(adapter->IndexToStorage(items, focused_idx))
        {
            ms_io.RangeSrcReset = true;    // Request to recover RangeSrc from NavId next frame. Would be ok to reset even when NavIdSelected==true, but it would take an extra frame to recover RangeSrc when deleting a selected item.
            return focused_idx;             // Request to focus same item after deletion.
        }

        // If focused item is selected: land on first unselected item after focused item.
        for (int idx = focused_idx + 1; idx < items_count; idx++)
            if (!storage.Contains(storage.GetStorageIdFromIndex(idx)))
                return idx;

        // If focused item is selected: otherwise return last unselected item before focused item.
        for (int idx = Math.Min(focused_idx, items_count) - 1; idx >= 0; idx--)
            if (!storage.Contains(storage.GetStorageIdFromIndex(idx)))
                return idx;

        return -1;
    }

    // Rewrite item list (delete items) + update selection.
    // - Call after EndMultiSelect()
    // - We cannot provide this logic in core Dear ImGui because we don't have access to your items, nor to selection data.
    private static void ApplyDeletionPostLoop<T>(ImGuiSelectionBasicStoragePtr storage, ImGuiMultiSelectIOPtr ms_io, List<T> items, int item_curr_idx_to_select)
    {
        // Rewrite item list (delete items) + convert old selection index (before deletion) to new selection index (after selection).
        // If NavId was not part of selection, we will stay on same item.
        List<T> new_items = [];
        int item_next_idx_to_select = -1;
        for (int idx = 0; idx < items.Count; idx++)
        {
            if (!storage.Contains(storage.GetStorageIdFromIndex(idx)))
                new_items.Add(items[idx]);
            if (item_curr_idx_to_select == idx)
                item_next_idx_to_select = new_items.Count - 1;
        }
        items.Clear();
        items.AddRange(new_items);

        // Update selection
        storage.Clear();
        if (item_next_idx_to_select != -1 && ms_io.NavIdSelected)
            storage.SetItemSelected(storage.GetStorageIdFromIndex(item_next_idx_to_select), true);
    }
}

record ExampleAsset(uint Id, uint Type);

class ExampleAssetComparer(ImGuiTableSortSpecsPtr sortSpecs) : IComparer<ExampleAsset>
{
    public unsafe int Compare(ExampleAsset x, ExampleAsset y)
    {
        for (int n = 0; n < sortSpecs.SpecsCount; n++)
        {
            ImGuiTableColumnSortSpecsPtr sort_spec = new(&sortSpecs.Specs.NativePtr[n]);

            int delta = 0;
            if (sort_spec.ColumnIndex == 0)
                delta = (int)(x.Id - y.Id);
            else if (sort_spec.ColumnIndex == 1)
                delta = (int)(x.Type - y.Type);
            if (delta > 0)
                return (sort_spec.SortDirection == ImGuiSortDirection.Ascending) ? +1 : -1;
            if (delta < 0)
                return (sort_spec.SortDirection == ImGuiSortDirection.Ascending) ? -1 : +1;
        }

        return (int)(x.Id - y.Id);
    }
}
