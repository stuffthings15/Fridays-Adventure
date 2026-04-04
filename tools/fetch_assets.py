#!/usr/bin/env python3
"""
Fetch approved third-party asset packs, store archives in-repo, extract them,
and copy runtime-safe files into detected runtime asset root under third_party/vendor.

Usage:
  python tools/fetch_assets.py
  python tools/fetch_assets.py --preset pixel
  python tools/fetch_assets.py --preset clean
  python tools/fetch_assets.py --enable mattbas-platformer-pack
  python tools/fetch_assets.py --force
"""

from __future__ import annotations

import argparse
import datetime as dt
import json
import os
from pathlib import Path
import shutil
import urllib.parse
import urllib.request
import zipfile

ROOT = Path(__file__).resolve().parent.parent
THIRD_PARTY = ROOT / "third_party"
MANIFEST_PATH = THIRD_PARTY / "asset_manifest.json"
INDEX_PATH = THIRD_PARTY / "asset_index.json"
ARCHIVES_ROOT = THIRD_PARTY / "archives"
EXTRACTED_ROOT = THIRD_PARTY / "extracted"

ALWAYS_SAFE_EXTS = {
    ".png", ".jpg", ".jpeg", ".webp",
    ".wav", ".ogg", ".mp3",
    ".json", ".txt", ".xml",
}
SVG_EXT = ".svg"
MODEL_EXTS = {".fbx", ".obj", ".gltf", ".glb"}


def detect_engine() -> str:
    if (ROOT / "ProjectSettings").exists() and (ROOT / "Assets").exists():
        return "unity"
    if (ROOT / "project.godot").exists():
        return "godot"
    if (ROOT / "package.json").exists() and ((ROOT / "public").exists() or (ROOT / "src").exists()):
        return "web"
    return "custom"


def detect_asset_root(engine: str) -> Path:
    if engine == "unity":
        # Requested path for Unity projects
        return ROOT / "Assets" / "Art" / "ThirdParty"
    if engine == "godot":
        for p in (ROOT / "assets" / "art" / "third_party", ROOT / "assets" / "art", ROOT / "assets", ROOT / "Assets"):
            if p.exists():
                return p
        return ROOT / "assets" / "art"
    if engine == "web":
        for p in (ROOT / "public" / "assets" / "third_party", ROOT / "public" / "assets", ROOT / "assets", ROOT / "public"):
            if p.exists():
                return p
        return ROOT / "public" / "assets"

    # Custom fallback: detect likely existing asset root, then add third_party beneath it.
    for p in (ROOT / "Assets", ROOT / "assets", ROOT / "public" / "assets", ROOT / "Content"):
        if p.exists():
            return p
    return ROOT / "Assets"


def repo_has_svg_assets() -> bool:
    for p in ROOT.rglob("*.svg"):
        if "third_party" not in p.parts:
            return True
    return False


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Fetch approved third-party asset packs")
    parser.add_argument("--preset", action="append", default=[], help="Preset to enable (pixel/clean/etc)")
    parser.add_argument("--enable", action="append", default=[], help="Explicitly enable a pack id")
    parser.add_argument("--force", action="store_true", help="Force re-download and re-copy")
    return parser.parse_args()


def load_json(path: Path, default):
    if not path.exists():
        return default
    with path.open("r", encoding="utf-8") as f:
        return json.load(f)


def save_json(path: Path, data) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    with path.open("w", encoding="utf-8") as f:
        json.dump(data, f, indent=2)
        f.write("\n")


def url_filename(url: str) -> str:
    parsed = urllib.parse.urlparse(url)
    base = os.path.basename(parsed.path)
    return urllib.parse.unquote(base) or "download.zip"


def download_file(url: str, dst: Path, force: bool) -> bool:
    if dst.exists() and not force:
        return False
    dst.parent.mkdir(parents=True, exist_ok=True)
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(req, timeout=60) as resp, dst.open("wb") as out:
        shutil.copyfileobj(resp, out)
    return True


def extract_zip(archive_path: Path, extract_dir: Path, force: bool) -> bool:
    if extract_dir.exists() and not force and any(extract_dir.rglob("*")):
        return False
    if extract_dir.exists() and force:
        shutil.rmtree(extract_dir)
    extract_dir.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(archive_path, "r") as zf:
        zf.extractall(extract_dir)
    return True


def is_runtime_safe(src: Path, pack: dict, has_svg: bool) -> bool:
    ext = src.suffix.lower()
    if ext in ALWAYS_SAFE_EXTS:
        return True
    if ext == SVG_EXT:
        return has_svg
    if ext in MODEL_EXTS:
        return "optional-3d" in set(pack.get("preset", []))
    return False


def should_copy(src: Path, dst: Path, force: bool) -> bool:
    if force or not dst.exists():
        return True
    sst = src.stat()
    dstst = dst.stat()
    return sst.st_size != dstst.st_size or int(sst.st_mtime) != int(dstst.st_mtime)


def copy_runtime_files(pack: dict, extract_dir: Path, runtime_root: Path, has_svg: bool, force: bool) -> tuple[Path, int]:
    runtime_subdir = Path(pack["runtimeSubdir"])
    runtime_dir = runtime_root / runtime_subdir
    runtime_dir.mkdir(parents=True, exist_ok=True)

    copied = 0
    for src in extract_dir.rglob("*"):
        if not src.is_file():
            continue
        if not is_runtime_safe(src, pack, has_svg):
            continue
        rel = src.relative_to(extract_dir)
        dst = runtime_dir / rel
        dst.parent.mkdir(parents=True, exist_ok=True)
        if should_copy(src, dst, force):
            shutil.copy2(src, dst)
            copied += 1
    return runtime_dir, copied


def selected_packs(manifest: dict, presets: list[str], enable_ids: set[str]) -> list[dict]:
    active_presets = set(presets)
    if not active_presets:
        active_presets.add(manifest.get("defaultPreset", "pixel"))

    out = []
    for p in manifest.get("packs", []):
        p_presets = set(p.get("preset", []))
        enabled_by_manifest = bool(p.get("enabled", False))
        enabled = enabled_by_manifest and bool(active_presets.intersection(p_presets))
        if p["id"] in enable_ids:
            enabled = True
        if enabled:
            out.append(p)
    return out


def main() -> int:
    args = parse_args()

    manifest = load_json(MANIFEST_PATH, default=None)
    if manifest is None:
        print(f"Manifest missing: {MANIFEST_PATH}")
        return 1

    engine = detect_engine()
    asset_root = detect_asset_root(engine)

    # Runtime imports always under a third_party subtree in the chosen asset root.
    runtime_asset_root = asset_root / "third_party"
    runtime_asset_root.mkdir(parents=True, exist_ok=True)

    ARCHIVES_ROOT.mkdir(parents=True, exist_ok=True)
    EXTRACTED_ROOT.mkdir(parents=True, exist_ok=True)

    has_svg = repo_has_svg_assets()
    packs = selected_packs(manifest, args.preset, set(args.enable or []))

    index = load_json(
        INDEX_PATH,
        default={
            "version": 1,
            "generatedAt": None,
            "engine": engine,
            "runtimeAssetRoot": str(runtime_asset_root.relative_to(ROOT)).replace("\\", "/"),
            "records": [],
        },
    )
    records_by_id = {r.get("id"): r for r in index.get("records", []) if r.get("id")}

    downloaded_ids = []

    for pack in packs:
        pack_id = pack["id"]
        archive_dir = ARCHIVES_ROOT / pack_id
        archive_name = url_filename(pack["download"])
        archive_path = archive_dir / archive_name
        extracted_dir = EXTRACTED_ROOT / pack_id

        was_downloaded = download_file(pack["download"], archive_path, args.force)
        if was_downloaded:
            downloaded_ids.append(pack_id)

        extract_zip(archive_path, extracted_dir, args.force)
        runtime_dir, _copied_count = copy_runtime_files(pack, extracted_dir, runtime_asset_root, has_svg, args.force)

        records_by_id[pack_id] = {
            "id": pack_id,
            "sourcePage": pack["sourcePage"],
            "download": pack["download"],
            "license": pack["license"],
            "attribution": pack["attribution"],
            "localArchivePath": str(archive_path.relative_to(ROOT)).replace("\\", "/"),
            "localExtractedPath": str(extracted_dir.relative_to(ROOT)).replace("\\", "/"),
            "localRuntimePath": str(runtime_dir.relative_to(ROOT)).replace("\\", "/"),
            "fetchedAt": dt.datetime.utcnow().isoformat(timespec="seconds") + "Z",
        }

    index["generatedAt"] = dt.datetime.utcnow().isoformat(timespec="seconds") + "Z"
    index["engine"] = engine
    index["runtimeAssetRoot"] = str(runtime_asset_root.relative_to(ROOT)).replace("\\", "/")
    index["records"] = [records_by_id[k] for k in sorted(records_by_id.keys())]
    save_json(INDEX_PATH, index)

    print(f"engine={engine}")
    print(f"asset_root={asset_root}")
    print(f"runtime_asset_root={runtime_asset_root}")
    print("selected_packs=" + ",".join(p["id"] for p in packs))
    print("downloaded=" + ",".join(downloaded_ids))

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
