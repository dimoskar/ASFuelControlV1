const path = require('path');
const fs = require('fs');
const { chromium } = require('playwright');
const sharp = require('sharp');

(async () => {
  const root = __dirname;
  const html = path.join(root, 'mockups.html');
  const files = [
    '01-settings-mydiesel.png',
    '02-invoice-coupon-capture.png',
    '03-invoice-reserved-low-balance.png',
    '04-issued-redeem-unknown.png',
    '05-verification-invalid.png',
    '06-catalog-intervention.png',
    '07-history-evidence.png',
    '08-correct-mark-dialog.png',
    '09-change-station-dialog.png',
    '10-close-release-flow.png'
  ];
  const browser = await chromium.launch({
    headless: true,
    executablePath: 'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe'
  });
  const page = await browser.newPage({ viewport: { width: 1248, height: 828 }, deviceScaleFactor: 1 });
  for (const file of files) {
    await page.goto('file:///' + html.replace(/\\/g, '/') + '?screen=' + encodeURIComponent(file));
    await page.locator('.screen.active').screenshot({ path: path.join(root, file) });
  }
  await browser.close();
  const thumbWidth = 560;
  const thumbHeight = 364;
  const gutter = 20;
  const contactWidth = thumbWidth * 2 + gutter * 3;
  const contactHeight = thumbHeight * 5 + gutter * 6;
  const tiles = [];
  for (let index = 0; index < files.length; index++) {
    const input = await sharp(path.join(root, files[index])).resize(thumbWidth, thumbHeight).png().toBuffer();
    tiles.push({
      input,
      left: gutter + (index % 2) * (thumbWidth + gutter),
      top: gutter + Math.floor(index / 2) * (thumbHeight + gutter)
    });
  }
  await sharp({ create: { width: contactWidth, height: contactHeight, channels: 3, background: '#eeeeee' } })
    .composite(tiles)
    .png()
    .toFile(path.join(root, '00-mydiesel-ui-overview.png'));
  console.log(`Rendered ${files.length} myDiesel UI mockups and overview to ${root}`);
})();
