/* AFTERZERO — rendering: renderer, post, per-state looks, materials, hologram shader, mesh helpers.
   Graphics layer (all procedural, no texture assets):
   - world-space surface detail on the environment materials (blocks/brick/planks/plaster/tiles/metal, wetness + puddles)
   - sky dome (3 blended time-state skies, sun/moon glow, clouds, stars) whose horizon equals the fog colour
   - PMREM image-based lighting generated per time state from a small procedural "city" scene
   - texel-snapped sun shadows that follow the player (Medium 1024 / High 2048)
   - radial height fog with sun in-scattering (global fog chunk patch; only uses mvPosition, safe for skinned meshes)
   - one grade pass: FXAA (Medium/High) + chromatic edge + grade + vignette + film grain + time-state dissolve */
(function () {
  "use strict";
  const AZ = window.AZ;
  const R = (AZ.render = {});
  let renderer, scene, camera, composer, bloom, grade, sun, hemi, sky, copyPass, pmrem;
  R.holoUniforms = { uTime: { value: 0 }, uReveal: { value: 0 } };

  // ---------------- procedural noise texture (tileable value-noise fbm, 4 channels) ----------------
  // R: fbm base 4 (4 oct)  G: fbm base 8 (3 oct)  B: fbm base 16 (2 oct)  A: value noise base 64
  const NOISE = (() => {
    const N = 128, data = new Uint8Array(N * N * 4);
    let seed = 1337;
    const rnd = () => (seed = (seed * 16807) % 2147483647) / 2147483647;
    const lattice = p => { const g = new Float32Array(p * p); for (let i = 0; i < g.length; i++) g[i] = rnd(); return g; };
    const vnoise = (g, p, x, y) => {
      const x0 = Math.floor(x), y0 = Math.floor(y), fx = x - x0, fy = y - y0;
      const sx = fx * fx * (3 - 2 * fx), sy = fy * fy * (3 - 2 * fy);
      const at = (a, b) => g[(((b % p) + p) % p) * p + (((a % p) + p) % p)];
      const a = at(x0, y0) + (at(x0 + 1, y0) - at(x0, y0)) * sx, b = at(x0, y0 + 1) + (at(x0 + 1, y0 + 1) - at(x0, y0 + 1)) * sx;
      return a + (b - a) * sy;
    };
    [[4, 4], [8, 3], [16, 2], [64, 1]].forEach(([base, oct], c) => {
      const L = []; for (let o = 0; o < oct; o++) L.push(lattice(base << o));
      const v = new Float32Array(N * N); let mn = 1e9, mx = -1e9;
      for (let y = 0; y < N; y++) for (let x = 0; x < N; x++) {
        let s = 0, amp = 1, tot = 0;
        for (let o = 0; o < oct; o++) { const p = base << o; s += vnoise(L[o], p, (x / N) * p, (y / N) * p) * amp; tot += amp; amp *= 0.5; }
        s /= tot; v[y * N + x] = s; if (s < mn) mn = s; if (s > mx) mx = s;
      }
      for (let i = 0; i < N * N; i++) data[i * 4 + c] = Math.round(((v[i] - mn) / (mx - mn)) * 255);
    });
    const t = new THREE.DataTexture(data, N, N, THREE.RGBAFormat);
    t.wrapS = t.wrapT = THREE.RepeatWrapping; t.magFilter = THREE.LinearFilter; t.minFilter = THREE.LinearMipmapLinearFilter;
    t.generateMipmaps = true; t.needsUpdate = true;
    return t;
  })();
  const NOISE_U = { value: NOISE };
  R.noiseTexture = NOISE;

  // ---------------- fog: radial exp2 + height falloff + sun in-scattering ----------------
  // Shared Float32Arrays are referenced (not copied) by UniformsUtils.clone, so every material sees live values.
  // A material without these uniforms reads zeros, which the chunk treats as "plain exp2 fog".
  const FOGA = new Float32Array([0, 1, 0, 0]); // sun dir xyz, scatter amount
  const FOGB = new Float32Array([0, 0, 0, 0.09]); // scatter colour rgb, height falloff (1/m)
  const FOGC = new Float32Array([0, 0.55, 1.1, 1]); // base height, density mul high, density mul low, enabled
  (function patchFog() {
    const SC = THREE.ShaderChunk;
    SC.fog_pars_vertex = "#ifdef USE_FOG\n\tvarying float fogDepth;\n\tvarying vec3 vAzFogDir;\n#endif";
    SC.fog_vertex = "#ifdef USE_FOG\n\tfogDepth = - mvPosition.z;\n\tvAzFogDir = vec3( dot( viewMatrix[ 0 ].xyz, mvPosition.xyz ), dot( viewMatrix[ 1 ].xyz, mvPosition.xyz ), dot( viewMatrix[ 2 ].xyz, mvPosition.xyz ) );\n#endif";
    SC.fog_pars_fragment = "#ifdef USE_FOG\n\tuniform vec3 fogColor;\n\tvarying float fogDepth;\n\tvarying vec3 vAzFogDir;\n\tuniform vec4 azFogA;\n\tuniform vec4 azFogB;\n\tuniform vec4 azFogC;\n\t#ifdef FOG_EXP2\n\t\tuniform float fogDensity;\n\t#else\n\t\tuniform float fogNear;\n\t\tuniform float fogFar;\n\t#endif\n#endif";
    SC.fog_fragment = `#ifdef USE_FOG
	float azFD = length( vAzFogDir );
	vec3 azFV = vAzFogDir / max( azFD, 1e-4 );
	#ifdef FOG_EXP2
		float azHM = 1.0;
		if ( azFogC.w > 0.5 ) azHM = mix( azFogC.y, azFogC.z, exp( - max( cameraPosition.y + vAzFogDir.y * 0.5 - azFogC.x, 0.0 ) * azFogB.w ) );
		float fogFactor = 1.0 - exp( - fogDensity * fogDensity * azHM * azHM * azFD * azFD );
	#else
		float fogFactor = smoothstep( fogNear, fogFar, fogDepth );
	#endif
	vec3 azFogCol = fogColor + azFogB.rgb * ( azFogA.w * pow( max( dot( azFV, azFogA.xyz ), 0.0 ), 6.0 ) );
	gl_FragColor.rgb = mix( gl_FragColor.rgb, azFogCol, fogFactor );
#endif`;
    const add = u => { u.azFogA = { value: FOGA }; u.azFogB = { value: FOGB }; u.azFogC = { value: FOGC }; };
    add(THREE.UniformsLib.fog);
    for (const k in THREE.ShaderLib) { const u = THREE.ShaderLib[k].uniforms; if (u && u.fogColor) add(u); }
  })();

  // ---------------- sky dome ----------------
  const SKY_VERT = "varying vec3 vDir; void main(){ vDir = position; vec4 p = projectionMatrix * viewMatrix * vec4( cameraPosition + position, 1.0 ); gl_Position = p.xyww; }";
  const HASH = "float azHash( vec2 p ) { vec3 p3 = fract( vec3( p.xyx ) * 0.1031 ); p3 += dot( p3, p3.yzx + 33.33 ); return fract( ( p3.x + p3.y ) * p3.z ); }\n";
  const SKY_FRAG = `uniform vec3 uZen; uniform vec3 uSky; uniform vec3 uHor; uniform vec3 uGround; uniform vec3 uSunDir; uniform vec3 uSunCol;
    uniform vec4 uGlow; uniform vec3 uCloud; uniform vec4 uMisc; uniform vec4 azFogA; uniform vec4 azFogB; uniform sampler2D azNoise;
    varying vec3 vDir;
    ${HASH}
    void main(){
      vec3 d = normalize( vDir ); float h = d.y;
      float t1 = smoothstep( 0.0, 0.32, h ), t2 = smoothstep( 0.22, 0.95, h );
      vec3 col = mix( mix( uHor, uSky, t1 ), uZen, t2 );
      col = mix( col, uGround, smoothstep( 0.0, -0.25, h ) );
      float sd = max( dot( d, uSunDir ), 0.0 );
      col += azFogB.rgb * ( azFogA.w * pow( sd, 6.0 ) );
      float halo = pow( sd, uGlow.x ) * uGlow.y + smoothstep( uGlow.z, uGlow.z + 0.00025, sd ) * uGlow.w;
      float cov = 0.0;
      if ( uMisc.x > 0.01 && h > 0.0 ) {
        vec2 cu = d.xz / ( h + 0.15 ) * 0.2 + vec2( uMisc.z * 0.004, uMisc.z * 0.0017 );
        float c = texture2D( azNoise, cu ).r * 0.62 + texture2D( azNoise, cu * 3.1 + 0.3 ).g * 0.38;
        cov = smoothstep( 1.0 - uMisc.x, 1.0 - uMisc.x * 0.4, c ) * smoothstep( 0.0, 0.18, h );
        vec3 cc = uCloud * ( 0.7 + 0.6 * c ) + uSunCol * pow( sd, 3.0 ) * 0.3;
        col = mix( col, cc, cov * 0.88 );
      }
      col += uSunCol * halo * ( 1.0 - cov * 0.75 );
      if ( uMisc.y > 0.01 ) {
        vec3 sp = d * 150.0; vec3 cp = floor( sp ); float hs = azHash( cp.xy + cp.z * 31.7 );
        float st = step( 0.986, hs ) * smoothstep( 0.34, 0.06, length( fract( sp ) - 0.5 ) ) * ( 0.55 + 0.45 * sin( uMisc.z * ( 1.5 + hs * 3.0 ) + hs * 90.0 ) );
        col += vec3( 0.85, 0.85, 1.0 ) * st * uMisc.y * smoothstep( 0.04, 0.3, h ) * ( 1.0 - cov );
      }
      gl_FragColor = vec4( col, 1.0 );
      // env capture: linear radiance, encoded for the PMREM target (RGBE); the visible dome stays raw like the fog colour
      if ( uMisc.w > 0.0 ) gl_FragColor = linearToOutputTexel( vec4( pow( max( col, 0.0 ), vec3( 2.2 ) ) * uMisc.w, 1.0 ) );
    }`;
  const skyMaterial = (fa = FOGA, fb = FOGB) => new THREE.ShaderMaterial({
    uniforms: {
      uZen: { value: new THREE.Color() }, uSky: { value: new THREE.Color() }, uHor: { value: new THREE.Color() }, uGround: { value: new THREE.Color() },
      uSunDir: { value: new THREE.Vector3(0, 1, 0) }, uSunCol: { value: new THREE.Color() }, uGlow: { value: new THREE.Vector4(8, 0.3, 0.9996, 1) },
      uCloud: { value: new THREE.Color() }, uMisc: { value: new THREE.Vector4() }, azFogA: { value: fa }, azFogB: { value: fb }, azNoise: NOISE_U,
    },
    vertexShader: SKY_VERT, fragmentShader: SKY_FRAG, side: THREE.BackSide, depthWrite: false, fog: false,
  });

  // ---------------- grade pass: FXAA + grade + vignette + grain + dissolve ----------------
  const GRADE = {
    defines: { FXAA: 1 },
    uniforms: {
      tDiffuse: { value: null }, tPrev: { value: null }, tNoise: NOISE_U, uRipple: { value: 0 }, uTime: { value: 0 }, uTint: { value: new THREE.Vector3(1, 1, 1) },
      uSat: { value: 1 }, uCon: { value: 1 }, uMem: { value: 0 }, uRes: { value: new THREE.Vector2(1 / 960, 1 / 540) }, uTrans: { value: -1 },
      uTransCol: { value: new THREE.Vector3(0.5, 0.85, 1) }, uGrain: { value: 0.035 },
    },
    vertexShader: "varying vec2 vUv; void main(){ vUv=uv; gl_Position=projectionMatrix*modelViewMatrix*vec4(position,1.0); }",
    fragmentShader: `uniform sampler2D tDiffuse; uniform sampler2D tPrev; uniform sampler2D tNoise; uniform float uRipple; uniform float uTime; uniform vec3 uTint;
      uniform float uSat; uniform float uCon; uniform float uMem; uniform vec2 uRes; uniform float uTrans; uniform vec3 uTransCol; uniform float uGrain; varying vec2 vUv;
      ${HASH}
      vec3 fetch( vec2 uv ) { return texture2D( tDiffuse, uv ).rgb; }
      #ifdef FXAA
      vec3 fxaa( vec2 uv ) {
        vec3 rgbM = fetch( uv );
        vec3 rgbNW = fetch( uv - uRes ), rgbNE = fetch( uv + vec2( uRes.x, -uRes.y ) ), rgbSW = fetch( uv + vec2( -uRes.x, uRes.y ) ), rgbSE = fetch( uv + uRes );
        vec3 lw = vec3( 0.299, 0.587, 0.114 );
        float lNW = dot( rgbNW, lw ), lNE = dot( rgbNE, lw ), lSW = dot( rgbSW, lw ), lSE = dot( rgbSE, lw ), lM = dot( rgbM, lw );
        float lMin = min( lM, min( min( lNW, lNE ), min( lSW, lSE ) ) ), lMax = max( lM, max( max( lNW, lNE ), max( lSW, lSE ) ) );
        if ( lMax - lMin < max( 0.04, lMax * 0.125 ) ) return rgbM;
        vec2 dir = vec2( -( ( lNW + lNE ) - ( lSW + lSE ) ), ( lNW + lSW ) - ( lNE + lSE ) );
        float red = max( ( lNW + lNE + lSW + lSE ) * 0.03125, 0.0078125 );
        dir = clamp( dir / ( min( abs( dir.x ), abs( dir.y ) ) + red ), -8.0, 8.0 ) * uRes;
        vec3 a = 0.5 * ( fetch( uv - dir * 0.1667 ) + fetch( uv + dir * 0.1667 ) );
        vec3 b = a * 0.5 + 0.25 * ( fetch( uv - dir * 0.5 ) + fetch( uv + dir * 0.5 ) );
        float lb = dot( b, lw );
        return ( lb < lMin || lb > lMax ) ? a : b;
      }
      #endif
      void main(){
        vec2 c = vUv - 0.5; float d = length( c );
        float w = sin( d * 38.0 - uTime * 18.0 ) * 0.012 * uRipple;
        vec2 uv = vUv + normalize( c + 1e-5 ) * w - c * 0.07 * uRipple;
        #ifdef FXAA
          vec3 col = fxaa( uv );
        #else
          vec3 col = fetch( uv );
        #endif
        float ck = min( 1.0, smoothstep( 0.2, 0.62, d ) + uRipple );
        if ( ck > 0.01 ) { vec2 ca = c * ( 0.0028 + 0.018 * uRipple ); col.r = mix( col.r, fetch( uv + ca ).r, ck ); col.b = mix( col.b, fetch( uv - ca ).b, ck ); }
        float edge = 0.0;
        if ( uTrans >= 0.0 ) {
          vec2 nuv = vec2( vUv.x * uRes.y / uRes.x, vUv.y );
          float n = texture2D( tNoise, nuv * 0.55 ).r * 0.75 + texture2D( tNoise, nuv * 1.9 + 0.31 ).g * 0.25;
          float e = uTrans * 1.95 - 0.1 - ( d * 1.25 + n * 0.5 );
          vec3 old = texture2D( tPrev, c * ( 1.0 - uTrans * 0.045 ) + 0.5 ).rgb;
          col = mix( old, col, smoothstep( 0.0, 0.03, e ) );
          edge = ( 1.0 - smoothstep( 0.0, 0.045, abs( e ) ) ) * ( 1.0 - smoothstep( 0.75, 1.0, uTrans ) );
        }
        col *= uTint; float l = dot( col, vec3( 0.299, 0.587, 0.114 ) ); col = mix( vec3( l ), col, uSat * ( 1.0 - uMem * 0.85 ) ); col = ( col - 0.5 ) * uCon + 0.5;
        col = mix( col, col * vec3( 1.05, 0.95, 0.8 ), uMem );
        col += edge * uTransCol * 1.1;
        col += uRipple * 0.22 * vec3( 0.6, 0.9, 1.0 ) * ( 1.0 - smoothstep( 0.0, 0.7, d ) ); col *= 1.0 - smoothstep( 0.38, 0.85, d ) * ( 0.5 + uMem * 0.3 );
        float g = azHash( floor( gl_FragCoord.xy ) + fract( uTime * 7.31 ) * 517.0 ) - 0.5;
        col += g * uGrain * ( 1.0 - 0.65 * clamp( l, 0.0, 1.0 ) );
        gl_FragColor = vec4( max( col, 0.0 ), 1.0 );
      }`,
  };

  let lastPre = null, snapRT = null, trans = -1, frameNo = 0;
  const TRANS_DUR = 0.7;
  const STATE_EDGE = [[1.0, 0.68, 0.32], [0.45, 0.85, 1.0], [0.82, 0.5, 1.0]];

  R.init = canvas => {
    renderer = new THREE.WebGLRenderer({ canvas, antialias: false, powerPreference: "high-performance" });
    renderer.toneMapping = THREE.ACESFilmicToneMapping;
    renderer.outputEncoding = THREE.sRGBEncoding;
    renderer.shadowMap.type = THREE.PCFShadowMap;
    renderer.info.autoReset = false; // reset once per frame in R.render so info covers every pass
    scene = new THREE.Scene();
    scene.fog = new THREE.FogExp2(0x223040, 0.02);
    camera = new THREE.PerspectiveCamera(70, 16 / 9, 0.05, 500);
    camera.rotation.order = "YXZ";
    hemi = new THREE.HemisphereLight(0xffffff, 0x222222, 0.6); scene.add(hemi);
    sun = new THREE.DirectionalLight(0xffffff, 1.2);
    sun.shadow.mapSize.set(1024, 1024);
    Object.assign(sun.shadow.camera, { left: -SHADOW_R, right: SHADOW_R, top: SHADOW_R, bottom: -SHADOW_R, near: 1, far: 170 });
    sun.shadow.camera.updateProjectionMatrix();
    sun.shadow.bias = -0.0004; sun.shadow.normalBias = 0.03;
    scene.add(sun, sun.target);
    sky = new THREE.Mesh(new THREE.SphereGeometry(10, 32, 16), skyMaterial());
    sky.frustumCulled = false; sky.renderOrder = 1e6; sky.userData.noMerge = sky.userData.noEcho = true;
    scene.add(sky);
    composer = new THREE.EffectComposer(renderer);
    // Scene is written sRGB-encoded into the 8-bit targets: correct display gamma + less banding in the dark states.
    [composer.renderTarget1, composer.renderTarget2].forEach(rt => { rt.texture.encoding = THREE.sRGBEncoding; });
    composer.addPass(new THREE.RenderPass(scene, camera));
    bloom = new THREE.UnrealBloomPass(new THREE.Vector2(512, 288), 0.8, 0.5, 0.9);
    composer.addPass(bloom);
    grade = new THREE.ShaderPass(GRADE);
    const gradeRender = grade.render.bind(grade);
    grade.render = (r, wb, rb, dt, mask) => { lastPre = rb; grade.uniforms.tPrev.value = snapRT ? snapRT.texture : rb.texture; gradeRender(r, wb, rb, dt, mask); };
    composer.addPass(grade);
    copyPass = new THREE.ShaderPass(THREE.CopyShader);
    pmrem = new THREE.PMREMGenerator(renderer);
    R.scene = scene; R.camera = camera; R.renderer = renderer;
    R.applyQuality();
    AZ.on("settings", AZ.persistent(() => R.applyQuality()));
  };

  /** Time-state dissolve. Call right BEFORE the new state is applied (same frame): captures the last rendered
   *  frame and dissolves from it to the live image over ~0.7 s with a glowing noise edge. Safe at any time. */
  R.transition = () => {
    if (!renderer || !lastPre || !copyPass) return;
    if (trans >= 0 && trans < 0.45) return; // early in a running dissolve: keep the original snapshot
    try {
      if (!snapRT) snapRT = new THREE.WebGLRenderTarget(lastPre.width, lastPre.height, { minFilter: THREE.LinearFilter, magFilter: THREE.LinearFilter, format: THREE.RGBAFormat });
      else if (snapRT.width !== lastPre.width || snapRT.height !== lastPre.height) snapRT.setSize(lastPre.width, lastPre.height);
      const prev = renderer.getRenderTarget();
      copyPass.render(renderer, snapRT, lastPre);
      renderer.setRenderTarget(prev);
      trans = 0;
    } catch (e) { trans = -1; }
  };

  // ---------------- light pool ----------------
  // Levels create many "virtual" point lights; only the nearest few are real THREE lights.
  // A fixed pool keeps shader cost constant (no recompiles) — essential on phones.
  const virtual = [];
  const rains = [];
  let pool = [];
  R.poolSize = () => [4, 6, 10][AZ.settings.quality < 0 ? (R.autoTier !== undefined ? R.autoTier : 1) : AZ.settings.quality];
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
  R.clearLights = () => { virtual.length = 0; rains.length = 0; };
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
  const SHADOW_R = 18; // half-size of the shadow box around the player (m)
  R.scale = 1;
  const PRESETS = [
    { scale: 0.6, bloom: false, shadows: false, fps: 30, detail: false, ibl: false, fxaa: false, shadowSize: 0, shadowEvery: 0 },
    { scale: 0.8, bloom: true, shadows: true, fps: 60, detail: true, ibl: true, fxaa: true, shadowSize: 1024, shadowEvery: 2 },
    { scale: 1, bloom: true, shadows: true, fps: 60, detail: true, ibl: true, fxaa: true, shadowSize: 2048, shadowEvery: 1 },
  ];
  R.preset = () => {
    const q = AZ.settings.quality < 0 ? (R.autoTier !== undefined ? R.autoTier : 1) : AZ.settings.quality;
    return PRESETS[q];
  };
  let applied = null;
  R.applyQuality = () => {
    if (!renderer) return;
    const p = R.preset();
    R.scale = p.scale;
    const type = p.shadowEvery === 1 ? THREE.PCFSoftShadowMap : THREE.PCFShadowMap;
    const sig = [p.shadows, type, p.shadowSize, p.fxaa, p.ibl].join();
    renderer.shadowMap.enabled = p.shadows;
    renderer.shadowMap.type = type;
    sun.castShadow = p.shadows;
    sun.shadow.autoUpdate = p.shadowEvery <= 1;
    if (p.shadows && sun.shadow.mapSize.x !== p.shadowSize) {
      sun.shadow.mapSize.set(p.shadowSize, p.shadowSize);
      if (sun.shadow.map) { sun.shadow.map.dispose(); sun.shadow.map = null; }
    }
    sun.shadow.normalBias = p.shadowSize >= 2048 ? 0.02 : 0.03;
    DET.glob.value.x = p.detail ? 1 : 0;
    if (p.fxaa) grade.material.defines.FXAA = 1; else delete grade.material.defines.FXAA;
    if (!p.ibl) scene.environment = null;
    if (applied !== null && applied !== sig) {
      grade.material.needsUpdate = true;
      scene.traverse(o => { if (o.material) (Array.isArray(o.material) ? o.material : [o.material]).forEach(m => { m.needsUpdate = true; }); });
    }
    if (applied === null) grade.material.needsUpdate = true;
    applied = sig;
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
    grade.uniforms.uRes.value.set(1 / Math.max(1, Math.floor(w * pr)), 1 / Math.max(1, Math.floor(h * pr)));
    camera.aspect = w / Math.max(1, h);
    camera.updateProjectionMatrix();
  };
  // Dynamic resolution in Auto: keep the frame rate by trading pixels; if even the floor is too slow
  // for several windows, drop to the Low feature tier (once per session, no oscillation).
  let avg = 1 / 60, adjust = 0, starved = 0;
  R.adapt = dt => {
    if (AZ.settings.quality >= 0) return;
    avg += (dt - avg) * 0.05; adjust += dt;
    if (adjust < 1.5) return;
    adjust = 0;
    const target = 1 / 55;
    if (avg > target * 1.25 && R.scale > 0.5) { R.scale = Math.max(0.5, R.scale - 0.05); R.resize(); starved = 0; }
    else if (avg > target * 1.4 && R.scale <= 0.5 && R.autoTier === undefined) { if (++starved >= 3) { R.autoTier = 0; R.applyQuality(); } }
    else if (avg < target * 0.85 && R.scale < 0.95) { R.scale = Math.min(0.95, R.scale + 0.03); R.resize(); starved = 0; }
  };

  // ---------------- per-state looks ----------------
  // sky = upper sky, zen = zenith, fog = horizon/fog (sky dome horizon equals fog so geometry fades seamlessly).
  // glow/glowP/disc: sun-or-moon halo amount / tightness / disc; clouds 0..1 cover; stars 0..1; scat: fog sun in-scatter;
  // env: IBL intensity; win/winI/winN: distant tower window colour / intensity / lit fraction; wet: optional override (0..1).
  R.LOOK = [
    { sky: 0xc39a7c, zen: 0x56628c, fog: 0xc89a66, density: 0.012, sun: 0xffcf8f, sunI: 1.5, sunPos: [-40, 22, 20], hemiS: 0xffd7a0, hemiG: 0x3a2814, hemiI: 0.55, exp: 0.85, bloom: 0.45, tint: [1.06, 0.98, 0.86], sat: 0.92, con: 1.04,
      cloud: 0xe0b090, clouds: 0.5, stars: 0, glow: 0.6, glowP: 7, disc: 1.2, scat: 0.35, env: 0.4, win: 0xffb36a, winI: 0.8, winN: 0.22 },
    { sky: 0x1c2a38, zen: 0x0a121c, fog: 0x2a3a4a, density: 0.03, sun: 0x9ab6d8, sunI: 0.5, sunPos: [30, 18, -40], hemiS: 0x4a6278, hemiG: 0x0c1014, hemiI: 0.5, exp: 1.0, bloom: 0.8, tint: [0.9, 1.0, 1.08], sat: 0.85, con: 1.1,
      cloud: 0x34465a, clouds: 0.8, stars: 0, glow: 0.35, glowP: 12, disc: 0.6, scat: 0.2, env: 1.0, win: 0xffc68a, winI: 1.1, winN: 0.4 },
    { sky: 0x1e0e3a, zen: 0x07030f, fog: 0x160a2a, density: 0.022, sun: 0x8a70ff, sunI: 0.4, sunPos: [10, 60, -20], hemiS: 0x3a2a78, hemiG: 0x05030a, hemiI: 0.45, exp: 1.05, bloom: 1.3, tint: [0.95, 0.93, 1.1], sat: 1.15, con: 1.15,
      cloud: 0x2c1858, clouds: 0.35, stars: 1, glow: 0.5, glowP: 9, disc: 0.8, scat: 0.3, env: 0.75, win: 0xb27cff, winI: 1.3, winN: 0.3 },
  ];
  R.weights = [0, 1, 0];
  R.exposureTrim = 0.8; // global trim: scenes are now output with correct sRGB gamma (brighter mid-tones)
  R.fogMul = 1;
  R.memory = 0;
  let ripple = 0;
  R.ripple = (s = 1) => { ripple = Math.max(ripple, s); };
  const C = new THREE.Color(), T = new THREE.Color();
  const DEF = R.LOOK.map(L => Object.assign({}, L));
  const val = (L, i, key) => (L[key] !== undefined ? L[key] : DEF[i][key]);
  const mixC = (key, w) => { C.setRGB(0, 0, 0); R.LOOK.forEach((L, i) => { if (!w[i]) return; T.setHex(val(L, i, key)); C.r += T.r * w[i]; C.g += T.g * w[i]; C.b += T.b * w[i]; }); return C; };
  const mixN = (key, w) => R.LOOK.reduce((a, L, i) => a + (w[i] ? val(L, i, key) * w[i] : 0), 0);

  // ---------------- image-based lighting ----------------
  let envScene = null, envSky = null, envLights = [], envRTs = [null, null, null], envSig = "", envState = -1;
  const ENV_PAL = [[0xffb060, 0xffd08a, 0xff9040], [0xff9a48, 0x6aa6e8, 0xffd28a, 0xbfe2ff], [0xff40c8, 0x55f0ff, 0x9a60ff, 0x6a46d8]];
  const ENV_LIT = [[0.35, 2.5], [0.75, 4.0], [0.85, 5.0]]; // fraction lit, intensity
  const lookSig = () => R.LOOK.map((L, i) => ["sky", "zen", "fog", "sun", "sunI", "hemiG", "cloud", "clouds", "stars", "glow", "disc"].map(k => val(L, i, k)).join() + L.sunPos.join()).join("|");
  function buildEnvScene() {
    envScene = new THREE.Scene();
    envSky = new THREE.Mesh(new THREE.SphereGeometry(10, 32, 16), skyMaterial(new Float32Array(4), new Float32Array(4)));
    envSky.material.depthTest = false; envSky.renderOrder = -1; envSky.frustumCulled = false;
    envScene.add(envSky);
    let s = 7; const rnd = () => (s = (s * 16807) % 2147483647) / 2147483647;
    const geo = new THREE.PlaneGeometry(1, 1);
    for (let i = 0; i < 34; i++) {
      const a = rnd() * Math.PI * 2, r = 25 + rnd() * 30, y = -3 + rnd() * rnd() * 16;
      const m = new THREE.Mesh(geo, new THREE.MeshBasicMaterial({ color: 0xffffff, depthTest: false, depthWrite: false, fog: false }));
      m.position.set(Math.cos(a) * r, y, Math.sin(a) * r); m.scale.set(0.8 + rnd() * 3.5, 0.5 + rnd() * 1.6, 1); m.lookAt(0, y, 0);
      m.frustumCulled = false; m.userData.k = rnd(); m.userData.c = Math.floor(rnd() * 4);
      envScene.add(m); envLights.push(m);
    }
  }
  const _sd = new THREE.Vector3();
  function setSkyUniforms(u, i, w, t, fa, fb) {
    const L = R.LOOK[i];
    const wts = w || [0, 0, 0].map((_, k) => (k === i ? 1 : 0));
    u.uZen.value.copy(mixC("zen", wts)); u.uSky.value.copy(mixC("sky", wts)); u.uHor.value.copy(mixC("fog", wts));
    const sp = [0, 1, 2].map(k => R.LOOK.reduce((a, LL, j) => a + LL.sunPos[k] * wts[j], 0));
    _sd.set(sp[0], sp[1], sp[2]).normalize();
    u.uSunDir.value.copy(_sd); u.uSunCol.value.copy(mixC("sun", wts));
    u.uGlow.value.set(mixN("glowP", wts), mixN("glow", wts), 0.99955, mixN("disc", wts));
    u.uCloud.value.copy(mixC("cloud", wts)); u.uMisc.value.set(mixN("clouds", wts), mixN("stars", wts), t, 0);
    fa[0] = _sd.x; fa[1] = _sd.y; fa[2] = _sd.z; fa[3] = mixN("scat", wts);
    const sc = mixC("sun", wts); fb[0] = sc.r; fb[1] = sc.g; fb[2] = sc.b;
    return L;
  }
  function genEnv(i) {
    if (!envScene) buildEnvScene();
    const u = envSky.material.uniforms;
    const L = setSkyUniforms(u, i, null, 0, u.azFogA.value, u.azFogB.value);
    u.uGround.value.setHex(val(L, i, "hemiG")).lerp(u.uHor.value, 0.35);
    u.uMisc.value.w = 1.2; u.uMisc.value.y *= 0.5;
    const pal = ENV_PAL[i], lit = ENV_LIT[i];
    envLights.forEach(m => { m.visible = m.userData.k < lit[0]; m.material.color.setHex(pal[m.userData.c % pal.length]).convertSRGBToLinear().multiplyScalar(lit[1] * (0.5 + m.userData.k)); });
    return pmrem.fromScene(envScene, 0.02, 0.1, 100);
  }
  function updateEnv() {
    const s = lookSig();
    if (s === envSig) return;
    envSig = s;
    for (let i = 0; i < 3; i++) { if (envRTs[i]) envRTs[i].dispose(); envRTs[i] = genEnv(i); }
    envState = -1;
  }

  // ---------------- sun / shadows ----------------
  const _lm = new THREE.Matrix4(), _li = new THREE.Matrix4(), _lv = new THREE.Vector3(), _O = new THREE.Vector3(), _UP = new THREE.Vector3(0, 1, 0), sunDir = new THREE.Vector3(0, 1, 0);
  function placeSun(target, p) {
    // Snap the shadow box centre to whole shadow-map texels in light space: no shimmer while moving.
    _lm.lookAt(sunDir, _O, _UP); _li.copy(_lm).transpose();
    _lv.copy(target).applyMatrix4(_li);
    const tex = (2 * SHADOW_R) / (p.shadowSize || 1024);
    _lv.x = Math.round(_lv.x / tex) * tex; _lv.y = Math.round(_lv.y / tex) * tex;
    _lv.applyMatrix4(_lm);
    sun.target.position.copy(_lv); sun.position.copy(_lv).addScaledVector(sunDir, 80);
  }

  let wetAuto = 0, fogBase = 0;
  function rainWetness() {
    let wet = 0;
    for (const o of rains) {
      if (!visibleChain(o)) continue;
      let p = o; while (p.parent) p = p.parent;
      if (p !== scene) continue;
      const a = o.userData.rain.area; wet += Math.min(1, (a[0] * a[2]) / 300);
    }
    return Math.min(1, wet);
  }

  R.frame = (dt, t, state, target) => {
    if (!renderer) return;
    const p = R.preset();
    frameNo++;
    const w = R.weights;
    for (let i = 0; i < 3; i++) { const g = i === state ? 1 : 0; w[i] += Math.sign(g - w[i]) * Math.min(Math.abs(g - w[i]), dt / 0.8); }
    scene.background = null;
    const fogC = mixC("fog", w);
    scene.fog.color.copy(fogC); renderer.setClearColor(fogC);
    scene.fog.density = mixN("density", w) * R.fogMul;
    fogBase += ((target ? target.y : 0) - 0.5 - fogBase) * Math.min(1, dt * 0.5);
    FOGC[0] = fogBase;
    // sky dome + fog scatter (same sun direction / colour → seamless horizon)
    const su = sky.material.uniforms;
    setSkyUniforms(su, state, w, t, FOGA, FOGB);
    su.uGround.value.copy(su.uHor.value);
    sunDir.copy(su.uSunDir.value);
    sun.color.copy(mixC("sun", w)); sun.intensity = mixN("sunI", w);
    if (p.shadows) {
      placeSun(target, p);
      if (!sun.shadow.autoUpdate) sun.shadow.needsUpdate = frameNo % p.shadowEvery === 0;
    } else { sun.target.position.copy(target); sun.position.copy(target).addScaledVector(sunDir, 80); }
    // image-based lighting
    if (p.ibl && pmrem) {
      if (frameNo % 30 === 1 || !envRTs[0]) updateEnv();
      const k = w.indexOf(Math.max(...w));
      if (k !== envState && envRTs[k]) { scene.environment = envRTs[k].texture; envState = k; }
    } else if (scene.environment) { scene.environment = null; envState = -1; }
    const envI = mixN("env", w);
    for (const m of ENV_MATS) m.envMapIntensity = envI * (m.userData.envMul || 1);
    hemi.color.copy(mixC("hemiS", w)); hemi.groundColor.copy(mixC("hemiG", w)); hemi.intensity = mixN("hemiI", w) * (scene.environment ? 0.8 : 1);
    renderer.toneMappingExposure = mixN("exp", w) * R.exposureTrim;
    // wet streets wherever rain falls (or per-look override)
    wetAuto += (rainWetness() - wetAuto) * Math.min(1, dt * 1.5);
    const wet = R.LOOK.reduce((a, L, i) => a + (L.wet !== undefined ? L.wet : wetAuto) * w[i], 0);
    DET.glob.value.set(p.detail ? 1 : 0, wet, t, 0);
    const wc = mixC("win", w), wi = mixN("winI", w); DET.win.value.set(wc.r * wi, wc.g * wi, wc.b * wi, mixN("winN", w));
    // post
    ripple = Math.max(0, ripple - dt * 1.6);
    const rp = Math.sin(Math.min(1, ripple) * Math.PI);
    bloom.strength = mixN("bloom", w) + rp * 0.6;
    const gu = grade.uniforms;
    gu.uRipple.value = rp;
    gu.uTime.value = t;
    gu.uTint.value.set(...[0, 1, 2].map(k => R.LOOK.reduce((a, L, i) => a + L.tint[k] * w[i], 0)));
    gu.uSat.value = mixN("sat", w); gu.uCon.value = mixN("con", w);
    gu.uMem.value += (R.memory - gu.uMem.value) * Math.min(1, dt * 2);
    if (trans >= 0) { trans += Math.min(dt, 0.1) / TRANS_DUR; if (trans >= 1) trans = -1; }
    gu.uTrans.value = trans;
    gu.uTransCol.value.set(...STATE_EDGE[state] || STATE_EDGE[1]);
    R.holoUniforms.uTime.value = t;
    R.updateLights(camera.position);
    MAT.window.emissiveIntensity = 0.42 + w[0] * 0.45 + w[1] * 0.2;
  };
  R.render = () => {
    if (!renderer) return;
    const p = R.preset();
    bloom.enabled = p.bloom;
    renderer.info.reset();
    composer.render();
  };

  // ---------------- materials ----------------
  const M = (c, r = 0.6, m = 0, e) => new THREE.MeshStandardMaterial({ color: c, roughness: r, metalness: m, emissive: e || 0x000000 });
  const B = c => new THREE.MeshBasicMaterial({ color: c, fog: true });
  const MAT = (R.MAT = {
    ground: M(0x4a443c, 0.85), stone: M(0xb8a888, 0.8), stoneWet: M(0x2a2f34, 0.24, 0.15), plaster: M(0xcbbba0, 0.92), plasterCold: M(0x6c747c, 0.75),
    brick: M(0x9a5a40, 0.85), wood: M(0x6a4428, 0.65), woodDark: M(0x2e1d12, 0.6), brass: M(0xd8a64c, 0.3, 1), iron: M(0x2a2c30, 0.45, 0.9),
    rust: M(0x5b3420, 0.8, 0.4), copper: M(0x4d8a78, 0.5, 0.7), rail: M(0x9a9ca0, 0.25, 1), concrete: M(0x5a5752, 0.88), tile: M(0x9fb1b4, 0.22),
    red: M(0xb2302a, 0.9), gold: M(0xd9a034, 0.9), teal: M(0x1f7a7a, 0.9), navy: M(0x1e2a48, 0.8), ochre: M(0xa6742a, 0.8), olive: M(0x4e5a2e, 0.8),
    burgundy: M(0x64202a, 0.8), grey: M(0x666670, 0.8), white: M(0xd8d8d0, 0.8), skin: M(0xc99a78, 0.5), skinDark: M(0x6e4a34, 0.5),
    paper: M(0xe8dfc8, 0.9), cup: M(0x1b52d8, 0.15), obsidian: M(0x0b0b10, 0.2, 0.5), ruin: M(0x1d1b22, 0.9), wallpaper: M(0x6f8a7a, 0.9),
    crystal: new THREE.MeshStandardMaterial({ color: 0x6a46d8, roughness: 0.05, metalness: 0.2, emissive: 0x3a1a9a, emissiveIntensity: 0.7 }),
    window: new THREE.MeshStandardMaterial({ color: 0x111111, emissive: 0xffa858, emissiveIntensity: 1.2 }),
    windowCold: new THREE.MeshStandardMaterial({ color: 0x111111, emissive: 0x6aa6e8, emissiveIntensity: 0.5 }),
    windowDark: new THREE.MeshStandardMaterial({ color: 0x07090c, roughness: 0.06, metalness: 0.85 }),
    water: new THREE.MeshStandardMaterial({ color: 0x0e2a33, roughness: 0.05, metalness: 0.6, transparent: true, opacity: 0.82 }),
    glass: new THREE.MeshStandardMaterial({ color: 0x8fb4c8, roughness: 0.05, metalness: 0.1, transparent: true, opacity: 0.2 }),
    puddle: M(0x0a0f14, 0.03, 0.35),
    neonC: B(0x55f0ff), neonM: B(0xff40c8), neonA: B(0xffa040), neonV: B(0x9a60ff), warm: B(0xffd28a), cold: B(0xbfe2ff), red2: B(0xff3020), zero: B(0xf2fbff), green: B(0x60ff90),
  });
  MAT.windowDark.userData.envMul = 1.6; MAT.puddle.userData.envMul = 1.5; MAT.stoneWet.userData.envMul = 1.3;
  const ENV_MATS = Object.keys(MAT).map(k => MAT[k]).filter(m => m.isMeshStandardMaterial);

  // ---- procedural world-space surface detail (injected into MeshStandardMaterial; one shared program) ----
  const DET = { glob: { value: new THREE.Vector4(1, 0, 0, 0) }, win: { value: new THREE.Vector4(1, 0.7, 0.4, 0.3) } };
  const DET_VERT = `
    vec4 azWP = vec4( transformed, 1.0 ); vec3 azON = objectNormal;
    #ifdef USE_INSTANCING
      azWP = instanceMatrix * azWP; azON = mat3( instanceMatrix ) * azON;
    #endif
    azWP = modelMatrix * azWP; vAzPos = azWP.xyz; vAzN = mat3( modelMatrix ) * azON;`;
  const DET_PARS = `uniform sampler2D azNoise; uniform vec4 azGlobal; uniform vec4 azWin; uniform vec4 azMat; uniform vec4 azMat2; uniform vec4 azMat3;
    varying vec3 vAzPos; varying vec3 vAzN;
    ${HASH}
    vec4 azBlocks( vec2 p, vec2 s, float gw, float aa, float off ) {
      vec2 q = p / s; float row = floor( q.y ); q.x += off * ( mod( row, 2.0 ) + azHash( vec2( row, 7.3 ) ) * 0.6 );
      vec2 c = floor( q ); vec2 f = fract( q ); vec2 e = min( f, 1.0 - f ) * s; float ed = min( e.x, e.y );
      return vec4( smoothstep( gw * 0.5, gw * 3.0, ed ), 1.0 - smoothstep( gw - aa, gw + aa, ed ), azHash( c ), 1.0 );
    }
    // x height 0..1, y grout mask, z per-cell random, w has-grid
    vec4 azPattern( vec2 p, float vert, float aa ) {
      float f = azMat.x;
      if ( f < 1.5 ) return azBlocks( p, vert > 0.5 ? vec2( 0.62, 0.34 ) : vec2( 0.6, 0.4 ), 0.013, aa, 0.5 );
      if ( f < 2.5 ) return azBlocks( p, vec2( 0.235, 0.075 ), 0.007, aa, 0.5 );
      if ( f < 3.5 ) { vec4 b = azBlocks( p, vec2( 1.85, 0.16 ), 0.0025, aa, 0.37 ); b.x = 0.4 + 0.6 * b.x; return b; }
      if ( f > 4.5 && f < 5.5 ) { vec4 b = azBlocks( p, vert > 0.5 ? vec2( 1.2, 0.6 ) : vec2( 2.4, 2.4 ), 0.003, aa, 0.0 ); b.x = 0.7 + 0.3 * b.x; b.y *= 0.6; return b; }
      if ( f > 5.5 && f < 6.5 ) return azBlocks( p, vec2( 0.3, 0.3 ), 0.003, aa, 0.0 );
      return vec4( 1.0, 0.0, 0.5, 0.0 );
    }
    float azHeight( vec2 p, float vert, float aa ) { return azPattern( p, vert, aa ).x * azMat.z + texture2D( azNoise, p * azMat2.x ).b * azMat.w; }
`;
  const DET_MAIN = `
    vec3 azNW = normalize( vAzN ); vec3 azAb = abs( azNW );
    vec2 azP = vAzPos.xz; vec3 azT = vec3( 1.0, 0.0, 0.0 ); vec3 azB = vec3( 0.0, 0.0, 1.0 ); float azVert = 0.0;
    if ( azAb.y < max( azAb.x, azAb.z ) ) { azVert = 1.0; azB = vec3( 0.0, 1.0, 0.0 ); if ( azAb.x > azAb.z ) { azP = vAzPos.zy; azT = vec3( 0.0, 0.0, 1.0 ); } else { azP = vAzPos.xy; } }
    float azAA = 0.001, azNS = 0.0, azRough = 1.0, azRoughAdd = 0.0, azPud = 0.0, azRust = 0.0, azWet = 0.0;
    vec3 azEmis = vec3( 0.0 );
    if ( azGlobal.x > 0.5 ) {
      azAA = max( length( fwidth( azP ) ) * 0.7, 1e-4 );
      float fam = azMat.x;
      vec4 pat = azPattern( azP, azVert, azAA );
      float fade = 1.0 - smoothstep( 0.03, 0.12, azAA );
      vec4 n1 = texture2D( azNoise, azP * azMat2.x * 0.31 );
      vec4 n2 = texture2D( azNoise, azP * 0.045 + vec2( 0.37, 0.71 ) );
      vec3 alb = vec3( 1.0 + ( pat.z - 0.5 ) * azMat2.y * fade ) * mix( vec3( 1.0 ), vec3( 1.06, 0.98, 0.9 ), azHash( vec2( pat.z * 91.0, 3.0 ) ) * azMat2.y * fade );
      alb *= mix( 0.78, 1.0, mix( 1.0, pat.x, pat.w * fade ) );
      alb *= ( 1.0 + ( n1.g - 0.5 ) * ( fam > 8.5 && fam < 9.5 ? 0.18 : 0.52 ) ) * mix( 1.0, 0.86 + 0.28 * texture2D( azNoise, azP * 2.3 ).a, fade );
      alb *= 1.0 - azMat2.z * smoothstep( 0.42, 0.85, n2.r );
      if ( azVert > 0.5 ) { float st = texture2D( azNoise, vec2( azP.x * 0.6, azP.y * 0.04 ) + 0.13 ).g; alb *= 1.0 - azMat2.z * 0.9 * smoothstep( 0.55, 0.9, st ); }
      if ( fam > 2.5 && fam < 3.5 ) { float gr = texture2D( azNoise, vec2( azP.x * 0.05, azP.y * 2.6 + pat.z * 7.0 ) ).b; alb *= 0.7 + 0.6 * gr; }
      else if ( fam > 1.5 && fam < 2.5 ) { alb *= mix( vec3( 1.0 ), vec3( 0.72, 0.8, 0.88 ), step( 0.88, pat.z ) ); }
      else if ( fam > 9.5 && fam < 10.5 ) { alb *= 0.9 + 0.14 * step( 0.5, fract( azP.x / 0.18 ) ) * azVert; }
      else if ( fam > 6.5 && fam < 7.5 ) { azRough *= 0.75 + 0.5 * texture2D( azNoise, vec2( azP.x * 0.04, azP.y * 3.0 ) ).a; }
      float g = pat.y * fade;
      diffuseColor.rgb *= alb;
      diffuseColor.rgb = mix( diffuseColor.rgb, azMat3.w > 0.0 ? vec3( azMat3.w ) * vec3( 1.0, 0.95, 0.86 ) : diffuseColor.rgb * 0.42, g );
      if ( azMat3.z > 0.0 ) { azRust = azMat3.z * smoothstep( 0.48, 0.72, n2.g * 0.7 + n1.r * 0.5 ); diffuseColor.rgb = mix( diffuseColor.rgb, vec3( 0.16, 0.055, 0.02 ) * ( 0.7 + 0.6 * n1.b ), azRust ); }
      azRough *= 1.0 + ( n1.b - 0.5 ) * azMat3.x;
      azRoughAdd = g * azMat3.y + azRust * 0.5;
      float horiz = smoothstep( 0.65, 0.9, azNW.y );
      azWet = max( azMat.y, azGlobal.y * azMat2.w * mix( 0.3, 1.0, horiz ) );
      azPud = azMat.y > 0.95 ? horiz : azWet * horiz * smoothstep( 0.6, 0.68, n2.g * 0.8 + n1.r * 0.25 + ( 1.0 - pat.x ) * 0.2 );
      diffuseColor.rgb *= mix( 1.0, 0.72, azWet ) * ( 1.0 - azPud * 0.3 );
      azNS = 1.0 - azPud * 0.9;
      if ( fam > 10.5 && azVert > 0.5 ) {
        vec2 q = azP / vec2( 2.6, 3.4 ); vec2 wc = floor( q ); vec2 wf = fract( q );
        float win = step( 0.24, wf.x ) * step( wf.x, 0.76 ) * step( 0.28, wf.y ) * step( wf.y, 0.8 );
        float hs = azHash( wc * 1.37 + 0.5 );
        diffuseColor.rgb *= 1.0 - win * 0.6;
        azEmis = azWin.rgb * ( win * step( 1.0 - azWin.w, hs ) * ( 0.45 + 0.9 * azHash( wc + 7.1 ) ) );
        azRough *= 1.0 - win * 0.8;
      }
    }`;
  const DET_ROUGH = `
    roughnessFactor = clamp( roughnessFactor * azRough + azRoughAdd, 0.04, 1.0 );
    roughnessFactor = mix( roughnessFactor, max( roughnessFactor * 0.5, 0.3 ), azWet );
    roughnessFactor = mix( roughnessFactor, 0.035, azPud );`;
  const DET_NORMAL = `
    if ( azGlobal.x > 0.5 && azNS > 0.01 && azMat.z + azMat.w > 0.0 ) {
      float azE = max( azAA, 0.004 );
      float azH0 = azHeight( azP, azVert, azAA );
      vec2 azG = vec2( azHeight( azP + vec2( azE, 0.0 ), azVert, azAA ) - azH0, azHeight( azP + vec2( 0.0, azE ), azVert, azAA ) - azH0 ) / azE;
      normal = normalize( normal - azNS * ( mat3( viewMatrix ) * ( azT * azG.x + azB * azG.y ) ) );
    }`;
  const makeOBC = u => function (sh) {
    sh.uniforms.azNoise = NOISE_U; sh.uniforms.azGlobal = DET.glob; sh.uniforms.azWin = DET.win;
    sh.uniforms.azMat = u.azMat; sh.uniforms.azMat2 = u.azMat2; sh.uniforms.azMat3 = u.azMat3;
    sh.vertexShader = "varying vec3 vAzPos;\nvarying vec3 vAzN;\n" + sh.vertexShader.replace("#include <worldpos_vertex>", "#include <worldpos_vertex>\n" + DET_VERT);
    sh.fragmentShader = DET_PARS + sh.fragmentShader
      .replace("#include <color_fragment>", "#include <color_fragment>\n" + DET_MAIN)
      .replace("#include <roughnessmap_fragment>", "#include <roughnessmap_fragment>\n" + DET_ROUGH)
      .replace("#include <metalnessmap_fragment>", "#include <metalnessmap_fragment>\nmetalnessFactor *= 1.0 - azRust;")
      .replace("#include <normal_fragment_maps>", "#include <normal_fragment_maps>\n" + DET_NORMAL)
      .replace("#include <emissivemap_fragment>", "#include <emissivemap_fragment>\ntotalEmissiveRadiance += azEmis;");
  };
  // [fam, wet, ampBlock, ampNoise | noiseFreq, cellVar, stain, rainOK | roughVar, groutRough, rust, mortarLum]
  // fam: 1 stone blocks, 2 brick, 3 planks, 4 plaster, 5 concrete, 6 tile, 7 metal, 8 ground, 9 paint/fabric, 10 wallpaper, 11 tower windows
  const FAMS = {
    ground: [8, 0, 0, 0.005, 0.8, 0, 0.35, 1, 0.5, 0, 0, 0],
    stone: [1, 0, 0.006, 0.003, 0.7, 0.5, 0.25, 1, 0.4, 0.15, 0, 0],
    stoneWet: [1, 0.8, 0.005, 0.0025, 0.7, 0.25, 0.15, 1, 0.3, 0.05, 0, 0],
    puddle: [8, 1, 0, 0.0006, 1.2, 0, 0, 0, 0.1, 0, 0, 0],
    plaster: [4, 0, 0, 0.004, 1.4, 0, 0.32, 1, 0.15, 0, 0, 0],
    plasterCold: [4, 0, 0, 0.004, 1.4, 0, 0.32, 1, 0.2, 0, 0, 0],
    brick: [2, 0, 0.004, 0.002, 1.6, 0.45, 0.22, 1, 0.25, 0.08, 0, 0.13],
    wood: [3, 0, 0.0018, 0.0008, 0.9, 0.38, 0.12, 1, 0.35, 0.15, 0, 0],
    woodDark: [3, 0, 0.0018, 0.0008, 0.9, 0.3, 0.12, 1, 0.35, 0.15, 0, 0],
    concrete: [5, 0, 0.002, 0.004, 0.8, 0.1, 0.38, 1, 0.3, 0.05, 0, 0],
    ruin: [5, 0, 0.002, 0.005, 0.8, 0.12, 0.42, 1, 0.3, 0.05, 0, 0],
    tile: [6, 0, 0.002, 0.0005, 1.0, 0.1, 0.08, 0, 0.25, 0.55, 0, 0.32],
    iron: [7, 0, 0, 0.001, 1.2, 0, 0.25, 1, 0.6, 0, 0.25, 0],
    rust: [7, 0, 0, 0.002, 1.2, 0, 0.3, 1, 0.4, 0, 0.75, 0],
    brass: [7, 0, 0, 0.0008, 1.2, 0, 0.3, 0, 0.5, 0, 0, 0],
    copper: [7, 0, 0, 0.001, 1.2, 0, 0.35, 0, 0.5, 0, 0.15, 0],
    rail: [7, 0, 0, 0.0006, 1.2, 0, 0.15, 1, 0.5, 0, 0, 0],
    obsidian: [9, 0, 0, 0.0012, 1.0, 0, 0.15, 1, 0.5, 0, 0, 0],
    wallpaper: [10, 0, 0, 0.001, 1.5, 0, 0.3, 0, 0.15, 0, 0, 0],
  };
  ["red", "gold", "teal", "navy", "ochre", "olive", "burgundy", "grey", "white", "paper"].forEach(k => { FAMS[k] = [9, 0, 0, 0.0012, 2.2, 0, 0.14, 0, 0.3, 0, 0, 0]; });
  const detail = (mat, prm) => {
    const u = { azMat: { value: new THREE.Vector4(prm[0], prm[1], prm[2], prm[3]) }, azMat2: { value: new THREE.Vector4(prm[4], prm[5], prm[6], prm[7]) }, azMat3: { value: new THREE.Vector4(prm[8], prm[9], prm[10], prm[11]) } };
    mat.azU = u;
    mat.extensions = { derivatives: true };
    mat.onBeforeCompile = makeOBC(u);
    return mat;
  };
  Object.keys(FAMS).forEach(k => { MAT[k].userData.azKey = k; detail(MAT[k], FAMS[k]); });
  // Distant towers: same family as their base material + procedural lit-window grid (no extra geometry).
  const TOWERS = {};
  R.towerMat = key => {
    const base = R.mat(key);
    if (TOWERS[base.uuid]) return TOWERS[base.uuid];
    if (!base.isMeshStandardMaterial) return base;
    const m = base.clone();
    const prm = (FAMS[base.userData.azKey] || FAMS.concrete).slice(); prm[0] = 11; prm[1] = 0;
    detail(m, prm);
    ENV_MATS.push(m);
    return (TOWERS[base.uuid] = m);
  };

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

  // ---------------- cheap light FX (lamp halo, volumetric-ish cone, ground light pool) ----------------
  // Additive, no real lights; merged per colour by R.mergeStatic (they keep their uv attribute), faded by fog.
  const FX_GEO = { cone: new THREE.CylinderGeometry(0.07, 1, 1, 18, 1, true), halo: new THREE.SphereGeometry(0.5, 12, 8), pool: new THREE.PlaneGeometry(1, 1) };
  const FX_VERT = `varying vec3 vN; varying vec3 vV; varying vec2 vUv;
    #include <fog_pars_vertex>
    void main(){ vUv = uv; vec4 mvPosition = modelViewMatrix * vec4( position, 1.0 ); vN = normalize( normalMatrix * normal ); vV = -mvPosition.xyz; gl_Position = projectionMatrix * mvPosition;
    #include <fog_vertex>
    }`;
  const FX_FRAG = `uniform vec3 uColor; uniform float uI; uniform float uKind; varying vec3 vN; varying vec3 vV; varying vec2 vUv;
    #include <fog_pars_fragment>
    void main(){
      float a; float f = abs( dot( normalize( vN ), normalize( vV ) ) );
      if ( uKind < 0.5 ) a = pow( f, 2.5 );
      else if ( uKind < 1.5 ) a = pow( f, 1.6 ) * pow( vUv.y, 1.8 ) * smoothstep( 1.0, 0.92, vUv.y );
      else { float r = length( vUv - 0.5 ) * 2.0; a = pow( max( 1.0 - r, 0.0 ), 2.0 ); }
      gl_FragColor = vec4( uColor * ( a * uI ), 1.0 );
      #if defined( USE_FOG ) && defined( FOG_EXP2 )
        float azFD = length( vAzFogDir ); gl_FragColor.rgb *= exp( - fogDensity * fogDensity * azFD * azFD );
      #endif
    }`;
  const FXC = {};
  const fxMat = (hex, kind) => {
    const k = hex + "|" + kind;
    if (FXC[k]) return FXC[k];
    const m = new THREE.ShaderMaterial({
      uniforms: THREE.UniformsUtils.merge([THREE.UniformsLib.fog, { uColor: { value: new THREE.Color(hex) }, uI: { value: [1.4, 0.2, 0.4][kind] }, uKind: { value: kind } }]),
      vertexShader: FX_VERT, fragmentShader: FX_FRAG, transparent: true, depthWrite: false, blending: THREE.AdditiveBlending, fog: true,
      side: kind === 1 ? THREE.DoubleSide : THREE.FrontSide,
    });
    if (kind === 2) { m.polygonOffset = true; m.polygonOffsetFactor = -2; m.polygonOffsetUnits = -4; }
    m.userData.azFX = true;
    return (FXC[k] = m);
  };
  /** Lamp glow: halo at (x, y+h, z), light cone down to the ground and a light pool on the ground at y. */
  R.lampFX = (parent, x, y, z, h, color, opts = {}) => {
    const add = (geo, kind, px, py, pz, sx, sy, sz, rx) => {
      const m = new THREE.Mesh(geo, fxMat(color, kind));
      m.position.set(px, py, pz); m.scale.set(sx, sy, sz); if (rx) m.rotation.x = rx;
      m.castShadow = m.receiveShadow = false; m.userData.noEcho = true;
      parent.add(m);
      return m;
    };
    const s = opts.size || 1;
    add(FX_GEO.halo, 0, x, y + h, z, 1.5 * s, 1.5 * s, 1.5 * s);
    if (opts.cone !== false) { const ch = h - 0.2, r = (opts.spread || 0.42) * h; add(FX_GEO.cone, 1, x, y + ch / 2, z, r, ch, r); }
    const pr = (opts.pool || 1.0) * Math.max(3, h * 1.05);
    add(FX_GEO.pool, 2, x, y + 0.03, z, pr * 2, pr * 2, 1, -Math.PI / 2);
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
    rains.push(l);
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
  // Light-FX meshes (material.userData.azFX) merge too and keep their uv attribute.
  R.mergeStatic = group => {
    const BGU = THREE.BufferGeometryUtils;
    if (!BGU) return;
    group.updateMatrixWorld(true);
    const buckets = new Map();
    const victims = [];
    group.traverse(o => {
      if (!o.isMesh || o.isSkinnedMesh || o.isInstancedMesh || Array.isArray(o.material) || o.userData.noMerge || o.userData.dynamic || o.userData.interactive) return;
      const fx = !!(o.material.userData && o.material.userData.azFX);
      if (!fx && (o.material.transparent || o.material.isShaderMaterial)) return;
      let p = o.parent, skip = false;
      while (p && p !== group) { if (p.userData.dynamic || p.userData.noMerge) { skip = true; break; } p = p.parent; }
      if (skip) return;
      const key = o.material.uuid + (o.castShadow ? "s" : "");
      if (!buckets.has(key)) buckets.set(key, { mat: o.material, geos: [], fx });
      const g = o.geometry.index ? o.geometry.toNonIndexed() : o.geometry.clone();
      const keep = {};
      (fx ? ["position", "normal", "uv"] : ["position", "normal"]).forEach(a => { if (g.attributes[a]) keep[a] = g.attributes[a]; });
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
        m.castShadow = m.receiveShadow = !b.fx;
        m.userData.merged = true;
        if (b.fx) m.userData.noEcho = true;
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
