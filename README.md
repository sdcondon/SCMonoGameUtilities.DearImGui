# SCMonoGameUtilities.DearImGui

Just my adaptation of the MonoGame ImGui renderer and examples found in the [ImGuiNET](https://github.com/ImGuiNET/ImGui.NET) monogame demo project.
Mostly for personal use, but making it public because others might find some value here - especially in the rewritten and expanded demos, many of
which don't feature in the ImGuiNET demo project.

Changes from MonoGame demo proj in ImGuiNET:

* Significant changes made to the renderer from that project, which IMO leaves a lot to be desired. Most notably:
  * While I understand the temptation given ImGui's immediate nature, don't do everything in Draw(). MonoGame
    doesn't necessarily do a Draw for every update step (see its game loop documentation), and the last thing
    we want if the game is struggling to keep up is for our GUI to become even less responsive because button
    clicks etc aren't coinciding with a Draw call. Plus of course in general it's a good idea to respect the
    conventions of the framework you are using - which in MonoGame's case means separating logic that updates
    state (which in ImGui's case happens alongside submitting GUI elements), and logic that sends to the graphics
    pipeline (which in ImGui's case doesn't happen until you retrieve the draw data and deal with it appropriately).
  * Key up/down event code rewritten, because enumerating a fairly large enumeration in each update is slightly 
    insane, when instead we can just use MonoGame's GetPressedKeys stuff - which does bitwise operations to look
    for pressed keys.
* Demos expanded significantly (the vast majority of the native ones are here now, plus one or two others), and
  most re-written for better encapsulation of the individual demos.
