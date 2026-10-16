/* AFTERZERO — rendering: renderer, post, per-state looks, materials, hologram shader, mesh helpers. */
(function () {
  "use strict";
  const AZ = window.AZ;
  const R = (AZ.render = {});
  let renderer, scene, camera, composer, bloom, grade, sun, hemi;
  R.holoUniforms = { uTime: { value: 0 }, uReveal: { value: 0 } };

  R.init = canvas => {
    renderer = new THREE.WebGLRenderer({ canvas, antialias: false, powerPreference: "high-performance" });
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.outputEncoding = THREE.sRGBEncoding;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    scene = new THREE.Scene();
    scene.fog = new THREE.FogExp2(0x223040, 0.02);
    camera = new THREE.PerspectiveCamera(70, 16 / 9, 0.05, 500);
    camera.rotation.order = "YXZ";
    hemi = new THREE.HemisphereLight(0xffffff, 0x222222, 0.6); scene.add(hemi);
    sun = new THREE.DirectionalLight(0xffffff, 1.2);
    sun.shadow.mapSize.set(1024, 1024);
    Object.assign(sun.shadow.camera, { left: -30, right: 30, top: 30, bottom: -30, near: 1, far: 160 });
    scene.add(sun, sun.target);
    composer = new THREE.EffectComposer(renderer);
    composer.addPass(new THREE.RenderPass(scene, camera));
    bloom = new THREE.UnrealBloomPass(new THREE.Vector2(512, 288), 0.8, 0.5, 0.8);
    composer.addPass(bloom);
    grade = new THREE.ShaderPass({
      uniforms: { tDiffuse: { value: null }, uRipple: { value: 0 }, uTime: { value: 0 }, uTint: { value: new THREE.Vector3(1, 1, 1) }, uSat: { value: 1 }, uCon: { value: 1 }, uMem: { value: 0 } },
      vertexShader: "varying vec2 vUv; void main(){ vUv=uv; gl_Position=projectionMatrix*modelViewMatrix*vec4(position,1.0); }",
      fragmentShader: `uniform sampler2D tDiffuse; uniform float uRipple; uniform float uTime; uniform vec3 uTint; uniform float uSat; uniform float uCon; uniform float uMem; varying vec2 vUv;
        void main(){ vec2 c=vUv-0.5; float d=length(c); float w=sin(d*38.0-uTime*18.0)*0.012*uRipple;
          vec2 uv=vUv+normalize(c+1e-5)*w-c*0.07*uRipple; vec2 ca=c*(0.002+0.018*uRipple);
          vec3 col=vec3(texture2D(tDiffuse,uv+ca).r, texture2D(tDiffuse,uv).g, texture2D(tDiffuse,uv-ca).b);
          col*=uTint; float l=dot(col,vec3(0.299,0.587,0.114)); col=mix(vec3(l),col,uSat*(1.0-uMem*0.85)); col=(col-0.5)*uCon+0.5;
          col=mix(col, col*vec3(1.05,0.95,0.8), uMem);
          col+=uRipple*0.22*vec3(0.6,0.9,1.0)*(1.0-smoothstep(0.0,0.7,d)); col*=1.0-smoothstep(0.38,0.85,d)*(0.5+uMem*0.3);
          gl_FragColor=vec4(max(col,0.0),1.0); }`,
    });
    composer.addPass(grade);
    R.scene = scene; R.camera = camera; R.renderer = renderer;
    R.applyQuality();
    AZ.on("settings", AZ.persistent(() => R.applyQuality()));
  };

  // ---------------- light pool ----------------
  // Levels create many "virtual" point lights; only the nearest few are real THREE lights.
  // A fixed pool keeps shader cost constant (no recompiles) — essential on phones.
  const virtual = [];
  let pool = [];
  R.poolSize = () => [4, 6, 10][AZ.settings.quality < 0 ? 1 : AZ.settings.quality];
  R.ensurePool = () => {
    const n = R.poolSize();
    while (pool.length < n) { const l = new THREE.PointLight(0xffffff, 0, 10, 2); scene.add(l); pool.push(l); }
    while (pool.length > n) scene.remove(pool.pop());
  };
  R.virtualLight = (parent, color, intensity, range, x, y, z) => {
    const v = { parent, local: new THREE.Vector3(x, y, z), color: new THREE.Color(color), intensity, distance: range, visible: true, world: new THREE.Vector3(), position: null };
    v.position = v.local;
    virtual.push(v);
    return v;
  };
  R.clearLights = () => { virtual.length = 0; };
  const visibleChain = o => { while (o) { if (!o.visible) return false; o = o.parent; } return true; };
  R.updateLights = cam => {
    const cands = [];
    for (const v of virtual) {
      if (!v.visible || v.intensity <= 0.001 || !visibleChain(v.parent)) continue;
      v.parent.localToWorld(v.world.copy(v.local));
      const d = v.world.distanceTo(cam);
      if (d > v.distance + 25) continue;
      cands.push([d - v.distance * 0.3, v]);
    }
    cands.sort((a, b) => a[0] - b[0]);
    pool.forEach((l, i) => {
      const c = cands[i];
      if (!c) { l.intensity = 0; return; }
      const v = c[1];
      l.position.copy(v.world); l.color.copy(v.color); l.distance = v.distance;
      // fade lights in/out at the pool edge to avoid popping
      const edge = i >= pool.length - 2 ? 0.6 : 1;
      l.intensity += (v.intensity * edge - l.intensity) * 0.35;
    });
  };

  // ---------------- quality / resolution ----------------
  R.scale = 1;
  R.preset = () => {
    const q = AZ.settings.quality < 0 ? (R.autoTier !== undefined ? R.autoTier : 1) : AZ.settings.quality;
    return [{ scale: 0.6, bloom: false, shadows: false, fps: 30 }, { scale: 0.8, bloom: true, shadows: false, fps: 60 }, { scale: 1, bloom: true, shadows: true, fps: 60 }][q];
  };
  R.applyQuality = () => {
    if (!renderer) return;
    const p = R.preset();
    R.scale = p.scale;
    renderer.shadowMap.enabled = p.shadows;
    sun.castShadow = p.shadows;
    R.ensurePool();
    R.resize();
  };
  R.resize = () => {
    if (!renderer) return;
    const w = window.innerWidth, h = window.innerHeight;
    const pr = Math.min(window.devicePixelRatio || 1, 2) * R.scale;
    renderer.setPixelRatio(pr);
    renderer.setSize(w, h, false);
    composer.setPixelRatio ? composer.setPixelRatio(pr) : null;
    composer.setSize(w, h);
    camera.aspect = w / Math.max(1, h);
    camera.updateProjectionMatrix();
  };
  // Dynamic resolution in Auto: keep the frame rate by trading pixels.
  let avg = 1 / 60, adjust = 0;
  R.adapt = dt => {
    if (AZ.settings.quality >= 0) return;
    avg += (dt - avg) * 0.05; adjust += dt;
    if (adjust < 1.5) return;
    adjust = 0;
    const target = 1 / 55;
    if (avg > target * 1.25 && R.scale > 0.5) { R.scale = Math.max(0.5, R.scale - 0.05); R.resize(); }
    else if (avg < target * 0.85 && R.scale < 0.95) { R.scale = Math.min(0.95, R.scale + 0.03); R.resize(); }
  };

  // ---------------- per-state looks ----------------
  R.LOOK = [
    { sky: 0xd9a46a, fog: 0xc89a66, density: 0.012, sun: 0xffcf8f, sunI: 2.0, sunPos: [-40, 30, 20], hemiS: 0xffd7a0, hemiG: 0x3a2814, hemiI: 0.8, exp: 1.05, bloom: 0.5, tint: [1.06, 0.98, 0.86], sat: 0.92, con: 1.04 },
    { sky: 0x1c2a38, fog: 0x2a3a4a, density: 0.03, sun: 0x9ab6d8, sunI: 0.5, sunPos: [30, 18, -40], hemiS: 0x4a6278, hemiG: 0x0c1014, hemiI: 0.55, exp: 1.0, bloom: 0.8, tint: [0.9, 1.0, 1.08], sat: 0.85, con: 1.12 },
    { sky: 0x0c0618, fog: 0x160a2a, density: 0.022, sun: 0x8a70ff, sunI: 0.4, sunPos: [10, 60, -20], hemiS: 0x3a2a78, hemiG: 0x05030a, hemiI: 0.5, exp: 1.1, bloom: 1.3, tint: [0.95, 0.93, 1.1], sat: 1.15, con: 1.18 },
  ];
  R.weights = [0, 1, 0];
  R.fogMul = 1;
  R.memory = 0;
  let ripple = 0;
  R.ripple = (s = 1) => { ripple = Math.max(ripple, s); };
  const C = new THREE.Color(), T = new THREE.Color();
  const mixC = (key, w) => { C.setRGB(0, 0, 0); R.LOOK.forEach((L, i) => { T.setHex(L[key]); C.r += T.r * w[i]; C.g += T.g * w[i]; C.b += T.b * w[i]; }); return C; };
  const mixN = (key, w) => R.LOOK.reduce((a, L, i) => a + L[key] * w[i], 0);

  R.frame = (dt, t, state, target) => {
    const w = R.weights;
    for (let i = 0; i < 3; i++) { const g = i === state ? 1 : 0; w[i] += Math.sign(g - w[i]) * Math.min(Math.abs(g - w[i]), dt / 0.8); }
    scene.background = mixC("sky", w).clone();
    scene.fog.color.copy(mixC("fog", w)); scene.fog.density = mixN("density", w) * R.fogMul;
    sun.color.copy(mixC("sun", w)); sun.intensity = mixN("sunI", w);
    const sp = [0, 1, 2].map(k => R.LOOK.reduce((a, L, i) => a + L.sunPos[k] * w[i], 0));
    sun.position.set(target.x + sp[0], target.y + sp[1], target.z + sp[2]); sun.target.position.copy(target);
    hemi.color.copy(mixC("hemiS", w)); hemi.groundColor.copy(mixC("hemiG", w)); hemi.intensity = mixN("hemiI", w);
    renderer.toneMappingExposure = mixN("exp", w);
    ripple = Math.max(0, ripple - dt * 1.6);
    const rp = Math.sin(Math.min(1, ripple) * Math.PI);
    bloom.strength = mixN("bloom", w) + rp * 0.6;
    grade.uniforms.uRipple.value = rp;
    grade.uniforms.uTime.value = t;
    grade.uniforms.uTint.value.set(...[0, 1, 2].map(k => R.LOOK.reduce((a, L, i) => a + L.tint[k] * w[i], 0)));
    grade.uniforms.uSat.value = mixN("sat", w); grade.uniforms.uCon.value = mixN("con", w);
    grade.uniforms.uMem.value += (R.memory - grade.uniforms.uMem.value) * Math.min(1, dt * 2);
    R.holoUniforms.uTime.value = t;
    R.updateLights(camera.position);
    MAT.window.emissiveIntensity = 0.6 + w[0] * 0.8 + w[1] * 0.4;
  };
  R.render = () => {
    const p = R.preset();
    bloom.enabled = p.bloom;
    composer.render();
  };

  // ---------------- materials ----------------
  const M = (c, r = 0.6, m = 0, e) => new THREE.MeshStandardMaterial({ color: c, roughness: r, metalness: m, emissive: e || 0x000000 });
  const B = c => new THREE.MeshBasicMaterial({ color: c, fog: true });
  const MAT = (R.MAT = {
    ground: M(0x4a443c, 0.8), stone: M(0xb8a888, 0.85), stoneWet: M(0x262b30, 0.18, 0.4), plaster: M(0xcbbba0, 0.95), plasterCold: M(0x6c747c, 0.7),
    brick: M(0x9a5a40, 0.9), wood: M(0x6a4428, 0.7), woodDark: M(0x2e1d12, 0.6), brass: M(0xd8a64c, 0.3, 1), iron: M(0x2a2c30, 0.5, 0.9),
    rust: M(0x5b3420, 0.8, 0.4), copper: M(0x4d8a78, 0.5, 0.7), rail: M(0x9a9ca0, 0.25, 1), concrete: M(0x5a5752, 0.9), tile: M(0x9fb1b4, 0.25),
    red: M(0xb2302a, 0.9), gold: M(0xd9a034, 0.9), teal: M(0x1f7a7a, 0.9), navy: M(0x1e2a48, 0.8), ochre: M(0xa6742a, 0.8), olive: M(0x4e5a2e, 0.8),
    burgundy: M(0x64202a, 0.8), grey: M(0x666670, 0.8), white: M(0xd8d8d0, 0.8), skin: M(0xc99a78, 0.5), skinDark: M(0x6e4a34, 0.5),
    paper: M(0xe8dfc8, 0.9), cup: M(0x1b52d8, 0.15), obsidian: M(0x0b0b10, 0.15, 0.5), ruin: M(0x1d1b22, 0.9), wallpaper: M(0x6f8a7a, 0.9),
    crystal: new THREE.MeshStandardMaterial({ color: 0x6a46d8, roughness: 0.05, metalness: 0.2, emissive: 0x3a1a9a, emissiveIntensity: 0.7 }),
    window: new THREE.MeshStandardMaterial({ color: 0x111111, emissive: 0xffa858, emissiveIntensity: 1.2 }),
    windowCold: new THREE.MeshStandardMaterial({ color: 0x111111, emissive: 0x6aa6e8, emissiveIntensity: 0.9 }),
    water: new THREE.MeshStandardMaterial({ color: 0x0e2a33, roughness: 0.05, metalness: 0.6, transparent: true, opacity: 0.82 }),
    glass: new THREE.MeshStandardMaterial({ color: 0x8fb4c8, roughness: 0.05, metalness: 0.1, transparent: true, opacity: 0.2 }),
    puddle: M(0x0c1218, 0.03, 0.9),
    neonC: B(0x55f0ff), neonM: B(0xff40c8), neonA: B(0xffa040), neonV: B(0x9a60ff), warm: B(0xffd28a), cold: B(0xbfe2ff), red2: B(0xff3020), zero: B(0xf2fbff), green: B(0x60ff90),
  });

  // Hologram / echo shader (same model as the Unity AfterZero/Hologram shader).
  R.holo = (color = 0x40d8ff, rim = 0xb6f6ff, alpha = 0.85, useReveal = false) => new THREE.ShaderMaterial({
    uniforms: { uTime: R.holoUniforms.uTime, uReveal: useReveal ? R.holoUniforms.uReveal : { value: 1 }, uColor: { value: new THREE.Color(color) }, uRim: { value: new THREE.Color(rim) }, uAlpha: { value: alpha } },
    vertexShader: "varying vec3 vN; varying vec3 vW; void main(){ vec4 w=modelMatrix*vec4(position,1.0); vW=w.xyz; vN=normalize(mat3(modelMatrix)*normal); gl_Position=projectionMatrix*viewMatrix*w; }",
    fragmentShader: `uniform float uTime; uniform float uReveal; uniform vec3 uColor; uniform vec3 uRim; uniform float uAlpha; varying vec3 vN; varying vec3 vW;
      void main(){ vec3 v=normalize(cameraPosition-vW); float rim=pow(1.0-abs(dot(normalize(vN),v)),2.5);
        float scan=smoothstep(0.3,1.0,0.5+0.5*sin(vW.y*30.0-uTime*4.0)); vec3 g=step(0.965,fract(vW*1.5)); float grid=max(g.x,max(g.y,g.z))*0.35;
        float fl=0.88+0.12*sin(uTime*44.0)*sin(uTime*3.1); vec3 col=uColor*(0.25+scan*0.5)+uRim*(rim+grid);
        gl_FragColor=vec4(col*1.6, clamp((0.12+rim*0.88+scan*0.2+grid)*uAlpha*fl*uReveal,0.0,1.0)); }`,
    transparent: true, depthWrite: false, blending: THREE.AdditiveBlending, side: THREE.DoubleSide,
  });
  MAT.holoC = R.holo(0x40d8ff, 0xb6f6ff); MAT.holoM = R.holo(0xff40c8, 0xffd0f0); MAT.holoA = R.holo(0xffa040, 0xfff0c0);
  MAT.echo = R.holo(0x3a90ff, 0x9af2ff, 0.5, true);
  R.mat = k => (typeof k === "string" ? MAT[k] || MAT.concrete : k || MAT.concrete);

  // ---------------- geometry helpers (cached geometries) ----------------
  const GEO = {
    box: new THREE.BoxGeometry(1, 1, 1), cyl: new THREE.CylinderGeometry(0.5, 0.5, 1, 16), cyl8: new THREE.CylinderGeometry(0.5, 0.5, 1, 8),
    sphere: new THREE.SphereGeometry(0.5, 14, 10), cone4: new THREE.ConeGeometry(0.5, 1, 4), cone: new THREE.ConeGeometry(0.5, 1, 16),
    plane: new THREE.PlaneGeometry(1, 1), torus: new THREE.TorusGeometry(0.5, 0.06, 8, 48), octa: new THREE.OctahedronGeometry(0.5),
  };
  R.GEO = GEO;
  R.mesh = (parent, geo, mat, x, y, z, sx = 1, sy = 1, sz = 1, ry = 0) => {
    const m = new THREE.Mesh(typeof geo === "string" ? GEO[geo] : geo, R.mat(mat));
    m.position.set(x, y, z); m.scale.set(sx, sy, sz); m.rotation.y = ry;
    m.castShadow = m.receiveShadow = true;
    parent.add(m);
    return m;
  };

  // Text labels (signs, glyphs, notes) as canvas textures — critical clues are never colour-only.
  const labelCache = {};
  R.label = (parent, text, x, y, z, ry = 0, size = 0.5, color = "#ffffff", bg = null) => {
    const key = text + "|" + color + "|" + bg;
    let tex = labelCache[key];
    const lines = String(text).split("\n");
    if (!tex) {
      const cv = document.createElement("canvas");
      const fs = 64, pad = 20;
      const g = cv.getContext("2d");
      g.font = "600 " + fs + "px sans-serif";
      const w = Math.max(...lines.map(l => g.measureText(l).width)) + pad * 2;
      cv.width = Math.min(2048, Math.ceil(w)); cv.height = lines.length * fs * 1.2 + pad * 2;
      const g2 = cv.getContext("2d");
      if (bg) { g2.fillStyle = bg; g2.fillRect(0, 0, cv.width, cv.height); }
      g2.font = "600 " + fs + "px sans-serif"; g2.fillStyle = color; g2.textAlign = "center"; g2.textBaseline = "middle";
      lines.forEach((l, i) => g2.fillText(l, cv.width / 2, pad + fs * 0.6 + i * fs * 1.2));
      tex = new THREE.CanvasTexture(cv); tex.encoding = THREE.sRGBEncoding; tex.anisotropy = 2;
      tex.userData = { aspect: cv.width / cv.height };
      labelCache[key] = tex;
    }
    const mat = new THREE.MeshBasicMaterial({ map: tex, transparent: !bg, depthWrite: !!bg, side: THREE.DoubleSide, fog: true });
    const h = size * lines.length;
    const m = new THREE.Mesh(GEO.plane, mat);
    m.scale.set(h * tex.userData.aspect, h, 1);
    m.position.set(x, y, z); m.rotation.y = ry;
    m.userData.noMerge = true; m.userData.noEcho = true;
    parent.add(m);
    return m;
  };

  // ---------------- particles ----------------
  R.points = (parent, count, color, size, area, center, opts = {}) => {
    const pos = new Float32Array(count * 3);
    for (let i = 0; i < count; i++) pos.set([center[0] + (Math.random() - 0.5) * area[0], center[1] + Math.random() * area[1], center[2] + (Math.random() - 0.5) * area[2]], i * 3);
    const g = new THREE.BufferGeometry(); g.setAttribute("position", new THREE.BufferAttribute(pos, 3));
    const p = new THREE.Points(g, new THREE.PointsMaterial({ color, size, transparent: true, opacity: opts.opacity || 0.85, blending: THREE.AdditiveBlending, depthWrite: false }));
    p.userData = { noMerge: true, noEcho: true, fall: opts.fall || 0, rise: opts.rise || 0, area, center, drift: opts.drift !== false };
    parent.add(p);
    return p;
  };
  R.rain = (parent, area, center, count = 1500) => {
    const pos = new Float32Array(count * 6), spd = new Float32Array(count);
    for (let i = 0; i < count; i++) { const x = center[0] + (Math.random() - 0.5) * area[0], y = center[1] + Math.random() * area[1], z = center[2] + (Math.random() - 0.5) * area[2]; pos.set([x, y, z, x - 0.04, y - 0.55, z + 0.02], i * 6); spd[i] = 16 + Math.random() * 8; }
    const g = new THREE.BufferGeometry(); g.setAttribute("position", new THREE.BufferAttribute(pos, 3));
    const l = new THREE.LineSegments(g, new THREE.LineBasicMaterial({ color: 0xa8c8e8, transparent: true, opacity: 0.35 }));
    l.userData = { noMerge: true, noEcho: true, rain: { spd, area, center, count } };
    parent.add(l);
    return l;
  };
  R.animateParticles = (root, dt, t) => {
    root.traverse(o => {
      const u = o.userData;
      if (!o.visible) return;
      if (u.rain) {
        const a = o.geometry.attributes.position.array, r = u.rain;
        for (let i = 0; i < r.count; i++) { const k = i * 6, s = r.spd[i] * dt; a[k + 1] -= s; a[k + 4] -= s; if (a[k + 4] < r.center[1]) { a[k + 1] += r.area[1]; a[k + 4] += r.area[1]; } }
        o.geometry.attributes.position.needsUpdate = true;
      } else if (o.isPoints && (u.fall || u.rise)) {
        const a = o.geometry.attributes.position.array;
        for (let i = 1; i < a.length; i += 3) { a[i] += (u.rise - u.fall) * dt; if (a[i] < u.center[1]) a[i] += u.area[1]; if (a[i] > u.center[1] + u.area[1]) a[i] -= u.area[1]; }
        o.geometry.attributes.position.needsUpdate = true;
      } else if (o.isPoints && u.drift) {
        o.position.y = Math.sin(t * 0.3 + o.id) * 0.25;
      }
    });
  };

  // ---------------- static batching (big win on phones) ----------------
  // Merges non-interactive, non-moving meshes per material inside a group into one draw call each.
  R.mergeStatic = group => {
    const BGU = THREE.BufferGeometryUtils;
    if (!BGU) return;
    group.updateMatrixWorld(true);
    const buckets = new Map();
    const victims = [];
    group.traverse(o => {
      if (!o.isMesh || o.userData.noMerge || o.userData.dynamic || o.userData.interactive || o.material.transparent || o.material.isShaderMaterial) return;
      let p = o.parent, skip = false;
      while (p && p !== group) { if (p.userData.dynamic || p.userData.noMerge) { skip = true; break; } p = p.parent; }
      if (skip) return;
      const key = o.material.uuid + (o.castShadow ? "s" : "");
      if (!buckets.has(key)) buckets.set(key, { mat: o.material, geos: [] });
      const g = o.geometry.index ? o.geometry.toNonIndexed() : o.geometry.clone();
      const keep = {};
      ["position", "normal"].forEach(a => { if (g.attributes[a]) keep[a] = g.attributes[a]; });
      const ng = new THREE.BufferGeometry();
      for (const a in keep) ng.setAttribute(a, keep[a]);
      ng.applyMatrix4(o.matrixWorld);
      buckets.get(key).geos.push(ng);
      victims.push(o);
    });
    victims.forEach(o => o.parent.remove(o));
    const inv = new THREE.Matrix4().copy(group.matrixWorld).invert();
    buckets.forEach(b => {
      for (let i = 0; i < b.geos.length; i += 400) {
        const merged = BGU.mergeBufferGeometries(b.geos.slice(i, i + 400), false);
        if (!merged) continue;
        merged.applyMatrix4(inv);
        const m = new THREE.Mesh(merged, b.mat);
        m.castShadow = m.receiveShadow = true;
        m.userData.merged = true;
        group.add(m);
      }
    });
  };

  // Holographic echoes of a state's group (revealed by Echo Sense).
  R.buildEcho = group => {
    const e = new THREE.Group();
    group.updateMatrixWorld(true);
    let n = 0;
    group.traverse(o => {
      if (n > 500 || !o.isMesh || o.userData.noEcho || o.material.isShaderMaterial || o.material.isMeshBasicMaterial) return;
      const m = new THREE.Mesh(o.geometry, MAT.echo);
      m.applyMatrix4(o.matrixWorld);
      m.userData.noMerge = true;
      e.add(m); n++;
    });
    e.visible = false;
    return e;
  };
})();
