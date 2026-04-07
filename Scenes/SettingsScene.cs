using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Scenes
{
    // ────────────────────────────────────────────────────────────────────────────
    // PHASE 2 - Team 9: UI Programmer
    // Feature: Settings Menu
    // Purpose: In-game settings UI for audio/graphics/controls configuration
    // Status: IMPLEMENTATION
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// PHASE 2 - Team 9: UI Programmer
    /// Feature: Settings Menu Scene
    /// Implements: Audio volume controls, graphics options, keybind settings
    /// Styled: SMB3-inspired simple rectangles and labels
    /// </summary>
    public sealed class SettingsScene : Scene
    {
        // Volume values stored as 0–100 integers matching AudioManager
        private int _musicVolume = 70;
        private int _sfxVolume   = 90;

        private Rectangle _backButton;
        private Rectangle _volumeMusicSlider;
        private Rectangle _volumeSfxSlider;

        private int _selectedOption = 0;  // 0=Music, 1=SFX, 2=Back
        private float _animTime = 0f;
        private string _status = "Use arrow keys to navigate, Left/Right to adjust, Esc to back";

        // Hold-repeat for smooth slider dragging
        private float _sliderRepeat;
        private const float SliderRate = 0.08f;

        public override void OnEnter()
        {
            // Load current settings from AudioManager
            LoadSettings();
            CalculateLayout();
        }

        public override void OnExit()
        {
            // Persist on exit
            SaveSettings();
        }

        /// <summary>
        /// Loads current volume settings from the AudioManager.
        /// </summary>
        private void LoadSettings()
        {
            _musicVolume = Game.Instance.Audio.MusicVolume;
            _sfxVolume   = Game.Instance.Audio.SfxVolume;
        }

        /// <summary>
        /// Applies current slider values to the AudioManager and persists
        /// them to SaveData so they survive a game restart.
        /// </summary>
        private void SaveSettings()
        {
            if (Game.Instance.Audio == null) return;

            Game.Instance.Audio.SetMusicVolume(_musicVolume);
            Game.Instance.Audio.SetSfxVolume(_sfxVolume);

            // Persist to SaveData so volumes survive a restart
            var save = Game.Instance.Save;
            if (save != null)
            {
                save.MusicVolume = _musicVolume;
                save.SfxVolume   = _sfxVolume;
                save.Save();
            }
        }

        /// <summary>
        /// Calculate UI layout dimensions based on canvas size
        /// </summary>
        private void CalculateLayout()
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // Slider dimensions (width x height)
            int sliderW = 200;
            int sliderH = 16;
            int sliderX = W / 2 - sliderW / 2;

            // Position sliders vertically with spacing
            int startY = H / 3;
            int spacing = 60;

            _volumeMusicSlider = new Rectangle(sliderX, startY, sliderW, sliderH);
            _volumeSfxSlider   = new Rectangle(sliderX, startY + spacing, sliderW, sliderH);

            // Back button
            _backButton = new Rectangle(W / 2 - 75, H - 80, 150, 40);
        }

        public override void Update(float dt)
        {
            _animTime += dt;

            // Navigation: Arrow keys to select option (3 options: 0=Music, 1=SFX, 2=Back)
            if (Game.Instance.Input.IsPressed(System.Windows.Forms.Keys.Up))
            {
                _selectedOption = (_selectedOption - 1 + 3) % 3;
            }
            if (Game.Instance.Input.IsPressed(System.Windows.Forms.Keys.Down))
            {
                _selectedOption = (_selectedOption + 1) % 3;
            }

            // Adjust volume with Left/Right arrows (with hold-repeat)
            if (_selectedOption < 2)  // Only for slider rows
            {
                bool left  = Game.Instance.Input.IsPressed(System.Windows.Forms.Keys.Left)
                          || (_sliderRepeat <= 0 && Game.Instance.Input.LeftHeld);
                bool right = Game.Instance.Input.IsPressed(System.Windows.Forms.Keys.Right)
                          || (_sliderRepeat <= 0 && Game.Instance.Input.RightHeld);
                _sliderRepeat -= dt;
                if (left || right)
                {
                    _sliderRepeat = SliderRate;
                    int delta = left ? -5 : 5;
                    AdjustSelectedVolume(delta);
                }
            }
            else
            {
                _sliderRepeat = 0;
            }

            // Back button or Escape
            if (Game.Instance.Input.PausePressed)
            {
                SaveSettings();
                Game.Instance.Scenes.Pop();
                return;
            }

            // Enter key selects back button
            if (Game.Instance.Input.InteractPressed && _selectedOption == 2)
            {
                SaveSettings();
                Game.Instance.Scenes.Pop();
            }
        }

        /// <summary>
        /// Adjust the volume of the currently selected slider by delta (±5).
        /// Applies immediately so the user hears the change in real time.
        /// </summary>
        private void AdjustSelectedVolume(int delta)
        {
            switch (_selectedOption)
            {
                case 0:  // Music Volume
                    _musicVolume = Math.Max(0, Math.Min(100, _musicVolume + delta));
                    Game.Instance.Audio.SetMusicVolume(_musicVolume);
                    break;
                case 1:  // SFX Volume
                    _sfxVolume = Math.Max(0, Math.Min(100, _sfxVolume + delta));
                    Game.Instance.Audio.SetSfxVolume(_sfxVolume);
                    break;
            }
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // Background: Semi-transparent overlay
            using (var br = new SolidBrush(Color.FromArgb(150, 20, 20, 40)))
                g.FillRectangle(br, 0, 0, W, H);

            // Draw semi-transparent darker background for focus
            using (var br = new SolidBrush(Color.FromArgb(200, 10, 10, 20)))
                g.FillRectangle(br, W / 4, H / 4, W / 2, H / 2);

            // Title
            using (var f = new Font("Courier New", 20, FontStyle.Bold))
            {
                string title = "SETTINGS";
                SizeF titleSize = g.MeasureString(title, f);
                g.DrawString(title, f, Brushes.LimeGreen, W / 2 - titleSize.Width / 2, H / 4 + 20);
            }

            // Draw volume sliders (0–100 integer scale)
            DrawVolumeOption(g, "Music Volume", _volumeMusicSlider, _musicVolume / 100f, 0);
            DrawVolumeOption(g, "SFX Volume",   _volumeSfxSlider,   _sfxVolume   / 100f, 1);

            // Draw back button
            DrawButton(g, _backButton, "BACK", _selectedOption == 2);

            // Draw status message
            using (var f = new Font("Courier New", 9, FontStyle.Regular))
            {
                g.DrawString(_status, f, Brushes.LightGray, 10, H - 25);
            }

            // Draw help text
            using (var f = new Font("Courier New", 10, FontStyle.Bold))
            {
                g.DrawString("\u2190 \u2192 to adjust  |  \u2191 \u2193 to select  |  [Esc] to back", f, Brushes.Yellow, W / 2 - 200, H / 2 + 80);
            }
        }

        /// <summary>
        /// Draw a single volume control option with slider
        /// </summary>
        private void DrawVolumeOption(Graphics g, string label, Rectangle sliderRect, float volume, int optionIndex)
        {
            bool isSelected = _selectedOption == optionIndex;

            // Label
            using (var f = new Font("Courier New", 12, FontStyle.Bold))
            {
                Color labelColor = isSelected ? Color.Gold : Color.White;
                g.DrawString(label, f, new SolidBrush(labelColor), sliderRect.X - 140, sliderRect.Y - 5);
            }

            // Slider background
            Color sliderBg = isSelected ? Color.FromArgb(100, 100, 200) : Color.FromArgb(60, 60, 80);
            using (var br = new SolidBrush(sliderBg))
            {
                g.FillRectangle(br, sliderRect);
            }

            // Slider border
            Color borderColor = isSelected ? Color.LimeGreen : Color.Gray;
            using (var pen = new Pen(borderColor, isSelected ? 2 : 1))
            {
                g.DrawRectangle(pen, sliderRect);
            }

            // Fill amount (volume level)
            int fillWidth = (int)(sliderRect.Width * volume);
            using (var br = new SolidBrush(Color.FromArgb(200, Color.LimeGreen)))
            {
                g.FillRectangle(br, sliderRect.X, sliderRect.Y, fillWidth, sliderRect.Height);
            }

            // Volume percentage text
            using (var f = new Font("Courier New", 10, FontStyle.Bold))
            {
                string volumeText = $"{(int)(volume * 100)}%";
                SizeF textSize = g.MeasureString(volumeText, f);
                g.DrawString(volumeText, f, Brushes.White, sliderRect.X + sliderRect.Width + 20, sliderRect.Y - 3);
            }
        }

        /// <summary>
        /// Draw a clickable button with highlight
        /// </summary>
        private void DrawButton(Graphics g, Rectangle rect, string text, bool isSelected)
        {
            // Button background
            Color bgColor = isSelected ? Color.FromArgb(200, 100, 200, 100) : Color.FromArgb(100, 80, 100, 80);
            using (var br = new SolidBrush(bgColor))
            {
                g.FillRectangle(br, rect);
            }

            // Button border
            Color borderColor = isSelected ? Color.LimeGreen : Color.Gray;
            int borderWidth = isSelected ? 3 : 1;
            using (var pen = new Pen(borderColor, borderWidth))
            {
                g.DrawRectangle(pen, rect);
            }

            // Button text
            using (var f = new Font("Courier New", 12, FontStyle.Bold))
            {
                SizeF textSize = g.MeasureString(text, f);
                g.DrawString(text, f, Brushes.White,
                    rect.X + (rect.Width - textSize.Width) / 2,
                    rect.Y + (rect.Height - textSize.Height) / 2);
            }
        }
    }
}
