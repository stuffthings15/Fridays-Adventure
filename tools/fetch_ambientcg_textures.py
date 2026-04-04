#!/usr/bin/env python3
"""
Lightweight ambientCG fetcher.
Searches manifest queries (dirt/grass/rock/wood), picks one material each,
and downloads 1K color/albedo/basecolor assets into third_party archives + runtime paths.
"""

from __future__ import annotations

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
DOCS_PATH = ROOT / "docs" / "THIRD_PARTY_ASSETS.md"
ARCHIVES_ROOT = THIRD_PARTY / "archives" / "ambientcg"

IMAGE_EXTS = {".png", ".jpg", ".jpeg", ".webp"}


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
        return ROOT / "Assets" / "Art" / "ThirdParty"
    if engine == "godot":
        for p in (ROOT / "assets" / "art", ROOT / "assets", ROOT / "Assets"):
            if p.exists():
                return p
        return ROOT / "assets" / "art"
    if engine == "web":
        for p in (ROOT / "public" / "assets", ROOT / "assets", ROOT / "public"):
            if p.exists():
                return p
        return ROOT / "public" / "assets"
    for p in (ROOT / "Assets", ROOT / "assets", ROOT / "public" / "assets", ROOT / "Content"):
        if p.exists():
            return p
    return ROOT / "Assets"


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


def http_json(url: str):
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(req, timeout=30) as r:
        return json.loads(r.read().decode("utf-8", errors="replace"))


def search_assets(query: str) -> list[dict]:
    encoded = urllib.parse.quote(query)
    urls = [
        f"https://ambientcg.com/api/v2/full_json?include=downloadData&limit=25&type=Material&q={encoded}",
        f"https://ambientcg.com/api/v2/full_json?include=downloadData&limit=25&q={encoded}",
    ]
    for u in urls:
        try:
            data = http_json(u)
        except Exception:
            continue
        if isinstance(data, dict):
            for key in ("foundAssets", "assets", "results"):
                if key in data and isinstance(data[key], list):
                    return data[key]
            if "foundAssets" in data and isinstance(data["foundAssets"], dict):
                return list(data["foundAssets"].values())
    return []


def find_id(asset: dict) -> str:
    for k in ("assetId", "id", "assetID"):
        if k in asset:
            return str(asset[k])
    return "unknown"


def collect_urls(obj):
    urls = []
    if isinstance(obj, dict):
        for v in obj.values():
            urls.extend(collect_urls(v))
    elif isinstance(obj, list):
        for v in obj:
            urls.extend(collect_urls(v))
    elif isinstance(obj, str) and obj.startswith("http"):
        urls.append(obj)
    return urls


def pick_download_url(asset: dict, target_resolution: str) -> str | None:
    urls = collect_urls(asset)
    preferred, fallback = [], []
    for u in urls:
        ul = u.lower()
        if not any(ext in ul for ext in [".zip", ".png", ".jpg", ".jpeg", ".webp"]):
            continue
        if target_resolution.lower() in ul and any(k in ul for k in ["color", "albedo", "basecolor"]):
            preferred.append(u)
        elif target_resolution.lower() in ul:
            fallback.append(u)
    if preferred:
        return preferred[0]
    if fallback:
        return fallback[0]
    for u in urls:
        ul = u.lower()
        if any(ext in ul for ext in [".zip", ".png", ".jpg", ".jpeg", ".webp"]):
            return u
    return None


def download_file(url: str, dst: Path):
    dst.parent.mkdir(parents=True, exist_ok=True)
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(req, timeout=60) as resp, dst.open("wb") as out:
        shutil.copyfileobj(resp, out)


def filename_from_url(url: str) -> str:
    p = urllib.parse.urlparse(url)
    qs = urllib.parse.parse_qs(p.query)
    if "file" in qs and qs["file"]:
        return urllib.parse.unquote(qs["file"][0])
    return urllib.parse.unquote(os.path.basename(p.path)) or "download.bin"


def copy_runtime_images(src_path: Path, runtime_dir: Path) -> list[Path]:
    runtime_dir.mkdir(parents=True, exist_ok=True)
    copied = []

    def copy_if_image(path: Path):
        if path.suffix.lower() in IMAGE_EXTS and any(k in path.name.lower() for k in ["color", "albedo", "basecolor", "diffuse"]):
            dst = runtime_dir / path.name
            if not dst.exists() or path.stat().st_size != dst.stat().st_size:
                shutil.copy2(path, dst)
            copied.append(dst)

    if zipfile.is_zipfile(src_path):
        extract_dir = src_path.parent / (src_path.stem + "_extract")
        extract_dir.mkdir(parents=True, exist_ok=True)
        with zipfile.ZipFile(src_path, "r") as zf:
            zf.extractall(extract_dir)
        for p in extract_dir.rglob("*"):
            if p.is_file():
                copy_if_image(p)
    else:
        copy_if_image(src_path)

    return copied


def update_docs(chosen_rows: list[dict]) -> None:
    if not DOCS_PATH.exists() or not chosen_rows:
        return
    with DOCS_PATH.open("r", encoding="utf-8") as f:
        text = f.read()

    marker = "## ambientCG Texture Imports (lightweight)"
    if marker not in text:
        return

    lines = [
        "## ambientCG Texture Imports (lightweight)",
        "",
        "Target query set from manifest:",
        "- dirt",
        "- grass",
        "- rock",
        "- wood",
        "",
        "Exact chosen asset IDs and final download URLs:",
    ]
    for row in chosen_rows:
        lines.append(f"- **{row['query']}** → `{row['assetId']}`")
        lines.append(f"  - Source: `{row['sourcePage']}`")
        lines.append(f"  - Downloaded: `{row['download']}`")
    lines.append("")
    lines.append("> Note: ambientCG imports are treated as source/reference textures only.")
    lines.append("> They are not a mandate to replace pixel-art tiles.")

    new_section = "\n".join(lines)
    after = text.split(marker, 1)[1]
    next_header_idx = after.find("\n## ")
    if next_header_idx == -1:
        updated = text.split(marker, 1)[0] + new_section + "\n"
    else:
        updated = text.split(marker, 1)[0] + new_section + after[next_header_idx:] + "\n"

    with DOCS_PATH.open("w", encoding="utf-8") as f:
        f.write(updated)


def main() -> int:
    manifest = load_json(MANIFEST_PATH, None)
    if manifest is None:
        print(f"Manifest missing: {MANIFEST_PATH}")
        return 1

    ambient = manifest.get("ambientcg", {})
    if not ambient.get("enabled", False):
        print("ambientcg disabled in manifest")
        return 0

    queries = ambient.get("queries", ["dirt", "grass", "rock", "wood"])
    target_resolution = ambient.get("targetResolution", "1K")

    engine = detect_engine()
    asset_root = detect_asset_root(engine)
    runtime_root = asset_root / "third_party" / ambient.get("runtimeSubdir", "vendor/ambientcg/source-textures")
    runtime_root.mkdir(parents=True, exist_ok=True)
    ARCHIVES_ROOT.mkdir(parents=True, exist_ok=True)

    index = load_json(INDEX_PATH, {
        "version": 1,
        "generatedAt": None,
        "engine": engine,
        "runtimeAssetRoot": str((asset_root / "third_party").relative_to(ROOT)).replace("\\", "/"),
        "records": [],
    })
    records_by_id = {r.get("id"): r for r in index.get("records", []) if r.get("id")}

    chosen_rows = []

    for q in queries:
        try:
            assets = search_assets(q)
        except Exception:
            assets = []

        if not assets:
            print(f"ambientcg: no result for query '{q}'")
            continue

        chosen = assets[0]
        asset_id = find_id(chosen)
        url = pick_download_url(chosen, target_resolution)
        if not url:
            print(f"ambientcg: no downloadable URL for query '{q}' (asset {asset_id})")
            continue

        archive_dir = ARCHIVES_ROOT / asset_id
        archive_dir.mkdir(parents=True, exist_ok=True)
        filename = filename_from_url(url)
        local_file = archive_dir / filename

        if not local_file.exists():
            try:
                download_file(url, local_file)
            except Exception as ex:
                print(f"ambientcg: failed download for '{q}' ({asset_id}): {ex}")
                continue

        runtime_query_dir = runtime_root / q
        copied = copy_runtime_images(local_file, runtime_query_dir)

        rec_id = f"ambientcg:{q}"
        row = {
            "id": rec_id,
            "query": q,
            "assetId": asset_id,
            "sourcePage": f"https://ambientcg.com/view?id={asset_id}",
            "download": url,
            "license": "CC0",
            "attribution": "ambientCG",
            "localArchivePath": str(local_file.relative_to(ROOT)).replace("\\", "/"),
            "localExtractedPath": str((local_file.parent).relative_to(ROOT)).replace("\\", "/"),
            "localRuntimePath": str(runtime_query_dir.relative_to(ROOT)).replace("\\", "/"),
            "runtimeFiles": [str(p.relative_to(ROOT)).replace("\\", "/") for p in copied],
            "fetchedAt": dt.datetime.utcnow().isoformat(timespec="seconds") + "Z",
        }
        records_by_id[rec_id] = row
        chosen_rows.append(row)

    index["generatedAt"] = dt.datetime.utcnow().isoformat(timespec="seconds") + "Z"
    index["engine"] = engine
    index["runtimeAssetRoot"] = str((asset_root / "third_party").relative_to(ROOT)).replace("\\", "/")
    index["records"] = [records_by_id[k] for k in sorted(records_by_id.keys())]
    save_json(INDEX_PATH, index)
    update_docs(chosen_rows)

    print(f"engine={engine}")
    print(f"runtime_asset_root={asset_root / 'third_party'}")
    print("ambientcg_downloaded=" + ",".join([f"{r['query']}:{r['assetId']}" for r in chosen_rows]))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
