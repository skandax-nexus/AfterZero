"""AFTERZERO motion: CMU motion capture retargeted onto our game rig -> anims.glb (Blender as a Python module).

    MH_DATA=/path/to/makehuman/data  venv/bin/python build_anims.py OUT_DIR       # ONLY=walk,run for a subset

Motion source: the CMU Graphics Lab Motion Capture Database (mocap.cs.cmu.edu; free for any use, including commercial
products), in the BVH conversion by B. Hahne, fetched from github.com/una-dinosauria/cmu-mocap and cached in
mocap/cache/ (or $MOCAP_CACHE). The raw files are never committed or shipped: only the clips baked here are.

Pipeline, per clip (see CLIPS):
 1. read the BVH, forward kinematics (bvh.py), convert to Blender space (Z up, actor front -Y like our rig).
 2. retarget onto the reference rig (the average MakeHuman body, rigged exactly like build_humans.py does):
    - limbs (clavicle, arm, hand, leg, foot, toe) are DIRECTION-MATCHED: a constant rest alignment C rotates our A-pose
      bone onto the CMU T-pose segment (twist fixed by an anatomical axis: forward for arms/legs, up for feet), then the
      source joint's world rotation is applied: R(t) = Rsrc(t) . C . Rrest. Bone directions match the actor exactly.
    - torso and head (hips, spine, chest, neck, head) take the source's world rotation delta (R(t) = Rsrc(t) . Rrest),
      keeping our own spine curvature.
    - the pelvis follows the mid-point of the hip joints, scaled by leg length (so stride matches our proportions).
 3. heading normalised (actor faces -Y = game +Z); locomotion is made IN-PLACE (the linear travel of one gait cycle is
    removed, vertical bob, sway and pelvis rotation are kept) and its speed is measured (m/s at timeScale 1).
 4. loops: the cut is searched for the best matching frame pair (pose + velocity), the seam is cross-faded.
 5. feet: the floor is found from the soles (heel, ball and toe tip), and a two-bone leg IK keeps every planted foot ON
    the floor (no sinking or floating); seated clips put the pelvis on a 0.466 m seat and the feet on the floor.
 6. resampled to 30 fps, keyed on the rig, exported with the same clip names the game uses (+ extras).
The rig node carries extras.az_clips (JSON: per-clip speed / cycle / flags) for assets/js/humans.js.
"""
import json, math, os, sys, urllib.request
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
sys.path.insert(0, os.path.dirname(HERE))
import bvh  # noqa: E402

CACHE = os.environ.get("MOCAP_CACHE") or os.path.join(HERE, "cache")
URL = "https://raw.githubusercontent.com/una-dinosauria/cmu-mocap/master/data/{s:03d}/{t}.bvh"
SRC_FPS, FPS = 120, 30
SEAT = 0.466          # prologue bench cushion height (m); humans.js H.SEAT
SEAT_DEPTH = 0.085    # hip-joint centre above the seat contact (ischial tuberosities) for the reference body

# ----------------------------------------------------------------------------------------------------------------
# the clips. trial = CMU subject_trial; range = source frames (120 fps) to search in.
# mode: cycle (locomotion, one gait cycle, in place) | loop (stationary, cross-faded) | pingpong
# ----------------------------------------------------------------------------------------------------------------
CLIPS = [
    dict(name="idle", trial="137_28", range=(3300, 3722), mode="loop", length=(2.3, 2.8), fade=0.5),
    dict(name="walk", trial="35_01", range=(40, 358), mode="cycle"),
    dict(name="walk_slow", trial="07_04", range=(40, 449), mode="cycle"),
    dict(name="jog", trial="35_17", range=(10, 167), mode="cycle"),
    dict(name="run", trial="127_07", range=(5, 145), mode="cycle"),
    dict(name="sprint", trial="143_01", range=(0, 100), mode="cycle"),
    dict(name="crouch", trial="136_09", range=(300, 1100), mode="cycle"),
    dict(name="talk", trial="18_08", range=(1330, 1740), mode="loop", length=(2.6, 3.3), fade=0.6),
    dict(name="nod", trial="18_08", range=(300, 1200), mode="loop", length=(4.0, 6.0), fade=0.8, nods=True),
    dict(name="sad", trial="137_28", range=(3300, 3722), mode="loop", length=(2.3, 2.8), fade=0.5,
         slump={"spine": 7, "chest": 10, "neck": 14, "head": 24, "clavicle_L": 6, "clavicle_R": 6}, slow=1.35),
    dict(name="look", trial="137_28", range=(2200, 2720), mode="loop", length=(3.5, 4.3), fade=0.7),
    dict(name="work", trial="62_07", range=(400, 950), mode="loop", length=(3.0, 4.2), fade=0.5),
    dict(name="fall", trial="143_05", range=(172, 186), mode="pingpong", slow=2.5),
    dict(name="sit", trial="86_15", range=(2060, 2640), mode="loop", length=(3.5, 4.5), fade=0.8, seated=True,
         slump={"spine": -9, "chest": -5, "neck": 4, "head": 4}, hands_on_thighs=True),
]

# our bone -> (source joint whose world rotation drives it, source child joint giving the segment direction)
SRC = {"hips": ("Hips", None), "spine": ("Spine", None), "chest": ("Spine1", None), "neck": ("Neck1", None), "head": ("Head", None)}
for _k, _s in (("L", "Left"), ("R", "Right")):
    SRC.update({
        "clavicle_" + _k: (_s + "Shoulder", _s + "Arm"), "upperarm_" + _k: (_s + "Arm", _s + "ForeArm"),
        "forearm_" + _k: (_s + "ForeArm", _s + "Hand"), "hand_" + _k: (_s + "FingerBase", _s + "HandIndex1"),
        "thigh_" + _k: (_s + "UpLeg", _s + "Leg"), "shin_" + _k: (_s + "Leg", _s + "Foot"),
        "foot_" + _k: (_s + "Foot", _s + "ToeBase"), "toe_" + _k: (_s + "ToeBase", _s + "ToeBase_end"),
    })
FWD, UP = np.array([0.0, -1.0, 0.0]), np.array([0.0, 0.0, 1.0])
TWIST = {b: (UP if b.startswith(("foot", "toe")) else FWD) for b in SRC}
M_BL = np.array([[1.0, 0, 0], [0, 0, -1], [0, 1, 0]])     # BVH (Y up, front +Z) -> Blender (Z up, front -Y)


# ----------------------------------------------------------------------------------------------------------------
# small math
# ----------------------------------------------------------------------------------------------------------------
def nrm(v):
    v = np.asarray(v, float)
    return v / np.maximum(np.linalg.norm(v, axis=-1, keepdims=True), 1e-12)


def frame(d, s):
    """Rotation whose Y column is d and whose X column is s made orthogonal to d."""
    y = nrm(d)
    x = nrm(s - np.sum(s * y, -1, keepdims=True) * y)
    z = np.cross(x, y)
    return np.stack([x, y, z], -1)


def rot_z(a):
    a = np.asarray(a, float)
    c, s = np.cos(a), np.sin(a)
    R = np.zeros(a.shape + (3, 3))
    R[..., 0, 0], R[..., 0, 1], R[..., 1, 0], R[..., 1, 1], R[..., 2, 2] = c, -s, s, c, 1
    return R


def axis_angle(axis, ang):
    axis = nrm(axis)
    x, y, z = axis[..., 0], axis[..., 1], axis[..., 2]
    c, s = np.cos(ang), np.sin(ang)
    C = 1 - c
    return np.stack([np.stack([c + x * x * C, x * y * C - z * s, x * z * C + y * s], -1),
                     np.stack([y * x * C + z * s, c + y * y * C, y * z * C - x * s], -1),
                     np.stack([z * x * C - y * s, z * y * C + x * s, c + z * z * C], -1)], -2)


def rot_between(a, b):
    """Minimal rotation taking direction a onto b (batched)."""
    a, b = nrm(a), nrm(b)
    ax = np.cross(a, b)
    s = np.linalg.norm(ax, axis=-1)
    c = np.sum(a * b, -1)
    ang = np.arctan2(s, c)
    fallback = np.cross(a, np.array([1.0, 0, 0]))
    ax = np.where((s < 1e-9)[..., None], fallback, ax)
    return axis_angle(ax, ang)


def m2q(R):
    """3x3 -> quaternion (w, x, y, z), batched."""
    R = np.asarray(R, float)
    sh = R.shape[:-2]
    R = R.reshape(-1, 3, 3)
    q = np.zeros((len(R), 4))
    for i, m in enumerate(R):
        t = m[0, 0] + m[1, 1] + m[2, 2]
        if t > 0:
            s = math.sqrt(t + 1.0) * 2
            q[i] = [0.25 * s, (m[2, 1] - m[1, 2]) / s, (m[0, 2] - m[2, 0]) / s, (m[1, 0] - m[0, 1]) / s]
        elif m[0, 0] > m[1, 1] and m[0, 0] > m[2, 2]:
            s = math.sqrt(1.0 + m[0, 0] - m[1, 1] - m[2, 2]) * 2
            q[i] = [(m[2, 1] - m[1, 2]) / s, 0.25 * s, (m[0, 1] + m[1, 0]) / s, (m[0, 2] + m[2, 0]) / s]
        elif m[1, 1] > m[2, 2]:
            s = math.sqrt(1.0 + m[1, 1] - m[0, 0] - m[2, 2]) * 2
            q[i] = [(m[0, 2] - m[2, 0]) / s, (m[0, 1] + m[1, 0]) / s, 0.25 * s, (m[1, 2] + m[2, 1]) / s]
        else:
            s = math.sqrt(1.0 + m[2, 2] - m[0, 0] - m[1, 1]) * 2
            q[i] = [(m[1, 0] - m[0, 1]) / s, (m[0, 2] + m[2, 0]) / s, (m[1, 2] + m[2, 1]) / s, 0.25 * s]
    return q.reshape(sh + (4,))


def q2m(q):
    q = nrm(q)
    w, x, y, z = q[..., 0], q[..., 1], q[..., 2], q[..., 3]
    return np.stack([np.stack([1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)], -1),
                     np.stack([2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)], -1),
                     np.stack([2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)], -1)], -2)


def q_continuous(q):
    """Flip signs along axis 0 so consecutive quaternions stay in one hemisphere."""
    q = q.copy()
    for i in range(1, len(q)):
        d = np.sum(q[i] * q[i - 1], -1)
        q[i][d < 0] *= -1
    return q


def slerp(a, b, t):
    t = np.asarray(t, float)[..., None]
    d = np.sum(a * b, -1, keepdims=True)
    b = np.where(d < 0, -b, b)
    d = np.abs(d)
    om = np.arccos(np.clip(d, -1, 1))
    so = np.sin(om)
    lin = so < 1e-6
    wa = np.where(lin, 1 - t, np.sin((1 - t) * om) / np.where(lin, 1, so))
    wb = np.where(lin, t, np.sin(t * om) / np.where(lin, 1, so))
    return nrm(wa * a + wb * b)


def smoothstep(x):
    x = np.clip(x, 0, 1)
    return x * x * (3 - 2 * x)


def gauss_smooth(x, sigma):
    """Gaussian smoothing along axis 0 (edges clamped)."""
    if sigma <= 0:
        return x
    r = int(math.ceil(sigma * 3))
    k = np.exp(-0.5 * (np.arange(-r, r + 1) / sigma) ** 2)
    k /= k.sum()
    pad = np.concatenate([np.repeat(x[:1], r, 0), x, np.repeat(x[-1:], r, 0)], 0)
    out = np.zeros_like(x)
    for i, w in enumerate(k):
        out += w * pad[i:i + len(x)]
    return out


# ----------------------------------------------------------------------------------------------------------------
# source
# ----------------------------------------------------------------------------------------------------------------
def fetch(trial):
    os.makedirs(CACHE, exist_ok=True)
    p = os.path.join(CACHE, trial + ".bvh")
    if not os.path.exists(p):
        url = URL.format(s=int(trial.split("_")[0]), t=trial)
        print("download", url, flush=True)
        tmp = p + ".part"
        with urllib.request.urlopen(url, timeout=120) as r, open(tmp, "wb") as f:
            f.write(r.read())
        os.replace(tmp, p)
    return p


class Source:
    def __init__(self, trial):
        m = bvh.load(fetch(trial))
        W, P = bvh.fk(m)
        self.trial, self.m, self.ix = trial, m, m.index
        self.W = M_BL @ W @ M_BL.T                      # world rotations relative to the T-pose rest (identity)
        self.P = P @ M_BL.T
        self.off = m.offset @ M_BL.T
        self.nf = m.nf
        assert abs(m.frame_time * SRC_FPS - 1) < 0.01, m.frame_time
        L = lambda j: np.linalg.norm(self.off[self.ix[j]])
        self.leg = 0.5 * (L("LeftLeg") + L("LeftFoot") + L("RightLeg") + L("RightFoot"))

    def seg_dir(self, child):
        return nrm(self.off[self.ix[child]])

    def hipmid(self):
        return 0.5 * (self.P[:, self.ix["LeftUpLeg"]] + self.P[:, self.ix["RightUpLeg"]])

    def facing(self):
        """Horizontal front direction per frame (from the hip joints' left-right axis)."""
        lr = self.P[:, self.ix["LeftUpLeg"]] - self.P[:, self.ix["RightUpLeg"]]   # +X is the actor's left
        f = np.cross(UP, lr)                         # up x left = back? (left=+X, up=+Z -> up x left = +Y = back)
        f = -f
        f[:, 2] = 0
        return nrm(f)


# ----------------------------------------------------------------------------------------------------------------
# target (our reference rig, built like build_humans.main)
# ----------------------------------------------------------------------------------------------------------------
class Target:
    def __init__(self, arm):
        bones = arm.data.bones
        self.names = [b.name for b in bones]
        self.bi = {n: i for i, n in enumerate(self.names)}
        self.parent = [self.bi[b.parent.name] if b.parent else -1 for b in bones]
        self.head = np.array([tuple(b.head_local) for b in bones])
        self.tail = np.array([tuple(b.tail_local) for b in bones])
        self.R = np.array([[list(r) for r in b.matrix_local.to_3x3()] for b in bones])
        self.dir = nrm(self.tail - self.head)
        h = lambda n: self.head[self.bi[n]]
        self.hipmid = 0.5 * (h("thigh_L") + h("thigh_R"))
        self.leg = 0.5 * sum(np.linalg.norm(self.tail[self.bi[b]] - self.head[self.bi[b]]) for b in ("thigh_L", "shin_L", "thigh_R", "shin_R"))
        self.height = self.tail[self.bi["head"]][2]
        # sole points in rest (on the floor z=0): heel (behind the ankle), ball, toe tip; carried by foot / foot / toe
        self.sole = {}
        for k in "LR":
            a, b, t = h("foot_" + k), h("toe_" + k), self.tail[self.bi["toe_" + k]]
            heel = a + nrm(np.array([a[0] - b[0], a[1] - b[1], 0.0])) * 0.045 * self.height / 1.75
            heel[2] = 0.0
            ball, tip = b.copy(), t.copy()
            ball[2] = tip[2] = 0.0
            self.sole[k] = [("foot_" + k, heel), ("foot_" + k, ball), ("toe_" + k, tip)]

    def fk(self, Rw, hips_head):
        """World rotations (n,B,3,3) + hips head (n,3) -> heads, tails (n,B,3)."""
        n, B = Rw.shape[:2]
        H = np.zeros((n, B, 3))
        T = np.zeros((n, B, 3))
        D = Rw @ np.transpose(self.R, (0, 2, 1))[None]          # world delta per bone
        for b in range(B):
            p = self.parent[b]
            if p < 0:
                H[:, b] = hips_head
            else:
                H[:, b] = H[:, p] + np.einsum("nij,j->ni", D[:, p], self.head[b] - self.head[p])
            T[:, b] = H[:, b] + np.einsum("nij,j->ni", D[:, b], self.tail[b] - self.head[b])
        return H, T, D

    def soles(self, H, D, k):
        """(n,3) per sole point of foot k."""
        out = []
        for bn, p in self.sole[k]:
            b = self.bi[bn]
            out.append(H[:, b] + np.einsum("nij,j->ni", D[:, b], p - self.head[b]))
        return np.stack(out, 1)

    def local_quats(self, Rw):
        """World rotations -> Blender pose-bone rotation_quaternion (w,x,y,z) per bone."""
        n, B = Rw.shape[:2]
        Q = np.zeros((n, B, 4))
        for b in range(B):
            p = self.parent[b]
            Rt = self.R[b].T
            if p < 0:
                L = Rt @ Rw[:, b]
            else:
                L = Rt @ self.R[p] @ np.transpose(Rw[:, p], (0, 2, 1)) @ Rw[:, b]
            Q[:, b] = m2q(L)
        return q_continuous(Q)

    def world_from_local(self, Q):
        n, B = Q.shape[:2]
        Rw = np.zeros((n, B, 3, 3))
        for b in range(B):
            p = self.parent[b]
            L = q2m(Q[:, b])
            if p < 0:
                Rw[:, b] = self.R[b] @ L
            else:
                Rw[:, b] = Rw[:, p] @ self.R[p].T @ self.R[b] @ L
        return Rw


# ----------------------------------------------------------------------------------------------------------------
# retarget
# ----------------------------------------------------------------------------------------------------------------
def retarget(src, tgt, frames, yaw, pivot):
    """Source frames (array of ints) -> target world rotations (n,B,3,3) and hip-midpoint (n,3) (scaled, yawed about
    the source point pivot)."""
    Y = rot_z(yaw)                                             # (n,3,3) heading correction per frame
    n, B = len(frames), len(tgt.names)
    Rw = np.zeros((n, B, 3, 3))
    for b, name in enumerate(tgt.names):
        j, child = SRC[name]
        Rs = Y @ src.W[frames, src.ix[j]]
        if child is None:
            C = np.eye(3)
        else:
            C = frame(src.seg_dir(child), TWIST[name]) @ frame(tgt.dir[b], TWIST[name]).T
        Rw[:, b] = Rs @ C @ tgt.R[b]
    s = tgt.leg / src.leg
    hm = np.einsum("nij,nj->ni", Y, src.hipmid()[frames] - pivot * np.array([1, 1, 0])) * s
    return Rw, hm, s


def hips_head(tgt, Rw, hm):
    b = tgt.bi["hips"]
    D = Rw[:, b] @ tgt.R[b].T
    return hm + np.einsum("nij,j->ni", D, tgt.head[b] - tgt.hipmid)


def leg_ik(tgt, Rw, hh, k, goal, weight):
    """Two-bone IK: move ankle k toward goal (n,3) by weight (n,), keeping the foot's world rotation."""
    return chain_ik(tgt, Rw, hh, "thigh_" + k, "shin_" + k, goal, weight)


def chain_ik(tgt, Rw, hh, upper, lower, goal, weight, pole=None):
    """Two-bone IK on upper -> lower (end = lower's tail) toward goal; the end bone keeps its world rotation. The bend
    plane follows the current middle joint, or pole (n,3: a direction the middle joint should point to)."""
    H, T, D = tgt.fk(Rw, hh)
    th, sh = tgt.bi[upper], tgt.bi[lower]
    hip, knee, ank = H[:, th], H[:, sh], T[:, sh]
    goal = ank + (goal - ank) * weight[:, None]
    l1 = np.linalg.norm(knee - hip, axis=1)
    l2 = np.linalg.norm(ank - knee, axis=1)
    d = np.clip(np.linalg.norm(goal - hip, axis=1), np.abs(l1 - l2) + 1e-4, (l1 + l2) * 0.9995)
    u = nrm(goal - hip)
    goal = hip + u * d[:, None]
    a = (l1 ** 2 - l2 ** 2 + d ** 2) / (2 * d)
    hgt = np.sqrt(np.maximum(l1 ** 2 - a ** 2, 0))
    pole = knee - hip if pole is None else pole
    v = nrm(pole - np.sum(pole * u, 1, keepdims=True) * u)
    knee2 = hip + u * a[:, None] + v * hgt[:, None]
    R1 = rot_between(knee - hip, knee2 - hip)
    R2 = rot_between(np.einsum("nij,nj->ni", R1, ank - knee), goal - knee2)
    Rw = Rw.copy()
    Rw[:, th] = R1 @ Rw[:, th]
    Rw[:, sh] = R2 @ R1 @ Rw[:, sh]
    return Rw


def hands_on_thighs(tgt, Rw, hh, life=0.35):
    """Seated: rest each palm on the top of its thigh (60% towards the knee), fingers forward and palm down; the
    source's own small hand movements are kept at `life` strength. Elbows point back and out."""
    H, T, D = tgt.fk(Rw, hh)
    n = len(Rw)
    for k, sd in (("L", 1.0), ("R", -1.0)):
        hip, knee = H[:, tgt.bi["thigh_" + k]], H[:, tgt.bi["shin_" + k]]
        hb = tgt.bi["hand_" + k]
        hand_len = np.linalg.norm(tgt.tail[hb] - tgt.head[hb])
        ax = nrm(knee - hip)
        side = np.tile(np.array([sd, 0.0, 0.0]), (n, 1))
        up = nrm(np.cross(ax, np.array([1.0, 0, 0])))       # thigh's upward normal (ax ~ forward: (-Y) x X = +Z)
        palm = hip + 0.58 * (knee - hip) + up * 0.075 * tgt.height / 1.75 + side * 0.012
        fdir = nrm(ax * 0.9 - up * 0.25 + side * -0.12)     # fingers forward, curling over the thigh, a little inward
        wrist = palm - fdir * hand_len * 0.45
        cur = T[:, tgt.bi["forearm_" + k]]
        wrist = wrist + (cur - cur.mean(0)) * life
        pole = np.tile(np.array([sd * 0.6, 0.8, -0.2]), (n, 1))     # elbow back and out
        Rw = chain_ik(tgt, Rw, hh, "upperarm_" + k, "forearm_" + k, wrist, np.ones(n), pole=pole)
        # hand: fingers along fdir, thumb towards the body midline (palm down)
        thumb = np.tile(np.array([-sd, 0.0, 0.0]), (n, 1))
        F = frame(fdir, thumb) @ frame(tgt.dir[hb], FWD).T
        Rw[:, hb] = F @ tgt.R[hb]
    return Rw


def sole_height(tgt, Rw, hh):
    H, T, D = tgt.fk(Rw, hh)
    return {k: tgt.soles(H, D, k)[:, :, 2].min(1) for k in "LR"}, H, T, D


def plant_feet(tgt, Rw, hh, grounded, lock_xy=False, speed=0.0, fps=SRC_FPS):
    """Keep feet on the floor: each planted foot (near the floor and still on the ground, the body travelling at speed)
    is moved (IK) so its lowest sole point is exactly at z=0; a foot below the floor is always lifted out.
    lock_xy: foot lock - while the same sole point (heel, ball or toe) stays planted it is pinned to one spot on the
    ground (with the body's travel), so the foot cannot skate; the correction is smoothed in time."""
    for k in "LR":
        h, H, T, D = sole_height(tgt, Rw, hh)
        hk = h[k]
        ank = T[:, tgt.bi["shin_" + k]]
        P, low, wc = contact(tgt, H, D, k, speed, fps)
        w = np.maximum(gauss_smooth(wc, 3.0 * fps / SRC_FPS), (hk < 0).astype(float)) * grounded
        goal = ank.copy()
        goal[:, 2] -= hk
        if lock_xy:
            n = len(P)
            on = wc > 0.5
            corr = np.zeros((n, 2))
            i = 0
            while i < n:
                if not on[i]:
                    i += 1
                    continue
                j = i
                while j + 1 < n and on[j + 1] and low[j + 1] == low[i]:
                    j += 1
                anchor = P[i:j + 1, :2].mean(0)
                corr[i:j + 1] = anchor - P[i:j + 1, :2]
                i = j + 1
            corr = gauss_smooth(corr, 1.5 * fps / 30.0)
            goal[:, :2] += corr * np.clip(w, 0, 1)[:, None]
        Rw = leg_ik(tgt, Rw, hh, k, goal, np.clip(w, 0, 1))
    return Rw


# ----------------------------------------------------------------------------------------------------------------
# clip processing
# ----------------------------------------------------------------------------------------------------------------
def pose_features(tgt, Rw, hh):
    H, T, D = tgt.fk(Rw, hh)
    rel = H - hh[:, None, :]
    rel = np.concatenate([rel, (T - hh[:, None, :])[:, [tgt.bi[b] for b in ("hand_L", "hand_R", "toe_L", "toe_R", "head")]]], 1)
    vel = np.gradient(rel, axis=0) * SRC_FPS
    return rel.reshape(len(rel), -1), vel.reshape(len(vel), -1)


def best_pair(feat, vel, lo, hi, a_range=None):
    """Frames (a, b) with lo <= b-a <= hi minimising pose + velocity difference."""
    n = len(feat)
    best = (1e18, 0, 0)
    a_list = range(0, n - lo) if a_range is None else a_range
    for a in a_list:
        bs = np.arange(a + lo, min(n, a + hi + 1))
        if not len(bs):
            continue
        e = np.sum((feat[bs] - feat[a]) ** 2, 1) + 0.01 * np.sum((vel[bs] - vel[a]) ** 2, 1)
        i = int(np.argmin(e))
        if e[i] < best[0]:
            best = (float(e[i]), a, int(bs[i]))
    if best[2] <= best[1]:
        raise ValueError("no loop of %d..%d frames fits in %d frames" % (lo, hi, n))
    return best


def gait_period(src, f0, f1):
    """Gait cycle length (source frames) from the autocorrelation of the left-right ankle height difference."""
    z = src.P[f0:f1, src.ix["LeftFoot"], 2] - src.P[f0:f1, src.ix["RightFoot"], 2]
    z = z - z.mean()
    ac = np.correlate(z, z, "full")[len(z) - 1:]
    ac /= ac[0]
    lo = int(0.45 * SRC_FPS)
    return lo + int(np.argmax(ac[lo:int(1.6 * SRC_FPS)]))


def resample(Q, hh, a, b, n_out, closed=True):
    """Sample local quats Q / hips hh (source rate, index 0 == frame a of this array) at n_out+1 evenly spaced times
    from a to b (inclusive)."""
    t = np.linspace(a, b, n_out + 1)
    i0 = np.clip(np.floor(t).astype(int), 0, len(Q) - 1)
    i1 = np.clip(i0 + 1, 0, len(Q) - 1)
    f = t - i0
    Qo = slerp(Q[i0], Q[i1], np.repeat(f[:, None], Q.shape[1], 1))
    Ho = hh[i0] + (hh[i1] - hh[i0]) * f[:, None]
    return Qo, Ho


def crossfade_loop(Q, hh, a, b, K):
    """Make frames [a, b] loop: the last K frames before b blend into the frames before a. Returns arrays a..b."""
    Q2, H2 = Q[a:b + 1].copy(), hh[a:b + 1].copy()
    N = b - a
    K = min(K, a, N - 1)
    for i in range(N - K, N + 1):
        w = smoothstep((i - (N - K)) / K)
        Q2[i] = slerp(Q[a + i], Q[a + i - N], np.full(Q.shape[1], w))
        H2[i] = hh[a + i] + (hh[a + i - N] - hh[a + i]) * w
    H2[N] = H2[0]
    Q2[N] = Q2[0]
    return Q2, H2


def add_nods(tgt, Q, fps):
    """Listening nods: two soft nods every ~2.6 s on neck + head (local X = pitch for these bones)."""
    n = len(Q)
    t = np.arange(n) / fps
    dur = (n - 1) / fps
    per = dur / max(1, round(dur / 2.6))
    ph = (t % per) / per
    env = np.where(ph < 0.35, np.sin(np.pi * ph / 0.35) ** 2, 0) * (0.6 + 0.4 * np.sin(2 * np.pi * ph / 0.175) ** 2)
    for bn, amp in (("neck", 0.10), ("head", 0.16)):
        b = tgt.bi[bn]
        R = q2m(Q[:, b]) @ axis_angle(np.array([1.0, 0, 0]), env * amp)
        Q[:, b] = m2q(R)
    return q_continuous(Q)


def build_clip(spec, tgt):
    src = Source(spec["trial"])
    f0, f1 = spec["range"]
    f1 = min(f1, src.nf - 1)
    frames = np.arange(f0, f1 + 1)
    mode = spec["mode"]
    info = dict(trial=spec["trial"])
    hm_src = src.hipmid()
    if mode == "cycle":
        per = gait_period(src, f0, f1)
        # heading: travel direction over the range
        dvec = hm_src[f1] - hm_src[f0]
        yaw0 = math.atan2(dvec[0], -dvec[1])                    # rotate travel onto -Y
        yaw = np.full(len(frames), -yaw0)
    else:
        fw = src.facing()[f0:f1 + 1].mean(0)
        yaw0 = math.atan2(fw[0], -fw[1])
        yaw = np.full(len(frames), -yaw0)
    pivot = hm_src[frames].mean(0)
    Rw, hm, s = retarget(src, tgt, frames, yaw, pivot)
    hh = hips_head(tgt, Rw, hm)
    feat, vel = pose_features(tgt, Rw, hh)

    if mode == "cycle":
        # one gait cycle with the best matching ends; travel removed linearly
        margin = max(4, min(int(0.25 * SRC_FPS), (len(frames) - int(per * 1.1)) // 2))
        best = best_pair(feat, vel, int(per * 0.9), int(per * 1.1), range(margin, len(frames) - int(per * 1.1)))
        _, a, b = best
        # yaw drift over the cycle (straight line between the ends)
        dv = hm[b] - hm[a]
        drift = math.atan2(dv[0], -dv[1])
        fa, fb = src.facing()[f0 + a], src.facing()[f0 + b]
        turn = math.atan2(fa[0] * fb[1] - fa[1] * fb[0], fa[0] * fb[0] + fa[1] * fb[1])
        yaw = -yaw0 - drift - turn * np.clip((np.arange(len(frames)) - a) / (b - a), 0, 1) + turn * 0.0
        Rw, hm, s = retarget(src, tgt, frames, yaw, hm_src[f0 + a])
        hh = hips_head(tgt, Rw, hm)
        dv = hm[b] - hm[a]
        n_cyc = max(8, int(round((b - a) / SRC_FPS * FPS)))
        speed = float(np.linalg.norm(dv[:2]) / (n_cyc / FPS))         # stride over the baked (30 fps) cycle length
        tt = (np.arange(len(frames)) - a) / (b - a)
        hm[:, :2] -= hm[a, :2] + tt[:, None] * dv[:2]
        hm[:, :2] -= hm[a:b, :2].mean(0)
        info.update(speed=round(speed, 4), stride=round(float(np.linalg.norm(dv[:2])), 4))
    else:
        a, b = 0, len(frames) - 1
        if mode == "loop":
            lo, hi = (int(x * SRC_FPS) for x in spec["length"])
            K = int(spec.get("fade", 0.6) * SRC_FPS)
            _, a, b = best_pair(feat, vel, lo, hi, range(K, max(K + 1, len(frames) - lo)))
        if spec.get("seated"):
            hm[:, :2] -= hm[a:b, :2].mean(0)
        elif mode == "pingpong":
            hm[:, :2] = 0.0                                        # airborne pose in place (no arc either)
            hm[:, 2] = hm[:, 2].mean()
        else:
            H, T, D = tgt.fk(Rw, hips_head(tgt, Rw, hm))
            ank = 0.5 * (T[:, tgt.bi["shin_L"]] + T[:, tgt.bi["shin_R"]])
            hm[:, :2] -= ank[a:b, :2].mean(0)
    if spec.get("slump"):      # posture offsets (local, degrees: + = bend forward / shoulders forward)
        Ql = tgt.local_quats(Rw)
        for bn, deg in spec["slump"].items():
            bi = tgt.bi[bn]
            ax = np.array([0.0, 0, 1.0]) if bn.startswith("clavicle") else np.array([1.0, 0, 0])
            sg = -1 if bn == "clavicle_L" else 1
            Ql[:, bi] = m2q(q2m(Ql[:, bi]) @ axis_angle(ax, np.full(len(Ql), sg * math.radians(deg))))
        Rw = tgt.world_from_local(Ql)
    hh = hips_head(tgt, Rw, hm)

    # ---- floor / seat
    if spec.get("seated"):
        dz = (SEAT + SEAT_DEPTH) - hm[a:b, 2].mean()
        hm[:, 2] += dz
        hh = hips_head(tgt, Rw, hm)
        Rw = plant_feet(tgt, Rw, hh, np.ones(len(hm)), lock_xy=False)
        if spec.get("hands_on_thighs"):
            Rw = hands_on_thighs(tgt, Rw, hh)
        info.update(seat=SEAT)
    elif mode != "pingpong":
        h, *_ = sole_height(tgt, Rw, hh)
        low = np.minimum(h["L"], h["R"])[a:b]
        floor = float(np.percentile(low, 15 if mode == "cycle" else 50))
        hm[:, 2] -= floor
        hh = hips_head(tgt, Rw, hm)
        Rw = plant_feet(tgt, Rw, hh, np.ones(len(hm)), lock_xy=(mode == "loop"), speed=info.get("speed", 0.0))
    else:
        # airborne: keep the source height above the take-off floor (feet stay above the floor)
        h, *_ = sole_height(tgt, Rw, hh)
        hm[:, 2] -= min(h["L"].min(), h["R"].min()) - 0.12
        hh = hips_head(tgt, Rw, hm)

    Q = tgt.local_quats(Rw)
    Q = q_continuous(gauss_smooth(Q, 1.5))
    Q = Q / np.linalg.norm(Q, axis=-1, keepdims=True)
    hh = gauss_smooth(hh, 1.5)

    if mode == "cycle":
        K = min(int(0.12 * SRC_FPS), a)
        Qc, Hc = crossfade_loop(Q, hh, a, b, K)
        n_out = n_cyc
        Qo, Ho = resample(Qc, Hc, 0, b - a, n_out)
        # every gait starts on the same footfall (left foot furthest forward) so the game can swap gaits in phase
        H_, T_, _ = tgt.fk(tgt.world_from_local(Qo), Ho)
        lead = T_[:-1, tgt.bi["shin_L"], 1] - T_[:-1, tgt.bi["shin_R"], 1]       # -Y is forward
        i0 = int(np.argmin(lead))
        Qo = np.concatenate([Qo[i0:-1], Qo[:i0], Qo[i0:i0 + 1]], 0)
        Ho = np.concatenate([Ho[i0:-1], Ho[:i0], Ho[i0:i0 + 1]], 0)
        Qo = q_continuous(Qo)
        info.update(cycle=round((b - a) / SRC_FPS, 4))
    elif mode == "loop":
        K = min(int(spec.get("fade", 0.6) * SRC_FPS), a)
        Qc, Hc = crossfade_loop(Q, hh, a, b, K)
        n_out = int(round((b - a) / SRC_FPS * FPS * spec.get("slow", 1.0)))
        Qo, Ho = resample(Qc, Hc, 0, b - a, n_out)
    else:   # pingpong
        Qf, Hf = Q, hh
        Qc = np.concatenate([Qf, Qf[-2::-1]], 0)
        Hc = np.concatenate([Hf, Hf[-2::-1]], 0)
        n_out = int(round((len(Qc) - 1) / SRC_FPS * FPS * spec.get("slow", 1.0)))
        Qo, Ho = resample(Qc, Hc, 0, len(Qc) - 1, n_out)
    if spec.get("nods"):
        Qo = add_nods(tgt, Qo, FPS)
    if mode in ("cycle", "loop") and not spec.get("seated"):
        # final contact pass on the baked frames (the loop seam blend and smoothing must not lift / sink / slide feet)
        Rw2 = plant_feet(tgt, tgt.world_from_local(Qo), Ho, np.ones(len(Qo)), lock_xy=True,
                         speed=info.get("speed", 0.0), fps=n_out / (n_out / FPS))
        Qo = tgt.local_quats(Rw2)
        Qo[-1] = Qo[0]

    info.update(check(tgt, Qo, Ho, info.get("speed", 0.0), n_out / FPS))
    info.update(frames=[int(f0 + a), int(f0 + b)], duration=round(n_out / FPS, 4))
    return Qo, Ho, info


def contact(tgt, H, D, k, speed, fps):
    """Lowest sole point of foot k per frame (world, the body travelling at `speed` along -Y), and a 0..1 contact weight:
    near the floor AND nearly still on the ground (a swinging foot skimming the floor is not planted)."""
    S = tgt.soles(H, D, k)
    n = len(S)
    S[:, :, 1] -= speed * np.arange(n)[:, None] / fps
    low = S[:, :, 2].argmin(1)
    P = S[np.arange(n), low]
    v = np.linalg.norm(np.gradient(S[:, :, :2], axis=0), axis=2).min(1) * fps     # slowest sole point (m/s)
    w = (1 - smoothstep((P[:, 2] - 0.012) / 0.02)) * (1 - smoothstep((v - 0.25) / 0.35))
    return P, low, w


def check(tgt, Q, Hh, speed, dur):
    """Diagnostics on the final clip: how far planted soles slide on the ground (cm per contact) when the body travels at
    `speed`, the deepest floor penetration and the mean planted height (cm)."""
    Rw = tgt.world_from_local(Q)
    H, T, D = tgt.fk(Rw, Hh)
    n = len(Q)
    fps = (n - 1) / dur
    slide, sink, plant = [], 0.0, []
    for k in "LR":
        P, low, w = contact(tgt, H, D, k, speed, fps)
        z = P[:, 2]
        sink = min(sink, float(z.min()))
        on = w > 0.5
        plant += list(z[on])
        i = 0
        while i < n:
            if not on[i]:
                i += 1
                continue
            j = i
            while j + 1 < n and on[j + 1] and low[j + 1] == low[i]:
                j += 1
            if j > i:
                slide.append(float(np.linalg.norm(P[j, :2] - P[i, :2])))
            i = j + 1
    return dict(slide_cm=round(100 * (max(slide) if slide else 0), 1), sink_cm=round(100 * sink, 1),
                plant_cm=round(100 * (float(np.mean(plant)) if plant else 0), 1))


# ----------------------------------------------------------------------------------------------------------------
# Blender
# ----------------------------------------------------------------------------------------------------------------
def make_rig():
    import mh
    import build_characters as BC
    import build_humans as BH
    BC.clear()
    P = mh.sculpt(dict(gender=0.5))
    P = P - np.array([0, P[:, 1].min(), 0])
    return BC.build_armature("rig", BH.rig_points(P))


def key_clip(arm, tgt, name, Q, Hh):
    import bpy
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm.animation_data.action = act
    hb = tgt.bi["hips"]
    for f in range(len(Q)):
        for b, bn in enumerate(tgt.names):
            pb = arm.pose.bones[bn]
            pb.rotation_mode = "QUATERNION"
            pb.rotation_quaternion = tuple(Q[f, b])
            pb.keyframe_insert("rotation_quaternion", frame=f)
        loc = tgt.R[hb].T @ (Hh[f] - tgt.head[hb])
        arm.pose.bones["hips"].location = tuple(loc)
        arm.pose.bones["hips"].keyframe_insert("location", frame=f)
    arm.animation_data.action = None


def main():
    import bpy
    import build_characters as BC
    out = os.path.abspath(sys.argv[-1] if len(sys.argv) > 1 and not sys.argv[-1].endswith(".py") else "out")
    os.makedirs(out, exist_ok=True)
    only = os.environ.get("ONLY")
    arm = make_rig()
    bpy.context.scene.render.fps = FPS
    bpy.context.scene.render.fps_base = 1.0
    tgt = Target(arm)
    arm.animation_data_create()
    meta = {"fps": FPS, "hipHeight": round(float(tgt.head[tgt.bi["hips"]][2]), 4), "leg": round(float(tgt.leg), 4), "seat": SEAT, "clips": {}}
    for kv in filter(None, os.environ.get("TRY", "").split(";")):     # TRY="run=143_01:2:100" (experiments)
        n_, v_ = kv.split("=")
        tr, a_, b_ = v_.split(":")
        next(c for c in CLIPS if c["name"] == n_).update(trial=tr, range=(int(a_), int(b_)))
    for spec in CLIPS:
        if only and spec["name"] not in only.split(","):
            continue
        Q, Hh, info = build_clip(spec, tgt)
        key_clip(arm, tgt, spec["name"], Q, Hh)
        meta["clips"][spec["name"]] = info
        print("clip", spec["name"], json.dumps(info), flush=True)
    arm["az_clips"] = json.dumps(meta)
    json.dump(meta, open(os.path.join(out, "anims.json"), "w"), indent=1)
    BC.export(os.path.join(out, "anims.glb"), {arm}, True)
    print("done ->", out)


if __name__ == "__main__":
    main()
