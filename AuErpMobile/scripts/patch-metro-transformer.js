/**
 * Metro 0.80 uses String.replaceAll in Transformer.js. On some Windows setups
 * localPath is not a normal string, or Node < 15 lacks replaceAll — both cause
 * "localPath.replaceAll is not a function". Use split/join style replace instead.
 */
const fs = require('fs');
const path = require('path');

const targets = [
  'node_modules/metro/src/DeltaBundler/Transformer.js',
  'node_modules/metro/src/Assets.js',
  'node_modules/metro/src/lib/contextModuleTemplates.js',
];

const replacements = [
  {
    from: 'path.sep === "/" ? localPath : localPath.replaceAll(path.sep, "/")',
    to: 'path.sep === "/" ? String(localPath) : String(localPath).replace(/\\\\/g, "/")',
  },
  {
    from: 'assetUrlPath = assetUrlPath.replaceAll("\\\\", "/")',
    to: 'assetUrlPath = String(assetUrlPath).replace(/\\\\/g, "/")',
  },
  {
    from: 'filePath = filePath.replaceAll("\\\\", "/")',
    to: 'filePath = String(filePath).replace(/\\\\/g, "/")',
  },
];

let changed = 0;
for (const rel of targets) {
  const file = path.join(__dirname, '..', rel);
  if (!fs.existsSync(file)) continue;
  let src = fs.readFileSync(file, 'utf8');
  let fileChanged = false;
  for (const {from, to} of replacements) {
    if (src.includes(from)) {
      src = src.replace(from, to);
      fileChanged = true;
    }
  }
  if (fileChanged) {
    fs.writeFileSync(file, src);
    changed++;
  }
}

if (changed > 0) {
  console.log(`patch-metro-transformer: updated ${changed} file(s)`);
}
