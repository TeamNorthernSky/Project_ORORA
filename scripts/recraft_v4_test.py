"""Test Recraft V4 vector API - single button generation."""
import json
import urllib.request
import urllib.error
from pathlib import Path

MCP_JSON = Path(r"C:\dev\orora\.mcp.json")
API_URL = "https://external.api.recraft.ai/v1/images/generations"

with open(MCP_JSON, encoding="utf-8") as f:
    api_key = json.load(f)["mcpServers"]["recraft"]["env"]["RECRAFT_API_KEY"]

prompt = (
    "Flat 2D square game UI button icon, front view, no 3D perspective, no isometric tilt, "
    "no photo realism. Modern flat icon design with cel-shaded comic accents. "
    "Palette: Overwatch inspired warm orange, deep navy, cream, accent red. "
    "Thin outlined square panel with subtle interior gradient. "
    "Centered single emblem: an anvil with a hammer striking it and small sparks, "
    "in deep navy on a cream panel. "
    "No text, no numbers, no letters, no badges, no stickers, no characters, no people, "
    "no metal box, no tin can, no realistic shading."
)

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
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json",
    },
    method="POST",
)

try:
    with urllib.request.urlopen(req, timeout=120) as resp:
        body = resp.read().decode("utf-8")
        print(f"HTTP {resp.status}")
        print(body)
except urllib.error.HTTPError as e:
    print(f"HTTPError {e.code}: {e.reason}")
    print(e.read().decode("utf-8", errors="replace"))
except Exception as e:
    print(f"Error: {type(e).__name__}: {e}")
