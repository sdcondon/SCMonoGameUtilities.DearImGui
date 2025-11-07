using BenchmarkDotNet.Attributes;
using Microsoft.Xna.Framework;
using SCMonoGameUtilities.DearImGui;

namespace SCGraphTheory.Search.Benchmarks;

// weirdness: first one (only) to run seems a bit slower? So put imgui.net one first cos thats way slower anyways..
public class ImGuiRendererBenchmarks
{
    static readonly FromImGuiNetGame fromImGuiNetGame = new();
    static readonly WithIOStateIndirectionGame withIOStateIndirectionGame = new();
    static readonly ProductionGame productionGame = new();

    [Benchmark()]
    public void FromImGuiNET()
    {
        fromImGuiNetGame.RunOneFrame();
    }

    [Benchmark()]
    public void WithIOStateIndirection()
    {
        withIOStateIndirectionGame.RunOneFrame();
    }

    [Benchmark(Baseline = true)]
    public void Production()
    {
        productionGame.RunOneFrame();
    }

    private class ProductionGame : Game
    {
        readonly ImGuiRenderer renderer;

        public ProductionGame()
        {
            new GraphicsDeviceManager(this).ApplyChanges();
            renderer = new(this);
            renderer.BuildFontAtlas();
            IsFixedTimeStep = false;
        }

        protected override void Update(GameTime gameTime)
        {
            renderer.BeginUpdate(gameTime);
            renderer.EndUpdate();
            SuppressDraw();
        }
    }

    private class WithIOStateIndirectionGame : Game
    {
        readonly ImGuiRenderer_WithIOStateIndirection renderer;

        public WithIOStateIndirectionGame()
        {
            new GraphicsDeviceManager(this).ApplyChanges();
            renderer = new(this);
            renderer.BuildFontAtlas();
            IsFixedTimeStep = false;
        }

        protected override void Update(GameTime gameTime)
        {
            renderer.BeginUpdate(gameTime);
            renderer.EndUpdate();
            SuppressDraw();
        }
    }

    private class FromImGuiNetGame : Game
    {
        readonly ImGuiRenderer_FromImGuiNET renderer;

        public FromImGuiNetGame()
        {
            new GraphicsDeviceManager(this).ApplyChanges();
            renderer = new(this);
            renderer.RebuildFontAtlas();
            IsFixedTimeStep = false;
        }

        protected override void Update(GameTime gameTime)
        {
            renderer.BeforeLayout(gameTime);
            renderer.AfterLayout();
            SuppressDraw();
        }
    }
}
