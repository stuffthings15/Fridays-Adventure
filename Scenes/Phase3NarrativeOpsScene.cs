// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 - Team 6: Narrative Designer
// Feature: Narrative Ops Scene
// Purpose: In-game validation interface for Team 6 narrative systems.
// ────────────────────────────────────────────────────────────────────────────

using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Runtime scene for Team 6 narrative feature validation.
    /// </summary>
    public sealed class Phase3NarrativeOpsScene : Scene
    {
        private readonly string[] _tabs =
        {
            "Origins",
            "RivalArc",
            "Ending",
            "Romance",
            "Lore",
            "Mentor",
            "Prophecy",
            "Sequel",
            "Consequences",
            "Timeline"
        };

        private int _tab;
        private bool _allBosses;
        private bool _allCollectibles;
        private bool _noDeaths = true;

        public override void OnEnter() { }
        public override void OnExit() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Left) && _tab > 0) _tab--;
            if (input.IsPressed(System.Windows.Forms.Keys.Right) && _tab < _tabs.Length - 1) _tab++;

            if (_tab == 1 && input.IsPressed(System.Windows.Forms.Keys.A)) SecretRivalArcSystem.AdvanceChapter();
            if (_tab == 2)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.B)) _allBosses = !_allBosses;
                if (input.IsPressed(System.Windows.Forms.Keys.C)) _allCollectibles = !_allCollectibles;
                if (input.IsPressed(System.Windows.Forms.Keys.D)) _noDeaths = !_noDeaths;
            }
            if (_tab == 3 && input.IsPressed(System.Windows.Forms.Keys.R)) CharacterRomanceSubplotSystem.AddAffinity("Swan", 5);
            if (_tab == 8 && input.IsPressed(System.Windows.Forms.Keys.K)) CharacterDeathConsequencesSystem.RegisterDeath();

            if (input.PausePressed || input.InteractPressed)
                Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;

            using (var br = new SolidBrush(Color.FromArgb(18, 16, 24)))
                g.FillRectangle(br, 0, 0, W, H);

            using (var f = new Font("Courier New", 20, FontStyle.Bold))
                g.DrawString("PHASE 3 NARRATIVE OPS", f, Brushes.Gold, 14, 10);

            DrawTabs(g, W);

            var body = new Rectangle(14, 90, W - 28, H - 126);
            using (var br = new SolidBrush(Color.FromArgb(24, 24, 36))) g.FillRectangle(br, body);
            g.DrawRectangle(Pens.DimGray, body);

            switch (_tab)
            {
                case 0: DrawOrigins(g, body); break;
                case 1: DrawRivalArc(g, body); break;
                case 2: DrawEnding(g, body); break;
                case 3: DrawRomance(g, body); break;
                case 4: DrawLore(g, body); break;
                case 5: DrawMentor(g, body); break;
                case 6: DrawProphecy(g, body); break;
                case 7: DrawSequel(g, body); break;
                case 8: DrawConsequences(g, body); break;
                default: DrawTimeline(g, body); break;
            }

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Left/Right: tab   Esc/Enter: back   A/B/C/D/R/K keys per tab", f, Brushes.DimGray, 14, H - 26);
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

        private void DrawOrigins(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Character Origins", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("• Miss Friday: " + CharacterOriginsSystem.GetOrigin("Miss Friday"), f, Brushes.LightGray, body.X + 12, body.Y + 40);
                g.DrawString("• Orca: " + CharacterOriginsSystem.GetOrigin("Orca"), f, Brushes.LightGray, body.X + 12, body.Y + 66);
                g.DrawString("• Swan: " + CharacterOriginsSystem.GetOrigin("Swan"), f, Brushes.LightGray, body.X + 12, body.Y + 92);
            }
        }

        private void DrawRivalArc(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Secret Rival Arc", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("A: advance chapter", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString($"Current chapter: {SecretRivalArcSystem.CurrentChapter}", f, Brushes.LightGray, body.X + 12, body.Y + 60);
            }
        }

        private void DrawEnding(Graphics g, Rectangle body)
        {
            string key = MultiverseEndingSystem.ResolveEnding(_allBosses, _allCollectibles, _noDeaths);
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Multiverse Ending", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("B: bosses  C: collectibles  D: no-deaths", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString($"bosses={_allBosses} collectibles={_allCollectibles} noDeaths={_noDeaths}", f, Brushes.LightGray, body.X + 12, body.Y + 60);
                g.DrawString("Ending key: " + key, f, Brushes.LightGray, body.X + 12, body.Y + 84);
            }
        }

        private void DrawRomance(Graphics g, Rectangle body)
        {
            int swan = CharacterRomanceSubplotSystem.GetAffinity("Swan");
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Character Romance Subplot", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("R: add affinity for Swan", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString("Swan affinity: " + swan, f, Brushes.LightGray, body.X + 12, body.Y + 60);
            }
        }

        private void DrawLore(Graphics g, Rectangle body)
        {
            var lines = WorldLoreExpansionSystem.GetEntries();
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("World Lore Expansion", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10)) foreach (var l in lines) { g.DrawString("• " + l, f, Brushes.LightGray, body.X + 12, y); y += 22; }
        }

        private void DrawMentor(Graphics g, Rectangle body)
        {
            string msg = MentorCharacterSystem.GetGuidance("final_fortress");
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Mentor Character", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10)) g.DrawString(msg, f, Brushes.LightGray, body.X + 12, body.Y + 40);
        }

        private void DrawProphecy(Graphics g, Rectangle body)
        {
            var lines = AncientProphecySystem.GetFragments();
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Ancient Prophecy", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10)) foreach (var l in lines) { g.DrawString("• " + l, f, Brushes.LightGray, body.X + 12, y); y += 22; }
        }

        private void DrawSequel(Graphics g, Rectangle body)
        {
            string hook = PostCreditSequelHookSystem.GetHook();
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Post-Credit Sequel Hook", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10)) g.DrawString(hook, f, Brushes.LightGray, body.X + 12, body.Y + 40);
        }

        private void DrawConsequences(Graphics g, Rectangle body)
        {
            string tag = CharacterDeathConsequencesSystem.GetConsequenceTag();
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Character Death Consequences", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
            {
                g.DrawString("K: register narrative death", f, Brushes.Gold, body.X + 12, body.Y + 34);
                g.DrawString("Consequence tag: " + tag, f, Brushes.LightGray, body.X + 12, body.Y + 60);
            }
        }

        private void DrawTimeline(Graphics g, Rectangle body)
        {
            string branch = TimelineSplitSystem.ResolveBranch(SecretRivalArcSystem.CurrentChapter >= 3, true);
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString("Timeline Split", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10)) g.DrawString("Branch: " + branch, f, Brushes.LightGray, body.X + 12, body.Y + 40);
        }
    }
}
