// ────────────────────────────────────────────────────────────────────────────
// PHASE 2 - Team 6: Narrative Designer
// Feature: Narrative Ops Scene
// Purpose: In-game validation panel for Team 6 Phase 2 narrative systems.
// ────────────────────────────────────────────────────────────────────────────

using System.Drawing;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Runtime scene for Team 6 Phase 2 narrative feature verification.
    /// </summary>
    public sealed class Phase2NarrativeOpsScene : Scene
    {
        private readonly string[] _tabs = { "Dialogue", "Relations", "AudioLogs", "Flashbacks", "Epilogue", "EnvStory", "SideQuests", "Rival", "SecretEnd", "Codex" };
        private int _tab;
        private bool _allBosses;
        private bool _allRelics;
        private int _deaths;

        public override void OnEnter() { }
        public override void OnExit() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Left) && _tab > 0) _tab--;
            if (input.IsPressed(System.Windows.Forms.Keys.Right) && _tab < _tabs.Length - 1) _tab++;

            if (_tab == 1 && input.IsPressed(System.Windows.Forms.Keys.R)) CharacterRelationshipSystem.Add("Swan", 4);
            if (_tab == 4)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.B)) _allBosses = !_allBosses;
                if (input.IsPressed(System.Windows.Forms.Keys.C)) _allRelics = !_allRelics;
            }
            if (_tab == 6 && input.IsPressed(System.Windows.Forms.Keys.Q)) NpcSideQuestsSystem.Complete("quest_" + System.DateTime.Now.Second);
            if (_tab == 7 && input.IsPressed(System.Windows.Forms.Keys.A)) RivalEncountersSystem.Advance();
            if (_tab == 8)
            {
                if (input.IsPressed(System.Windows.Forms.Keys.C)) _allRelics = !_allRelics;
                if (input.IsPressed(System.Windows.Forms.Keys.J)) _deaths = System.Math.Max(0, _deaths - 1);
                if (input.IsPressed(System.Windows.Forms.Keys.K)) _deaths = System.Math.Min(20, _deaths + 1);
            }

            if (input.PausePressed || input.InteractPressed) Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            using (var br = new SolidBrush(Color.FromArgb(18, 16, 24))) g.FillRectangle(br, 0, 0, W, H);
            using (var f = new Font("Courier New", 20, FontStyle.Bold)) g.DrawString("PHASE 2 NARRATIVE OPS", f, Brushes.Gold, 14, 10);

            DrawTabs(g, W);
            var body = new Rectangle(14, 90, W - 28, H - 126);
            using (var br = new SolidBrush(Color.FromArgb(24, 24, 36))) g.FillRectangle(br, body);
            g.DrawRectangle(Pens.DimGray, body);

            switch (_tab)
            {
                case 0: DrawLines(g, body, "Branch Dialogue Trees", new[] { BranchDialogueTreesSystem.Resolve("intro", 0), BranchDialogueTreesSystem.Resolve("intro", 1) }); break;
                case 1: DrawLines(g, body, "Character Relationship System", new[] { "R: add Swan affinity", "Swan=" + CharacterRelationshipSystem.Get("Swan") }); break;
                case 2: DrawLines(g, body, "World Building Audio Logs", WorldBuildingAudioLogsSystem.GetEntries()); break;
                case 3: DrawLines(g, body, "Flashback Scenes", FlashbackScenesSystem.GetScenes()); break;
                case 4: DrawLines(g, body, "Post-Game Epilogue", new[] { "B: bosses  C: relics", $"bosses={_allBosses} relics={_allRelics}", PostGameEpilogueSystem.Resolve(_allBosses, _allRelics) }); break;
                case 5: DrawLines(g, body, "Environmental Storytelling", EnvironmentalStorytellingSystem.Clues()); break;
                case 6: DrawLines(g, body, "NPC Side Quests", new[] { "Q: complete sample quest", "Completed=" + NpcSideQuestsSystem.CompletedCount() }); break;
                case 7: DrawLines(g, body, "Rival Encounters", new[] { "A: advance stage", "Stage=" + RivalEncountersSystem.Stage }); break;
                case 8: DrawLines(g, body, "Secret Ending", new[] { "C: relics  J/K: deaths", $"relics={_allRelics} quests={NpcSideQuestsSystem.CompletedCount()} deaths={_deaths}", "Unlocked=" + SecretEndingSystem.Unlocked(_allRelics, NpcSideQuestsSystem.CompletedCount(), _deaths) }); break;
                default: DrawLines(g, body, "Codex System", CodexSystem.Keys()); break;
            }

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Left/Right tab   Esc/Enter back   R/B/C/Q/A/J/K actions", f, Brushes.DimGray, 14, H - 26);
        }

        private void DrawTabs(Graphics g, int W)
        {
            int x = 14;
            for (int i = 0; i < _tabs.Length; i++)
            {
                bool sel = i == _tab;
                int w = 100;
                if (x + w > W - 20) break;
                var r = new Rectangle(x, 52, w, 28);
                using (var br = new SolidBrush(sel ? Color.FromArgb(70, 130, 220) : Color.FromArgb(40, 40, 55))) g.FillRectangle(br, r);
                g.DrawRectangle(sel ? Pens.Cyan : Pens.Gray, r);
                using (var f = new Font("Courier New", 8, FontStyle.Bold)) g.DrawString(_tabs[i], f, sel ? Brushes.Cyan : Brushes.LightGray, x + 6, 60);
                x += w + 6;
            }
        }

        private static void DrawLines(Graphics g, Rectangle body, string title, System.Collections.Generic.IReadOnlyList<string> lines)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold)) g.DrawString(title, f, Brushes.Cyan, body.X + 10, body.Y + 8);
            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
                foreach (var line in lines)
                {
                    g.DrawString("• " + line, f, Brushes.LightGray, body.X + 12, y);
                    y += 20;
                    if (y > body.Bottom - 12) break;
                }
        }
    }
}
