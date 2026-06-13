const { chromium } = require('C:/Users/machi/AppData/Roaming/npm/node_modules/@playwright/cli/node_modules/playwright');

(async () => {
  const browser = await chromium.launch({ channel: 'msedge' });
  const page = await browser.newPage({ viewport: { width: 1400, height: 900 } });
  const errors = [];
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
  page.on('pageerror', e => errors.push(String(e)));

  await page.goto('http://localhost:5210/', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500); // Blazor circuit connect
  await page.screenshot({ path: '.verify/01-initial.png' });

  // サンプル図を読み込み
  await page.click('button:has-text("サンプル")');
  await page.waitForTimeout(800);
  await page.screenshot({ path: '.verify/02-sample.png' });

  // パレットの Lambda をクリックで中央配置
  await page.click('.wd-item:has-text("Lambda")');
  await page.waitForTimeout(500);

  // 配置直後は選択中。ノードを 100,60 ドラッグして移動（スナップ確認）
  const node = page.locator('g.wd-node').last();
  const box = await node.boundingBox();
  if (box) {
    const cx = box.x + box.width / 2, cy = box.y + box.height / 2;
    await page.mouse.move(cx, cy);
    await page.mouse.down();
    for (let i = 1; i <= 8; i++) {
      await page.mouse.move(cx + (103 * i) / 8, cy + (57 * i) / 8);
      await page.waitForTimeout(40);
    }
    await page.mouse.up();
  }
  await page.waitForTimeout(500);
  await page.screenshot({ path: '.verify/03-after-drag.png' });

  const counts = await page.evaluate(() => ({
    nodes: document.querySelectorAll('g.wd-node').length,
    edges: document.querySelectorAll('g.wd-edge').length,
  }));
  console.log('nodes:', counts.nodes, 'edges:', counts.edges);
  console.log('console errors:', errors.length ? JSON.stringify(errors, null, 2) : 'none');
  await browser.close();
})();
