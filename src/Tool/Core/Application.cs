using System.Diagnostics;

using SDL3;

using Tool.Assets;
using Tool.Diagnostics;
using Tool.Graphics;
using Tool.Input;
using Tool.Logging;
using Tool.Logging.Sinks;

using Vortice.Mathematics;

namespace Tool.Core;

/// <summary>
/// Base application class that manages the window lifecycle, main loop, and graphics device.
/// </summary>
public abstract class Application<TApp> : SDL.IMainCallbacks<TApp>
    where TApp : Application<TApp>, new()
{
    #region Fields

    private readonly Stopwatch _timer = new();
    private TimeSpan _lastFrameTime;
    private WindowMode _windowMode = WindowMode.Windowed;
    private bool _isExiting;
    private bool _isMinimized;
    private string _title = "Tool";

    #endregion

    #region Properties (only get)

    protected nint Window { get; private set; } = IntPtr.Zero;
    public GraphicsDevice GraphicsDevice { get; private set; } = null!;
    public AssetManager AssetManager { get; private set; } = null!;

    #endregion

    #region Properties (can be modified)

    public string Title
    {
        get => _title;
        protected set
        {
            _title = value;
            if (Window != IntPtr.Zero)
            {
                SDL.SetWindowTitle(Window, _title);
            }
        }
    }

    /// <summary>
    /// Gets or sets the window display mode (Windowed, BorderlessFullscreen, or Fullscreen).
    /// Can be pre-configured in the constructor or changed at runtime.
    /// </summary>
    public WindowMode WindowMode
    {
        get => _windowMode;
        protected set
        {
            if (_windowMode == value)
            {
                return;
            }

            _windowMode = value;
            if (Window != IntPtr.Zero)
            {
                ApplyWindowMode(_windowMode);
                GraphicsDevice?.OnWindowModeChanged(_windowMode);
                OnWindowModeChanged(_windowMode);
            }
        }
    }

    #endregion

    #region Properties (init)

    public string Version { get; protected init; } = "0.1";
    public string Identifier { get; protected init; } = "semisoft.tool";
    public SizeI PreferredWindowSize { get; protected init; } = new(1280, 720);
    public bool IsResizable { get; protected init; } = true;
    public GraphicsAPI PreferredGraphicsAPI { get; protected init; } = GraphicsAPI.Default;
    public string ContentRoot { get; protected init; } = "";

    #endregion

    #region Runtime

    /// <summary>
    /// Signals the application to exit at the end of the current frame.
    /// </summary>
    public void Exit() => _isExiting = true;

    /// <summary>
    /// Called after the window and graphics device have been created. Calls <see cref="LoadContent"/> by default.
    /// </summary>
    protected virtual void Initialize()
    {
        LoadContent();
    }

    /// <summary>
    /// Called when graphics and application resources should be loaded.
    /// </summary>
    protected virtual void LoadContent() { }

    /// <summary>
    /// Called once right before the main loop starts running.
    /// </summary>
    protected virtual void BeginRun() { }

    /// <summary>
    /// Called every frame to update application logic.
    /// </summary>
    protected virtual void Update(GameTime gameTime) { }

    /// <summary>
    /// Called before rendering a frame. Returns false to skip rendering the current frame.
    /// </summary>
    protected virtual bool BeginDraw()
    {
        if (_isMinimized)
        {
            return false;
        }

        return GraphicsDevice.BeginFrame();
    }

    /// <summary>
    /// Called every frame to render the scene.
    /// </summary>
    protected virtual void Draw() { }

    /// <summary>
    /// Called after rendering a frame has completed.
    /// </summary>
    protected virtual bool EndDraw()
    {
        return GraphicsDevice.EndFrame();
    }

    /// <summary>
    /// Called when the window pixel size changes.
    /// </summary>
    protected virtual void OnResize(int width, int height) { }
    /// <summary>
    /// Called when the window position on the desktop changes.
    /// </summary>
    protected virtual void OnWindowMoved(RectI bounds) { }
    /// <summary>
    /// Called when the window display mode changes (Windowed, BorderlessFullscreen, or Fullscreen).
    /// </summary>
    protected virtual void OnWindowModeChanged(WindowMode mode) { }
    /// <summary>
    /// Called when the window is minimized to the taskbar.
    /// </summary>
    protected virtual void OnMinimized() { }
    /// <summary>
    /// Called when the window is restored from a minimized state.
    /// </summary>
    protected virtual void OnRestored() { }

    /// <summary>
    /// Called when the main loop is stopping before content is unloaded.
    /// </summary>
    protected virtual void EndRun() { }

    /// <summary>
    /// Called when graphics and application resources should be released prior to device disposal.
    /// </summary>
    protected virtual void UnloadContent() { }

    /// <summary>
    /// Called during application shutdown for final cleanup.
    /// </summary>
    protected virtual void Cleanup() { }

    #endregion

    #region Systems

    protected NativeWindowInfo GetNativeWindowInfo(nint window)
    {
        // TBD: log and return default, then check default
        uint props = SDL.GetWindowProperties(window);
        if (props == 0)
        {
            throw new InvalidOperationException("Couldn't get window properties.");
        }

        if (!SDL.GetWindowSizeInPixels(window, out int w, out int h))
        {
            throw new InvalidOperationException("Couldn't get window size in pixels.");
        }

        if (!SDL.GetWindowPosition(window, out int x, out int y))
        {
            throw new InvalidOperationException("Couldn't get window position.");
        }

        var size = new SizeI(w, h);
        var bounds = new RectI(x, y, w, h);

#if PLATFORM_WINDOWS
        nint hwnd = SDL.GetPointerProperty(props, SDL.Props.WindowWin32HWNDPointer, IntPtr.Zero);
        return hwnd == IntPtr.Zero
            ? throw new InvalidOperationException("Couldn't get Win32 HWND.")
            : new NativeWindowInfo(window, hwnd, size, bounds);
#else
        return new NativeWindowInfo(window, IntPtr.Zero, size, bounds);
#endif
    }

    protected virtual void ConfigureLogging(string[] args)
    {
        Log.AddSink(new ConsoleLogSink());
    }

    protected abstract GraphicsDevice CreateGraphicsDevice(GraphicsAPI api, in NativeWindowInfo window);

    #endregion

    #region SDL

    public static SDL.AppResult AppInit(out TApp? appState, string[] args)
    {
        appState = new TApp();
        appState.ConfigureLogging(args);
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        try
        {
            return appState.InternalInit(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(LogCategories.Core, "Initialization failed.", ex);
            return SDL.AppResult.Failure;
        }
    }

    private SDL.AppResult InternalInit(string[] args)
    {
        // Initialize SDL
        SDL.SetAppMetadata(Title, Version, Identifier);

        if (!SDL.Init(SDL.InitFlags.Video))
        {
            Log.Fatal(LogCategories.Core, $"Couldn't initialize SDL: {SDL.GetError()}");
            return SDL.AppResult.Failure;
        }

        // Initialize window
        SDL.WindowFlags windowFlags = BuildInitialWindowFlags();
        Window = SDL.CreateWindow(Title, PreferredWindowSize.Width, PreferredWindowSize.Height, windowFlags);
        if (Window == IntPtr.Zero)
        {
            Log.Fatal(LogCategories.Core, $"Couldn't create window: {SDL.GetError()}");
            return SDL.AppResult.Failure;
        }

        // Initialize graphics device
        GraphicsDevice = CreateGraphicsDevice(PreferredGraphicsAPI, GetNativeWindowInfo(Window));
        AssetManager = new AssetManager(GraphicsDevice, ContentRoot);

        // Initialize systems
        Keyboard.Initialize();
        Mouse.Initialize(Window);

        // Initialize application
        Initialize();
        BeginRun();

        // Collect garbage
        GarbageCollection.FullBlockingCollect();
        GarbageCollection.EnterGameplayMode();

        // Start timers
        AllocationTracker.Reset();
        _timer.Restart();

        return SDL.AppResult.Continue;
    }

    public SDL.AppResult AppEvent(ref SDL.Event e)
    {
        try
        {
            switch ((SDL.EventType)e.Type)
            {
                case SDL.EventType.Quit:
                    return SDL.AppResult.Success;

                case SDL.EventType.WindowMinimized:
                    _isMinimized = true;
                    GraphicsDevice?.OnMinimized();
                    OnMinimized();
                    break;

                case SDL.EventType.WindowRestored:
                    _isMinimized = false;
                    GraphicsDevice?.OnRestored();
                    OnRestored();
                    break;

                case SDL.EventType.WindowPixelSizeChanged:
                    int w = e.Window.Data1;
                    int h = e.Window.Data2;
                    if (w > 0 && h > 0)
                    {
                        GraphicsDevice?.OnResize(w, h);
                        OnResize(w, h);
                    }
                    break;

                case SDL.EventType.WindowMoved:
                    int x = e.Window.Data1;
                    int y = e.Window.Data2;
                    SizeI currentSize = GraphicsDevice?.ClientSize ?? PreferredWindowSize;
                    var bounds = new RectI(x, y, currentSize.Width, currentSize.Height);
                    GraphicsDevice?.OnWindowMoved(bounds);
                    OnWindowMoved(bounds);
                    break;

                case SDL.EventType.WindowEnterFullscreen:
                    if (_windowMode == WindowMode.Windowed)
                    {
                        _windowMode = WindowMode.BorderlessFullscreen;
                        GraphicsDevice?.OnWindowModeChanged(_windowMode);
                        OnWindowModeChanged(_windowMode);
                    }
                    break;

                case SDL.EventType.WindowLeaveFullscreen:
                    if (_windowMode != WindowMode.Windowed)
                    {
                        _windowMode = WindowMode.Windowed;
                        GraphicsDevice?.OnWindowModeChanged(_windowMode);
                        OnWindowModeChanged(_windowMode);
                    }
                    break;

                case SDL.EventType.MouseWheel:
                    Mouse.OnScrollWheel(e.Wheel.X, e.Wheel.Y);
                    break;
            }

            return SDL.AppResult.Continue;
        }
        catch (Exception ex)
        {
            Log.Fatal(LogCategories.Core, "Unhandled exception in the main loop.", ex);
            return SDL.AppResult.Failure;
        }
    }

    public SDL.AppResult AppIterate()
    {
        try
        {
            AllocationTracker.BeginFrame();

            if (_isExiting)
            {
                return SDL.AppResult.Success;
            }

            TimeSpan totalTime = _timer.Elapsed;
            TimeSpan elapsedTime = totalTime - _lastFrameTime;
            _lastFrameTime = totalTime;
            var gameTime = new GameTime(totalTime, elapsedTime);

            // Systems
            Keyboard.Update();
            Mouse.Update();
            AllocationTracker.Mark("Input");

            Update(gameTime);
            AllocationTracker.Mark("Update");

            if (_isExiting)
            {
                return SDL.AppResult.Success;
            }

            if (BeginDraw())
            {
                Draw();
                EndDraw();
            }
            AllocationTracker.Mark("Draw");

            AllocationTracker.EndFrame();

            return SDL.AppResult.Continue;
        }
        catch (Exception ex)
        {
            Log.Fatal(LogCategories.Core, "Unhandled exception in the main loop.", ex);
            return SDL.AppResult.Failure;
        }
    }

    public void AppQuit(SDL.AppResult result)
    {
        try
        {
            GraphicsDevice?.WaitIdle();

            EndRun();
            UnloadContent();
            Cleanup();

            // Systems
            Mouse.Shutdown();
            Keyboard.Shutdown();

            AssetManager?.Dispose();
            GraphicsDevice?.Dispose();

            if (Window != IntPtr.Zero)
            {
                SDL.DestroyWindow(Window);
                Window = IntPtr.Zero;
            }

            SDL.Quit();

            Log.Info(LogCategories.Core, $"Successful completion with status code: {result}.");
        }
        catch (Exception ex)
        {
            Log.Fatal(LogCategories.Core, "Shutdown failed.", ex);
        }
        finally
        {
            Log.Shutdown();
        }
    }

    #endregion

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Log.Fatal(LogCategories.Core, "Unhandled exception.", e.ExceptionObject as Exception);
        Log.Shutdown();
    }

    private SDL.WindowFlags BuildInitialWindowFlags()
    {
        SDL.WindowFlags flags = 0;

        if (IsResizable)
        {
            flags |= SDL.WindowFlags.Resizable;
        }

        switch (_windowMode)
        {
            case WindowMode.BorderlessFullscreen:
                flags |= SDL.WindowFlags.Borderless | SDL.WindowFlags.Fullscreen;
                break;

            case WindowMode.Fullscreen:
                flags |= SDL.WindowFlags.Fullscreen;
                break;
        }

        return flags;
    }

    private void ApplyWindowMode(WindowMode mode)
    {
        switch (mode)
        {
            case WindowMode.Windowed:
                SDL.SetWindowFullscreen(Window, false);
                SDL.SetWindowBordered(Window, true);
                SDL.SetWindowResizable(Window, IsResizable);
                SDL.SetWindowSize(Window, PreferredWindowSize.Width, PreferredWindowSize.Height);
                break;

            case WindowMode.BorderlessFullscreen:
                SDL.SetWindowBordered(Window, false);
                SDL.SetWindowFullscreen(Window, true);
                break;

            case WindowMode.Fullscreen:
                SDL.SetWindowFullscreen(Window, true);
                break;
        }
    }
}
