/* AFTERZERO — human characters: skinned glTF models, retargeted animation clips, per-instance variation.
   Models (assets/models): civilian_f / civilian_m (townsfolk), keeper (Keepers and pursuers), anims (shared clip library). */
(function () {
  "use strict";
  const AZ = window.AZ;
  const H = (AZ.humans = { ready: false, list: [] });
  const FILES = { f: "models/civilian_f.glb", m: "models/civilian_m.glb", keeper: "models/keeper.glb", arin: "models/arin.glb", anims: "models/anims.glb" };
  // shared clip library name -> our action name
  const LIB = { idle: "idle", walk: "walk", run: "run", sneak_pose: "crouch", agree: "nod", headShake: "talk", sad_pose: "sad" };
  const ADULT = 1.76;
  const T = {};
  const key = n => n.replace(/^mixamorig/, "");
  let libRoot = null;

  // ---------------- loading ----------------
  H.load = async () => {
    if (H.ready || H.loading) return H.loading;
    const loader = new THREE.GLTFLoader();
    const get = f => new Promise((ok, bad) => loader.load(f, ok, null, bad));
    H.loading = (async () => {
      try {
        const [f, m, keeper, arin, anims] = await Promise.all([FILES.f, FILES.m, FILES.keeper, FILES.arin, FILES.anims].map(get));
        const lib = anims.animations.filter(a => LIB[a.name]);
        libRoot = anims.scene;
        prepare("f", f, lib, {});
        prepare("m", m, lib, {});
        prepare("arin", arin, lib, {});
        prepare("keeper", keeper, lib, { native: { Idle: "idle", Walk: "walk", Run: "run" } });
        H.ready = true;
      } catch (e) { console.warn("Human models unavailable, using stylised figures.", e && e.message); }
    })();
    return H.loading;
  };

  function prepare(kind, gltf, lib, opts) {
    const scene = gltf.scene;
    scene.updateMatrixWorld(true);
    let top = 0;
    scene.traverse(o => { if (o.isBone) { const y = o.getWorldPosition(new THREE.Vector3()).y; if (/HeadTop_End$/.test(key(o.name))) top = Math.max(top, y); else if (!top && /Head$/.test(key(o.name))) top = y * 1.08; } });
    if (!top) top = ADULT;
    const clips = {};
    const native = opts.native || {};
    gltf.animations.forEach(a => { if (native[a.name]) clips[native[a.name]] = a; });
    lib.forEach(a => { const n = LIB[a.name]; if (!clips[n]) clips[n] = retarget(libRoot, a, scene); });
    scene.traverse(o => {
      if (!o.isMesh) return;
      o.castShadow = true; o.receiveShadow = true; o.frustumCulled = false;
      if (o.material) { o.material.envMapIntensity = 0.6; if (o.material.metalness > 0.5) o.material.metalness = 0.5; }
    });
    T[kind] = { scene, clips, scale: ADULT / top };
  }

  /** World-space retarget: bakes a clip from the shared skeleton onto another skeleton (bone names match without the mixamorig prefix). */
  function retarget(srcRoot, clip, dstRoot, fps = 30) {
    const sb = {}, db = [];
    srcRoot.traverse(b => { if (b.name && !b.isMesh) sb[key(b.name)] = b; });
    dstRoot.traverse(b => { if (b.isBone && sb[key(b.name)]) db.push(b); });
    srcRoot.updateMatrixWorld(true); dstRoot.updateMatrixWorld(true);
    const wq = o => o.getWorldQuaternion(new THREE.Quaternion());
    const restS = {}, restD = new Map(), restLocal = new Map();
    for (const k in sb) restS[k] = wq(sb[k]).invert();
    db.forEach(b => { restD.set(b, wq(b)); restLocal.set(b, b.quaternion.clone()); });
    const sHips = sb.Hips, dHips = db.find(b => key(b.name) === "Hips");
    if (!sHips || !dHips) return clip;
    const sHip0 = sHips.getWorldPosition(new THREE.Vector3()), dHip0 = dHips.getWorldPosition(new THREE.Vector3());
    const ratio = dHip0.y / Math.max(1e-3, sHip0.y);
    const mixer = new THREE.AnimationMixer(srcRoot);
    mixer.clipAction(clip).play();
    const n = Math.max(2, Math.round(clip.duration * fps) + 1), times = new Float32Array(n);
    const qv = new Map(db.map(b => [b, new Float32Array(n * 4)])), pv = new Float32Array(n * 3);
    const W = new THREE.Quaternion(), PI = new THREE.Quaternion(), v = new THREE.Vector3(), wp = new THREE.Vector3();
    for (let i = 0; i < n; i++) {
      const t = Math.min(clip.duration, i / fps); times[i] = t;
      mixer.setTime(t); srcRoot.updateMatrixWorld(true);
      for (const b of db) {
        const k = key(b.name);
        W.copy(wq(sb[k])).multiply(restS[k]).multiply(restD.get(b));
        b.parent.getWorldQuaternion(PI).invert();
        b.quaternion.copy(PI.multiply(W));
        b.updateMatrixWorld(true);
        b.quaternion.toArray(qv.get(b), i * 4);
      }
      sHips.getWorldPosition(v);
      wp.copy(dHip0); wp.y += (v.y - sHip0.y) * ratio; dHips.parent.worldToLocal(wp); wp.toArray(pv, i * 3);
    }
    mixer.stopAllAction(); mixer.uncacheRoot(srcRoot);
    db.forEach(b => b.quaternion.copy(restLocal.get(b)));
    dstRoot.updateMatrixWorld(true);
    const tracks = db.map(b => new THREE.QuaternionKeyframeTrack(b.name + ".quaternion", times, qv.get(b)));
    tracks.push(new THREE.VectorKeyframeTrack(dHips.name + ".position", times, pv));
    return new THREE.AnimationClip(clip.name, clip.duration, tracks);
  }

  // ---------------- variation ----------------
  const COAT = { navy: 0x2a3a66, ochre: 0xb07a30, olive: 0x5a6a34, burgundy: 0x7a2432, grey: 0x707078, white: 0xe0ded4, teal: 0x2a8484, red: 0xb03028, obsidian: 0x26262e, brass: 0xb08840 };
  const SKIN = [1, 0.92, 0.8, 0.66, 0.52, 0.42];
  const matCache = new Map();
  function variantMat(src, id, fn) {
    const k = src.uuid + "|" + id;
    let m = matCache.get(k);
    if (!m) { m = src.clone(); fn(m); matCache.set(k, m); }
    return m;
  }
  const hash = s => { let h = 2166136261; for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 16777619); } return h >>> 0; };
  const FEMALE = /maren|odile|ilse|lise|ondine|mother|witness|nurse|matron|sister|aunt|widow|girl|woman|lady|mrs|miss|tamsin|ada|vera|mira|clara|nell|hester|agnes|rosa|elin|greta|ines|june|lena|sabine|theda|wren/i;
  H.kindFor = (name, i) => (name ? (FEMALE.test(name) ? "f" : hash(name) % 5 === 0 ? "f" : "m") : (i % 2 ? "f" : "m"));

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
  /** Make a character. opts: {coat, seed, holo, shadow}. Returns {root, play(name, fade, speed), update(dt, dist), setHolo(color)}. */
  H.make = (kind, opts = {}) => {
    const t = T[kind] || T.m;
    const root = THREE.SkeletonUtils.clone(t.scene);
    root.scale.setScalar(t.scale);
    const seed = opts.seed !== undefined ? opts.seed : Math.floor(Math.random() * 1e6);
    const coat = COAT[opts.coat] !== undefined ? COAT[opts.coat] : typeof opts.coat === "number" ? opts.coat : null;
    const skin = SKIN[seed % SKIN.length];
    const meshes = [];
    root.traverse(o => {
      if (!o.isMesh) return;
      meshes.push(o);
      o.userData.noMerge = true; o.userData.noEcho = true;
      const n = o.name || "";
      if (kind === "m") {
        if (/Outfit_Top/.test(n) && coat !== null) o.material = variantMat(o.material, "top" + coat, m => m.color.setHex(coat).lerp(new THREE.Color(1, 1, 1), 0.35));
        else if (/Outfit_Bottom/.test(n) && coat !== null) o.material = variantMat(o.material, "bot" + coat, m => m.color.setHex(coat).multiplyScalar(0.55).lerp(new THREE.Color(0.2, 0.2, 0.22), 0.4));
        else if (/Body|Head/.test(n)) o.material = variantMat(o.material, "skin" + skin, m => m.color.setScalar(skin).lerp(new THREE.Color(0.55, 0.38, 0.28), (1 - skin) * 0.5));
        else if (n === "Mesh") o.visible = seed % 3 === 0;            // hat
        else if (/Beard/.test(n)) o.visible = seed % 2 === 0;
        else if (/Eye|Teeth/.test(n)) { o.castShadow = false; }
      } else if (kind === "f" || kind === "arin") {
        const tint = coat !== null ? coat : 0xffffff;
        o.material = variantMat(o.material, "f" + tint, m => m.color.setRGB(1, 1, 1).lerp(new THREE.Color(tint), coat !== null ? 0.3 : 0));
      } else if (kind === "keeper") {
        o.material = variantMat(o.material, "keeper", m => { m.color.setRGB(0.32, 0.31, 0.36); m.roughness = 0.45; m.metalness = 0.35; m.emissive = new THREE.Color(0x1a0c00); });
      }
    });
    const mixer = new THREE.AnimationMixer(root);
    const actions = {};
    for (const n in t.clips) actions[n] = mixer.clipAction(t.clips[n]);
    let cur = null, acc = 0, frame = seed % 3;
    const h = { root, kind, mixer, actions, meshes, current: null };
    h.play = (name, fade = 0.25, speed = 1) => {
      const a = actions[name] || actions.idle;
      if (!a) return;
      a.timeScale = speed;
      if (cur === a) return;
      a.reset(); a.enabled = true; a.setEffectiveWeight(1);
      if (name === "idle" || name === "talk" || name === "nod") a.time = (seed % 97) / 97 * a.getClip().duration;
      a.play();
      if (cur) cur.crossFadeTo(a, fade, false);
      cur = a; h.current = name;
    };
    /** dist: distance to camera (far characters animate at a lower rate). */
    h.update = (dt, dist = 0, seen = true) => {
      acc += dt;
      root.visible = seen;
      if (!seen) return;
      const every = dist > 30 ? 4 : dist > 16 ? 2 : 1;
      if (++frame % every) return;
      mixer.update(acc); acc = 0;
      const sh = dist < 22 && opts.shadow !== false;
      if (sh !== h._sh) { h._sh = sh; meshes.forEach(m => { m.castShadow = sh && m.visible; }); }
    };
    h.setHolo = color => { const m = H.holoMat(color); meshes.forEach(o => { o.material = m; o.castShadow = false; o.receiveShadow = false; }); };
    h.play("idle", 0);
    if (opts.holo) h.setHolo(opts.holo);
    return h;
  };
})();
