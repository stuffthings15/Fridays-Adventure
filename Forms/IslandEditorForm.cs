using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Fridays_Adventure.Data;

namespace Fridays_Adventure.Forms
{
    public sealed class IslandEditorForm : Form
    {
        private enum Tool { Select, Platform, WaterPit, SeaStone, Fire, Enemy, Boss }

        private IslandDefinition _island;
        private Tool _tool = Tool.Select;
        private object _selected;
        private Point _dragStart;
        private bool _dragging;
        private const float EditorScale = 0.25f;

        private Panel _canvas;
        private PropertyGrid _propGrid;
        private ComboBox _cboIsland;
        private Label _lblStatus;

        public IslandEditorForm()
        {
            Text = "Island Editor";
            ClientSize = new Size(1100, 680);
            MinimumSize = new Size(900, 600);
            BackColor = Color.FromArgb(30, 30, 40);
            ForeColor = Color.White;
            Font = new Font("Courier New", 9);
            BuildUI();
            LoadIsland("dino");
        }

        private void BuildUI()
        {
            var toolbar = new ToolStrip { GripStyle = ToolStripGripStyle.Hidden, BackColor = Color.FromArgb(20,20,30) };
            toolbar.Items.Add(MakeToolBtn("Select",   Tool.Select));
            toolbar.Items.Add(MakeToolBtn("Platform", Tool.Platform));
            toolbar.Items.Add(MakeToolBtn("Water",    Tool.WaterPit));
            toolbar.Items.Add(MakeToolBtn("SeaStone", Tool.SeaStone));
            toolbar.Items.Add(MakeToolBtn("Fire",     Tool.Fire));
            toolbar.Items.Add(MakeToolBtn("Enemy",    Tool.Enemy));
            toolbar.Items.Add(MakeToolBtn("Boss",     Tool.Boss));
            toolbar.Items.Add(new ToolStripSeparator());
            var btnSave = new ToolStripButton("Save") { BackColor = Color.DarkGreen, ForeColor = Color.White };
            btnSave.Click += (s,e) => SaveCurrent();
            toolbar.Items.Add(btnSave);

            _cboIsland = new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var id in ContentLoader.BuiltInIds()) _cboIsland.Items.Add(id);
            _cboIsland.SelectedIndexChanged += (s,e) => { if (_cboIsland.SelectedItem != null) LoadIsland(_cboIsland.SelectedItem.ToString()); };
            toolbar.Items.Add(new ToolStripControlHost(_cboIsland));

            Controls.Add(toolbar);

            _propGrid = new PropertyGrid
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = Color.FromArgb(25, 25, 35),
                LineColor  = Color.FromArgb(50, 50, 70),
                ViewForeColor = Color.White,
                ViewBackColor = Color.FromArgb(25, 25, 35),
            };
            Controls.Add(_propGrid);

            _canvas = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(40, 60, 100),
            };
            _canvas.Paint += (s,e) => DrawCanvas(e.Graphics);
            _canvas.MouseDown += OnCanvasMouseDown;
            _canvas.MouseMove += OnCanvasMouseMove;
            _canvas.MouseUp   += OnCanvasMouseUp;
            Controls.Add(_canvas);

            _lblStatus = new Label
            {
                Dock = DockStyle.Bottom, Height = 22,
                BackColor = Color.FromArgb(20,20,30),
                ForeColor = Color.LightGray,
                Text = "Click canvas to place items. Right-click to delete.",
            };
            Controls.Add(_lblStatus);
        }

        private ToolStripButton MakeToolBtn(string label, Tool tool)
        {
            var btn = new ToolStripButton(label) { BackColor = Color.FromArgb(40,40,55), ForeColor = Color.White, CheckOnClick = true };
            btn.Click += (s,e) => { _tool = tool; btn.Checked = true; };
            return btn;
        }

        private void LoadIsland(string id)
        {
            _island = ContentLoader.LoadIsland(id);
            _selected = null;
            _propGrid.SelectedObject = null;
            _lblStatus.Text = "Loaded: " + _island.Name;
            _canvas.Invalidate();
        }

        private void SaveCurrent()
        {
            if (_island == null) return;
            ContentLoader.SaveIsland(_island);
            _lblStatus.Text = "Saved: " + _island.Id;
        }

        private Point IslandToCanvas(float ix, float iy)
            => new Point((int)(ix * EditorScale), (int)(iy * EditorScale));

        private PointF CanvasToIsland(int cx, int cy)
            => new PointF(cx / EditorScale, cy / EditorScale);

        private void DrawCanvas(Graphics g)
        {
            if (_island == null) return;
            g.SmoothingMode = SmoothingMode.None;

            using (var br = new LinearGradientBrush(new Rectangle(0,0,_canvas.Width,_canvas.Height),Color.FromArgb(60,90,140),Color.FromArgb(140,110,70),90f))
                g.FillRectangle(br, 0, 0, _canvas.Width, _canvas.Height);

            // Platforms
            foreach (var p in _island.Platforms)
            {
                var r = ScaleRect(p.X, p.Y, p.W, p.H);
                using (var br = new SolidBrush(Color.FromArgb(160, 100, 60)))
                    g.FillRectangle(br, r);
                g.DrawRectangle(_selected == (object)p ? Pens.Yellow : Pens.White, r);
            }

            // Hazards
            foreach (var h in _island.Hazards)
            {
                var r = ScaleRect(h.X, h.Y, h.W, h.H);
                Color c;
                switch (h.HazardType)
                {
                    case "WaterPit":      c = Color.FromArgb(120, Color.Blue);    break;
                    case "SeaStoneZone":  c = Color.FromArgb(120, Color.Olive);   break;
                    case "FireSource":    c = Color.FromArgb(120, Color.OrangeRed); break;
                    default:              c = Color.FromArgb(120, Color.Gray);     break;
                }
                using (var br = new SolidBrush(c))
                    g.FillRectangle(br, r);
                g.DrawRectangle(_selected == (object)h ? Pens.Yellow : Pens.White, r);
            }

            // Enemies
            foreach (var e in _island.Enemies)
            {
                var pt = IslandToCanvas(e.X, e.Y);
                Color c = e.IsBoss ? Color.Crimson : Color.Red;
                using (var br = new SolidBrush(c))
                    g.FillEllipse(br, pt.X - 6, pt.Y - 6, 12, 12);
                if (_selected == (object)e)
                    g.DrawEllipse(Pens.Yellow, pt.X - 8, pt.Y - 8, 16, 16);
            }
        }

        private Rectangle ScaleRect(int x, int y, int w, int h)
            => new Rectangle((int)(x * EditorScale), (int)(y * EditorScale), (int)(w * EditorScale), (int)(h * EditorScale));

        private void OnCanvasMouseDown(object sender, MouseEventArgs e)
        {
            if (_island == null) return;
            _dragStart = e.Location;
            _dragging  = true;
        }

        private void OnCanvasMouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
        }

        private void OnCanvasMouseUp(object sender, MouseEventArgs e)
        {
            _dragging = false;
            _canvas.Invalidate();
        }
    }
}
