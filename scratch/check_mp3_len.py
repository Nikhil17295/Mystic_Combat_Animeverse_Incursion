import os

def get_mp3_duration(filename):
    # Quick MP3 duration from file size and bitrate or frame header
    import struct
    with open(filename, 'rb') as f:
        data = f.read()
    
    # Simple search for sync bytes
    pos = 0
    while pos < len(data) - 4:
        if data[pos] == 0xFF and (data[pos+1] & 0xE0) == 0xE0:
            # Found frame header
            version = (data[pos+1] >> 3) & 3
            layer = (data[pos+1] >> 1) & 3
            bitrate_idx = (data[pos+2] >> 4) & 0x0F
            samplerate_idx = (data[pos+2] >> 2) & 3
            
            # MPEG1 Layer 3 bitrates
            bitrates = [0, 32, 40, 48, 56, 64, 80, 96, 112, 128, 160, 192, 224, 256, 320, 0]
            if bitrate_idx > 0 and bitrate_idx < 15:
                br = bitrates[bitrate_idx] * 1000
                duration = (len(data) * 8) / float(br)
                return duration
        pos += 1
    return 0

base = r"Assets\Audio\SFX_Library\Anime_Powers"
for name in ["consecutive_normal_punches.mp3", "serious_punch.mp3", "dbz_super_saiyan_aura.mp3"]:
    p = os.path.join(base, name)
    print(f"{name}: approx {get_mp3_duration(p):.2f}s (file size: {os.path.getsize(p)} bytes)")
