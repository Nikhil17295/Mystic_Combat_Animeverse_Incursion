const fs = require('fs');
const html = fs.readFileSync('Tools/SpriteSlicer/index.html', 'utf8');

const elementMap = {};
function getEl(id) {
    if (!elementMap[id]) {
        elementMap[id] = {
            id,
            classList: { toggle: (cls, on) => console.log('toggle', id, cls, on) },
            style: {},
            appendChild: (c) => console.log('appendChild on', id),
            innerHTML: '',
            value: '0',
            textContent: ''
        };
    }
    return elementMap[id];
}

global.document = {
    getElementById: getEl,
    createElement: (tag) => ({ tag, style: {}, classList: { add: () => {} }, appendChild: () => {} })
};
global.window = { location: { protocol: 'http:' } };
global.Image = class {
    constructor() { this.naturalWidth = 980; this.naturalHeight = 2447; this.complete = true; }
    set src(v) {
        console.log("Image src set to:", v);
        if (this.onload) this.onload();
    }
};
global.canvas = { width: 800, height: 600, getContext: () => ({ clearRect: () => {}, save: () => {}, translate: () => {}, scale: () => {}, drawImage: () => {}, restore: () => {} }) };
global.viewport = { addEventListener: () => {}, clientWidth: 800, clientHeight: 600 };
global.dropOverlay = { style: {} };
global.coordBadge = { textContent: "" };
global.zoomLevel = { textContent: "" };
global.imageInfoBadge = { textContent: "", style: {} };
global.previewCanvas = { width: 128, height: 128, getContext: () => ({ clearRect: () => {}, save: () => {}, drawImage: () => {}, restore: () => {} }) };
global.fpsVal = { textContent: "" };
global.fpsSlider = { value: 8 };
global.requestAnimationFrame = () => {};

const scriptMatch = html.match(/<script>([\s\S]*?)<\/script>/);
try {
    eval(scriptMatch[1]);
    console.log("Successfully ran index.html JS! currentCharacter =", currentCharacter);
} catch (e) {
    console.error("Error executing JS:", e);
}
