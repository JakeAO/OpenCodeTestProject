// Simple bundler for Nakama JavaScript runtime
// Combines all files into a single file without CommonJS exports/requires

const fs = require('fs');
const path = require('path');

const buildDir = path.join(__dirname, '..', 'build');
const modulesDir = path.join(__dirname, '..', 'modules');

// Read all files in dependency order
const files = [
  'lib/types.js',
  'lib/utils.js',
  'rpc/analytics.js',
  'rpc/config.js',
  'rpc/experiments.js',
  'rpc/health.js',
  'index.js'
];

let bundle = '';

// Helper to remove CommonJS exports/require and fix references
function stripCommonJS(content) {
  return content
    .split('\n')
    .filter(line => {
      // Remove CommonJS boilerplate
      return !line.includes('"use strict"') &&
             !line.includes('Object.defineProperty(exports') &&
             !line.includes('exports.') &&
             !line.match(/^var .+ = require\(/);
    })
    .map(line => {
      // Fix module references: analytics_1.analyticsCollectEvents -> analyticsCollectEvents
      let fixedLine = line
        .replace(/analytics_1\./g, '')
        .replace(/config_1\./g, '')
        .replace(/experiments_1\./g, '')
        .replace(/health_1\./g, '')
        .replace(/utils_1\./g, '');
      
      // Fix CommonJS function calls: (0, functionName) -> functionName
      fixedLine = fixedLine.replace(/\(0, ([a-zA-Z_][a-zA-Z0-9_]*)\)/g, '$1');
      
      return fixedLine;
    })
    .join('\n');
}

// Process each file
for (const file of files) {
  const filePath = path.join(buildDir, file);
  if (fs.existsSync(filePath)) {
    console.log(`Processing: ${file}`);
    let content = fs.readFileSync(filePath, 'utf8');
    content = stripCommonJS(content);
    bundle += `\n// ===== ${file} =====\n`;
    bundle += content + '\n';
  } else {
    console.warn(`Warning: File not found: ${file}`);
  }
}

// Write bundled output
const outputPath = path.join(modulesDir, 'index.js');
fs.writeFileSync(outputPath, bundle);
console.log(`\nBundle created: ${outputPath}`);
console.log(`Bundle size: ${bundle.length} bytes`);
