const fs = require('fs');
const { execSync } = require('child_process');
const path = require('path');

const generateVersionInfo = () => {
  try {
    // Get the latest git tag (navigate to root if needed)
    const gitTag = execSync('git describe --tags --abbrev=0', { cwd: path.join(__dirname, '../..') }).toString().trim();
    const buildDate = new Date().toISOString();
    
    // Create TypeScript content
    const content = `// This file is auto-generated. Do not edit manually
export const VERSION_INFO = {
  version: '${gitTag}',
  buildDate: '${buildDate}',
  formattedBuildDate: '${new Date(buildDate).toLocaleString()}'
} as const;
`;

    // Write to src/app/version.generated.ts
    fs.writeFileSync(path.join(__dirname, 'src/app/version.generated.ts'), content);
    console.log('✓ Version information generated:', gitTag);

  } catch (error) {
    console.error('Error generating version info:', error);
    process.exit(1);
  }
};

generateVersionInfo();