"""AFTERZERO realistic cast builder: MakeHuman sculpting (mh.py) + our own skin/clothes/hair texturing, game rig and
animations, in Blender (bpy). Every character is designed in cast_humans.py.

    export MH_DATA=/path/to/makehuman/makehuman/data
    venv/bin/python build_humans.py OUT_DIR            # ONLY=arin,pell to build a subset

Outputs OUT_DIR/cast.glb (all characters) and OUT_DIR/anims.glb (reference rig + clips).
Per character: one texture atlas (skin + clothes + hair, painted procedurally in the body's UV space) and one small
vertex-coloured "detail" material (eyes, lashes, skirts, hats, buttons, bags) -> two draw calls.
"""
import json, math, os, sys
import numpy as np
import bpy, bmesh
from mathutils import Vector, Matrix

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import mh  # noqa: E402
import build_characters as BC  # noqa: E402  (tube/shell helpers, clips, export)
from cast_humans import CAST  # noqa: E402

V = Vector
OURS = {  # MakeHuman bone -> our game bone
    "root": "hips", "spine05": "hips", "pelvis.L": "hips", "pelvis.R": "hips",
    "spine04": "spine", "spine03": "spine", "spine02": "chest", "spine01": "chest", "breast.L": "chest", "breast.R": "chest",
    "neck01": "neck", "neck02": "neck", "neck03": "neck",
}
for s in "LR":
    OURS.update({f"clavicle.{s}": f"clavicle_{s}", f"shoulder01.{s}": f"clavicle_{s}", f"upperarm01.{s}": f"upperarm_{s}", f"upperarm02.{s}": f"upperarm_{s}",
                 f"lowerarm01.{s}": f"forearm_{s}", f"lowerarm02.{s}": f"forearm_{s}", f"wrist.{s}": f"hand_{s}",
                 f"upperleg01.{s}": f"thigh_{s}", f"upperleg02.{s}": f"thigh_{s}", f"lowerleg01.{s}": f"shin_{s}", f"lowerleg02.{s}": f"shin_{s}",
                 f"foot.{s}": f"foot_{s}"})


def our_bone(mhb):
    if mhb in OURS:
        return OURS[mhb]
    side = "L" if mhb.endswith(".L") else "R" if mhb.endswith(".R") else None
    if side and (mhb.startswith("finger") or mhb.startswith("metacarpal")):
        return "hand_" + side
    if side and mhb.startswith("toe"):
        return "toe_" + side
    return "head"   # face muscles, jaw, eyes, tongue, special*


def to_bl(p):
    """MakeHuman (x left, y up, z forward) metres -> Blender (x left, -y forward, z up)."""
    return V((p[0], -p[2], p[1]))


# ----------------------------------------------------------------------------------------------------------------
# skeleton (from MakeHuman joints) and weights
# ----------------------------------------------------------------------------------------------------------------
SKEL = None


def skel():
    global SKEL
    if SKEL is None:
        SKEL = json.load(open(mh.path("rigs", "default.mhskel")))
        SKEL["w"] = json.load(open(mh.path("rigs", "default_weights.mhw")))["weights"]
    return SKEL


def jpos(P, name):
    return P[skel()["joints"][name]].mean(axis=0)


def rig_points(P):
    """Our rig joints in Blender space from MakeHuman bone heads/tails."""
    h = lambda b: to_bl(jpos(P, b + "____head"))
    t = lambda b: to_bl(jpos(P, b + "____tail"))
    J = {"pelvis": h("spine05"), "spine": h("spine04"), "chest": h("spine02"), "neck": h("neck01"), "head": h("head"), "headTop": t("head")}
    for s in "LR":
        J["clav_" + s] = h(f"clavicle.{s}")
        J["shoulder_" + s] = h(f"upperarm01.{s}")
        J["elbow_" + s] = h(f"lowerarm01.{s}")
        J["wrist_" + s] = h(f"wrist.{s}")
        J["handEnd_" + s] = h(f"finger3-2.{s}")
        J["hip_" + s] = h(f"upperleg01.{s}")
        J["knee_" + s] = h(f"lowerleg01.{s}")
        J["ankle_" + s] = h(f"foot.{s}")
        J["ball_" + s] = h(f"toe3-1.{s}")
        J["toe_" + s] = t(f"toe3-3.{s}")
    return J


def our_weights(n_base):
    """(n_base, bones) dict: per base-vertex weights folded onto our bones."""
    W = {}
    for mhb, lst in skel()["w"].items():
        ob = our_bone(mhb)
        for vi, w in lst:
            d = W.setdefault(vi, {})
            d[ob] = d.get(ob, 0.0) + w
    return W


# ----------------------------------------------------------------------------------------------------------------
# proxies (.mhclo) e.g. eyes
# ----------------------------------------------------------------------------------------------------------------
def fit_proxy(rel, Pdm):
    """Fit a MakeHuman proxy (eyes) to sculpted base vertices (decimetres). Returns verts (metres), faces."""
    d = os.path.dirname(mh.path(rel))
    scale = {}
    rows, obj = [], None
    in_verts = False
    for line in open(mh.path(rel)):
        a = line.split()
        if not a or a[0].startswith("#"):
            continue
        if a[0] == "obj_file":
            obj = os.path.join(d, a[1])
        elif a[0] in ("x_scale", "y_scale", "z_scale"):
            ax = "xyz".index(a[0][0])
            scale[ax] = abs(Pdm[int(a[1])][ax] - Pdm[int(a[2])][ax]) / float(a[3])
        elif a[0] == "verts":
            in_verts = True
        elif in_verts and len(a) in (1, 9):
            rows.append(a)
        elif in_verts:
            in_verts = False
    out = []
    for a in rows:
        if len(a) == 1:
            out.append(Pdm[int(a[0])])
            continue
        i = [int(x) for x in a[:3]]
        w = [float(x) for x in a[3:6]]
        off = np.array([float(x) for x in a[6:9]]) * np.array([scale.get(0, 1), scale.get(1, 1), scale.get(2, 1)])
        out.append(w[0] * Pdm[i[0]] + w[1] * Pdm[i[1]] + w[2] * Pdm[i[2]] + off)
    faces = []
    for line in open(obj):
        if line.startswith("f "):
            faces.append([int(t.split("/")[0]) - 1 for t in line.split()[1:]])
    return np.array(out) * 0.1, faces


# ----------------------------------------------------------------------------------------------------------------
# colour helpers (sRGB, 0..1)
# ----------------------------------------------------------------------------------------------------------------
def hexc(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) / 255 for i in (0, 2, 4)])


def lin(c):
    c = np.asarray(c, dtype=float)
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)


def vnoise(p, f, seed=0.0):
    """Smooth pseudo-noise of 3D points p (N,3) at frequency f (per metre); 0..1."""
    x, y, z = p[:, 0] * f + seed, p[:, 1] * f + seed * 0.7, p[:, 2] * f - seed * 0.3
    n = (np.sin(x * 1.7 + np.sin(y * 2.3)) + np.sin(y * 1.9 + np.sin(z * 2.9)) + np.sin(z * 2.1 + np.sin(x * 3.1))
         + 0.5 * np.sin(x * 4.3 + y * 3.7) + 0.5 * np.sin(z * 4.1 - y * 3.3))
    return n / 8 + 0.5


def g2(d2, s):
    return np.exp(-d2 / (2 * s * s))


# ----------------------------------------------------------------------------------------------------------------
# region of each base vertex (by dominant game bone) + hair / face masks
# ----------------------------------------------------------------------------------------------------------------
def vertex_bone(Wv, n):
    out = ["hips"] * n
    for vi, d in Wv.items():
        if vi < n:
            out[vi] = max(d.items(), key=lambda kv: kv[1])[0]
    return out


class Landmarks:
    def __init__(self, P):
        self.eyeL = P[mh.group_vertices("helper-l-eye")].mean(axis=0)
        self.eyeR = P[mh.group_vertices("helper-r-eye")].mean(axis=0)
        self.eye = (self.eyeL + self.eyeR) / 2
        self.mouth = mh.joint(P, "mouth")
        head_v = P[mh.group_vertices("helper-hair")] if mh.group_vertices("helper-hair") else P
        self.top = head_v[:, 1].max()
        self.H = P[:, 1].max() - P[:, 1].min()
        self.ground = P[:, 1].min()
        upper = P[mh.group_vertices("helper-upper-teeth")].mean(axis=0)
        self.mouth = np.array([self.mouth[0], upper[1] - 0.004, max(self.mouth[2], upper[2]) + 0.01])


def hair_mask(P, L, style):
    """True for scalp vertices covered by the given hair style."""
    e = L.eye
    rel = P - e
    front = rel[:, 2] > -0.035                # in front of the ear line
    up = rel[:, 1]
    ear = (np.abs(rel[:, 0]) > 0.062) & (up < 0.03) & (up > -0.06) & (rel[:, 2] < -0.04) & (rel[:, 2] > -0.11)
    hairline_front = {"crop": 0.05, "swept": 0.055, "short": 0.045, "bob": 0.04, "long": 0.04, "bun": 0.045, "braid": 0.045, "pony": 0.045}.get(style, 0.045)
    nape = {"crop": -0.05, "bob": -0.09, "long": -0.10}.get(style, -0.075)
    side_low = {"bob": -0.04, "long": -0.04}.get(style, 0.0)
    m = np.where(front, up > hairline_front - 0.015 * np.clip(np.abs(rel[:, 0]) / 0.06, 0, 1),
                 np.where(rel[:, 2] < -0.075, up > nape, up > side_low))
    head = up > -0.12
    return m & head & ~ear & (rel[:, 1] < 0.2)


def hair_soft(pos, L, style):
    """0..1 fade band just below the hairline (so the edge reads as hair, not a cap)."""
    rel = pos - L.eye
    hf = {"crop": 0.05, "swept": 0.055, "short": 0.045}.get(style, 0.042)
    front = rel[:, 2] > -0.035
    edge = np.where(front, hf - 0.015 * np.clip(np.abs(rel[:, 0]) / 0.06, 0, 1), 0.0)
    d = rel[:, 1] - edge
    band = np.clip(1 - (-d) / 0.012, 0, 1) * (d < 0.004) * (rel[:, 1] > -0.12)
    ear = (np.abs(rel[:, 0]) > 0.06) & (rel[:, 1] < 0.03) & (rel[:, 1] > -0.06)
    return band * ~ear * front


# ----------------------------------------------------------------------------------------------------------------
# texture atlas: rasterise base-mesh triangles in UV space, colour each texel from its 3D position + region
# ----------------------------------------------------------------------------------------------------------------
def rasterize(F_uv, UV, P, face_ids, res):
    """Returns per-texel arrays: px, py, pos(3), face index."""
    pxs, pys, poss, fids = [], [], [], []
    for fi in face_ids:
        vs, ts, _ = F_uv[fi]
        tris = [(0, 1, 2), (0, 2, 3)] if len(vs) == 4 else [(0, 1, 2)]
        for a, b, c in tris:
            uv = np.array([UV[ts[a]], UV[ts[b]], UV[ts[c]]]) * res
            uv[:, 1] = res - uv[:, 1]
            x0, y0 = np.floor(uv.min(axis=0)).astype(int)
            x1, y1 = np.ceil(uv.max(axis=0)).astype(int)
            x0, y0, x1, y1 = max(x0, 0), max(y0, 0), min(x1, res - 1), min(y1, res - 1)
            if x1 < x0 or y1 < y0:
                continue
            gx, gy = np.meshgrid(np.arange(x0, x1 + 1) + 0.5, np.arange(y0, y1 + 1) + 0.5)
            gx, gy = gx.ravel(), gy.ravel()
            (ax, ay), (bx, by), (cx, cy) = uv
            den = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
            if abs(den) < 1e-12:
                continue
            l1 = ((by - cy) * (gx - cx) + (cx - bx) * (gy - cy)) / den
            l2 = ((cy - ay) * (gx - cx) + (ax - cx) * (gy - cy)) / den
            l3 = 1 - l1 - l2
            ok = (l1 >= -0.02) & (l2 >= -0.02) & (l3 >= -0.02)
            if not ok.any():
                continue
            pa, pb, pc = P[vs[a]], P[vs[b]], P[vs[c]]
            pos = l1[ok, None] * pa + l2[ok, None] * pb + l3[ok, None] * pc
            pxs.append(gx[ok].astype(int))
            pys.append(gy[ok].astype(int))
            poss.append(pos)
            fids.append(np.full(ok.sum(), fi))
    return np.concatenate(pxs), np.concatenate(pys), np.concatenate(poss), np.concatenate(fids)


def dilate(img, mask, n=6):
    """Bleed filled texels outward so mip-maps never pull in empty background at UV seams."""
    img, mask = img.copy(), mask.copy()
    for _ in range(n):
        acc = np.zeros_like(img)
        cnt = np.zeros(mask.shape)
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            m2 = np.roll(np.roll(mask, dy, 0), dx, 1)
            acc += np.roll(np.roll(img, dy, 0), dx, 1) * m2[..., None]
            cnt += m2
        new = (~mask) & (cnt > 0)
        img[new] = acc[new] / cnt[new][:, None]
        mask = mask | new
    return img


def paint_atlas(c, P, L, face_region, F_uv, UV, face_ids, res, path_out):
    """face_region: dict face index -> region key ('skin', 'hair', 'top', 'bottom', 'shoe', 'collar')."""
    px, py, pos, fid = rasterize(F_uv, UV, P, face_ids, res)
    reg = np.array([face_region[f] for f in fid])
    n = len(px)
    col = np.zeros((n, 3))
    seed = (sum(map(ord, c["id"])) % 97) * 0.37
    # ---- skin ----
    skin = hexc(c["skin"])
    rel = pos - L.eye
    fr = np.clip(rel[:, 2] + 0.05, 0, 1) * 20  # 'front of face' weight (0 behind)
    fr = np.clip(fr, 0, 1)
    base = skin[None, :] * (0.95 + 0.05 * vnoise(pos, 140, seed)[:, None] + 0.03 * vnoise(pos, 600, seed)[:, None] - 0.015)
    warm = np.array([1.0, 0.82, 0.78])
    # redness: cheeks, nose tip, lips area, ears, knuckles
    d_mouth = ((pos - L.mouth) ** 2 * np.array([1, 4.0, 2])).sum(1)
    cheekL = L.eyeL + np.array([0.012, -0.035, -0.005])
    cheekR = L.eyeR + np.array([-0.012, -0.035, -0.005])
    blush = c.get("blush", 0.25) * (g2(((pos - cheekL) ** 2).sum(1), 0.016) + g2(((pos - cheekR) ** 2).sum(1), 0.016)) * fr
    nose_tip = np.array([L.eye[0], L.eye[1] - 0.042, L.eye[2] + 0.035])
    blush += 0.25 * g2(((pos - nose_tip) ** 2).sum(1), 0.012) * fr
    base = base * (1 - blush[:, None]) + base * warm[None, :] * blush[:, None]
    # lips
    lip_c = hexc(c.get("lip", "b06a62"))
    lipm = g2(((pos[:, 0] - L.mouth[0]) / 0.021) ** 2 + ((pos[:, 1] - L.mouth[1]) / 0.0075) ** 2, 1.0) * fr
    lipm = np.clip(lipm * 1.3, 0, 1) * 0.75
    base = base * (1 - lipm[:, None]) + (lip_c[None, :] * 0.6 + skin[None, :] * 0.4) * lipm[:, None]
    line = g2((pos[:, 1] - L.mouth[1]) ** 2, 0.0009) * g2(((pos[:, 0] - L.mouth[0]) / 0.02) ** 2, 1.0) * fr
    base *= (1 - 0.22 * line)[:, None]
    # eyebrows
    hair_c = hexc(c.get("hairColor", "2e2119"))
    brow_c = hexc(c.get("browColor", c.get("hairColor", "2e2119")))
    for e, sd in ((L.eyeL, 1), (L.eyeR, -1)):
        dx = (pos[:, 0] - e[0]) * sd   # outward positive
        t = np.clip((dx + 0.016) / 0.036, 0, 1)
        arch_y = e[1] + 0.0205 + 0.006 * np.sin(np.pi * np.clip(t * 1.25, 0, 1)) - 0.003 * t
        thick = (0.0042 + 0.002 * c.get("browThick", 0.5)) * (1.15 - 0.6 * t)
        bm = g2((pos[:, 1] - arch_y) ** 2, thick) * ((dx > -0.017) & (dx < 0.022)) * fr
        bm *= 0.75 + 0.25 * vnoise(pos * np.array([1, 3, 1]), 900, seed)
        bm = np.clip(bm * 1.15, 0, 0.85)
        base = base * (1 - bm[:, None]) + brow_c[None, :] * bm[:, None]
        # lid crease shadow + lash line
        lid = g2(((pos[:, 0] - e[0]) / 0.016) ** 2 + ((pos[:, 1] - e[1] - 0.009) / 0.004) ** 2, 1.0) * fr
        base *= (1 - 0.1 * lid)[:, None]
    # stubble / beard shading
    stub = c.get("stubble", 0.0)
    if stub:
        sstep = lambda x, a, b: np.clip((x - a) / (b - a), 0, 1)
        dy = pos[:, 1] - L.mouth[1]
        jaw = sstep(-dy, -0.02, -0.004) * sstep(dy, -0.085, -0.06) * sstep(rel[:, 2], -0.09, -0.06)
        lip_hole = g2(((pos[:, 0] - L.mouth[0]) / 0.022) ** 2 + (dy / 0.009) ** 2, 0.7)
        sm = stub * jaw * (1 - lip_hole) * (0.6 + 0.4 * vnoise(pos, 1500, seed))
        base = base * (1 - 0.45 * sm[:, None]) + hair_c[None, :] * 0.45 * sm[:, None]
    # soft hairline: hair colour fades onto the skin just outside the hair shell
    if c.get("hair", "short") not in ("none",) and c.get("hat") != "hood":
        soft = hair_soft(pos, L, c.get("hair", "short"))
        sm = (soft * (0.55 + 0.45 * vnoise(pos * np.array([1, 4, 1]), 1200, seed)))[:, None]
        base = base * (1 - sm) + hair_c[None, :] * sm
    col = base
    # ---- hair (painted on the scalp + shell, strand streaks along the head) ----
    hm = reg == "hair"
    if hm.any():
        p = pos[hm] - L.eye
        ang = np.arctan2(p[:, 0], -p[:, 2])
        k = 0.78 + 0.22 * (0.5 + 0.5 * np.sin(ang * 90 + np.sin(p[:, 1] * 60) * 2)) + 0.1 * vnoise(pos[hm], 400, seed) - 0.05
        hl = 1 + 0.18 * g2((p[:, 1] - 0.085) ** 2, 0.02)   # sheen band
        col[hm] = hair_c[None, :] * (k * hl)[:, None]
    # ---- clothes ----
    for key, chex in (("top", c.get("colorA", "444a55")), ("bottom", c.get("colorB", "2a2a30")), ("shoe", c.get("shoeColor", "2a1d16")), ("collar", c.get("collarColor", c.get("colorA", "444a55")))):
        m = reg == key
        if not m.any():
            continue
        cc = hexc(chex)
        fab = c.get("fabric", {}).get(key, "wool" if key in ("top", "collar") else "twill" if key == "bottom" else "leather")
        q = pos[m]
        if fab == "leather":
            k = 0.85 + 0.25 * vnoise(q, 300, seed) + 0.08 * vnoise(q, 1800, seed)
        elif fab == "twill":
            k = 0.88 + 0.08 * np.sin((q[:, 0] + q[:, 1]) * 2600) + 0.1 * vnoise(q, 200, seed)
        else:  # wool / knit
            k = 0.86 + 0.07 * np.sin(q[:, 1] * 3000) * np.sin(q[:, 0] * 3000 + q[:, 2] * 3000) + 0.12 * vnoise(q, 160, seed)
        k = k * (0.92 + 0.08 * np.clip((q[:, 1] - L.ground) / (0.5 * L.H), 0, 1))   # grime toward the hem
        col[m] = cc[None, :] * k[:, None]
    img = np.zeros((res, res, 3))
    mask = np.zeros((res, res), bool)
    img[py, px] = np.clip(col, 0, 1)
    mask[py, px] = True
    img = dilate(img, mask, 8)
    img[~dilate(np.ones((res, res, 1)) * mask[..., None], mask, 8)[..., 0].astype(bool)] = img.mean(axis=(0, 1))
    from PIL import Image
    Image.fromarray((img * 255).astype(np.uint8)).save(path_out, quality=c.get("jpegQ", 86))
    return path_out


# ----------------------------------------------------------------------------------------------------------------
# geometry
# ----------------------------------------------------------------------------------------------------------------
def mesh_from(name, verts, faces, uvs=None):
    me = bpy.data.meshes.new(name)
    me.from_pydata([tuple(v) for v in verts], [], faces)
    if uvs is not None:
        uvl = me.uv_layers.new(name="UVMap")
        for poly in me.polygons:
            for li in poly.loop_indices:
                uvl.data[li].uv = uvs[li]
    me.update()
    for p in me.polygons:
        p.use_smooth = True
    o = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(o)
    return o


def faces_to_object(name, P, F, face_ids, offset=0.0, smooth_iter=0, vnormals=None, taper=0, relax=0):
    """Object from a subset of base faces (keeps base UVs). offset pushes along vertex normals (clothing / hair)."""
    _, UV, _ = mh.base()
    used = sorted({v for fi in face_ids for v in F[fi][0]})
    remap = {v: i for i, v in enumerate(used)}
    verts = np.array([to_bl(P[v]) for v in used])
    faces = [[remap[v] for v in F[fi][0]] for fi in face_ids]
    uvs = []
    for fi in face_ids:
        for t in F[fi][1]:
            uvs.append(tuple(UV[t]))
    o = mesh_from(name, verts, faces, uvs)
    o["base_index"] = used
    if smooth_iter:
        m = o.modifiers.new("smooth", "SMOOTH")
        m.factor, m.iterations = 0.6, smooth_iter
        BC.apply_modifiers(o)
    if relax:   # straighten zig-zag borders (necklines, hairlines) left by the base mesh's face edges
        bm = bmesh.new()
        bm.from_mesh(o.data)
        for _ in range(relax):
            new = {}
            for v in bm.verts:
                if not v.is_boundary:
                    continue
                nb = [e.other_vert(v) for e in v.link_edges if e.is_boundary]
                if len(nb) == 2:
                    new[v] = v.co * 0.4 + (nb[0].co + nb[1].co) * 0.3
            for v, co in new.items():
                v.co = co
        bm.to_mesh(o.data)
        bm.free()
    if offset:
        bm = bmesh.new()
        bm.from_mesh(o.data)
        bm.normal_update()
        ring = {}
        if taper:   # distance (in edge rings) from the open border -> thin edges (hairlines, cuffs)
            front = [v for v in bm.verts if v.is_boundary]
            for v in front:
                ring[v.index] = 0
            k = 0
            while front and k < taper:
                k += 1
                nxt = []
                for v in front:
                    for e in v.link_edges:
                        w = e.other_vert(v)
                        if w.index not in ring:
                            ring[w.index] = k
                            nxt.append(w)
                front = nxt
        for v in bm.verts:
            f = min(1.0, ring.get(v.index, taper) / taper) if taper else 1.0
            v.co += v.normal * offset * (0.15 + 0.85 * f)
        bm.to_mesh(o.data)
        bm.free()
    return o


def inflate(o, P, fitobj, bones_sel, vb_of, amount=1.0):
    """Loosen a garment: pull torso vertices toward a smooth elliptical shell (hides anatomy under coats)."""
    me = o.data
    for i, v in enumerate(me.vertices):
        bn = vb_of[i]
        if bn not in ("chest", "spine", "hips"):
            continue
        cen, rx, ry = fitobj.ring(v.co.z, 0.015, bones_sel)
        d = V((v.co.x - cen.x, v.co.y - cen.y, 0))
        if d.length < 1e-5:
            continue
        ang = math.atan2(d.y / max(ry, 1e-3), d.x / max(rx, 1e-3))
        target = V((math.cos(ang) * rx, math.sin(ang) * ry, 0)).length * 1.04
        r = d.length
        nr = r + (max(r, target) - r) * amount
        nr = nr * (1 - 0.6 * amount) + target * 0.6 * amount if r > target else nr
        v.co.x = cen.x + d.x / r * nr
        v.co.y = cen.y + d.y / r * nr


def vcolor(o, rgb_fn):
    a = o.data.color_attributes.get("Col") or o.data.color_attributes.new("Col", "FLOAT_COLOR", "POINT")
    for i, v in enumerate(o.data.vertices):
        a.data[i].color = (*rgb_fn(v.co), 1)


# ----------------------------------------------------------------------------------------------------------------
# outfit coverage by game bone (+ height limits)
# ----------------------------------------------------------------------------------------------------------------
TOP_BONES = {"chest", "spine", "clavicle_L", "clavicle_R", "upperarm_L", "upperarm_R"}
SLEEVE_LONG = {"forearm_L", "forearm_R"}
LEG_BONES = {"thigh_L", "thigh_R", "shin_L", "shin_R"}
FOOT_BONES = {"foot_L", "foot_R", "toe_L", "toe_R"}


def face_regions(c, P, F, body_faces, vb, L):
    """Region per body face: skin / hair / top / bottom / shoe / collar."""
    top = c.get("top", "jacket")
    bottom = c.get("bottom", "trousers")
    sleeve = c.get("sleeve", "long")
    hairm = hair_mask(P, L, c.get("hair", "short")) if c.get("hair", "short") != "none" else np.zeros(len(P), bool)
    if c.get("hat") in ("hood",):
        hairm[:] = False
    boot = c.get("boots", 0.10)
    out = {}
    for fi in body_faces:
        vs = F[fi][0]
        bones = [vb[v] for v in vs]
        b = max(set(bones), key=bones.count)
        y = P[list(vs)][:, 1].mean() - L.ground
        r = "skin"
        if b in TOP_BONES or (b == "hips" and y > 0.535 * L.H) or (b in SLEEVE_LONG and sleeve == "long") or (b in SLEEVE_LONG and sleeve == "mid" and y > 0.0):
            r = "top"
            if b in SLEEVE_LONG and sleeve == "mid":
                ex = P[list(vs)][:, 1].mean()
                r = "top" if ex > mh_elbow_y(P, vs) - 0.06 else "skin"
        if b == "hips" and y <= 0.535 * L.H:
            r = "bottom" if bottom in ("trousers",) else "top" if top in ("dress", "robe") else "bottom"
        if b in LEG_BONES:
            r = "bottom" if bottom == "trousers" else "skin"
            if y < (0.045 + boot) * L.H:
                r = "shoe"
        if b in FOOT_BONES:
            r = "shoe"
        if b == "neck" and top in ("coat", "robe", "uniform", "sweater") and y < 0.845 * L.H:
            r = "collar"
        if r == "skin" and all(hairm[v] for v in vs):
            r = "hair"
        out[fi] = r
    return out


def mh_elbow_y(P, vs):
    return P[list(vs)][:, 1].mean()


# ----------------------------------------------------------------------------------------------------------------
# build one character
# ----------------------------------------------------------------------------------------------------------------
def build(c, tex_dir):
    Pdm = mh.sculpt(c["macro"], c.get("details")) * 10.0
    P = Pdm * 0.1
    P = P - np.array([0, P[:, 1].min(), 0])
    Pdm = P * 10
    Vb, UV, F = mh.base()
    L = Landmarks(P)
    body_faces = [i for i, f in enumerate(F) if f[2] == "body"]
    Wv = our_weights(len(P))
    vb = vertex_bone(Wv, len(P))
    regions = face_regions(c, P, F, body_faces, vb, L)
    c["_vb"] = vb
    # ---- atlas ----
    res = c.get("res", 1024)
    tex = paint_atlas(c, P, L, regions, F, UV, body_faces, res, os.path.join(tex_dir, c["id"] + ".jpg"))
    # ---- body (skin faces only; covered faces live on the garment layers) ----
    skin_faces = [f for f in body_faces if regions[f] in ("skin",) or (regions[f] == "hair" and c.get("hair") == "crop")]
    hair_faces = [f for f in body_faces if regions[f] == "hair"]
    top_faces = [f for f in body_faces if regions[f] in ("top", "collar")]
    bot_faces = [f for f in body_faces if regions[f] == "bottom"]
    shoe_faces = [f for f in body_faces if regions[f] == "shoe"]
    keep_under = c.get("hair") == "crop"
    parts = []
    body = faces_to_object(c["id"] + "_body", P, F, skin_faces + (hair_faces if not keep_under else []) * 0)
    parts.append(body)
    thick = {"coat": 0.012, "robe": 0.014, "jacket": 0.009, "uniform": 0.009, "sweater": 0.008, "vest": 0.006, "shirt": 0.005, "dress": 0.006}.get(c.get("top"), 0.008)
    if top_faces:
        loose = {"coat": 10, "robe": 12, "jacket": 8, "uniform": 8, "sweater": 8, "dress": 8}.get(c.get("top"), 6)
        topo = faces_to_object(c["id"] + "_top", P, F, top_faces, offset=thick, smooth_iter=loose, taper=2, relax=6)
        amt = {"coat": 1.0, "robe": 1.0, "jacket": 0.85, "uniform": 0.8, "sweater": 0.75, "dress": 0.7}.get(c.get("top"), 0.5)
        fitb = Fit(P, mh.group_vertices("body"), vb)
        inflate(topo, P, fitb, fitb.of("chest", "spine", "hips"), [vb[bi] for bi in topo["base_index"]], amt)
        parts.append(topo)
        nb = [topo.matrix_world @ v.co for v in topo.data.vertices]
        bm = bmesh.new(); bm.from_mesh(topo.data)
        neck_z = rig_points(P)["chest"].z
        bnd = np.array([tuple(v.co) for v in bm.verts if v.is_boundary and v.co.z > neck_z and abs(v.co.x) < 0.13])
        bm.free()
        if len(bnd) > 6:
            c["_neckline"] = (V((float((bnd[:, 0].max() + bnd[:, 0].min()) / 2), float((bnd[:, 1].max() + bnd[:, 1].min()) / 2), float(np.median(bnd[:, 2])))),
                              float((bnd[:, 0].max() - bnd[:, 0].min()) / 2), float((bnd[:, 1].max() - bnd[:, 1].min()) / 2), float(bnd[:, 2].max() - bnd[:, 2].min()))
    if bot_faces:
        parts.append(faces_to_object(c["id"] + "_bottom", P, F, bot_faces, offset=0.006, smooth_iter=5))
    if shoe_faces:   # shoe shafts follow the leg; the feet themselves are lofted shoes (see extras)
        shaft = [f for f in shoe_faces if vb[F[f][0][0]] not in FOOT_BONES]
        if shaft:
            parts.append(faces_to_object(c["id"] + "_shaft", P, F, shaft, offset=0.006, smooth_iter=6))
    if hair_faces:
        vol = {"crop": 0.002, "short": 0.008, "swept": 0.011, "bob": 0.012, "long": 0.010, "bun": 0.008, "braid": 0.008, "pony": 0.008}.get(c.get("hair"), 0.009)
        parts.append(faces_to_object(c["id"] + "_hair", P, F, hair_faces, offset=vol, smooth_iter=4, taper=3, relax=8))
    atlas_parts = list(parts)
    # ---- detail (vertex-coloured) ----
    details = []
    eyes_v, eyes_f = fit_proxy("eyes/high-poly/high-poly.mhclo", Pdm)
    iris = hexc(c.get("eyes", "5a4030"))
    eo = mesh_from("eyes", [to_bl(p) for p in eyes_v], eyes_f)
    cen = [to_bl(L.eyeL), to_bl(L.eyeR)]

    def eye_col(p):
        e = cen[0] if (p - cen[0]).length < (p - cen[1]).length else cen[1]
        d = (p - e)
        f = -d.normalized().y
        if f > 0.965:
            return (0.02, 0.02, 0.02)
        if f > 0.85:
            k = 0.7 + 0.3 * (f - 0.85) / 0.115
            return tuple(lin(iris * (1.15 - 0.4 * k)))
        return (0.6, 0.57, 0.54)

    vcolor(eo, eye_col)
    eo["rigid"] = "head"
    details.append(eo)
    # lashes from the helper geometry
    lash_faces = [i for i, f in enumerate(F) if f[2] and "eyelashes" in f[2]]
    if lash_faces:
        lo = faces_to_object("lashes", P, F, lash_faces)
        lo.data.uv_layers.clear() if hasattr(lo.data.uv_layers, "clear") else None
        vcolor(lo, lambda p: (0.02, 0.015, 0.012))
        lo["rigid"] = "head"
        details.append(lo)
    # accessories / skirts / hats re-using the older generator pieces in Blender space
    J = rig_points(P)
    details += extras(c, J, L, P)
    # ---- rig + weights ----
    arm = BC.build_armature(c["id"], J)
    for o in atlas_parts:
        skin_weights(o, arm, Wv)
    for o in details:
        if o.get("tag") == "skirt":
            BC.skirt_weight(o, arm, J, L.H)
        else:
            BC.rigid_weight(o, arm, o.get("rigid", "head"))
    # ---- materials ----
    atlas = atlas_material(c, tex)
    det = detail_material(c)
    for o in atlas_parts:
        o.data.materials.clear()
        o.data.materials.append(atlas)
        o.data.materials.append(det)
        if not o.data.color_attributes.get("Col"):
            vcolor(o, lambda p: (1, 1, 1))
    for o in details:
        o.data.materials.clear()
        o.data.materials.append(atlas)
        o.data.materials.append(det)
        for p in o.data.polygons:
            p.material_index = 1
        if not o.data.uv_layers:
            o.data.uv_layers.new(name="UVMap")
    # join
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    allp = atlas_parts + details
    for o in allp:
        o.select_set(True)
    bpy.context.view_layer.objects.active = atlas_parts[0]
    bpy.ops.object.join()
    mesh = atlas_parts[0]
    mesh.name = c["id"] + "_mesh"
    budget = c.get("tris", 10000)
    tris = sum(len(p.vertices) - 2 for p in mesh.data.polygons)
    if tris > budget:
        d = mesh.modifiers.new("dec", "DECIMATE")
        d.ratio = budget / tris
        d.use_collapse_triangulate = True
        bpy.context.view_layer.objects.active = mesh
        bpy.ops.object.modifier_move_to_index(modifier="dec", index=0)
        bpy.ops.object.modifier_apply(modifier="dec")
    arm["character"] = c["id"]
    return arm, mesh


def skin_weights(o, arm, Wv):
    o.vertex_groups.clear()
    groups = {}
    for i, bi in enumerate(o["base_index"]):
        d = Wv.get(bi)
        if not d:
            continue
        tot = sum(d.values())
        for bn, w in d.items():
            vg = groups.get(bn) or groups.setdefault(bn, o.vertex_groups.new(name=bn))
            vg.add([i], w / tot, "REPLACE")
    o.parent = arm
    m = o.modifiers.new("arm", "ARMATURE")
    m.object = arm


class Fit:
    """Measures the sculpted body (Blender space) so accessories sit on it."""
    def __init__(self, P, body_idx, vb=None):
        self.B = np.array([to_bl(p) for p in P[body_idx]])
        self.bones = np.array([vb[i] for i in body_idx]) if vb is not None else None

    def of(self, *bones):
        return lambda B: np.isin(self.bones, bones)

    def ring(self, z, dz=0.012, sel=None):
        B = self.B if sel is None else self.B[sel(self.B)]
        m = B[np.abs(B[:, 2] - z) < dz]
        if len(m) < 6:
            m = B[np.argsort(np.abs(B[:, 2] - z))[:40]]
        cx, cy = (m[:, 0].max() + m[:, 0].min()) / 2, (m[:, 1].max() + m[:, 1].min()) / 2
        return V((cx, cy, z)), (m[:, 0].max() - m[:, 0].min()) / 2, (m[:, 1].max() - m[:, 1].min()) / 2


def colored(o, rgb, k=0.18, f=160):
    vcolor(o, lambda p, rgb=rgb: tuple(lin(rgb * (1 - k / 2 + k * vnoise(np.array([[p.x, p.y, p.z]]), f)[0]))))
    return o


def extras(c, J, L, P):
    """Fitted, vertex-coloured pieces: coat tails / skirts, belt, scarf, buttons, bag, shoes, hats, bun, glasses, mask."""
    out = []
    H = L.H
    body_idx = mh.group_vertices("body")
    fit = Fit(P, body_idx, c["_vb"])
    colA, colB = hexc(c.get("colorA", "444a55")), hexc(c.get("colorB", "2a2a30"))
    acc = hexc(c.get("accentColor", "8a3a2a"))
    trim = hexc(c.get("trimColor", "2a1d16"))
    metal = hexc(c.get("metalColor", "b09060"))
    shoe = hexc(c.get("shoeColor", c.get("trimColor", "2a1d16")))
    top, bottom, accs = c.get("top"), c.get("bottom", "trousers"), c.get("acc", [])
    waist_z = J["spine"].z - 0.01
    hip_z = (J["hip_L"].z + J["pelvis"].z) / 2
    torso = fit.of("hips", "spine", "chest")
    # ---- skirt / coat tails / robe ----
    length = {"coat": 0.24, "robe": 0.45, "dress": 0.36}.get(top, 0.0)
    if bottom == "longskirt":
        length = max(length, 0.46)
    elif bottom == "skirt":
        length = max(length, 0.30)
    if length:
        top_z = waist_z if top in ("dress", "robe") or bottom != "trousers" else hip_z + 0.03
        bot_z = max(0.07, top_z - length * H)
        legs = fit.of("hips", "thigh_L", "thigh_R", "spine")
        _, hrx, hry = fit.ring(hip_z, 0.02, legs)
        pts, rad = [], []
        for i in range(7):
            t = i / 6
            z = top_z + (bot_z - top_z) * t
            cen, rx, ry = fit.ring(max(z, hip_z - 0.02) if z > hip_z - 0.02 else hip_z - 0.02, 0.02, legs)
            rx, ry = max(rx, hrx * (0.98 if z > hip_z else 1.0)), max(ry, hry)
            fl = 1 + t * (0.32 if top == "coat" else 0.45 if top == "robe" else 0.38)
            pts.append(V((0, cen.y + 0.004, z)))
            rad.append((rx * fl + 0.012, ry * fl * 1.08 + 0.014))
        o = BC.tube("skirt", pts, rad, seg=32)
        o.modifiers.new("solid", "SOLIDIFY").thickness = 0.005
        BC.apply_modifiers(o)
        BC.smooth(o)
        o["tag"] = "skirt"
        out.append(colored(o, colA if top in ("coat", "robe", "dress") and bottom != "longskirt" else colB, 0.2, 120))
        if "apron" in accs:
            o = BC.tube("apron", [V((p.x, p.y, p.z)) for p in pts[:5]], [(a * 1.03 + 0.003, b * 1.03 + 0.004) for a, b in rad[:5]], seg=36)
            bm = bmesh.new(); bm.from_mesh(o.data)
            bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.calc_center_median().y > pts[0].y - 0.04], context="FACES")
            bm.to_mesh(o.data); bm.free()
            o["tag"] = "skirt"
            out.append(colored(o, acc, 0.12, 200))
    # ---- belt ----
    if "belt" in accs or top in ("robe", "uniform"):
        cen, rx, ry = fit.ring(waist_z, 0.012, torso)
        th = 0.016 if top in ("robe", "coat") else 0.008
        o = BC.tube("belt", [cen + V((0, 0, -0.016)), cen + V((0, 0, 0.016))], [(rx + th, ry + th)] * 2, seg=28)
        o["rigid"] = "spine"
        out.append(colored(o, trim, 0.1))
    # ---- scarf ----
    if "scarf" in accs:
        nz = J["neck"].z + 0.012
        cen, rx, ry = fit.ring(nz, 0.01, fit.of("neck"))
        o = BC.tube("scarf", [cen + V((0, 0, -0.03)), cen + V((0, 0, 0.0)), cen + V((0, 0, 0.028))], [(rx + 0.02, ry + 0.022), (rx + 0.024, ry + 0.026), (rx + 0.016, ry + 0.017)], seg=24)
        o["rigid"] = "neck"
        out.append(colored(o, acc, 0.25, 300))
        ch = J["chest"]
        _, crx, cry = fit.ring(ch.z + 0.06, 0.015, torso)
        tail = BC.tube("scarf_tail", [cen + V((0.025, -ry - 0.012, -0.02)), V((0.03, ch.y - cry - 0.03, ch.z + 0.06)), V((0.035, ch.y - cry - 0.035, ch.z - 0.04)), V((0.036, ch.y - cry - 0.03, ch.z - 0.12))],
                       [(0.03, 0.005)] * 4, seg=8)
        tail["rigid"] = "chest"
        out.append(colored(tail, acc * 0.92, 0.25, 300))
    # ---- collar / neckline band (hides the garment edge) ----
    if top and top not in ("dress",) and c.get("hat") != "hood":
        if "_neckline" in c:
            cen, rx, ry, span = c["_neckline"]
        else:
            cen, rx, ry = fit.ring(J["neck"].z - 0.02, 0.012, fit.of("neck", "chest"))
            span = 0.02
        hi = 0.028 if top in ("coat", "uniform", "robe") else 0.01
        lo = -(span * 0.5 + 0.006)
        o = BC.tube("collar", [cen + V((0, 0.004, lo)), cen + V((0, 0.002, hi * 0.5)), cen + V((0, 0, hi))],
                    [(rx + 0.008, ry + 0.008), (rx + 0.004, ry + 0.005), (rx + 0.002 + (0.004 if hi > 0.02 else 0), ry + 0.003)], seg=28)
        o.modifiers.new("solid", "SOLIDIFY").thickness = 0.003
        BC.apply_modifiers(o)
        o["rigid"] = "neck"
        out.append(colored(o, colA * 0.92, 0.12, 200))
    # ---- buttons down the front ----
    if top in ("coat", "uniform") or "buttons" in accs:
        for i in range(5):
            z = J["neck"].z - 0.07 - i * 0.075
            if z < hip_z:
                break
            cen, rx, ry = fit.ring(z, 0.01, torso)
            bm = bmesh.new()
            bmesh.ops.create_uvsphere(bm, u_segments=8, v_segments=6, radius=0.0065)
            o = BC.mesh_obj("button", bm)
            o.location = V((0.0, cen.y - ry - (0.018 if top == "coat" else 0.012), z))
            o["rigid"] = "chest" if z > J["chest"].z else "spine"
            out.append(colored(o, metal, 0.05))
    # ---- satchel ----
    if "satchel" in accs:
        cen, rx, ry = fit.ring(hip_z, 0.02, torso)
        bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
        for v in bm.verts:
            v.co = V((v.co.x * 0.05, v.co.y * 0.22, v.co.z * 0.17))
        o = BC.mesh_obj("satchel", bm)
        o.location = V((-(rx + 0.04), cen.y, hip_z + 0.02))
        o["rigid"] = "hips"
        out.append(colored(o, trim, 0.2, 200))
        sh = J["shoulder_L"]
        strap = BC.tube("strap", [V((-(rx + 0.03), cen.y - 0.02, hip_z + 0.1)), V((-0.04, J["chest"].y - 0.13, J["chest"].z)), V((sh.x - 0.03, sh.y - 0.05, sh.z + 0.06)),
                                  V((sh.x - 0.02, sh.y + 0.06, sh.z + 0.04)), V((-0.05, J["chest"].y + 0.12, J["chest"].z - 0.02)), V((-(rx + 0.03), cen.y + 0.03, hip_z + 0.1))],
                        [(0.02, 0.003)] * 6, seg=6)
        strap["rigid"] = "chest"
        out.append(colored(strap, trim * 0.9, 0.1))
    # ---- shoes (lofted around the real foot) ----
    for s in "LR":
        an, ba, to = J["ankle_" + s], J["ball_" + s], J["toe_" + s]
        fw = 0.048 * (H / 1.75)
        x = an.x
        pts = [V((x, an.y + 0.055, 0.03)), V((x, an.y + 0.04, 0.06)), V((x, an.y, 0.075)), V((x, (an.y + ba.y) / 2, 0.055)),
               V((ba.x, ba.y, 0.04)), V((to.x, to.y + 0.012, 0.028)), V((to.x, to.y - 0.008, 0.022))]
        rad = [(0.75, 0.32), (1.0, 0.6), (0.98, 0.62), (1.0, 0.5), (1.06, 0.4), (0.86, 0.3), (0.35, 0.18)]
        o = BC.tube("shoe", pts, [(a * fw, b * fw * 1.2) for a, b in rad], seg=16)
        bm = bmesh.new(); bm.from_mesh(o.data)
        bmesh.ops.holes_fill(bm, edges=bm.edges, sides=0)
        for v in bm.verts:
            v.co.z = max(v.co.z, 0.003)
        bm.to_mesh(o.data); bm.free()
        o.modifiers.new("sub", "SUBSURF").levels = 1
        BC.apply_modifiers(o)
        BC.smooth(o)
        vcolor(o, lambda p, shoe=shoe: tuple(lin(shoe * (0.35 if p.z < 0.009 else 0.85 + 0.3 * vnoise(np.array([[p.x, p.y, p.z]]), 220)[0]))))
        o["rigid"] = "foot_" + s
        out.append(o)
    # ---- head pieces (hats, hood, bun, braids, long hair, glasses, mask) ----
    hcen, hrx, hry = fit.ring(to_bl(L.eye).z + 0.03, 0.02, fit.of("head"))
    hc = V((hcen.x, hcen.y, to_bl(L.eye).z + 0.02))
    hs = hrx / 0.86
    hat, hair = c.get("hat"), c.get("hair", "short")
    hair_c = hexc(c.get("hairColor", "2e2119"))
    fake = {"height": H, "hat": hat, "hatMat": c.get("hatMat", "trim"), "hair": hair, "acc": accs, "fem": c["macro"].get("gender", 0.5) < 0.5}

    class HeadProxy:
        location = hc
    for o in BC.hat_objects(fake, HeadProxy(), hs * 1.04, H):
        mt = o.get("mat", "trim")
        out.append(colored(o, {"clothA": colA, "clothB": colB, "accent": acc}.get(mt, trim), 0.15, 200))
        o["rigid"] = "head"
    if hat not in ("hood", "headscarf"):
        hp = []
        if hair == "bun":
            hp.append(BC.shell("bun", hc + V((0, hry * 0.82, hrx * 0.55)), 0.042, 0.04, 0.038, lambda p: True, seg=(16, 12)))
        if hair in ("braid", "pony"):
            for i in range(5 if hair == "braid" else 4):
                r = 0.019 - i * 0.0024
                hp.append(BC.shell("braid", hc + V((0, hry * 0.95 + i * 0.006, -0.03 - i * 0.034)), r, r, r * 1.4, lambda p: True, seg=(10, 8)))
        if hair == "long":
            bm = bmesh.new()
            bmesh.ops.create_cone(bm, cap_ends=False, segments=24, radius1=1.0, radius2=0.9, depth=1.0)
            bmesh.ops.delete(bm, geom=[f for f in bm.faces if f.calc_center_median().y < -0.2], context="FACES")
            for v in bm.verts:
                v.co = V((v.co.x * hrx * 1.12, v.co.y * hry * 0.85 + hry * 0.25, v.co.z * 0.17 - 0.1))
            o = BC.mesh_obj("hair_long", bm)
            o.modifiers.new("solid", "SOLIDIFY").thickness = 0.006
            BC.apply_modifiers(o); BC.smooth(o)
            o.location = hc
            hp.append(o)
        for o in hp:
            vcolor(o, lambda p: tuple(lin(hair_c * (0.8 + 0.25 * (0.5 + 0.5 * math.sin(p.x * 900 + p.z * 40))))))
            o["rigid"] = "head"
            out.append(o)
    if "glasses" in accs:
        for e in (L.eyeL, L.eyeR):
            bm = bmesh.new()
            bmesh.ops.create_circle(bm, cap_ends=False, segments=16, radius=0.017)
            bmesh.ops.rotate(bm, verts=bm.verts, cent=V((0, 0, 0)), matrix=Matrix.Rotation(math.pi / 2, 3, "X"))
            o = BC.mesh_obj("lens", bm)
            o.modifiers.new("skin", "SKIN")
            for dd in o.data.skin_vertices[0].data:
                dd.radius = (0.0012, 0.0012)
            BC.apply_modifiers(o)
            o.location = to_bl(e) + V((0, -0.018, 0))
            o["rigid"] = "head"
            out.append(colored(o, metal, 0.02))
    if "mask" in accs:
        e = to_bl(L.eye)
        o = BC.shell("mask", hc + V((0, -0.006, 0)), hrx * 1.1, hry * 1.12, hrx * 1.45, lambda p: p.y < -0.3 and -0.75 < p.z < 0.62, seg=(36, 24))
        o["rigid"] = "head"
        out.append(colored(o, metal, 0.12, 300))
        for sd, ex in ((1, L.eyeL), (-1, L.eyeR)):
            bm = bmesh.new(); bmesh.ops.create_cube(bm, size=1.0)
            for v in bm.verts:
                v.co = V((v.co.x * 0.026, v.co.y * 0.004, v.co.z * 0.0035))
            sl = BC.mesh_obj("slit", bm)
            sl.location = to_bl(ex) + V((0, -hry * 0.36, 0.002))
            sl.rotation_euler = (0, 0, sd * 0.2)
            sl["rigid"] = "head"
            out.append(colored(sl, hexc(c.get("glowColor", "ffb050")) * 1.0, 0.0))
    return out


def atlas_material(c, tex_path):
    m = bpy.data.materials.new(c["id"] + "_atlas")
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    im = nt.nodes.new("ShaderNodeTexImage")
    im.image = bpy.data.images.load(tex_path)
    nt.links.new(im.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.62
    m["slot"] = "atlas"
    return m


def detail_material(c):
    m = bpy.data.materials.new(c["id"] + "_detail")
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    ca = nt.nodes.new("ShaderNodeVertexColor")
    ca.layer_name = "Col"
    nt.links.new(ca.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.5
    m["slot"] = "detail"
    return m


# ----------------------------------------------------------------------------------------------------------------
def main():
    out = os.path.abspath(sys.argv[-1] if len(sys.argv) > 1 and not sys.argv[-1].endswith(".py") else "out")
    tex_dir = os.path.join(out, "tex")
    os.makedirs(tex_dir, exist_ok=True)
    only = os.environ.get("ONLY")
    BC.clear()
    objs = []
    for c in CAST:
        if only and c["id"] not in only.split(","):
            continue
        arm, mesh = build(c, tex_dir)
        objs += [arm, mesh]
        print("built", c["id"], sum(len(p.vertices) - 2 for p in mesh.data.polygons), "tris", flush=True)
    BC.export(os.path.join(out, "cast.glb"), set(objs), False)
    # reference rig + clips from an average body
    BC.clear()
    P = mh.sculpt(dict(gender=0.5))
    P = P - np.array([0, P[:, 1].min(), 0])
    ref = BC.build_armature("rig", rig_points(P))
    BC.bake_clips(ref, P[:, 1].max())
    BC.export(os.path.join(out, "anims.glb"), {ref}, True)
    print("done ->", out)


if __name__ == "__main__":
    main()
