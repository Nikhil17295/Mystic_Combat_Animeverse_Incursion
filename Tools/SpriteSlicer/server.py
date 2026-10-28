import os
import sys
import time
import json
import urllib.parse
import threading
from http.server import HTTPServer, SimpleHTTPRequestHandler
from PIL import Image

PROJECT_ROOT = r"B:\Unity\Projects\Mystic_Combat_Animeverse_Incursion"
TOOL_DIR = os.path.dirname(os.path.abspath(__file__))
PORT = 8085

class SlicerHandler(SimpleHTTPRequestHandler):
    def __init__(self, *args, **kwargs):
        super().__init__(*args, directory=TOOL_DIR, **kwargs)

    def end_headers(self):
        self.send_header("Cache-Control", "no-store, no-cache, must-revalidate, max-age=0")
        self.send_header("Pragma", "no-cache")
        self.send_header("Expires", "0")
        self.send_header("Access-Control-Allow-Origin", "*")
        super().end_headers()

    def do_GET(self):
        parsed = urllib.parse.urlparse(self.path)
        # Serve project sprites directly
        if parsed.path.startswith("/sprites/"):
            rel_path = urllib.parse.unquote(parsed.path.replace("/sprites/", ""))
            full_path = os.path.join(PROJECT_ROOT, "Assets", "Sprites", rel_path)
            if os.path.exists(full_path):
                self.send_response(200)
                if full_path.endswith(".png"):
                    self.send_header("Content-Type", "image/png")
                elif full_path.endswith(".jpg") or full_path.endswith(".jpeg"):
                    self.send_header("Content-Type", "image/jpeg")
                self.end_headers()
                with open(full_path, "rb") as f:
                    self.wfile.write(f.read())
                return
            else:
                self.send_error(404, f"File not found: {rel_path}")
                return
        super().do_GET()

    def do_POST(self):
        # API: Graceful shutdown
        if self.path == "/api/shutdown":
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.end_headers()
            self.wfile.write(b'{"status":"stopped"}')
            print("[SpriteSlicer] Server shutdown requested by user from web interface. Stopping...")
            def shutdown_task():
                time.sleep(0.5)
                os._exit(0)
            threading.Thread(target=shutdown_task, daemon=True).start()
            return

        if self.path == "/api/export":
            content_length = int(self.headers["Content-Length"])
            post_data = self.rfile.read(content_length)
            data = json.loads(post_data.decode("utf-8"))

            character = data.get("character", "Goku") # Goku or Saitama
            animations = data.get("animations", {})   # dict of anim_name -> list of {x, y, w, h}
            sheet_rel = urllib.parse.unquote(data.get("sheetPath", ""))
            sheet_full = os.path.join(PROJECT_ROOT, "Assets", "Sprites", sheet_rel)
            if not os.path.exists(sheet_full):
                self.send_response(400)
                self.end_headers()
                self.wfile.write(b'{"status":"error","message":"Sheet not found"}')
                return

            dest_folder = data.get("destFolder")
            if dest_folder:
                dest_dir = os.path.join(PROJECT_ROOT, "Assets", "Sprites", dest_folder)
            elif character.lower() == "goku_ssj":
                dest_dir = os.path.join(PROJECT_ROOT, "Assets", "Sprites", "Fighters", "Goku", "Frames_SSJ")
            else:
                dest_dir = os.path.join(PROJECT_ROOT, "Assets", "Sprites", "Fighters", character, "Frames")
            os.makedirs(dest_dir, exist_ok=True)

            img = Image.open(sheet_full).convert("RGBA")

            # Check if Goku has background color that needs transparency
            bg_color = data.get("bgColor") # optional [r, g, b]

            total_exported = 0
            for anim_name, frames in animations.items():
                for idx, box in enumerate(frames):
                    x = int(box["x"])
                    y = int(box["y"])
                    w = int(box["w"])
                    h = int(box["h"])

                    # Crop
                    crop = img.crop((x, y, x + w, y + h))

                    # Remove background if requested
                    if bg_color:
                        cdata = crop.getdata()
                        ndata = []
                        for px in cdata:
                            if abs(px[0] - bg_color[0]) < 25 and abs(px[1] - bg_color[1]) < 25 and abs(px[2] - bg_color[2]) < 25:
                                ndata.append((0, 0, 0, 0))
                            else:
                                ndata.append(px)
                        crop.putdata(ndata)

                    # Save single frame or numbered frames
                    if len(frames) == 1:
                        fname = f"{anim_name}.png"
                    else:
                        fname = f"{anim_name}_{idx}.png"

                    out_path = os.path.join(dest_dir, fname)
                    crop.save(out_path, "PNG")
                    total_exported += 1

            # Also save coordinates to JSON in Assets/Data/Characters/
            json_dir = os.path.join(PROJECT_ROOT, "Assets", "Data", "Characters")
            os.makedirs(json_dir, exist_ok=True)
            json_path = os.path.join(json_dir, f"{character.lower()}_frames.json")
            with open(json_path, "w", encoding="utf-8") as jf:
                json.dump(animations, jf, indent=2)

            print(f"[SpriteSlicer] Exported {total_exported} frames for {character} to {dest_dir}!")

            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.end_headers()
            resp = json.dumps({"status": "ok", "exported": total_exported, "path": dest_dir})
            self.wfile.write(resp.encode("utf-8"))
            return

        self.send_error(404)

if __name__ == "__main__":
    server = HTTPServer(("127.0.0.1", PORT), SlicerHandler)
    print(f"=== Mystic Combat Interactive Slicer running at http://127.0.0.1:{PORT} ===")
    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
