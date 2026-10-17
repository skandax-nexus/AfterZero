"""AFTERZERO - procedural PBR texture + sky environment baker.

Generates the environment texture sets used by assets/js/render.js (world-space triplanar mapping), plus three small
HDR sky environments (PAST / PRESENT / FUTURE) used for image-based lighting.

Run with Blender's Python module (bpy) - numpy is bundled with it:
    <blender-venv>/bin/python tools/textures/bake_textures.py [--only cobble,brick] [--no-sky] [--preview out.png]

Texture sets (written to assets/textures/):
    <name>_a.jpg   albedo (sRGB). Cavity AO, grime and edge wear are baked in.
    <name>_n.jpg   R,G = tangent-space normal X,Y (OpenGL convention: +Y = up the image), B = roughness (linear).
Every map is SEAMLESSLY TILEABLE by construction: all noise is periodic lattice noise whose period divides the tile,
Voronoi cells wrap around, block patterns use whole rows/columns, and blurs/derivatives are circular (FFT / np.roll).

Sky environments (written to assets/textures/):
    sky_past.hdr / sky_present.hdr / sky_future.hdr - 256x128 equirect, Radiance RGBE (flat scanlines), in the
    three.js equirect convention (u = atan2(z, x) / 2pi + 0.5, v = asin(y) / pi + 0.5).
    PAST is Blender's physical (multiple scattering / Nishita) sky at dusk; PRESENT starts from a physical blue-hour
    sky, then gets a rainy overcast deck lit from below by sodium city glow; FUTURE is a re-tinted physical sky with an
    alien violet atmosphere, aurora bands, a second moon and stars.

A manifest (assets/textures/manifest.json) records each set's mean linear albedo and mean roughness; render.js keeps
a copy of those numbers in R.TEXTURES so the material colours chosen by the level code stay authoritative (the
texture contributes relative variation: colour * tex / mean).
"""
import argparse
import json
import math
import os
import sys

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.normpath(os.path.join(HERE, "..", "..", "assets", "textures"))


# =====================================================================================================================
# periodic primitives (coordinates x, y in tile units [0, 1); everything wraps at 1)
# =====================================================================================================================
def grid(n):
    u = (np.arange(n, dtype=np.float64) + 0.5) / n
    return np.meshgrid(u, u)  # x: columns (u), y: rows (row 0 = TOP of the image)


def _fade(t):
    return t * t * t * (t * (t * 6 - 15) + 10)


_GRAD = {}


def perlin(x, y, px, py, seed):
    """Periodic gradient noise; px, py integer lattice periods across the tile. Output roughly in [-1, 1]."""
    key = (px, py, seed)
    if key not in _GRAD:
        a = np.random.default_rng(seed).uniform(0, 2 * np.pi, (py, px))
        _GRAD[key] = (np.cos(a), np.sin(a))
    gx, gy = _GRAD[key]
    X, Y = x * px, y * py
    xi, yi = np.floor(X).astype(np.int64), np.floor(Y).astype(np.int64)
    xf, yf = X - xi, Y - yi
    x0, x1, y0, y1 = xi % px, (xi + 1) % px, yi % py, (yi + 1) % py
    d = lambda ix, iy, dx, dy: gx[iy, ix] * dx + gy[iy, ix] * dy
    n00, n10, n01, n11 = d(x0, y0, xf, yf), d(x1, y0, xf - 1, yf), d(x0, y1, xf, yf - 1), d(x1, y1, xf - 1, yf - 1)
    u, v = _fade(xf), _fade(yf)
    a = n00 + u * (n10 - n00)
    b = n01 + u * (n11 - n01)
    return (a + v * (b - a)) * 1.41


def fbm(x, y, p, octaves, seed, gain=0.5, aniso=(1, 1)):
    s, amp, tot = 0.0, 1.0, 0.0
    for o in range(octaves):
        f = 2 ** o
        s = s + perlin(x, y, max(1, int(p * aniso[0] * f)), max(1, int(p * aniso[1] * f)), seed + 101 * o) * amp
        tot += amp
        amp *= gain
    return s / tot


def ridged(x, y, p, octaves, seed):
    return 1.0 - np.abs(fbm(x, y, p, octaves, seed))


def voronoi(x, y, cols, rows, seed, jitter=0.85, stagger=0.0, aspect=1.0):
    """Periodic Voronoi on a (optionally staggered) cols x rows lattice. Distances are in tile units with the y axis
    scaled by `aspect` (tile height / tile width in the world). Returns F1, F2, cell id, offset to nearest point."""
    rng = np.random.default_rng(seed)
    jx = rng.uniform(-0.5, 0.5, (rows, cols)) * jitter
    jy = rng.uniform(-0.5, 0.5, (rows, cols)) * jitter
    X, Y = x * cols, y * rows
    ci, ri = np.floor(X).astype(np.int64), np.floor(Y).astype(np.int64)
    F1 = np.full(x.shape, 1e9)
    F2 = np.full(x.shape, 1e9)
    ID = np.zeros(x.shape, np.int64)
    DX = np.zeros(x.shape)
    DY = np.zeros(x.shape)
    for dr in (-1, 0, 1):
        for dc in (-2, -1, 0, 1, 2):
            r = ri + dr
            c = ci + dc
            rr, cc = r % rows, c % cols
            px = c + 0.5 + stagger * (rr % 2) + jx[rr, cc]
            py = r + 0.5 + jy[rr, cc]
            dx = (X - px) / cols
            dy = (Y - py) / rows * aspect
            d = np.sqrt(dx * dx + dy * dy)
            closer = d < F1
            F2 = np.where(closer, F1, np.minimum(F2, d))
            F1 = np.where(closer, d, F1)
            ID = np.where(closer, rr * cols + cc, ID)
            DX = np.where(closer, dx, DX)
            DY = np.where(closer, dy, DY)
    return F1, F2, ID, DX, DY


def blocks(x, y, cols, rows, offsets):
    """Running-bond blocks. offsets[row] = horizontal shift (in block units) of that row. Returns
    (block id, fx, fy local 0..1, edge distance in tile units along x and y)."""
    Y = y * rows
    r = np.floor(Y).astype(np.int64) % rows
    off = np.asarray(offsets)[r]
    X = x * cols + off
    c = np.floor(X).astype(np.int64)
    fx, fy = X - c, Y - np.floor(Y)
    c = c % cols
    ex = np.minimum(fx, 1 - fx) / cols
    ey = np.minimum(fy, 1 - fy) / rows
    return r * cols + c, fx, fy, ex, ey


def blur(img, sigma):
    """Circular gaussian blur (sigma in pixels) - wraps, so tiling is preserved."""
    if sigma <= 0:
        return img
    n0, n1 = img.shape[:2]
    f0, f1 = np.fft.fftfreq(n0), np.fft.fftfreq(n1)
    g = np.exp(-2 * (np.pi * sigma) ** 2 * (f0[:, None] ** 2 + f1[None, :] ** 2))
    if img.ndim == 3:
        return np.stack([np.real(np.fft.ifft2(np.fft.fft2(img[..., k]) * g)) for k in range(img.shape[2])], -1)
    return np.real(np.fft.ifft2(np.fft.fft2(img) * g))


def sstep(a, b, t):
    t = np.clip((t - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def lin(hexv):
    c = np.array([(hexv >> 16) & 255, (hexv >> 8) & 255, hexv & 255], np.float64) / 255.0
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def to_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1 / 2.4) - 0.055)


def mixc(a, b, t):
    t = t[..., None] if np.ndim(t) == 2 else t
    return a + (b - a) * t


def per_id(ids, seed, n=1, lo=0.0, hi=1.0):
    """Stable random value(s) per integer id."""
    m = int(ids.max()) + 1
    r = np.random.default_rng(seed).uniform(lo, hi, (m, n))
    v = r[ids]
    return v[..., 0] if n == 1 else v


def palette(ids, seed, cols, weights=None):
    """Pick a colour per id from a list of linear colours, with weights."""
    cols = np.array(cols)
    m = int(ids.max()) + 1
    rng = np.random.default_rng(seed)
    p = None if weights is None else np.array(weights) / np.sum(weights)
    pick = rng.choice(len(cols), m, p=p)
    return cols[pick][ids]


def lines(n, count, seed, length=(0.02, 0.12), angle=None, width=1.0):
    """Rasterise short periodic scratches; returns intensity map in [0,1]."""
    rng = np.random.default_rng(seed)
    img = np.zeros((n, n))
    for _ in range(count):
        x0, y0 = rng.uniform(0, n, 2)
        a = rng.uniform(0, np.pi) if angle is None else angle + rng.normal(0, 0.08)
        L = rng.uniform(*length) * n
        k = int(max(2, L * 2))
        t = np.linspace(0, 1, k)
        xs = (x0 + np.cos(a) * L * t + np.sin(t * 3.1) * rng.normal(0, 1.5)).astype(np.int64) % n
        ys = (y0 + np.sin(a) * L * t).astype(np.int64) % n
        s = rng.uniform(0.4, 1.0) * np.sin(np.pi * t) ** 0.5
        np.maximum.at(img, (ys, xs), s)
    return np.clip(blur(img, width * 0.5) * 2.2, 0, 1)


# =====================================================================================================================
# texture sets - each returns (albedo linear NxNx3, height in pixels NxN, roughness NxN) at resolution n
# world tile size (metres) is listed in SETS and mirrored in render.js R.TEXTURES
# =====================================================================================================================
def tex_cobble(n):
    """Wet-capable granite setts / cobbles in staggered rows with dirty sand joints."""
    x, y = grid(n)
    wx = x + 0.010 * fbm(x, y, 5, 3, 11)
    wy = y + 0.010 * fbm(x, y, 5, 3, 12)
    cols, rows = 12, 14
    F1, F2, ID, DX, DY = voronoi(wx, wy, cols, rows, 3, jitter=0.45, stagger=0.5)
    e = F2 - F1 + 0.004 * fbm(x, y, 40, 2, 13)
    stone = sstep(0.003, 0.016, e)
    cell = 1.0 / cols
    dome = np.clip(1 - (F1 / (cell * 0.62)) ** 2, 0, 1)
    tilt = per_id(ID, 14, 2, -1, 1)
    slope = (tilt[..., 0] * DX + tilt[..., 1] * DY) / cell
    h = stone ** 0.55 * (5.0 + 2.0 * dome + 0.9 * slope) + 0.6 * fbm(x, y, 64, 3, 15) * stone
    h += 0.35 * fbm(x, y, 128, 2, 16)
    base = palette(ID, 17, [lin(0x77736e), lin(0x5f5d5c), lin(0x847a6c), lin(0x4f4b48), lin(0x6c665e), lin(0x8a8378)],
                   [3, 3, 2, 2, 2, 1])
    tone = 0.82 + 0.36 * per_id(ID, 18)
    alb = base * tone[..., None]
    speck = perlin(x, y, 160, 160, 19) + 0.5 * perlin(x, y, 320, 320, 20)
    alb *= (1 + 0.18 * speck)[..., None]
    alb = mixc(alb, alb * 1.5, sstep(0.9, 1.3, speck) * 0.6)
    blotch = fbm(x, y, 4, 4, 21)
    alb *= (0.9 + 0.2 * blotch)[..., None]
    joint = lin(0x3a3128) * (0.75 + 0.5 * (0.5 + 0.5 * fbm(x, y, 60, 3, 22)))[..., None]
    alb = mixc(joint, alb, stone)
    # polished crowns (worn by feet): smoother + a touch lighter
    crown = dome * stone
    alb *= (1 + 0.08 * crown)[..., None]
    rough = 0.62 + 0.12 * fbm(x, y, 32, 2, 23) - 0.16 * crown
    rough = rough * stone + 0.95 * (1 - stone)
    return alb, h, rough


def tex_flagstone(n):
    """Dressed limestone / sandstone ashlar blocks (walls and paving), chipped arrises, chisel tooling, streaks."""
    x, y = grid(n)
    cols, rows = 4, 7
    rng = np.random.default_rng(31)
    offs = [0.5 * (r % 2) + rng.uniform(-0.08, 0.08) for r in range(rows)]
    ID, fx, fy, ex, ey = blocks(x, y, cols, rows, offs)
    chip = 0.0035 * fbm(x, y, 24, 4, 32) + 0.003 * np.maximum(fbm(x, y, 12, 3, 33) - 0.3, 0) * 4
    ed = np.minimum(ex, ey * (7 / 4) / (7 / 4)) - chip
    mortar = 0.0028
    face = sstep(mortar, mortar + 0.009, ed)
    tilt = per_id(ID, 34, 2, -1, 1)
    h = face * (4.0 + 0.8 * (tilt[..., 0] * (fx - 0.5) + tilt[..., 1] * (fy - 0.5)))
    tool = fbm(x, y, 8, 2, 35, aniso=(18, 1.2))  # diagonal-ish chisel striations
    h += face * (0.35 * fbm(x, y, 48, 4, 36) + 0.25 * tool)
    base = palette(ID, 37, [lin(0xb9ab8f), lin(0xa89a80), lin(0xc4b79b), lin(0x9d927f), lin(0xb5a27f)], [3, 3, 2, 2, 2])
    alb = base * (0.86 + 0.28 * per_id(ID, 38))[..., None]
    alb *= (1 + 0.10 * fbm(x, y, 32, 4, 39))[..., None]
    alb *= (0.9 + 0.2 * (0.5 + 0.5 * fbm(x, y, 6, 4, 40)))[..., None]
    # vertical weathering streaks (rain run-off) and lichen spots
    streak = sstep(0.15, 0.7, fbm(x, y, 1, 4, 41, aniso=(26, 2)))
    alb *= (1 - 0.22 * streak)[..., None]
    lich = sstep(0.55, 0.75, fbm(x, y, 18, 4, 42)) * 0.5
    alb = mixc(alb, lin(0x8a8c70) * 0.9, lich * face)
    mort = lin(0x9a9283) * (0.8 + 0.3 * (0.5 + 0.5 * fbm(x, y, 50, 2, 43)))[..., None]
    alb = mixc(mort * 0.82, alb, face)
    rough = (0.78 + 0.1 * fbm(x, y, 24, 2, 44)) * face + 0.95 * (1 - face)
    return alb, h, rough


def tex_brick(n):
    """Old fired clay brick, stretcher bond, recessed lime mortar with soot and efflorescence."""
    x, y = grid(n)
    cols, rows = 8, 24
    offs = [0.5 * (r % 2) for r in range(rows)]
    ID, fx, fy, ex, ey = blocks(x, y, cols, rows, offs)
    ey = ey * (1.8 / 1.88)  # tile is 1.88 x 1.8 m
    chip = 0.0022 * fbm(x, y, 40, 3, 51) + 0.004 * np.maximum(fbm(x, y, 20, 3, 52) - 0.35, 0) * 3
    ed = np.minimum(ex, ey) - chip
    mortar = 0.0042
    face = sstep(mortar, mortar + 0.0045, ed)
    h = face * 3.2 + face * 0.55 * fbm(x, y, 96, 3, 53) - (1 - face) * 0.4 * (0.5 + 0.5 * fbm(x, y, 120, 2, 54))
    pits = sstep(0.62, 0.8, perlin(x, y, 180, 180, 55)) * face
    h -= pits * 0.9
    base = palette(ID, 56, [lin(0x8e4a33), lin(0x9d5a3c), lin(0x7a3b2a), lin(0xa86a48), lin(0x5e2f24), lin(0x8a5a45),
                            lin(0x6b3a2c)], [4, 3, 3, 2, 1, 1, 2])
    alb = base * (0.8 + 0.4 * per_id(ID, 57))[..., None]
    # firing variation across each brick (darker headers/ends), sandy surface
    alb *= (0.9 + 0.1 * np.sin(np.pi * fx) ** 0.5)[..., None]
    alb *= (1 + 0.16 * fbm(x, y, 64, 3, 58))[..., None]
    alb *= (1 - 0.35 * pits)[..., None]
    soot = sstep(0.0, 0.8, fbm(x, y, 3, 4, 59))
    alb *= (1 - 0.3 * soot)[..., None]
    eff = sstep(0.55, 0.8, fbm(x, y, 2, 4, 60, aniso=(10, 3))) * sstep(0.5, 0.0, fy)
    alb = mixc(alb, lin(0xc9c2b5), eff * 0.35 * face)
    mort = lin(0xa39a8a) * (0.75 + 0.35 * (0.5 + 0.5 * fbm(x, y, 70, 3, 61)))[..., None] * (1 - 0.35 * soot)[..., None]
    alb = mixc(mort * 0.8, alb, face)
    rough = (0.82 + 0.08 * fbm(x, y, 48, 2, 62)) * face + 0.96 * (1 - face)
    return alb, h, rough


def tex_plaster(n):
    """Weathered lime stucco: trowel blotches, hairline cracks, spalled patches showing brick, run-off stains."""
    x, y = grid(n)
    wx = x + 0.03 * fbm(x, y, 3, 3, 71)
    wy = y + 0.03 * fbm(x, y, 3, 3, 72)
    F1, F2, ID, _, _ = voronoi(wx, wy, 5, 5, 73, jitter=0.9)
    crack = (1 - sstep(0.0, 0.0028, F2 - F1)) * sstep(0.1, 0.4, fbm(x, y, 4, 3, 74))
    spall = sstep(0.42, 0.47, fbm(x, y, 3, 5, 75))
    h = 1.6 * fbm(x, y, 96, 3, 76) + 0.8 * fbm(x, y, 20, 3, 77) - crack * 1.6 - spall * 2.6
    alb = lin(0xcdbd9f) * np.ones((n, n, 1))
    alb *= (0.86 + 0.28 * (0.5 + 0.5 * fbm(x, y, 4, 5, 78)))[..., None]
    alb *= (1 + 0.06 * fbm(x, y, 80, 2, 79))[..., None]
    stain = sstep(0.2, 0.75, fbm(x, y, 1, 5, 80, aniso=(14, 2)))
    alb = mixc(alb, alb * np.array([0.72, 0.72, 0.68]), stain * 0.7)
    # tide-line stain rings
    tl = fbm(x, y, 3, 4, 81)
    ring = (1 - sstep(0.0, 0.03, np.abs(tl - 0.25))) * 0.35
    alb *= (1 - ring)[..., None]
    # spalled areas: rough render undercoat with some brick showing
    bID, bfx, bfy, bex, bey = blocks(x, y, 12, 36, [0.5 * (r % 2) for r in range(36)])
    brick = palette(bID, 82, [lin(0x8a4a33), lin(0x9b5b3e), lin(0x6d3828)]) * (0.8 + 0.4 * per_id(bID, 83))[..., None]
    bm = sstep(0.0018, 0.0035, np.minimum(bex, bey))
    under = mixc(lin(0x8d8476), brick, bm * sstep(0.5, 0.56, fbm(x, y, 3, 5, 75)))
    alb = mixc(alb, under * 0.85, spall)
    alb *= (1 - 0.6 * crack)[..., None]
    rough = 0.9 + 0.06 * fbm(x, y, 30, 2, 84)
    return alb, h, rough


def tex_concrete(n):
    """Cast concrete: mottling, bug holes, aggregate, formwork seams + tie holes, rust/water streaks."""
    x, y = grid(n)
    h = 0.8 * fbm(x, y, 64, 3, 91) + 0.5 * fbm(x, y, 8, 3, 92)
    F1, F2, ID, _, _ = voronoi(x, y, 70, 70, 93, jitter=0.9)
    rad = per_id(ID, 94, 1, 0.0, 1.0)
    holes = (F1 < 0.0022 * rad) & (rad > 0.55)
    hole = holes.astype(np.float64)
    hole = np.clip(blur(hole, 0.7) * 1.6, 0, 1)
    h -= hole * 2.0
    seamx = 1 - sstep(0.0008, 0.0022, np.minimum(np.abs(x - 0.0), np.minimum(np.abs(x - 1.0), np.abs(x - 0.5))))
    seamy = 1 - sstep(0.0008, 0.0022, np.minimum(np.abs(y - 0.0), np.abs(y - 1.0)))
    seam = np.maximum(seamx, seamy)
    h += seam * 0.8
    alb = lin(0x85827c) * np.ones((n, n, 1))
    alb *= (0.84 + 0.32 * (0.5 + 0.5 * fbm(x, y, 3, 6, 95)))[..., None]
    alb *= (1 + 0.07 * fbm(x, y, 140, 2, 96))[..., None]
    agg = sstep(0.55, 0.75, perlin(x, y, 110, 110, 97))
    alb = mixc(alb, lin(0xa29c92), agg * 0.35)
    alb *= (1 - 0.55 * hole)[..., None]
    # streaks down from the tie holes (four per tile) and general run-off
    tie = np.zeros((n, n))
    for tx in (0.25, 0.75):
        for ty in (0.3, 0.8):
            d = np.hypot(np.minimum(np.abs(x - tx), 1 - np.abs(x - tx)), np.minimum(np.abs(y - ty), 1 - np.abs(y - ty)))
            tie = np.maximum(tie, 1 - sstep(0.008, 0.011, d))
    h -= tie * 1.5
    run = sstep(0.1, 0.8, fbm(x, y, 1, 4, 98, aniso=(20, 2)))
    alb = mixc(alb, alb * np.array([0.68, 0.66, 0.64]), run * 0.6)
    alb = mixc(alb, lin(0x2a2622), tie * 0.85)
    alb *= (1 - 0.08 * seam)[..., None]
    rough = 0.88 + 0.06 * fbm(x, y, 24, 2, 99) - 0.1 * run
    return alb, h, rough


def _grain(x, y, rows, seed, rings=34.0, warp=0.12):
    w = fbm(x, y, 2, 4, seed, aniso=(1, 3)) * warp
    g = np.sin((y * rows + w * rows) * rings * np.pi / rows * rows / 4 + 3 * fbm(x, y, 1, 3, seed + 1, aniso=(4, 20)))
    fine = fbm(x, y, 2, 3, seed + 2, aniso=(3, 160))
    return g, fine


def tex_planks(n):
    """Worn floor boards / planks (medium wood) with grain, knots, gaps and nails."""
    x, y = grid(n)
    rows = 12
    rng = np.random.default_rng(111)
    offs = [rng.uniform(0, 1) for _ in range(rows)]
    ID, fx, fy, ex, ey = blocks(x, y, 2, rows, offs)
    gap = 0.0016
    face = sstep(gap, gap + 0.0035, np.minimum(ex, ey))
    w = fbm(x, y, 2, 4, 112, aniso=(1, 6)) * 0.03
    pid = per_id(ID, 113, 1, 0, 50)
    ring = np.sin(((y + w) * rows * 9.0 + pid + 2.5 * fbm(x, y, 1, 3, 114, aniso=(3, 12))) * np.pi)
    ring = 0.5 + 0.5 * ring
    fine = fbm(x, y, 2, 3, 115, aniso=(2, 200))
    # knots: radial darkening around a few points (periodic distance)
    knot = np.zeros((n, n))
    for k in range(7):
        kx, ky = rng.uniform(0, 1, 2)
        dx = (x - kx + 0.5) % 1 - 0.5
        dy = ((y - ky + 0.5) % 1 - 0.5) * 3.0
        d = np.hypot(dx, dy)
        knot = np.maximum(knot, 1 - sstep(0.004, 0.02, d))
    base = palette(ID, 116, [lin(0x7b5232), lin(0x6b4428), lin(0x8a5e3a), lin(0x5e3b22)])
    alb = base * (0.82 + 0.36 * per_id(ID, 117))[..., None]
    alb *= (0.8 + 0.3 * ring ** 2)[..., None] * (1 + 0.12 * fine)[..., None]
    alb = mixc(alb, lin(0x2a170c), knot * 0.8)
    wear = sstep(0.2, 0.7, fbm(x, y, 3, 4, 118))
    alb = mixc(alb, alb * np.array([1.12, 1.08, 1.0]), wear * 0.35)
    dirt = sstep(0.4, 0.0, np.minimum(ex, ey) / 0.012)
    alb *= (1 - 0.35 * dirt)[..., None]
    # nails near the plank ends
    nail = np.zeros((n, n))
    for side in (0.035, 0.965):
        for vy in (0.28, 0.72):
            d = np.hypot((fx - side) * (1 / 2) * 1.0, (fy - vy) / rows)
            nail = np.maximum(nail, 1 - sstep(0.0018, 0.0028, d))
    alb = mixc(alb, lin(0x1c1a18), nail * 0.9)
    alb = mixc(lin(0x0d0805) * np.ones((n, n, 1)), alb, face)
    h = face * (2.6 + 0.6 * np.sin(np.pi * fy)) + face * (0.5 * fine + 0.35 * ring) - nail * 0.5 - knot * 0.4
    rough = (0.66 + 0.1 * fine - 0.12 * wear) * face + 0.95 * (1 - face)
    return alb, h, rough


def tex_timber(n):
    """Dark weathered rough-sawn timber (beams, frames, shutters, cornices): deep grain, checks, grey fibres."""
    x, y = grid(n)
    rows = 4
    rng = np.random.default_rng(121)
    offs = [rng.uniform(0, 1) for _ in range(rows)]
    ID, fx, fy, ex, ey = blocks(x, y, 1, rows, offs)
    face = sstep(0.0015, 0.005, ey)
    w = fbm(x, y, 2, 4, 122, aniso=(1, 4)) * 0.05
    pid = per_id(ID, 123, 1, 0, 40)
    ring = 0.5 + 0.5 * np.sin(((y + w) * rows * 14.0 + pid + 3 * fbm(x, y, 1, 3, 124, aniso=(3, 10))) * np.pi)
    fine = fbm(x, y, 2, 4, 125, aniso=(2, 220))
    checks = (1 - sstep(0.0, 0.05, np.abs(fbm(x, y, 1, 3, 126, aniso=(2, 24))))) * sstep(0.2, 0.5, fbm(x, y, 3, 3, 127))
    alb = lin(0x3a2616) * np.ones((n, n, 1))
    alb *= (0.8 + 0.4 * per_id(ID, 128))[..., None]
    alb *= (0.65 + 0.55 * ring ** 1.5)[..., None] * (1 + 0.25 * fine)[..., None]
    grey = sstep(0.0, 0.7, fbm(x, y, 2, 5, 129)) * sstep(0.2, 0.9, 0.5 + 0.5 * fine)
    alb = mixc(alb, lin(0x5a5248), grey * 0.45)
    alb *= (1 - 0.75 * checks)[..., None]
    alb = mixc(lin(0x080503) * np.ones((n, n, 1)), alb, face)
    h = face * 2.4 + face * (0.9 * fine + 0.6 * ring) - checks * 1.6
    rough = (0.74 + 0.12 * fine + 0.08 * grey) * face + 0.95 * (1 - face)
    return alb, h, rough


def tex_metal(n):
    """Cast / wrought iron and worked metal: brushed direction, scratches, smudges, pitting (neutral, tintable)."""
    x, y = grid(n)
    brushed = fbm(x, y, 2, 3, 131, aniso=(1, 128))
    smudge = fbm(x, y, 3, 5, 132)
    scratch = lines(n, 260, 133, (0.02, 0.15)) + 0.6 * lines(n, 160, 134, (0.05, 0.3), angle=0.05)
    scratch = np.clip(scratch, 0, 1)
    pit = sstep(0.6, 0.85, perlin(x, y, 150, 150, 135)) * sstep(0.0, 0.6, fbm(x, y, 4, 3, 136))
    alb = lin(0x8a8a88) * np.ones((n, n, 1))
    alb *= (0.85 + 0.2 * (0.5 + 0.5 * smudge))[..., None] * (1 + 0.05 * brushed)[..., None]
    alb = mixc(alb, alb * 1.35, scratch * 0.6)
    alb *= (1 - 0.5 * pit)[..., None]
    h = 0.25 * brushed - 0.5 * scratch - 0.8 * pit + 0.3 * fbm(x, y, 24, 3, 137)
    rough = 0.42 + 0.18 * (0.5 + 0.5 * smudge) + 0.06 * brushed - 0.12 * scratch + 0.3 * pit
    return alb, h, rough


def tex_rust(n):
    """Rusty painted plate: riveted seams, flaking paint remnants, rust blooms and pitting."""
    x, y = grid(n)
    r1 = fbm(x, y, 3, 6, 141)
    r2 = fbm(x, y, 16, 4, 142)
    rust = lin(0x5b3420) * np.ones((n, n, 1))
    rust = mixc(rust, lin(0x8a4a1e), sstep(-0.2, 0.6, r1) * 0.7)
    rust = mixc(rust, lin(0x2c170c), sstep(0.1, 0.7, -r2) * 0.6)
    rust *= (1 + 0.25 * perlin(x, y, 200, 200, 143))[..., None]
    paintm = sstep(0.08, 0.14, fbm(x, y, 4, 5, 144))
    paint = lin(0x3c4a46) * (0.85 + 0.3 * (0.5 + 0.5 * fbm(x, y, 8, 3, 145)))[..., None]
    alb = mixc(rust, paint, paintm)
    run = sstep(0.2, 0.8, fbm(x, y, 1, 4, 146, aniso=(22, 2)))
    alb = mixc(alb, lin(0x6a3412), run * paintm * 0.55)
    pit = sstep(0.5, 0.8, perlin(x, y, 120, 120, 147)) * (1 - paintm)
    # seams + rivets
    sx = np.minimum(np.abs(x - 0.0), np.abs(x - 1.0))
    sy = np.minimum(np.minimum(np.abs(y), np.abs(y - 1.0)), np.abs(y - 0.5))
    seam = 1 - sstep(0.0015, 0.004, np.minimum(sx, sy))
    riv = np.zeros((n, n))
    for k in range(16):
        c = (k + 0.5) / 16
        for (ax, ay) in ((0.02, c), (0.98, c), (c, 0.02), (c, 0.52), (c, 0.48), (c, 0.98)):
            d = np.hypot((x - ax + 0.5) % 1 - 0.5, (y - ay + 0.5) % 1 - 0.5)
            riv = np.maximum(riv, np.clip(1 - (d / 0.008) ** 2, 0, 1))
    alb *= (1 - 0.5 * seam)[..., None]
    alb *= (1 - 0.45 * pit)[..., None]
    alb = mixc(alb, alb * 1.25, sstep(0.3, 1.0, riv) * 0.4)
    h = paintm * 0.9 + 0.5 * r2 - pit * 1.0 - seam * 1.2 + np.sqrt(riv) * 3.2 + 0.3 * fbm(x, y, 60, 3, 148)
    rough = (0.92 - 0.25 * paintm + 0.05 * r2) * (1 - 0.15 * riv)
    return alb, h, rough


def tex_roof(n):
    """Overlapping natural slate roof (rows of staggered slates, proud lower edges, lichen)."""
    x, y = grid(n)
    cols, rows = 8, 16
    rng = np.random.default_rng(151)
    offs = [0.5 * (r % 2) + rng.uniform(-0.1, 0.1) for r in range(rows)]
    yw = y + 0.002 * fbm(x, y, 30, 2, 152)
    ID, fx, fy, ex, ey = blocks(x, yw, cols, rows, offs)
    side = sstep(0.0015, 0.004, ex - 0.0015 * fbm(x, y, 40, 2, 153))
    # row is overlapped from above: lower edge (fy -> 1, image downwards) is proud
    ramp = fy
    lowedge = 1 - sstep(0.0, 0.08, 1 - fy)
    h = side * (3.0 * ramp) + 0.4 * fbm(x, y, 60, 3, 154) * side - lowedge * 0.0
    jitter = per_id(ID, 155, 1, -0.5, 0.5)
    h += side * jitter * 0.8 * fy
    base = palette(ID, 156, [lin(0x45484f), lin(0x3c3f45), lin(0x4f4d4c), lin(0x393b3d), lin(0x51545c)])
    alb = base * (0.82 + 0.36 * per_id(ID, 157))[..., None]
    alb *= (1 + 0.12 * fbm(x, y, 40, 3, 158, aniso=(1, 0.5)))[..., None]
    shadow = sstep(0.25, 0.0, fy)
    alb *= (1 - 0.55 * shadow)[..., None]
    lich = sstep(0.5, 0.72, fbm(x, y, 10, 5, 159)) * side
    alb = mixc(alb, lin(0x7c7a52), lich * 0.45)
    alb = mixc(lin(0x111214) * np.ones((n, n, 1)), alb, side)
    rough = 0.72 + 0.1 * fbm(x, y, 20, 2, 160) + 0.15 * lich
    return alb, h, rough


def tex_dirt(n):
    """Compacted earth with gravel, pebbles and damp patches."""
    x, y = grid(n)
    base = lin(0x4f4436) * np.ones((n, n, 1))
    damp = sstep(-0.1, 0.5, fbm(x, y, 3, 5, 161))
    alb = base * (0.85 + 0.3 * (0.5 + 0.5 * fbm(x, y, 12, 4, 162)))[..., None]
    alb = mixc(alb, alb * 0.62, damp * 0.6)
    h = 1.2 * fbm(x, y, 16, 4, 163) + 0.6 * fbm(x, y, 96, 2, 164)
    rough = 0.92 - 0.12 * damp
    for (cols, rad, dens, seed, hh) in ((44, 0.0065, 0.65, 165, 2.5), (16, 0.017, 0.35, 166, 4.0), (90, 0.0035, 0.7, 167, 1.4)):
        wx = x + 0.004 * fbm(x, y, 30, 2, seed + 7)
        F1, F2, ID, DX, DY = voronoi(wx, y, cols, cols, seed, jitter=0.9)
        r = per_id(ID, seed + 1, 1, 0.5, 1.0) * rad
        on = per_id(ID, seed + 2) < dens
        st = (1 - sstep(r * 0.75, r, F1)) * on
        dome = np.sqrt(np.clip(1 - (F1 / r) ** 2, 0, 1)) * on
        col = palette(ID, seed + 3, [lin(0x8b857a), lin(0x6e675d), lin(0xa09684), lin(0x5b544c), lin(0x8a7a62)])
        col = col * (0.8 + 0.4 * per_id(ID, seed + 4))[..., None]
        alb = mixc(alb, col, st)
        h = np.maximum(h, h * (1 - st) + dome * hh)
        rough = rough * (1 - st) + 0.7 * st
    return alb, h, rough


def tex_tiles(n):
    """Glazed ceramic tiles with dirty grout, crazing and a few chipped corners."""
    x, y = grid(n)
    cols = rows = 8
    ID, fx, fy, ex, ey = blocks(x, y, cols, rows, [0] * rows)
    ed = np.minimum(ex, ey)
    chipmask = per_id(ID, 171) < 0.25
    corner = np.minimum(np.minimum(fx, 1 - fx), 1) + np.minimum(fy, 1 - fy)
    chip = (corner < 0.12 + 0.05 * fbm(x, y, 40, 2, 172)) & chipmask
    face = sstep(0.0018, 0.0042, ed) * (1 - chip)
    F1, F2, _, _, _ = voronoi(x + 0.01 * fbm(x, y, 8, 2, 173), y, 26, 26, 174, jitter=0.95)
    craze = (1 - sstep(0.0, 0.0012, F2 - F1)) * sstep(0.0, 0.5, fbm(x, y, 3, 3, 175))
    base = lin(0xb6c6c8) * (0.92 + 0.16 * per_id(ID, 176))[..., None]
    alb = base * (1 + 0.04 * fbm(x, y, 16, 3, 177))[..., None]
    alb *= (1 - 0.25 * craze)[..., None]
    grime = sstep(0.0, 0.7, fbm(x, y, 3, 5, 178))
    alb *= (1 - 0.15 * grime)[..., None]
    grout = lin(0x6f6a60) * (0.6 + 0.4 * (0.5 + 0.5 * fbm(x, y, 60, 2, 179)))[..., None] * (1 - 0.4 * grime)[..., None]
    alb = mixc(grout, alb, face)
    h = face * (2.0 + 0.4 * np.sin(np.pi * fx) * np.sin(np.pi * fy)) + 0.1 * fbm(x, y, 64, 2, 180) - craze * 0.2
    rough = (0.16 + 0.12 * grime + 0.2 * craze) * face + 0.9 * (1 - face)
    return alb, h, rough


def tex_wallpaper(n):
    """Faded damask wallpaper in 0.6 m drops: medallion motif, pinstripes, water stains, lifting seams."""
    x, y = grid(n)
    mx, my = 4, 6
    X, Y = x * mx, y * my
    r = np.floor(Y).astype(np.int64)
    X = X + 0.5 * (r % 2)
    lx, ly = X - np.floor(X) - 0.5, Y - np.floor(Y) - 0.5
    ang = np.arctan2(ly, lx)
    rad = np.hypot(lx * 1.0, ly * 1.35)
    petal = 0.30 + 0.07 * np.cos(6 * ang) + 0.03 * np.cos(12 * ang)
    motif = sstep(petal + 0.012, petal - 0.012, rad) * (1 - sstep(0.12, 0.10, rad) * 0.0)
    inner = sstep(0.025, 0.01, np.abs(rad - 0.16 - 0.02 * np.cos(4 * ang)))
    motif = np.clip(motif - inner * 0.8, 0, 1)
    dots = sstep(0.05, 0.03, np.hypot(np.abs(lx) - 0.5, np.abs(ly) - 0.5))
    motif = np.maximum(motif, dots)
    stripe = 0.5 + 0.5 * np.cos(2 * np.pi * x * 48)
    base = lin(0x6f8a7a) * np.ones((n, n, 1))
    alb = mixc(base, base * np.array([1.22, 1.18, 1.05]), motif * 0.55)
    alb *= (1 + 0.04 * stripe)[..., None]
    alb *= (1 + 0.05 * fbm(x, y, 64, 2, 181))[..., None]
    fade = sstep(-0.2, 0.6, fbm(x, y, 2, 5, 182))
    alb = mixc(alb, alb * np.array([1.12, 1.08, 0.88]), fade * 0.45)
    st = fbm(x, y, 3, 5, 183)
    stain = sstep(0.18, 0.3, st)
    ring = sstep(0.04, 0.0, np.abs(st - 0.18))
    alb = mixc(alb, alb * np.array([0.85, 0.75, 0.55]), stain * 0.45)
    alb *= (1 - 0.35 * ring)[..., None]
    seam = np.minimum(np.minimum(np.abs(x), np.abs(x - 1)), np.abs(x - 0.5))
    lift = (1 - sstep(0.0, 0.006, seam)) * sstep(0.2, 0.6, fbm(x, y, 2, 3, 184, aniso=(1, 4)))
    alb = mixc(alb, alb * 1.25, lift * 0.5)
    alb *= (1 - 0.35 * (1 - sstep(0.0, 0.0015, seam)))[..., None]
    h = motif * 0.6 + lift * 1.6 + 0.25 * fbm(x, y, 100, 2, 185)
    rough = 0.82 - 0.18 * motif + 0.08 * stain
    return alb, h, rough


def tex_fabric(n):
    """Heavy canvas (awnings, bunting, upholstery): plain weave with slubs, soft folds, weather fading."""
    x, y = grid(n)
    T = n // 5
    X, Y = x * T, y * T
    fxw, fyw = X - np.floor(X), Y - np.floor(Y)
    cx, cy = np.floor(X).astype(np.int64), np.floor(Y).astype(np.int64)
    over = (cx + cy) % 2 == 0
    slubx = 1 + 0.25 * fbm(x, y, 2, 3, 191, aniso=(64, 1))
    sluby = 1 + 0.25 * fbm(x, y, 2, 3, 192, aniso=(1, 64))
    warp = np.sin(np.pi * fxw) ** 0.7 * slubx
    weft = np.sin(np.pi * fyw) ** 0.7 * sluby
    hw = np.where(over, warp * (0.6 + 0.4 * np.sin(np.pi * fyw)), weft * (0.6 + 0.4 * np.sin(np.pi * fxw)))
    fold = fbm(x, y, 2, 3, 193, aniso=(3, 1))
    h = hw * 1.1 + fold * 3.0
    alb = lin(0x8a8378) * np.ones((n, n, 1))
    alb *= (0.82 + 0.3 * hw)[..., None]
    alb *= (1 + 0.08 * fbm(x, y, 8, 3, 194, aniso=(4, 1)))[..., None]
    fadeM = sstep(-0.2, 0.7, fbm(x, y, 2, 5, 195))
    alb = mixc(alb, alb * 1.18, fadeM * 0.5)
    dirt = sstep(0.3, 0.8, fbm(x, y, 1, 4, 196, aniso=(10, 2)))
    alb *= (1 - 0.2 * dirt)[..., None]
    rough = 0.9 + 0.05 * hw
    return alb, h, rough


# name: (fn, size px, normal strength, ao strength, world tile size m [u, v], families -> documented in render.js)
SETS = {
    "cobble": (tex_cobble, 1024, 1.0, 0.75, [2.0, 2.0]),
    "flagstone": (tex_flagstone, 512, 1.0, 0.6, [2.4, 2.4]),
    "brick": (tex_brick, 1024, 1.0, 0.7, [1.88, 1.8]),
    "plaster": (tex_plaster, 512, 0.9, 0.5, [3.0, 3.0]),
    "concrete": (tex_concrete, 512, 0.8, 0.45, [2.4, 2.4]),
    "planks": (tex_planks, 512, 0.9, 0.6, [2.4, 2.4]),
    "timber": (tex_timber, 512, 1.0, 0.6, [2.0, 2.0]),
    "metal": (tex_metal, 512, 0.6, 0.3, [1.5, 1.5]),
    "rust": (tex_rust, 512, 0.9, 0.5, [2.0, 2.0]),
    "roof": (tex_roof, 512, 1.0, 0.6, [2.4, 2.4]),
    "dirt": (tex_dirt, 512, 1.0, 0.7, [3.0, 3.0]),
    "tiles": (tex_tiles, 512, 0.9, 0.55, [2.4, 2.4]),
    "wallpaper": (tex_wallpaper, 512, 0.6, 0.35, [1.2, 1.2]),
    "fabric": (tex_fabric, 512, 0.8, 0.35, [1.0, 1.0]),
}


def finish(alb, h, rough, nstr, ao_str):
    """Normal map from height (circular differences), cavity AO + edge wear baked into albedo."""
    n = h.shape[0]
    dx = (np.roll(h, -1, 1) - np.roll(h, 1, 1)) * 0.5 * nstr
    dy = (np.roll(h, -1, 0) - np.roll(h, 1, 0)) * 0.5 * nstr
    nx, ny, nz = -dx, dy, np.ones_like(h)  # +Y = up the image (row index decreases)
    ln = np.sqrt(nx * nx + ny * ny + nz * nz)
    nx, ny = nx / ln, ny / ln
    s = n / 512.0
    cav = np.clip((blur(h, 2.5 * s) - h) * 0.35 + (blur(h, 9 * s) - h) * 0.12, 0, 1)
    ao = 1 - ao_str * np.clip(cav, 0, 1) ** 0.8
    edge = np.clip((h - blur(h, 1.5 * s)) * 0.5, 0, 1)
    alb = alb * ao[..., None] * (1 + 0.25 * edge)[..., None]
    alb = np.clip(alb, 0.004, 1)
    rough = np.clip(rough + 0.08 * (1 - ao), 0.03, 1)
    return alb, nx, ny, rough


def save_jpeg(path, arr, quality, subsampling):
    from PIL import Image
    Image.fromarray(np.clip(arr * 255 + 0.5, 0, 255).astype(np.uint8)).save(path, quality=quality, optimize=True,
                                                                          progressive=True, subsampling=subsampling)


def bake_set(name, preview):
    fn, size, nstr, ao_str, tile = SETS[name]
    alb, h, rough = fn(size)
    alb, nx, ny, rough = finish(alb, h, rough, nstr * size / 512.0 * 0.5 + nstr * 0.5, ao_str)
    save_jpeg(os.path.join(OUT, name + "_a.jpg"), to_srgb(alb), 84, 2)
    nrm = np.stack([nx * 0.5 + 0.5, ny * 0.5 + 0.5, rough], -1)
    save_jpeg(os.path.join(OUT, name + "_n.jpg"), nrm, 88, 0)
    mean = alb.reshape(-1, 3).mean(0)
    info = {"size": size, "tile": tile, "mean": [round(float(v), 4) for v in mean],
            "rough": round(float(rough.mean()), 4)}
    if preview is not None:
        # lit preview, 2x2 tiled (seams visible if any)
        L = np.array([-0.5, 0.6, 0.62])
        L /= np.linalg.norm(L)
        nz = np.sqrt(np.clip(1 - nx * nx - ny * ny, 0, 1))
        shade = np.clip(nx * L[0] + ny * L[1] + nz * L[2], 0, 1)
        img = to_srgb(alb * (0.25 + 0.95 * shade)[..., None])
        tiled = np.tile(img, (2, 2, 1))
        preview[name] = tiled
    return info


# =====================================================================================================================
# sky environments
# =====================================================================================================================
SKY_W, SKY_H = 256, 128
# sun/moon directions match render.js R.LOOK[].sunPos (three.js world, y up)
SUN = {"past": (-40, 22, 20), "present": (30, 18, -40), "future": (10, 60, -20)}


def sky_dirs(w, h):
    """Unit directions per pixel in the three.js equirect convention (row 0 = top = +y)."""
    u = (np.arange(w) + 0.5) / w
    v = 1 - (np.arange(h) + 0.5) / h
    U, V = np.meshgrid(u, v)
    phi = (U - 0.5) * 2 * np.pi
    lat = (V - 0.5) * np.pi
    return np.cos(lat) * np.cos(phi), np.sin(lat), np.cos(lat) * np.sin(phi)


def render_physical_sky(elev_deg, w=SKY_W * 2, h=SKY_H * 2, air=1.0, dust=1.0, ozone=1.0, sun_int=1.0, samples=4):
    """Render Blender's physical sky (multiple scattering model; Nishita successor) to an equirect float array, sun at
    azimuth 0 of the render. Returns HxWx3 float32 (row 0 = top) and the pixel column of the sun."""
    import bpy
    scn = bpy.context.scene
    scn.render.engine = "CYCLES"
    scn.cycles.samples = samples
    scn.cycles.device = "CPU"
    scn.render.resolution_x, scn.render.resolution_y, scn.render.resolution_percentage = w, h, 100
    scn.render.film_transparent = False
    scn.view_settings.view_transform = "Standard"
    world = bpy.data.worlds.get("AZSky") or bpy.data.worlds.new("AZSky")
    scn.world = world
    world.use_nodes = True
    nt = world.node_tree
    nt.nodes.clear()
    sky = nt.nodes.new("ShaderNodeTexSky")
    for t in ("MULTIPLE_SCATTERING", "NISHITA", "SINGLE_SCATTERING"):
        try:
            sky.sky_type = t
            break
        except TypeError:
            continue
    sky.sun_elevation = math.radians(elev_deg)
    sky.sun_rotation = 0.0
    sky.air_density, sky.aerosol_density, sky.ozone_density = air, dust, ozone
    sky.sun_intensity = sun_int
    sky.sun_disc = True
    bg = nt.nodes.new("ShaderNodeBackground")
    out = nt.nodes.new("ShaderNodeOutputWorld")
    nt.links.new(sky.outputs[0], bg.inputs[0])
    nt.links.new(bg.outputs[0], out.inputs[0])
    for o in list(scn.objects):
        bpy.data.objects.remove(o, do_unlink=True)
    cam = bpy.data.cameras.new("AZPano")
    cam.type = "PANO"
    cam.panorama_type = "EQUIRECTANGULAR"
    co = bpy.data.objects.new("AZPano", cam)
    scn.collection.objects.link(co)
    co.rotation_euler = (math.radians(90), 0, 0)
    scn.camera = co
    path = os.path.join(OUT, "_sky_tmp.exr")
    scn.render.image_settings.file_format = "OPEN_EXR"
    scn.render.image_settings.color_depth = "32"
    scn.render.filepath = path
    bpy.ops.render.render(write_still=True)
    img = bpy.data.images.load(path)
    px = np.empty(w * h * 4, np.float32)
    img.pixels.foreach_get(px)
    bpy.data.images.remove(img)
    os.remove(path)
    a = px.reshape(h, w, 4)[::-1, :, :3].astype(np.float64)  # Blender rows start at the bottom
    lum = a @ np.array([0.2126, 0.7152, 0.0722])
    col = int(np.argmax(lum.max(0)))
    return a, col


def place_sun(img, col, sun):
    """Roll the rendered sky horizontally so its sun sits at the three.js azimuth of `sun`, then downsample 2x."""
    x, y, z = sun
    u = math.atan2(z, x) / (2 * math.pi) + 0.5
    w = img.shape[1]
    img = np.roll(img, int(round(u * w)) - col, axis=1)
    return img.reshape(img.shape[0] // 2, 2, w // 2, 2, 3).mean((1, 3))


def sphere_noise(d, scale, octaves, seed):
    """Seamless noise on the sphere: periodic 2D noise sampled through a cube-ish projection blend."""
    dx, dy, dz = d
    acc = 0
    wsum = 0
    for (a, b, wgt) in ((dx, dz, np.abs(dy) ** 4), (dx, dy, np.abs(dz) ** 4), (dz, dy, np.abs(dx) ** 4)):
        acc = acc + fbm((a * 0.5 + 0.5) % 1, (b * 0.5 + 0.5) % 1, scale, octaves, seed) * wgt
        wsum = wsum + wgt
    return acc / wsum


def clouds_on_plane(d, scale, octaves, seed, height=0.15):
    """Cloud-deck coordinates: project directions onto a plane above the viewer (like the in-game sky dome)."""
    dx, dy, dz = d
    k = 1.0 / (np.maximum(dy, 0) + height)
    u = (dx * k * 0.08) % 1
    v = (dz * k * 0.08) % 1
    return fbm(u, v, scale, octaves, seed)


def bake_skies():
    d = sky_dirs(SKY_W, SKY_H)
    dx, dy, dz = d
    up = np.clip(dy, 0, 1)
    below = dy < 0
    out = {}

    # ---- PAST: physical dusk sky, sun low over the roofs, thin warm cloud streaks ----
    try:
        img, col = render_physical_sky(6.0, air=1.2, dust=3.0, ozone=1.0)
        past = place_sun(img, col, SUN["past"])
    except Exception as e:  # pragma: no cover - fallback when Cycles is unavailable
        print("physical sky render failed, using analytic fallback:", e)
        past = np.zeros((SKY_H, SKY_W, 3)) + lin(0xc39a7c)
    past = past / max(1e-6, np.percentile(past[~below], 60)) * 0.9
    sd = np.array(SUN["past"], float)
    sd /= np.linalg.norm(sd)
    cosS = dx * sd[0] + dy * sd[1] + dz * sd[2]
    cl = clouds_on_plane(d, 4, 5, 501)
    cov = sstep(0.05, 0.45, cl) * sstep(0.0, 0.12, dy) * 0.55
    cloud_col = lin(0x8a6a74) * 1.2 + lin(0xffb070)[None, None, :] * (np.clip(cosS, 0, 1) ** 4 * 3.0)[..., None]
    past = mixc(past, cloud_col, cov)
    ground = lin(0x3a2814) * 0.35 + lin(0xc89a66) * 0.25 * np.exp(dy * 6)[..., None]
    past = np.where(below[..., None], ground, past)
    out["past"] = past

    # ---- PRESENT: blue-hour physical sky under a low rain deck, lit from below by sodium city glow ----
    try:
        img, col = render_physical_sky(-4.0, air=1.0, dust=2.0, ozone=2.0)
        base = place_sun(img, col, SUN["present"])
        base = base / max(1e-6, np.percentile(base[~below], 95)) * 0.10
    except Exception as e:  # pragma: no cover
        print("physical sky render failed, using analytic fallback:", e)
        base = np.zeros((SKY_H, SKY_W, 3)) + lin(0x1c2a38) * 0.15
    deck = 0.5 + 0.5 * clouds_on_plane(d, 3, 6, 601, height=0.08)
    deck2 = 0.5 + 0.5 * clouds_on_plane(d, 8, 4, 602, height=0.08)
    glow = np.exp(-np.clip(dy, 0, 1) * 7.0)
    city = lin(0xff9a48) * 0.55 + lin(0xffc68a) * 0.15
    cloudcol = lin(0x2a3644) * (0.10 + 0.10 * deck)[..., None] + city * (glow * (0.25 + 0.55 * deck2))[..., None]
    present = mixc(base, cloudcol, np.clip(0.85 + 0.15 * deck, 0, 1) * sstep(-0.02, 0.05, dy))
    # moon behind the deck: soft bright patch
    md = np.array(SUN["present"], float)
    md /= np.linalg.norm(md)
    cosM = dx * md[0] + dy * md[1] + dz * md[2]
    present += lin(0x9ab6d8) * (np.clip(cosM, 0, 1) ** 60 * 0.6 * (1.2 - deck))[..., None]
    # horizon: lit city haze
    present += city * (np.exp(-np.abs(dy) * 18) * 0.45)[..., None]
    groundP = lin(0x0c1014) * 0.4 + city * (0.12 * np.exp(dy * 8))[..., None]
    present = np.where(below[..., None], groundP, present)
    out["present"] = present

    # ---- FUTURE: alien violet atmosphere, aurora curtains, pale second moon, stars ----
    try:
        img, col = render_physical_sky(12.0, air=4.0, dust=0.5, ozone=6.0)
        base = place_sun(img, col, SUN["future"])
        base = base / max(1e-6, np.percentile(base[~below], 70)) * 0.12
        l = base @ np.array([0.2126, 0.7152, 0.0722])
        base = l[..., None] * lin(0x9a60ff) * 2.2 + base * 0.25
    except Exception as e:  # pragma: no cover
        print("physical sky render failed, using analytic fallback:", e)
        base = np.zeros((SKY_H, SKY_W, 3)) + lin(0x1e0e3a) * 0.2
    zen = lin(0x07030f) * 0.3
    future = mixc(base, zen * np.ones_like(base), sstep(0.2, 0.9, dy) * 0.7)
    band = sphere_noise(d, 3, 4, 701)
    aur = np.exp(-((dy - 0.3 - 0.12 * band) ** 2) / 0.006) * sstep(0.2, 0.6, 0.5 + 0.5 * sphere_noise(d, 6, 3, 702))
    future += (lin(0x55f0ff) * 0.6 + lin(0xff40c8) * 0.25)[None, None, :] * (aur * 0.9)[..., None]
    fd = np.array(SUN["future"], float)
    fd /= np.linalg.norm(fd)
    cosF = dx * fd[0] + dy * fd[1] + dz * fd[2]
    future += lin(0x8a70ff) * (np.clip(cosF, 0, 1) ** 400 * 6.0 + np.clip(cosF, 0, 1) ** 12 * 0.25)[..., None]
    # second moon
    m2 = np.array([-0.6, 0.35, 0.5])
    m2 /= np.linalg.norm(m2)
    cos2 = dx * m2[0] + dy * m2[1] + dz * m2[2]
    future += lin(0xffd0f0) * (sstep(0.9990, 0.9993, cos2) * 2.5 + np.clip(cos2, 0, 1) ** 80 * 0.3)[..., None]
    rng = np.random.default_rng(703)
    stars = (rng.random((SKY_H, SKY_W)) > 0.994) * rng.uniform(0.3, 2.0, (SKY_H, SKY_W)) * sstep(0.05, 0.3, dy)
    future += stars[..., None] * np.array([0.8, 0.8, 1.0])
    future += (lin(0xff40c8) * 0.2 + lin(0x9a60ff) * 0.2) * np.exp(-np.abs(dy) * 14)[..., None]
    groundF = lin(0x05030a) * 0.5 + lin(0x6a46d8) * (0.08 * np.exp(dy * 8))[..., None]
    future = np.where(below[..., None], groundF, future)
    out["future"] = future

    for k, v in out.items():
        write_hdr(os.path.join(OUT, "sky_" + k + ".hdr"), np.clip(v, 0, 65000))
    return out


def write_hdr(path, img):
    """Radiance RGBE, flat (uncompressed) scanlines, top row first."""
    h, w, _ = img.shape
    m = img.max(-1)
    e = np.zeros_like(m, np.int64)
    mant = np.zeros_like(m)
    ok = m > 1e-32
    mant[ok], e[ok] = np.frexp(m[ok])
    scale = np.where(ok, mant * 256.0 / np.maximum(m, 1e-32), 0)
    rgbe = np.zeros((h, w, 4), np.uint8)
    rgbe[..., :3] = np.clip(img * scale[..., None], 0, 255).astype(np.uint8)
    rgbe[..., 3] = np.where(ok, e + 128, 0).astype(np.uint8)
    with open(path, "wb") as f:
        f.write(b"#?RADIANCE\nFORMAT=32-bit_rle_rgbe\n\n-Y %d +X %d\n" % (h, w))
        f.write(rgbe.tobytes())


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--only", default="")
    ap.add_argument("--no-sky", action="store_true")
    ap.add_argument("--no-tex", action="store_true")
    ap.add_argument("--preview", default="")
    args = ap.parse_args([a for a in sys.argv[1:]])
    os.makedirs(OUT, exist_ok=True)
    man_path = os.path.join(OUT, "manifest.json")
    manifest = json.load(open(man_path)) if os.path.exists(man_path) else {"sets": {}}
    names = [s for s in args.only.split(",") if s] or list(SETS)
    preview = {} if args.preview else None
    if not args.no_tex:
        for nm in names:
            manifest["sets"][nm] = bake_set(nm, preview)
            print("baked", nm, manifest["sets"][nm])
    if not args.no_sky:
        bake_skies()
        manifest["skies"] = ["sky_past.hdr", "sky_present.hdr", "sky_future.hdr"]
        print("baked skies")
    with open(man_path, "w") as f:
        json.dump(manifest, f, indent=1)
    if preview:
        from PIL import Image
        tiles = []
        for nm, img in preview.items():
            im = Image.fromarray((np.clip(img, 0, 1) * 255).astype(np.uint8)).resize((384, 384))
            tiles.append(im)
        cols = 4
        rows = (len(tiles) + cols - 1) // cols
        sheet = Image.new("RGB", (cols * 384, rows * 384))
        for i, im in enumerate(tiles):
            sheet.paste(im, ((i % cols) * 384, (i // cols) * 384))
        sheet.save(args.preview)
    total = sum(os.path.getsize(os.path.join(OUT, f)) for f in os.listdir(OUT))
    print("assets/textures total: %.2f MB" % (total / 1e6))


if __name__ == "__main__":
    main()
