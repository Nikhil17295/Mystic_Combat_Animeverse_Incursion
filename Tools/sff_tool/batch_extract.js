const fs = require('fs');
const path = require('path');
const { PNG } = require('./node_modules/pngjs');
const extract = require('./node_modules/sff-extractor').default;

const ROOT = path.resolve(__dirname, '../..');
const BASE_SFF = path.join(ROOT, 'Assets/Sprites/Fighters/Nezuko/Nezuko/Nezuko.sff');
const DEMON_SFF = path.join(ROOT, 'Assets/Sprites/Fighters/Nezuko/Nezuko Advanced Demon Form/sff.sff');

const OUT_DIR = path.join(ROOT, 'Assets/Sprites/Fighters/Nezuko/Preview_Sprites');

function extractFile(sffPath, outSubDir, namePrefix) {
    console.log(`Processing: ${sffPath}`);
    const buf = fs.readFileSync(sffPath);
    const targetDir = path.join(OUT_DIR, outSubDir);
    fs.mkdirSync(targetDir, { recursive: true });

    // Extract all sprites with decoding
    const data = extract(buf, {
        palettes: true,
        decodeSpriteBuffer: true
    });

    console.log(`  Found ${data.sprites.length} sprites in ${namePrefix}.`);

    let savedCount = 0;
    const groupCounts = {};

    for (const spr of data.sprites) {
        if (!spr.decodedBuffer || spr.width <= 0 || spr.height <= 0) continue;

        const g = spr.group;
        const n = spr.number !== undefined ? spr.number : 0;
        groupCounts[g] = (groupCounts[g] || 0) + 1;

        // Save PNG
        try {
            const png = new PNG({ width: spr.width, height: spr.height });
            png.data = spr.decodedBuffer;
            const filename = `${namePrefix}_g${g}_i${n}.png`;
            fs.writeFileSync(path.join(targetDir, filename), PNG.sync.write(png));
            savedCount++;
        } catch (e) {
            // Ignore corrupted or invalid individual frames
        }
    }

    console.log(`  Successfully exported ${savedCount} PNG sprites to ${targetDir}`);
    return { targetDir, groupCounts, savedCount };
}

console.log('--- Starting MUGEN SFF Extraction to PNG ---');
const baseRes = extractFile(BASE_SFF, 'Base', 'nezuko_base');
const demonRes = extractFile(DEMON_SFF, 'Demon', 'nezuko_demon');
console.log('--- Extraction Complete ---');
