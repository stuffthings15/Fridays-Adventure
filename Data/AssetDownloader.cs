// ────────────────────────────────────────────
// PHASE 2 - Team 8: Systems Programmer
// Feature: Automatic Asset Downloader
// Purpose: Downloads CC0 asset packs from the manifest on first launch,
//          extracts them, and copies runtime-safe files into
//          Assets/third_party/vendor/ permanently.
// ────────────────────────────────────────────
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.IO.Compression;
using System.Threading;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Fridays_Adventure.Data
{
    /// <summary>
    /// Reads the third_party/asset_manifest.json, downloads enabled CC0 asset
    /// packs on first launch, extracts ZIPs, and copies PNG/WAV/JSON files
    /// into the permanent Assets/third_party/vendor/ directory tree.
    /// <remarks>PHASE 2 - Team 8: Automatic Asset Downloader</remarks>
    /// </summary>
    public static class AssetDownloader
    {
        // ── Marker file indicating all assets have been fetched ───────
        private const string MarkerFileName = ".assets_downloaded";

        // ── File extensions considered safe for runtime use ───────────
        private static readonly HashSet<string> SafeExtensions = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ".png", ".jpg", ".jpeg", ".webp",
            ".wav", ".ogg", ".mp3",
            ".json", ".txt", ".xml"
        };

        // ── Progress callback signature ──────────────────────────────
        /// <summary>
        /// Reports download progress. packIndex = current pack (0-based),
        /// totalPacks = total enabled packs, packName = human-readable label,
        /// status = short description of current action.
        /// </summary>
        public delegate void ProgressCallback(int packIndex, int totalPacks,
            string packName, string status);

        // ── Public API ───────────────────────────────────────────────

        /// <summary>
        /// Returns true if all enabled asset packs have already been downloaded
        /// and the marker file exists.
        /// </summary>
        public static bool AssetsAlreadyDownloaded()
        {
            string marker = Path.Combine(GetAssetsRoot(), MarkerFileName);
            return File.Exists(marker);
        }

        /// <summary>
        /// Finds the project root by walking up from the executable directory
        /// looking for asset_manifest.json in a third_party folder.
        /// Returns null if not found.
        /// </summary>
        public static string FindProjectRoot()
        {
            // Start from the executable's base directory
            string dir = AppDomain.CurrentDomain.BaseDirectory;
            // Walk up to 6 levels (bin\Debug\ → project root is 2 up typically)
            for (int i = 0; i < 6; i++)
            {
                string manifest = Path.Combine(dir, "third_party", "asset_manifest.json");
                if (File.Exists(manifest)) return dir;
                string parent = Path.GetDirectoryName(dir);
                if (string.IsNullOrEmpty(parent) || parent == dir) break;
                dir = parent;
            }
            return null;
        }

        /// <summary>
        /// Gets the runtime Assets root under the executable's base directory.
        /// Creates the directory if it doesn't exist.
        /// </summary>
        public static string GetAssetsRoot()
        {
            string root = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, "Assets", "third_party", "vendor");
            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);
            return root;
        }

        /// <summary>
        /// Downloads all enabled packs from the manifest, extracts them, and
        /// copies runtime-safe files. Thread-safe. Can be called from a
        /// background thread.
        /// </summary>
        /// <param name="onProgress">Progress reporter (may be null).</param>
        /// <param name="cancel">Set to true to abort the download loop.</param>
        /// <returns>True if all packs were fetched successfully.</returns>
        public static bool DownloadAllAssets(ProgressCallback onProgress,
            ref bool cancel)
        {
            // Locate the project root (where third_party/asset_manifest.json lives)
            string projectRoot = FindProjectRoot();
            if (projectRoot == null)
            {
                // Fallback: try directly under base directory
                projectRoot = AppDomain.CurrentDomain.BaseDirectory;
            }

            string manifestPath = Path.Combine(projectRoot, "third_party", "asset_manifest.json");
            if (!File.Exists(manifestPath))
            {
                // No manifest — nothing to download. Write marker so we don't retry.
                WriteMarker();
                return true;
            }

            // Parse the manifest
            AssetManifest manifest = ParseManifest(manifestPath);
            if (manifest == null || manifest.Packs == null || manifest.Packs.Count == 0)
            {
                WriteMarker();
                return true;
            }

            // Collect enabled packs
            string defaultPreset = manifest.DefaultPreset ?? "pixel";
            var enabledPacks = new List<AssetPack>();
            foreach (var pack in manifest.Packs)
            {
                if (!pack.Enabled) continue;
                // Check if pack matches the default preset
                bool matchesPreset = false;
                if (pack.Presets != null)
                {
                    foreach (string p in pack.Presets)
                    {
                        if (string.Equals(p, defaultPreset, StringComparison.OrdinalIgnoreCase))
                        { matchesPreset = true; break; }
                    }
                }
                if (matchesPreset || pack.Enabled)
                    enabledPacks.Add(pack);
            }

            if (enabledPacks.Count == 0)
            {
                WriteMarker();
                return true;
            }

            // Prepare directories
            string archivesDir = Path.Combine(projectRoot, "third_party", "archives");
            string extractedDir = Path.Combine(projectRoot, "third_party", "extracted");
            string runtimeRoot = GetAssetsRoot();

            // Also prepare the project-side Assets directory for permanent storage
            string projectAssetsRoot = Path.Combine(projectRoot, "Assets", "third_party", "vendor");
            if (!Directory.Exists(projectAssetsRoot))
                Directory.CreateDirectory(projectAssetsRoot);

            int total = enabledPacks.Count;
            bool allOk = true;

            for (int i = 0; i < total; i++)
            {
                if (cancel) return false;

                var pack = enabledPacks[i];
                string label = pack.Id ?? ("Pack " + (i + 1));

                try
                {
                    // ── Step 1: Download ZIP ──────────────────────────────
                    onProgress?.Invoke(i, total, label, "Downloading...");

                    string archiveDir = Path.Combine(archivesDir, pack.Id);
                    Directory.CreateDirectory(archiveDir);
                    string fileName = UrlFileName(pack.DownloadUrl);
                    string archivePath = Path.Combine(archiveDir, fileName);

                    if (!File.Exists(archivePath))
                    {
                        DownloadFile(pack.DownloadUrl, archivePath);
                    }

                    if (cancel) return false;

                    // ── Step 2: Extract ZIP ───────────────────────────────
                    onProgress?.Invoke(i, total, label, "Extracting...");

                    string extractDir = Path.Combine(extractedDir, pack.Id);
                    if (!Directory.Exists(extractDir) || DirectoryIsEmpty(extractDir))
                    {
                        Directory.CreateDirectory(extractDir);
                        ExtractZip(archivePath, extractDir);
                    }

                    if (cancel) return false;

                    // ── Step 3: Copy runtime-safe files ───────────────────
                    onProgress?.Invoke(i, total, label, "Installing assets...");

                    string subdir = pack.RuntimeSubdir ?? ("vendor/" + pack.Id);
                    // Normalize subdir separators
                    subdir = subdir.Replace("/", Path.DirectorySeparatorChar.ToString());

                    // Copy to runtime output directory (bin\Debug\Assets\...)
                    string runtimeDest = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, "Assets", "third_party", subdir);
                    Directory.CreateDirectory(runtimeDest);
                    CopyRuntimeFiles(extractDir, runtimeDest);

                    // Copy to project source directory (permanent integration)
                    string projectDest = Path.Combine(projectRoot, "Assets", "third_party", subdir);
                    if (!string.Equals(projectDest, runtimeDest, StringComparison.OrdinalIgnoreCase))
                    {
                        Directory.CreateDirectory(projectDest);
                        CopyRuntimeFiles(extractDir, projectDest);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[AssetDownloader] Failed to process pack '{label}': {ex.Message}");
                    allOk = false;
                    // Continue with next pack — don't fail everything for one broken pack
                }
            }

            if (allOk)
            {
                onProgress?.Invoke(total, total, "Complete", "All assets installed!");
                WriteMarker();
            }

            return allOk;
        }

        // ── Internal helpers ─────────────────────────────────────────

        /// <summary>Writes the marker file to the runtime Assets root.</summary>
        private static void WriteMarker()
        {
            try
            {
                string marker = Path.Combine(GetAssetsRoot(), MarkerFileName);
                File.WriteAllText(marker, DateTime.UtcNow.ToString("o"));
            }
            catch { /* non-critical */ }
        }

        /// <summary>Downloads a file from url to localPath using WebClient.</summary>
        private static void DownloadFile(string url, string localPath)
        {
            // .NET Framework 4.7.2 compatible: use WebClient
            // Force TLS 1.2 for OpenGameArt / AmbientCG HTTPS
            ServicePointManager.SecurityProtocol =
                SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            using (var client = new WebClient())
            {
                client.Headers.Add("User-Agent",
                    "MissFridaysAdventure/1.0 (asset-downloader; +CC0)");
                client.DownloadFile(new Uri(url), localPath);
            }
        }

        /// <summary>Extracts a ZIP archive to the target directory.</summary>
        private static void ExtractZip(string archivePath, string extractDir)
        {
            // Use reflection to call ZipFile.ExtractToDirectory to avoid
            // compile-time resolution issues with the forwarded type.
            // The assembly reference is present in the csproj but the IDE
            // sometimes fails to resolve forwarded types.
            var asm = System.Reflection.Assembly.Load(
                "System.IO.Compression.FileSystem, Version=4.0.0.0, " +
                "Culture=neutral, PublicKeyToken=b77a5c561934e089");
            var zipFileType = asm.GetType("System.IO.Compression.ZipFile");
            var method = zipFileType.GetMethod("ExtractToDirectory",
                new[] { typeof(string), typeof(string) });
            method.Invoke(null, new object[] { archivePath, extractDir });
        }

        /// <summary>
        /// Copies all runtime-safe files (PNG, WAV, JSON, etc.) from
        /// sourceDir to destDir, preserving subdirectory structure.
        /// Skips files that already exist with the same size.
        /// </summary>
        private static void CopyRuntimeFiles(string sourceDir, string destDir)
        {
            foreach (string srcFile in Directory.GetFiles(sourceDir, "*.*",
                SearchOption.AllDirectories))
            {
                string ext = Path.GetExtension(srcFile);
                if (!SafeExtensions.Contains(ext)) continue;

                // Compute relative path and destination
                string rel = srcFile.Substring(sourceDir.Length).TrimStart(
                    Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string dstFile = Path.Combine(destDir, rel);

                // Skip if already exists with same size
                if (File.Exists(dstFile))
                {
                    var srcInfo = new FileInfo(srcFile);
                    var dstInfo = new FileInfo(dstFile);
                    if (srcInfo.Length == dstInfo.Length) continue;
                }

                string dstDir2 = Path.GetDirectoryName(dstFile);
                if (!Directory.Exists(dstDir2))
                    Directory.CreateDirectory(dstDir2);

                File.Copy(srcFile, dstFile, true);
            }
        }

        /// <summary>Extracts a filename from a download URL.</summary>
        private static string UrlFileName(string url)
        {
            if (string.IsNullOrEmpty(url)) return "download.zip";
            try
            {
                var uri = new Uri(url);
                string name = Path.GetFileName(uri.LocalPath);
                if (string.IsNullOrWhiteSpace(name)) return "download.zip";
                return Uri.UnescapeDataString(name);
            }
            catch { return "download.zip"; }
        }

        /// <summary>Checks if a directory is empty (no files).</summary>
        private static bool DirectoryIsEmpty(string dir)
        {
            return Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories).Length == 0;
        }

        // ── Manifest parsing (manual JSON for .NET 4.7.2 compat) ────

        /// <summary>
        /// Parses asset_manifest.json into a lightweight data model.
        /// Uses manual JSON parsing for maximum .NET Framework compatibility.
        /// </summary>
        private static AssetManifest ParseManifest(string path)
        {
            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                var manifest = new AssetManifest();

                // Parse defaultPreset
                manifest.DefaultPreset = ExtractJsonString(json, "defaultPreset") ?? "pixel";

                // Parse packs array
                manifest.Packs = new List<AssetPack>();
                int packsStart = json.IndexOf("\"packs\"");
                if (packsStart < 0) return manifest;

                int arrStart = json.IndexOf('[', packsStart);
                if (arrStart < 0) return manifest;

                int arrEnd = FindMatchingBracket(json, arrStart, '[', ']');
                if (arrEnd < 0) return manifest;

                string packsJson = json.Substring(arrStart + 1, arrEnd - arrStart - 1);

                // Split by object boundaries
                int depth = 0;
                int objStart = -1;
                for (int i = 0; i < packsJson.Length; i++)
                {
                    char c = packsJson[i];
                    if (c == '{')
                    {
                        if (depth == 0) objStart = i;
                        depth++;
                    }
                    else if (c == '}')
                    {
                        depth--;
                        if (depth == 0 && objStart >= 0)
                        {
                            string objJson = packsJson.Substring(objStart,
                                i - objStart + 1);
                            var pack = ParsePack(objJson);
                            if (pack != null)
                                manifest.Packs.Add(pack);
                            objStart = -1;
                        }
                    }
                }

                return manifest;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[AssetDownloader] Failed to parse manifest: {ex.Message}");
                return null;
            }
        }

        /// <summary>Parses a single pack JSON object.</summary>
        private static AssetPack ParsePack(string json)
        {
            var pack = new AssetPack();
            pack.Id = ExtractJsonString(json, "id");
            pack.DownloadUrl = ExtractJsonString(json, "download");
            pack.RuntimeSubdir = ExtractJsonString(json, "runtimeSubdir");
            pack.License = ExtractJsonString(json, "license");

            // Parse enabled boolean
            string enabledStr = ExtractJsonValue(json, "enabled");
            pack.Enabled = string.Equals(enabledStr, "true",
                StringComparison.OrdinalIgnoreCase);

            // Parse preset array
            pack.Presets = new List<string>();
            int presetStart = json.IndexOf("\"preset\"");
            if (presetStart >= 0)
            {
                int arrStart = json.IndexOf('[', presetStart);
                if (arrStart >= 0)
                {
                    int arrEnd = FindMatchingBracket(json, arrStart, '[', ']');
                    if (arrEnd >= 0)
                    {
                        string arrContent = json.Substring(arrStart + 1,
                            arrEnd - arrStart - 1);
                        // Extract quoted strings from the array
                        int qStart = -1;
                        for (int i = 0; i < arrContent.Length; i++)
                        {
                            if (arrContent[i] == '"')
                            {
                                if (qStart < 0)
                                    qStart = i + 1;
                                else
                                {
                                    pack.Presets.Add(arrContent.Substring(
                                        qStart, i - qStart));
                                    qStart = -1;
                                }
                            }
                        }
                    }
                }
            }

            return pack;
        }

        /// <summary>Extracts a JSON string value by key name.</summary>
        private static string ExtractJsonString(string json, string key)
        {
            string pattern = "\"" + key + "\"";
            int idx = json.IndexOf(pattern);
            if (idx < 0) return null;

            // Find the colon, then the opening quote
            int colonIdx = json.IndexOf(':', idx + pattern.Length);
            if (colonIdx < 0) return null;

            int quoteStart = json.IndexOf('"', colonIdx + 1);
            if (quoteStart < 0) return null;

            int quoteEnd = json.IndexOf('"', quoteStart + 1);
            if (quoteEnd < 0) return null;

            return json.Substring(quoteStart + 1, quoteEnd - quoteStart - 1);
        }

        /// <summary>Extracts a raw JSON value (for booleans/numbers).</summary>
        private static string ExtractJsonValue(string json, string key)
        {
            string pattern = "\"" + key + "\"";
            int idx = json.IndexOf(pattern);
            if (idx < 0) return null;

            int colonIdx = json.IndexOf(':', idx + pattern.Length);
            if (colonIdx < 0) return null;

            // Skip whitespace after colon
            int start = colonIdx + 1;
            while (start < json.Length && char.IsWhiteSpace(json[start])) start++;

            // Read until comma, closing brace, or end
            int end = start;
            while (end < json.Length && json[end] != ',' && json[end] != '}'
                && json[end] != ']' && !char.IsWhiteSpace(json[end]))
                end++;

            return json.Substring(start, end - start).Trim();
        }

        /// <summary>Finds the matching closing bracket.</summary>
        private static int FindMatchingBracket(string json, int openIdx,
            char open, char close)
        {
            int depth = 0;
            for (int i = openIdx; i < json.Length; i++)
            {
                if (json[i] == open) depth++;
                else if (json[i] == close)
                {
                    depth--;
                    if (depth == 0) return i;
                }
            }
            return -1;
        }

        // ── Data classes ─────────────────────────────────────────────

        /// <summary>Lightweight model for the asset manifest.</summary>
        internal class AssetManifest
        {
            public string DefaultPreset;
            public List<AssetPack> Packs;
        }

        /// <summary>Lightweight model for a single asset pack entry.</summary>
        internal class AssetPack
        {
            public string Id;
            public string DownloadUrl;
            public string RuntimeSubdir;
            public string License;
            public bool Enabled;
            public List<string> Presets;
        }
    }
}
