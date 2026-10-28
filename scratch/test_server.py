import urllib.request
import subprocess
import time
import os

proc = subprocess.Popen(["python", "Tools/SpriteSlicer/server.py"])
time.sleep(1.5)

try:
    # 1. Fetch index.html
    req = urllib.request.urlopen("http://127.0.0.1:8085/")
    headers = dict(req.getheaders())
    content = req.read().decode("utf-8")
    print("HTTP Status:", req.status)
    print("Cache-Control Header:", headers.get("Cache-Control"))
    print("Contains 'btnCharGokuSSJ':", "btnCharGokuSSJ" in content)
    print("Contains 'goku_ssj_transparent.png':", "goku_ssj_transparent.png" in content)
    
    # 2. Fetch sprite directly
    sprite_req = urllib.request.urlopen("http://127.0.0.1:8085/sprites/Fighters/Goku/goku_ssj_transparent.png")
    print("Sprite HTTP Status:", sprite_req.status)
    print("Sprite Content-Type:", sprite_req.headers.get("Content-Type"))
    print("Sprite Size Bytes:", len(sprite_req.read()))
    
finally:
    proc.terminate()
    proc.wait()
    print("Server test completed and stopped cleanly.")
