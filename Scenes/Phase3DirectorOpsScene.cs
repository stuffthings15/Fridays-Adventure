// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 - Team 1: Game Director
// Feature: Director Ops Scene
// Purpose: Interactive validation UI for Team 1 expansion systems.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using System.Linq;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Runtime UI surface for Team 1 Phase 3 systems.
    /// </summary>
    public sealed class Phase3DirectorOpsScene : Scene
    {
        private readonly string[] _tabs =
        {
            "NewGame+",
            "Endless",
            "Challenge",
            "Cosmetics",
            "Achv2",
            "Season",
            "Gauntlet",
            "StoryDLC",
            "Modifiers",
            "WorldTour"
        };

        private int _tab;

        public override void OnEnter() { }
        public override void OnExit() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Left) && _tab > 0) _tab--;
            if (input.IsPressed(System.Windows.Forms.Keys.Right) && _tab < _tabs.Length - 1) _tab++;

            if (_tab == 0 && input.IsPressed(System.Windows.Forms.Keys.N))
            {
                NewGamePlusMode.Enable();
                SMB3Hud.ShowToast("New Game+ enabled.");
            }

            if (_tab == 1)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.S)) EndlessModeSystem.Start();
                if (input.IsPressed(System.Windows.Forms.Keys.W)) EndlessModeSystem.AdvanceWave();
            }

            if (_tab == 3)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.B))
                {
                    string id = "captain-gold";
                    int cost = CosmeticShopEconomy.GetPrice(id);
                    if (Game.Instance.PlayerBounty >= cost)
                    {
                        Game.Instance.PlayerBounty -= cost;
                        CosmeticInventorySystem.AddOwned(id);
                        SMB3Hud.ShowToast("Purchased captain-gold");
                    }
                    else SMB3Hud.ShowToast("Not enough bounty for purchase.");
                }
            }

            if (_tab == 8)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.D1)) CustomGameModifiers.DamageTakenMultiplier = Math.Max(0.5f, CustomGameModifiers.DamageTakenMultiplier - 0.1f);
                if (input.IsPressed(System.Windows.Forms.Keys.D2)) CustomGameModifiers.DamageTakenMultiplier = Math.Min(3.0f, CustomGameModifiers.DamageTakenMultiplier + 0.1f);
                if (input.IsPressed(System.Windows.Forms.Keys.D3)) CustomGameModifiers.ScoreMultiplier = Math.Max(0.5f, CustomGameModifiers.ScoreMultiplier - 0.1f);
                if (input.IsPressed(System.Windows.Forms.Keys.D4)) CustomGameModifiers.ScoreMultiplier = Math.Min(3.0f, CustomGameModifiers.ScoreMultiplier + 0.1f);
                if (input.IsPressed(System.Windows.Forms.Keys.H)) CustomGameModifiers.OneHitMode = !CustomGameModifiers.OneHitMode;
                if (input.IsPressed(System.Windows.Forms.Keys.R)) CustomGameModifiers.Reset();
            }

            if (input.PausePressed || input.InteractPressed)
                Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            using (var br = new SolidBrush(Color.FromArgb(18, 14, 24)))
                g.FillRectangle(br, 0, 0, W, H);

            using (var f = new Font("Courier New", 20, FontStyle.Bold))
                g.DrawString("PHASE 3 DIRECTOR OPS", f, Brushes.Gold, 14, 10);

            DrawTabs(g, W);

            var body = new Rectangle(14, 90, W - 28, H - 126);
            using (var br = new SolidBrush(Color.FromArgb(24, 24, 36)))
                g.FillRectangle(br, body);
            g.DrawRectangle(Pens.DimGray, body);

            switch (_tab)
            {
                case 0: DrawNewGamePlus(g, body); break;
                case 1: DrawEndless(g, body); break;
                case 2: DrawChallenge(g, body); break;
                case 3: DrawCosmetics(g, body); break;
                case 4: DrawAchievement2(g, body); break;
                case 5: DrawSeason(g, body); break;
                case 6: DrawGauntlet(g, body); break;
                case 7: DrawStoryDlc(g, body); break;
                case 8: DrawModifiers(g, body); break;
                default: DrawWorldTour(g, body); break;
            }

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Left/Right: Tab   Esc/Enter: Back   Per-tab hotkeys shown in panel",
                    f, Brushes.DimGray, 14, H - 26);
        }

        private void DrawTabs(Graphics g, int W)
        {
            int x = 14;
            for (int i = 0; i < _tabs.Length; i++)
            {
                bool sel = i == _tab;
                int w = 102;
                if (x + w > W - 20) break;
                var r = new Rectangle(x, 52, w, 28);
                using (var br = new SolidBrush(sel ? Color.FromArgb(80, 120, 220) : Color.FromArgb(40, 40, 55)))
                    g.FillRectangle(br, r);
                g.DrawRectangle(sel ? Pens.Cyan : Pens.Gray, r);
                using (var f = new Font("Courier New", 8, FontStyle.Bold))
                    g.DrawString(_tabs[i], f, sel ? Brushes.Cyan : Brushes.LightGray, x + 6, 60);
                x += w + 6;
            }
        }

        private void DrawNewGamePlus(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("New Game+ Mode", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("Press N to enable New Game+", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString("Active: " + NewGamePlusMode.IsActive(), f,
                    NewGamePlusMode.IsActive() ? Brushes.LimeGreen : Brushes.LightGray,
                    body.X + 12, body.Y + 60);
            }
        }

        private void DrawEndless(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Endless Mode", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("S=start   W=advance wave", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString($"Wave: {EndlessModeSystem.Wave}", f, Brushes.LightGray, body.X + 12, body.Y + 60);
                g.DrawString($"Score: {EndlessModeSystem.Score:N0}", f, Brushes.LightGray, body.X + 12, body.Y + 84);
            }
        }

        private void DrawChallenge(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Challenge of the Week", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
                g.DrawString("Current: " + ChallengeOfWeekSystem.GetCurrentChallenge(), f, Brushes.LightGray, body.X + 12, body.Y + 40);
        }

        private void DrawCosmetics(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Cosmetic Shop", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("B=buy captain-gold", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString($"Price: {CosmeticShopEconomy.GetPrice("captain-gold"):N0}", f, Brushes.LightGray, body.X + 12, body.Y + 60);
                g.DrawString($"Bounty: {Game.Instance.PlayerBounty:N0}", f, Brushes.LightGray, body.X + 12, body.Y + 84);
            }
        }

        private void DrawAchievement2(Graphics g, Rectangle body)
        {
            var lines = AchievementSystem2.GetTierProgress();
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Achievement System 2.0", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
                foreach (var line in lines)
                {
                    g.DrawString("• " + line, f, Brushes.LightGray, body.X + 12, y);
                    y += 22;
                }
        }

        private void DrawSeason(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Seasonal Events", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
                g.DrawString(SeasonalEventsSystem.GetCurrentSeasonEvent(), f, Brushes.LightGray, body.X + 12, body.Y + 40);
        }

        private void DrawGauntlet(Graphics g, Rectangle body)
        {
            var lineup = BossGauntletExtended.GetBossLineup();
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Boss Gauntlet Extended", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
                foreach (var b in lineup)
                {
                    g.DrawString("• " + b, f, Brushes.LightGray, body.X + 12, y);
                    y += 20;
                }
        }

        private void DrawStoryDlc(Graphics g, Rectangle body)
        {
            var lines = StoryDlcPipeline.EnsureAndReadManifest();
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Story DLC Pipeline", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
                foreach (var line in lines)
                {
                    g.DrawString("• " + line, f, Brushes.LightGray, body.X + 12, y);
                    y += 20;
                }
        }

        private void DrawModifiers(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Custom Game Modifiers", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("1/2 damage  3/4 score  H one-hit  R reset", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString($"Damage Taken Mult: {CustomGameModifiers.DamageTakenMultiplier:F1}", f, Brushes.LightGray, body.X + 12, body.Y + 60);
                g.DrawString($"Score Mult:       {CustomGameModifiers.ScoreMultiplier:F1}", f, Brushes.LightGray, body.X + 12, body.Y + 84);
                g.DrawString($"One-Hit Mode:     {CustomGameModifiers.OneHitMode}", f, Brushes.LightGray, body.X + 12, body.Y + 108);
            }
        }

        private void DrawWorldTour(Graphics g, Rectangle body)
        {
            var worlds = WorldTourMode.GetTourSequence();
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("World Tour Mode", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
                foreach (var w in worlds)
                {
                    g.DrawString("• " + w, f, Brushes.LightGray, body.X + 12, y);
                    y += 20;
                }
        }
    }
}
