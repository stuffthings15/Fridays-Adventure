using System;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    // ────────────────────────────────────────────────────────────────────────────
    // PHASE 2 - Team 1: Game Director
    // Feature: Difficulty Selection Scene
    // Purpose: UI for selecting game difficulty before gameplay
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// PHASE 2 - Team 1: Game Director
    /// Feature: Difficulty Selection Scene
    /// Allows player to choose between Normal, Hard, and Challenge modes
    /// </summary>
    public sealed class DifficultySelectScene : Scene
    {
        private DifficultyModifiers.Difficulty[] _difficulties = 
        {
            DifficultyModifiers.Difficulty.Normal,
            DifficultyModifiers.Difficulty.Hard,
            DifficultyModifiers.Difficulty.Challenge
        };

        private int _selectedIndex = 0;
        private float _animTime = 0f;
        private string _status = "Use arrow keys to select, [Enter] to confirm";

        public override void OnEnter()
        {
            // Find current difficulty index
            for (int i = 0; i < _difficulties.Length; i++)
            {
                if (_difficulties[i] == DifficultyModifiers.CurrentDifficulty)
                {
                    _selectedIndex = i;
                    break;
                }
            }

            Game.Instance.Audio.ContinueOrPlay("overworld");
        }

        public override void OnExit() { }

        public override void Update(float dt)
        {
            _animTime += dt;

            // Navigation
            if (Game.Instance.Input.IsPressed(System.Windows.Forms.Keys.Left))
            {
                _selectedIndex = (_selectedIndex - 1 + _difficulties.Length) % _difficulties.Length;
            }
            if (Game.Instance.Input.IsPressed(System.Windows.Forms.Keys.Right))
            {
                _selectedIndex = (_selectedIndex + 1) % _difficulties.Length;
            }
            if (Game.Instance.Input.IsPressed(System.Windows.Forms.Keys.Up))
            {
                _selectedIndex = (_selectedIndex - 1 + _difficulties.Length) % _difficulties.Length;
            }
            if (Game.Instance.Input.IsPressed(System.Windows.Forms.Keys.Down))
            {
                _selectedIndex = (_selectedIndex + 1) % _difficulties.Length;
            }

            // Confirm selection — replace this scene with the overworld so
            // CharacterSelectScene stays on the stack below for a clean back-nav.
            if (Game.Instance.Input.InteractPressed)
            {
                DifficultyModifiers.CurrentDifficulty = _difficulties[_selectedIndex];
                Game.Instance.Scenes.Replace(new OverworldScene());
            }

            // Cancel
            if (Game.Instance.Input.PausePressed)
            {
                Game.Instance.Scenes.Pop();
            }
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // Background overlay
            using (var br = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
                g.FillRectangle(br, 0, 0, W, H);

            // Title
            using (var f = new Font("Courier New", 24, FontStyle.Bold))
            {
                string title = "SELECT DIFFICULTY";
                SizeF titleSize = g.MeasureString(title, f);
                g.DrawString(title, f, Brushes.LimeGreen, W / 2 - titleSize.Width / 2, H / 4);
            }

            // Draw difficulty options
            int optionY = H / 3;
            int optionSpacing = 80;

            for (int i = 0; i < _difficulties.Length; i++)
            {
                DrawDifficultyOption(g, _difficulties[i], optionY + (i * optionSpacing), i == _selectedIndex);
            }

            // Draw status/hint
            using (var f = new Font("Courier New", 10, FontStyle.Regular))
            {
                g.DrawString(_status, f, Brushes.LightGray, W / 2 - 150, H - 50);
                g.DrawString("← → or ↑ ↓ to select  |  [Enter] to confirm  |  [Esc] to cancel", 
                    f, Brushes.Yellow, W / 2 - 250, H - 30);
            }

            // Draw current description
            using (var f = new Font("Courier New", 11, FontStyle.Regular))
            {
                string desc = DifficultyModifiers.GetDifficultyDescription(_difficulties[_selectedIndex]);
                SizeF descSize = g.MeasureString(desc, f);
                g.DrawString(desc, f, Brushes.White, W / 2 - descSize.Width / 2, H * 0.75f);
            }
        }

        /// <summary>
        /// Draw a single difficulty option
        /// </summary>
        private void DrawDifficultyOption(Graphics g, DifficultyModifiers.Difficulty difficulty, int y, bool isSelected)
        {
            int W = Game.Instance.CanvasWidth;
            int boxWidth = 300;
            int boxHeight = 60;
            int boxX = W / 2 - boxWidth / 2;

            // Background
            Color bgColor = isSelected ? Color.FromArgb(150, 100, 200, 100) : Color.FromArgb(80, 60, 80, 60);
            using (var br = new SolidBrush(bgColor))
            {
                g.FillRectangle(br, boxX, y, boxWidth, boxHeight);
            }

            // Border
            Color borderColor = isSelected ? Color.LimeGreen : Color.Gray;
            int borderWidth = isSelected ? 3 : 1;
            using (var pen = new Pen(borderColor, borderWidth))
            {
                g.DrawRectangle(pen, boxX, y, boxWidth, boxHeight);
            }

            // Text
            using (var f = new Font("Courier New", 14, isSelected ? FontStyle.Bold : FontStyle.Regular))
            {
                string text = DifficultyModifiers.GetDifficultyName(difficulty);
                SizeF textSize = g.MeasureString(text, f);
                Color textColor = isSelected ? Color.Gold : Color.White;
                g.DrawString(text, f, new SolidBrush(textColor),
                    boxX + (boxWidth - textSize.Width) / 2,
                    y + (boxHeight - textSize.Height) / 2);
            }

            // Selection indicator
            if (isSelected)
            {
                using (var f = new Font("Courier New", 16, FontStyle.Bold))
                {
                    g.DrawString("◄", f, Brushes.Gold, boxX - 40, y + boxHeight / 2 - 8);
                    g.DrawString("►", f, Brushes.Gold, boxX + boxWidth + 20, y + boxHeight / 2 - 8);
                }
            }
        }
    }
}
