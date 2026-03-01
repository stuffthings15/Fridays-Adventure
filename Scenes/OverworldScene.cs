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

        public override void OnEnter()
        {
            BuildNodes();
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                                       "Assets", "Sprites", "bg_overworld.png");
            if (File.Exists(path)) _bg = new Bitmap(path);
            Game.Instance.Audio.PlayOverworld();
        }

        public override void OnExit()   { _bg?.Dispose(); _bg = null; }
        public override void OnResume() => Game.Instance.Audio.PlayOverworld();

        private void BuildNodes()
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            _nodes = new List<OverworldNode>
            {
                new OverworldNode("start",   "Sea Serpent",    new Point((int)(W*0.10f), H/2),            NodeType.Start,  true),
                new OverworldNode("dino",    "Dinosaur Island",new Point((int)(W*0.28f), (int)(H*0.42f)), NodeType.Island, true),
                new OverworldNode("storm1",  "Storm Belt",     new Point((int)(W*0.46f), (int)(H*0.55f)), NodeType.Storm,  false),
                new OverworldNode("sky",     "Sky Island",     new Point((int)(W*0.62f), (int)(H*0.30f)), NodeType.Island, false),
                new OverworldNode("wano",    "Blade Nation",   new Point((int)(W*0.82f), (int)(H*0.48f)), NodeType.Island, false),
                new OverworldNode("blockade","Marine Blockade",new Point((int)(W*0.55f), (int)(H*0.68f)), NodeType.Boss,   false),
                new OverworldNode("warlord1","Warlord: Lord Sudo", new Point((int)(W*0.92f),(int)(H*0.30f)),NodeType.Boss, false),
            };
            Link("start","dino"); Link("dino","storm1"); Link("storm1","sky");
            Link("storm1","blockade"); Link("sky","wano"); Link("blockade","wano");
            Link("wano","warlord1");
            _current = Find("start");
            _current.Visited = true;
        }

        private void Link(string a, string b)
        { Find(a)?.Links.Add(b); Find(b)?.Links.Add(a); }

        private OverworldNode Find(string id)
        { foreach (var n in _nodes) if (n.Id == id) return n; return null; }

        public override void HandleClick(Point p)
        {
            if (_mainMenuBtn.Contains(p))
            {
                Game.Instance.Scenes.Replace(new TitleScene());
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
            if (Game.Instance.Input.PausePressed)
                Game.Instance.Scenes.Replace(new TitleScene());
            if (Game.Instance.Input.InteractPressed && _selected != null && _selected.Unlocked)
                Travel(_selected);
        }

        private void Travel(OverworldNode node)
        {
            _current        = node;
            node.Visited    = true;
            _selected       = null;
            Game.Instance.Save.CurrentNodeId = node.Id;
            Systems.ThreatSystem.OnNodeTraversed();
            foreach (var id in node.Links) Find(id).Unlocked = true;

            if (node.Id == "storm1")
            {
                Game.Instance.Scenes.Push(new StormScene());
            }
            else if (node.Id == "warlord1")
            {
                TriggerDialogueThen(Dialogues.MarineEncounter(), () =>
                    Game.Instance.Scenes.Push(new WarlordBossScene(WarlordConfig.FireLordSudo())));
            }
            else if (node.Type == NodeType.Boss)
            {
                TriggerDialogueThen(Dialogues.MarineEncounter(), () =>
                    Game.Instance.Scenes.Push(new BossScene()));
            }
            else if (node.Id == "sky")
            {
                bool firstVisit = !Game.Instance.Save.GetFlag("sky_visited");
                Game.Instance.Save.SetFlag("sky_visited");
                if (firstVisit)
                    TriggerDialogueThen(Dialogues.MeetAmelia(), () =>
                        Game.Instance.Scenes.Push(new SkyIslandScene()));
                else
                    Game.Instance.Scenes.Push(new SkyIslandScene());
            }
            else if (node.Id == "wano")
            {
                bool firstVisit = !Game.Instance.Save.GetFlag("wano_visited");
                Game.Instance.Save.SetFlag("wano_visited");
                if (firstVisit)
                    TriggerDialogueThen(Dialogues.BladeSamuriGate(), () =>
                        Game.Instance.Scenes.Push(new IslandScene(node.Id, node.Name)));
                else
                    Game.Instance.Scenes.Push(new IslandScene(node.Id, node.Name));
            }
            else if (node.Id == "dino")
            {
                bool firstVisit = !Game.Instance.Save.GetFlag("dino_visited");
                Game.Instance.Save.SetFlag("dino_visited");
                if (firstVisit)
                    TriggerDialogueThen(Dialogues.MeetFinn(), () =>
                        Game.Instance.Scenes.Push(new IslandScene(node.Id, node.Name)));
                else
                    Game.Instance.Scenes.Push(new IslandScene(node.Id, node.Name));
            }
            else if (node.Type == NodeType.Island)
                Game.Instance.Scenes.Push(new IslandScene(node.Id, node.Name));
        }

        private void TriggerDialogueThen(DialogueSequence seq, Action then)
        {
            seq.OnDone = _ => then?.Invoke();
            Game.Instance.Scenes.Push(new DialogueScene(seq));
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            DrawOcean(g, W, H);
            DrawLinks(g);
            DrawNodes(g);
            DrawShip(g);
            DrawHUD(g, W, H);
            DrawMainMenuButton(g);
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
            var seen = new HashSet<string>();
            foreach (var n in _nodes)
                foreach (var id in n.Links)
                {
                    string key = string.Compare(n.Id, id, StringComparison.Ordinal) < 0
                               ? n.Id + "|" + id : id + "|" + n.Id;
                    if (!seen.Add(key)) continue;
                    var other = Find(id);
                    if (other == null) continue;
                    bool both = n.Unlocked && other.Unlocked;
                    using (var pen = new Pen(both
                        ? Color.FromArgb(100, Color.Cyan) : Color.FromArgb(40, Color.Gray), 2)
                        { DashStyle = both ? DashStyle.Dash : DashStyle.Dot })
                        g.DrawLine(pen, n.Pos, other.Pos);
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

                Color fill;
                switch (n.Type)
                {
                    case NodeType.Island: fill = Color.FromArgb(n.Unlocked?200:70, Color.ForestGreen); break;
                    case NodeType.Storm:  fill = Color.FromArgb(n.Unlocked?200:70, Color.SlateBlue);   break;
                    case NodeType.Boss:   fill = Color.FromArgb(n.Unlocked?200:70, Color.Crimson);     break;
                    default:              fill = Color.FromArgb(n.Unlocked?200:70, Color.Gold);        break;
                }
                using (var br = new SolidBrush(fill))
                    g.FillEllipse(br, n.Pos.X-14, n.Pos.Y-14, 28, 28);
                using (var pen = new Pen(n.Visited ? Color.White : Color.DimGray, 1))
                    g.DrawEllipse(pen, n.Pos.X-14, n.Pos.Y-14, 28, 28);
                using (var f = new Font("Courier New", 12, FontStyle.Bold))
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

        private void DrawShip(Graphics g)
        {
            int sx = _current.Pos.X - 10, sy = _current.Pos.Y - 32;
            g.FillRectangle(Brushes.SaddleBrown, sx, sy + 10, 20, 8);
            g.FillRectangle(Brushes.Sienna, sx + 9, sy, 3, 18);
            g.FillPolygon(Brushes.Ivory, new[]
            { new Point(sx+10,sy+1), new Point(sx+19,sy+8), new Point(sx+10,sy+14) });
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
    }
}
