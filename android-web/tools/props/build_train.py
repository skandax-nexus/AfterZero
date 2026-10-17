"""AFTERZERO - Prologue train carriage ("The Last Train"): modelled in code, lightmap-baked in Cycles, exported as glTF.

Run with Blender's Python module (bpy 4.x/5.x, numpy + Pillow):
    <blender-venv>/bin/python tools/props/build_train.py [--samples 160] [--res 2048] [--fast] [--no-bake]

Outputs (assets/props/):
    train_carriage.glb    nodes (one primitive per material each):
                            interior   static interior, TEXCOORD_0 = tiling texture coords, TEXCOORD_1 = lightmap atlas
                            pendants   hanging ceiling lamps (sway in a vertex shader; brass parts lightmapped)
                            doorL/R    front sliding door leaves (baked closed; slide into the wall pockets)
                            panes      window glass (rain-on-glass shader at runtime, no lightmap)
                            exterior   body shell, roof, underframe, bogies, buffers (lit at runtime, seen from the platform)
                            coat/watch/letter   the prologue's clue props, baked in place
                          scene extras: lightmap scale, uv0 packing, material table, layout anchors (seats, lamps, windows)
    train_lightmap.jpg    baked diffuse irradiance (direct + indirect, no albedo), sRGB-encoded, value = jpeg * scale
    train_wood.jpg, train_moquette.jpg, train_velvet.jpg, train_carpet.jpg, train_ceiling.jpg, train_decals.jpg
                          tiling albedo textures (cavity shading baked in) and the decal atlas (signs, posters, clock)

Game space (three.js): x across the carriage, y up, z along it; the exit door is at +z. Floor top y = 0.
The level (js/levels/p00_prologue.js) adds colliders matching this layout; keep the two in sync (anchors in extras).

Lighting: warm 2700 K pendants over the aisle + tulip sconces on every window pier, cool night sky through the windows.
The runtime material (js/props.js) uses the baked irradiance for all diffuse light and keeps only specular from the
game's pooled lights / IBL, so the carriage looks the same on every quality tier at the cost of one texture fetch.
"""
import argparse
import json
import math
import os
import shutil
import struct
import subprocess
import sys
import time

import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
OUT = os.path.join(ROOT, "assets", "props")
sys.path.insert(0, os.path.join(HERE, "..", "textures"))
from bake_textures import grid, fbm, blur, sstep, lin, to_srgb, mixc  # noqa: E402

# =====================================================================================================================
# layout (metres)
# =====================================================================================================================
W = 1.60            # inner wall face |x|
WO = 1.78           # outer skin |x|
WG = 1.69           # glass plane |x|
A = 0.52            # aisle half width (bench end panels start here)
ZB, ZF = -10.90, 10.85     # inner faces of the back / front end walls
ZBO, ZFO = -11.15, 11.10   # outer faces
BAYS = [-8 + 2 * i for i in range(9)]       # bay centres (facing benches)
PARTS = [-9 + 2 * i for i in range(10)]     # back-to-back partitions
WIN_Y0, WIN_Y1 = 1.00, 1.92                 # window opening
WINDOWS = [(zb, 0.62) for zb in BAYS] + [(-10.0, 0.32), (9.93, 0.32)]
FRIEZE = 2.26
CROWN = 2.86
DOOR_HW, DOOR_H = 0.70, 2.30                # front (exit) door opening
DOOR_Z0, DOOR_Z1 = 10.955, 10.995           # sliding leaf thickness (inside the wall pocket)
BDOOR_HW, BDOOR_H = 0.40, 2.05              # back (communicating) door
SEAT_Y = 0.466
WARM = (1.0, 0.60, 0.30)                    # 2700 K-ish lamp colour (linear)
WARM_SCONCE = (1.0, 0.55, 0.26)

# material table: tex = tiling albedo (tile m), tint multiplies it; baked = has a lightmap chart
MATS = {
    "wood": dict(tex="wood", tile=0.9, tint=(1, 1, 1), rough=0.32),
    "woodDark": dict(tex="wood", tile=1.6, tint=(0.36, 0.33, 0.32), rough=0.5),
    "moquette": dict(tex="moquette", tile=0.32, tint=(1, 1, 1), rough=0.92),
    "velvet": dict(tex="velvet", tile=0.5, tint=(1, 1, 1), rough=0.75),
    "carpet": dict(tex="carpet", tile=1.0, tint=(1, 1, 1), rough=0.95),
    "ceiling": dict(tex="ceiling", tile=0.6, tint=(1, 1, 1), rough=0.55),
    "decals": dict(tex="decals", tile=1.0, tint=(1, 1, 1), rough=0.45),
    "brass": dict(color=(0.80, 0.56, 0.24), rough=0.28, metal=1.0, bake=(0.55, 0.40, 0.18)),
    "iron": dict(color=(0.030, 0.030, 0.034), rough=0.4, metal=0.6),
    "leather": dict(color=(0.13, 0.055, 0.03), rough=0.42),
    "frost": dict(tex="decals", tile=1.0, tint=(1, 1, 1), rough=0.1, emissive=(0.012, 0.010, 0.008)),
    "wool": dict(color=(0.026, 0.028, 0.040), rough=0.95),
    "lampGlass": dict(color=(1.0, 0.70, 0.40), baked=False, emissive=(1.0, 0.70, 0.40), kind="lamp"),
    "glass": dict(color=(0.6, 0.7, 0.75), baked=False, kind="glass"),
    "liveryG": dict(color=(0.022, 0.06, 0.04), rough=0.3, baked=False, kind="lit"),
    "liveryC": dict(color=(0.50, 0.40, 0.26), rough=0.35, baked=False, kind="lit"),
    "roof": dict(color=(0.025, 0.026, 0.03), rough=0.65, baked=False, kind="lit"),
    "under": dict(color=(0.012, 0.012, 0.014), rough=0.6, metal=0.3, baked=False, kind="lit"),
    "gold": dict(color=(0.75, 0.52, 0.2), rough=0.3, metal=1.0, baked=False, kind="lit"),
}
for _k, _m in MATS.items():
    _m.setdefault("baked", True)
    _m.setdefault("metal", 0.0)
    _m.setdefault("rough", 0.6)
    _m.setdefault("kind", "baked")

UV0_SCALE = 64.0   # TEXCOORD_0 is stored as uv / 64 + 0.5 (fits [0,1] -> clean 16-bit quantisation)


# =====================================================================================================================
# geometry kernel: patches = triangle sets with per-vertex normals, tiling UV (metres) and a 2D lightmap chart (metres)
# =====================================================================================================================
def _n(v):
    v = np.asarray(v, float)
    return v / np.maximum(np.linalg.norm(v, axis=-1, keepdims=True), 1e-12)


class Geo:
    def __init__(self):
        self.patches = []
        self.node = "interior"
        self._seed = 0

    def add(self, mat, P, N, C, T, UV=None, dens=1.0, uvoff=True):
        P = np.asarray(P, float)
        N = _n(np.asarray(N, float))
        C = np.asarray(C, float)
        T = np.asarray(T, np.int64).reshape(-1, 3)
        UV = C.copy() if UV is None else np.asarray(UV, float)
        if uvoff and MATS[mat].get("tex") not in (None, "decals", "carpet"):
            self._seed += 1
            UV = UV + np.array([(self._seed * 0.618034) % 1.0, (self._seed * 0.381966) % 1.0]) * MATS[mat]["tile"] * 3
        if len(T) == 0:
            return None
        p = dict(node=self.node, mat=mat, P=P, N=N, C=C, T=T, UV=UV, dens=dens)
        self.patches.append(p)
        return p

    def mark(self):
        return len(self.patches)

    def mirror(self, start, sx=1, sz=1, dx=0.0, dz=0.0):
        for p in self.patches[start:]:
            P, N = p["P"], p["N"]
            p["P"] = np.stack([sx * P[:, 0] + dx, P[:, 1], sz * P[:, 2] + dz], 1)
            p["N"] = np.stack([sx * N[:, 0], N[:, 1], sz * N[:, 2]], 1)

    def rotate(self, start, axis, ang, pivot):
        R = rotmat(axis, ang)
        pv = np.asarray(pivot, float)
        for p in self.patches[start:]:
            p["P"] = (p["P"] - pv) @ R.T + pv
            p["N"] = p["N"] @ R.T


def rotmat(axis, a):
    x, y, z = _n(axis)
    c, s, C = math.cos(a), math.sin(a), 1 - math.cos(a)
    return np.array([[c + x * x * C, x * y * C - z * s, x * z * C + y * s],
                     [y * x * C + z * s, c + y * y * C, y * z * C - x * s],
                     [z * x * C - y * s, z * y * C + x * s, c + z * z * C]])


def fix_winding(p):
    P, N, T = p["P"], p["N"], p["T"]
    g = np.cross(P[T[:, 1]] - P[T[:, 0]], P[T[:, 2]] - P[T[:, 0]])
    nv = N[T[:, 0]] + N[T[:, 1]] + N[T[:, 2]]
    bad = (g * nv).sum(1) < 0
    T[bad] = T[bad][:, [0, 2, 1]]


def newell(P):
    n = np.zeros(3)
    for i in range(len(P)):
        a, b = P[i], P[(i + 1) % len(P)]
        n += [(a[1] - b[1]) * (a[2] + b[2]), (a[2] - b[2]) * (a[0] + b[0]), (a[0] - b[0]) * (a[1] + b[1])]
    return _n(n)


def earclip(C):
    n = len(C)
    idx = list(range(n))
    area = 0.5 * sum(C[i, 0] * C[(i + 1) % n, 1] - C[(i + 1) % n, 0] * C[i, 1] for i in range(n))
    if area < 0:
        idx.reverse()
    tris = []

    def inside(p, a, b, c):
        d1 = (p[0] - b[0]) * (a[1] - b[1]) - (a[0] - b[0]) * (p[1] - b[1])
        d2 = (p[0] - c[0]) * (b[1] - c[1]) - (b[0] - c[0]) * (p[1] - c[1])
        d3 = (p[0] - a[0]) * (c[1] - a[1]) - (c[0] - a[0]) * (p[1] - a[1])
        neg = (d1 < -1e-12) or (d2 < -1e-12) or (d3 < -1e-12)
        pos = (d1 > 1e-12) or (d2 > 1e-12) or (d3 > 1e-12)
        return not (neg and pos)

    guard = 0
    while len(idx) > 3 and guard < 20000:
        guard += 1
        m = len(idx)
        done = False
        for k in range(m):
            i0, i1, i2 = idx[k - 1], idx[k], idx[(k + 1) % m]
            a, b, c = C[i0], C[i1], C[i2]
            cr = (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])
            if cr <= 1e-12:
                continue
            if any(inside(C[j], a, b, c) for j in idx if j not in (i0, i1, i2)
                   and not (np.allclose(C[j], a) or np.allclose(C[j], b) or np.allclose(C[j], c))):
                continue
            tris.append((i0, i1, i2))
            idx.pop(k)
            done = True
            break
        if not done:  # drop a collinear vertex, else give up
            for k in range(m):
                i0, i1, i2 = idx[k - 1], idx[k], idx[(k + 1) % m]
                a, b, c = C[i0], C[i1], C[i2]
                if abs((b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])) < 1e-10:
                    idx.pop(k)
                    done = True
                    break
            if not done:
                break
    if len(idx) == 3:
        tris.append(tuple(idx))
    return tris


def world_uv(P, n):
    a = np.abs(n)
    if a[1] >= a[0] and a[1] >= a[2]:
        return np.stack([P[:, 0], P[:, 2]], 1)
    if a[0] >= a[2]:
        return np.stack([P[:, 2], -P[:, 1]], 1)
    return np.stack([P[:, 0], -P[:, 1]], 1)


def poly(G, mat, pts, dens=1.0, uv=None, want=None):
    """Planar polygon; `want` = desired facing direction (points are reversed if needed)."""
    P = np.asarray(pts, float)
    n = newell(P)
    if want is not None and np.dot(n, want) < 0:
        P = P[::-1].copy()
        n = -n
    e1 = _n(P[1] - P[0])
    e2 = np.cross(n, e1)
    C = np.stack([(P - P[0]) @ e1, (P - P[0]) @ e2], 1)
    T = earclip(C)
    if uv == "world":
        UV = world_uv(P, n)
    elif callable(uv):
        UV = uv(P)
    else:
        UV = None
    return G.add(mat, P, np.tile(n, (len(P), 1)), C, T, UV, dens, uvoff=not callable(uv))


def quad(G, mat, pts, dens=1.0, uv="long", want=None):
    pts = [np.asarray(p, float) for p in pts]
    if uv == "long":
        if np.linalg.norm(pts[2] - pts[1]) > np.linalg.norm(pts[1] - pts[0]):
            pts = pts[1:] + pts[:1]
        uv = None
    return poly(G, mat, pts, dens, uv, want)


def box(G, mat, x0, x1, y0, y1, z0, z1, dens=1.0, skip=(), dd=None, uv="long"):
    F = {
        "+x": [(x1, y0, z1), (x1, y0, z0), (x1, y1, z0), (x1, y1, z1)],
        "-x": [(x0, y0, z0), (x0, y0, z1), (x0, y1, z1), (x0, y1, z0)],
        "+y": [(x0, y1, z1), (x1, y1, z1), (x1, y1, z0), (x0, y1, z0)],
        "-y": [(x0, y0, z0), (x1, y0, z0), (x1, y0, z1), (x0, y0, z1)],
        "+z": [(x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)],
        "-z": [(x1, y0, z0), (x0, y0, z0), (x0, y1, z0), (x1, y1, z0)],
    }
    for k, pts in F.items():
        if k in skip:
            continue
        quad(G, mat, pts, dens * ((dd or {}).get(k, 1.0)), uv)


def _prof_normals(Q, closed, smooth_deg):
    """Per-segment start/end normals (right of travel), smoothed across joints below smooth_deg."""
    m = len(Q)
    segs = m if closed else m - 1
    tn = []
    for k in range(segs):
        d = Q[(k + 1) % m] - Q[k]
        L = np.linalg.norm(d)
        tn.append(np.array([d[1], -d[0]]) / max(L, 1e-12))
    cs = math.cos(math.radians(smooth_deg))
    out = []
    for k in range(segs):
        n = tn[k]
        prev = tn[k - 1] if (closed or k > 0) else None
        nxt = tn[(k + 1) % segs] if (closed or k < segs - 1) else None
        n0 = _n(n + prev) if prev is not None and np.dot(n, prev) > cs else n
        n1 = _n(n + nxt) if nxt is not None and np.dot(n, nxt) > cs else n
        out.append((n0, n1))
    return out


def _orient_closed(Q):
    Q = np.asarray(Q, float)
    a = 0.5 * np.sum(Q[:, 0] * np.roll(Q[:, 1], -1) - np.roll(Q[:, 0], -1) * Q[:, 1])
    return Q if a > 0 else Q[::-1].copy()


def sweep(G, mat, prof, a, b, side, up, dens=1.0, closed=False, caps=False, smooth=40, toward=None, uvscale=1.0):
    """Profile prof [(s, t)] (s along `side`, t along `up`) extruded along the straight segment a -> b."""
    Q = _orient_closed(prof) if closed else np.asarray(prof, float)
    a, b = np.asarray(a, float), np.asarray(b, float)
    side, up = _n(side), _n(up)
    L = np.linalg.norm(b - a)
    nrm = _prof_normals(Q, closed, smooth)
    m = len(Q)
    segs = m if closed else m - 1
    P, N, C, T = [], [], [], []
    arc = 0.0
    for k in range(segs):
        q0, q1 = Q[k], Q[(k + 1) % m]
        sl = np.linalg.norm(q1 - q0)
        if sl < 1e-9:
            continue
        (n0, n1) = nrm[k]
        base = len(P)
        for (q, n, v) in ((q0, n0, arc), (q1, n1, arc + sl)):
            for t in (0.0, 1.0):
                P.append(a + (b - a) * t + side * q[0] + up * q[1])
                N.append(side * n[0] + up * n[1])
                C.append((t * L, v))
        # vertices: base+0 (q0,a) base+1 (q0,b) base+2 (q1,a) base+3 (q1,b)
        T += [(base, base + 2, base + 3), (base, base + 3, base + 1)]
        arc += sl
    P, N = np.array(P), np.array(N)
    if toward is not None and not closed:
        if np.sum(((np.asarray(toward) - P) * N).sum(1)) < 0:
            N = -N
    pt = G.add(mat, P, N, np.array(C), T, np.array(C) * uvscale, dens)
    if closed and caps:
        d = _n(b - a)
        for end, want in ((a, -d), (b, d)):
            poly(G, mat, [end + side * q[0] + up * q[1] for q in Q], dens, None, want)
    return pt


def tube(G, mat, prof, path, binormal, dens=1.0, closed=True, smooth=50, toward=None):
    """Profile [(s, t)] swept along a polyline; s along `binormal`, t along the in-plane normal (sign: toward)."""
    Q = _orient_closed(prof) if closed else np.asarray(prof, float)
    path = [np.asarray(p, float) for p in path]
    B = _n(binormal)
    frames = []
    for i in range(len(path)):
        tg = _n(path[min(i + 1, len(path) - 1)] - path[max(i - 1, 0)])
        nn = _n(np.cross(tg, B))
        if toward is not None and np.dot(nn, np.asarray(toward) - path[i]) < 0:
            nn = -nn
        frames.append((B, nn))
    plen = [0.0]
    for i in range(1, len(path)):
        plen.append(plen[-1] + np.linalg.norm(path[i] - path[i - 1]))
    nrm = _prof_normals(Q, closed, smooth)
    m = len(Q)
    segs = m if closed else m - 1
    P, N, C, T = [], [], [], []
    arc = 0.0
    for k in range(segs):
        q0, q1 = Q[k], Q[(k + 1) % m]
        sl = np.linalg.norm(q1 - q0)
        if sl < 1e-9:
            continue
        (n0, n1) = nrm[k]
        base = len(P)
        for i, p in enumerate(path):
            bb, nn = frames[i]
            for (q, n, v) in ((q0, n0, arc), (q1, n1, arc + sl)):
                P.append(p + bb * q[0] + nn * q[1])
                N.append(bb * n[0] + nn * n[1])
                C.append((plen[i], v))
        for i in range(len(path) - 1):
            r0, r1 = base + 2 * i, base + 2 * (i + 1)
            T += [(r0, r0 + 1, r1 + 1), (r0, r1 + 1, r1)]
        arc += sl
    P, N = np.array(P), np.array(N)
    if toward is not None and not closed and np.sum(((np.asarray(toward) - P) * N).sum(1)) < 0:
        N = -N
    return G.add(mat, P, N, np.array(C), T, None, dens)


def lathe(G, mat, prof, c, axis=(0, 1, 0), seg=16, dens=1.0, smooth=50, ref=None):
    """Profile [(r, h)] revolved about `axis` through c. Outward = right of travel (list outer surfaces bottom->top)."""
    Q = np.asarray(prof, float)
    c = np.asarray(c, float)
    ax = _n(axis)
    e1 = _n(np.cross(ax, ref if ref is not None else ((1, 0, 0) if abs(ax[0]) < 0.9 else (0, 0, 1))))
    e2 = np.cross(ax, e1)
    rmax = max(1e-3, Q[:, 0].max())
    nrm = _prof_normals(Q, False, smooth)
    P, N, C, T = [], [], [], []
    arc = 0.0
    for k in range(len(Q) - 1):
        q0, q1 = Q[k], Q[k + 1]
        sl = np.linalg.norm(q1 - q0)
        if sl < 1e-9:
            continue
        (n0, n1) = nrm[k]
        base = len(P)
        for j in range(seg + 1):
            th = 2 * math.pi * j / seg
            rd = e1 * math.cos(th) + e2 * math.sin(th)
            for (q, n, v) in ((q0, n0, arc), (q1, n1, arc + sl)):
                P.append(c + rd * q[0] + ax * q[1])
                N.append(rd * n[0] + ax * n[1])
                C.append((th * rmax, v))
        for j in range(seg):
            r0, r1 = base + 2 * j, base + 2 * (j + 1)
            T += [(r0, r0 + 1, r1 + 1), (r0, r1 + 1, r1)]
        arc += sl
    return G.add(mat, np.array(P), np.array(N), np.array(C), T, None, dens)


def surf(G, mat, Pg, dens=1.0, toward=None):
    """Grid surface Pg[nu+1, nv+1, 3] with smooth normals (oriented toward a point if given)."""
    Pg = np.asarray(Pg, float)
    nu, nv = Pg.shape[0] - 1, Pg.shape[1] - 1
    du = np.gradient(Pg, axis=0)
    dv = np.gradient(Pg, axis=1)
    Ng = _n(np.cross(du, dv))
    if toward is not None:
        s = np.sum((np.asarray(toward) - Pg) * Ng)
        if s < 0:
            Ng = -Ng
    cu = np.concatenate([np.zeros((1, nv + 1)), np.cumsum(np.linalg.norm(np.diff(Pg, axis=0), axis=2), 0)], 0)
    cv = np.concatenate([np.zeros((nu + 1, 1)), np.cumsum(np.linalg.norm(np.diff(Pg, axis=1), axis=2), 1)], 1)
    C = np.stack([cu, cv], -1).reshape(-1, 2)
    T = []
    for i in range(nu):
        for j in range(nv):
            a, b, c, d = i * (nv + 1) + j, (i + 1) * (nv + 1) + j, (i + 1) * (nv + 1) + j + 1, i * (nv + 1) + j + 1
            T += [(a, b, c), (a, c, d)]
    return G.add(mat, Pg.reshape(-1, 3), Ng.reshape(-1, 3), C, T, None, dens)


def loft_x(G, mat, prof, x0, x1, rad=0.03, dens=1.0, caps=True, mids=1):
    """Closed (z, y) profile lofted along x with rounded ends (profile shrinks toward its centroid by up to `rad`)."""
    Q = _orient_closed(prof)
    cen = Q.mean(0)
    rad = min(rad, (x1 - x0) * 0.45)
    es = [0.0, 0.3, 0.7, 1.0]
    xs = [(x0 + e * rad, rad - math.sqrt(max(0.0, rad * rad - (rad - e * rad) ** 2))) for e in es]
    inner = [(x0 + rad + (x1 - x0 - 2 * rad) * k / (mids + 1), 0.0) for k in range(1, mids + 1)]
    xs = xs + inner + [(x1 - (x - x0), s) for (x, s) in reversed(xs)]
    rows = []
    for (x, s) in xs:
        d = Q - cen
        dl = np.linalg.norm(d, axis=1, keepdims=True)
        k = np.maximum(dl - s, 0.0) / np.maximum(dl, 1e-9)
        q = cen + d * k
        rows.append([(x, qq[1], qq[0]) for qq in q])
    Pg = np.array(rows)                          # [stations, m, 3]
    Pg = np.concatenate([Pg, Pg[:, :1]], 1)      # close the ring
    nu, nv = Pg.shape[0] - 1, Pg.shape[1] - 1
    du = np.gradient(Pg, axis=0)
    dv = np.gradient(Pg, axis=1)
    dv[:, 0] = dv[:, -1] = (Pg[:, 1] - Pg[:, -2]) * 0.5
    Ng = _n(np.cross(du, dv))
    cc = Pg.mean(1, keepdims=True)
    flip = np.sum((Pg - cc) * Ng) < 0
    if flip:
        Ng = -Ng
    cu = np.array([x for (x, s) in xs])[:, None] * np.ones((1, nv + 1))
    mid = Pg[len(xs) // 2]
    cvl = np.concatenate([[0], np.cumsum(np.linalg.norm(np.diff(mid, axis=0), axis=1))])
    C = np.stack([cu - x0, np.ones((nu + 1, 1)) * cvl[None, :]], -1).reshape(-1, 2)
    T = []
    for i in range(nu):
        for j in range(nv):
            a, b, c, d = i * (nv + 1) + j, (i + 1) * (nv + 1) + j, (i + 1) * (nv + 1) + j + 1, i * (nv + 1) + j + 1
            T += [(a, b, c), (a, c, d)]
    G.add(mat, Pg.reshape(-1, 3), Ng.reshape(-1, 3), C, T, None, dens)
    if caps:
        poly(G, mat, list(Pg[0, :-1]), dens * 0.6, None, (-1, 0, 0))
        poly(G, mat, list(Pg[-1, :-1]), dens * 0.6, None, (1, 0, 0))


def beam(G, mat, a, b, w, h, up=(0, 1, 0), dens=1.0):
    """Rectangular bar from a to b (w across, h along `up`)."""
    a, b = np.asarray(a, float), np.asarray(b, float)
    d = _n(b - a)
    up = _n(np.asarray(up, float) - d * np.dot(up, d))
    side = np.cross(up, d)
    sweep(G, mat, [(-w / 2, -h / 2), (w / 2, -h / 2), (w / 2, h / 2), (-w / 2, h / 2)], a, b, side, up, dens, closed=True, caps=True, smooth=10)


def rod(G, mat, a, b, r, seg=8, dens=1.0, caps=False):
    a, b = np.asarray(a, float), np.asarray(b, float)
    d = _n(b - a)
    ref = np.array((0, 1, 0)) if abs(d[1]) < 0.9 else np.array((1, 0, 0))
    up = _n(ref - d * np.dot(ref, d))
    side = np.cross(up, d)
    prof = [(r * math.cos(2 * math.pi * k / seg), r * math.sin(2 * math.pi * k / seg)) for k in range(seg)]
    sweep(G, mat, prof, a, b, side, up, dens, closed=True, caps=caps, smooth=60)


# =====================================================================================================================
# the carriage
# =====================================================================================================================
def ceiling_profile():
    """(x, y) from the right wall over the vault to the left wall."""
    r = [(W, FRIEZE), (W - 0.012, FRIEZE + 0.008), (W - 0.018, FRIEZE + 0.03), (W - 0.022, FRIEZE + 0.04)]
    cx, cy, rr = W - 0.16, FRIEZE + 0.04, 0.138
    for a in range(0, 91, 15):
        t = math.radians(a)
        r.append((cx + rr * math.cos(t), cy + rr * math.sin(t)))
    y0 = cy + rr + 0.01
    r.append((W - 0.19, y0))
    ae, be = W - 0.19, CROWN - y0
    for k in range(1, 15):
        t = math.radians(90 * k / 14)
        r.append((ae * math.cos(t), y0 + be * math.sin(t)))
    left = [(-x, y) for (x, y) in reversed(r[:-1])]
    return r + left


def ceil_y(x):
    """Ceiling height at |x| (for placing things on the vault)."""
    prof = ceiling_profile()
    best = None
    for (px, py) in prof:
        if px >= 0 and (best is None or abs(px - abs(x)) < abs(best[0] - abs(x))):
            best = (px, py)
    return best[1]


def floor_and_runner(G):
    quad(G, "woodDark", [(-W, 0, ZB), (-W, 0, ZF), (W, 0, ZF), (W, 0, ZB)], 0.8, uv=lambda P: np.stack([P[:, 2], P[:, 0]], 1))
    rw = 0.48
    zr0, zr1 = ZB + 0.25, ZF - 0.05
    quad(G, "carpet", [(-rw, 0.007, zr0), (-rw, 0.007, zr1), (rw, 0.007, zr1), (rw, 0.007, zr0)], 1.0,
         uv=lambda P: np.stack([(P[:, 0] + rw) / (2 * rw), -P[:, 2]], 1))
    for s in (-1, 1):
        box(G, "brass", s * rw - 0.012, s * rw + 0.012, 0, 0.010, zr0, zr1, 0.5, skip=("-y",))
    box(G, "brass", -rw, rw, 0, 0.010, zr0 - 0.012, zr0, 0.5, skip=("-y",))


def side_wall(G, s):
    m0 = G.mark()
    # local frame: right wall (x = +W, facing -x); mirrored for s = -1
    want = (-1, 0, 0)
    wins = sorted(WINDOWS)
    # dado + skirting + waist rail
    quad(G, "wood", [(W, 0.11, ZF), (W, 0.11, ZB), (W, 0.925, ZB), (W, 0.925, ZF)], 0.6, uv="world", want=want)
    box(G, "woodDark", W - 0.016, W, 0, 0.11, ZB, ZF, 0.6, skip=("-y", "+x", "-z", "+z"))
    sweep(G, "wood", [(0.925, 0.0), (0.93, 0.012), (0.94, 0.025), (0.952, 0.03), (0.962, 0.025), (0.966, 0.01), (0.966, 0.0)],
          (W, 0, ZB), (W, 0, ZF), (0, 1, 0), (-1, 0, 0), 0.7, toward=(0, 0.95, 0))
    # window band: piers, under-sill strips, reveals, sills, trims, transoms, pelmets
    edges = [ZB] + [v for (zc, hw) in wins for v in (zc - hw, zc + hw)] + [ZF]
    for k in range(0, len(edges), 2):
        z0, z1 = edges[k], edges[k + 1]
        if z1 - z0 > 0.01:
            quad(G, "wood", [(W, 0.966, z1), (W, 0.966, z0), (W, FRIEZE, z0), (W, FRIEZE, z1)], 0.8, uv="world", want=want)
    for (zc, hw) in wins:
        z0, z1 = zc - hw, zc + hw
        quad(G, "wood", [(W, WIN_Y1, z1), (W, WIN_Y1, z0), (W, FRIEZE, z0), (W, FRIEZE, z1)], 0.8, uv="world", want=want)
        # reveals (wall thickness)
        quad(G, "wood", [(W, WIN_Y0, z0), (WO, WIN_Y0, z0), (WO, WIN_Y0, z1), (W, WIN_Y0, z1)], 1.0, want=(0, 1, 0))
        quad(G, "wood", [(W, WIN_Y1, z0), (W, WIN_Y1, z1), (WO, WIN_Y1, z1), (WO, WIN_Y1, z0)], 1.0, want=(0, -1, 0))
        quad(G, "wood", [(W, WIN_Y0, z0), (W, WIN_Y1, z0), (WO, WIN_Y1, z0), (WO, WIN_Y0, z0)], 1.0, want=(0, 0, 1))
        quad(G, "wood", [(W, WIN_Y0, z1), (WO, WIN_Y0, z1), (WO, WIN_Y1, z1), (W, WIN_Y1, z1)], 1.0, want=(0, 0, -1))
        # sill board
        box(G, "wood", W - 0.065, W, 0.962, WIN_Y0 + 0.002, z0 - 0.07, z1 + 0.07, 1.0, skip=("+x", "-y"), dd={"-y": 0.3})
        # architrave (top + sides), transom bar
        tp = [(0, 0), (0, 0.010), (0.012, 0.019), (0.05, 0.021), (0.064, 0.013), (0.07, 0.0)]
        sweep(G, "wood", tp, (W, WIN_Y1, z0 - 0.07), (W, WIN_Y1, z1 + 0.07), (0, 1, 0), (-1, 0, 0), 1.0, toward=(0, 1.9, zc))
        for (zz, sd) in ((z0, -1), (z1, 1)):
            sweep(G, "wood", tp, (W, WIN_Y0, zz), (W, WIN_Y1 + 0.07, zz), (0, 0, sd), (-1, 0, 0), 1.0, toward=(0, 1.5, zc))
        box(G, "wood", W + 0.01, WG - 0.012, 1.615, 1.655, z0, z1, 1.0, skip=("+x",))
        # pelmet
        pz0, pz1 = z0 - 0.10, z1 + 0.10
        box(G, "wood", W - 0.11, W, 1.928, 2.005, pz0, pz1, 1.0, skip=("+x",), dd={"+y": 0.3})
        # curtains (gathered velvet, tied back)
        for e in (-1, 1):
            curtain(G, zc, hw, e)
    # frieze (above windows) is part of the piers/heads; picture rail under the luggage rack
    sweep(G, "wood", [(0.0, 0.0), (0.0, 0.012), (0.01, 0.02), (0.03, 0.018), (0.04, 0.0)], (W, 2.08, ZB), (W, 2.08, ZF),
          (0, 1, 0), (-1, 0, 0), 0.6, toward=(0, 2.0, 0))
    # sconces on every pier
    for zp in PARTS:
        sconce(G, zp)
    # luggage rack
    rack(G)
    G.mirror(m0, sx=s)


def curtain(G, zc, hw, e):
    zo = zc + e * (hw + 0.06)
    ytop, ybot, ytie = 1.93, 0.995, 1.37
    nu, nv = 11, 12
    Pg = np.zeros((nu + 1, nv + 1, 3))
    for j in range(nv + 1):
        y = ytop - (ytop - ybot) * j / nv
        if y >= ytie:
            t = ((ytop - y) / (ytop - ytie)) ** 1.25
            wdt = 0.25 + (0.095 - 0.25) * t
        else:
            t = ((ytie - y) / (ytie - ybot)) ** 0.8
            wdt = 0.095 + (0.19 - 0.095) * t
        amp = 0.010 + 0.034 * (1 - wdt / 0.25)
        for i in range(nu + 1):
            u = i / nu
            z = zo - e * wdt * u
            ph = 0.6 * e + 0.15 * math.sin(y * 7.0)
            xd = W - 0.078 - amp * (0.5 + 0.5 * math.sin(2 * math.pi * (u * 3.4) + ph)) - 0.004 * math.sin(y * 13 + u * 5)
            Pg[i, j] = (xd, y, z)
    surf(G, "velvet", Pg, 1.0, toward=(0, 1.4, zc))
    # tie-back band
    zt = zo - e * 0.05
    box(G, "brass", W - 0.13, W - 0.066, ytie - 0.012, ytie + 0.012, min(zt, zo) - 0.005 - 0.045, max(zt, zo) + 0.005 + 0.0, 0.4, skip=("+x",))


def sconce(G, zp):
    y = 1.63
    lathe(G, "brass", [(0.047, 0.0), (0.049, 0.004), (0.044, 0.010), (0.03, 0.013), (0.0, 0.014)], (W, y, zp), axis=(-1, 0, 0), seg=14, dens=0.6)
    arm = [(W - 0.012, y, zp), (W - 0.06, y - 0.012, zp), (W - 0.105, y + 0.004, zp), (W - 0.135, y + 0.045, zp), (W - 0.14, y + 0.06, zp)]
    tube(G, "brass", [(0.007 * math.cos(a), 0.007 * math.sin(a)) for a in np.linspace(0, 2 * math.pi, 7)[:-1]], arm, (0, 0, 1), 0.4)
    cx = W - 0.14
    lathe(G, "brass", [(0.0, -0.002), (0.012, 0.0), (0.019, 0.012), (0.021, 0.026)], (cx, y + 0.055, zp), seg=12, dens=0.4)
    old = G.node
    lathe(G, "lampGlass", [(0.016, 0.0), (0.03, 0.02), (0.045, 0.055), (0.054, 0.09), (0.058, 0.115), (0.063, 0.128)], (cx, y + 0.07, zp), seg=12, dens=0)
    lathe(G, "lampGlass", [(0.059, 0.126), (0.052, 0.112), (0.048, 0.09), (0.040, 0.056), (0.026, 0.024), (0.012, 0.006)], (cx, y + 0.07, zp), seg=12, dens=0)
    G.node = old


def rack(G):
    z0, z1 = PARTS[0] - 0.3, PARTS[-1] + 0.3
    rod(G, "brass", (W - 0.40, 2.055, z0), (W - 0.40, 2.055, z1), 0.013, 8, 0.5, caps=True)
    rod(G, "brass", (W - 0.02, 2.06, z0), (W - 0.02, 2.06, z1), 0.008, 6, 0.3, caps=True)
    for k in range(5):
        xa = W - 0.33 + 0.07 * k
        rod(G, "brass", (xa, 2.022, z0), (xa, 2.022, z1), 0.0065, 6, 0.3, caps=True)
    for zp in PARTS:
        beam(G, "brass", (W, 2.04, zp), (W - 0.42, 2.04, zp), 0.012, 0.022, (0, 1, 0), 0.4)
        beam(G, "brass", (W - 0.005, 1.82, zp), (W - 0.31, 2.03, zp), 0.012, 0.018, (0, 1, 0), 0.4)


def ceiling(G):
    prof = ceiling_profile()
    sweep(G, "ceiling", prof, (0, 0, ZB), (0, 0, ZF), (1, 0, 0), (0, 1, 0), 0.55, toward=(0, 1.2, 0))
    # timber ribs at the partitions + crown strip
    arch = [(x, y) for (x, y) in prof if abs(x) <= W - 0.17]
    for zp in PARTS + [ZB + 0.05, ZF - 0.05]:
        path = [(x, y - 0.002, zp) for (x, y) in arch]
        tube(G, "wood", [(0.035, 0.0), (0.035, 0.016), (-0.035, 0.016), (-0.035, 0.0)], path, (0, 0, 1), 0.6, closed=False,
             smooth=30, toward=(0, 1.2, zp))
    sweep(G, "wood", [(-0.06, 0.0), (-0.055, -0.012), (0.055, -0.012), (0.06, 0.0)], (0, CROWN - 0.003, ZB), (0, CROWN - 0.003, ZF),
          (1, 0, 0), (0, 1, 0), 0.5, toward=(0, 1.2, 0))
    # cornice moulding line
    for s in (-1, 1):
        m0 = G.mark()
        sweep(G, "wood", [(0.0, 0.0), (0.0, 0.03), (0.012, 0.04), (0.02, 0.06), (0.0, 0.06)], (W, FRIEZE - 0.06, ZB), (W, FRIEZE - 0.06, ZF),
              (0, 1, 0), (-1, 0, 0), 0.5, toward=(0, 2.0, 0))
        G.mirror(m0, sx=s)


def end_outline(door_hw, door_h):
    prof = ceiling_profile()
    pts = [(-W, 0)]
    if door_hw:
        pts += [(-door_hw, 0), (-door_hw, door_h), (door_hw, door_h), (door_hw, 0)]
    pts += [(W, 0)] + prof[:]
    return pts


def frame(G, mat, x0, x1, y0, y1, z, dirz, w=0.03, d=0.018, dens=0.6):
    """Picture/sign frame of four bars on a wall plane at z (protruding along dirz)."""
    z0, z1 = (z, z + dirz * d) if dirz > 0 else (z + dirz * d, z)
    box(G, mat, x0 - w, x1 + w, y1, y1 + w, z0, z1, dens, skip=("-z" if dirz > 0 else "+z",))
    box(G, mat, x0 - w, x1 + w, y0 - w, y0, z0, z1, dens, skip=("-z" if dirz > 0 else "+z",))
    box(G, mat, x0 - w, x0, y0, y1, z0, z1, dens, skip=("-z" if dirz > 0 else "+z",))
    box(G, mat, x1, x1 + w, y0, y1, z0, z1, dens, skip=("-z" if dirz > 0 else "+z",))


def decal(G, name, x0, x1, y0, y1, z, dirz, dens=1.0):
    u0, v0, u1, v1 = DECALS[name]

    def uv(P):
        fx = (P[:, 0] - x0) / (x1 - x0)
        if dirz < 0:
            fx = 1 - fx
        fy = (y1 - P[:, 1]) / (y1 - y0)
        return np.stack([u0 + (u1 - u0) * fx, v0 + (v1 - v0) * fy], 1)
    quad(G, "decals", [(x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z)], dens, uv=uv, want=(0, 0, dirz))


def etch(G, x0, x1, y0, y1, z, want):
    """Etched (frosted) glass panel: decal 'etch' stretched over the pane."""
    u0, v0, u1, v1 = DECALS["etch"]

    def uv(P):
        return np.stack([u0 + (u1 - u0) * (P[:, 0] - x0) / (x1 - x0), v0 + (v1 - v0) * (y1 - P[:, 1]) / (y1 - y0)], 1)
    quad(G, "frost", [(x0, y0, z), (x1, y0, z), (x1, y1, z), (x0, y1, z)], 0.8, uv=uv, want=want)


def door_trim(G, hw, h, z, dirz):
    tp = [(0, 0), (0, 0.012), (0.014, 0.022), (0.06, 0.024), (0.075, 0.014), (0.08, 0.0)]
    up = (0, 0, dirz)
    sweep(G, "wood", tp, (-hw - 0.08, h, z), (hw + 0.08, h, z), (0, 1, 0), up, 0.8, toward=(0, h - 0.5, z + dirz))
    for sd in (-1, 1):
        sweep(G, "wood", tp, (sd * hw, 0.0, z), (sd * hw, h + 0.08, z), (sd, 0, 0), up, 0.8, toward=(0, 1.0, z + dirz))


def end_walls(G):
    # ---- back wall (z = ZB, facing +z) with the closed communicating door
    poly(G, "wood", [(x, y, ZB) for (x, y) in end_outline(BDOOR_HW, BDOOR_H)], 0.8, "world", (0, 0, 1))
    dz = 0.06
    for sd in (-1, 1):
        quad(G, "wood", [(sd * BDOOR_HW, 0, ZB), (sd * BDOOR_HW, BDOOR_H, ZB), (sd * BDOOR_HW, BDOOR_H, ZB - dz), (sd * BDOOR_HW, 0, ZB - dz)], 0.6, want=(-sd, 0, 0))
    quad(G, "wood", [(-BDOOR_HW, BDOOR_H, ZB), (BDOOR_HW, BDOOR_H, ZB), (BDOOR_HW, BDOOR_H, ZB - dz), (-BDOOR_HW, BDOOR_H, ZB - dz)], 0.6, want=(0, -1, 0))
    zd = ZB - dz
    quad(G, "wood", [(-BDOOR_HW, 0, zd), (BDOOR_HW, 0, zd), (BDOOR_HW, BDOOR_H, zd), (-BDOOR_HW, BDOOR_H, zd)], 0.9, want=(0, 0, 1))
    etch(G, -0.26, 0.26, 1.16, 1.86, zd + 0.004, (0, 0, 1))
    frame(G, "wood", -0.26, 0.26, 1.16, 1.86, zd, 1, 0.035, 0.02, 0.7)
    for (y0, y1) in ((0.22, 0.98),):
        box(G, "wood", -0.27, 0.27, y0, y1, zd, zd + 0.012, 0.8, skip=("-z",))
    box(G, "brass", -0.38, 0.38, 0.02, 0.16, zd, zd + 0.004, 0.5, skip=("-z",))
    box(G, "brass", 0.27, 0.32, 0.98, 1.14, zd, zd + 0.006, 0.5, skip=("-z",))
    lathe(G, "brass", [(0.0, 0.0), (0.012, 0.0), (0.012, 0.03), (0.022, 0.045), (0.02, 0.06), (0.0, 0.065)], (0.30, 1.04, zd), axis=(0, 0, 1), seg=12, dens=0.4)
    door_trim(G, BDOOR_HW, BDOOR_H, ZB, 1)
    # clock (stopped at a minute to midnight)
    cy = 2.37
    lathe(G, "brass", [(0.178, 0.0), (0.175, 0.016), (0.165, 0.032), (0.152, 0.038), (0.140, 0.03), (0.138, 0.02)], (0, cy, ZB), axis=(0, 0, 1), seg=28, dens=0.8)
    cu0, cv0, cu1, cv1 = DECALS["clock"]

    def cuv(P):
        return np.stack([cu0 + (cu1 - cu0) * (0.5 + (P[:, 0]) / 0.30), cv0 + (cv1 - cv0) * (0.5 - (P[:, 1] - cy) / 0.30)], 1)
    poly(G, "decals", [(0.142 * math.cos(a), cy + 0.142 * math.sin(a), ZB + 0.02) for a in np.linspace(0, 2 * math.pi, 33)[:-1]], 1.0, cuv, (0, 0, 1))
    # posters, photograph, plaque, coat hooks
    decal(G, "advert", -1.42, -0.96, 1.18, 1.79, ZB + 0.006, 1)
    frame(G, "wood", -1.42, -0.96, 1.18, 1.79, ZB, 1, 0.028, 0.02)
    decal(G, "photo", 0.98, 1.40, 1.44, 1.76, ZB + 0.006, 1)
    frame(G, "brass", 0.98, 1.40, 1.44, 1.76, ZB, 1, 0.014, 0.012, 0.4)
    decal(G, "plaque", 0.86, 1.50, 1.24, 1.28, ZB + 0.005, 1, 0.8)
    for hx in (0.80, 1.05, 1.30):
        lathe(G, "brass", [(0.0, 0.0), (0.014, 0.0), (0.012, 0.008), (0.006, 0.012), (0.006, 0.05), (0.01, 0.06), (0.0, 0.066)], (hx, 1.95, ZB), axis=(0, 0.4, 1), seg=8, dens=0.3)
    # waist rail on the end walls
    for (xa, xb) in ((-W, -BDOOR_HW - 0.08), (BDOOR_HW + 0.08, W)):
        sweep(G, "wood", [(0.925, 0.0), (0.93, 0.012), (0.94, 0.025), (0.952, 0.03), (0.962, 0.025), (0.966, 0.01), (0.966, 0.0)],
              (xa, 0, ZB), (xb, 0, ZB), (0, 1, 0), (0, 0, 1), 0.6, toward=(0, 0.95, 0))
    # steamer trunk + case in the back corner
    G.node, old = "interior", G.node
    loft_x(G, "leather", [(ZB + 0.02, 0.0), (ZB + 0.62, 0.0), (ZB + 0.62, 0.52), (ZB + 0.02, 0.52)], 0.95, 1.55, 0.03, 0.7)
    box(G, "brass", 0.94, 1.56, 0.24, 0.27, ZB + 0.02, ZB + 0.625, 0.3, skip=("-y", "-z"))
    loft_x(G, "leather", [(ZB + 0.08, 0.52), (ZB + 0.50, 0.52), (ZB + 0.50, 0.70), (ZB + 0.08, 0.70)], 1.02, 1.50, 0.025, 0.7)

    # ---- front wall (z = ZF, facing -z) with the exit door opening + pockets
    poly(G, "wood", [(x, y, ZF) for (x, y) in end_outline(DOOR_HW, DOOR_H)], 0.8, "world", (0, 0, -1))
    for sd in (-1, 1):
        x = sd * DOOR_HW
        quad(G, "wood", [(x, 0, ZF), (x, DOOR_H, ZF), (x, DOOR_H, DOOR_Z0), (x, 0, DOOR_Z0)], 0.6, want=(-sd, 0, 0))
        quad(G, "wood", [(x, 0, DOOR_Z1), (x, DOOR_H, DOOR_Z1), (x, DOOR_H, ZFO), (x, 0, ZFO)], 0.6, want=(-sd, 0, 0))
    quad(G, "wood", [(-DOOR_HW, DOOR_H, ZF), (DOOR_HW, DOOR_H, ZF), (DOOR_HW, DOOR_H, ZFO), (-DOOR_HW, DOOR_H, ZFO)], 0.6, want=(0, -1, 0))
    box(G, "brass", -DOOR_HW, DOOR_HW, 0, 0.012, ZF - 0.02, ZFO, 0.5, skip=("-y",))
    door_trim(G, DOOR_HW, DOOR_H, ZF, -1)
    decal(G, "board", -0.60, 0.60, 2.40, 2.61, ZF - 0.012, -1, 1.2)
    frame(G, "wood", -0.60, 0.60, 2.40, 2.61, ZF, -1, 0.03, 0.025)
    decal(G, "routemap", -1.42, -0.98, 1.16, 1.75, ZF - 0.006, -1)
    frame(G, "wood", -1.42, -0.98, 1.16, 1.75, ZF, -1, 0.028, 0.02)
    decal(G, "photo2", 0.98, 1.40, 1.40, 1.72, ZF - 0.006, -1)
    frame(G, "brass", 0.98, 1.40, 1.40, 1.72, ZF, -1, 0.014, 0.012, 0.4)
    for (xa, xb) in ((-W, -DOOR_HW - 0.08), (DOOR_HW + 0.08, W)):
        sweep(G, "wood", [(0.925, 0.0), (0.93, 0.012), (0.94, 0.025), (0.952, 0.03), (0.962, 0.025), (0.966, 0.01), (0.966, 0.0)],
              (xa, 0, ZF), (xb, 0, ZF), (0, 1, 0), (0, 0, -1), 0.6, toward=(0, 0.95, 0))
    # two cases by the exit
    loft_x(G, "leather", [(ZF - 0.40, 0.0), (ZF - 0.02, 0.0), (ZF - 0.02, 0.42), (ZF - 0.40, 0.42)], 1.00, 1.55, 0.03, 0.7)
    loft_x(G, "leather", [(ZF - 0.36, 0.42), (ZF - 0.08, 0.42), (ZF - 0.08, 0.60), (ZF - 0.36, 0.60)], 1.08, 1.48, 0.025, 0.7)
    G.node = old


def door_leaf(G, sd):
    """Front sliding leaf: meets the other at x = 0, slides along sd * x into the pocket."""
    m0 = G.mark()
    x0, x1 = 0.0, DOOR_HW + 0.03
    z0, z1 = DOOR_Z0, DOOR_Z1
    y0, y1 = 0.012, DOOR_H - 0.01
    for (ya, yb) in ((y0, 0.26), (0.94, 1.06), (2.17, y1)):
        box(G, "wood", x0, x1, ya, yb, z0, z1, 0.8, skip=("+z",) if False else ())
    box(G, "wood", x0, 0.075, 0.26, 2.17, z0, z1, 0.8)
    box(G, "wood", x1 - 0.075, x1, 0.26, 2.17, z0, z1, 0.8)
    box(G, "wood", 0.075, x1 - 0.075, 0.26, 0.94, z0 + 0.008, z1 - 0.008, 0.8, skip=("-x", "+x", "-y", "+y"))
    etch(G, 0.075, x1 - 0.075, 1.06, 2.17, z0 + 0.012, (0, 0, -1))
    etch(G, 0.075, x1 - 0.075, 1.06, 2.17, z1 - 0.012, (0, 0, 1))
    box(G, "brass", 0.085, 0.11, 0.98, 1.22, z0 - 0.012, z0, 0.4, skip=("+z",))
    box(G, "brass", 0.085, 0.11, 0.98, 1.22, z1, z1 + 0.012, 0.4, skip=("-z",))
    G.mirror(m0, sx=sd)


def partition(G, s, zp, minus, plus):
    m0 = G.mark()
    box(G, "wood", A + 0.045, W, 0, 1.28, zp - 0.03, zp + 0.03, 1.0, skip=("+y", "-y", "+x", "-x"),
        dd={"+z": 0.1 if plus else 1.0, "-z": 0.1 if minus else 1.0})
    if not (minus and plus):
        zf = zp + 0.03 if not plus else zp - 0.03
        dz = 1 if not plus else -1
        for (ya, yb) in ((0.12, 0.62), (0.70, 1.18)):
            frame(G, "wood", A + 0.13, W - 0.10, ya, yb, zf, dz, 0.022, 0.012, 0.6)
    sweep(G, "wood", [(-0.045, 1.275), (0.045, 1.275), (0.05, 1.29), (0.046, 1.31), (0.028, 1.324), (0.0, 1.328), (-0.028, 1.324), (-0.046, 1.31), (-0.05, 1.29)],
          (A, 0, zp), (W, 0, zp), (0, 0, -1), (0, 1, 0), 0.8, closed=True, caps=True)
    lathe(G, "brass", [(0.0, 0.0), (0.014, 0.0), (0.012, 0.012), (0.018, 0.03), (0.014, 0.045), (0.0, 0.05)], (A + 0.03, 1.325, zp), seg=12, dens=0.4)
    for (d, has) in ((1, plus), (-1, minus)):
        m1 = G.mark()
        if has:
            wing = [(0.0, 0.0), (0.62, 0.0), (0.62, 0.50), (0.638, 0.545), (0.655, 0.59), (0.652, 0.625), (0.635, 0.648), (0.60, 0.658),
                    (0.30, 0.662), (0.245, 0.68), (0.215, 0.76), (0.198, 0.90), (0.178, 1.02), (0.155, 1.12), (0.125, 1.20), (0.085, 1.25), (0.04, 1.272), (0.0, 1.28)]
        else:
            wing = [(0.0, 0.0), (0.05, 0.0), (0.05, 1.25), (0.03, 1.272), (0.0, 1.28)]
        prism_x(G, "wood", wing, A, A + 0.045, skip_zero=True)
        if has:
            sweep(G, "leather", [(-0.012, 0.656), (0.057, 0.656), (0.063, 0.668), (0.058, 0.684), (0.04, 0.695), (0.0225, 0.698), (0.005, 0.695), (-0.013, 0.684), (-0.018, 0.668)],
                  (0, 0, 0.26), (0, 0, 0.645), (1, 0, 0), (0, 1, 0), 0.8, closed=True, caps=True)
            for p in G.patches[-3:]:
                p["P"][:, 0] += A
            bench(G)
        G.mirror(m1, sz=d, dz=zp)
    G.mirror(m0, sx=s)


def prism_x(G, mat, pts, x0, x1, skip_zero=False, dens=0.8):
    """Polygon in (z, y) extruded along x."""
    Q = _orient_closed(pts)
    m = len(Q)
    for k in range(m):
        a, b = Q[k], Q[(k + 1) % m]
        if skip_zero and abs(a[0]) < 1e-9 and abs(b[0]) < 1e-9:
            continue
        nrm2 = np.array([b[1] - a[1], -(b[0] - a[0])])
        want = (0, nrm2[1], nrm2[0])
        quad(G, mat, [(x0, a[1], a[0]), (x0, b[1], b[0]), (x1, b[1], b[0]), (x1, a[1], a[0])], dens, want=want)
    poly(G, mat, [(x0, q[1], q[0]) for q in Q], dens, "world", (-1, 0, 0))
    poly(G, mat, [(x1, q[1], q[0]) for q in Q], dens, "world", (1, 0, 0))


def bench(G):
    """One bench in the local frame: x = aisle (A) -> wall (W), z from the partition centre toward the facing direction."""
    box(G, "wood", A + 0.045, W, 0.0, 0.33, 0.03, 0.60, 0.9, skip=("-y", "+x", "-x", "-z"), dd={"+y": 0.1})
    box(G, "iron", A + 0.045, W, 0.0, 0.05, 0.58, 0.595, 0.5, skip=("-y", "+x", "-x", "-z", "+y"))
    seat = [(0.10, 0.33), (0.60, 0.33), (0.632, 0.342), (0.652, 0.368), (0.66, 0.40), (0.654, 0.43), (0.635, 0.452), (0.60, 0.462),
            (0.45, 0.467), (0.28, 0.463), (0.16, 0.455), (0.11, 0.443), (0.095, 0.41), (0.095, 0.36)]
    back = [(0.035, 0.47), (0.17, 0.47), (0.195, 0.50), (0.198, 0.56), (0.19, 0.66), (0.175, 0.80), (0.158, 0.94), (0.145, 1.04),
            (0.152, 1.10), (0.148, 1.155), (0.13, 1.195), (0.095, 1.218), (0.055, 1.225), (0.035, 1.215)]
    xa, xm, xb = A + 0.05, A + 0.05 + 0.515, W - 0.005
    for (x0, x1) in ((xa, xm - 0.003), (xm + 0.003, xb)):
        loft_x(G, "moquette", seat, x0, x1, 0.04, 1.0)
        loft_x(G, "moquette", back, x0, x1, 0.035, 1.0)


def table(G, zb):
    m0 = G.mark()
    sweep(G, "wood", [(W, 0.705), (W - 0.32, 0.705), (W - 0.335, 0.71), (W - 0.342, 0.7175), (W - 0.335, 0.725), (W - 0.32, 0.73), (W, 0.73)],
          (0, 0, zb - 0.25), (0, 0, zb + 0.25), (1, 0, 0), (0, 1, 0), 1.0, closed=True, caps=True)
    beam(G, "brass", (W - 0.005, 0.52, zb), (W - 0.25, 0.70, zb), 0.012, 0.02, (0, 1, 0), 0.4)
    return m0


def pendant(G, zb):
    y0 = CROWN - 0.012
    lathe(G, "brass", [(0.0, y0 - 0.026), (0.03, y0 - 0.024), (0.06, y0 - 0.017), (0.075, y0 - 0.008), (0.078, y0 + 0.002)], (0, 0, zb), seg=18, dens=0.5)
    lathe(G, "brass", [(0.007, 2.68), (0.007, y0 - 0.02)], (0, 0, zb), seg=8, dens=0.3)
    lathe(G, "brass", [(0.0, 2.652), (0.045, 2.652), (0.06, 2.662), (0.062, 2.674), (0.04, 2.686), (0.008, 2.688)], (0, 0, zb), seg=18, dens=0.5)
    lathe(G, "brass", [(0.128, 2.654), (0.137, 2.660), (0.137, 2.670), (0.128, 2.674)], (0, 0, zb), seg=24, dens=0.3)
    lathe(G, "brass", [(0.0, 2.512), (0.012, 2.516), (0.016, 2.53), (0.0, 2.54)], (0, 0, zb), seg=10, dens=0.2)
    pts = [(0.13 * math.cos(math.radians(a)), 2.66 + 0.13 * math.sin(math.radians(a))) for a in range(-90, 1, 10)]
    lathe(G, "lampGlass", pts, (0, 0, zb), seg=24, dens=0)


def flush_lamp(G, z):
    yc = ceil_y(0) - 0.004
    lathe(G, "brass", [(0.0, yc - 0.01), (0.15, yc - 0.012), (0.165, yc - 0.004), (0.165, yc + 0.004)], (0, 0, z), seg=24, dens=0.4)
    pts = [(0.14 * math.cos(math.radians(a)), yc - 0.012 - 0.09 * math.sin(math.radians(-a))) for a in range(-90, 1, 10)]
    pts = [(r, yc - 0.012 - 0.09 * (1 - (r / 0.14) ** 2) ** 0.5) for (r, _) in pts]
    lathe(G, "lampGlass", pts, (0, 0, z), seg=24, dens=0)


def luggage(G):
    items = [
        (1, "leather", (W - 0.37, W - 0.04), (2.035, 2.21), (-6.45, -5.85)),
        (1, "leather", (W - 0.36, W - 0.06), (2.035, 2.17), (1.55, 2.05)),
        (1, "leather", (W - 0.33, W - 0.08), (2.17, 2.27), (1.6, 2.0)),
        (-1, "velvet", (W - 0.36, W - 0.05), (2.035, 2.20), (5.55, 6.15)),
        (1, "leather", (W - 0.34, W - 0.06), (2.035, 2.16), (7.6, 8.05)),
        (-1, "leather", (W - 0.37, W - 0.05), (2.035, 2.19), (-3.9, -3.3)),
    ]
    for (s, mat, (x0, x1), (y0, y1), (z0, z1)) in items:
        m0 = G.mark()
        loft_x(G, mat, [(z0, y0), (z1, y0), (z1, y1), (z0, y1)], x0, x1, 0.025, 0.6)
        G.mirror(m0, sx=s)
    # hat box
    m0 = G.mark()
    lathe(G, "leather", [(0.0, 2.035), (0.15, 2.035), (0.16, 2.045), (0.16, 2.20), (0.15, 2.215), (0.0, 2.215)], (W - 0.2, 0, -1.6), seg=20, dens=0.6)
    G.mirror(m0, sx=-1)


def exterior(G):
    G.node = "exterior"
    wins = sorted(WINDOWS)
    for s in (-1, 1):
        m0 = G.mark()
        want = (1, 0, 0)
        quad(G, "liveryG", [(WO, -0.32, ZBO), (WO, -0.32, ZFO), (WO, WIN_Y0 - 0.04, ZFO), (WO, WIN_Y0 - 0.04, ZBO)], 0, want=want)
        quad(G, "liveryC", [(WO, WIN_Y0 - 0.04, ZBO), (WO, WIN_Y0 - 0.04, ZFO), (WO, WIN_Y0, ZFO), (WO, WIN_Y0, ZBO)], 0, want=want)
        quad(G, "liveryC", [(WO, WIN_Y1, ZBO), (WO, WIN_Y1, ZFO), (WO, 2.42, ZFO), (WO, 2.42, ZBO)], 0, want=want)
        edges = [ZBO] + [v for (zc, hw) in wins for v in (zc - hw, zc + hw)] + [ZFO]
        for k in range(0, len(edges), 2):
            quad(G, "liveryC", [(WO, WIN_Y0, edges[k]), (WO, WIN_Y0, edges[k + 1]), (WO, WIN_Y1, edges[k + 1]), (WO, WIN_Y1, edges[k])], 0, want=want)
        box(G, "gold", WO, WO + 0.004, WIN_Y0 - 0.06, WIN_Y0 - 0.048, ZBO, ZFO, 0, skip=("-x",))
        box(G, "gold", WO, WO + 0.004, 2.30, 2.312, ZBO, ZFO, 0, skip=("-x",))
        box(G, "under", WO - 0.06, WO + 0.01, -0.44, -0.32, ZBO + 0.2, ZFO - 0.2, 0)
        G.mirror(m0, sx=s)
    roof = [(WO + 0.04, 2.40), (WO + 0.045, 2.43), (WO, 2.45)]
    for k in range(1, 15):
        t = math.radians(90 * k / 14)
        roof.append(((WO) * math.cos(t), 2.45 + 0.60 * math.sin(t)))
    roof = roof + [(-x, y) for (x, y) in reversed(roof[:-1])]
    sweep(G, "roof", roof, (0, 0, ZBO - 0.06), (0, 0, ZFO + 0.06), (1, 0, 0), (0, 1, 0), 0, toward=(0, 0, 0) and None)
    for p in G.patches[-1:]:
        if p["N"][:, 1].mean() < 0:
            p["N"] = -p["N"]
    for zz, want in ((ZFO + 0.06, (0, 0, 1)), (ZBO - 0.06, (0, 0, -1))):
        poly(G, "roof", [(x, y, zz) for (x, y) in roof] + [(-WO - 0.04, 2.40, zz)][:0], 0, None, want)
    # end faces (front one has the door opening)
    rq = [(WO, 2.42)] + [(WO * math.cos(math.radians(90 * k / 14)), 2.42 + 0.58 * math.sin(math.radians(90 * k / 14))) for k in range(1, 14)] + [(0.0, 3.0)]
    back = [(-WO, -0.32), (WO, -0.32)] + rq + [(-x, y) for (x, y) in reversed(rq[:-1])]
    poly(G, "liveryG", [(x, y, ZBO) for (x, y) in back], 0, None, (0, 0, -1))
    half = [(-WO, -0.32), (0.0, -0.32), (0.0, 0.0), (-DOOR_HW, 0.0), (-DOOR_HW, DOOR_H), (0.0, DOOR_H), (0.0, 3.0)] + [(-x, y) for (x, y) in reversed(rq[:-1])]
    for sx in (1, -1):
        poly(G, "liveryG", [(sx * x, y, ZFO) for (x, y) in half], 0, None, (0, 0, 1))
    for sd in (-1, 1):
        box(G, "gold", sd * DOOR_HW - 0.02 * (sd < 0) - 0.0, sd * DOOR_HW + 0.02 * (sd > 0), 0, DOOR_H + 0.02, ZFO, ZFO + 0.008, 0, skip=("-z",))
    box(G, "gold", -DOOR_HW - 0.02, DOOR_HW + 0.02, DOOR_H, DOOR_H + 0.02, ZFO, ZFO + 0.008, 0, skip=("-z",))
    # underframe, bogies, buffers
    box(G, "under", -1.35, 1.35, -0.86, -0.32, ZBO + 0.5, ZFO - 0.5, 0, skip=("+y",))
    for zc in (-7.4, 7.4):
        box(G, "under", -1.08, 1.08, -0.98, -0.62, zc - 1.25, zc + 1.25, 0, skip=("+y",))
        for wx in (-0.76, 0.76):
            for wz in (-0.9, 0.9):
                lathe(G, "under", [(0.0, -0.065), (0.46, -0.065), (0.46, 0.065), (0.0, 0.065)], (wx, -0.64, zc + wz), axis=(1, 0, 0), seg=16, dens=0)
    for bx in (-0.95, 0.95):
        lathe(G, "under", [(0.09, 0.0), (0.09, 0.30), (0.19, 0.31), (0.19, 0.37), (0.0, 0.38)], (bx, -0.14, ZFO), axis=(0, 0, 1), seg=14, dens=0)
        lathe(G, "under", [(0.09, 0.0), (0.09, 0.30), (0.19, 0.31), (0.19, 0.37), (0.0, 0.38)], (bx, -0.14, ZBO), axis=(0, 0, -1), seg=14, dens=0)
    G.node = "interior"


def panes(G):
    G.node = "panes"
    for s in (-1, 1):
        for (zc, hw) in WINDOWS:
            m0 = G.mark()
            quad(G, "glass", [(WG, WIN_Y0, zc + hw), (WG, WIN_Y0, zc - hw), (WG, WIN_Y1, zc - hw), (WG, WIN_Y1, zc + hw)], 0,
                 uv=lambda P: np.stack([P[:, 2], P[:, 1]], 1), want=(-1, 0, 0))
            G.mirror(m0, sx=s)
    G.node = "interior"


# --------------------------------------------------------------------------------------------------- clue props
COAT_AT = (-0.95, 0.47, PARTS[0] + 0.03 + 0.36)      # Arin's coat: bay -8, left, bench facing the exit
LETTER_AT = (0.95, 0.469, PARTS[2] + 0.03 + 0.37)    # the letter: bay -4, right, bench facing the exit


def clue_props(G):
    G.node = "coat"
    cx, cy, cz = COAT_AT
    # folded coat body (lumpy slab) + collar roll + a sleeve slipping over the cushion front
    prof = [(cz - 0.17, cy), (cz + 0.16, cy), (cz + 0.19, cy + 0.03), (cz + 0.17, cy + 0.06), (cz + 0.05, cy + 0.075), (cz - 0.08, cy + 0.07), (cz - 0.17, cy + 0.05), (cz - 0.19, cy + 0.025)]
    loft_x(G, "wool", prof, cx - 0.23, cx + 0.21, 0.05, 1.2)
    rod(G, "wool", (cx - 0.20, cy + 0.07, cz - 0.15), (cx + 0.18, cy + 0.08, cz - 0.13), 0.03, 10, 1.2, caps=True)
    sleeve = [(cx + 0.12, cy + 0.04, cz + 0.12), (cx + 0.14, cy + 0.03, cz + 0.24), (cx + 0.15, cy - 0.01, cz + 0.30), (cx + 0.155, cy - 0.10, cz + 0.33), (cx + 0.16, cy - 0.22, cz + 0.335)]
    tube(G, "wool", [(0.045 * math.cos(a), 0.035 * math.sin(a)) for a in np.linspace(0, 2 * math.pi, 11)[:-1]], sleeve, (1, 0, 0), 1.2)
    G.node = "watch"
    wx, wy, wz = cx + 0.06, cy + 0.078, cz + 0.02
    lathe(G, "brass", [(0.0, 0.0), (0.024, 0.0), (0.027, 0.004), (0.027, 0.009), (0.022, 0.012)], (wx, wy, wz), seg=20, dens=2.0)
    cu0, cv0, cu1, cv1 = DECALS["clock"]
    poly(G, "decals", [(wx + 0.021 * math.cos(a), wy + 0.0121, wz + 0.021 * math.sin(a)) for a in np.linspace(0, 2 * math.pi, 21)[:-1]], 2.0,
         lambda P: np.stack([cu0 + (cu1 - cu0) * (0.5 + (P[:, 0] - wx) / 0.044), cv0 + (cv1 - cv0) * (0.5 + (P[:, 2] - wz) / 0.044)], 1), (0, 1, 0))
    lathe(G, "brass", [(0.0, 0.0), (0.005, 0.0), (0.005, 0.008), (0.0, 0.01)], (wx, wy + 0.004, wz - 0.03), axis=(0, 0, -1), seg=8, dens=1.0)
    chain = [(wx, wy + 0.004, wz - 0.04), (wx - 0.04, wy + 0.001, wz - 0.06), (wx - 0.09, wy - 0.002, wz - 0.05), (wx - 0.12, wy - 0.004, wz - 0.01)]
    tube(G, "brass", [(0.0025 * math.cos(a), 0.0025 * math.sin(a)) for a in np.linspace(0, 2 * math.pi, 5)[:-1]], chain, (0, 1, 0), 1.0)
    G.node = "letter"
    lx, ly, lz = LETTER_AT
    m0 = G.mark()
    u0, v0, u1, v1 = DECALS["envelope"]
    hw, hd = 0.115, 0.065
    poly(G, "decals", [(lx - hw, ly + 0.004, lz - hd), (lx + hw, ly + 0.004, lz - hd), (lx + hw, ly + 0.004, lz + hd), (lx - hw, ly + 0.004, lz + hd)], 3.0,
         lambda P: np.stack([u0 + (u1 - u0) * (1 - (P[:, 0] - lx + hw) / (2 * hw)), v0 + (v1 - v0) * ((P[:, 2] - lz + hd) / (2 * hd))], 1), (0, 1, 0))
    box(G, "decals", lx - hw, lx + hw, ly, ly + 0.0035, lz - hd, lz + hd, 0.5, skip=("+y", "-y"))
    for p in G.patches[-4:]:
        p["UV"] = np.tile(np.array([[u0 + 0.01, v0 + 0.01]]), (len(p["P"]), 1)) if p is not G.patches[m0] else p["UV"]
    G.rotate(m0, (0, 1, 0), 0.32, (lx, ly, lz))
    G.node = "interior"


def build_carriage():
    G = Geo()
    floor_and_runner(G)
    for s in (-1, 1):
        side_wall(G, s)
    ceiling(G)
    end_walls(G)
    for s in (-1, 1):
        for i, zp in enumerate(PARTS):
            partition(G, s, zp, minus=i > 0, plus=i < len(PARTS) - 1)
        for zb in BAYS:
            m0 = table(G, zb)
            G.mirror(m0, sx=s)
    luggage(G)
    for z in (-10.0, 9.93):
        flush_lamp(G, z)
    G.node = "pendants"
    for zb in BAYS:
        pendant(G, zb)
    G.node = "doorL"
    door_leaf(G, -1)
    G.node = "doorR"
    door_leaf(G, 1)
    G.node = "interior"
    panes(G)
    exterior(G)
    clue_props(G)
    for p in G.patches:
        fix_winding(p)
    return G


# =====================================================================================================================
# textures (tileable, linear albedo -> sRGB JPEG)
# =====================================================================================================================
def tex_wood(n):
    """Varnished mahogany: straight grain along u, ribbon figure, pores."""
    x, y = grid(n)
    warp = fbm(x, y, 2, 4, 501, aniso=(1, 3)) * 0.035
    rings = 0.5 + 0.5 * np.sin((y + warp) * 2 * np.pi * 26 + 3.2 * fbm(x, y, 1, 3, 502, aniso=(2, 6)))
    fibre = fbm(x, y, 2, 3, 503, aniso=(1, 90))
    ribbon = 0.5 + 0.5 * np.sin(y * 2 * np.pi * 4 + 2.5 * fbm(x, y, 1, 3, 504, aniso=(2, 1)))
    ribbon *= 0.75 + 0.25 * np.sin(x * 2 * np.pi * 2 + 1.5 * fbm(x, y, 1, 2, 507))
    pores = sstep(0.55, 0.8, fbm(x, y, 4, 3, 505, aniso=(1, 48)))
    dark, mid, light = lin(0x2c0e07), lin(0x5a2111), lin(0x8a3d1d)
    t = np.clip(0.25 + 0.55 * rings ** 1.6 + 0.18 * fibre, 0, 1)
    alb = mixc(dark * np.ones((n, n, 1)), mid * np.ones((n, n, 1)), t)
    alb = mixc(alb, light * np.ones((n, n, 1)), (ribbon * 0.35 * (0.6 + 0.4 * rings)))
    alb *= (1 - 0.35 * pores)[..., None]
    alb *= (0.92 + 0.16 * fbm(x, y, 1, 4, 506))[..., None]
    return np.clip(alb, 0.003, 1)


def tex_moquette(n):
    """Bottle-green moquette with a dull-gold diamond lattice and small four-petal flowers (4 x 4 motifs per tile)."""
    x, y = grid(n)
    K = 4
    u, v = x * K, y * K
    fu, fv = u - np.floor(u) - 0.5, v - np.floor(v) - 0.5
    dia = np.abs(fu) + np.abs(fv)
    lattice = 1 - sstep(0.018, 0.04, np.abs(dia - 0.5))
    r = np.hypot(fu, fv)
    ang = np.arctan2(fv, fu)
    petal = 1 - sstep(-0.01, 0.02, r - (0.07 + 0.075 * np.abs(np.cos(2 * ang)) ** 1.5))
    dot = 1 - sstep(0.025, 0.04, r)
    cross = (1 - sstep(0.012, 0.022, np.minimum(np.abs(fu), np.abs(fv)))) * sstep(0.30, 0.26, dia) * (1 - petal)
    pile = fbm(x, y, 32, 3, 511)
    field = lin(0x173a2a) * np.ones((n, n, 1))
    field *= (0.82 + 0.3 * pile)[..., None] * (0.9 + 0.2 * sstep(0.2, 0.5, dia))[..., None]
    alb = mixc(field, lin(0x0c2018), cross * 0.6)
    alb = mixc(alb, lin(0x7e6128), lattice * 0.9)
    alb = mixc(alb, lin(0x96773a), petal)
    alb = mixc(alb, lin(0x5a1a1c), dot)
    alb *= (0.9 + 0.18 * pile)[..., None]
    wear = sstep(0.3, 0.8, fbm(x, y, 1, 3, 512))
    alb = mixc(alb, alb * 1.15, wear * 0.4)
    return np.clip(alb, 0.003, 1)


def tex_velvet(n):
    x, y = grid(n)
    crush = fbm(x, y, 2, 4, 521, aniso=(10, 1))
    pile = fbm(x, y, 32, 2, 522)
    alb = lin(0x5a0d14) * np.ones((n, n, 1))
    alb *= (0.7 + 0.45 * (0.5 + 0.5 * crush))[..., None] * (0.92 + 0.16 * pile)[..., None]
    return np.clip(alb, 0.003, 1)


def tex_carpet(n):
    """Runner: u across the runner (0..1 = full width), v along it (1 m per tile)."""
    x, y = grid(n)
    alb = lin(0x5a1214) * np.ones((n, n, 1))
    pile = fbm(x, y, 32, 3, 531)
    bx = np.minimum(x, 1 - x)
    border = bx < 0.11
    guard = (bx >= 0.11) & (bx < 0.13)
    navy, gold, cream = lin(0x141d33), lin(0x8a6422), lin(0xb59a6a)
    alb = np.where(border[..., None], navy, alb)
    alb = np.where(guard[..., None], gold, alb)
    # border pattern: small gold diamonds
    by = (y * 10) % 1 - 0.5
    bd = np.abs((bx - 0.055) / 0.035) + np.abs(by / 0.3)
    alb = mixc(alb, gold * np.ones((n, n, 1)), (1 - sstep(0.75, 0.95, bd)) * border)
    alb = mixc(alb, lin(0x6a1416) * np.ones((n, n, 1)), (1 - sstep(0.25, 0.4, bd)) * border)
    # field: two lozenge medallions per tile + scattered crosses
    for cy in (0.25, 0.75):
        d = np.abs((x - 0.5) / 0.30) + np.abs((y - cy) / 0.20)
        alb = mixc(alb, gold * np.ones((n, n, 1)), (1 - sstep(0.97, 1.0, d)) * sstep(0.88, 0.91, d))
        alb = mixc(alb, navy * np.ones((n, n, 1)), (1 - sstep(0.86, 0.89, d)))
        alb = mixc(alb, lin(0x6a1416) * np.ones((n, n, 1)), (1 - sstep(0.45, 0.48, d)))
        alb = mixc(alb, cream * np.ones((n, n, 1)), (1 - sstep(0.16, 0.2, d)))
    fx, fy = (x * 6) % 1 - 0.5, (y * 6 + 0.5 * (np.floor(x * 6) % 2)) % 1 - 0.5
    crs = (1 - sstep(0.02, 0.035, np.minimum(np.abs(fx), np.abs(fy)))) * (1 - sstep(0.15, 0.2, np.hypot(fx, fy)))
    inner = (bx > 0.16)
    alb = mixc(alb, gold * np.ones((n, n, 1)), crs * inner * 0.7)
    alb *= (0.8 + 0.35 * pile)[..., None]
    worn = sstep(0.42, 0.0, np.abs(x - 0.5)) * sstep(0.2, 0.8, fbm(x, y, 1, 3, 532))
    alb = mixc(alb, alb * 1.25 + 0.01, worn * 0.35)
    return np.clip(alb, 0.003, 1)


def tex_ceiling(n):
    """Ivory lincrusta: embossed quatrefoil relief, shaded by a fixed light (cavity) + faint soot."""
    x, y = grid(n)
    K = 3
    fu, fv = (x * K) % 1 - 0.5, (y * K) % 1 - 0.5
    r = np.hypot(fu, fv)
    ang = np.arctan2(fv, fu)
    h = sstep(0.04, 0.0, np.abs(r - (0.22 + 0.08 * np.cos(4 * ang))))
    h += sstep(0.03, 0.0, np.abs(np.abs(fu) + np.abs(fv) - 0.5)) * 0.8
    h += sstep(0.05, 0.0, r) * 0.7
    hb = blur(h, 1.2)
    sh = np.roll(hb, 2, 0) - hb + 0.5 * (np.roll(hb, 2, 1) - hb)
    alb = lin(0xd8ccae) * np.ones((n, n, 1))
    alb *= (1 + 0.35 * sh)[..., None]
    alb *= (0.93 + 0.1 * fbm(x, y, 2, 4, 541))[..., None]
    return np.clip(alb, 0.003, 1)


# ---------------------------------------------------------------------------------------------------- decal atlas
DSZ = 1024
DECAL_PX = {
    "board": (0, 0, 1024, 176), "clock": (0, 176, 256, 432), "routemap": (256, 176, 640, 688), "advert": (640, 176, 1024, 688),
    "photo": (0, 432, 256, 624), "photo2": (0, 688, 256, 880), "plaque": (256, 688, 1024, 752), "envelope": (256, 752, 576, 928),
    "etch": (576, 752, 832, 1024),
}
DECALS = {k: (a / DSZ, b / DSZ, c / DSZ, d / DSZ) for k, (a, b, c, d) in DECAL_PX.items()}


def _font(names, size):
    from PIL import ImageFont
    dirs = ["/usr/share/fonts/truetype/dejavu", "/usr/share/fonts/truetype/freefont", "/usr/share/fonts/truetype/liberation", "/usr/share/fonts/TTF", "/Library/Fonts", "C:/Windows/Fonts"]
    for nm in names:
        for d in dirs:
            p = os.path.join(d, nm)
            if os.path.exists(p):
                return ImageFont.truetype(p, size)
    return ImageFont.load_default()


def _ctext(dr, cx, y, text, font, fill, spacing=0):
    if spacing:
        w = sum(dr.textlength(ch, font=font) for ch in text) + spacing * (len(text) - 1)
        x = cx - w / 2
        for ch in text:
            dr.text((x, y), ch, font=font, fill=fill)
            x += dr.textlength(ch, font=font) + spacing
    else:
        w = dr.textlength(text, font=font)
        dr.text((cx - w / 2, y), text, font=font, fill=fill)


def make_decals():
    from PIL import Image, ImageDraw, ImageFilter
    img = Image.new("RGB", (DSZ, DSZ), (40, 30, 24))
    dr = ImageDraw.Draw(img)
    serifB = ["DejaVuSerif-Bold.ttf", "FreeSerifBold.ttf", "LiberationSerif-Bold.ttf"]
    serif = ["DejaVuSerif.ttf", "FreeSerif.ttf", "LiberationSerif-Regular.ttf"]
    italic = ["FreeSerifItalic.ttf", "DejaVuSerif-Italic.ttf", "LiberationSerif-Italic.ttf"]
    sansB = ["DejaVuSans-Bold.ttf", "FreeSansBold.ttf", "LiberationSans-Bold.ttf"]
    rng = np.random.default_rng(7)
    # destination board: black enamel, gold letters
    x0, y0, x1, y1 = DECAL_PX["board"]
    dr.rectangle([x0, y0, x1, y1], fill=(14, 12, 11))
    dr.rectangle([x0 + 10, y0 + 10, x1 - 10, y1 - 10], outline=(176, 132, 60), width=4)
    dr.rectangle([x0 + 20, y0 + 20, x1 - 20, y1 - 20], outline=(120, 88, 40), width=2)
    _ctext(dr, (x0 + x1) / 2, y0 + 22, "VEYRA", _font(serifB, 96), (214, 170, 88), spacing=22)
    _ctext(dr, (x0 + x1) / 2, y0 + 128, "LAST STOP  \u00b7  TERMINUS  \u00b7  CENTRAL", _font(serif, 26), (176, 140, 80), spacing=4)
    # clock face: white enamel, roman numerals, hands at a minute to midnight
    x0, y0, x1, y1 = DECAL_PX["clock"]
    cx, cy, R = (x0 + x1) / 2, (y0 + y1) / 2, 126
    dr.rectangle([x0, y0, x1, y1], fill=(60, 44, 24))
    dr.ellipse([cx - R, cy - R, cx + R, cy + R], fill=(232, 224, 204))
    dr.ellipse([cx - R + 8, cy - R + 8, cx + R - 8, cy + R - 8], outline=(30, 26, 22), width=2)
    fnt = _font(serif, 22)
    for k in range(60):
        a = math.radians(k * 6 - 90)
        r0 = R - 14 if k % 5 else R - 20
        dr.line([cx + math.cos(a) * r0, cy + math.sin(a) * r0, cx + math.cos(a) * (R - 10), cy + math.sin(a) * (R - 10)], fill=(30, 26, 22), width=2 if k % 5 else 3)
    for k, num in enumerate(["XII", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI"]):
        a = math.radians(k * 30 - 90)
        tx, ty = cx + math.cos(a) * (R - 40), cy + math.sin(a) * (R - 40)
        w = dr.textlength(num, font=fnt)
        dr.text((tx - w / 2, ty - 13), num, font=fnt, fill=(28, 24, 20))
    _ctext(dr, cx, cy + 34, "V.L.R.", _font(serif, 14), (90, 70, 50))

    def hand(ang_deg, length, width, col):
        a = math.radians(ang_deg - 90)
        px, py = -math.sin(a), math.cos(a)
        tip = (cx + math.cos(a) * length, cy + math.sin(a) * length)
        tail = (cx - math.cos(a) * length * 0.18, cy - math.sin(a) * length * 0.18)
        dr.polygon([tail, (cx + px * width, cy + py * width), tip, (cx - px * width, cy - py * width)], fill=col)
    hand((11 + 59 / 60) * 30, R * 0.55, 6, (20, 16, 14))
    hand(59 * 6, R * 0.8, 4, (20, 16, 14))
    dr.ellipse([cx - 7, cy - 7, cx + 7, cy + 7], fill=(150, 110, 50))
    # route map poster
    x0, y0, x1, y1 = DECAL_PX["routemap"]
    dr.rectangle([x0, y0, x1, y1], fill=(222, 208, 178))
    dr.rectangle([x0 + 14, y0 + 14, x1 - 14, y1 - 14], outline=(70, 50, 34), width=3)
    _ctext(dr, (x0 + x1) / 2, y0 + 30, "VEYRA LINE", _font(serifB, 40), (60, 34, 22), spacing=3)
    _ctext(dr, (x0 + x1) / 2, y0 + 80, "NIGHT SERVICE  \u00b7  ALL STATIONS", _font(serif, 15), (90, 64, 44), spacing=2)
    lx = x0 + 90
    stations = ["????????", "MERRIN", "HOLLOW BRIDGE", "ST. ODILE", "ASHGROVE HALT", "VEYRA CENTRAL"]
    sy0, sy1 = y0 + 130, y1 - 90
    dr.line([lx, sy0, lx, sy1], fill=(140, 40, 30), width=8)
    fnt = _font(serifB, 21)
    for k, nm in enumerate(stations):
        sy = sy0 + (sy1 - sy0) * k / (len(stations) - 1)
        last = k == len(stations) - 1
        rr = 14 if last else 9
        dr.ellipse([lx - rr, sy - rr, lx + rr, sy + rr], fill=(222, 208, 178), outline=(60, 34, 22), width=4)
        if k == 0:
            dr.text((lx + 28, sy - 13), "ELMWYCK JCT.", font=fnt, fill=(60, 40, 30))
            for j in range(26):   # scratched out, many times
                ax, ay = lx + 24 + rng.uniform(0, 180), sy - 14 + rng.uniform(0, 28)
                dr.line([ax, ay, ax + rng.uniform(20, 70), ay + rng.uniform(-10, 10)], fill=(40, 26, 18), width=2)
        else:
            dr.text((lx + 28, sy - 13), nm, font=fnt, fill=(50, 30, 20))
        if last:
            dr.ellipse([lx - 5, sy - 5, lx + 5, sy + 5], fill=(60, 34, 22))
            ex, ey = x1 - 70, sy
            dr.ellipse([ex - 26, ey - 26, ex + 26, ey + 26], outline=(140, 40, 30), width=4)
            _ctext(dr, ex, ey - 15, "0", _font(serifB, 26), (140, 40, 30))
    _ctext(dr, (x0 + x1) / 2, y1 - 56, "Tickets are valid in one direction only.", _font(italic, 15), (80, 56, 40))
    # advertisement: Festival of Bells lithograph
    x0, y0, x1, y1 = DECAL_PX["advert"]
    for yy in range(y0, y1):
        t = (yy - y0) / (y1 - y0)
        c = np.array([22, 52, 64]) * (1 - t) + np.array([214, 120, 58]) * t ** 1.6 * 0.95 + np.array([30, 20, 10]) * t
        dr.line([x0, yy, x1, yy], fill=tuple(int(v) for v in np.clip(c, 0, 255)))
    cx = (x0 + x1) / 2
    base = y1 - 120
    dr.polygon([(cx - 46, base), (cx - 40, y0 + 200), (cx + 40, y0 + 200), (cx + 46, base)], fill=(20, 22, 30))
    dr.polygon([(cx - 52, y0 + 205), (cx, y0 + 120), (cx + 52, y0 + 205)], fill=(20, 22, 30))
    dr.ellipse([cx - 30, y0 + 222, cx + 30, y0 + 282], fill=(250, 214, 140))
    dr.line([cx, y0 + 252, cx - 3, y0 + 230], fill=(30, 24, 20), width=3)
    dr.line([cx, y0 + 252, cx - 2, y0 + 240], fill=(30, 24, 20), width=4)
    for bxx in (-1, 1):
        bx = cx + bxx * 120
        dr.polygon([(bx - 26, y0 + 330), (bx - 18, y0 + 290), (bx + 18, y0 + 290), (bx + 26, y0 + 330)], fill=(200, 150, 60))
        dr.ellipse([bx - 6, y0 + 328, bx + 6, y0 + 340], fill=(200, 150, 60))
    dr.rectangle([x0, base, x1, y1], fill=(24, 18, 16))
    for k in range(14):
        hx = x0 + k * 28 + int(rng.uniform(0, 10))
        hh = int(rng.uniform(30, 80))
        dr.rectangle([hx, base - hh, hx + 26, base], fill=(24, 18, 16))
        if rng.uniform() < 0.6:
            dr.rectangle([hx + 8, base - hh + 10, hx + 14, base - hh + 18], fill=(250, 200, 110))
    _ctext(dr, cx, y0 + 26, "VEYRA", _font(serifB, 58), (246, 226, 180), spacing=10)
    _ctext(dr, cx, y0 + 92, "THE CITY OF CLOCKS", _font(serif, 20), (246, 226, 180), spacing=3)
    _ctext(dr, cx, y1 - 104, "THE FESTIVAL OF BELLS", _font(serifB, 24), (240, 200, 120), spacing=2)
    _ctext(dr, cx, y1 - 66, "Midsummer  \u00b7  Excursions by the Veyra Line", _font(italic, 16), (220, 200, 170))
    _ctext(dr, cx, y1 - 38, "Return tickets not issued.", _font(italic, 13), (180, 150, 120))
    # sepia photographs
    for key, seed in (("photo", 1), ("photo2", 2)):
        x0, y0, x1, y1 = DECAL_PX[key]
        dr.rectangle([x0, y0, x1, y1], fill=(236, 228, 210))
        ix0, iy0, ix1, iy1 = x0 + 14, y0 + 14, x1 - 14, y1 - 14
        for yy in range(iy0, iy1):
            t = (yy - iy0) / (iy1 - iy0)
            c = np.array([200, 176, 140]) * (1 - t) + np.array([120, 94, 66]) * t
            dr.line([ix0, yy, ix1, yy], fill=tuple(int(v) for v in c))
        if seed == 1:
            tx = (ix0 + ix1) / 2 + 30
            dr.rectangle([tx - 12, iy0 + 40, tx + 12, iy1 - 30], fill=(70, 52, 38))
            dr.polygon([(tx - 16, iy0 + 42), (tx, iy0 + 18), (tx + 16, iy0 + 42)], fill=(70, 52, 38))
            dr.ellipse([tx - 7, iy0 + 52, tx + 7, iy0 + 66], fill=(210, 190, 160))
            dr.rectangle([ix0, iy1 - 40, ix1, iy1], fill=(90, 70, 50))
            for k in range(8):
                hx = ix0 + k * 30
                dr.rectangle([hx, iy1 - 40 - int(rng.uniform(8, 30)), hx + 26, iy1 - 30], fill=(96, 74, 54))
        else:
            dr.polygon([(ix0, iy1 - 50), (ix0 + 60, iy0 + 60), (ix0 + 120, iy1 - 60), (ix0 + 170, iy0 + 80), (ix1, iy1 - 40), (ix1, iy1), (ix0, iy1)], fill=(110, 90, 66))
            dr.rectangle([ix0, iy1 - 34, ix1, iy1 - 28], fill=(60, 46, 34))
            for k in range(11):
                dr.line([ix0 + 6 + k * 21, iy1 - 34, ix0 + 6 + k * 21, iy1 - 22], fill=(60, 46, 34), width=3)
        _ctext(dr, (x0 + x1) / 2, y1 - 13, "Veyra, from the line" if seed == 1 else "The viaduct at Hollow Bridge", _font(italic, 10), (90, 70, 50))
    # enamel plaque
    x0, y0, x1, y1 = DECAL_PX["plaque"]
    dr.rectangle([x0, y0, x1, y1], fill=(232, 230, 222))
    dr.rectangle([x0 + 5, y0 + 5, x1 - 5, y1 - 5], outline=(24, 40, 90), width=3)
    _ctext(dr, (x0 + x1) / 2, y0 + 16, "DO NOT LEAN OUT OF THE WINDOW  \u00b7  NE PAS SE PENCHER AU DEHORS", _font(sansB, 24), (24, 40, 90))
    # envelope addressed to Arin, ring-and-zero seal
    x0, y0, x1, y1 = DECAL_PX["envelope"]
    dr.rectangle([x0, y0, x1, y1], fill=(226, 214, 186))
    dr.line([x0, y0, (x0 + x1) / 2, (y0 + y1) / 2 + 10, x1, y0], fill=(196, 182, 152), width=2)
    _ctext(dr, (x0 + x1) / 2 - 30, y0 + 98, "Arin", _font(italic, 46), (40, 34, 60))
    sx, sy = x1 - 70, (y0 + y1) / 2 + 12
    dr.ellipse([sx - 30, sy - 30, sx + 30, sy + 30], fill=(130, 22, 24))
    dr.ellipse([sx - 20, sy - 20, sx + 20, sy + 20], outline=(190, 70, 60), width=4)
    _ctext(dr, sx, sy - 14, "0", _font(serifB, 22), (190, 70, 60))
    # etched glass: frosted field, clear border lines, corner fans and a ring-and-zero monogram
    x0, y0, x1, y1 = DECAL_PX["etch"]
    dr.rectangle([x0, y0, x1, y1], fill=(150, 156, 160))
    for k, c in ((10, (72, 80, 88)), (16, (150, 156, 160)), (22, (90, 98, 106))):
        dr.rectangle([x0 + k, y0 + k, x1 - k, y1 - k], outline=c, width=3)
    cx, cy = (x0 + x1) / 2, (y0 + y1) / 2
    for r in (54, 46):
        dr.ellipse([cx - r, cy - r, cx + r, cy + r], outline=(84, 92, 100), width=4)
    _ctext(dr, cx, cy - 26, "0", _font(serifB, 40), (84, 92, 100))
    for (fx, fy) in ((x0 + 22, y0 + 22), (x1 - 22, y0 + 22), (x0 + 22, y1 - 22), (x1 - 22, y1 - 22)):
        for k in range(5):
            a0 = math.radians(k * 18 + (0 if fx < cx and fy < cy else 90 if fx > cx and fy < cy else 270 if fx < cx else 180))
            dr.line([fx, fy, fx + math.cos(a0) * 40 * (1 if fx < cx else 1), fy + math.sin(a0) * 40], fill=(96, 104, 112), width=2)
    a = np.asarray(img).astype(np.float64) / 255.0
    # age: slight grain + stains
    x, y = grid(DSZ)
    a *= (0.94 + 0.08 * fbm(x, y, 8, 3, 551))[..., None]
    return a


# =====================================================================================================================
# lightmap charts: pack every baked patch into one atlas
# =====================================================================================================================
def pack_charts(patches, res, pad=2, fill=0.80):
    charts = [p for p in patches if p["dens"] > 0 and MATS[p["mat"]]["baked"]]
    for p in charts:
        C = p["C"]
        p["c0"] = C.min(0)
        p["ext"] = np.maximum(C.max(0) - p["c0"], 1e-4)
    area = sum(float(p["ext"][0] * p["ext"][1]) * p["dens"] ** 2 for p in charts)
    rho = math.sqrt(fill * res * res / max(area, 1e-6))
    while True:
        rects = []
        for i, p in enumerate(charts):
            w = int(math.ceil(p["ext"][0] * rho * p["dens"])) + 2 * pad
            h = int(math.ceil(p["ext"][1] * rho * p["dens"])) + 2 * pad
            w, h = max(w, 2 * pad + 2), max(h, 2 * pad + 2)
            rot = h > w
            rects.append((h if rot else w, w if rot else h, rot, i))
        rects.sort(key=lambda r: (-r[1], -r[0]))
        x = y = rowh = 0
        place = {}
        ok = True
        for (w, h, rot, i) in rects:
            if w > res:
                ok = False
                break
            if x + w > res:
                y += rowh
                x = rowh = 0
            if y + h > res:
                ok = False
                break
            place[i] = (x, y, w, h, rot)
            x += w
            rowh = max(rowh, h)
        if ok:
            break
        rho *= 0.96
    idmap = np.full((res, res), -1, np.int32)
    for i, p in enumerate(charts):
        x0, y0, w, h, rot = place[i]
        C = (p["C"] - p["c0"]) * rho * p["dens"]
        if rot:
            C = C[:, ::-1]
        p["UV1"] = np.stack([(x0 + pad + C[:, 0]) / res, (y0 + pad + C[:, 1]) / res], 1)
        p["rect"] = (x0, y0, w, h)
        idmap[y0:y0 + h, x0:x0 + w] = i
    used = sum(w * h for (_, _, w, h, _) in place.values()) / (res * res)
    print(f"  lightmap: {len(charts)} charts, {rho:.1f} texels/m, {used * 100:.0f}% of {res}^2 used")
    return rho, idmap


# =====================================================================================================================
# Cycles bake
# =====================================================================================================================
def bake_lights():
    L = []
    for zb in BAYS:
        L.append(((0.0, 2.58, zb), WARM, 22.0, 0.06, "spot"))
        L.append(((0.0, 2.62, zb), WARM, 4.0, 0.06))
    for z in (-10.0, 9.93):
        L.append(((0.0, ceil_y(0) - 0.07, z), WARM, 16.0, 0.08))
    for s in (-1, 1):
        for zp in PARTS:
            L.append(((s * (W - 0.14), 1.76, zp), WARM_SCONCE, 5.0, 0.025))
    return L


def blender_bake(G, res, samples):
    import bpy
    import addon_utils
    bpy.ops.wm.read_factory_settings(use_empty=True)
    addon_utils.enable("cycles", default_set=True)
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = samples
    sc.cycles.use_denoising = False
    sc.cycles.max_bounces = 6
    sc.cycles.diffuse_bounces = 4
    sc.cycles.glossy_bounces = 1
    sc.cycles.transmission_bounces = 0
    sc.cycles.transparent_max_bounces = 2
    sc.cycles.sample_clamp_indirect = 2.0
    try:
        sc.render.threads_mode = "AUTO"
    except Exception:
        pass
    world = bpy.data.worlds.new("night")
    sc.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs[0].default_value = (0.20, 0.30, 0.52, 1.0)
    bg.inputs[1].default_value = 0.55
    img = bpy.data.images.new("LM", res, res, alpha=True, float_buffer=True)
    img.generated_color = (0, 0, 0, 0)
    texmeans = G.texmeans
    bmats = {}

    def bmat(name, target):
        k = name + ("*" if target else "")
        if k in bmats:
            return bmats[k]
        m = bpy.data.materials.new(k)
        m.use_nodes = True
        nt = m.node_tree
        bsdf = nt.nodes.get("Principled BSDF")
        md = MATS[name]
        if "bake" in md:
            col = md["bake"]
        elif md.get("tex"):
            col = tuple(np.array(texmeans[md["tex"]]) * np.array(md.get("tint", (1, 1, 1))))
        else:
            col = md.get("color", (0.5, 0.5, 0.5))
        bsdf.inputs["Base Color"].default_value = (*[float(c) for c in col], 1.0)
        bsdf.inputs["Roughness"].default_value = 0.75
        bsdf.inputs["Metallic"].default_value = 0.0
        if target:
            tex = nt.nodes.new("ShaderNodeTexImage")
            tex.image = img
            nt.nodes.active = tex
            tex.select = True
        bmats[k] = m
        return m

    def make(name, plist, target):
        plist = [p for p in plist if len(p["T"])]
        P = np.concatenate([p["P"] for p in plist])
        N = np.concatenate([p["N"] for p in plist])
        offs = np.cumsum([0] + [len(p["P"]) for p in plist])[:-1]
        T = np.concatenate([p["T"] + o for p, o in zip(plist, offs)])
        names = sorted({p["mat"] for p in plist})
        mi = np.concatenate([np.full(len(p["T"]), names.index(p["mat"])) for p in plist])
        me = bpy.data.meshes.new(name)
        Pb = np.stack([P[:, 0], -P[:, 2], P[:, 1]], 1).astype(np.float32)
        Nb = np.stack([N[:, 0], -N[:, 2], N[:, 1]], 1).astype(np.float32)
        nv, nt_ = len(P), len(T)
        me.vertices.add(nv)
        me.vertices.foreach_set("co", Pb.ravel())
        me.loops.add(nt_ * 3)
        me.loops.foreach_set("vertex_index", T.ravel().astype(np.int32))
        me.polygons.add(nt_)
        me.polygons.foreach_set("loop_start", np.arange(0, nt_ * 3, 3, dtype=np.int32))
        try:
            me.polygons.foreach_set("loop_total", np.full(nt_, 3, np.int32))
        except Exception:
            pass
        me.update(calc_edges=True)
        for nm in names:
            me.materials.append(bmat(nm, target))
        me.polygons.foreach_set("material_index", mi.astype(np.int32))
        if target:
            UV = np.concatenate([p["UV1"] for p in plist])
            uvb = np.stack([UV[:, 0], 1.0 - UV[:, 1]], 1)
            lay = me.uv_layers.new(name="lm")
            lay.data.foreach_set("uv", uvb[T.ravel()].astype(np.float32).ravel())
        me.polygons.foreach_set("use_smooth", np.ones(nt_, bool))
        try:
            me.normals_split_custom_set(Nb[T.ravel()].tolist())
        except Exception as e:
            print("  (custom normals unavailable:", e, ")")
        me.update()
        ob = bpy.data.objects.new(name, me)
        sc.collection.objects.link(ob)
        return ob

    baked = [p for p in G.patches if MATS[p["mat"]]["baked"] and p["dens"] > 0]
    occl = [p for p in G.patches if not (MATS[p["mat"]]["baked"] and p["dens"] > 0) and MATS[p["mat"]]["kind"] not in ("glass", "lamp")]
    tgt = make("bake", baked, True)
    make("occl", occl, False)
    gm = bpy.data.meshes.new("ground")
    gm.from_pydata([(-40, -40, -1.15), (40, -40, -1.15), (40, 40, -1.15), (-40, 40, -1.15)], [], [(0, 1, 2, 3)])
    gm.materials.append(bmat("iron", False))
    go = bpy.data.objects.new("ground", gm)
    sc.collection.objects.link(go)
    for i, lt in enumerate(bake_lights()):
        pos, col, pw, rad = lt[:4]
        spot = len(lt) > 4 and lt[4] == "spot"
        ld = bpy.data.lights.new("L%d" % i, "SPOT" if spot else "POINT")
        ld.energy = pw
        ld.color = col
        ld.shadow_soft_size = rad
        if spot:
            ld.spot_size = math.radians(150)
            ld.spot_blend = 0.6
        ob = bpy.data.objects.new("L%d" % i, ld)
        ob.location = (pos[0], -pos[2], pos[1])
        sc.collection.objects.link(ob)
    for o in sc.objects:
        o.select_set(o == tgt)
    bpy.context.view_layer.objects.active = tgt
    t0 = time.time()
    bpy.ops.object.bake(type="DIFFUSE", pass_filter={"DIRECT", "INDIRECT"}, margin=0, use_clear=True, uv_layer="lm")
    print(f"  bake: {time.time() - t0:.0f}s ({samples} samples, {res}^2)")
    buf = np.empty(res * res * 4, np.float32)
    img.pixels.foreach_get(buf)
    return buf.reshape(res, res, 4)[::-1].copy()


def post_lightmap(px, idmap):
    L = px[..., :3].astype(np.float32)
    cov = px[..., 3] > 0.5
    # chart-aware bilateral denoise
    lum = L.mean(-1)
    acc = np.zeros_like(L)
    ws = np.zeros(lum.shape, np.float32)
    r = 2
    for dy in range(-r, r + 1):
        for dx in range(-r, r + 1):
            g = math.exp(-(dx * dx + dy * dy) / (2 * 1.25 ** 2))
            Ls = np.roll(L, (dy, dx), (0, 1))
            w = g * np.roll(cov, (dy, dx), (0, 1)) * (np.roll(idmap, (dy, dx), (0, 1)) == idmap)
            lm = np.roll(lum, (dy, dx), (0, 1))
            w = w * np.exp(-((lm - lum) / (0.3 * lum + 0.004)) ** 2)
            acc += w[..., None] * Ls
            ws += w
    L = np.where((cov & (ws > 0))[..., None], acc / np.maximum(ws, 1e-8)[..., None], L)
    # dilate into the padding
    for _ in range(10):
        m = cov.astype(np.float32)
        s = np.zeros_like(L)
        c = np.zeros_like(m)
        for (dy, dx) in ((0, 1), (0, -1), (1, 0), (-1, 0), (1, 1), (1, -1), (-1, 1), (-1, -1)):
            s += np.roll(L * m[..., None], (dy, dx), (0, 1))
            c += np.roll(m, (dy, dx), (0, 1))
        new = (~cov) & (c > 0)
        L[new] = s[new] / c[new][:, None]
        cov = cov | new
    if cov.any():
        L[~cov] = L[cov].mean(0)
    return L


# =====================================================================================================================
# glTF (GLB) writer
# =====================================================================================================================
def write_glb(path, nodes, materials, extras):
    binb = bytearray()
    views, accs = [], []

    def view(data, target):
        off = len(binb)
        binb.extend(data)
        while len(binb) % 4:
            binb.append(0)
        v = {"buffer": 0, "byteOffset": off, "byteLength": len(data)}
        if target:
            v["target"] = target
        views.append(v)
        return len(views) - 1

    def acc(arr, ctype, typ, target, mm=False):
        a = {"bufferView": view(arr.tobytes(), target), "componentType": ctype, "count": int(len(arr)), "type": typ}
        if mm:
            a["min"] = [float(v) for v in arr.min(0)]
            a["max"] = [float(v) for v in arr.max(0)]
        accs.append(a)
        return len(accs) - 1

    meshes, gn = [], []
    for name, prims in nodes:
        ps = []
        for (mi, P, N, UV0, UV1, I) in prims:
            at = {"POSITION": acc(P.astype(np.float32), 5126, "VEC3", 34962, True), "NORMAL": acc(N.astype(np.float32), 5126, "VEC3", 34962)}
            if UV0 is not None:
                at["TEXCOORD_0"] = acc(UV0.astype(np.float32), 5126, "VEC2", 34962)
            if UV1 is not None:
                at["TEXCOORD_1"] = acc(UV1.astype(np.float32), 5126, "VEC2", 34962)
            big = len(P) > 65535
            ix = I.astype(np.uint32 if big else np.uint16).ravel()
            ps.append({"attributes": at, "indices": acc(ix, 5125 if big else 5123, "SCALAR", 34963), "material": mi})
        meshes.append({"name": name, "primitives": ps})
        gn.append({"name": name, "mesh": len(meshes) - 1})
    gltf = {"asset": {"version": "2.0", "generator": "AfterZero tools/props/build_train.py"}, "scene": 0,
            "scenes": [{"name": "train", "nodes": list(range(len(gn))), "extras": extras}], "nodes": gn, "meshes": meshes,
            "materials": materials, "accessors": accs, "bufferViews": views, "buffers": [{"byteLength": len(binb)}]}
    js = json.dumps(gltf, separators=(",", ":")).encode()
    while len(js) % 4:
        js += b" "
    out = struct.pack("<III", 0x46546C67, 2, 12 + 8 + len(js) + 8 + len(binb))
    out += struct.pack("<II", len(js), 0x4E4F534A) + js + struct.pack("<II", len(binb), 0x004E4942) + bytes(binb)
    with open(path, "wb") as f:
        f.write(out)


def export(G, path, lm_scale, res, rho):
    order = ["interior", "pendants", "doorL", "doorR", "panes", "exterior", "coat", "watch", "letter"]
    mnames = list(MATS.keys())
    materials = []
    for k in mnames:
        md = MATS[k]
        col = md.get("color", md.get("tint", (1, 1, 1)))
        mat = {"name": k, "pbrMetallicRoughness": {"baseColorFactor": [float(c) for c in col] + [1.0], "metallicFactor": float(md["metal"]),
                                                   "roughnessFactor": float(md["rough"])},
               "extras": {"kind": md["kind"], "tex": md.get("tex"), "tile": md.get("tile", 1.0), "baked": md["baked"]}}
        if md.get("emissive"):
            mat["emissiveFactor"] = [float(c) for c in md["emissive"]]
        materials.append(mat)
    nodes = []
    stats = {}
    for nd in order:
        prims = []
        for mk in mnames:
            ps = [p for p in G.patches if p["node"] == nd and p["mat"] == mk and len(p["T"])]
            if not ps:
                continue
            P = np.concatenate([p["P"] for p in ps])
            N = np.concatenate([p["N"] for p in ps])
            offs = np.cumsum([0] + [len(p["P"]) for p in ps])[:-1]
            I = np.concatenate([p["T"] + o for p, o in zip(ps, offs)])
            tile = MATS[mk].get("tile", 1.0)
            UV0 = np.concatenate([p["UV"] for p in ps]) / (tile if MATS[mk].get("tex") not in ("decals", "carpet") else 1.0)
            UV0 = UV0 / UV0_SCALE + 0.5
            UV1 = np.concatenate([p.get("UV1", np.zeros((len(p["P"]), 2))) for p in ps]) if MATS[mk]["baked"] else None
            prims.append((mnames.index(mk), P, N, UV0, UV1, I))
            stats[nd] = stats.get(nd, 0) + len(I)
        if prims:
            nodes.append((nd, prims))
    lights = [{"p": list(lt[0]), "c": list(lt[1]), "w": lt[2]} for lt in bake_lights() if not (len(lt) == 4 and lt[2] < 5 and lt[0][1] > 2.6)]
    extras = {
        "lightmap": {"file": "train_lightmap.jpg", "scale": float(lm_scale), "size": res, "texelsPerMetre": round(rho, 1)},
        "uv0": {"scale": UV0_SCALE, "offset": 0.5},
        "layout": {"W": W, "WO": WO, "WG": WG, "A": A, "ZB": ZB, "ZF": ZF, "ZBO": ZBO, "ZFO": ZFO, "bays": BAYS, "partitions": PARTS,
                   "windows": [[zc, hw] for (zc, hw) in WINDOWS], "winY": [WIN_Y0, WIN_Y1], "crown": CROWN, "seatY": SEAT_Y,
                   "door": {"hw": DOOR_HW, "h": DOOR_H, "z0": DOOR_Z0, "z1": DOOR_Z1}, "coat": list(COAT_AT), "letter": list(LETTER_AT)},
        "lights": lights,
    }
    write_glb(path, nodes, materials, extras)
    print("  triangles per node:", stats, "total", sum(stats.values()))


def meshopt(path):
    cli = os.environ.get("GLTF_TRANSFORM") or shutil.which("gltf-transform")
    if not cli:
        guess = os.path.join(os.environ.get("AZ_SCRATCH", ""), "tools", "node_modules", ".bin", "gltf-transform")
        cli = guess if os.path.exists(guess) else None
    if not cli:
        print("  (gltf-transform not found: GLB left uncompressed; set GLTF_TRANSFORM to enable meshopt)")
        return
    tmp = path + ".tmp.glb"
    r = subprocess.run([cli, "meshopt", path, tmp, "--level", "medium", "--quantize-position", "16", "--quantize-texcoord", "16",
                        "--quantize-normal", "10"], capture_output=True, text=True)
    if r.returncode == 0 and os.path.exists(tmp):
        os.replace(tmp, path)
    else:
        print("  meshopt failed:", r.stderr[-400:])


# =====================================================================================================================
def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--samples", type=int, default=160)
    ap.add_argument("--res", type=int, default=2048)
    ap.add_argument("--fast", action="store_true", help="quick low-res bake for layout iteration")
    ap.add_argument("--no-bake", action="store_true", help="flat lightmap (geometry only)")
    ap.add_argument("--textures-only", action="store_true")
    args = ap.parse_args([a for a in sys.argv[1:] if a.startswith("--")] if "--" not in sys.argv else sys.argv[sys.argv.index("--") + 1:])
    if args.fast:
        args.res, args.samples = 1024, 24
    os.makedirs(OUT, exist_ok=True)
    t0 = time.time()
    print("textures ...")
    from PIL import Image  # noqa: F401
    texmeans = {}
    for name, fn, size in (("wood", tex_wood, 512), ("moquette", tex_moquette, 512), ("velvet", tex_velvet, 256), ("carpet", tex_carpet, 512),
                           ("ceiling", tex_ceiling, 256)):
        a = fn(size)
        texmeans[name] = [float(v) for v in a.reshape(-1, 3).mean(0)]
        save(os.path.join(OUT, "train_%s.jpg" % name), to_srgb(a), 86, 2)
    dec = make_decals()
    save(os.path.join(OUT, "train_decals.jpg"), dec, 88, 0)
    texmeans["decals"] = [float(v) for v in (dec.reshape(-1, 3) ** 2.2).mean(0)]
    if args.textures_only:
        return
    print("geometry ...")
    G = build_carriage()
    G.texmeans = texmeans
    print("  patches:", len(G.patches))
    rho, idmap = pack_charts(G.patches, args.res)
    if args.no_bake:
        L = np.full((args.res, args.res, 3), 0.35, np.float32)
    else:
        print("baking ...")
        px = blender_bake(G, args.res, args.samples)
        L = post_lightmap(px, idmap)
    lum = L.mean(-1)
    used = idmap >= 0
    scale = float(np.percentile(lum[used], 99.6)) * 1.15 if used.any() else 1.0
    enc = to_srgb(np.clip(L / scale, 0, 1))
    save(os.path.join(OUT, "train_lightmap.jpg"), enc, 90, 0)
    print("export ...")
    path = os.path.join(OUT, "train_carriage.glb")
    export(G, path, scale, args.res, rho)
    meshopt(path)
    for f in sorted(os.listdir(OUT)):
        if f.startswith("train_"):
            print("  %-26s %8.1f KB" % (f, os.path.getsize(os.path.join(OUT, f)) / 1024))
    print("done in %.0fs" % (time.time() - t0))


def save(path, arr, q, sub):
    from PIL import Image
    Image.fromarray(np.clip(arr * 255 + 0.5, 0, 255).astype(np.uint8)).save(path, quality=q, optimize=True, progressive=True, subsampling=sub)


if __name__ == "__main__":
    main()
