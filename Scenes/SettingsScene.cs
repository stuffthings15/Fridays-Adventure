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
        private float _masterVolume = 1.0f;
        private float _musicVolume = 0.7f;
        private float _sfxVolume = 0.9f;

        private Rectangle _backButton;
        private Rectangle _volumeMasterSlider;
        private Rectangle _volumeMusicSlider;
        private Rectangle _volumeSfxSlider;

        private int _selectedOption = 0;  // 0=Master, 1=Music, 2=SFX, 3=Back
        private float _animTime = 0f;
        private string _status = "Use arrow keys to navigate, [Enter] to adjust, [Esc] to back";

        public override void OnEnter()
        {
            // Load current settings from config
            LoadSettings();
            CalculateLayout();
            Game.Instance.Audio.ContinueOrPlay("overworld");  // Continue ambient music
        }

        public override void OnExit()
        {
            // Automatically saves on exit
            SaveSettings();
        }

        /// <summary>
        /// Loads current settings from Game configuration
        /// </summary>
        private void LoadSettings()
        {
            // Load from game's audio manager (already has volume settings)
            _masterVolume = 1.0f;
            _musicVolume = Game.Instance.Audio.MusicVolume / 100f;  // Convert from 0-100 to 0-1
            _sfxVolume = Game.Instance.Audio.SfxVolume / 100f;      // Convert from 0-100 to 0-1
        }

        /// <summary>
        /// Saves current settings to the AudioManager and persists them to SaveData
        /// so they survive a game restart.
        /// Master volume is applied as a multiplier over both music and SFX.
        /// </summary>
        private void SaveSettings()
        {
            if (Game.Instance.Audio == null) return;

            // Master volume scales both channels proportionally (0–1 × 0–1 × 100 = final %)
            Game.Instance.Audio.SetMusicVolume((int)(_musicVolume * _masterVolume * 100));
            Game.Instance.Audio.SetSfxVolume((int)(_sfxVolume   * _masterVolume * 100));

            // Persist to SaveData so volumes survive a restart
            var save = Game.Instance.Save;
            if (save != null)
            {
                save.MusicVolume = Game.Instance.Audio.MusicVolume;
                save.SfxVolume   = Game.Instance.Audio.SfxVolume;
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

            _volumeMasterSlider = new Rectangle(sliderX, startY, sliderW, sliderH);
            _volumeMusicSlider = new Rectangle(sliderX, startY + spacing, sliderW, sliderH);
            _volumeSfxSlider = new Rectangle(sliderX, startY + spacing * 2, sliderW, sliderH);

            // Back button
            _backButton = new Rectangle(W / 2 - 75, H - 80, 150, 40);
        }

        public override void Update(float dt)
        {
            _animTime += dt;

            // Navigation: Arrow keys to select option
            if (Game.Instance.Input.IsPressed(System.Windows.Forms.Keys.Up))
            {
                _selectedOption = (_selectedOption - 1 + 4) % 4;
            }
            if (Game.Instance.Input.IsPressed(System.Windows.Forms.Keys.Down))
            {
                _selectedOption = (_selectedOption + 1) % 4;
            }

            // Adjust volume with Left/Right arrows
            if (Game.Instance.Input.IsPressed(System.Windows.Forms.Keys.Left))
            {
                AdjustSelectedVolume(-0.1f);
            }
            if (Game.Instance.Input.IsPressed(System.Windows.Forms.Keys.Right))
            {
                AdjustSelectedVolume(0.1f);
            }

            // Back button or Escape
            if (Game.Instance.Input.PausePressed)
            {
                SaveSettings();
                Game.Instance.Scenes.Pop();
                return;
            }

            // Enter key selects back button
            if (Game.Instance.Input.InteractPressed && _selectedOption == 3)
            {
                SaveSettings();
                Game.Instance.Scenes.Pop();
            }
        }

        /// <summary>
        /// Adjust the volume of the currently selected option
        /// </summary>
        private void AdjustSelectedVolume(float delta)
        {
            switch (_selectedOption)
            {
                case 0:  // Master Volume
                    _masterVolume = Math.Max(0f, Math.Min(1f, _masterVolume + delta));
                    break;
                case 1:  // Music Volume
                    _musicVolume = Math.Max(0f, Math.Min(1f, _musicVolume + delta));
                    break;
                case 2:  // SFX Volume
                    _sfxVolume = Math.Max(0f, Math.Min(1f, _sfxVolume + delta));
                    break;
            }

            // Apply immediately for audio preview
            SaveSettings();
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

            // Draw volume sliders
            DrawVolumeOption(g, "Master Volume", _volumeMasterSlider, _masterVolume, 0);
            DrawVolumeOption(g, "Music Volume", _volumeMusicSlider, _musicVolume, 1);
            DrawVolumeOption(g, "SFX Volume", _volumeSfxSlider, _sfxVolume, 2);

            // Draw back button
            DrawButton(g, _backButton, "BACK", _selectedOption == 3);

            // Draw status message
            using (var f = new Font("Courier New", 9, FontStyle.Regular))
            {
                g.DrawString(_status, f, Brushes.LightGray, 10, H - 25);
            }

            // Draw help text
            using (var f = new Font("Courier New", 10, FontStyle.Bold))
            {
                g.DrawString("← → to adjust  |  ↑ ↓ to select  |  [Esc] to back", f, Brushes.Yellow, W / 2 - 200, H / 2 + 80);
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
