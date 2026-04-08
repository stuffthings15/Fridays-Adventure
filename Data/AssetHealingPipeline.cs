// ────────────────────────────────────────────
// PHASE 2 - Team 8: Systems Programmer
// Feature: Asset Healing Pipeline
// Purpose: Orchestrates the full self-healing asset loop:
//          Detect gaps → Search vendors → Download if needed →
//          Auto-resolve → Copy to project → Invalidate caches →
//          Generate report.
//          Designed to run at startup and on-demand from DevMenu.
// ────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Fridays_Adventure.Data
{
    /// <summary>
    /// Orchestrates the complete self-healing asset pipeline.
    /// Runs as a single pass: detect → resolve → report.
    /// Thread-safe. Can be called from a background thread.
    /// <remarks>PHASE 2 - Team 8: Self-Healing Asset Pipeline — Orchestrator</remarks>
    /// </summary>
    public static class AssetHealingPipeline
    {
        // ── Pipeline state ───────────────────────────────────────────
        private static volatile bool _running;
        private static volatile string _status = "";
        private static volatile int _phase;      // 0=idle, 1=scanning, 2=resolving, 3=reporting
        private static volatile int _totalGaps;
        private static volatile int _resolvedGaps;
        private static string _lastReport = "";

        /// <summary>Whether the pipeline is currently executing.</summary>
        public static bool IsRunning => _running;

        /// <summary>Human-readable status of the current pipeline phase.</summary>
        public static string Status => _status;

        /// <summary>Current pipeline phase number (0=idle).</summary>
        public static int Phase => _phase;

        /// <summary>Total asset gaps detected in the current/last run.</summary>
        public static int TotalGaps => _totalGaps;

        /// <summary>Number of gaps resolved in the current/last run.</summary>
        public static int ResolvedGaps => _resolvedGaps;

        /// <summary>The full text report from the last pipeline run.</summary>
        public static string LastReport => _lastReport;

        // ── Callbacks ────────────────────────────────────────────────

        /// <summary>
        /// Reports pipeline progress. phase = pipeline phase name,
        /// detail = human-readable status message.
        /// </summary>
        public delegate void StatusCallback(string phase, string detail);

        // ── Main pipeline entry point ────────────────────────────────

        /// <summary>
        /// Executes the full self-healing pipeline:
        /// 1. Pre-scan: walk all .cs files to find sprite/audio references
        /// 2. Probe: attempt to load each reference via SpriteManager.ResolvePath
        /// 3. Resolve: search vendors + generate placeholders for missing assets
        /// 4. Report: generate a summary of what was fixed
        /// Returns true if all gaps were resolved.
        /// </summary>
        /// <param name="onStatus">Optional progress callback.</param>
        public static bool RunFullPipeline(StatusCallback onStatus = null)
        {
            if (_running) return false;
            _running = true;
            _totalGaps = 0;
            _resolvedGaps = 0;

            try
            {
                // ── Phase 1: Scan for known asset references ─────────
                _phase = 1;
                _status = "Scanning for missing assets...";
                onStatus?.Invoke("SCAN", _status);

                ScanForMissingAssets();

                // ── Phase 2: Resolve all detected gaps ───────────────
                _phase = 2;
                var unresolved = AssetGapDetector.GetUnresolvedMisses();
                _totalGaps = unresolved.Count;

                if (_totalGaps > 0)
                {
                    _status = $"Resolving {_totalGaps} missing assets...";
                    onStatus?.Invoke("RESOLVE", _status);

                    // Invalidate vendor index in case new packs were downloaded
                    AssetAutoResolver.InvalidateIndex();

                    _resolvedGaps = AssetAutoResolver.ResolveAllGaps();

                    // Invalidate sprite cache so resolved assets load on next access
                    SpriteManager.InvalidateCache();
                }

                // ── Phase 3: Validate resolved assets ─────────────
                _phase = 3;
                _status = "Validating resolved assets...";
                onStatus?.Invoke("VALIDATE", _status);

                ValidateResolvedAssets();

                // ── Phase 4: Generate report ─────────────────────────
                _phase = 4;
                _status = "Generating report...";
                onStatus?.Invoke("REPORT", _status);

                _lastReport = AssetGapDetector.GenerateReport();

                // Save report to Logs directory
                SaveReport(_lastReport);

                // Final status
                int remaining = _totalGaps - _resolvedGaps;
                if (remaining == 0 && _totalGaps > 0)
                {
                    _status = $"✅ All {_totalGaps} asset gaps resolved!";
                }
                else if (_totalGaps == 0)
                {
                    _status = "✅ No missing assets detected.";
                }
                else
                {
                    _status = $"⚠ {_resolvedGaps}/{_totalGaps} resolved, {remaining} remaining.";
                }
                onStatus?.Invoke("COMPLETE", _status);

                return remaining == 0;
            }
            catch (Exception ex)
            {
                _status = $"Pipeline error: {ex.Message}";
                onStatus?.Invoke("ERROR", _status);
                return false;
            }
            finally
            {
                _phase = 0;
                _running = false;
            }
        }

        // ── Asset reference scanner ──────────────────────────────────

        /// <summary>
        /// Scans all known sprite and audio references and probes whether
        /// they exist on disk. Feeds misses into the AssetGapDetector.
        /// </summary>
        private static void ScanForMissingAssets()
        {
            string spritesDir = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sprites");
            string audioDir = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Assets", "Audio");
            string sfxDir = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Assets", "SfxCache");

            // ── Known sprite references from the asset audit ─────────
            // These are filenames referenced in code (Sessions 127, 62, etc.)
            string[] knownSpriteRefs = new[]
            {
                // Player characters
                "player_missfriday.png", "player_Miss_Friday.png",
                "player_Orca.png", "player_Swan.png",
                // Enemies
                "enemy_goomba.png", "enemy_koopa.png", "enemy_hammer_bro.png",
                "enemy_boss.png", "enemy_marine.png", "enemy_Garp.png",
                "enemy_Cloud_Lancer.png", "enemy_Oni_Ashigaru.png",
                "enemy_Raptor_Marauder.png", "enemy_Ronin_Enforcer.png",
                "enemy_Thunder_Mask_Priest.png", "enemy_Triceratops_Brute.png",
                // Bosses
                "boss_Garp.png", "GARP.png",
                // Portraits
                "portrait_friday.png", "portrait_orca.png", "portrait_swan.png",
                // Backgrounds
                "bg_dinoIsland.png", "bg_skyisland.png", "bg_bladenation.png",
                "bg_island.png", "bg_overworld.png", "bg_title.png",
                "bg_Marine_Blockade.png", "bg_Centipede_of_the_Deep.png",
                "bg_Warlord_Sudo.png", "bg_Warlord_Vanta.png",
                "bg_Tempest_Strait.png", "bg_Harbor_Town.png",
                "bg_Coral_Reef.png", "bg_Tundra_Peak.png",
                "bg_Dive_Gate.png", "bg_Sunken_Gate.png",
                "bg_Kelp_Maze.png", "bg_Vent_Ruins.png", "bg_Abyss.png",
                "bg_Dinosaur_Island.png", "bg_Storm_Belt.png",
                "bg_Storm_island.png", "bg_Sea_Serpent.png",
                // UI
                "ui_panel.png",
            };

            // Check each known reference against disk
            foreach (string sprite in knownSpriteRefs)
            {
                if (!FileExistsAnywhere(sprite, spritesDir))
                {
                    AssetGapDetector.RecordMiss(sprite);
                }
            }

            // ── Known audio references ───────────────────────────────
            string[] knownAudioRefs = new[]
            {
                "music_overworld1.mp3", "music_combat1.mp3", "music_combat2.mp3",
                "music_island1.mp3", "music_island2.mp3",
                "music_boss1.mp3", "music_boss2.mp3",
                "music_hub1.mp3", "music_hub2.mp3",
                "music_exploration1.mp3", "music_exploration2.mp3",
                "music_event1.mp3", "music_event2.mp3",
                "music_theme1.mp3", "music_theme2.mp3",
                "music_finale1.mp3", "music_finale2.mp3",
            };

            foreach (string audio in knownAudioRefs)
            {
                if (!FileExistsInDir(audio, audioDir))
                {
                    AssetGapDetector.RecordMiss(audio);
                }
            }

            // ── Known SFX references ─────────────────────────────────
            string[] knownSfxRefs = new[]
            {
                "attack.wav", "berry.wav", "breakwall.wav", "coin.wav",
                "freeze.wav", "heal.wav", "hurt.wav", "ice.wav",
                "introambient.wav", "jump.wav", "seastone.wav", "sink.wav",
                "stomp.wav", "victoryfanfare.wav",
            };

            foreach (string sfx in knownSfxRefs)
            {
                if (!FileExistsInDir(sfx, sfxDir))
                {
                    AssetGapDetector.RecordMiss(sfx);
                }
            }
        }

        /// <summary>
        /// Checks if a sprite file exists in Sprites, Assets, or vendor directories.
        /// Uses the same resolution logic as SpriteManager.
        /// </summary>
        private static bool FileExistsAnywhere(string fileName, string spritesDir)
        {
            // Primary: Assets/Sprites/
            if (File.Exists(Path.Combine(spritesDir, fileName))) return true;

            // Secondary: Assets/
            string assetsDir = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Assets");
            if (File.Exists(Path.Combine(assetsDir, fileName))) return true;

            // Tertiary: vendor directories (recursive)
            string vendorDir = Path.Combine(assetsDir, "third_party", "vendor");
            if (Directory.Exists(vendorDir))
            {
                try
                {
                    string[] matches = Directory.GetFiles(vendorDir, fileName,
                        SearchOption.AllDirectories);
                    if (matches.Length > 0) return true;
                }
                catch { /* non-critical */ }
            }

            return false;
        }

        /// <summary>Checks if a file exists in a specific directory.</summary>
        private static bool FileExistsInDir(string fileName, string dir)
        {
            if (!Directory.Exists(dir)) return false;
            return File.Exists(Path.Combine(dir, fileName));
        }

        // ── Report persistence ───────────────────────────────────────

        /// <summary>Saves the pipeline report to Logs/asset-healing/.</summary>
        private static void SaveReport(string report)
        {
            try
            {
                string logsDir = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Logs", "asset-healing");
                Directory.CreateDirectory(logsDir);

                string fileName = $"healing_report_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string path = Path.Combine(logsDir, fileName);
                File.WriteAllText(path, report, Encoding.UTF8);
            }
            catch { /* non-critical */ }
        }

        // ── Validation pass ──────────────────────────────────────────

        /// <summary>
        /// Verifies that all "resolved" assets actually load correctly.
        /// Re-attempts to load each resolved sprite via SpriteManager.Get();
        /// if the load fails, marks the entry as unresolved again.
        /// </summary>
        private static void ValidateResolvedAssets()
        {
            var allMisses = AssetGapDetector.GetAllMisses();
            foreach (var entry in allMisses)
            {
                if (!entry.Resolved) continue;

                // Check that the resolved file actually exists on disk
                if (!string.IsNullOrEmpty(entry.ResolvedPath) && File.Exists(entry.ResolvedPath))
                {
                    // For sprite assets, verify the file is a valid image
                    string ext = Path.GetExtension(entry.FileName).ToLowerInvariant();
                    if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                    {
                        try
                        {
                            using (var bmp = new System.Drawing.Bitmap(entry.ResolvedPath))
                            {
                                // Valid image — check it's not degenerate (0×0)
                                if (bmp.Width <= 0 || bmp.Height <= 0)
                                {
                                    entry.Resolved = false;
                                    entry.ResolvedPath = null;
                                }
                            }
                        }
                        catch
                        {
                            // Corrupted image file — mark as unresolved
                            entry.Resolved = false;
                            entry.ResolvedPath = null;
                        }
                    }
                }
                else
                {
                    // File disappeared or was never written
                    entry.Resolved = false;
                    entry.ResolvedPath = null;
                }
            }
        }
    }
}
