// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 - Team 4: Lead Game Designer
// Feature: Design Ops Scene
// Purpose: Interactive validation scene for Team 4 Phase 3 design systems.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Runtime scene for Team 4 design system verification.
    /// </summary>
    public sealed class Phase3DesignOpsScene : Scene
    {
        private readonly string[] _tabs =
        {
            "MegaBosses",
            "Roguelike",
            "Progression",
            "RiskReward",
            "Puzzle",
            "TimeAttack",
            "Collectibles",
            "Coop",
            "Ranking",
            "DiffTiers"
        };

        private int _tab;
        private int _seed = 101;
        private int _xp = 0;
        private int _risk = 0;
        private int _bosses = 0;
        private string _status = "Use per-tab keys shown in panel.";

        public override void OnEnter() { }
        public override void OnExit() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Left) && _tab > 0) _tab--;
            if (input.IsPressed(System.Windows.Forms.Keys.Right) && _tab < _tabs.Length - 1) _tab++;

            if (_tab == 1)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.N)) _seed++;
                if (input.IsPressed(System.Windows.Forms.Keys.B)) _seed = Math.Max(1, _seed - 1);
            }
            else if (_tab == 2)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.X)) _xp += 250;
            }
            else if (_tab == 3)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.J)) _risk = Math.Max(0, _risk - 1);
                if (input.IsPressed(System.Windows.Forms.Keys.K)) _risk = Math.Min(3, _risk + 1);
            }
            else if (_tab == 5 && input.IsPressed(System.Windows.Forms.Keys.T))
            {
                TimeAttackLeaderboardsSystem.Record(string.IsNullOrWhiteSpace(Game.Instance.PlayerName) ? "Player" : Game.Instance.PlayerName,
                    TimeSpan.FromMinutes(4.5));
                _status = "Recorded sample time-attack result.";
            }
            else if (_tab == 6 && input.IsPressed(System.Windows.Forms.Keys.C))
            {
                CollectibleHuntingSystem.MarkFound("collectible-" + DateTime.Now.Second);
            }
            else if (_tab == 9)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.J)) _bosses = Math.Max(0, _bosses - 1);
                if (input.IsPressed(System.Windows.Forms.Keys.K)) _bosses = Math.Min(10, _bosses + 1);
            }

            if (input.PausePressed || input.InteractPressed)
                Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            using (var br = new SolidBrush(Color.FromArgb(16, 16, 22)))
                g.FillRectangle(br, 0, 0, W, H);

            using (var f = new Font("Courier New", 20, FontStyle.Bold))
                g.DrawString("PHASE 3 DESIGN OPS", f, Brushes.Gold, 14, 10);

            DrawTabs(g, W);

            var body = new Rectangle(14, 90, W - 28, H - 126);
            using (var br = new SolidBrush(Color.FromArgb(24, 24, 36))) g.FillRectangle(br, body);
            g.DrawRectangle(Pens.DimGray, body);

            switch (_tab)
            {
                case 0: DrawMegaBosses(g, body); break;
                case 1: DrawRoguelike(g, body); break;
                case 2: DrawProgression(g, body); break;
                case 3: DrawRiskReward(g, body); break;
                case 4: DrawPuzzle(g, body); break;
                case 5: DrawTimeAttack(g, body); break;
                case 6: DrawCollectibles(g, body); break;
                case 7: DrawCoop(g, body); break;
                case 8: DrawRanking(g, body); break;
                default: DrawDiffTiers(g, body); break;
            }

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Left/Right: Tab   Esc/Enter: Back   N/B X J/K T C keys per tab", f, Brushes.DimGray, 14, H - 26);
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
                using (var br = new SolidBrush(sel ? Color.FromArgb(70, 130, 220) : Color.FromArgb(40, 40, 55)))
                    g.FillRectangle(br, r);
                g.DrawRectangle(sel ? Pens.Cyan : Pens.Gray, r);
                using (var f = new Font("Courier New", 8, FontStyle.Bold))
                    g.DrawString(_tabs[i], f, sel ? Brushes.Cyan : Brushes.LightGray, x + 6, 60);
                x += w + 6;
            }
        }

        private void DrawMegaBosses(Graphics g, Rectangle body)
        {
            var list = MegaBossesSystem.GetBosses();
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Mega Bosses", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10)) foreach (var b in list) { g.DrawString("• " + b, f, Brushes.LightGray, body.X + 12, y); y += 22; }
        }

        private void DrawRoguelike(Graphics g, Rectangle body)
        {
            var mods = RoguelikeElementsSystem.GenerateRunModifiers(_seed);
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Roguelike Elements", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString($"Seed: {_seed} (N/B)", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString("Positive: " + mods[0], f, Brushes.LightGray, body.X + 12, body.Y + 60);
                g.DrawString("Negative: " + mods[1], f, Brushes.LightGray, body.X + 12, body.Y + 84);
            }
        }

        private void DrawProgression(Graphics g, Rectangle body)
        {
            int lvl = CharacterProgressionSystem.LevelFromXp(_xp);
            int next = CharacterProgressionSystem.XpToNextLevel(_xp);
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Character Progression", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("Press X to add +250 XP", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString($"XP: {_xp:N0}", f, Brushes.LightGray, body.X + 12, body.Y + 60);
                g.DrawString($"Level: {lvl}", f, Brushes.LightGray, body.X + 12, body.Y + 84);
                g.DrawString($"XP to next: {next:N0}", f, Brushes.LightGray, body.X + 12, body.Y + 108);
            }
        }

        private void DrawRiskReward(Graphics g, Rectangle body)
        {
            float m = RiskRewardBalancingSystem.RewardMultiplier(_risk);
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Risk/Reward Balancing", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("J/K adjust risk", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString($"Risk level: {_risk}", f, Brushes.LightGray, body.X + 12, body.Y + 60);
                g.DrawString($"Reward multiplier: x{m:F2}", f, Brushes.LightGray, body.X + 12, body.Y + 84);
            }
        }

        private void DrawPuzzle(Graphics g, Rectangle body)
        {
            var route = PuzzlePlatformingSystem.BuildPuzzleRoute(5);
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Puzzle Platforming", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10)) foreach (var n in route) { g.DrawString("• " + n, f, Brushes.LightGray, body.X + 12, y); y += 20; }
        }

        private void DrawTimeAttack(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Time-Attack Leaderboards", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("Press T to record sample run.", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString(_status, f, Brushes.LightGray, body.X + 12, body.Y + 60);
            }
        }

        private void DrawCollectibles(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Collectible Hunting", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("Press C to mark collectible found.", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString($"Found count: {CollectibleHuntingSystem.CountFound()}", f, Brushes.LightGray, body.X + 12, body.Y + 60);
            }
        }

        private void DrawCoop(Graphics g, Rectangle body)
        {
            var notes = CoopMechanicsDesignSystem.GetDesignNotes();
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Co-op Mechanics Design", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10)) foreach (var n in notes) { g.DrawString("• " + n, f, Brushes.LightGray, body.X + 12, y); y += 22; }
        }

        private void DrawRanking(Graphics g, Rectangle body)
        {
            string rank = SkillBasedRankingSystem.Rank(7200, SessionStats.Instance.DeathCount);
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Skill-Based Ranking", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString($"Sample rank (score=7200, deaths={SessionStats.Instance.DeathCount}): {rank}", f, Brushes.LightGray, body.X + 12, body.Y + 40);
            }
        }

        private void DrawDiffTiers(Graphics g, Rectangle body)
        {
            var tiers = UnlockableDifficultyTiersSystem.GetUnlockedTiers(_bosses);
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Unlockable Difficulty Tiers", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
                g.DrawString("J/K adjust bosses defeated", f, Brushes.Gold, body.X + 12, body.Y + 34);
            int y = body.Y + 60;
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString($"Bosses defeated: {_bosses}", f, Brushes.LightGray, body.X + 12, y); y += 22;
                foreach (var t in tiers) { g.DrawString("• " + t, f, Brushes.LightGray, body.X + 12, y); y += 20; }
            }
        }
    }
}
