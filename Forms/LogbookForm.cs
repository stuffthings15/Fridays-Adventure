using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Fridays_Adventure.Data;

namespace Fridays_Adventure.Forms
{
    public sealed class LogbookForm : Form
    {
        private LogbookData _data;

        // Water tab controls
        private Label       _lblTotal;
        private ProgressBar _waterBar;
        private ListBox     _lstLog;
        private NumericUpDown _nudGoal;

        // Cargo tab controls
        private CheckedListBox _clbCargo;
        private TextBox        _txtNewCargo;

        // Notes
        private RichTextBox _rtfNotes;

        // Drawing
        private readonly Color _ocean  = Color.FromArgb(10,  30,  80);
        private readonly Color _panel  = Color.FromArgb(20,  50, 100);
        private readonly Color _accent = Color.FromArgb(0,  180, 200);

        public LogbookForm()
        {
            _data = LogbookData.Load();
            BuildUI();
        }

        private void BuildUI()
        {
            Text          = "🐍 Sea Serpent Logbook";
            ClientSize    = new Size(560, 520);
            BackColor     = _ocean;
            ForeColor     = Color.White;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox   = false;
            StartPosition = FormStartPosition.CenterScreen;
            Font          = new Font("Courier New", 9);

            // Header
            var header = new Label
            {
                Text      = "SEA SERPENT  LOGBOOK",
                Font      = new Font("Courier New", 14, FontStyle.Bold),
                ForeColor = _accent,
                AutoSize  = false,
                Size      = new Size(560, 36),
                Location  = new Point(0, 8),
                TextAlign = ContentAlignment.MiddleCenter
            };
            Controls.Add(header);

            var tabs = new TabControl
            {
                Location  = new Point(8, 52),
                Size      = new Size(540, 400),
                Appearance = TabAppearance.FlatButtons
            };
            tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabs.DrawItem += DrawTab;
            Controls.Add(tabs);

            tabs.TabPages.Add(BuildWaterTab());
            tabs.TabPages.Add(BuildCargoTab());
            tabs.TabPages.Add(BuildNotesTab());

            // Save button
            var btnSave = MakeButton("💾 Save & Close", new Point(200, 462), 160);
            btnSave.Click += (s, e) => { Save(); Close(); };
            Controls.Add(btnSave);

            FormClosing += (s, e) => Save();
        }

        private void DrawTab(object sender, DrawItemEventArgs e)
        {
            var tc = (TabControl)sender;
            bool sel = e.Index == tc.SelectedIndex;
            using (var br = new SolidBrush(sel ? _accent : _panel))
                e.Graphics.FillRectangle(br, e.Bounds);
            using (var f = new Font("Courier New", 9, sel ? FontStyle.Bold : FontStyle.Regular))
                e.Graphics.DrawString(tc.TabPages[e.Index].Text, f,
                                      sel ? Brushes.Black : Brushes.LightGray,
                                      e.Bounds.X + 8, e.Bounds.Y + 4);
        }

        private TabPage BuildWaterTab()
        {
            var page = new TabPage("💧 Water") { BackColor = _ocean, ForeColor = Color.White };

            page.Controls.Add(MakeLabel("Daily Goal (ml):", new Point(10, 12)));
            _nudGoal = new NumericUpDown
            {
                Location = new Point(140, 10), Size = new Size(80, 22),
                Minimum = 500, Maximum = 5000, Increment = 250,
                Value = _data.DailyGoalMl,
                BackColor = _panel, ForeColor = Color.White
            };
            _nudGoal.ValueChanged += (s, e) => { _data.DailyGoalMl = (int)_nudGoal.Value; RefreshWater(); };
            page.Controls.Add(_nudGoal);

            _lblTotal = new Label
            {
                Location = new Point(10, 42), Size = new Size(510, 22),
                Font = new Font("Courier New", 10, FontStyle.Bold), ForeColor = _accent
            };
            page.Controls.Add(_lblTotal);

            _waterBar = new ProgressBar
            {
                Location = new Point(10, 68), Size = new Size(510, 18),
                Minimum = 0, Maximum = 100, Style = ProgressBarStyle.Continuous
            };
            page.Controls.Add(_waterBar);

            // Add buttons
            int[] amounts = { 150, 250, 500, 750 };
            for (int i = 0; i < amounts.Length; i++)
            {
                int ml = amounts[i];
                var btn = MakeButton($"+{ml}ml", new Point(10 + i * 125, 96), 115);
                btn.Click += (s, e) => { _data.AddWater(ml); RefreshWater(); };
                page.Controls.Add(btn);
            }

            var btnClear = MakeButton("✖ Clear today", new Point(10, 130), 130);
            btnClear.Click += (s, e) => { _data.ClearToday(); RefreshWater(); };
            page.Controls.Add(btnClear);

            page.Controls.Add(MakeLabel("Today's log:", new Point(10, 162)));
            _lstLog = new ListBox
            {
                Location = new Point(10, 182), Size = new Size(510, 165),
                BackColor = _panel, ForeColor = Color.LightCyan, BorderStyle = BorderStyle.FixedSingle
            };
            page.Controls.Add(_lstLog);

            RefreshWater();
            return page;
        }

        private TabPage BuildCargoTab()
        {
            var page = new TabPage("📦 Cargo") { BackColor = _ocean, ForeColor = Color.White };

            page.Controls.Add(MakeLabel("Ship Supplies:", new Point(10, 10)));

            _clbCargo = new CheckedListBox
            {
                Location = new Point(10, 34), Size = new Size(510, 270),
                BackColor = _panel, ForeColor = Color.White,
                CheckOnClick = true, BorderStyle = BorderStyle.FixedSingle
            };
            for (int i = 0; i < _data.CargoItems.Count; i++)
            {
                _clbCargo.Items.Add(_data.CargoItems[i], _data.CargoDone[i]);
            }
            _clbCargo.ItemCheck += (s, e) =>
            {
                if (e.Index < _data.CargoDone.Count)
                    _data.CargoDone[e.Index] = e.NewValue == CheckState.Checked;
            };
            page.Controls.Add(_clbCargo);

            _txtNewCargo = new TextBox
            {
                Location = new Point(10, 314), Size = new Size(380, 22),
                BackColor = _panel, ForeColor = Color.Gray, BorderStyle = BorderStyle.FixedSingle,
                Text = "Add new cargo item..."
            };
            _txtNewCargo.GotFocus  += (s, e) => { if (_txtNewCargo.ForeColor == Color.Gray) { _txtNewCargo.Text = ""; _txtNewCargo.ForeColor = Color.White; } };
            _txtNewCargo.LostFocus += (s, e) => { if (string.IsNullOrWhiteSpace(_txtNewCargo.Text)) { _txtNewCargo.Text = "Add new cargo item..."; _txtNewCargo.ForeColor = Color.Gray; } };
            page.Controls.Add(_txtNewCargo);

            var btnAdd = MakeButton("+ Add", new Point(400, 312), 80);
            btnAdd.Click += (s, e) =>
            {
                string item = _txtNewCargo.Text.Trim();
                if (string.IsNullOrEmpty(item)) return;
                _data.AddCargoItem(item);
                _clbCargo.Items.Add(item, false);
                _txtNewCargo.Clear();
            };
            page.Controls.Add(btnAdd);

            return page;
        }

        private TabPage BuildNotesTab()
        {
            var page = new TabPage("📝 Notes") { BackColor = _ocean, ForeColor = Color.White };
            page.Controls.Add(MakeLabel("Ship's log & weather notes:", new Point(10, 10)));

            _rtfNotes = new RichTextBox
            {
                Location = new Point(10, 34), Size = new Size(510, 310),
                BackColor = _panel, ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Text = _data.Notes
            };
            page.Controls.Add(_rtfNotes);

            // Tips panel
            var tips = new Label
            {
                Location = new Point(10, 352), Size = new Size(510, 50),
                Text = "☀ Heat tip: Drink 500ml extra for every hour in direct sun.\n"
                     + "🌊 Grand Line rule: Always know where the water is before you need it.",
                ForeColor = Color.LightBlue, Font = new Font("Courier New", 8)
            };
            page.Controls.Add(tips);
            return page;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        private void RefreshWater()
        {
            _lblTotal.Text = $"Today: {_data.TodayTotalMl} ml  /  {_data.DailyGoalMl} ml  " +
                             $"({(int)(_data.GoalPercent * 100)}% of daily goal)";
            _waterBar.Value = (int)(_data.GoalPercent * 100);
            _lstLog.Items.Clear();
            foreach (var e in _data.TodayLog)
                _lstLog.Items.Insert(0, $"  {e.Time:HH:mm}  +{e.Amount} ml");
        }

        private void Save()
        {
            if (_rtfNotes != null) _data.Notes = _rtfNotes.Text;
            for (int i = 0; i < _clbCargo?.Items.Count; i++)
                if (i < _data.CargoDone.Count)
                    _data.CargoDone[i] = _clbCargo.GetItemChecked(i);
            _data.Save();
        }

        private Label MakeLabel(string text, Point loc)
            => new Label { Text = text, Location = loc, AutoSize = true, ForeColor = Color.LightGray };

        private Button MakeButton(string text, Point loc, int width)
            => new Button
            {
                Text = text, Location = loc, Size = new Size(width, 26),
                BackColor = _panel, ForeColor = _accent,
                FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
            };
    }
}
