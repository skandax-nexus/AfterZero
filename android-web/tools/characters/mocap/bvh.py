"""Minimal BVH reader + forward kinematics (numpy). Used by build_anims.py to read CMU motion capture.

load(path) -> Motion with
  names[j], parent[j], offset[j] (rest offsets, file units), frame_time,
  rot[f, j] (3x3 local rotation), root_pos[f] (file units)
fk(m) -> (W[f, j] 3x3 world rotations, P[f, j] world joint positions) in the file's frame (CMU: Y up, actor rest faces +Z).
End sites are added as joints named <parent>_end with no channels.
"""
import numpy as np


class Motion:
    pass


def _rot(axis, deg):
    a = np.radians(deg)
    c, s = np.cos(a), np.sin(a)
    n = len(a)
    R = np.zeros((n, 3, 3))
    i, j = {"X": (1, 2), "Y": (2, 0), "Z": (0, 1)}[axis]
    k = 3 - i - j
    R[:, k, k] = 1
    R[:, i, i] = c
    R[:, j, j] = c
    R[:, i, j] = -s
    R[:, j, i] = s
    return R


def load(path):
    tok = open(path).read().split()
    m = Motion()
    m.names, m.parent, m.offset, m.channels = [], [], [], []
    stack, i = [], 0
    while tok[i] != "MOTION":
        t = tok[i]
        if t in ("ROOT", "JOINT"):
            m.names.append(tok[i + 1])
            m.parent.append(stack[-1] if stack else -1)
            m.channels.append([])
            m.offset.append(None)
            i += 2
        elif t == "End":
            m.names.append(m.names[stack[-1]] + "_end")
            m.parent.append(stack[-1])
            m.channels.append([])
            m.offset.append(None)
            i += 2
        elif t == "{":
            stack.append(len(m.names) - 1)
            i += 1
        elif t == "}":
            stack.pop()
            i += 1
        elif t == "OFFSET":
            m.offset[stack[-1]] = [float(x) for x in tok[i + 1:i + 4]]
            i += 4
        elif t == "CHANNELS":
            n = int(tok[i + 1])
            m.channels[stack[-1]] = tok[i + 2:i + 2 + n]
            i += 2 + n
        else:
            i += 1
    nf = int(tok[i + 2])
    m.frame_time = float(tok[i + 5])
    data = np.array(tok[i + 6:], float)
    nc = sum(len(c) for c in m.channels)
    data = data[: nf * nc].reshape(nf, nc)
    m.offset = np.array(m.offset, float)
    J = len(m.names)
    m.rot = np.tile(np.eye(3), (nf, J, 1, 1))
    m.root_pos = np.zeros((nf, 3))
    c0 = 0
    for j, ch in enumerate(m.channels):
        R = np.tile(np.eye(3), (nf, 1, 1))
        for k, name in enumerate(ch):
            col = data[:, c0 + k]
            if name.endswith("position"):
                m.root_pos[:, "XYZ".index(name[0])] = col
            else:
                R = R @ _rot(name[0], col)
        m.rot[:, j] = R
        c0 += len(ch)
    m.nf = nf
    m.index = {n: j for j, n in enumerate(m.names)}
    return m


def fk(m):
    nf, J = m.rot.shape[:2]
    W = np.zeros((nf, J, 3, 3))
    P = np.zeros((nf, J, 3))
    for j in range(J):
        p = m.parent[j]
        if p < 0:
            W[:, j] = m.rot[:, j]
            P[:, j] = m.root_pos + m.offset[j]
        else:
            W[:, j] = W[:, p] @ m.rot[:, j]
            P[:, j] = P[:, p] + np.einsum("fab,b->fa", W[:, p], m.offset[j])
    return W, P
