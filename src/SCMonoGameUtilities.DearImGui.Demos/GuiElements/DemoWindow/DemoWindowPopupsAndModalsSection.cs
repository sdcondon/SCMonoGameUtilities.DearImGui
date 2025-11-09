using ImGuiNET;
using static ImGuiNET.ImGui;
using static SCMonoGameUtilities.DearImGui.Demos.GuiElements.GuiElementHelpers;
using Vec2 = System.Numerics.Vector2;
using Vec4 = System.Numerics.Vector4;

namespace SCMonoGameUtilities.DearImGui.Demos.GuiElements.DemoWindow;

class DemoWindowPopupsAndModalsSection
{
    // Popups subsection
    int selected_fish = -1;
    readonly bool[] toggles = [true, false, false, false, false];

    // Context menus subsection
    float value = 0.5f;
    string name = "Label1";

    // Modals subsection
    bool show = false;
    bool show_stacked = false;
    bool dont_ask_me_next_time = false;
    int item_mod = 1;
    Vec4 color = new(0.4f, 0.7f, 0.0f, 0.5f);

    public void Update()
    {
        if (!CollapsingHeader("Popups & Modal windows"))
        {
            return;
        }

        UpdatePopupsSubsection();
        UpdateContextMenusSubsection();
        UpdateModalsSubsection();
    }

    private void UpdatePopupsSubsection()
    {
        if (!TreeNode("Popups"))
        {
            return;
        }

        TextWrapped("When a popup is active, it inhibits interacting with windows that are behind the popup. " +
        "Clicking outside the popup closes it.");

        string[] names = ["Bream", "Haddock", "Mackerel", "Pollock", "Tilefish"];

        if (Button("Select.."))
        {
            OpenPopup("my_select_popup");
        }
        SameLine();
        TextUnformatted(selected_fish == -1 ? "<None>" : names[selected_fish]);
        if (BeginPopup("my_select_popup"))
        {
            Text("Aquarium");
            Separator();
            for (int i = 0; i < names.Length; i++)
            {
                if (Selectable(names[i]))
                {
                    selected_fish = i;
                }
            }
            EndPopup();
        }

        //menu with toggles
        if (Button("Toggle.."))
        {
            OpenPopup("my_toggle_popup");
        }
        if (BeginPopup("my_toggle_popup"))
        {
            for (int i = 0; i < names.Length; i++)
            {
                MenuItem(names[i], "", ref toggles[i]);
            }
            if (BeginMenu("Sub-menu"))
            {
                MenuItem("Click me");
                EndMenu();
            }

            Separator();
            Text("Tooltip here");
            if (IsItemHovered())
            {
                SetTooltip("I am a tooltip over a popup");
            }

            if (Button("Stacked Popup"))
            {
                OpenPopup("another popup");
            }
            if (BeginPopup("another popup"))
            {
                for (int i = 0; i < names.Length; i++)
                {
                    MenuItem(names[i], "", ref toggles[i]);
                }
                if (BeginMenu("Sub-menu"))
                {
                    MenuItem("Click me");
                    if (Button("Stacked Popup"))
                    {
                        OpenPopup("another popup");
                    }
                    if (BeginPopup("another popup"))
                    {
                        Text("I am the last one here.");
                        EndPopup();
                    }
                    EndMenu();
                }
                EndPopup();
            }
            EndPopup();
        }

        if (Button("File Menu.."))
        {
            OpenPopup("my_file_popup");
        }
        if (BeginPopup("my_file_popup"))
        {
            ExampleFileMenu();
            EndPopup();
        }

        TreePop();
    }

    private void UpdateContextMenusSubsection()
    {
        if (!TreeNode("Context menus"))
        {
            return;
        }

        Text(string.Format("Value = {0} (<-- right-click here)", value));
        if (BeginPopupContextItem("item context menu"))
        {
            if (Selectable("Set to zero")) { value = 0.0f; }
            if (Selectable("Set to PI")) { value = 3.1415f; }
            SetNextItemWidth(-1.0f);
            DragFloat("##Value", ref value, 0.1f, 0.0f, 0.0f);
            EndPopup();
        }

        Text("(You can also right-click me to open the same popup as above.)");
        OpenPopupOnItemClick("item context menu", ImGuiPopupFlags.MouseButtonRight);

        Button(string.Format("Button: {0}###Button", name));
        if (BeginPopupContextItem())
        {
            Text("Edit name:");
            InputText("##edit", ref name, 100);
            if (Button("Close"))
            {
                CloseCurrentPopup();
            }
            EndPopup();
        }
        SameLine(); Text("(<-- right-click here)");

        TreePop();
    }

    private void UpdateModalsSubsection()
    {
        if (!TreeNode("Modals"))
        {
            return;
        }

        TextWrapped("Modal windows are like popups but the user cannot close them by clicking outside.");

        if (Button("Delete.."))
        {
            OpenPopup("Delete?");
            show = true;
        }

        Vec2 center = new(400, 400);
        SetNextWindowPos(center, ImGuiCond.Appearing, new Vec2(0.5f, 0.5f));

        if (BeginPopupModal("Delete?", ref show, ImGuiWindowFlags.AlwaysAutoResize))
        {
            Text("All those beautiful files will be deleted.\nThis operation cannot be undone!\n\n");
            Separator();

            PushStyleVar(ImGuiStyleVar.FramePadding, new Vec2(0, 0));
            Checkbox("Don't ask me next time", ref dont_ask_me_next_time);
            PopStyleVar();

            if (Button("OK", new Vec2(120, 0))) { CloseCurrentPopup(); }
            SetItemDefaultFocus();
            SameLine();
            if (Button("Cancel", new Vec2(120, 0))) { CloseCurrentPopup(); }
            EndPopup();
        }

        if (Button("Stacked modals.."))
        {
            OpenPopup("Stacked 1");
            show_stacked = true;
        }
        if (BeginPopupModal("Stacked 1", ref show_stacked, ImGuiWindowFlags.MenuBar))
        {
            if (BeginMenuBar())
            {
                if (BeginMenu("File"))
                {
                    if (MenuItem("Some menu item")) { }
                    EndMenu();
                }
                EndMenuBar();
            }
            Text("Hello from Stacked The First\nUsing style.Colors[ImGuiCol_ModalWindowDimBg] behind it.");

            Combo("Combo", ref item_mod, "aaaa\0bbbb\0cccc\0dddd\0eeee\0\0");
            ColorEdit4("color", ref color);

            if (Button("Add another modal.."))
            {
                OpenPopup("Stacked 2");
            }

            bool unused_open = true;
            if (BeginPopupModal("Stacked 2", ref unused_open))
            {
                Text("Hello from Stacked The Second!");
                if (Button("Close"))
                {
                    CloseCurrentPopup();
                }
                EndPopup();
            }

            if (Button("Close"))
            {
                CloseCurrentPopup();
            }
            EndPopup();
        }
        TreePop();
    }
}
