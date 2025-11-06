using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Runtime.InteropServices;

namespace SCMonoGameUtilities.DearImGui;

/// <summary>
/// Renderer for Dear ImGui.
/// </summary>
public sealed class ImGuiRenderer : IDisposable
{
    private const float MOUSE_WHEEL_DELTA = 120;

    private static readonly int ImDrawVertexStride = Marshal.SizeOf<ImDrawVert>();

    private static readonly VertexDeclaration ImDrawVertexDeclaration = new(
        ImDrawVertexStride,
        new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
        new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0));

    // Context
    private readonly Game _game;
    private readonly nint _imGuiContext;
    private readonly ImGuiIOPtr _imGuiIO;

    // Graphics
    private readonly GraphicsDevice _graphicsDevice;
    private readonly RasterizerState _rasterizerState;
    private readonly BasicEffect _effect;
    private readonly Dictionary<nint, Texture2D> _texturesById;

    private byte[] _vertexData = [];
    private VertexBuffer _vertexBuffer;
    private byte[] _indexData = [];
    private IndexBuffer _indexBuffer;

    private nint? _fontTextureId;
    private nint _nextTextureId;

    // Input
    // NB: Instead of just a single "_last..State" field for mouse and keyboard, we maintain a couple, as
    // well as a boolean that indicates which is the current (and which is therefore the last). This ultimately
    // lets us minimise the number of times we make a direct assignment (i.e. a copy) of these rather large
    // structs. Specifically, precisely once (assigning the result of GetState()) for each update step.
    private KeyboardState _keyboardStateA;
    private KeyboardState _keyboardStateB;
    private bool _keyboardStateAIsCurrent;
    private MouseState _mouseStateA;
    private MouseState _mouseStateB;
    private bool _mouseStateAIsCurrent;

    /// <summary>
    /// Initialises a new instance of the <see cref="ImGuiRenderer"/> class.
    /// </summary>
    /// <param name="game">The <see cref="Game"/> the this renderer is used by.</param>
    public ImGuiRenderer(Game game)
    {
        // Setup context
        _game = game ?? throw new ArgumentNullException(nameof(game));

        _imGuiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(_imGuiContext);
        _imGuiIO = ImGui.GetIO();

        // Setup graphics
        _graphicsDevice = game.GraphicsDevice;
        _rasterizerState = new()
        {
            CullMode = CullMode.None,
            DepthBias = 0,
            FillMode = FillMode.Solid,
            MultiSampleAntiAlias = false,
            ScissorTestEnable = true,
            SlopeScaleDepthBias = 0
        };
        _effect = new BasicEffect(_graphicsDevice)
        {
            World = Matrix.Identity,
            View = Matrix.Identity,
            TextureEnabled = true,
            VertexColorEnabled = true,
        };

        _texturesById = [];
        _vertexBuffer = new VertexBuffer(
            _graphicsDevice,
            ImDrawVertexDeclaration,
            0,
            BufferUsage.None);
        _indexBuffer = new IndexBuffer(
            _graphicsDevice,
            IndexElementSize.SixteenBits,
            0,
            BufferUsage.None);

        // Setup input
        _game.Window.TextInput += HandleWindowTextInput;
    }

    ~ImGuiRenderer()
    {
        ImGui.DestroyContext(_imGuiContext);
    }

    /// <summary>
    /// Creates a texture and loads the font data from ImGui.
    /// Should be called when the <see cref="GraphicsDevice" /> is initialized but before any rendering is done.
    /// In a <see cref="Game.LoadContent"/> override is ideal.
    /// </summary>
    public void BuildFontAtlas()
    {
        // might not be needed - if its fine to access the IO struct of a context that isn't active? probably is?
        ImGui.SetCurrentContext(_imGuiContext);

        // If the font atlas has already been built, unregister it first (which also disposes the texture).
        if (_fontTextureId.HasValue)
        {
            UnregisterTexture(_fontTextureId.Value);
        }

        // Get font texture data from ImGui and copy it to a managed array
        _imGuiIO.Fonts.GetTexDataAsRGBA32(out nint pixelData, out int width, out int height, out int bytesPerPixel);
        var pixels = new byte[width * height * bytesPerPixel];
        Marshal.Copy(pixelData, pixels, 0, pixels.Length);

        // Create and register the texture as an XNA texture
        var tex2d = new Texture2D(_graphicsDevice, width, height, false, SurfaceFormat.Color);
        tex2d.SetData(pixels);

        // Bind the new texture to an ImGui-friendly id
        _fontTextureId = RegisterTexture(tex2d);

        // Let ImGui know where to find the texture
        _imGuiIO.Fonts.SetTexID(_fontTextureId.Value);
        _imGuiIO.Fonts.ClearTexData(); // Clears CPU side texture data
    }

    /// <summary>
    /// <para>
    /// Sets up ImGui for a new frame.
    /// </para>
    /// <para>
    /// Should be called in your Update method, prior to any <see cref="ImGui"/> calls.
    /// </para>
    /// </summary>
    public void BeginUpdate(GameTime gameTime)
    {
        ImGui.SetCurrentContext(_imGuiContext);
        UpdateIO(gameTime);
        ImGui.NewFrame();
    }

    /// <summary>
    /// <para>
    /// Tells ImGui that all GUI submissions have been made for the current frame.
    /// </para>
    /// <para>
    /// Should be called in your Update method, after all <see cref="ImGui"/> calls.
    /// </para>
    /// </summary>
    public void EndUpdate()
    {
        ImGui.SetCurrentContext(_imGuiContext);
        ImGui.EndFrame();
    }

    /// <summary>
    /// <para>
    /// Asks ImGui for the generated geometry data and sends it to the graphics pipeline.
    /// </para>
    /// <para>
    /// Should be called in your Draw method.
    /// </para>
    /// </summary>
    public void Draw()
    {
        ImGui.SetCurrentContext(_imGuiContext);

        ImGui.Render();
        var drawData = ImGui.GetDrawData();

        // Store graphics device state for restoration after we're done
        var lastRasterizer = _graphicsDevice.RasterizerState;
        var lastDepthStencil = _graphicsDevice.DepthStencilState;
        var lastBlendFactor = _graphicsDevice.BlendFactor;
        var lastBlendState = _graphicsDevice.BlendState;
        var lastScissorBox = _graphicsDevice.ScissorRectangle;
        var lastViewport = _graphicsDevice.Viewport;

        SetGraphicsDeviceState(drawData);
        SetBufferData(drawData);
        RenderCommandLists(drawData);

        // Restore graphics device state
        _graphicsDevice.RasterizerState = lastRasterizer;
        _graphicsDevice.DepthStencilState = lastDepthStencil;
        _graphicsDevice.BlendFactor = lastBlendFactor;
        _graphicsDevice.BlendState = lastBlendState;
        _graphicsDevice.ScissorRectangle = lastScissorBox;
        _graphicsDevice.Viewport = lastViewport;
    }

    /// <summary>
    /// <para>
    /// Creates an identifier for a texture, which can then be passed to ImGui calls such as <see cref="ImGui.Image" />.
    /// </para>
    /// <para>
    /// NB: The renderer considers itself as taking ownership of the lifetime of the passed texture when this method is called - it will be disposed when unregistered.
    /// </para>
    /// </summary>
    public nint RegisterTexture(Texture2D texture)
    {
        var id = _nextTextureId++;
        _texturesById.Add(id, texture);
        return id;
    }

    /// <summary>
    /// Removes a previously created texture identifier, releasing its reference and disposing the texture object.
    /// </summary>
    /// <param name="textureId">The ID of the texture to unregister</param>
    /// <returns>True if the texture identifier was valid and a texture was unregistered. Otherwise false.</returns>
    public bool UnregisterTexture(nint textureId)
    {
        bool textureRemoved;
        if (textureRemoved = _texturesById.TryGetValue(textureId, out var texture))
        {
            _texturesById.Remove(textureId);
            texture?.Dispose();
        }

        return textureRemoved;
    }

    public void Dispose()
    {
        _game.Window.TextInput -= HandleWindowTextInput;

        foreach (var texture in _texturesById.Values)
        {
            texture?.Dispose();
        }

        ImGui.DestroyContext(_imGuiContext);

        GC.SuppressFinalize(this);
    }

    private void HandleWindowTextInput(object? sender, TextInputEventArgs eventArgs)
    {
        // TODO: do we need any kind of synchronisation here? when/how might this event be raised?
        // We *could* (if needed) store it in a (thread-safe) queue for consumption during the next BeginUpdate()?
        // Almost certainly fine as-is..
        if (eventArgs.Character == '\t') return;
        ImGui.SetCurrentContext(_imGuiContext);
        _imGuiIO.AddInputCharacter(eventArgs.Character);
    }

    private void UpdateIO(GameTime gameTime)
    {
        if (!_game.IsActive) return;

        _imGuiIO.DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _imGuiIO.DisplaySize = new(_graphicsDevice.PresentationParameters.BackBufferWidth, _graphicsDevice.PresentationParameters.BackBufferHeight);
        _imGuiIO.DisplayFramebufferScale = new(1f, 1f);

        // As mentioned in the fields above, all the ref stuff here is ultimately so that we only directly assign
        // keyboard and mouse state ONCE per update step (when assigning return value of GetState) - these are pretty beefy as structs go.
        ref var currentMouseState = ref (_mouseStateAIsCurrent ? ref _mouseStateA : ref _mouseStateB);
        ref var lastMouseState = ref (_mouseStateAIsCurrent ? ref _mouseStateB : ref _mouseStateA);
        currentMouseState = Mouse.GetState();
        AddMousePosEvent(ref currentMouseState, ref lastMouseState);
        AddMouseWheelEvent(ref currentMouseState, ref lastMouseState);
        AddMouseButtonEvent(0, lastMouseState.LeftButton, currentMouseState.LeftButton);
        AddMouseButtonEvent(1, lastMouseState.RightButton, currentMouseState.RightButton);
        AddMouseButtonEvent(2, lastMouseState.MiddleButton, currentMouseState.MiddleButton);
        AddMouseButtonEvent(3, lastMouseState.XButton1, currentMouseState.XButton1);
        AddMouseButtonEvent(4, lastMouseState.XButton2, currentMouseState.XButton2);
        _mouseStateAIsCurrent = !_mouseStateAIsCurrent;

        ref var currentKeyboardState = ref (_keyboardStateAIsCurrent ? ref _keyboardStateA : ref _keyboardStateB);
        ref var lastKeyboardState = ref (_keyboardStateAIsCurrent ? ref _keyboardStateB : ref _keyboardStateA);
        currentKeyboardState = Keyboard.GetState();
        AddKeyEvents(ref lastKeyboardState, ref currentKeyboardState, false);
        AddKeyEvents(ref currentKeyboardState, ref lastKeyboardState, true);
        _keyboardStateAIsCurrent = !_keyboardStateAIsCurrent;

        void AddMousePosEvent(ref MouseState mouseState, ref MouseState lastMouseState)
        {
            if (mouseState.X != lastMouseState.X || mouseState.Y != lastMouseState.Y)
            {
                _imGuiIO.AddMousePosEvent(mouseState.X, mouseState.Y);
            }
        }

        void AddMouseWheelEvent(ref MouseState mouseState, ref MouseState lastMouseState)
        {
            var scrollDelta = mouseState.ScrollWheelValue - lastMouseState.ScrollWheelValue;
            var horizontalScrollDelta = mouseState.HorizontalScrollWheelValue - lastMouseState.HorizontalScrollWheelValue;

            if (scrollDelta != 0 || horizontalScrollDelta != 0)
            {
                _imGuiIO.AddMouseWheelEvent(horizontalScrollDelta / MOUSE_WHEEL_DELTA, scrollDelta / MOUSE_WHEEL_DELTA);
            }
        }

        void AddMouseButtonEvent(int button, ButtonState lastState, ButtonState thisState)
        {
            if (lastState != thisState)
            {
                _imGuiIO.AddMouseButtonEvent(button, thisState == ButtonState.Pressed);
            }
        }

        void AddKeyEvents(ref KeyboardState fromState, ref KeyboardState toState, bool isBackwards)
        {
            foreach (var toPressedKey in toState.GetPressedKeys())
            {
                if (fromState[toPressedKey] == KeyState.Up && TryMapKey(toPressedKey, out ImGuiKey imguikey))
                {
                    _imGuiIO.AddKeyEvent(imguikey, !isBackwards);
                }
            }
        }

        static bool TryMapKey(Keys key, out ImGuiKey imGuiKey)
        {
            ImGuiKey? mappedKey = key switch
            {
                Keys.None => ImGuiKey.None,
                Keys.Back => ImGuiKey.Backspace,
                Keys.Tab => ImGuiKey.Tab,
                Keys.Enter => ImGuiKey.Enter,
                Keys.CapsLock => ImGuiKey.CapsLock,
                Keys.Escape => ImGuiKey.Escape,
                Keys.Space => ImGuiKey.Space,
                Keys.PageUp => ImGuiKey.PageUp,
                Keys.PageDown => ImGuiKey.PageDown,
                Keys.End => ImGuiKey.End,
                Keys.Home => ImGuiKey.Home,
                Keys.Left => ImGuiKey.LeftArrow,
                Keys.Right => ImGuiKey.RightArrow,
                Keys.Up => ImGuiKey.UpArrow,
                Keys.Down => ImGuiKey.DownArrow,
                Keys.PrintScreen => ImGuiKey.PrintScreen,
                Keys.Insert => ImGuiKey.Insert,
                Keys.Delete => ImGuiKey.Delete,
                >= Keys.D0 and <= Keys.D9 => ImGuiKey._0 + (key - Keys.D0),
                >= Keys.A and <= Keys.Z => ImGuiKey.A + (key - Keys.A),
                >= Keys.NumPad0 and <= Keys.NumPad9 => ImGuiKey.Keypad0 + (key - Keys.NumPad0),
                Keys.Multiply => ImGuiKey.KeypadMultiply,
                Keys.Add => ImGuiKey.KeypadAdd,
                Keys.Subtract => ImGuiKey.KeypadSubtract,
                Keys.Decimal => ImGuiKey.KeypadDecimal,
                Keys.Divide => ImGuiKey.KeypadDivide,
                >= Keys.F1 and <= Keys.F12 => ImGuiKey.F1 + (key - Keys.F1),
                Keys.NumLock => ImGuiKey.NumLock,
                Keys.Scroll => ImGuiKey.ScrollLock,
                Keys.LeftShift => ImGuiKey.ModShift,
                Keys.LeftControl => ImGuiKey.ModCtrl,
                Keys.LeftAlt => ImGuiKey.ModAlt,
                Keys.OemSemicolon => ImGuiKey.Semicolon,
                Keys.OemPlus => ImGuiKey.Equal,
                Keys.OemComma => ImGuiKey.Comma,
                Keys.OemMinus => ImGuiKey.Minus,
                Keys.OemPeriod => ImGuiKey.Period,
                Keys.OemQuestion => ImGuiKey.Slash,
                Keys.OemTilde => ImGuiKey.GraveAccent,
                Keys.OemOpenBrackets => ImGuiKey.LeftBracket,
                Keys.OemCloseBrackets => ImGuiKey.RightBracket,
                Keys.OemPipe => ImGuiKey.Backslash,
                Keys.OemQuotes => ImGuiKey.Apostrophe,
                _ => null,
            };

            if (mappedKey.HasValue)
            {
                imGuiKey = mappedKey.Value;
                return true;
            }
            else
            {
                imGuiKey = ImGuiKey.None;
                return false;
            }
        }
    }

    private void SetGraphicsDeviceState(ImDrawDataPtr drawData)
    {
        // Set render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, vertex/texcoord/color pointers
        _graphicsDevice.RasterizerState = _rasterizerState;
        _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
        _graphicsDevice.BlendFactor = Color.White;
        _graphicsDevice.BlendState = BlendState.NonPremultiplied;

        // Handle cases of screen coordinates != from framebuffer coordinates (e.g. retina displays)
        drawData.ScaleClipRects(_imGuiIO.DisplayFramebufferScale);

        // Set viewport
        _graphicsDevice.Viewport = new Viewport(
            0,
            0,
            _graphicsDevice.PresentationParameters.BackBufferWidth,
            _graphicsDevice.PresentationParameters.BackBufferHeight);
    }

    private void SetBufferData(ImDrawDataPtr drawData)
    {
        if (drawData.TotalVtxCount == 0)
        {
            return;
        }

        // Expand buffers if we need more room
        if (drawData.TotalVtxCount > _vertexBuffer.VertexCount)
        {
            _vertexBuffer.Dispose();

            var newVertexCount = (int)(drawData.TotalVtxCount * 1.5f);
            _vertexBuffer = new VertexBuffer(_graphicsDevice, ImDrawVertexDeclaration, newVertexCount, BufferUsage.None);
            _vertexData = new byte[newVertexCount * ImDrawVertexStride];
        }

        if (drawData.TotalIdxCount > _indexBuffer.IndexCount)
        {
            _indexBuffer.Dispose();

            var newIndexCount = (int)(drawData.TotalIdxCount * 1.5f);
            _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, newIndexCount, BufferUsage.None);
            _indexData = new byte[newIndexCount * sizeof(ushort)];
        }

        // Copy ImGui's vertices and indices to a set of managed byte arrays
        int vtxOffset = 0;
        int idxOffset = 0;

        for (var cmdListIx = 0; cmdListIx < drawData.CmdListsCount; cmdListIx++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[cmdListIx];

            Marshal.Copy(cmdList.VtxBuffer.Data, _vertexData, vtxOffset * ImDrawVertexStride, cmdList.VtxBuffer.Size * ImDrawVertexStride);
            Marshal.Copy(cmdList.IdxBuffer.Data, _indexData, idxOffset * sizeof(ushort), cmdList.IdxBuffer.Size * sizeof(ushort));

            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }

        // Copy the managed byte arrays to the GPU vertex and index buffers
        // TODO: keep an eye on whether any MonoGame update adds support for Span<byte> instead of byte[].
        // Then wouldn't need the intermediate arrays at all; could just set data in the loop above.
        _vertexBuffer.SetData(_vertexData, 0, drawData.TotalVtxCount * ImDrawVertexStride);
        _indexBuffer.SetData(_indexData, 0, drawData.TotalIdxCount * sizeof(ushort));
    }

    private void RenderCommandLists(ImDrawDataPtr drawData)
    {
        _graphicsDevice.SetVertexBuffer(_vertexBuffer);
        _graphicsDevice.Indices = _indexBuffer;

        int vtxOffset = 0;
        int idxOffset = 0;

        for (var cmdListIx = 0; cmdListIx < drawData.CmdListsCount; cmdListIx++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[cmdListIx];

            for (var cmdIx = 0; cmdIx < cmdList.CmdBuffer.Size; cmdIx++)
            {
                ImDrawCmdPtr cmd = cmdList.CmdBuffer[cmdIx];

                if (cmd.ElemCount == 0)
                {
                    continue;
                }

                if (!_texturesById.TryGetValue(cmd.TextureId, out var texture))
                {
                    throw new InvalidOperationException($"Could not find a texture with id '{cmd.TextureId}', please check your bindings");
                }

                _graphicsDevice.ScissorRectangle = new Rectangle(
                    (int)cmd.ClipRect.X,
                    (int)cmd.ClipRect.Y,
                    (int)(cmd.ClipRect.Z - cmd.ClipRect.X),
                    (int)(cmd.ClipRect.W - cmd.ClipRect.Y));

                _effect.Projection = Matrix.CreateOrthographicOffCenter(0f, _imGuiIO.DisplaySize.X, _imGuiIO.DisplaySize.Y, 0f, -1f, 1f);
                _effect.Texture = texture;

                foreach (var pass in _effect.CurrentTechnique.Passes)
                {
                    pass.Apply();

                    _graphicsDevice.DrawIndexedPrimitives(
                        primitiveType: PrimitiveType.TriangleList,
                        baseVertex: (int)cmd.VtxOffset + vtxOffset,
                        startIndex: (int)cmd.IdxOffset + idxOffset,
                        primitiveCount: (int)cmd.ElemCount / 3);
                }
            }

            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }
    }
}
