import json

with open(r"Assets\Data\Characters\saitama_frames.json", "r") as f:
    d = json.load(f)

for k, v in d.items():
    if "vic" in k or "win" in k:
        print(k, v)
