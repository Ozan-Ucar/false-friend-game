const fs   = require('fs');
const zlib = require('zlib');

// Render at 2x then downsample → smooth anti-aliased edges.
const OUT = 128, SS = 2, N = OUT * SS;       // N = 256 work resolution
const CX = N / 2, CY = N / 2;

const ICON = [74, 46, 18];                    // dark brown #4A2E12

function newBuf() { return new Float32Array(N * N * 4); } // rgba, premultiplied-ish

function set(buf, x, y, r, g, b, a) {
    if (x < 0 || x >= N || y < 0 || y >= N) return;
    const i = (y * N + x) * 4;
    buf[i] = r; buf[i+1] = g; buf[i+2] = b; buf[i+3] = a;
}

// ── geometry helpers ──────────────────────────────────────────────────────────
function pointInPoly(px, py, pts) {
    let inside = false;
    for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) {
        const xi = pts[i][0], yi = pts[i][1], xj = pts[j][0], yj = pts[j][1];
        if (((yi > py) !== (yj > py)) &&
            (px < (xj - xi) * (py - yi) / (yj - yi) + xi)) inside = !inside;
    }
    return inside;
}
function fillTest(buf, test) {
    for (let y = 0; y < N; y++) for (let x = 0; x < N; x++)
        if (test(x + 0.5, y + 0.5)) set(buf, x, y, ICON[0], ICON[1], ICON[2], 255);
}
function poly(buf, pts)        { fillTest(buf, (x, y) => pointInPoly(x, y, pts)); }
function disc(buf, cx, cy, r)  { fillTest(buf, (x, y) => (x-cx)**2 + (y-cy)**2 <= r*r); }
function ring(buf, cx, cy, rO, rI, test) {
    fillTest(buf, (x, y) => {
        const d2 = (x-cx)**2 + (y-cy)**2;
        return d2 <= rO*rO && d2 >= rI*rI && (test ? test(x, y) : true);
    });
}
function rect(buf, x0, y0, x1, y1) {
    fillTest(buf, (x, y) => x >= x0 && x <= x1 && y >= y0 && y <= y1);
}
function ellipse(buf, cx, cy, rx, ry) {
    fillTest(buf, (x, y) => ((x-cx)/rx)**2 + ((y-cy)/ry)**2 <= 1);
}
function star(buf, cx, cy, rO, rI, points, rot) {
    const pts = [];
    for (let i = 0; i < points * 2; i++) {
        const r = (i % 2 === 0) ? rO : rI;
        const a = rot + i * Math.PI / points;
        pts.push([cx + Math.cos(a) * r, cy + Math.sin(a) * r]);
    }
    poly(buf, pts);
}

// ── icons ──────────────────────────────────────────────────────────────────────
const icons = {};

// PLAY — right triangle
icons.play = (b) => poly(b, [[96,72],[96,184],[182,128]]);

// CONTINUE — double "fast-forward" triangle
icons.continue = (b) => {
    poly(b, [[72,76],[72,180],[126,128]]);
    poly(b, [[126,76],[126,180],[180,128]]);
};

// OPTION — gear (8 teeth) with centre hole
icons.option = (b) => {
    ring(b, CX, CY, 78, 30, (x, y) => {
        const ang = Math.atan2(y - CY, x - CX);
        const r   = Math.hypot(x - CX, y - CY);
        const tooth = Math.cos(ang * 8) > 0.15;     // teeth sectors
        return r <= (tooth ? 78 : 62);
    });
};

// ACHIEVEMENTS — trophy
icons.achievements = (b) => {
    ellipse(b, CX, 80, 46, 14);                     // rim
    poly(b, [[82,80],[174,80],[156,142],[100,142]]);// bowl
    ring(b, 70, 96, 26, 14, (x,y)=> x < 84);        // left handle
    ring(b, 186, 96, 26, 14, (x,y)=> x > 172);      // right handle
    rect(b, 120, 142, 136, 162);                    // stem
    rect(b, 98, 162, 158, 172);                     // base top
    rect(b, 88, 172, 168, 184);                     // base bottom
};

// QUIT — power symbol (ring with top gap + vertical bar)
icons.quit = (b) => {
    ring(b, CX, CY + 6, 66, 50, (x, y) => {
        const ang = Math.atan2(y - (CY + 6), x - CX); // -PI..PI, up = -PI/2
        return Math.abs(ang + Math.PI / 2) > 0.45;    // gap at the very top
    });
    rect(b, 120, 52, 136, 130);                       // vertical bar
};

// CREDITS — 5-point star
icons.credits = (b) => star(b, CX, CY + 4, 82, 34, 5, -Math.PI / 2);

// ── PNG encode (RGBA) with downsample ───────────────────────────────────────────
const crcTable = new Uint32Array(256);
for (let n = 0; n < 256; n++) { let c = n; for (let k = 0; k < 8; k++) c = (c & 1) ? 0xEDB88320 ^ (c >>> 1) : c >>> 1; crcTable[n] = c; }
function crc32(b){let c=0xFFFFFFFF;for(let i=0;i<b.length;i++)c=crcTable[(c^b[i])&0xFF]^(c>>>8);return(c^0xFFFFFFFF)>>>0;}
function chunk(t,d){const tt=Buffer.from(t,'ascii');const l=Buffer.alloc(4);l.writeUInt32BE(d.length);const cb=Buffer.alloc(4);cb.writeUInt32BE(crc32(Buffer.concat([tt,d])));return Buffer.concat([l,tt,d,cb]);}

function encode(buf) {
    // downsample SSxSS box filter → OUT x OUT RGBA
    const W = OUT, H = OUT, stride = W * 4 + 1;
    const raw = Buffer.alloc(H * stride);
    for (let y = 0; y < H; y++) {
        raw[y * stride] = 0;
        for (let x = 0; x < W; x++) {
            let r=0,g=0,bl=0,a=0;
            for (let sy=0; sy<SS; sy++) for (let sx=0; sx<SS; sx++) {
                const i = ((y*SS+sy)*N + (x*SS+sx))*4;
                r+=buf[i]; g+=buf[i+1]; bl+=buf[i+2]; a+=buf[i+3];
            }
            const n = SS*SS, di = y*stride + 1 + x*4;
            raw[di]=r/n|0; raw[di+1]=g/n|0; raw[di+2]=bl/n|0; raw[di+3]=a/n|0;
        }
    }
    const ihdr = Buffer.alloc(13);
    ihdr.writeUInt32BE(W,0); ihdr.writeUInt32BE(H,4); ihdr[8]=8; ihdr[9]=6; // RGBA
    const comp = zlib.deflateSync(raw, {level:6});
    const sig  = Buffer.from([137,80,78,71,13,10,26,10]);
    return Buffer.concat([sig, chunk('IHDR',ihdr), chunk('IDAT',comp), chunk('IEND',Buffer.alloc(0))]);
}

const dir = 'Assets/Textures/UI/Icons';
fs.mkdirSync(dir, {recursive:true});
for (const [name, draw] of Object.entries(icons)) {
    const b = newBuf();
    draw(b);
    fs.writeFileSync(`${dir}/${name}.png`, encode(b));
    console.log('  ' + name + '.png');
}
console.log('Done → ' + dir);
