const { chromium } = require('C:/Users/machi/AppData/Roaming/npm/node_modules/@playwright/cli/node_modules/playwright');

(async () => {
  const browser = await chromium.launch({ channel: 'msedge' });
  const page = await browser.newPage({ viewport: { width: 1400, height: 900 } });
  const errors = [];
  page.on('console', m => { if (m.type() === 'error') errors.push(m.text()); });
  page.on('pageerror', e => errors.push(String(e)));

  await page.goto('http://localhost:5210/', { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500);

  // --- HTML5 ドラッグ＆ドロップでパレットから EC2 を配置 ---
  const tile = page.locator('.wd-item:has-text("EC2") .wd-item-tile').first();
  await tile.dragTo(page.locator('svg.wd-svg'), { targetPosition: { x: 400, y: 300 } });
  await page.waitForTimeout(600);
  let count = await page.locator('g.wd-node').count();
  console.log('after dnd, nodes =', count, '(expect 1)');
  await page.screenshot({ path: '.verify/04-dnd.png' });

  // --- 2個目を配置（S3 をクリック配置）して接続テスト ---
  await page.click('.wd-item:has-text("S3")');
  await page.waitForTimeout(500);

  // EC2 ノードにホバー → 右ポートからドラッグして S3 ノード上でドロップ
  const ec2 = page.locator('g.wd-node:has-text("EC2")').first();
  const s3 = page.locator('g.wd-node:has-text("S3")').first();
  const eb = await ec2.boundingBox();
  const sb = await s3.boundingBox();
  // ラベル分下に膨らむので、アイコン部分の中心は box の上側
  const ecx = eb.x + eb.width / 2, ecy = eb.y + (eb.height - 18) / 2;
  await page.mouse.move(ecx, ecy);           // ホバーでポート表示
  await page.waitForTimeout(400);
  const port = page.locator('.wd-port').nth(1); // 右ポート
  const pb = await port.boundingBox();
  await page.mouse.move(pb.x + pb.width / 2, pb.y + pb.height / 2);
  await page.mouse.down();
  const scx = sb.x + sb.width / 2, scy = sb.y + (sb.height - 18) / 2;
  for (let i = 1; i <= 6; i++) {
    await page.mouse.move(pb.x + (scx - pb.x) * i / 6, pb.y + (scy - pb.y) * i / 6);
    await page.waitForTimeout(50);
  }
  // 計測: マウスを離す直前に仮線と接続先ハイライトが出ているか
  const dbg = await page.evaluate(() => ({
    temp: document.querySelectorAll('#scene > path[data-noexport]').length,
    target: document.querySelectorAll('g.wd-node rect[stroke="#2196F3"][stroke-width="3"]').length,
  }));
  console.log('before up: temp line =', dbg.temp, ', target highlight =', dbg.target);
  await page.mouse.up();
  await page.waitForTimeout(500);
  const edgeCount = await page.locator('g.wd-edge').count();
  console.log('after connect, edges =', edgeCount, '(expect 1)');
  await page.screenshot({ path: '.verify/05-connected.png' });

  // --- ダブルクリックでラベル編集 ---
  await ec2.dblclick({ position: { x: eb.width / 2, y: (eb.height - 18) / 2 } });
  await page.waitForTimeout(400);
  const hasInput = await page.locator('input.wd-label-edit').count();
  console.log('label editor visible =', hasInput, '(expect 1)');
  if (hasInput) {
    await page.fill('input.wd-label-edit', 'Web サーバー');
    await page.keyboard.press('Enter');
    await page.waitForTimeout(400);
  }

  // --- Delete キーで選択削除（S3 を選択して削除）---
  await s3.click({ position: { x: sb.width / 2, y: (sb.height - 18) / 2 } });
  await page.waitForTimeout(300);
  await page.keyboard.press('Delete');
  await page.waitForTimeout(400);
  count = await page.locator('g.wd-node').count();
  const edges2 = await page.locator('g.wd-edge').count();
  console.log('after delete, nodes =', count, '(expect 1), edges =', edges2, '(expect 0)');

  // --- Ctrl+Z で元に戻す ---
  await page.keyboard.press('Control+z');
  await page.waitForTimeout(400);
  count = await page.locator('g.wd-node').count();
  console.log('after undo, nodes =', count, '(expect 2)');
  await page.screenshot({ path: '.verify/06-final.png' });

  console.log('console errors:', errors.length ? JSON.stringify(errors, null, 2) : 'none');
  await browser.close();
})();
