/* ============================================================
   미사일 키우기 : 궤도 방어전
   - 소환 / 합성으로 미사일 등급을 키우고
   - 밀려오는 적을 타워 디펜스처럼 막아내는 게임
   의존성 없는 순수 자바스크립트. 모든 좌표는 540x960 논리 해상도 기준.
   ============================================================ */
(() => {
  'use strict';

  const W = 540, H = 960;
  const FIELD_BOTTOM = 500;

  /* ---------- 미사일 등급 테이블 ---------- */
  const TIERS = [
    { name: '폭죽탄',      dmg: 6,     cd: 1.10, splash: 0,   speed: 330, color: '#94a3b8' },
    { name: '소이탄',      dmg: 14,    cd: 1.06, splash: 0,   speed: 340, color: '#a3e635' },
    { name: '파쇄탄',      dmg: 33,    cd: 1.02, splash: 26,  speed: 350, color: '#38bdf8' },
    { name: '고폭탄',      dmg: 78,    cd: 0.97, splash: 34,  speed: 365, color: '#818cf8' },
    { name: '유도탄',      dmg: 182,   cd: 0.92, splash: 40,  speed: 385, color: '#c084fc' },
    { name: '확산탄',      dmg: 425,   cd: 0.86, splash: 50,  speed: 400, color: '#f472b6' },
    { name: '열압력탄',    dmg: 995,   cd: 0.80, splash: 62,  speed: 415, color: '#fb923c' },
    { name: 'EMP 미사일',  dmg: 2320,  cd: 0.74, splash: 74,  speed: 440, color: '#22d3ee', slow: true },
    { name: '플라즈마 탄두', dmg: 5400, cd: 0.68, splash: 88,  speed: 470, color: '#facc15' },
    { name: '궤도 강습포',  dmg: 12600, cd: 0.60, splash: 112, speed: 520, color: '#ef4444' },
  ];
  const MAX_TIER = TIERS.length;

  /* ---------- 진격 경로 ---------- */
  const PATH = [
    { x: -40, y: 70 }, { x: 380, y: 70 }, { x: 452, y: 132 }, { x: 452, y: 202 },
    { x: 392, y: 258 }, { x: 132, y: 258 }, { x: 70, y: 318 }, { x: 70, y: 388 },
    { x: 132, y: 444 }, { x: 428, y: 444 },
  ];
  const SEG = [];
  let PATH_LEN = 0;
  for (let i = 0; i < PATH.length - 1; i++) {
    const a = PATH[i], b = PATH[i + 1];
    const len = Math.hypot(b.x - a.x, b.y - a.y);
    SEG.push({ a, b, len, start: PATH_LEN });
    PATH_LEN += len;
  }
  const BASE = { x: 470, y: 444 };

  function pointAt(d) {
    if (d <= 0) return { x: PATH[0].x, y: PATH[0].y };
    if (d >= PATH_LEN) return { x: BASE.x, y: BASE.y };
    for (let i = 0; i < SEG.length; i++) {
      const s = SEG[i];
      if (d <= s.start + s.len) {
        const t = (d - s.start) / s.len;
        return { x: s.a.x + (s.b.x - s.a.x) * t, y: s.a.y + (s.b.y - s.a.y) * t };
      }
    }
    return { x: BASE.x, y: BASE.y };
  }

  /* ---------- 발사대 격자 ---------- */
  const COLS = 4, ROWS = 3, SLOTS = COLS * ROWS;
  const CELL_W = 118.5, CELL_H = 132, GAP = 10, GRID_X = 18, GRID_Y = 516;
  const cells = [];
  for (let r = 0; r < ROWS; r++) {
    for (let c = 0; c < COLS; c++) {
      const x = GRID_X + c * (CELL_W + GAP);
      const y = GRID_Y + r * (CELL_H + GAP);
      cells.push({ x, y, w: CELL_W, h: CELL_H, cx: x + CELL_W / 2, cy: y + CELL_H / 2 });
    }
  }

  /* ---------- 상태 ---------- */
  let S = null;

  function newGame() {
    return {
      gold: 60,
      baseHp: 20, baseHpMax: 20,
      wave: 0,
      slotsUnlocked: 6,
      launchers: new Array(SLOTS).fill(null), // { tier, cool, recoil }
      enemies: [], shots: [], fx: [], texts: [],
      spawnQueue: [], spawnTimer: 0,
      waveState: 'ready', // ready | spawning | clearing | over
      waveTimer: 1.2,
      summonCount: 0,
      up: { power: 0, rate: 0, gold: 0, grade: 0, repair: 0 },
      skillCd: 0,
      autoMerge: false, autoSummon: false,
      priority: 'first', // first | strong | close
      speed: 1,
      elapsed: 0,
      killCount: 0,
      over: false,
    };
  }

  /* ---------- 파생 수치 ---------- */
  const powerMult = () => Math.pow(1.08, S.up.power);
  const rateMult = () => Math.max(0.22, Math.pow(0.955, S.up.rate));
  const goldMult = () => Math.pow(1.10, S.up.gold);

  const summonCost = () => Math.floor(25 * Math.pow(1.065, S.summonCount));
  const costPower = () => Math.floor(45 * Math.pow(1.32, S.up.power));
  const costRate = () => Math.floor(70 * Math.pow(1.36, S.up.rate));
  const costGold = () => Math.floor(55 * Math.pow(1.30, S.up.gold));
  const costGrade = () => Math.floor(150 * Math.pow(1.55, S.up.grade));
  const costRepair = () => Math.floor(120 * Math.pow(1.60, S.up.repair));
  const costSlot = () => Math.floor(220 * Math.pow(2.4, S.slotsUnlocked - 6));

  function launcherDmg(tier) { return TIERS[tier - 1].dmg * powerMult(); }
  function totalDps() {
    let t = 0;
    for (const l of S.launchers) {
      if (!l) continue;
      const T = TIERS[l.tier - 1];
      t += (T.dmg * powerMult()) / (T.cd * rateMult());
    }
    return t;
  }

  /* ---------- 숫자 표기 ---------- */
  const UNITS = ['', 'K', 'M', 'B', 'T', 'aa', 'ab', 'ac', 'ad'];
  function fmt(n) {
    if (!isFinite(n)) return '∞';
    if (n < 1000) return (n < 10 && n % 1 !== 0) ? n.toFixed(1) : String(Math.floor(n));
    let i = 0;
    while (n >= 1000 && i < UNITS.length - 1) { n /= 1000; i++; }
    return (n >= 100 ? n.toFixed(0) : n.toFixed(2)) + UNITS[i];
  }

  /* ---------- 웨이브 ---------- */
  function enemyHp(wave) { return 30 * Math.pow(1.235, wave - 1); }
  function enemyGold(wave) { return 4 * Math.pow(1.155, wave - 1); }

  function startWave() {
    S.wave++;
    const w = S.wave;
    const boss = w % 10 === 0;
    const q = [];
    if (boss) {
      q.push({ kind: 'boss', hp: enemyHp(w) * 14, gold: enemyGold(w) * 18, speed: 28, r: 24 });
      const minions = 6 + Math.floor(w / 5);
      for (let i = 0; i < minions; i++) q.push(makeMinion(w, i));
    } else {
      const count = Math.min(24, 8 + Math.floor(w * 0.7));
      for (let i = 0; i < count; i++) q.push(makeMinion(w, i));
    }
    S.spawnQueue = q;
    S.spawnTimer = 0;
    S.waveState = 'spawning';
    toast(`WAVE ${w}` + (boss ? ' — 보스 접근' : ''), boss ? '#f87171' : '#7dd3fc');
  }

  function makeMinion(w, i) {
    const roll = Math.random();
    if (w >= 6 && roll < 0.25) {
      return { kind: 'armor', hp: enemyHp(w) * 2.4, gold: enemyGold(w) * 2, speed: 34, r: 15 };
    }
    if (w >= 3 && roll < 0.5) {
      return { kind: 'fast', hp: enemyHp(w) * 0.6, gold: enemyGold(w) * 0.9, speed: 82, r: 10 };
    }
    return { kind: 'normal', hp: enemyHp(w), gold: enemyGold(w), speed: 50, r: 12 };
  }

  /* ---------- 액션 ---------- */
  function firstEmpty() {
    for (let i = 0; i < S.slotsUnlocked; i++) if (!S.launchers[i]) return i;
    return -1;
  }

  function summon(silent) {
    const cost = summonCost();
    if (S.gold < cost) { if (!silent) toast('골드가 부족합니다', '#fca5a5'); return false; }
    const slot = firstEmpty();
    if (slot < 0) { if (!silent) toast('빈 발사대가 없습니다', '#fca5a5'); return false; }
    S.gold -= cost;
    S.summonCount++;
    let tier = 1;
    const g = S.up.grade;
    const r = Math.random();
    if (r < Math.min(0.22, g * 0.012)) tier = 3;
    else if (r < Math.min(0.55, g * 0.035)) tier = 2;
    tier = Math.min(tier, MAX_TIER);
    S.launchers[slot] = { tier, cool: Math.random() * 0.3, recoil: 0, pop: 0.35 };
    return true;
  }

  function mergeInto(from, to) {
    const a = S.launchers[from], b = S.launchers[to];
    if (!a || !b || a.tier !== b.tier || a.tier >= MAX_TIER) return false;
    S.launchers[to] = { tier: a.tier + 1, cool: 0, recoil: 0, pop: 0.5 };
    S.launchers[from] = null;
    burst(cells[to].cx, cells[to].cy, TIERS[a.tier].color, 16);
    toast(`${TIERS[a.tier].name} 합성!`, TIERS[a.tier].color);
    return true;
  }

  function autoMergeStep() {
    for (let t = 1; t < MAX_TIER; t++) {
      const idx = [];
      for (let i = 0; i < S.slotsUnlocked; i++) {
        if (S.launchers[i] && S.launchers[i].tier === t) idx.push(i);
        if (idx.length === 2) break;
      }
      if (idx.length === 2) { mergeInto(idx[0], idx[1]); return; }
    }
  }

  function useSkill() {
    if (S.skillCd > 0 || S.over) return;
    // 전 화면 타격이라 적 수만큼 곱해져 들어간다. 배율을 낮게 잡아야
    // 상시 DPS 를 대체하지 않고 위기 탈출용 버튼으로 남는다.
    const dmg = Math.max(50, totalDps() * 3.5);
    S.skillCd = 55;
    for (const e of S.enemies) { e.hp -= dmg; burst(e.x, e.y, '#fca5a5', 8); }
    for (let i = 0; i < 26; i++) {
      S.fx.push({ x: Math.random() * W, y: Math.random() * FIELD_BOTTOM, vx: 0, vy: 0, life: 0.5 + Math.random() * 0.4, max: 0.9, r: 20 + Math.random() * 30, color: '#fbbf24' });
    }
    toast('궤도 폭격!', '#fbbf24');
  }

  function buy(kind) {
    const table = {
      power: [costPower, () => S.up.power++],
      rate: [costRate, () => S.up.rate++],
      gold: [costGold, () => S.up.gold++],
      grade: [costGrade, () => S.up.grade++],
      repair: [costRepair, () => { S.up.repair++; S.baseHpMax += 2; S.baseHp = Math.min(S.baseHpMax, S.baseHp + 5); }],
      slot: [costSlot, () => S.slotsUnlocked++],
    };
    const entry = table[kind];
    if (!entry) return;
    if (kind === 'slot' && S.slotsUnlocked >= SLOTS) { toast('모든 발사대를 개방했습니다', '#fca5a5'); return; }
    const c = entry[0]();
    if (S.gold < c) { toast('골드가 부족합니다', '#fca5a5'); return; }
    S.gold -= c;
    entry[1]();
  }

  /* ---------- 이펙트 ---------- */
  function burst(x, y, color, n) {
    for (let i = 0; i < n; i++) {
      const a = Math.random() * Math.PI * 2, sp = 40 + Math.random() * 150;
      S.fx.push({ x, y, vx: Math.cos(a) * sp, vy: Math.sin(a) * sp, life: 0.35 + Math.random() * 0.3, max: 0.65, r: 2 + Math.random() * 3, color });
    }
  }
  function toast(text, color) {
    S.texts.push({ text, color: color || '#e2e8f0', life: 1.6, max: 1.6 });
    if (S.texts.length > 4) S.texts.shift();
  }

  /* ---------- 조준 ---------- */
  function pickTarget(lx, ly) {
    const list = S.enemies;
    if (!list.length) return null;
    let best = null, bestScore = -Infinity;
    for (const e of list) {
      if (e.hp <= 0) continue;
      let score;
      if (S.priority === 'strong') score = e.hp;
      else if (S.priority === 'close') score = -Math.hypot(e.x - lx, e.y - ly);
      else score = e.d;
      if (score > bestScore) { bestScore = score; best = e; }
    }
    return best;
  }

  /* ---------- 업데이트 ---------- */
  function update(dt) {
    if (S.over) { updateFx(dt); return; }
    S.elapsed += dt;
    if (S.skillCd > 0) S.skillCd = Math.max(0, S.skillCd - dt);

    // 웨이브 진행
    if (S.waveState === 'ready') {
      S.waveTimer -= dt;
      if (S.waveTimer <= 0) startWave();
    } else if (S.waveState === 'spawning') {
      S.spawnTimer -= dt;
      if (S.spawnTimer <= 0 && S.spawnQueue.length) {
        const cfg = S.spawnQueue.shift();
        S.enemies.push({
          kind: cfg.kind, hp: cfg.hp, hpMax: cfg.hp, gold: cfg.gold,
          speed: cfg.speed, r: cfg.r, d: 0, x: PATH[0].x, y: PATH[0].y, slowT: 0, hit: 0,
        });
        S.spawnTimer = cfg.kind === 'boss' ? 1.4 : 0.55;
      }
      if (!S.spawnQueue.length && !S.enemies.length) {
        const bonus = 22 * Math.pow(1.17, S.wave) * goldMult();
        S.gold += bonus;
        toast(`웨이브 클리어 +${fmt(bonus)}G`, '#86efac');
        S.waveState = 'ready';
        S.waveTimer = 1.6;
      }
    }

    // 자동화
    if (S.autoSummon) { for (let i = 0; i < 3; i++) if (!summon(true)) break; }
    if (S.autoMerge) autoMergeStep();

    // 적 이동
    for (const e of S.enemies) {
      const mul = e.slowT > 0 ? 0.55 : 1;
      if (e.slowT > 0) e.slowT -= dt;
      if (e.hit > 0) e.hit -= dt;
      e.d += e.speed * mul * dt;
      const p = pointAt(e.d);
      e.x = p.x; e.y = p.y;
      if (e.d >= PATH_LEN) {
        e.hp = 0; e.leaked = true;
        S.baseHp -= (e.kind === 'boss' ? 5 : 1);
        burst(BASE.x, BASE.y, '#f87171', 12);
        toast('기지 피격!', '#fca5a5');
      }
    }

    // 발사대
    for (let i = 0; i < S.slotsUnlocked; i++) {
      const l = S.launchers[i];
      if (!l) continue;
      if (l.pop > 0) l.pop -= dt * 2;
      if (l.recoil > 0) l.recoil -= dt * 4;
      const T = TIERS[l.tier - 1];
      l.cool -= dt;
      if (l.cool <= 0) {
        const c = cells[i];
        const tgt = pickTarget(c.cx, c.cy);
        if (tgt) {
          l.cool = T.cd * rateMult();
          l.recoil = 1;
          const ang = Math.atan2(tgt.y - (c.cy - 30), tgt.x - c.cx);
          S.shots.push({
            x: c.cx + (Math.random() - 0.5) * 10, y: c.cy - 34, ang,
            speed: T.speed, dmg: T.dmg * powerMult(), splash: T.splash,
            slow: !!T.slow, color: T.color, target: tgt, life: 4, trail: [], size: 3 + l.tier * 0.5,
          });
        } else {
          l.cool = 0.15;
        }
      }
    }

    // 탄 이동
    for (const s of S.shots) {
      s.life -= dt;
      if (s.target && s.target.hp > 0) {
        const want = Math.atan2(s.target.y - s.y, s.target.x - s.x);
        let diff = want - s.ang;
        while (diff > Math.PI) diff -= Math.PI * 2;
        while (diff < -Math.PI) diff += Math.PI * 2;
        s.ang += Math.max(-7 * dt, Math.min(7 * dt, diff));
      } else {
        s.target = pickTarget(s.x, s.y);
      }
      s.x += Math.cos(s.ang) * s.speed * dt;
      s.y += Math.sin(s.ang) * s.speed * dt;
      s.trail.push({ x: s.x, y: s.y });
      if (s.trail.length > 7) s.trail.shift();

      if (s.target && s.target.hp > 0) {
        const dd = Math.hypot(s.target.x - s.x, s.target.y - s.y);
        if (dd < s.target.r + 7) {
          damage(s.target, s.dmg, s);
          if (s.splash > 0) {
            for (const e of S.enemies) {
              if (e === s.target || e.hp <= 0) continue;
              if (Math.hypot(e.x - s.x, e.y - s.y) < s.splash) damage(e, s.dmg * 0.55, s);
            }
            S.fx.push({ x: s.x, y: s.y, vx: 0, vy: 0, life: 0.3, max: 0.3, r: s.splash, color: s.color, ring: true });
          }
          burst(s.x, s.y, s.color, 7);
          s.life = 0;
        }
      }
      if (s.x < -60 || s.x > W + 60 || s.y < -60 || s.y > H) s.life = 0;
    }
    S.shots = S.shots.filter(s => s.life > 0);

    // 적 정리
    const alive = [];
    for (const e of S.enemies) {
      if (e.hp > 0) { alive.push(e); continue; }
      if (!e.leaked) {
        const g = e.gold * goldMult();
        S.gold += g;
        S.killCount++;
        burst(e.x, e.y, '#fbbf24', 9);
      }
    }
    S.enemies = alive;

    if (S.baseHp <= 0 && !S.over) {
      S.baseHp = 0; S.over = true; S.waveState = 'over';
      const best = Number(localStorage.getItem('mrush.best') || 0);
      if (S.wave > best) localStorage.setItem('mrush.best', String(S.wave));
      showGameOver();
    }

    updateFx(dt);
  }

  function damage(e, amount, s) {
    e.hp -= amount;
    e.hit = 0.12;
    if (s && s.slow) e.slowT = 2;
  }

  function updateFx(dt) {
    for (const f of S.fx) {
      f.life -= dt;
      f.x += f.vx * dt; f.y += f.vy * dt;
      f.vx *= 0.92; f.vy *= 0.92;
    }
    S.fx = S.fx.filter(f => f.life > 0);
    for (const t of S.texts) t.life -= dt;
    S.texts = S.texts.filter(t => t.life > 0);
  }

  /* ---------- 렌더 ---------- */
  const canvas = document.getElementById('game');
  const ctx = canvas.getContext('2d');
  canvas.width = W; canvas.height = H;

  function draw() {
    ctx.clearRect(0, 0, W, H);
    drawField();
    drawPath();
    drawBase();
    drawEnemies();
    drawShots();
    drawFx();
    drawGrid();
    drawToasts();
    drawDrag();
  }

  function drawField() {
    const g = ctx.createLinearGradient(0, 0, 0, FIELD_BOTTOM);
    g.addColorStop(0, '#0b1220');
    g.addColorStop(1, '#111c2e');
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, W, FIELD_BOTTOM);
    ctx.strokeStyle = 'rgba(56,189,248,0.06)';
    ctx.lineWidth = 1;
    for (let x = 0; x <= W; x += 30) { ctx.beginPath(); ctx.moveTo(x, 0); ctx.lineTo(x, FIELD_BOTTOM); ctx.stroke(); }
    for (let y = 0; y <= FIELD_BOTTOM; y += 30) { ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(W, y); ctx.stroke(); }

    ctx.fillStyle = '#0a0f18';
    ctx.fillRect(0, FIELD_BOTTOM, W, H - FIELD_BOTTOM);
    ctx.strokeStyle = 'rgba(148,163,184,0.25)';
    ctx.beginPath(); ctx.moveTo(0, FIELD_BOTTOM + 0.5); ctx.lineTo(W, FIELD_BOTTOM + 0.5); ctx.stroke();
  }

  function drawPath() {
    ctx.lineCap = 'round'; ctx.lineJoin = 'round';
    ctx.strokeStyle = 'rgba(148,163,184,0.16)';
    ctx.lineWidth = 44;
    ctx.beginPath();
    ctx.moveTo(PATH[0].x, PATH[0].y);
    for (let i = 1; i < PATH.length; i++) ctx.lineTo(PATH[i].x, PATH[i].y);
    ctx.stroke();
    ctx.strokeStyle = 'rgba(56,189,248,0.22)';
    ctx.lineWidth = 2;
    ctx.setLineDash([10, 12]);
    ctx.lineDashOffset = -S.elapsed * 30;
    ctx.stroke();
    ctx.setLineDash([]);
  }

  function drawBase() {
    const pulse = 1 + Math.sin(S.elapsed * 3) * 0.05;
    ctx.save();
    ctx.translate(BASE.x, BASE.y);
    ctx.scale(pulse, pulse);
    const ratio = S.baseHp / S.baseHpMax;
    const col = ratio > 0.5 ? '#38bdf8' : ratio > 0.25 ? '#fbbf24' : '#ef4444';
    ctx.fillStyle = 'rgba(56,189,248,0.12)';
    ctx.beginPath(); ctx.arc(0, 0, 34, 0, Math.PI * 2); ctx.fill();
    ctx.fillStyle = col;
    ctx.beginPath();
    for (let i = 0; i < 6; i++) {
      const a = (Math.PI / 3) * i - Math.PI / 2;
      const px = Math.cos(a) * 18, py = Math.sin(a) * 18;
      i ? ctx.lineTo(px, py) : ctx.moveTo(px, py);
    }
    ctx.closePath(); ctx.fill();
    ctx.fillStyle = '#0b1220';
    ctx.font = 'bold 13px system-ui, sans-serif';
    ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
    ctx.fillText(String(S.baseHp), 0, 1);
    ctx.restore();
  }

  function drawEnemies() {
    for (const e of S.enemies) {
      ctx.save();
      ctx.translate(e.x, e.y);
      const flash = e.hit > 0;
      let fill = '#94a3b8';
      if (e.kind === 'fast') fill = '#4ade80';
      else if (e.kind === 'armor') fill = '#a78bfa';
      else if (e.kind === 'boss') fill = '#ef4444';
      if (e.slowT > 0) fill = '#22d3ee';
      ctx.fillStyle = flash ? '#ffffff' : fill;
      ctx.strokeStyle = 'rgba(0,0,0,0.5)'; ctx.lineWidth = 2;

      if (e.kind === 'fast') {
        ctx.beginPath();
        ctx.moveTo(0, -e.r); ctx.lineTo(e.r, e.r * 0.8); ctx.lineTo(-e.r, e.r * 0.8);
        ctx.closePath(); ctx.fill(); ctx.stroke();
      } else if (e.kind === 'armor') {
        ctx.fillRect(-e.r, -e.r, e.r * 2, e.r * 2);
        ctx.strokeRect(-e.r, -e.r, e.r * 2, e.r * 2);
      } else if (e.kind === 'boss') {
        ctx.beginPath();
        for (let i = 0; i < 8; i++) {
          const a = (Math.PI / 4) * i - Math.PI / 2;
          const rr = i % 2 ? e.r * 0.66 : e.r;
          const px = Math.cos(a) * rr, py = Math.sin(a) * rr;
          i ? ctx.lineTo(px, py) : ctx.moveTo(px, py);
        }
        ctx.closePath(); ctx.fill(); ctx.stroke();
      } else {
        ctx.beginPath(); ctx.arc(0, 0, e.r, 0, Math.PI * 2); ctx.fill(); ctx.stroke();
      }
      ctx.restore();

      // 체력바
      const bw = e.r * 2.4, ratio = Math.max(0, e.hp / e.hpMax);
      ctx.fillStyle = 'rgba(0,0,0,0.55)';
      ctx.fillRect(e.x - bw / 2, e.y - e.r - 10, bw, 4);
      ctx.fillStyle = e.kind === 'boss' ? '#f87171' : '#4ade80';
      ctx.fillRect(e.x - bw / 2, e.y - e.r - 10, bw * ratio, 4);
    }
  }

  function drawShots() {
    for (const s of S.shots) {
      if (s.trail.length > 1) {
        ctx.strokeStyle = s.color; ctx.globalAlpha = 0.28; ctx.lineWidth = s.size * 1.4;
        ctx.beginPath();
        ctx.moveTo(s.trail[0].x, s.trail[0].y);
        for (const p of s.trail) ctx.lineTo(p.x, p.y);
        ctx.stroke();
        ctx.globalAlpha = 1;
      }
      ctx.save();
      ctx.translate(s.x, s.y); ctx.rotate(s.ang);
      ctx.fillStyle = s.color;
      ctx.beginPath();
      ctx.moveTo(s.size * 2.2, 0);
      ctx.lineTo(-s.size, s.size * 0.8);
      ctx.lineTo(-s.size, -s.size * 0.8);
      ctx.closePath(); ctx.fill();
      ctx.restore();
    }
  }

  function drawFx() {
    for (const f of S.fx) {
      const a = Math.max(0, f.life / f.max);
      ctx.globalAlpha = a;
      if (f.ring) {
        ctx.strokeStyle = f.color; ctx.lineWidth = 3;
        ctx.beginPath(); ctx.arc(f.x, f.y, f.r * (1.2 - a * 0.5), 0, Math.PI * 2); ctx.stroke();
      } else {
        ctx.fillStyle = f.color;
        ctx.beginPath(); ctx.arc(f.x, f.y, f.r * a, 0, Math.PI * 2); ctx.fill();
      }
      ctx.globalAlpha = 1;
    }
  }

  function drawGrid() {
    for (let i = 0; i < SLOTS; i++) {
      const c = cells[i];
      const locked = i >= S.slotsUnlocked;
      const l = S.launchers[i];

      ctx.fillStyle = locked ? 'rgba(30,41,59,0.35)' : 'rgba(30,41,59,0.75)';
      roundRect(c.x, c.y, c.w, c.h, 12); ctx.fill();
      ctx.strokeStyle = drag.active && drag.hover === i && drag.from !== i
        ? '#fbbf24'
        : (locked ? 'rgba(100,116,139,0.25)' : 'rgba(100,116,139,0.55)');
      ctx.lineWidth = drag.active && drag.hover === i ? 3 : 1.5;
      roundRect(c.x, c.y, c.w, c.h, 12); ctx.stroke();

      if (locked) {
        ctx.fillStyle = 'rgba(148,163,184,0.5)';
        ctx.font = '22px system-ui, sans-serif';
        ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
        ctx.fillText('🔒', c.cx, c.cy - 6);
        ctx.font = '11px system-ui, sans-serif';
        ctx.fillText('잠김', c.cx, c.cy + 18);
        continue;
      }
      if (!l) {
        ctx.fillStyle = 'rgba(100,116,139,0.35)';
        ctx.font = '12px system-ui, sans-serif';
        ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
        ctx.fillText('빈 발사대', c.cx, c.cy);
        continue;
      }
      if (drag.active && drag.from === i) continue;
      drawLauncher(c.cx, c.cy, l);
    }
  }

  function drawLauncher(cx, cy, l) {
    const T = TIERS[l.tier - 1];
    const pop = l.pop > 0 ? 1 + l.pop * 0.5 : 1;
    const recoil = l.recoil > 0 ? l.recoil * 5 : 0;
    ctx.save();
    ctx.translate(cx, cy - 10 + recoil);
    ctx.scale(pop, pop);

    // 발사대 받침
    ctx.fillStyle = 'rgba(15,23,42,0.9)';
    roundRect(-34, 24, 68, 14, 5); ctx.fill();
    ctx.fillStyle = 'rgba(100,116,139,0.5)';
    roundRect(-26, 20, 52, 8, 3); ctx.fill();

    // 미사일 본체
    ctx.fillStyle = T.color;
    ctx.beginPath();
    ctx.moveTo(0, -34);
    ctx.quadraticCurveTo(11, -16, 11, 6);
    ctx.lineTo(-11, 6);
    ctx.quadraticCurveTo(-11, -16, 0, -34);
    ctx.closePath(); ctx.fill();
    // 날개
    ctx.beginPath();
    ctx.moveTo(11, -2); ctx.lineTo(20, 16); ctx.lineTo(11, 16);
    ctx.moveTo(-11, -2); ctx.lineTo(-20, 16); ctx.lineTo(-11, 16);
    ctx.fill();
    // 창
    ctx.fillStyle = 'rgba(15,23,42,0.55)';
    ctx.beginPath(); ctx.arc(0, -18, 4.2, 0, Math.PI * 2); ctx.fill();

    // 등급 배지
    ctx.fillStyle = T.color;
    roundRect(-17, 42, 34, 17, 8); ctx.fill();
    ctx.fillStyle = '#0b1220';
    ctx.font = 'bold 12px system-ui, sans-serif';
    ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
    ctx.fillText('Lv' + l.tier, 0, 51);

    ctx.restore();

    ctx.fillStyle = 'rgba(226,232,240,0.85)';
    ctx.font = '10px system-ui, sans-serif';
    ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
    ctx.fillText(T.name, cx, cy + 54);
  }

  function drawToasts() {
    ctx.textAlign = 'center'; ctx.textBaseline = 'middle';
    for (let i = 0; i < S.texts.length; i++) {
      const t = S.texts[i];
      ctx.globalAlpha = Math.min(1, t.life / 0.5);
      ctx.fillStyle = t.color;
      ctx.font = 'bold 15px system-ui, sans-serif';
      ctx.fillText(t.text, W / 2, 470 - (S.texts.length - 1 - i) * 22);
      ctx.globalAlpha = 1;
    }
  }

  function drawDrag() {
    if (!drag.active) return;
    const l = S.launchers[drag.from];
    if (!l) return;
    ctx.globalAlpha = 0.9;
    drawLauncher(drag.x, drag.y, l);
    ctx.globalAlpha = 1;
  }

  function roundRect(x, y, w, h, r) {
    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.arcTo(x + w, y, x + w, y + h, r);
    ctx.arcTo(x + w, y + h, x, y + h, r);
    ctx.arcTo(x, y + h, x, y, r);
    ctx.arcTo(x, y, x + w, y, r);
    ctx.closePath();
  }

  /* ---------- 드래그 & 드롭 합성 ---------- */
  const drag = { active: false, from: -1, x: 0, y: 0, hover: -1, moved: false };

  function toLocal(evt) {
    const rect = canvas.getBoundingClientRect();
    return {
      x: (evt.clientX - rect.left) * (W / rect.width),
      y: (evt.clientY - rect.top) * (H / rect.height),
    };
  }
  function slotAt(x, y) {
    for (let i = 0; i < SLOTS; i++) {
      const c = cells[i];
      if (x >= c.x && x <= c.x + c.w && y >= c.y && y <= c.y + c.h) return i;
    }
    return -1;
  }

  canvas.addEventListener('pointerdown', (e) => {
    const p = toLocal(e);
    const i = slotAt(p.x, p.y);
    if (i < 0) return;
    if (i >= S.slotsUnlocked) { buy('slot'); return; }
    if (!S.launchers[i]) { summon(); return; }
    drag.active = true; drag.from = i; drag.x = p.x; drag.y = p.y; drag.hover = i; drag.moved = false;
    canvas.setPointerCapture(e.pointerId);
  });

  canvas.addEventListener('pointermove', (e) => {
    if (!drag.active) return;
    const p = toLocal(e);
    drag.x = p.x; drag.y = p.y; drag.moved = true;
    drag.hover = slotAt(p.x, p.y);
  });

  function endDrag() {
    if (!drag.active) return;
    const to = drag.hover, from = drag.from;
    drag.active = false; drag.from = -1; drag.hover = -1;
    if (to < 0 || to === from || to >= S.slotsUnlocked) return;
    const a = S.launchers[from], b = S.launchers[to];
    if (!a) return;
    if (!b) { S.launchers[to] = a; S.launchers[from] = null; return; }
    if (a.tier === b.tier) { mergeInto(from, to); return; }
    S.launchers[to] = a; S.launchers[from] = b; // 교체
  }
  canvas.addEventListener('pointerup', endDrag);
  canvas.addEventListener('pointercancel', endDrag);

  /* ---------- HTML UI 바인딩 ---------- */
  const el = (id) => document.getElementById(id);
  const ui = {
    gold: el('stat-gold'), wave: el('stat-wave'), hp: el('stat-hp'), dps: el('stat-dps'),
    summon: el('btn-summon'), skill: el('btn-skill'),
    autoMerge: el('btn-automerge'), autoSummon: el('btn-autosummon'),
    speed: el('btn-speed'), priority: el('btn-priority'),
    panel: el('panel'), over: el('gameover'), overText: el('gameover-text'),
  };

  ui.summon.addEventListener('click', () => summon());
  ui.skill.addEventListener('click', useSkill);
  ui.autoMerge.addEventListener('click', () => { S.autoMerge = !S.autoMerge; });
  ui.autoSummon.addEventListener('click', () => { S.autoSummon = !S.autoSummon; });
  ui.speed.addEventListener('click', () => { S.speed = S.speed === 1 ? 2 : S.speed === 2 ? 3 : 1; });
  ui.priority.addEventListener('click', () => {
    S.priority = S.priority === 'first' ? 'strong' : S.priority === 'strong' ? 'close' : 'first';
  });
  el('btn-upgrades').addEventListener('click', () => ui.panel.classList.toggle('open'));
  el('panel-close').addEventListener('click', () => ui.panel.classList.remove('open'));
  el('btn-restart').addEventListener('click', () => { ui.over.classList.remove('open'); S = newGame(); });
  document.querySelectorAll('[data-buy]').forEach(b => {
    b.addEventListener('click', () => buy(b.getAttribute('data-buy')));
  });

  const PRIORITY_LABEL = { first: '선두', strong: '최강', close: '근접' };

  function showGameOver() {
    const best = localStorage.getItem('mrush.best') || S.wave;
    ui.overText.innerHTML =
      `도달 웨이브 <b>${S.wave}</b><br>격추 ${fmt(S.killCount)}기 · 최고 기록 웨이브 ${best}`;
    ui.over.classList.add('open');
  }

  function syncUI() {
    ui.gold.textContent = fmt(S.gold);
    ui.wave.textContent = S.wave;
    ui.hp.textContent = `${S.baseHp}/${S.baseHpMax}`;
    ui.dps.textContent = fmt(totalDps());

    const sc = summonCost();
    ui.summon.innerHTML = `소환<small>${fmt(sc)}G</small>`;
    ui.summon.disabled = S.gold < sc || firstEmpty() < 0;

    ui.skill.innerHTML = S.skillCd > 0
      ? `궤도 폭격<small>${S.skillCd.toFixed(0)}초</small>`
      : `궤도 폭격<small>준비 완료</small>`;
    ui.skill.disabled = S.skillCd > 0;

    ui.autoMerge.classList.toggle('on', S.autoMerge);
    ui.autoSummon.classList.toggle('on', S.autoSummon);
    ui.speed.textContent = `x${S.speed}`;
    ui.priority.innerHTML = `조준<small>${PRIORITY_LABEL[S.priority]}</small>`;

    if (ui.panel.classList.contains('open')) {
      setRow('power', S.up.power, costPower(), `탄두 위력 +8% (현재 x${powerMult().toFixed(2)})`);
      setRow('rate', S.up.rate, costRate(), `장전 속도 +4.5% (현재 x${(1 / rateMult()).toFixed(2)})`);
      setRow('gold', S.up.gold, costGold(), `자금 회수 +10% (현재 x${goldMult().toFixed(2)})`);
      setRow('grade', S.up.grade, costGrade(), '소환 등급 확률 상승');
      setRow('repair', S.up.repair, costRepair(), '기지 최대 내구 +2, 즉시 +5 수리');
      const full = S.slotsUnlocked >= SLOTS;
      setRow('slot', S.slotsUnlocked - 6, full ? Infinity : costSlot(),
        full ? '모든 발사대 개방 완료' : `발사대 개방 (${S.slotsUnlocked}/${SLOTS})`);
    }
  }

  function setRow(key, lv, cost, desc) {
    const row = document.querySelector(`[data-buy="${key}"]`);
    if (!row) return;
    row.querySelector('.u-lv').textContent = `Lv.${lv}`;
    row.querySelector('.u-desc').textContent = desc;
    row.querySelector('.u-cost').textContent = isFinite(cost) ? fmt(cost) + 'G' : 'MAX';
    row.disabled = !isFinite(cost) || S.gold < cost;
  }

  /* ---------- 루프 ---------- */
  let last = performance.now();
  function loop(now) {
    let dt = (now - last) / 1000;
    last = now;
    dt = Math.min(dt, 0.05);
    const steps = S.speed;
    for (let i = 0; i < steps; i++) update(dt);
    draw();
    syncUI();
    requestAnimationFrame(loop);
  }

  S = newGame();
  requestAnimationFrame(loop);
})();
