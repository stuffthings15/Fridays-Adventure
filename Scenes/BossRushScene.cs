// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 – Team 1: Game Director
// Feature: Boss Rush Mode
// Purpose: Fight every boss in sequence with a shared HP pool.
//          Inspired by SMB3's king sequence — all challenges in one run.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Boss Rush — fight all bosses back-to-back with a shared health pool.
    /// Accessible from the Dev Menu or from the VictoryScene "Play Again" option.
    ///
    /// Flow:
    ///   Splash screen → Boss 1 → short fanfare → Boss 2 → ... → Final Score
    ///
    /// Team 1 (Game Director) — Phase 2 Idea 4: Boss Rush Mode.
    /// </summary>
    public sealed class BossRushScene : Scene
    {
        // ── Boss queue ────────────────────────────────────────────────────────
        private sealed class BossEntry
        {
            public string Label;
            public Func<Scene> Factory;
        }

        private static readonly BossEntry[] _bossQueue =
        {
            new BossEntry { Label = "Marine Captain",    Factory = () => new BossScene() },
            new BossEntry { Label = "Fire Lord Sudo",    Factory = () => new WarlordBossScene(WarlordConfig.FireLordSudo()) },
            new BossEntry { Label = "Centipede of Deep", Factory = () => new WarlordBossScene(WarlordConfig.CentipedeOfTheDeep()) },
            new BossEntry { Label = "Storm Lord Vanta",  Factory = () => new WarlordBossScene(WarlordConfig.StormLordVanta()) },
        };

        // ── State ─────────────────────────────────────────────────────────────
        private int   _bossIndex   = 0;
        private bool  _waitingNext = false;  // true between boss clear and next launch
        private float _waitTimer   = 0f;
        private const float WaitBetween = 2.5f;

        // ── Results ───────────────────────────────────────────────────────────
        private int   _bossesDefeated = 0;
        private float _totalTime      = 0f;
        private bool  _complete       = false;

        // ── Fonts ─────────────────────────────────────────────────────────────
        private static readonly Font _bigFont  = new Font("Courier New", 28, FontStyle.Bold);
        private static readonly Font _midFont  = new Font("Courier New", 16, FontStyle.Bold);
        private static readonly Font _smFont   = new Font("Courier New", 11, FontStyle.Bold);

        public BossRushScene() { }

        // ── Lifecycle ─────────────────────────────────────────────────────────

        public override void OnEnter()
        {
            Game.Instance.Audio.ContinueOrPlay("boss");
            // Buff all enemies to Hard mode for the rush
            DifficultyModifiers.CurrentDifficulty = DifficultyModifiers.Difficulty.Hard;
            _waitingNext = true;    // start with the splash then launch first boss
            _waitTimer   = WaitBetween;
        }

        public override void OnExit() { }

        public override void OnResume()
        {
            // Called when a boss scene pops back to us
            Game.Instance.Audio.ContinueOrPlay("boss");

            if (Game.Instance.LevelJustCompleted)
            {
                Game.Instance.LevelJustCompleted = false;
                _bossesDefeated++;
                _bossIndex++;
                SMB3Hud.ShowToast($"Boss {_bossesDefeated} down! {BossesLeft()} to go.");

                if (_bossIndex >= _bossQueue.Length)
                {
                    // All bosses cleared!
                    _complete    = true;
                    _waitingNext = false;
                    Game.Instance.Audio.ContinueOrPlay("overworld");
                    AchievementSystem.Grant("ach_boss_slayer");
                    AchievementSystem.Grant("ach_warlord_bane");
                }
                else
                {
                    _waitingNext = true;
                    _waitTimer   = WaitBetween;
                }
            }
            else
            {
                // Player quit mid-boss — still counts as a run attempt, just exit
                Game.Instance.Scenes.Replace(new OverworldScene());
            }
        }

        public override void Update(float dt)
        {
            if (!_complete) _totalTime += dt;

            if (_complete)
            {
                if (Game.Instance.Input.InteractPressed || Game.Instance.Input.AnyMash)
                {
                    // Show score then return to overworld
                    int score = _bossesDefeated * 5000 + Math.Max(0, 20000 - (int)_totalTime * 10);
                    Game.Instance.Scenes.Replace(new VictoryScene(
                        $"BOSS RUSH COMPLETE!",
                        $"Defeated: {_bossesDefeated}/{_bossQueue.Length}  Time: {_totalTime:F1}s  Bonus: {score:N0}",
                        () => Game.Instance.Scenes.Replace(new OverworldScene())));
                }
                return;
            }

            if (_waitingNext)
            {
                _waitTimer -= dt;
                if (_waitTimer <= 0f && _bossIndex < _bossQueue.Length)
                {
                    _waitingNext = false;
                    // Push the next boss via LevelIntroScene wrapper
                    var entry = _bossQueue[_bossIndex];
                    var node  = new OverworldNode($"rush_{_bossIndex}", entry.Label,
                                                  new System.Drawing.Point(0, 0), NodeType.Boss, true);
                    Game.Instance.Scenes.Push(new LevelIntroScene(
                        1, _bossIndex + 1, entry.Label,
                        nextScene: () => Game.Instance.Scenes.Replace(entry.Factory())));
                }
            }

            if (Game.Instance.Input.PausePressed)
                Game.Instance.Scenes.Replace(new OverworldScene());
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            // Dark backdrop
            g.Clear(Color.FromArgb(12, 8, 20));
            EnvironmentFeatures.DrawStarField(g, W, H);

            if (_complete)
            {
                DrawComplete(g, W, H);
                return;
            }

            // Title
            DrawCentered(g, _bigFont, Brushes.OrangeRed, "BOSS RUSH", W / 2, H / 4);

            // Progress bar
            DrawProgressBar(g, W, H);

            if (_waitingNext)
            {
                string next = _bossIndex < _bossQueue.Length
                    ? $"Next: {_bossQueue[_bossIndex].Label}"
                    : "ALL BOSSES DEFEATED!";
                DrawCentered(g, _midFont, Brushes.White, next, W / 2, (int)(H * 0.55f));
                DrawCentered(g, _smFont, Brushes.LightGray,
                    $"Defeated: {_bossesDefeated}  Time: {_totalTime:F0}s",
                    W / 2, (int)(H * 0.65f));
            }

            SMB3Hud.DrawOverlays(g, W, H);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private int BossesLeft() => _bossQueue.Length - _bossIndex;

        private void DrawCentered(Graphics g, Font font, Brush brush, string text, int cx, int cy)
        {
            SizeF sz = g.MeasureString(text, font);
            g.DrawString(text, font, brush, cx - sz.Width / 2f, cy - sz.Height / 2f);
        }

        private void DrawProgressBar(Graphics g, int W, int H)
        {
            int barW = W - 80, barH = 24;
            int barX = 40, barY = (int)(H * 0.42f);
            using (var br = new SolidBrush(Color.FromArgb(60, 60, 60, 80)))
                g.FillRectangle(br, barX, barY, barW, barH);
            float pct = _bossQueue.Length > 0 ? (float)_bossesDefeated / _bossQueue.Length : 0f;
            using (var br = new SolidBrush(Color.OrangeRed))
                g.FillRectangle(br, barX, barY, (int)(barW * pct), barH);
            g.DrawRectangle(Pens.DarkRed, barX, barY, barW, barH);
            DrawCentered(g, _smFont, Brushes.White,
                $"{_bossesDefeated} / {_bossQueue.Length} Bosses", W / 2, barY + barH / 2);
        }

        private void DrawComplete(Graphics g, int W, int H)
        {
            DrawCentered(g, _bigFont, Brushes.Gold, "ALL BOSSES CLEARED!", W / 2, H / 3);
            DrawCentered(g, _midFont, Brushes.White,
                $"Time: {_totalTime:F1}s   Defeated: {_bossesDefeated}/{_bossQueue.Length}",
                W / 2, (int)(H * 0.5f));
            DrawCentered(g, _smFont, Brushes.LightGray,
                "[Enter] Continue", W / 2, (int)(H * 0.65f));
        }
    }
}
