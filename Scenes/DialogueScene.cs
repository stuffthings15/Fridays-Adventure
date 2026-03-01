using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;

namespace Fridays_Adventure.Scenes
{
    public sealed class DialogueScene : Scene
    {
        private readonly DialogueSequence _seq;
        private int   _lineIndex;
        private int   _charIndex;
        private float _typeTimer;
        private const float TypeSpeed = 0.032f;

        private bool  _showingChoices;
        private int   _selectedChoice;
        private bool  _waitForRelease;

        private static readonly Font _speakerFont = new Font("Courier New", 10, FontStyle.Bold);
        private static readonly Font _textFont    = new Font("Courier New", 11);
        private static readonly Font _choiceFont  = new Font("Courier New", 10);

        public DialogueScene(DialogueSequence seq) => _seq = seq;

        public override void OnEnter()  { }
        public override void OnExit()   { }
        public override void OnPause()  { }
        public override void OnResume() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;

            if (_showingChoices)
            {
                if (!_waitForRelease)
                {
                    if (input.UpHeld   && _selectedChoice > 0)              { _selectedChoice--; _waitForRelease = true; }
                    if (input.DownHeld && _selectedChoice < _seq.Choices.Count - 1) { _selectedChoice++; _waitForRelease = true; }
                    if (input.InteractPressed || input.AttackPressed)
                        CommitChoice(_selectedChoice);
                }
                else if (!input.UpHeld && !input.DownHeld)
                    _waitForRelease = false;
                return;
            }

            // Typing effect
            if (_lineIndex < _seq.Lines.Count)
            {
                string full = _seq.Lines[_lineIndex].Text;
                _typeTimer += dt;
                while (_typeTimer >= TypeSpeed && _charIndex < full.Length)
                { _charIndex++; _typeTimer -= TypeSpeed; }

                // Advance on confirm
                if (input.InteractPressed || input.AttackPressed)
                {
                    if (_charIndex < full.Length)
                        _charIndex = full.Length; // skip to end
                    else
                        NextLine();
                }
            }
        }

        private void NextLine()
        {
            _lineIndex++;
            _charIndex = 0;
            _typeTimer = 0;
            if (_lineIndex >= _seq.Lines.Count)
            {
                if (_seq.Choices.Count > 0)
                    _showingChoices = true;
                else
                    Close(-1);
            }
        }

        private void CommitChoice(int idx)
        {
            var choice = _seq.Choices[idx];
            Game.Instance.CrewBonds += choice.BondChange;
            if (!string.IsNullOrEmpty(choice.FlagToSet))
                Game.Instance.Save.SetFlag(choice.FlagToSet);
            Close(idx);
        }

        private void Close(int choiceIdx)
        {
            _seq.OnDone?.Invoke(choiceIdx);
            Game.Instance.Scenes.Pop();
        }

        // ── Draw ─────────────────────────────────────────────────────────────

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // Semi-transparent backdrop
            using (var br = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
                g.FillRectangle(br, 0, H - 200, W, 200);
            using (var pen = new Pen(Color.FromArgb(180, Color.Cyan), 2))
                g.DrawRectangle(pen, 10, H - 198, W - 20, 196);

            if (_lineIndex >= _seq.Lines.Count) return;
            var line = _seq.Lines[_lineIndex];

            // Speaker name
            using (var br = new SolidBrush(Color.FromArgb(200, Color.DarkSlateBlue)))
                g.FillRectangle(br, 14, H - 198, 200, 24);
            g.DrawString(line.Speaker, _speakerFont, Brushes.Cyan, 18, H - 196);

            // Dialogue text (partial — typing effect)
            string visible = line.Text.Substring(0, Math.Min(_charIndex, line.Text.Length));
            g.DrawString(visible, _textFont, Brushes.White, new RectangleF(16, H - 168, W - 32, 130));

            // Advance prompt
            if (_charIndex >= line.Text.Length && !_showingChoices)
                using (var f = new Font("Courier New", 8))
                    g.DrawString("[ Z / Enter — continue ]", f, Brushes.Gray, W - 200, H - 24);

            // Choices
            if (_showingChoices)
                DrawChoices(g, W, H);
        }

        private void DrawChoices(Graphics g, int W, int H)
        {
            int startY = H - 185;
            for (int i = 0; i < _seq.Choices.Count; i++)
            {
                bool sel = i == _selectedChoice;
                int cy = startY + i * 36;
                if (sel)
                    using (var br = new SolidBrush(Color.FromArgb(80, Color.Cyan)))
                        g.FillRectangle(br, 14, cy - 2, W - 28, 30);
                string prefix = sel ? "► " : "  ";
                using (var br = new SolidBrush(sel ? Color.Yellow : Color.LightGray))
                    g.DrawString(prefix + _seq.Choices[i].Text, _choiceFont, br, 20, cy);
            }
            using (var f = new Font("Courier New", 8))
                g.DrawString("↑↓ select   Z / Enter — confirm", f, Brushes.Gray, W - 240, H - 24);
        }
    }
}
