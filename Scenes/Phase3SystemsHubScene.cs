// ────────────────────────────────────────────────────────────────────────────
// PHASE 3 - Team 9: UI Programmer
// Feature: Systems Hub UI (Mod Manager / DLC Browser / Profile / Season Pass)
// Purpose: Provide Wave 1 UI surfaces for Team 8 + Team 11 foundation systems.
// ────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Fridays_Adventure.Engine;
using Fridays_Adventure.Systems;

namespace Fridays_Adventure.Scenes
{
    /// <summary>
    /// Unified Phase 3 systems UI hub for mod, DLC, profile, and build operations.
    /// </summary>
    public sealed class Phase3SystemsHubScene : Scene
    {
        private readonly string[] _tabs =
        {
            "Mod Manager",
            "DLC Browser",
            "Profile",
            "Season Pass",
            "Build Ops",
            "Customization",
            "Cosmetics Shop",
            "Leaderboard",
            "Custom Setup",
            "Streaming"
        };
        private int _tab;

        private List<ModMetadata> _mods = new List<ModMetadata>();
        private IReadOnlyList<string> _dlcs = Array.Empty<string>();
        private IReadOnlyList<string> _languages = Array.Empty<string>();
        private PlayerProfile _profile;
        private string _buildOpsSummary = "Press G to generate checklist.\nPress A to analyze build size.";
        private string _leaderboardSummary = string.Empty;

        public override void OnEnter()
        {
            DataMigrationTool.MigrateLegacyProfileIfNeeded();
            RefreshData();
        }

        public override void OnExit() { }

        public override void Update(float dt)
        {
            var input = Game.Instance.Input;
            if (input.IsPressed(System.Windows.Forms.Keys.Left) && _tab > 0) _tab--;
            if (input.IsPressed(System.Windows.Forms.Keys.Right) && _tab < _tabs.Length - 1) _tab++;

            switch (_tab)
            {
                case 0:
                    HandleModManagerInput(input);
                    break;
                case 2:
                    HandleProfileInput(input);
                    break;
                case 3:
                    HandleSeasonPassInput(input);
                    break;
                case 4:
                    HandleBuildOpsInput(input);
                    break;
                case 5:
                    HandleCustomizationInput(input);
                    break;
                case 6:
                    HandleCosmeticsShopInput(input);
                    break;
                case 8:
                    HandleCustomSetupInput(input);
                    break;
                case 9:
                    HandleStreamingInput(input);
                    break;
            }

            if (input.PausePressed || input.InteractPressed)
                Game.Instance.Scenes.Pop();
        }

        public override void Draw(Graphics g)
        {
            int W = Game.Instance.CanvasWidth;
            int H = Game.Instance.CanvasHeight;
            using (var br = new SolidBrush(Color.FromArgb(16, 18, 28)))
                g.FillRectangle(br, 0, 0, W, H);

            using (var f = new Font("Courier New", 20, FontStyle.Bold))
                g.DrawString("PHASE 3 SYSTEMS HUB", f, Brushes.Gold, 14, 10);

            DrawTabs(g, W);

            var body = new Rectangle(14, 90, W - 28, H - 126);
            using (var br = new SolidBrush(Color.FromArgb(24, 24, 36)))
                g.FillRectangle(br, body);
            g.DrawRectangle(Pens.DimGray, body);

            switch (_tab)
            {
                case 0: DrawModManager(g, body); break;
                case 1: DrawDlcBrowser(g, body); break;
                case 2: DrawProfile(g, body); break;
                case 3: DrawSeasonPass(g, body); break;
                case 4: DrawBuildOps(g, body); break;
                case 5: DrawCustomization(g, body); break;
                case 6: DrawCosmeticsShop(g, body); break;
                case 7: DrawLeaderboard(g, body); break;
                case 8: DrawCustomSetup(g, body); break;
                default: DrawStreaming(g, body); break;
            }

            using (var f = new Font("Courier New", 10, FontStyle.Bold))
                g.DrawString("Left/Right: Tab   Esc/Enter: Back   X/B/L/C/T keys for tab actions", f, Brushes.DimGray, 14, H - 26);
        }

        private void DrawTabs(Graphics g, int W)
        {
            int x = 14;
            for (int i = 0; i < _tabs.Length; i++)
            {
                bool sel = i == _tab;
                int w = 140;
                if (x + w > W - 20) break;
                var r = new Rectangle(x, 52, w, 28);
                using (var br = new SolidBrush(sel ? Color.FromArgb(70, 130, 220) : Color.FromArgb(40, 40, 55)))
                    g.FillRectangle(br, r);
                g.DrawRectangle(sel ? Pens.Cyan : Pens.Gray, r);
                using (var f = new Font("Courier New", 9, FontStyle.Bold))
                    g.DrawString(_tabs[i], f, sel ? Brushes.Cyan : Brushes.LightGray, x + 8, 60);
                x += w + 8;
            }
        }

        private void DrawModManager(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Mod Manager UI", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
                g.DrawString("Press [1..9] to toggle listed mods.", f, Brushes.Gold, body.X + 10, body.Y + 30);

            int y = body.Y + 56;
            int idx = 1;
            using (var f = new Font("Courier New", 10))
                foreach (var m in _mods.Take(9))
                {
                    Brush b = m.Enabled ? Brushes.LimeGreen : Brushes.LightGray;
                    g.DrawString($"[{idx}] {(m.Enabled ? "ON " : "OFF")}  {m.Name}  v{m.Version}  by {m.Author}",
                        f, b, body.X + 12, y);
                    y += 20;
                    g.DrawString($"      id={m.Id}  tags={m.Tags}", f, Brushes.DimGray, body.X + 12, y);
                    y += 18;
                    idx++;
                }
        }

        private void DrawDlcBrowser(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("DLC Content Browser", f, Brushes.Cyan, body.X + 10, body.Y + 8);

            int y = body.Y + 38;
            using (var f = new Font("Courier New", 10))
                foreach (string dlc in _dlcs)
                {
                    g.DrawString($"• {dlc}", f, Brushes.Gold, body.X + 12, y);
                    y += 22;
                }
            if (_dlcs.Count == 0)
                using (var f = new Font("Courier New", 10))
                    g.DrawString("No DLC packages detected.", f, Brushes.DimGray, body.X + 12, y);
        }

        private void DrawProfile(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Profile Screen", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
                g.DrawString("Press X to add +250 XP and auto-save profile.", f, Brushes.Gold, body.X + 10, body.Y + 30);

            int y = body.Y + 62;
            using (var f = new Font("Courier New", 11))
            {
                g.DrawString($"Display Name : {_profile.DisplayName}", f, Brushes.LightGray, body.X + 12, y); y += 24;
                g.DrawString($"Total XP     : {_profile.TotalXp:N0}", f, Brushes.LightGray, body.X + 12, y); y += 24;
                g.DrawString($"Cosmetics    : {_profile.CosmeticCount}", f, Brushes.LightGray, body.X + 12, y); y += 24;
                g.DrawString($"Season Tier  : {_profile.SeasonTier}", f, Brushes.LightGray, body.X + 12, y);
            }
        }

        private void DrawSeasonPass(Graphics g, Rectangle body)
        {
            int tier = SeasonPassManager.CalculateTier(_profile.TotalXp);
            int toNext = SeasonPassManager.XpToNextTier(_profile.TotalXp);

            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Season Pass UI", f, Brushes.Cyan, body.X + 10, body.Y + 8);

            using (var f = new Font("Courier New", 11))
            {
                g.DrawString($"Current Tier : {tier}", f, Brushes.Gold, body.X + 12, body.Y + 44);
                g.DrawString($"XP To Next   : {toNext:N0}", f, Brushes.LightGray, body.X + 12, body.Y + 70);
            }

            int barX = body.X + 12, barY = body.Y + 106, barW = body.Width - 24, barH = 14;
            int xpInTier = _profile.TotalXp % 500;
            float fill = xpInTier / 500f;
            using (var br = new SolidBrush(Color.FromArgb(60, 60, 60))) g.FillRectangle(br, barX, barY, barW, barH);
            using (var br = new SolidBrush(Color.FromArgb(80, 180, 255))) g.FillRectangle(br, barX, barY, (int)(barW * fill), barH);
            g.DrawRectangle(Pens.DimGray, barX, barY, barW, barH);
        }

        private void DrawBuildOps(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Build Ops", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
                g.DrawString("A:size G:checklist C:ci V:variants L:loc M:mods R:crash D:deploy P:perf-save O:perf-compare Z:compress",
                    f, Brushes.Gold, body.X + 10, body.Y + 30);

            using (var f = new Font("Courier New", 9))
                g.DrawString(_buildOpsSummary, f, Brushes.LightGray, body.X + 12, body.Y + 58);
        }

        private void HandleModManagerInput(InputManager input)
        {
            for (int i = 0; i < Math.Min(9, _mods.Count); i++)
            {
                var key = System.Windows.Forms.Keys.D1 + i;
                if (!input.IsPressed(key)) continue;
                var m = _mods[i];
                ModMetadataSystem.SetEnabled(m.Id, !m.Enabled);
                RefreshData();
                SMB3Hud.ShowToast($"Mod '{m.Name}' set to {(m.Enabled ? "OFF" : "ON")}");
                break;
            }
        }

        private void HandleProfileInput(InputManager input)
        {
            if (!input.IsPressed(System.Windows.Forms.Keys.X)) return;
            _profile = PlayerProfileSystem.AddXp(250);
            SMB3Hud.ShowToast("+250 profile XP");
        }

        private void HandleSeasonPassInput(InputManager input)
        {
            if (!input.IsPressed(System.Windows.Forms.Keys.X)) return;
            _profile = PlayerProfileSystem.AddXp(100);
            SMB3Hud.ShowToast("+100 season XP");
        }

        private void HandleBuildOpsInput(InputManager input)
        {
            if (input.IsPressed(System.Windows.Forms.Keys.A))
            {
                var report = BuildSizeAnalyzer.AnalyzeReleaseFolder();
                _buildOpsSummary = BuildSizeAnalyzer.Format(report);
                return;
            }
            if (input.IsPressed(System.Windows.Forms.Keys.G))
            {
                string path = ReleaseChecklistGenerator.Generate();
                _buildOpsSummary = "Checklist generated:\n" + path;
                return;
            }
            if (input.IsPressed(System.Windows.Forms.Keys.C))
            {
                string path = CiCdExpanded.GenerateCiSummary();
                _buildOpsSummary = "CI summary generated:\n" + path;
                return;
            }
            if (input.IsPressed(System.Windows.Forms.Keys.V))
            {
                string dir = BuildVariantSystem.GenerateVariants();
                _buildOpsSummary = "Variants generated:\n" + dir;
                return;
            }
            if (input.IsPressed(System.Windows.Forms.Keys.L))
            {
                _buildOpsSummary = string.Join("\n", LocalizationBuildChecker.Validate());
                return;
            }
            if (input.IsPressed(System.Windows.Forms.Keys.M))
            {
                _buildOpsSummary = string.Join("\n", ModValidationTool.Validate());
                return;
            }
            if (input.IsPressed(System.Windows.Forms.Keys.R))
            {
                _buildOpsSummary = string.Join("\n", CrashAnalytics.Summarize());
                return;
            }
            if (input.IsPressed(System.Windows.Forms.Keys.D))
            {
                string path = DeploymentAutomation.Run();
                _buildOpsSummary = string.IsNullOrWhiteSpace(path) ? "Deployment failed (no release output)." : "Deployment created:\n" + path;
                return;
            }
            if (input.IsPressed(System.Windows.Forms.Keys.P))
            {
                string snap = string.Join("\n", PerformanceOptimizationSuite.Snapshot());
                PerformanceRegressionTesting.SaveBaseline(snap);
                _buildOpsSummary = "Performance baseline saved.";
                return;
            }
            if (input.IsPressed(System.Windows.Forms.Keys.O))
            {
                string snap = string.Join("\n", PerformanceOptimizationSuite.Snapshot());
                _buildOpsSummary = PerformanceRegressionTesting.Compare(snap);
                return;
            }
            if (input.IsPressed(System.Windows.Forms.Keys.Z))
            {
                _buildOpsSummary = string.Join("\n", AssetCompressionTool.Estimate());
            }
        }

        private void DrawCustomization(Graphics g, Rectangle body)
        {
            var owned = CosmeticInventorySystem.GetOwned().OrderBy(x => x).ToList();
            string equipped = CosmeticInventorySystem.GetEquipped();

            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString(LanguagePackManager.T("ui.customization", "Character Customization"),
                    f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
                g.DrawString("Press [1..9] to equip owned cosmetics. Press L to cycle language.",
                    f, Brushes.Gold, body.X + 10, body.Y + 30);

            int y = body.Y + 60;
            using (var f = new Font("Courier New", 10))
            {
                int i = 1;
                foreach (string id in owned.Take(9))
                {
                    bool isEq = id.Equals(equipped, StringComparison.OrdinalIgnoreCase);
                    g.DrawString($"[{i}] {(isEq ? "EQUIPPED" : "       ")}  {id}",
                        f, isEq ? Brushes.LimeGreen : Brushes.LightGray, body.X + 12, y);
                    y += 22;
                    i++;
                }

                string lang = LanguagePackManager.CurrentLanguage;
                g.DrawString($"Language pack: {lang}", f, Brushes.Cyan, body.X + 12, body.Bottom - 28);
            }
        }

        private void DrawCosmeticsShop(Graphics g, Rectangle body)
        {
            var catalog = WorkshopIntegration.GetCatalog().Take(8).ToList();
            var owned = CosmeticInventorySystem.GetOwned();

            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString(LanguagePackManager.T("ui.shop", "Cosmetics Shop"), f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
                g.DrawString("Press [1..8] to buy/install item (cost 500 bounty each).", f, Brushes.Gold, body.X + 10, body.Y + 30);

            int y = body.Y + 60;
            using (var f = new Font("Courier New", 10))
            {
                for (int i = 0; i < catalog.Count; i++)
                {
                    var item = catalog[i];
                    bool have = owned.Contains(item.Id);
                    g.DrawString($"[{i + 1}] {(have ? "OWNED" : "$500 ")}  {item.Title} ({item.Id})",
                        f, have ? Brushes.LimeGreen : Brushes.LightGray, body.X + 12, y);
                    y += 22;
                }
            }
        }

        private void DrawLeaderboard(Graphics g, Rectangle body)
        {
            var rows = Phase3ProducerSystems.GetLeaderboard().Take(12).ToList();
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString(LanguagePackManager.T("ui.leaderboard", "Leaderboard Display"), f, Brushes.Cyan, body.X + 10, body.Y + 8);

            int y = body.Y + 40;
            using (var f = new Font("Courier New", 10))
            {
                int rank = 1;
                foreach (var r in rows)
                {
                    g.DrawString($"#{rank,2}  {r.Player,-14}  {r.Score,8:N0}",
                        f, rank <= 3 ? Brushes.Gold : Brushes.LightGray, body.X + 12, y);
                    y += 20;
                    rank++;
                }
                if (rows.Count == 0)
                    g.DrawString("No leaderboard entries yet.", f, Brushes.DimGray, body.X + 12, y);
            }
        }

        private void DrawCustomSetup(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString("Custom Game Setup", f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
                g.DrawString("J/K lives   H hard enemies   Y skip tutorials", f, Brushes.Gold, body.X + 10, body.Y + 30);

            int y = body.Y + 62;
            using (var f = new Font("Courier New", 11))
            {
                g.DrawString($"Starting Lives : {CustomGameSetupState.StartingLives}", f, Brushes.LightGray, body.X + 12, y); y += 24;
                g.DrawString($"Hard Enemies   : {CustomGameSetupState.HardEnemies}", f, Brushes.LightGray, body.X + 12, y); y += 24;
                g.DrawString($"Skip Tutorials : {CustomGameSetupState.SkipTutorials}", f, Brushes.LightGray, body.X + 12, y);
            }
        }

        private void DrawStreaming(Graphics g, Rectangle body)
        {
            using (var f = new Font("Courier New", 11, FontStyle.Bold))
                g.DrawString(LanguagePackManager.T("ui.streaming", "Streaming Mode"), f, Brushes.Cyan, body.X + 10, body.Y + 8);
            using (var f = new Font("Courier New", 10))
                g.DrawString("Press T to toggle stream-safe mode.", f, Brushes.Gold, body.X + 10, body.Y + 30);

            using (var f = new Font("Courier New", 11))
                g.DrawString($"Enabled: {StreamingModeSettings.Enabled}",
                    f, StreamingModeSettings.Enabled ? Brushes.LimeGreen : Brushes.LightGray, body.X + 12, body.Y + 62);
        }

        private void HandleCustomizationInput(InputManager input)
        {
            var owned = CosmeticInventorySystem.GetOwned().OrderBy(x => x).ToList();
            for (int i = 0; i < Math.Min(9, owned.Count); i++)
            {
                var key = System.Windows.Forms.Keys.D1 + i;
                if (!input.IsPressed(key)) continue;
                string id = owned[i];
                if (CosmeticInventorySystem.Equip(id))
                {
                    SMB3Hud.ShowToast($"Equipped {id}");
                    RefreshData();
                }
                return;
            }

            if (input.IsPressed(System.Windows.Forms.Keys.L))
            {
                var langs = _languages;
                if (langs.Count == 0) return;
                int idx = langs.ToList().FindIndex(x => x.Equals(LanguagePackManager.CurrentLanguage, StringComparison.OrdinalIgnoreCase));
                int next = (idx + 1) % langs.Count;
                LanguagePackManager.SetLanguage(langs[next]);
                SMB3Hud.ShowToast($"Language: {langs[next]}");
            }
        }

        private void HandleCosmeticsShopInput(InputManager input)
        {
            var catalog = WorkshopIntegration.GetCatalog().Take(8).ToList();
            for (int i = 0; i < catalog.Count; i++)
            {
                var key = System.Windows.Forms.Keys.D1 + i;
                if (!input.IsPressed(key)) continue;
                var item = catalog[i];
                var owned = CosmeticInventorySystem.GetOwned();
                if (owned.Contains(item.Id))
                {
                    SMB3Hud.ShowToast($"Already owned: {item.Id}");
                    return;
                }

                if (Game.Instance.PlayerBounty < 500)
                {
                    SMB3Hud.ShowToast("Not enough bounty (need 500)");
                    return;
                }

                Game.Instance.PlayerBounty -= 500;
                CosmeticInventorySystem.AddOwned(item.Id);
                WorkshopIntegration.Install(item.Id);
                PlayerProfileSystem.AddXp(100);
                SMB3Hud.ShowToast($"Purchased {item.Title}");
                RefreshData();
                return;
            }
        }

        private void HandleCustomSetupInput(InputManager input)
        {
            if (input.IsPressed(System.Windows.Forms.Keys.J))
                CustomGameSetupState.StartingLives = Math.Max(1, CustomGameSetupState.StartingLives - 1);
            if (input.IsPressed(System.Windows.Forms.Keys.K))
                CustomGameSetupState.StartingLives = Math.Min(9, CustomGameSetupState.StartingLives + 1);
            if (input.IsPressed(System.Windows.Forms.Keys.H))
                CustomGameSetupState.HardEnemies = !CustomGameSetupState.HardEnemies;
            if (input.IsPressed(System.Windows.Forms.Keys.Y))
                CustomGameSetupState.SkipTutorials = !CustomGameSetupState.SkipTutorials;
        }

        private void HandleStreamingInput(InputManager input)
        {
            if (!input.IsPressed(System.Windows.Forms.Keys.T)) return;
            StreamingModeSettings.Toggle();
            SMB3Hud.ShowToast("Streaming mode: " + (StreamingModeSettings.Enabled ? "ON" : "OFF"));
        }

        private void RefreshData()
        {
            _mods = ModMetadataSystem.LoadAll().ToList();
            _dlcs = DlcDetectionSystem.GetInstalledPackages();
            _profile = PlayerProfileSystem.Load();
            LanguagePackManager.EnsureLoaded();
            _languages = LanguagePackManager.GetAvailableLanguages();
            _leaderboardSummary = string.Join("\n", Phase3ProducerSystems.GetLeaderboard().Take(5)
                .Select((r, i) => $"#{i + 1} {r.Player} {r.Score:N0}"));
        }
    }
}
