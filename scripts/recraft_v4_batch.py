"""Generate Hero Influence lobby button SVGs via Recraft V4 Vector API."""
import json
import urllib.request
import urllib.error
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

MCP_JSON = Path(r"C:\dev\orora\.mcp.json")
API_URL = "https://external.api.recraft.ai/v1/images/generations"
OUT_DIR = Path(r"C:\dev\_IMG_recraft")

with open(MCP_JSON, encoding="utf-8") as f:
    API_KEY = json.load(f)["mcpServers"]["recraft"]["env"]["RECRAFT_API_KEY"]

BASE_PROMPT = (
    "Flat 2D square game UI button icon, front view, no 3D perspective, no isometric tilt, "
    "no photo realism. Modern flat icon design with cel-shaded comic accents. "
    "Palette: Overwatch inspired warm orange, deep navy, cream, accent red. "
    "Thin outlined square panel with subtle interior gradient. "
    "Centered single emblem: {icon}. "
    "No text, no numbers, no letters, no badges, no stickers, no characters, no people, "
    "no metal box, no tin can, no realistic shading."
)

BUTTONS = [
    ("HQ",          "a stylized capitol dome civic government building silhouette in deep navy on a cream panel"),
    ("Broadcast",   "a stylized megaphone with sound waves emanating outward, white megaphone on a warm orange panel"),
    ("Recruitment", "a heroic helmet emblem in deep navy on a cream panel"),
    ("Research",    "a hexagonal molecular lattice with a glowing blue crystal at the center, on a deep navy panel"),
]


def generate_one(name: str, icon: str) -> dict:
    prompt = BASE_PROMPT.format(icon=icon)
    payload = json.dumps({
        "prompt": prompt,
        "model": "recraftv4_vector",
        "n": 1,
        "size": "1024x1024",
        "style": "vector_illustration",
    }).encode("utf-8")
    req = urllib.request.Request(
        API_URL,
        data=payload,
        headers={
            "Authorization": f"Bearer {API_KEY}",
            "Content-Type": "application/json",
        },
        method="POST",
    )
    try:
        with urllib.request.urlopen(req, timeout=180) as resp:
            body = json.loads(resp.read().decode("utf-8"))
        url = body["data"][0]["url"]
        svg_path = OUT_DIR / f"LobbyBtn_{name}_v4.svg"
        with urllib.request.urlopen(url, timeout=60) as r:
            svg_path.write_bytes(r.read())
        # PNG preview via resvg
        try:
            import resvg_py
            png = resvg_py.svg_to_bytes(
                svg_string=svg_path.read_text(encoding="utf-8"),
                width=512, height=512,
            )
            (OUT_DIR / f"LobbyBtn_{name}_v4_preview.png").write_bytes(bytes(png))
        except Exception as e:
            print(f"[{name}] preview gen failed: {e}")
        return {"name": name, "ok": True, "credits": body.get("credits"), "svg": str(svg_path)}
    except urllib.error.HTTPError as e:
        return {"name": name, "ok": False, "error": f"HTTP {e.code}: {e.read().decode('utf-8', errors='replace')}"}
    except Exception as e:
        return {"name": name, "ok": False, "error": f"{type(e).__name__}: {e}"}


results = [generate_one(*b) for b in BUTTONS]

for r in results:
    print(json.dumps(r, ensure_ascii=False))
