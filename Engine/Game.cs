using System;
using System.Windows.Forms;
using Fridays_Adventure.Audio;
using Fridays_Adventure.Data;
using Fridays_Adventure.Scenes;

namespace Fridays_Adventure.Engine
{
    public sealed class Game
    {
        public static Game Instance { get; private set; }
        public static event Action OpenLogbookRequested;
        public static event Action CloseRequested;
        public static void RequestOpenLogbook() => OpenLogbookRequested?.Invoke();
        public static void RequestClose()        => CloseRequested?.Invoke();

        public InputManager  Input  { get; }
        public SceneManager  Scenes { get; }
        public AudioManager  Audio  { get; }
        public SaveData      Save   { get; private set; }

        public int   PlayerBounty  { get; set; }
        public float ThreatLevel   { get; set; }
        public int   CrewBonds     { get; set; }
        public int   ShipHealth    { get; set; } = 100;
        public int   Cargo         { get; set; }
        public int   Water         { get; set; } = 50;
        public int   Food          { get; set; } = 30;
        public int   SeaStoneCount { get; set; }
        public bool  GodMode       { get; set; }

        public int CanvasWidth  { get; private set; } = 900;
        public int CanvasHeight { get; private set; } = 600;

        private readonly GameCanvas _canvas;
        private readonly Timer      _timer;
        private const float FixedDt = 1f / 60f;

        public Game(GameCanvas canvas)
        {
            Instance = this;
            _canvas  = canvas;
            Input    = new InputManager();
            Scenes   = new SceneManager();
            Audio    = new AudioManager();
            Save     = SaveData.Load();

            _canvas.Render += OnRender;
            _canvas.Resize += (s, e) =>
            {
                CanvasWidth  = _canvas.Width;
                CanvasHeight = _canvas.Height;
            };
            CanvasWidth  = _canvas.Width;
            CanvasHeight = _canvas.Height;

            _timer          = new Timer { Interval = 16 };
            _timer.Tick    += OnTick;
        }

        public void Start()
        {
            Audio.LoadAll();
            Audio.SetMusicVolume(Save.MusicVolume);
            Audio.SetSfxVolume(Save.SfxVolume);
            Audio.ApplySavedPlaylists(Save.PlaylistData);
            Audio.Prewarm();             // open first track on background thread
            Scenes.Push(new LoadingScene());
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            Audio.StopMusic();
            Save.MusicVolume = Audio.MusicVolume;
            Save.SfxVolume   = Audio.SfxVolume;
            Save.Save();
        }

        private void OnTick(object sender, EventArgs e)
        {
            Audio.Tick(FixedDt);
            Scenes.Current?.Update(FixedDt);
            Input.EndFrame();
            _canvas.Invalidate();
        }

        private void OnRender(System.Drawing.Graphics g)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
            Scenes.Current?.Draw(g);
        }
    }
}
