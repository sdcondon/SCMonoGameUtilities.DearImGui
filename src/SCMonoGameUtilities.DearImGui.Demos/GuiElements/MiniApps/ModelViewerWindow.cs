using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using static ImGuiNET.ImGui;

namespace SCMonoGameUtilities.DearImGui.Demos.GuiElements.MiniApps;

// Demo of a window that includes a rendered 3D image.
class ModelViewerWindow(
    GraphicsDevice graphicsDevice,
    ContentManager contentManager,
    ImGuiRenderer imGuiRenderer,
    string modelAssetName,
    bool isVisible = false)
{
    public bool IsVisible = isVisible;

    private Model model;
    private Matrix modelWorldTransform = Matrix.CreateRotationX(-1.5f);
    private float modelAspectRatio;
    private RenderTarget2D modelRenderTarget;
    private nint modelTextureId;

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
            }
        }

        modelRenderTarget = new RenderTarget2D(
            graphicsDevice,
            graphicsDevice.PresentationParameters.BackBufferWidth,
            graphicsDevice.PresentationParameters.BackBufferHeight,
            false,
            graphicsDevice.PresentationParameters.BackBufferFormat,
            DepthFormat.Depth24);

        modelTextureId = imGuiRenderer.RegisterTexture(modelRenderTarget);
    }

    public void UnloadContent()
    {
        contentManager.UnloadAsset(modelAssetName);
        imGuiRenderer.UnregisterTexture(modelTextureId);
    }

    public void Update()
    {
        if (!IsVisible) return;

        if (Begin("Example: Model Viewer", ref IsVisible))
        {
            System.Numerics.Vector2 imageSize = GetContentRegionAvail();
            if (imageSize.X > 0 && imageSize.Y > 0)
            {
                modelAspectRatio = imageSize.X / imageSize.Y;

                var cursorPos = GetCursorPos();

                InvisibleButton("model", imageSize, ImGuiButtonFlags.MouseButtonLeft);
                if (IsItemActive() && IsMouseDragging(ImGuiMouseButton.Left))
                {
                    var io = GetIO();
                    modelWorldTransform *= Matrix.CreateRotationY(0.01f * io.MouseDelta.X);
                    modelWorldTransform *= Matrix.CreateRotationX(0.01f * io.MouseDelta.Y);
                }

                SetCursorPos(cursorPos);
                Image(modelTextureId, imageSize);
            }
        }

        End();
    }

    public void DrawModel()
    {
        if (!IsVisible) return;

        var priorRenderTargets = graphicsDevice.GetRenderTargets();

        graphicsDevice.SetRenderTarget(modelRenderTarget);
        graphicsDevice.Clear(Color.Transparent);

        foreach (ModelMesh mesh in model.Meshes)
        {
            foreach (BasicEffect effect in mesh.Effects)
            {
                effect.World = modelWorldTransform;
                effect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), modelAspectRatio, 0.1f, 100.0f);
            }

            mesh.Draw();
        }

        graphicsDevice.SetRenderTargets(priorRenderTargets);
    }
}
