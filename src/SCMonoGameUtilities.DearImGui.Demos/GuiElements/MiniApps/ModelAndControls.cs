using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using static ImGuiNET.ImGui;

namespace SCMonoGameUtilities.DearImGui.Demos.GuiElements.MiniApps;

// Demonstrates a combination of non-GUI and GUI elements that interact
class ModelAndControls(GraphicsDevice graphicsDevice, ContentManager contentManager, string modelAssetName, bool isVisible = false)
{
    public bool IsVisible = isVisible;

    private Model model;
    private Matrix modelWorldTransform = Matrix.CreateRotationX(-1.5f);

    public void LoadContent()
    {
        model = contentManager.Load<Model>(modelAssetName);
        foreach (ModelMesh mesh in model.Meshes)
        {
            foreach (BasicEffect effect in mesh.Effects)
            {
                effect.TextureEnabled = false;
                effect.EnableDefaultLighting();
                effect.View = Matrix.CreateLookAt(new(0, 0, 4), Vector3.Zero, Vector3.UnitY);
                effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), graphicsDevice.Viewport.AspectRatio, 0.1f, 100.0f);
            }
        }
    }

    public void UnloadContent()
    {
        contentManager.UnloadAsset(modelAssetName);
    }

    public void Update()
    {
        if (!IsVisible) return;

        System.Numerics.Vector2 windowPosition = new(graphicsDevice.Viewport.Width / 2f, graphicsDevice.Viewport.Height * 3f / 4f);

        SetNextWindowPos(windowPosition);
        SetNextWindowBgAlpha(0.35f);

        ImGuiWindowFlags windowFlags = ImGuiWindowFlags.NoDecoration
            | ImGuiWindowFlags.NoBackground 
            | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoSavedSettings
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoMove 
            | ImGuiWindowFlags.NoBringToFrontOnFocus;

        if (Begin("Example: Model viewer controls", windowFlags))
        {
            PushItemFlag(ImGuiItemFlags.ButtonRepeat, true);

            Indent();
            if (ArrowButton("up", ImGuiDir.Up))
            {
                modelWorldTransform *= Matrix.CreateRotationX(-0.1f);
            }
            Unindent();
            if (ArrowButton("left", ImGuiDir.Left))
            {
                modelWorldTransform *= Matrix.CreateRotationY(-0.1f);
            }
            SameLine();
            Text(" ");
            SameLine();
            if (ArrowButton("right", ImGuiDir.Right))
            {
                modelWorldTransform *= Matrix.CreateRotationY(0.1f);
            }

            Indent();
            if (ArrowButton("down", ImGuiDir.Down))
            {
                modelWorldTransform *= Matrix.CreateRotationX(0.1f);
            }
            Unindent();
            PopItemFlag();
        }

        End();
    }

    public void DrawModel()
    {
        if (!IsVisible) return;

        graphicsDevice.DepthStencilState = DepthStencilState.Default;
        graphicsDevice.BlendState = BlendState.Opaque;

        foreach (ModelMesh mesh in model.Meshes)
        {
            foreach (BasicEffect effect in mesh.Effects)
            {
                effect.World = modelWorldTransform;
            }

            mesh.Draw();
        }
    }
}
