using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using Fridays_Adventure.Data;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    public enum NodeType { Start, Island, Storm, Boss }

    public sealed class OverworldNode
    {
        public string   Id       { get; }
        public string   Name     { get; }
        public Point    Pos      { get; }
        public NodeType Type     { get; }
        public bool     Visited  { get; set; }
        public bool     Unlocked { get; set; }
        public List<string> Links { get; } = new List<string>();

        public OverworldNode(string id, string name, Point pos, NodeType type, bool unlocked = false)
        { Id = id; Name = name; Pos = pos; Type = type; Unlocked = unlocked; }

        public bool HitTest(Point p) => Math.Abs(p.X - Pos.X) < 20 && Math.Abs(p.Y - Pos.Y) < 20;
    }

    public sealed class OverworldScene : Scene
    {
        private List<OverworldNode> _nodes;
        private OverworldNode _current;
        private OverworldNode _selected;
        private float _anim;
        private string _status = "Choose your next destination.";
        private Bitmap _bg;
        private Rectangle _mainMenuBtn;
        private Rectangle _crewBtn;

        /// <summary>
        /// The node that was most recently launched as a gameplay scene.
        /// Cleared in OnResume once the completion is processed.
        /// </summary>
        private OverworldNode _pendingNode;

        public override void OnEnter()
        {
            BuildNodes();

            // ── Restore saved progress ────────────────────────────────────
            // Restore visited flags from save data, then unlock only nodes
            // that are directly linked to a visited node. This enforces
            // strict sequential progression — no skipping levels.
            foreach (var node in _nodes)
            {
                if (Game.Instance.Save.GetFlag($"node_visited_{node.Id}"))
                    node.Visited = true;
            }

            // Walk the chain: for each visited node, unlock its linked neighbors.
            // This means the player can only reach the NEXT unvisited node.
            foreach (var node in _nodes)
            {
                if (!node.Visited) continue;
                foreach (var linkId in node.Links)
                {
                    var linked = Find(linkId);
                    if (linked != null) linked.Unlocked = true;
                }
            }

            // Set current position from save, defaulting to the start node.
            // CurrentNodeId records where the player last entered a level;
            // on fresh load we want the ship at start so the player can choose.
            _current = Find("start");
            _current.Visited = true;

            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                       "Assets", "Sprites", "bg_overworld.png");
            if (File.Exists(path)) _bg = new Bitmap(path);

            // Phase 3 — Hammer Bros patrol spawns (two per chapter style).
            HammerBrosSystem.Spawn("sky", "harbor");

            // Force the overworld track — ContinueOrPlay would skip if the
            // title theme is still flagged as playing after a scene Replace.
            Game.Instance.Audio.PlayMood("overworld");
        }

        public override void OnExit()   { _bg?.Dispose(); _bg = null; }

        // World names for the SMB3 WorldTitleScene card (1-indexed by world number)
        private static readonly string[] WorldNames =
        {
            "",                     // [0] unused
            "Dinosaur Shores",      // World 1
            "The Grand Line",       // World 2
            "Tide of the Lost",     // World 3
        };

        // Island IDs that count as regular island nodes for Toad House eligibility
        private static readonly System.Collections.Generic.HashSet<string> IslandNodeIds =
            new System.Collections.Generic.HashSet<string>
            { "dino","sky","wano","harbor","coral","tundra","dive_gate","sunken_gate","kelp","boiling_vent","abyss" };

        private static readonly Random _rng = new Random();

        public override void OnResume()
        {
            // Force the overworld track — the level or CourseClearScene may
            // have been playing a different mood (e.g. "clear", "combat").
            Game.Instance.Audio.PlayMood("overworld");

            // Only process a level completion if a gameplay scene was pending AND it
            // returned successfully (LevelJustCompleted flag set by the scene itself).
            if (_pendingNode != null && Game.Instance.LevelJustCompleted)
            {
                Game.Instance.LevelJustCompleted = false;

                // Check if this is a first-time completion BEFORE setting the flag
                bool firstTime = !Game.Instance.Save.GetFlag($"node_visited_{_pendingNode.Id}");

                // Mark the completed node as visited and persist to save
                _pendingNode.Visited = true;
                Game.Instance.Save.SetFlag($"node_visited_{_pendingNode.Id}");

                // Unlock the nodes connected to the completed node (progression gate)
                foreach (var id in _pendingNode.Links)
                {
                    var linked = Find(id);
                    if (linked != null) linked.Unlocked = true;
                }

                // Increment the campaign level counter only on first-time
                // completions so replaying levels doesn't inflate the
                // displayed World number in the HUD.
                if (_pendingNode.Type != NodeType.Start && firstTime)
                    Game.Instance.CurrentLevel++;

                // Auto-save after completion processing
                Game.Instance.SyncRuntimeToSaveData();
                Game.Instance.Save.Save();
                SMB3Hud.ShowToast("Progress saved.");

                // ── END CONDITION ─────────────────────────────────────────────
                if (AllIslandsCompleted())
                {
                    Game.Instance.Scenes.Replace(new VictoryScene(
                        "ALL ISLANDS CONQUERED!",
                        $"All 11 Islands Explored   Score: {Game.Instance.PlayerBounty:N0}",
                        () => Game.Instance.Scenes.Replace(new CreditsScene())));
                    return;
                }

                // ── Achievement: first island complete ────────────────────────
                AchievementSystem.Grant("ach_first_step");

                // ── World-title card when entering a new world (every 3 islands) ─
                int lvl = Game.Instance.CurrentLevel;
                int worldNum  = ((lvl - 1) / 3) + 1;
                int prevWorld = ((lvl - 2) / 3) + 1;
                if (lvl > 1 && worldNum != prevWorld)
                {
                    string wName  = worldNum < WorldNames.Length ? WorldNames[worldNum] : $"World {worldNum}";
                    Game.Instance.WorldNumber = worldNum;
                    Game.Instance.LevelNumber = 1;
                    Game.Instance.Scenes.Push(new WorldTitleScene(worldNum, wName, () =>
                    {
                        Game.Instance.Audio.ContinueOrPlay("overworld");
                    }));
                    _pendingNode = null;
                    return;
                }

                // ── Toad House: 30 % chance after completing any island node ──
                if (IslandNodeIds.Contains(_pendingNode.Id) && _rng.NextDouble() < 0.30)
                {
                    Game.Instance.Scenes.Push(new ToadHouseScene());
                    _pendingNode = null;
                    return;
                }

                _status = $"Level {Game.Instance.CurrentLevel} — Choose your next destination.";
            }
            else if (_pendingNode != null)
            {
                // Returned without clearing (e.g. quit via pause) — do NOT advance level.
                _status = "Choose your next destination.";
            }

            _pendingNode = null;
        }

        /// <summary>
        /// Returns true if ALL 17 levels have been completed (11 story islands + 6 bosses).
        /// Victory requires beating every area in the game, not just islands.
        /// </summary>
        private bool AllIslandsCompleted()
        {
            // ALL 17 LEVELS REQUIRED FOR VICTORY
            string[] allLevelIds = { 
                // Story Islands (11) - required
                "dino", "sky", "wano", "harbor", "coral", "tundra", 
                "dive_gate", "sunken_gate", "kelp", "boiling_vent", "abyss",
                // Boss/Storm Encounters (6) - ALSO REQUIRED
                "storm1", "blockade", "warlord1", "storm2", "warlord2", "centipede_final"
            };

            foreach (string id in allLevelIds)
            {
                var node = Find(id);
                if (node == null || !node.Visited)
                    return false;
            }
            return true;
        }

        private void BuildNodes()
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            _nodes = new List<OverworldNode>
            {
                // ── Original game ──────────────────────────────────────────
                new OverworldNode("start",          "Sea Serpent",        new Point((int)(W*0.10f), H/2),            NodeType.Start,  true),
                new OverworldNode("dino",           "Dinosaur Island",    new Point((int)(W*0.28f), (int)(H*0.42f)), NodeType.Island, true),
                new OverworldNode("storm1",         "Storm Belt",         new Point((int)(W*0.46f), (int)(H*0.55f)), NodeType.Storm,  false),
                new OverworldNode("sky",            "Sky Island",         new Point((int)(W*0.62f), (int)(H*0.30f)), NodeType.Island, false),
                new OverworldNode("wano",           "Blade Nation",       new Point((int)(W*0.82f), (int)(H*0.48f)), NodeType.Island, false),
                new OverworldNode("blockade",       "Marine Blockade",    new Point((int)(W*0.55f), (int)(H*0.68f)), NodeType.Boss,   false),
                new OverworldNode("warlord1",       "Warlord: Sudo",      new Point((int)(W*0.92f), (int)(H*0.30f)), NodeType.Boss,   false),
                // ── Sequel expansion ───────────────────────────────────
                new OverworldNode("harbor",         "Harbor Town",        new Point((int)(W*0.65f), (int)(H*0.72f)), NodeType.Island, false),
                new OverworldNode("coral",          "Coral Reef",         new Point((int)(W*0.55f), (int)(H*0.82f)), NodeType.Island, false),
                new OverworldNode("tundra",         "Tundra Peak",        new Point((int)(W*0.75f), (int)(H*0.82f)), NodeType.Island, false),
                new OverworldNode("storm2",         "Tempest Strait",     new Point((int)(W*0.85f), (int)(H*0.67f)), NodeType.Storm,  false),
                new OverworldNode("warlord2",       "Warlord: Vanta",     new Point((int)(W*0.92f), (int)(H*0.55f)), NodeType.Boss,   false),
                // ── Underwater chapter (Tide of the Lost) ───────────────
                new OverworldNode("dive_gate",      "Dive Gate",          new Point((int)(W*0.12f), (int)(H*0.60f)), NodeType.Island, false),
                new OverworldNode("sunken_gate",    "Sunken Gate",        new Point((int)(W*0.08f), (int)(H*0.68f)), NodeType.Island, false),
                new OverworldNode("kelp",           "Kelp Maze",          new Point((int)(W*0.08f), (int)(H*0.78f)), NodeType.Island, false),
                new OverworldNode("boiling_vent",   "Vent Ruins",         new Point((int)(W*0.18f), (int)(H*0.82f)), NodeType.Island, false),
                new OverworldNode("abyss",          "Abyss",              new Point((int)(W*0.28f), (int)(H*0.75f)), NodeType.Island, false),
                new OverworldNode("centipede_final","Centipede",          new Point((int)(W*0.22f), (int)(H*0.53f)), NodeType.Boss,   false),
            };
            Link("start","dino"); Link("dino","storm1"); Link("storm1","sky");
            Link("storm1","blockade"); Link("sky","wano"); Link("blockade","wano");
            Link("wano","warlord1");
            // Sequel
            Link("warlord1","harbor");
            Link("harbor","coral"); Link("harbor","tundra");
            Link("coral","storm2"); Link("tundra","storm2");
            Link("storm2","warlord2");
            // Underwater chapter
            Link("harbor","dive_gate");
            Link("dive_gate","sunken_gate");
            Link("sunken_gate","kelp");
            Link("kelp","boiling_vent");
            Link("boiling_vent","abyss");
            Link("abyss","centipede_final");
            // Team 1 (Game Director) — Idea 5: centipede gauntlet unlocks Warlord Sudo.
            // Requested: centipede_final connects to Warlord Sudo on the campaign map.
            Link("centipede_final","warlord1");
            _current = Find("start");
            _current.Visited = true;
        }

        private void Link(string a, string b)
        { Find(a)?.Links.Add(b); Find(b)?.Links.Add(a); }

        private OverworldNode Find(string id)
        { foreach (var n in _nodes) if (n.Id == id) return n; return null; }

        public override void HandleClick(Point p)
        {
            if (HandleDevMenuClick(p)) return;
            if (_mainMenuBtn.Contains(p))
            {
                Game.Instance.Scenes.ReplaceAll(new TitleScene());
                return;
            }
            if (_crewBtn.Contains(p))
            {
                Game.Instance.Scenes.Push(new CrewScene());
                return;
            }
            foreach (var n in _nodes)
            {
                if (!n.HitTest(p)) continue;
                _selected = n;
                _status = n.Unlocked ? $"Travel to {n.Name}?  [Enter] to confirm." : $"{n.Name} — Not yet reachable.";
                return;
            }
            _selected = null;
            _status   = "Choose your next destination.";
        }

        public override void Update(float dt)
        {
            _anim += dt;

            // ── Hammer Bros patrol (Phase 3 — Team 1 Ideas 8–10) ──────────────
            HammerBrosSystem.Update(
                dt,
                _current?.Id,
                nodeId =>
                {
                    var n = Find(nodeId);
                    return (IReadOnlyList<string>)n?.Links;
                });

            if (!string.IsNullOrEmpty(HammerBrosSystem.PendingEncounterNodeId))
            {
                var encounterNodeId = HammerBrosSystem.PendingEncounterNodeId;
                HammerBrosSystem.PendingEncounterNodeId = null;

                var hbNode = Find(encounterNodeId) ?? _current ?? _nodes[0];
                _pendingNode = hbNode;

                // Reuse island pipeline for an encounter (intro card + return flow)
                LaunchLevel(hbNode, () => new IslandScene("hammer_bros", "Hammer Bros!"));
                return;
            }

            // ── N-Spade mini-game auto-trigger after 80+ berries ─────────────
            // Team 1 (Game Director) — Idea 7: N-Spade card entry.
            bool enoughBerries = Game.Instance.TotalBerriesCollected >= 80;
            if (Game.Instance.Input.IsPressed(System.Windows.Forms.Keys.N)
                && enoughBerries && !GameDirector.Instance.NSpadeHintShown)
            {
                GameDirector.Instance.NSpadeHintShown = true;
                _status = "N-SPADE! Match cards to win items.";
                Game.Instance.Scenes.Push(new CardMiniGameScene());
                return;
            }
            else if (enoughBerries && !GameDirector.Instance.NSpadeHintShown)
            {
                _status = "You have 80+ berries! Press N for N-Spade card game!";
            }

            if (Game.Instance.Input.PausePressed)
                Game.Instance.Scenes.ReplaceAll(new TitleScene());
            if (Game.Instance.Input.InteractPressed && _selected != null && _selected.Unlocked)
                Travel(_selected);
        }

        private void Travel(OverworldNode node)
        {
            _current        = node;
            node.Visited    = true;
            // Persist visited flag so progression survives save/load
            Game.Instance.Save.SetFlag($"node_visited_{node.Id}");
            _selected       = null;
            Game.Instance.Save.CurrentNodeId = node.Id;
            Systems.ThreatSystem.OnNodeTraversed();
            // NOTE: node links are unlocked in OnResume *after* the level is successfully
            // cleared — this ensures progression only advances on completion, not on entry.

            // Pre-set _pendingNode for direct-push branches (no dialogue).
            // TriggerDialogueThen overrides this inside its callback after the dialogue pops,
            // so dialogue-first branches also work correctly.
            _pendingNode = node;

            if (node.Id == "storm1")
            {
                LaunchLevel(node, () => new StormScene());
            }
            else if (node.Id == "blockade")
            {
                // Team 5 (Level Designer) — Fortress level integration.
                LaunchLevel(node, () => new FortressScene());
            }
            else if (node.Id == "warlord1")
            {
                TriggerDialogueThen(Dialogues.MarineEncounter(), () =>
                    LaunchLevel(node, () => new WarlordBossScene(WarlordConfig.FireLordSudo())));
            }
            else if (node.Id == "centipede_final")
            {
                TriggerDialogueThen(Dialogues.MarineEncounter(), () =>
                    LaunchLevel(node, () => new WarlordBossScene(WarlordConfig.CentipedeOfTheDeep())));
            }
            else if (node.Id == "dive_gate")
            {
                // Team 5 (Level Designer) — underwater chapter entry point.
                LaunchLevel(node, () => new UnderwaterScene());
            }
            else if (node.Id == "sunken_gate")
            {
                bool firstVisit = !Game.Instance.Save.GetFlag(NarrativeFlags.SunkenGateVisited);
                Game.Instance.Save.SetFlag(NarrativeFlags.SunkenGateVisited);
                if (firstVisit)
                    TriggerDialogueThen(Dialogues.OrcaJoinsCrew(), () =>
                        LaunchLevel(node, () => new UnderwaterScene()));
                else
                    LaunchLevel(node, () => new UnderwaterScene());
            }
            else if (node.Id == "kelp")
            {
                Game.Instance.Save.SetFlag(NarrativeFlags.KelpVisited);
                LaunchLevel(node, () => new UnderwaterScene());
            }
            else if (node.Id == "boiling_vent")
            {
                Game.Instance.Save.SetFlag(NarrativeFlags.BoilingVentVisited);
                LaunchLevel(node, () => new UnderwaterScene());
            }
            else if (node.Id == "abyss")
            {
                bool firstVisit = !Game.Instance.Save.GetFlag(NarrativeFlags.AbyssVisited);
                Game.Instance.Save.SetFlag(NarrativeFlags.AbyssVisited);
                if (firstVisit)
                    TriggerDialogueThen(Dialogues.SwanJoinsCrew(), () =>
                        LaunchLevel(node, () => new UnderwaterScene()));
                else
                    LaunchLevel(node, () => new UnderwaterScene());
            }
            else if (node.Type == NodeType.Boss)
            {
                TriggerDialogueThen(Dialogues.MarineEncounter(), () =>
                    LaunchLevel(node, () => new BossScene()));
            }
            else if (node.Id == "sky")
            {
                bool firstVisit = !Game.Instance.Save.GetFlag("sky_visited");
                Game.Instance.Save.SetFlag("sky_visited");
                if (firstVisit)
                    TriggerDialogueThen(Dialogues.MeetAmelia(), () =>
                        LaunchLevel(node, () => new SkyIslandScene()));
                else
                    LaunchLevel(node, () => new SkyIslandScene());
            }
            else if (node.Id == "wano")
            {
                bool firstVisit = !Game.Instance.Save.GetFlag("wano_visited");
                Game.Instance.Save.SetFlag("wano_visited");
                if (firstVisit)
                    TriggerDialogueThen(Dialogues.BladeSamuriGate(), () =>
                        LaunchLevel(node, () => new IslandScene(node.Id, node.Name)));
                else
                    LaunchLevel(node, () => new IslandScene(node.Id, node.Name));
            }
            else if (node.Id == "harbor")
            {
                bool firstVisit = !Game.Instance.Save.GetFlag(NarrativeFlags.HarborVisited);
                Game.Instance.Save.SetFlag(NarrativeFlags.HarborVisited);
                if (firstVisit)
                    TriggerDialogueThen(Dialogues.MeetOrca(), () =>
                        LaunchLevel(node, () => new IslandScene(node.Id, node.Name)));
                else
                    LaunchLevel(node, () => new IslandScene(node.Id, node.Name));
            }
            else if (node.Id == "coral")
            {
                bool firstVisit = !Game.Instance.Save.GetFlag(NarrativeFlags.CoralVisited);
                Game.Instance.Save.SetFlag(NarrativeFlags.CoralVisited);
                if (firstVisit)
                    TriggerDialogueThen(Dialogues.MeetSwan(), () =>
                        LaunchLevel(node, () => new IslandScene(node.Id, node.Name)));
                else
                    LaunchLevel(node, () => new IslandScene(node.Id, node.Name));
            }
            else if (node.Id == "tundra")
            {
                Game.Instance.Save.SetFlag(NarrativeFlags.TundraVisited);
                LaunchLevel(node, () => new IslandScene(node.Id, node.Name));
            }
            else if (node.Id == "storm2")
            {
                // Team 5 (Level Designer) — Airship level integration.
                LaunchLevel(node, () => new AirshipLevelScene(), isAirship: true);
            }
            else if (node.Id == "warlord2")
            {
                TriggerDialogueThen(Dialogues.MarineEncounter(), () =>
                    LaunchLevel(node, () => new WarlordBossScene(WarlordConfig.StormLordVanta())));
            }
            else if (node.Id == "dino")
            {
                bool firstVisit = !Game.Instance.Save.GetFlag("dino_visited");
                Game.Instance.Save.SetFlag("dino_visited");
                if (firstVisit)
                    TriggerDialogueThen(Dialogues.MeetFinn(), () =>
                        LaunchLevel(node, () => new IslandScene(node.Id, node.Name)));
                else
                    LaunchLevel(node, () => new IslandScene(node.Id, node.Name));
            }
            else if (node.Type == NodeType.Island)
                LaunchLevel(node, () => new IslandScene(node.Id, node.Name));
        }

        private void TriggerDialogueThen(DialogueSequence seq, Action then)
        {
            // Capture the current node so OnResume can credit it on success.
            var nodeToCredit = _current;
            seq.OnDone = _ =>
            {
                _pendingNode = nodeToCredit;
                then?.Invoke();
            };
            Game.Instance.Scenes.Push(new DialogueScene(seq));
        }

        /// <summary>
        /// Wraps a level-scene factory in an SMB3-style LevelIntroScene card,
        /// then replaces it with the actual level. Handles WorldNumber/LevelNumber
        /// label update for the HUD automatically.
        /// </summary>
        private void LaunchLevel(OverworldNode node, Func<Scene> factory,
                                 bool isAirship = false, bool isToadHouse = false)
        {
            // Derive the correct world and level-within-world numbers from the
            // global CurrentLevel counter.  Worlds change every 3 levels:
            //   CurrentLevel 1,2,3 → World 1  Levels 1,2,3
            //   CurrentLevel 4,5,6 → World 2  Levels 1,2,3
            //   etc.
            int cur = Math.Max(1, Game.Instance.CurrentLevel);
            int worldNum  = ((cur - 1) / 3) + 1;
            int levelInWorld = ((cur - 1) % 3) + 1;

            Game.Instance.WorldNumber = worldNum;
            Game.Instance.LevelNumber = levelInWorld;
            Game.Instance.LevelElapsedSeconds = 0f;
            // Set the display name so the GameHUD shows the level name at the top
            Game.Instance.CurrentLevelName = node.Name.ToUpperInvariant();

            var intro = new LevelIntroScene(
                worldNum,
                levelInWorld,
                node.Name,
                nextScene: () => Game.Instance.Scenes.Replace(factory()),
                isAirship:  isAirship,
                isToadHouse: isToadHouse);

            Game.Instance.Scenes.Push(intro);
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            DrawOcean(g, W, H);
            DrawLinks(g);
            DrawNodes(g);

            // Draw Hammer Bros patrol icons on top of the map nodes.
            HammerBrosSystem.Draw(g,
        nodeId =>
        {
            var n = Find(nodeId);
            return n == null ? (Point?)null : n.Pos;
        },
        _anim);

            DrawShip(g);
            DrawIslandChecklist(g, W, H);  // ── Island completion checklist
            DrawHUD(g, W, H);
            DrawMainMenuButton(g);
            DrawDevMenuButton(g);
        }

        private void DrawOcean(Graphics g, int W, int H)
        {
            // Always draw a procedural blue ocean — no image dependency
            using (var br = new LinearGradientBrush(new Rectangle(0, 0, W, H),
                Color.FromArgb(15, 60, 160), Color.FromArgb(10, 100, 200), 90f))
                g.FillRectangle(br, 0, 0, W, H);
            // Wave shimmer lines
            using (var pen = new Pen(Color.FromArgb(30, 180, 220, 255), 1))
            {
                for (int wy = 20; wy < H; wy += 36)
                {
                    float offset = (float)(Math.Sin(_anim * 0.8 + wy * 0.05) * 12);
                    for (int wx = -20; wx < W; wx += 60)
                        g.DrawLine(pen, wx + offset, wy, wx + 30 + offset, wy);
                }
            }
        }

        private void DrawLinks(Graphics g)
        {
            // Draw the complete route graph as dotted red flow lines.
            // Unlocked paths get a bright, thick stroke; locked paths are dark and thin.
            // Team 1  (Game Director)      — visual clarity of progression routes.
            // Team 9  (UI Programmer)      — darker unlocked lines improve readability.
            // Team 15 (UI/UX Artist)       — thicker dots match SMB3 map path style.
            // Requested change: darker, thicker red dotted lines throughout.
            var seen = new HashSet<string>();
            foreach (var n in _nodes)
                foreach (var id in n.Links)
                {
                    string key = string.Compare(n.Id, id, StringComparison.Ordinal) < 0
                               ? n.Id + "|" + id : id + "|" + n.Id;
                    if (!seen.Add(key)) continue;
                    var other = Find(id);
                    if (other == null) continue;

                    bool unlockedPath = n.Unlocked && other.Unlocked;

                    // Unlocked: deep crimson, 5 px wide.  Locked: dark maroon, 3 px wide.
                    Color lineColor = unlockedPath
                        ? Color.FromArgb(240, 220, 30, 30)    // deep crimson — high contrast
                        : Color.FromArgb(150, 120, 20, 20);   // dark maroon  — subtle locked hint

                    float lineWidth = unlockedPath ? 5f : 3f;

                    using (var pen = new Pen(lineColor, lineWidth) { DashStyle = DashStyle.Dot })
                        g.DrawLine(pen, n.Pos, other.Pos);

                    // Extra glow pass on unlocked paths (slightly wider, semi-transparent).
                    if (unlockedPath)
                        using (var glowPen = new Pen(Color.FromArgb(50, 255, 60, 60), lineWidth + 4f) { DashStyle = DashStyle.Dot })
                            g.DrawLine(glowPen, n.Pos, other.Pos);
                }
        }

        private void DrawNodes(Graphics g)
        {
            foreach (var n in _nodes)
            {
                // Procedural island landmass under each node
                DrawIslandLandmass(g, n);

                if (n == _current)
                {
                    float pulse = (float)(Math.Sin(_anim * 3) * 5 + 16);
                    using (var pen = new Pen(Color.FromArgb(160, Color.Cyan), 2))
                        g.DrawEllipse(pen, n.Pos.X-(int)pulse, n.Pos.Y-(int)pulse,
                                      (int)(pulse*2), (int)(pulse*2));
                }
                if (n == _selected)
                    using (var pen = new Pen(Color.Yellow, 2))
                        g.DrawEllipse(pen, n.Pos.X-22, n.Pos.Y-22, 44, 44);

                // Node fill color: visited (completed) nodes get bright gold,
                // unlocked nodes keep their type color, locked nodes are dim.
                Color fill;
                if (n.Visited)
                {
                    // Completed — bright gold so the player sees progress at a glance
                    fill = Color.FromArgb(220, Color.Gold);
                }
                else
                {
                    switch (n.Type)
                    {
                        case NodeType.Island: fill = Color.FromArgb(n.Unlocked?200:70, Color.ForestGreen); break;
                        case NodeType.Storm:  fill = Color.FromArgb(n.Unlocked?200:70, Color.SlateBlue);   break;
                        case NodeType.Boss:   fill = Color.FromArgb(n.Unlocked?200:70, Color.Crimson);     break;
                        default:              fill = Color.FromArgb(n.Unlocked?200:70, Color.Gold);        break;
                    }
                }
                using (var br = new SolidBrush(fill))
                    g.FillEllipse(br, n.Pos.X-14, n.Pos.Y-14, 28, 28);
                using (var pen = new Pen(n.Visited ? Color.White : Color.DimGray, 1))
                    g.DrawEllipse(pen, n.Pos.X-14, n.Pos.Y-14, 28, 28);

                // Bold red X overlay on completed (visited) islands — provides
                // an unmistakable "defeated" indicator visible at a glance.
                // Skips the Start node since it's not a real level.
                if (n.Visited && n.Type != NodeType.Start)
                {
                    int xArm = 10; // half-size of the X in pixels
                    using (var xPen = new Pen(Color.FromArgb(230, 220, 30, 30), 3.5f))
                    {
                        g.DrawLine(xPen, n.Pos.X - xArm, n.Pos.Y - xArm,
                                         n.Pos.X + xArm, n.Pos.Y + xArm);
                        g.DrawLine(xPen, n.Pos.X + xArm, n.Pos.Y - xArm,
                                         n.Pos.X - xArm, n.Pos.Y + xArm);
                    }
                }

                using (var f = new Font("Courier New", 10, FontStyle.Bold))
                {
                    SizeF sz = g.MeasureString(n.Name, f);
                    float tx = n.Pos.X - sz.Width / 2f;
                    float ty = n.Pos.Y + 22;
                    // Background chip for contrast
                    using (var chip = new SolidBrush(Color.FromArgb(200, 0, 0, 0)))
                        g.FillRectangle(chip, tx - 4, ty - 2, sz.Width + 8, sz.Height + 3);
                    // Shadow
                    g.DrawString(n.Name, f, Brushes.Black, tx + 1, ty + 1);
                    // Label
                    Brush labelBr = n.Unlocked ? Brushes.White : Brushes.Gray;
                    g.DrawString(n.Name, f, labelBr, tx, ty);
                }
            }
        }

        private void DrawShip(Graphics g)
        {
            int sx = _current.Pos.X - 10, sy = _current.Pos.Y - 32;
            g.FillRectangle(Brushes.SaddleBrown, sx, sy + 10, 20, 8);
            g.FillRectangle(Brushes.Sienna, sx + 9, sy, 3, 18);
            g.FillPolygon(Brushes.Ivory, new[]
            { new Point(sx+10,sy+1), new Point(sx+19,sy+8), new Point(sx+10,sy+14) });
        }

        /// <summary>
        /// Draws a complete level progression checklist showing all 17 levels:
        /// - 11 Story Island levels (required for victory)
        /// - 6 Boss/Storm encounter levels (blocking progression gates)
        /// Shows checkmark (✓) for visited, bullet (•) for unvisited.
        /// Counter increments 0-17 based on completion.
        /// </summary>
        private void DrawIslandChecklist(Graphics g, int W, int H)
        {
            // All 17 levels in progression order
            string[] levelIds = { 
                "dino", "storm1", "sky", "blockade", "wano", "warlord1",
                "harbor", "coral", "tundra", "storm2", "warlord2",
                "dive_gate", "sunken_gate", "kelp", "boiling_vent", "abyss", "centipede_final"
            };

            string[] levelNames = { 
                "1. Dinosaur Island", 
                "2. Storm Belt", 
                "3. Sky Island",
                "4. Marine Blockade", 
                "5. Blade Nation", 
                "6. Warlord: Sudo",
                "7. Harbor Town", 
                "8. Coral Reef", 
                "9. Tundra Peak", 
                "10. Tempest Strait",
                "11. Warlord: Vanta",
                "12. Dive Gate", 
                "13. Sunken Gate", 
                "14. Kelp Maze",
                "15. Vent Ruins", 
                "16. Abyss", 
                "17. Centipede Boss"
            };

            // Story-critical islands (required for victory)
            bool[] isStoryCritical = {
                true, false, true, false, true, false,
                true, true, true, false, false,
                true, true, true, true, true, false
            };

            int panelX = W - 250;
            int panelY = 50;
            int panelW = 240;
            int itemsPerPanel = 17;  // Show all 17 levels in the checklist
            int panelH = 16 + (itemsPerPanel * 14) + 20;

            // ── MAIN PANEL: ALL 17 LEVELS (ALL REQUIRED FOR VICTORY) ──
            using (var br = new SolidBrush(Color.FromArgb(200, 20, 30, 60)))
                g.FillRectangle(br, panelX, panelY, panelW, panelH);
            using (var pen = new Pen(Color.FromArgb(160, Color.Gold), 2))
                g.DrawRectangle(pen, panelX, panelY, panelW, panelH);

            // ── Title with completion percentage ──
            int totalCompleted = 0;
            int storyCompleted = 0;
            for (int i = 0; i < levelIds.Length; i++)
            {
                var node = Find(levelIds[i]);
                if (node != null && node.Visited)
                {
                    totalCompleted++;
                    if (isStoryCritical[i]) storyCompleted++;
                }
            }

            // Victory when all levels in the levelIds array are completed
            int totalLevels = levelIds.Length;  // 17 levels total
            bool allStoriesComplete = totalCompleted == totalLevels;
            Color titleColor = allStoriesComplete ? Color.Gold : Color.LimeGreen;
            string titleText = $"VICTORY: {totalCompleted}/{totalLevels} Levels Complete";

            using (var f = new Font("Courier New", 9, FontStyle.Bold))
            using (var titleBrush = new SolidBrush(titleColor))
                g.DrawString(titleText, f, titleBrush, panelX + 6, panelY + 2);

            // ── Level list (ALL levels) ──
            int drawCount = 0;
            for (int i = 0; i < levelIds.Length && drawCount < itemsPerPanel; i++)
            {
                var node = Find(levelIds[i]);
                bool visited = node != null && node.Visited;

                int itemY = panelY + 18 + (drawCount * 14);

                using (var f = new Font("Courier New", 8, FontStyle.Bold))
                {
                    // Item number and status marker
                    string marker = visited ? "✓" : "•";
                    string itemText = $"{marker} {levelNames[i]}";

                    Color textColor = visited ? Color.White : Color.DarkGray;
                    using (var textBrush = new SolidBrush(textColor))
                        g.DrawString(itemText, f, textBrush, panelX + 8, itemY);
                }
                drawCount++;
            }

            // ── Victory indicator ──
            if (allStoriesComplete)
            {
                int victoryY = panelY + panelH + 2;
                using (var br = new SolidBrush(Color.FromArgb(100, 200, 160, 0)))
                    g.FillRectangle(br, panelX, victoryY, panelW, 16);
                using (var f = new Font("Courier New", 9, FontStyle.Bold))
                    g.DrawString("★ ALL LEVELS BEATEN! ★", f, Brushes.Gold, panelX + 6, victoryY + 1);
            }

            // ── SECONDARY PANEL: All Levels + Total Counter ──
            int panel2Y = panelY + panelH + 24;
            int panel2H = 100;
            using (var br = new SolidBrush(Color.FromArgb(180, 30, 30, 50)))
                g.FillRectangle(br, panelX, panel2Y, panelW, panel2H);
            using (var pen = new Pen(Color.FromArgb(140, 100, 150, 200), 1))
                g.DrawRectangle(pen, panelX, panel2Y, panelW, panel2H);

            // ── ALL LEVELS COUNTER ──
            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("ALL LEVELS", f, Brushes.LimeGreen, panelX + 8, panel2Y + 2);

            // Draw counter: total/17
            int counterX = panelX + 140;
            int counterY = panel2Y + 2;
            using (var br = new SolidBrush(Color.FromArgb(180, 40, 40, 80)))
                g.FillRectangle(br, counterX, counterY, 100, 24);
            using (var pen = new Pen(Color.FromArgb(120, 200, 100, 255), 2))
                g.DrawRectangle(pen, counterX, counterY, 100, 24);

            Color counterColor = totalCompleted == totalLevels ? Color.Gold : Color.Cyan;
            string counterText = $"{totalCompleted} / {totalLevels}";
            using (var f = new Font("Courier New", 12, FontStyle.Bold))
            using (var counterBrush = new SolidBrush(counterColor))
                g.DrawString(counterText, f, counterBrush, counterX + 18, counterY + 4);

            // ── Legend ──
            int legendY = panel2Y + 30;
            using (var f = new Font("Courier New", 8, FontStyle.Regular))
            {
                g.DrawString("✓ = Completed", f, Brushes.LimeGreen, panelX + 8, legendY);
                g.DrawString("• = Locked", f, Brushes.DarkGray, panelX + 8, legendY + 12);
                g.DrawString("Gold Border = Victory Unlocked", f, Brushes.Gold, panelX + 8, legendY + 24);
            }

            // ── Progress bar (visual representation) ──
            int barY = panel2Y + 68;
            int barW = panelW - 16;
            using (var br = new SolidBrush(Color.FromArgb(60, 60, 60)))
                g.FillRectangle(br, panelX + 8, barY, barW, 12);
            using (var br = new SolidBrush(totalCompleted == totalLevels ? Color.Gold : Color.Cyan))
                g.FillRectangle(br, panelX + 8, barY, (int)(barW * totalCompleted / (float)totalLevels), 12);
            using (var pen = new Pen(Color.FromArgb(120, 200, 200, 200)))
                g.DrawRectangle(pen, panelX + 8, barY, barW, 12);
        }

        private void DrawHUD(Graphics g, int W, int H)
        {
            using (var br = new SolidBrush(Color.FromArgb(190, 0, 0, 0)))
                g.FillRectangle(br, 0, H-50, W, 50);
            using (var f = new Font("Courier New", 13, FontStyle.Bold))
                g.DrawString(_status, f, Brushes.White, 10, H-38);
            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("[Click] Select node   [Enter/F] Travel   [Esc] Main Menu",
                             f, Brushes.LightGray, W-390, H-34);
            float threat = Game.Instance.ThreatLevel;
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString($"Marine Threat: {(int)threat}%", f, Brushes.OrangeRed, W-200, 10);
            g.FillRectangle(Brushes.DarkRed, W-200, 30, 175, 12);
            using (var br = new SolidBrush(Color.OrangeRed))
                g.FillRectangle(br, W-200, 30, (int)(175*threat/100f), 12);
        }

        /// <summary>
        /// Draws a procedural island landmass under each node on the map.
        /// </summary>
        private void DrawIslandLandmass(Graphics g, OverworldNode n)
        {
            int cx = n.Pos.X;
            int cy = n.Pos.Y;
            int seed = cx * 31 + cy * 17;
            var rng = new Random(seed);
            int baseR = 26 + rng.Next(12);
            int pts = 10;
            var shape = new PointF[pts];
            for (int i = 0; i < pts; i++)
            {
                double ang = 2 * Math.PI * i / pts;
                float r = baseR + rng.Next(-6, 7);
                shape[i] = new PointF(cx + (float)(Math.Cos(ang) * r * 1.5),
                                      cy + (float)(Math.Sin(ang) * r * 0.8));
            }

            if (n.Type == NodeType.Storm)
            {
                using (var br = new SolidBrush(Color.FromArgb(60, 80, 80, 120)))
                    g.FillPolygon(br, shape);
                return;
            }

            // Sandy base
            using (var br = new SolidBrush(Color.FromArgb(180, 194, 178, 120)))
                g.FillPolygon(br, shape);
            // Green vegetation (top half)
            var veg = new PointF[pts];
            for (int i = 0; i < pts; i++)
                veg[i] = new PointF(shape[i].X * 0.8f + cx * 0.2f,
                                    Math.Min(shape[i].Y, cy) * 0.8f + cy * 0.2f - 4);
            using (var br = new SolidBrush(Color.FromArgb(150, 50, 120, 40)))
                g.FillPolygon(br, veg);
            // Shore outline
            using (var pen = new Pen(Color.FromArgb(80, 200, 200, 160), 1))
                g.DrawPolygon(pen, shape);
        }

        private void DrawMainMenuButton(Graphics g)
        {
            _mainMenuBtn = new Rectangle(10, 10, 148, 34);
            using (var br = new SolidBrush(Color.FromArgb(190, 100, 20, 20)))
                g.FillRectangle(br, _mainMenuBtn);
            using (var pen = new Pen(Color.FromArgb(220, Color.Crimson), 1))
                g.DrawRectangle(pen, _mainMenuBtn);
            using (var f = new Font("Courier New", 10, FontStyle.Bold))
            {
                const string label = "\u2190  MAIN MENU";
                SizeF sz = g.MeasureString(label, f);
                g.DrawString(label, f, Brushes.White,
                    _mainMenuBtn.X + (_mainMenuBtn.Width  - sz.Width)  / 2f,
                    _mainMenuBtn.Y + (_mainMenuBtn.Height - sz.Height) / 2f);
            }

            _crewBtn = new Rectangle(168, 10, 110, 34);
            using (var br = new SolidBrush(Color.FromArgb(190, 20, 80, 130)))
                g.FillRectangle(br, _crewBtn);
            using (var pen = new Pen(Color.FromArgb(220, Color.SteelBlue), 1))
                g.DrawRectangle(pen, _crewBtn);
            using (var f = new Font("Courier New", 10, FontStyle.Bold))
            {
                const string label = "\u2605  CREW";
                SizeF sz = g.MeasureString(label, f);
                g.DrawString(label, f, Brushes.LightCyan,
                    _crewBtn.X + (_crewBtn.Width  - sz.Width)  / 2f,
                    _crewBtn.Y + (_crewBtn.Height - sz.Height) / 2f);
            }
        }
    }
}
