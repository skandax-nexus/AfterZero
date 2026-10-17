/* AFTERZERO — the cast: our own characters (built by tools/characters/build_characters.py), skinned and animated.
   models/cast.glb holds every character (one root per character id); models/anims.glb holds the shared rig + clips.
   Every body has its own proportions, so the shared clips are retargeted onto each skeleton (once, on first use). */
(function () {
  "use strict";
  const AZ = window.AZ;
  const H = (AZ.humans = { ready: false, ids: [] });
  const T = {};          // id -> { root, clips, height, hipsRest, townsfolk }
  const key = n => n.replace(/_\d+$/, "");
  let lib = null;        // { clips, root }

  // ---------------- loading ----------------
  H.load = async () => {
    if (H.ready || H.loading) return H.loading;
    const loader = new THREE.GLTFLoader();
    if (window.MeshoptDecoder) loader.setMeshoptDecoder(window.MeshoptDecoder);
    // models come from files, or (web preview) from base64 embedded by js/models_embedded.js
    const emb = window.AZ_MODELS || {};
    const get = f => new Promise((ok, bad) => {
      const k = f.replace(/^models\/|\.glb$/g, "");
      if (!emb[k]) return loader.load(f, ok, null, bad);
      const bin = atob(emb[k]), buf = new Uint8Array(bin.length);
      for (let i = 0; i < bin.length; i++) buf[i] = bin.charCodeAt(i);
      loader.parse(buf.buffer, "", ok, bad);
    });
    H.loading = (async () => {
      try {
        if (window.MeshoptDecoder && MeshoptDecoder.ready) await MeshoptDecoder.ready;
        const [cast, anims] = await Promise.all([get("models/cast.glb"), get("models/anims.glb")]);
        lib = { clips: anims.animations, root: anims.scene };
        cast.scene.children.slice().forEach(root => {
          let skinned = null, hipsB = null, top = 0;
          root.traverse(o => {
            if (o.isSkinnedMesh) skinned = o;
            if (o.isBone && key(o.name) === "hips") hipsB = o;
            if (o.isMesh) {
              o.castShadow = o.receiveShadow = true; o.frustumCulled = false;
              [].concat(o.material).forEach(m => { const t = m.userData.tint; if (t) m.color.setRGB(t[0], t[1], t[2]); m.envMapIntensity = 0.7; });
            }
          });
          if (!skinned || !hipsB) return;
          root.updateMatrixWorld(true);
          root.traverse(o => { if (o.isBone) top = Math.max(top, o.getWorldPosition(new THREE.Vector3()).y); });
          T[root.name] = { root, height: top, hipsRest: hipsB.position.clone(), clips: null, townsfolk: /^town_/.test(root.name) };
          H.ids.push(root.name);
        });
        H.ready = H.ids.length > 0;
      } catch (e) { console.warn("Cast unavailable, using stylised figures.", e && e.message); }
    })();
    return H.loading;
  };

  /** Shared clips baked onto a template's skeleton (world-space retarget: every body has its own proportions/rest pose). */
  function clipsFor(t) {
    if (t.clips) return t.clips;
    t.clips = {};
    lib.clips.forEach(c => { t.clips[c.name] = retarget(lib.root, c, t.root); });
    return t.clips;
  }
  function retarget(srcRoot, clip, dstRoot, fps = 30) {
    const sb = {}, db = [];
    srcRoot.traverse(b => { if (b.name && !b.isMesh) sb[key(b.name)] = b; });
    dstRoot.traverse(b => { if (b.isBone && sb[key(b.name)]) db.push(b); });
    srcRoot.updateMatrixWorld(true); dstRoot.updateMatrixWorld(true);
    const wq = o => o.getWorldQuaternion(new THREE.Quaternion());
    const restS = {}, restD = new Map(), restLocal = new Map();
    for (const k in sb) restS[k] = wq(sb[k]).invert();
    db.forEach(b => { restD.set(b, wq(b)); restLocal.set(b, b.quaternion.clone()); });
    const sHips = sb.hips, dHips = db.find(b => key(b.name) === "hips");
    if (!sHips || !dHips) return clip;
    const sHip0 = sHips.getWorldPosition(new THREE.Vector3()), dHip0 = dHips.getWorldPosition(new THREE.Vector3());
    const ratio = dHip0.y / Math.max(1e-3, sHip0.y);
    const mixer = new THREE.AnimationMixer(srcRoot);
    mixer.clipAction(clip).play();
    const n = Math.max(2, Math.round(clip.duration * fps) + 1), times = new Float32Array(n);
    const qv = new Map(db.map(b => [b, new Float32Array(n * 4)])), pv = new Float32Array(n * 3);
    const W = new THREE.Quaternion(), PI = new THREE.Quaternion(), v = new THREE.Vector3(), wp = new THREE.Vector3();
    for (let i = 0; i < n; i++) {
      const tt = Math.min(clip.duration, i / fps); times[i] = tt;
      mixer.setTime(tt); srcRoot.updateMatrixWorld(true);
      for (const b of db) {
        const k = key(b.name);
        W.copy(wq(sb[k])).multiply(restS[k]).multiply(restD.get(b));
        b.parent.getWorldQuaternion(PI).invert();
        b.quaternion.copy(PI.multiply(W));
        b.updateMatrixWorld(true);
        b.quaternion.toArray(qv.get(b), i * 4);
      }
      sHips.getWorldPosition(v);
      wp.copy(dHip0); wp.x += (v.x - sHip0.x) * ratio; wp.y += (v.y - sHip0.y) * ratio; wp.z += (v.z - sHip0.z) * ratio;
      dHips.parent.worldToLocal(wp); wp.toArray(pv, i * 3);
    }
    mixer.stopAllAction(); mixer.uncacheRoot(srcRoot);
    db.forEach(b => b.quaternion.copy(restLocal.get(b)));
    dstRoot.updateMatrixWorld(true);
    const tracks = db.map(b => new THREE.QuaternionKeyframeTrack(b.name + ".quaternion", times, qv.get(b)));
    tracks.push(new THREE.VectorKeyframeTrack(dHips.name + ".position", times, pv));
    return new THREE.AnimationClip(clip.name, clip.duration, tracks);
  }

  // ---------------- who is who ----------------
  const NAMED = [
    [/^arin/i, "arin"], [/pell/i, "pell"], [/lise/i, "lise"], [/mirela/i, "mirela"], [/hale dovan/i, "hale"], [/vossel/i, "vossel"],
    [/calder/i, "calder"], [/ondine/i, "ondine"], [/aurel|founder/i, "aurel"], [/odile/i, "odile"], [/ilse/i, "ilse"], [/ignatz/i, "ignatz"],
    [/maren/i, "maren"], [/edda/i, "edda"], [/witness/i, "witness"], [/wren/i, "wren"], [/warden|\broe\b/i, "roe"], [/conductor|sefa/i, "sefa"],
    [/nanny|oriel/i, "oriel"], [/lantern|boatman|gate-keeper/i, "keeper_b"], [/keeper/i, "keeper"],
  ];
  const FEMALE = /\b(ida|tamsin|ada|vera|mira|clara|nell|hester|agnes|rosa|elin|greta|ines|june|lena|sabine|theda|mother|sister|aunt|widow|woman|lady|mrs|miss|nurse|matron|girl)\b/i;
  const hash = s => { let h = 2166136261; for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 16777619); } return h >>> 0; };
  const TOWN_M = ["town_m1", "town_m2", "town_m3", "town_m4", "town_m5", "town_m6"], TOWN_F = ["town_f1", "town_f2", "town_f3", "town_f4", "town_f5"];
  const have = id => (T[id] ? id : null);
  /** Character id for a name (named cast first; otherwise a townsperson picked from the name or index). */
  H.kindFor = (name, i = 0, scale = 1) => {
    if (scale < 0.85) {
      for (const [re, id] of NAMED) if (name && re.test(name) && T[id] && T[id].height < 1.4) return id;
      return have("town_kid") || H.ids[0];
    }
    if (name) for (const [re, id] of NAMED) if (re.test(name) && T[id]) return id;
    const h = name ? hash(name) : (i * 2654435761) >>> 0;
    const fem = name ? FEMALE.test(name) || h % 3 === 0 : i % 2 === 1;
    const pool = fem ? TOWN_F : TOWN_M;
    return have(pool[(h >>> 4) % pool.length]) || H.ids[0];
  };
  H.isChild = id => !!(T[id] && T[id].height < 1.4);

  // ---------------- per-person colour (townsfolk) ----------------
  const lin = hex => new THREE.Color("#" + hex).convertSRGBToLinear();
  const CLOTH_A = ["2f3b57", "6e4a2a", "4b5a33", "6a2430", "54545c", "8a7a5a", "2a6464", "7a3a28", "3a2a4a", "5a6a7a", "9a6a2a", "3c3c34"].map(lin);
  const CLOTH_B = ["23262b", "33302c", "2a2420", "3a3a44", "4a3a2c", "1e2230", "5a4a3a", "2c2c30"].map(lin);
  const SKINS = ["f1d3bf", "e6b998", "c98e68", "b07d55", "8a5a3c", "5e3b28"].map(lin);
  const COAT = { navy: "2f3b57", ochre: "9a6a2a", olive: "4b5a33", burgundy: "6a2430", grey: "54545c", white: "c8c4b8", teal: "2a6464", red: "8a2a24", obsidian: "1a1a20", gold: "a07830", brass: "a07830", crimson: "7a1e22", charcoal: "2a2c30" };
  const matCache = new Map();
  const variant = (m, id, fn) => { const k = m.uuid + "|" + id; let v = matCache.get(k); if (!v) { v = m.clone(); fn(v); matCache.set(k, v); } return v; };

  // Holographic material that follows the skeleton (same look as R.holo).
  const holoCache = {};
  H.holoMat = color => {
    if (holoCache[color]) return holoCache[color];
    const R = AZ.render, cols = { magenta: [0xff40c8, 0xffd0f0], amber: [0xffa040, 0xfff0c0] }[color] || [0x40d8ff, 0xb6f6ff];
    const m = new THREE.ShaderMaterial({
      uniforms: { uTime: R.holoUniforms.uTime, uColor: { value: new THREE.Color(cols[0]) }, uRim: { value: new THREE.Color(cols[1]) } },
      vertexShader: `#include <common>
        #include <skinning_pars_vertex>
        varying vec3 vN; varying vec3 vW;
        void main(){
          #include <skinbase_vertex>
          #include <begin_vertex>
          #include <beginnormal_vertex>
          #include <skinnormal_vertex>
          #include <skinning_vertex>
          vec4 w=modelMatrix*vec4(transformed,1.0); vW=w.xyz; vN=normalize(mat3(modelMatrix)*objectNormal); gl_Position=projectionMatrix*viewMatrix*w; }`,
      fragmentShader: `uniform float uTime; uniform vec3 uColor; uniform vec3 uRim; varying vec3 vN; varying vec3 vW;
        void main(){ vec3 v=normalize(cameraPosition-vW); float rim=pow(1.0-abs(dot(normalize(vN),v)),2.5);
          float scan=smoothstep(0.3,1.0,0.5+0.5*sin(vW.y*30.0-uTime*4.0)); float fl=0.88+0.12*sin(uTime*44.0)*sin(uTime*3.1);
          gl_FragColor=vec4((uColor*(0.25+scan*0.5)+uRim*rim)*1.6, clamp((0.12+rim*0.88+scan*0.2)*0.85*fl,0.0,1.0)); }`,
      transparent: true, depthWrite: false, blending: THREE.AdditiveBlending, side: THREE.DoubleSide,
    });
    m.skinning = true;
    return (holoCache[color] = m);
  };

  // ---------------- culling (skinned meshes are drawn only when on screen and inside the fog range) ----------------
  const frustum = new THREE.Frustum(), pm = new THREE.Matrix4(), sph = new THREE.Sphere();
  H.beginFrame = cam => { cam.updateMatrixWorld(); pm.multiplyMatrices(cam.projectionMatrix, cam.matrixWorldInverse); frustum.setFromProjectionMatrix(pm); };
  H.seen = (group, dist, maxDist = 48) => {
    if (dist > maxDist) return false;
    group.getWorldPosition(sph.center); sph.center.y += 0.9 * group.scale.y; sph.radius = 1.2 * group.scale.y;
    return frustum.intersectsSphere(sph);
  };

  // ---------------- instances ----------------
  /** Make a character. opts: {coat, seed, holo, shadow}. Returns {root, play(name, fade, speed), update(dt, dist, seen), setHolo(color)}. */
  H.make = (id, opts = {}) => {
    const t = T[id] || T[H.ids[0]];
    const root = THREE.SkeletonUtils.clone(t.root);
    root.position.set(0, 0, 0); root.rotation.set(0, 0, 0);
    const seed = opts.seed !== undefined ? opts.seed : Math.floor(Math.random() * 1e6);
    const meshes = [];
    const coat = opts.coat && COAT[opts.coat] ? lin(COAT[opts.coat]) : null;
    root.traverse(o => {
      if (!o.isMesh) return;
      meshes.push(o);
      o.userData.noMerge = true; o.userData.noEcho = true;
      if (!t.townsfolk && !opts.recolor) return;
      o.material = [].concat(o.material).map(m => {
        const slot = m.userData.slot;
        const a = coat || CLOTH_A[(seed >> 1) % CLOTH_A.length], b = CLOTH_B[(seed >> 3) % CLOTH_B.length], sk = SKINS[(seed >> 5) % SKINS.length];
        if (slot === "clothA") return variant(m, "a" + a.getHexString(), v => v.color.copy(a));
        if (slot === "clothB" && t.townsfolk) return variant(m, "b" + b.getHexString(), v => v.color.copy(b));
        if (slot === "skin" && t.townsfolk) return variant(m, "s" + sk.getHexString(), v => v.color.copy(sk));
        return m;
      });
      if (o.material.length === 1) o.material = o.material[0];
    });
    const clips = clipsFor(t);
    const mixer = new THREE.AnimationMixer(root);
    const actions = {};
    for (const n in clips) actions[n] = mixer.clipAction(clips[n]);
    let cur = null, acc = 0, frame = seed % 3;
    const h = { root, id: t.root.name, mixer, actions, meshes, current: null, height: t.height };
    h.play = (name, fade = 0.25, speed = 1) => {
      const a = actions[name] || actions.idle;
      if (!a) return;
      a.timeScale = speed;
      if (cur === a) return;
      a.reset(); a.enabled = true; a.setEffectiveWeight(1);
      if (name === "idle" || name === "talk" || name === "nod" || name === "sad") a.time = (seed % 97) / 97 * a.getClip().duration;
      a.play();
      if (cur) cur.crossFadeTo(a, fade, false);
      cur = a; h.current = name;
    };
    /** dist: distance to camera (far characters animate at a lower rate); seen: on screen. */
    h.update = (dt, dist = 0, seen = true) => {
      acc += dt;
      root.visible = seen;
      if (!seen) return;
      const every = dist > 30 ? 4 : dist > 16 ? 2 : 1;
      if (++frame % every) return;
      mixer.update(acc); acc = 0;
      const sh = dist < 22 && opts.shadow !== false;
      if (sh !== h._sh) { h._sh = sh; meshes.forEach(m => { m.castShadow = sh; }); }
    };
    // Echoes (people seen across time) stay real people: their own skin and clothes, with a faint glow of their time state.
    h.setHolo = color => {
      const tint = new THREE.Color({ magenta: 0xff70d8, amber: 0xffb060 }[color] || 0x60d8ff);
      meshes.forEach(o => {
        o.material = [].concat(o.material).map(m => variant(m, "echo" + color, v => {
          v.emissive = tint.clone(); v.emissiveIntensity = 0.28; v.transparent = true; v.opacity = 0.88;
        }));
        if (o.material.length === 1) o.material = o.material[0];
        o.castShadow = false;
      });
    };
    h.play("idle", 0);
    if (opts.holo) h.setHolo(opts.holo);
    return h;
  };
})();
