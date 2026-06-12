const fs = require('fs');
const path = require('path');

const paths = [
    "apps/desktop/src/app",
    "apps/desktop/src/pages",
    "apps/desktop/src/widgets",
    "apps/desktop/src/features",
    "apps/desktop/src/entities",
    "apps/desktop/src/shared"
];

const patterns = [
    /bg-\[#/,
    /text-\[#/,
    /border-\[#/,
    /style=\{\{[^}]*color/,
    /style=\{\{[^}]*background/,
    /rgb\(/,
    /rgba\(/,
    /#[0-9a-fA-F]{3,8}/
];

function getFiles(dir, fileList = []) {
    if (!fs.existsSync(dir)) return fileList;
    const files = fs.readdirSync(dir);
    for (const file of files) {
        const filePath = path.join(dir, file);
        const stat = fs.statSync(filePath);
        if (stat.isDirectory()) {
            getFiles(filePath, fileList);
        } else if (file.endsWith('.ts') || file.endsWith('.tsx')) {
            fileList.push(filePath);
        }
    }
    return fileList;
}

let violations = [];

const projectRoot = path.resolve(__dirname, '../../../');

for (const p of paths) {
    const fullPath = path.resolve(projectRoot, p);
    const files = getFiles(fullPath);
    for (const file of files) {
        const content = fs.readFileSync(file, 'utf8');
        const lines = content.split(/\r?\n/);
        for (let i = 0; i < lines.length; i++) {
            const line = lines[i];
            for (const pattern of patterns) {
                if (pattern.test(line)) {
                    violations.push({
                        Path: path.relative(projectRoot, file),
                        Line: i + 1,
                        Text: line.trim()
                    });
                    break;
                }
            }
        }
    }
}

if (violations.length > 0) {
    console.table(violations);
    console.error("\x1b[31mError: Hardcoded frontend colors are not allowed. Use semantic theme tokens.\x1b[0m");
    process.exit(1);
}

console.log("\x1b[32mNo hardcoded frontend colors found.\x1b[0m");
