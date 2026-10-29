import os
import base64
import json

base_dir = r'Assets\Sprites\Fighters\Nezuko\Frames'
demon_dir = r'Assets\Sprites\Fighters\Nezuko\Frames_Demon'
artifact_dir = r'C:\Users\nikhi\.gemini\antigravity\brain\beb3fa88-c112-47b1-89f1-ed59ae6e798d'
os.makedirs(artifact_dir, exist_ok=True)

def load_frames(d):
    res = {}
    for f in sorted(os.listdir(d)):
        if f.endswith('.png'):
            anim_name = f.split('_')[0].replace('.png', '')
            if anim_name not in res:
                res[anim_name] = []
            with open(os.path.join(d, f), 'rb') as img_f:
                b64 = base64.b64encode(img_f.read()).decode('utf-8')
                res[anim_name].append({
                    'name': f,
                    'src': f'data:image/png;base64,{b64}'
                })
    return res

base_anims = load_frames(base_dir)
demon_anims = load_frames(demon_dir)

data_json = json.dumps({'base': base_anims, 'demon': demon_anims})

template = '''<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <title>Nezuko Kamado Sprite & Animation Showcase</title>
  <script src="https://www.gstatic.com/antigravity/web/dev/tailwindcss.min.js"></script>
  <style>
    .pixelated {
      image-rendering: pixelated;
      image-rendering: -moz-crisp-edges;
      image-rendering: crisp-edges;
    }
    .checkerboard {
      background-color: #1a1a24;
      background-image: 
        linear-gradient(45deg, #12121a 25%, transparent 25%), 
        linear-gradient(-45deg, #12121a 25%, transparent 25%), 
        linear-gradient(45deg, transparent 75%, #12121a 75%), 
        linear-gradient(-45deg, transparent 75%, #12121a 75%);
      background-size: 16px 16px;
      background-position: 0 0, 0 8px, 8px -8px, -8px 0px;
    }
  </style>
</head>
<body class="bg-transparent text-[var(--foreground)] antialiased p-4 font-sans select-none">
  <div class="max-w-4xl mx-auto bg-[var(--card)] border border-[var(--border)] rounded-2xl p-6 shadow-xl space-y-6">
    
    <!-- Header -->
    <div class="flex flex-wrap items-center justify-between gap-4 pb-4 border-b border-[var(--border)]">
      <div>
        <div class="flex items-center gap-2">
          <span class="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-pink-500/20 text-pink-400 border border-pink-500/30">Demon Slayer</span>
          <span class="px-2.5 py-0.5 rounded-full text-xs font-semibold bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">Transparent PNG Frames</span>
        </div>
        <h1 class="text-2xl font-bold mt-1 text-[var(--foreground)]">Nezuko Kamado — Fighter Sprite Viewer</h1>
        <p class="text-xs text-[var(--muted-foreground)]">Interactive animation loop player and high-definition pixel art inspection</p>
      </div>

      <!-- Form Selector Tabs -->
      <div class="flex bg-[var(--background)] p-1 rounded-xl border border-[var(--border)]">
        <button id="tab-base" onclick="selectForm('base')" class="px-4 py-2 rounded-lg text-xs font-bold transition-all bg-pink-600 text-white shadow-sm">
          🌸 Base & Smol Form
        </button>
        <button id="tab-demon" onclick="selectForm('demon')" class="px-4 py-2 rounded-lg text-xs font-bold transition-all text-[var(--muted-foreground)] hover:text-[var(--foreground)]">
          👹 Awakened Demon Form
        </button>
      </div>
    </div>

    <!-- Main Live Interactive Stage -->
    <div class="grid grid-cols-1 md:grid-cols-12 gap-6">
      
      <!-- Stage Viewport -->
      <div class="md:col-span-7 flex flex-col items-center justify-center checkerboard rounded-xl border border-[var(--border)] p-8 relative min-h-[340px] overflow-hidden">
        <div class="absolute top-3 left-3 flex items-center gap-2">
          <span id="form-badge" class="px-2 py-0.5 rounded text-[11px] font-bold bg-pink-900/60 text-pink-300 border border-pink-700/50">🌸 Base Form</span>
          <span id="anim-badge" class="px-2 py-0.5 rounded text-[11px] font-mono text-[var(--muted-foreground)] bg-black/40">idle</span>
        </div>

        <div class="absolute top-3 right-3 flex items-center gap-1.5 text-xs bg-black/50 px-2 py-1 rounded border border-white/10">
          <span class="text-[var(--muted-foreground)]">Zoom:</span>
          <button onclick="setZoom(2)" class="px-1.5 py-0.5 rounded hover:bg-white/20 font-mono text-xs">2x</button>
          <button onclick="setZoom(3)" class="px-1.5 py-0.5 rounded hover:bg-white/20 font-mono text-xs">3x</button>
          <button onclick="setZoom(4)" class="px-1.5 py-0.5 rounded bg-pink-600 text-white font-mono text-xs">4x</button>
          <button onclick="setZoom(6)" class="px-1.5 py-0.5 rounded hover:bg-white/20 font-mono text-xs">6x</button>
        </div>

        <!-- Animated Character Image -->
        <div class="my-auto flex items-end justify-center h-48">
          <img id="stage-img" src="" alt="Nezuko animation frame" class="pixelated transition-transform duration-75 origin-bottom" style="transform: scale(4);" />
        </div>

        <!-- Playback Controller Bar -->
        <div class="absolute bottom-3 inset-x-3 flex items-center justify-between bg-black/70 backdrop-blur px-3 py-2 rounded-lg border border-white/10 text-xs">
          <div class="flex items-center gap-2">
            <button id="play-btn" onclick="togglePlay()" class="px-2.5 py-1 rounded bg-pink-600 hover:bg-pink-500 font-bold text-white">Pause</button>
            <button onclick="stepFrame(-1)" class="px-2 py-1 rounded bg-white/10 hover:bg-white/20 font-mono">◀</button>
            <button onclick="stepFrame(1)" class="px-2 py-1 rounded bg-white/10 hover:bg-white/20 font-mono">▶</button>
            <span id="frame-counter" class="text-[var(--muted-foreground)] font-mono ml-2">Frame 1/3</span>
          </div>

          <div class="flex items-center gap-2">
            <span class="text-[var(--muted-foreground)]">Speed:</span>
            <select id="speed-select" onchange="changeSpeed(this.value)" class="bg-black/60 border border-white/20 rounded px-2 py-0.5 text-xs text-[var(--foreground)]">
              <option value="0.5">0.5x Slow</option>
              <option value="1" selected>1.0x Normal</option>
              <option value="1.5">1.5x Fast</option>
            </select>
          </div>
        </div>
      </div>

      <!-- Animation Selector Buttons -->
      <div class="md:col-span-5 flex flex-col justify-between space-y-4">
        <div>
          <h3 class="text-xs font-bold uppercase tracking-wider text-[var(--muted-foreground)] mb-3">Select Action / Pose</h3>
          <div class="grid grid-cols-2 gap-2 text-xs" id="anim-btn-container">
            <!-- dynamically inserted buttons -->
          </div>
        </div>

        <!-- Description Box -->
        <div id="action-desc" class="bg-[var(--background)] p-3 rounded-xl border border-[var(--border)] text-xs space-y-1">
          <div class="font-semibold text-pink-400" id="desc-title">Idle Pose</div>
          <p class="text-[var(--muted-foreground)] text-[12px] leading-relaxed" id="desc-text">
            Nezuko's classic breathing stance in her traditional kimono with bamboo muzzle.
          </p>
        </div>
      </div>
    </div>

    <!-- Frame Gallery Strip -->
    <div class="pt-4 border-t border-[var(--border)] space-y-3">
      <div class="flex items-center justify-between">
        <h3 class="text-xs font-bold uppercase tracking-wider text-[var(--muted-foreground)]">Animation Frames Breakdown</h3>
        <span class="text-xs text-[var(--muted-foreground)]" id="strip-info">Click any frame to jump to it</span>
      </div>
      <div id="frame-strip" class="flex items-center gap-3 overflow-x-auto p-3 bg-[var(--background)] rounded-xl border border-[var(--border)] min-h-[90px]">
        <!-- frames inserted here -->
      </div>
    </div>

  </div>

  <script>
    const animData = __DATA_PLACEHOLDER__;

    let currentForm = 'base';
    let currentAnim = 'idle';
    let currentFrameIdx = 0;
    let isPlaying = true;
    let animInterval = null;
    let currentFps = 6;
    let speedMult = 1.0;
    let zoomLevel = 4;

    const animMeta = {
      idle: { label: '🌸 Idle Breathing', fps: 5, desc: 'Classic fighting stance with bamboo muzzle and soft kimono breathing animation.' },
      walk: { label: '🏃 Smol Run / Walk', fps: 8, desc: 'Smol Nezuko running with arms thrown back, sprinting across the stage!' },
      punch: { label: '👊 Light Claw Slash', fps: 9.5, desc: 'Rapid sharp demon claw strikes with fierce forward reach.' },
      heavy: { label: '🦵 Heavy Axe Kick', fps: 7, desc: 'Heavy high-angle drop kick packing immense crushing kinetic impact.' },
      skill: { label: '🌀 Ricochet Flip', fps: 8, desc: 'Agile wall-spring ricochet flip and dive kick evasion technique.' },
      ultimate: { label: '🔥 Blood Demon Art', fps: 6, desc: 'Bakketsu! Hand ignition into devastating pink pyrokinesis explosion shockwaves!' },
      crouch: { label: '📦 Smol Crouch', fps: 1, desc: 'Iconic compact crouch pose, reducing hurtbox size to dodge high projectiles.' },
      jump: { label: '⬆ Jump Arc', fps: 6, desc: 'Mid-air leap and descending landing frames.' },
      meditate: { label: '✨ Ki Charge', fps: 6, desc: 'Concentrated demonic energy charging pose to restore spirit bar.' },
      hurt: { label: '💥 Hit Reaction', fps: 8, desc: 'Impact recoil when struck by enemy strikes.' },
      death: { label: '💀 Knockout', fps: 5, desc: 'Defeat and collapse animation sequence.' },
      victory: { label: '🏆 Victory Pose', fps: 6, desc: 'Celebratory round-win animation loop.' }
    };

    function init() {
      renderAnimButtons();
      setAnimation('idle');
      startPlayback();
    }

    function selectForm(form) {
      currentForm = form;
      document.getElementById('tab-base').className = form === 'base'
        ? 'px-4 py-2 rounded-lg text-xs font-bold transition-all bg-pink-600 text-white shadow-sm'
        : 'px-4 py-2 rounded-lg text-xs font-bold transition-all text-[var(--muted-foreground)] hover:text-[var(--foreground)]';
      document.getElementById('tab-demon').className = form === 'demon'
        ? 'px-4 py-2 rounded-lg text-xs font-bold transition-all bg-red-600 text-white shadow-sm'
        : 'px-4 py-2 rounded-lg text-xs font-bold transition-all text-[var(--muted-foreground)] hover:text-[var(--foreground)]';

      const badge = document.getElementById('form-badge');
      if (form === 'base') {
        badge.innerText = '🌸 Base Form';
        badge.className = 'px-2 py-0.5 rounded text-[11px] font-bold bg-pink-900/60 text-pink-300 border border-pink-700/50';
      } else {
        badge.innerText = '👹 Awakened Demon Form';
        badge.className = 'px-2 py-0.5 rounded text-[11px] font-bold bg-red-900/60 text-red-300 border border-red-700/50';
      }

      setAnimation(currentAnim);
    }

    function renderAnimButtons() {
      const container = document.getElementById('anim-btn-container');
      container.innerHTML = '';
      const anims = Object.keys(animMeta);
      anims.forEach(key => {
        const btn = document.createElement('button');
        btn.id = 'btn-anim-' + key;
        btn.className = 'p-2.5 rounded-lg text-left font-medium transition-all border border-white/5 bg-[var(--background)] hover:bg-white/5';
        btn.innerHTML = animMeta[key].label;
        btn.onclick = () => setAnimation(key);
        container.appendChild(btn);
      });
    }

    function setAnimation(animKey) {
      currentAnim = animKey;
      currentFrameIdx = 0;

      Object.keys(animMeta).forEach(k => {
        const b = document.getElementById('btn-anim-' + k);
        if (b) {
          if (k === animKey) {
            b.className = 'p-2.5 rounded-lg text-left font-bold transition-all border border-pink-500/50 bg-pink-500/10 text-pink-300 shadow-sm';
          } else {
            b.className = 'p-2.5 rounded-lg text-left font-medium transition-all border border-white/5 bg-[var(--background)] hover:bg-white/5 text-[var(--muted-foreground)]';
          }
        }
      });

      document.getElementById('anim-badge').innerText = animKey;
      document.getElementById('desc-title').innerText = animMeta[animKey].label;
      document.getElementById('desc-text').innerText = animMeta[animKey].desc;

      currentFps = animMeta[animKey].fps;
      renderFrameStrip();
      updateFrame();
      restartTimer();
    }

    function renderFrameStrip() {
      const strip = document.getElementById('frame-strip');
      strip.innerHTML = '';
      const frames = animData[currentForm][currentAnim] || [];
      frames.forEach((f, idx) => {
        const wrap = document.createElement('div');
        wrap.className = 'cursor-pointer p-2 rounded-lg border checkerboard transition-all ' + 
          (idx === currentFrameIdx ? 'border-pink-500 ring-2 ring-pink-500/40 bg-pink-900/20' : 'border-white/10 hover:border-white/30');
        wrap.onclick = () => {
          currentFrameIdx = idx;
          updateFrame();
        };
        const img = document.createElement('img');
        img.src = f.src;
        img.className = 'pixelated h-12 object-contain mx-auto';
        wrap.appendChild(img);
        const lbl = document.createElement('div');
        lbl.className = 'text-[10px] text-center font-mono text-[var(--muted-foreground)] mt-1';
        lbl.innerText = '#' + idx;
        wrap.appendChild(lbl);
        strip.appendChild(wrap);
      });
    }

    function updateFrame() {
      const frames = animData[currentForm][currentAnim] || [];
      if (frames.length === 0) return;
      if (currentFrameIdx >= frames.length) currentFrameIdx = 0;

      const img = document.getElementById('stage-img');
      img.src = frames[currentFrameIdx].src;
      document.getElementById('frame-counter').innerText = 'Frame ' + (currentFrameIdx + 1) + '/' + frames.length;

      const strip = document.getElementById('frame-strip');
      Array.from(strip.children).forEach((el, idx) => {
        if (idx === currentFrameIdx) {
          el.className = 'cursor-pointer p-2 rounded-lg border checkerboard transition-all border-pink-500 ring-2 ring-pink-500/40 bg-pink-900/20';
        } else {
          el.className = 'cursor-pointer p-2 rounded-lg border checkerboard transition-all border-white/10 hover:border-white/30';
        }
      });
    }

    function startPlayback() {
      if (animInterval) clearInterval(animInterval);
      const delay = (1000 / (currentFps * speedMult));
      animInterval = setInterval(() => {
        const frames = animData[currentForm][currentAnim] || [];
        if (frames.length <= 1) return;
        currentFrameIdx = (currentFrameIdx + 1) % frames.length;
        updateFrame();
      }, delay);
    }

    function restartTimer() {
      if (isPlaying) {
        startPlayback();
      }
    }

    function togglePlay() {
      isPlaying = !isPlaying;
      const btn = document.getElementById('play-btn');
      if (isPlaying) {
        btn.innerText = 'Pause';
        btn.className = 'px-2.5 py-1 rounded bg-pink-600 hover:bg-pink-500 font-bold text-white';
        startPlayback();
      } else {
        btn.innerText = 'Play';
        btn.className = 'px-2.5 py-1 rounded bg-emerald-600 hover:bg-emerald-500 font-bold text-white';
        if (animInterval) clearInterval(animInterval);
      }
    }

    function stepFrame(dir) {
      if (isPlaying) togglePlay();
      const frames = animData[currentForm][currentAnim] || [];
      if (frames.length === 0) return;
      currentFrameIdx = (currentFrameIdx + dir + frames.length) % frames.length;
      updateFrame();
    }

    function changeSpeed(val) {
      speedMult = parseFloat(val);
      restartTimer();
    }

    function setZoom(lvl) {
      zoomLevel = lvl;
      document.getElementById('stage-img').style.transform = 'scale(' + lvl + ')';
    }

    window.onload = init;
  </script>
</body>
</html>
'''

full_html = template.replace('__DATA_PLACEHOLDER__', data_json)
out_path = os.path.join(artifact_dir, 'nezuko_viewer.html')
with open(out_path, 'w', encoding='utf-8') as f:
    f.write(full_html)

print('Generated nezuko_viewer.html at:', out_path)
