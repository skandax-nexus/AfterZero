/* AFTERZERO — baked props (tools/props/build_train.py): glTF geometry + a Cycles-baked lightmap + tiling textures.
   AZ.props.load() fetches props/train_carriage.glb and its textures (or the copies embedded by tools/build_preview.py
   in window.AZ_PROPS). AZ.props.train(L, parent) builds the Prologue carriage and returns its controller.

   Baked materials: diffuse light comes only from the lightmap (direct + indirect, shadows, colour bleeding), so the
   interior looks identical on every quality tier for one texture fetch; the game's pooled lights and IBL still add
   specular (varnish, brass), and two analytic terms add moving light through the windows (passing lamps, lightning).
   Window glass: procedural rain (beads, running drops pushed back by the train's speed, condensation).
   Outside: one proxy plane per side whose fragment shader ray-casts virtual layers (poles + wires at 3 m, tree line and
   cottages at 38 m, hills with village lights at 400 m, ridge at 1.5 km, ground, clouds) — exact parallax, one draw. */
(function () {
  "use strict";
  const AZ = window.AZ, R = AZ.render;
  const P = (AZ.props = { ready: false, gltf: null, tex: {}, meta: null, gain: 2.9 });
  const DIR = "props/";
  P.FILES = ["train_carriage.glb", "train_lightmap.jpg", "train_wood.jpg", "train_moquette.jpg", "train_velvet.jpg", "train_carpet.jpg", "train_ceiling.jpg", "train_decals.jpg"];
  const emb = f => window.AZ_PROPS && window.AZ_PROPS[f];
  const src = f => { const e = emb(f); return !e ? DIR + f : /^data:/.test(e) ? e : "data:" + (f.endsWith(".glb") ? "model/gltf-binary" : "image/jpeg") + ";base64," + e; };

  // ---------------- loading ----------------
  P.load = async () => {
    if (P.ready || P.loading) return P.loading;
    P.loading = (async () => {
      try {
        const loader = new THREE.GLTFLoader();
        if (window.MeshoptDecoder) { if (MeshoptDecoder.ready) await MeshoptDecoder.ready; loader.setMeshoptDecoder(MeshoptDecoder); }
        const glb = await new Promise((ok, bad) => {
          const e = emb("train_carriage.glb");
          if (e && !/^data:/.test(e)) { const bin = atob(e), buf = new Uint8Array(bin.length); for (let i = 0; i < bin.length; i++) buf[i] = bin.charCodeAt(i); loader.parse(buf.buffer, "", ok, bad); }
          else loader.load(src("train_carriage.glb"), ok, null, bad);
        });
        const tl = new THREE.TextureLoader();
        const maxAn = R.renderer ? R.renderer.capabilities.getMaxAnisotropy() : 1;
        const loadTex = (f, lm) => new Promise(ok => tl.load(src(f), t => {
          t.flipY = false; t.encoding = THREE.sRGBEncoding;
          if (lm) { t.generateMipmaps = false; t.minFilter = THREE.LinearFilter; t.magFilter = THREE.LinearFilter; }
          else { t.wrapS = t.wrapT = THREE.RepeatWrapping; t.anisotropy = Math.min(4, maxAn); t.repeat.set(64, 64); t.offset.set(-32, -32); }
          t.needsUpdate = true; ok(t);
        }, undefined, () => ok(null)));
        const names = ["wood", "moquette", "velvet", "carpet", "ceiling", "decals"];
        const [lm, ...rest] = await Promise.all([loadTex("train_lightmap.jpg", true), ...names.map(n => loadTex("train_" + n + ".jpg"))]);
        names.forEach((n, i) => { P.tex[n] = rest[i]; });
        P.tex.lightmap = lm;
        P.gltf = glb;
        P.meta = glb.scene.userData || {};
        P.ready = !!(lm && P.meta.layout);
      } catch (e) { console.warn("Train props unavailable, using the fallback carriage.", e && e.message); }
    })();
    return P.loading;
  };

  // ---------------- shared uniforms (one carriage at a time) ----------------
  const U = {
    flick: { value: 1 }, sw: { value: [new THREE.Vector4(), new THREE.Vector4()] }, swC: { value: [new THREE.Color(1, 0.5, 0.18), new THREE.Color(1, 0.5, 0.18)] },
    flash: { value: new THREE.Vector4(0, 1, 0, 0) }, sway: { value: new THREE.Vector2() }, origin: { value: new THREE.Vector3() },
    time: { value: 0 }, speed: { value: 0 }, dist: { value: 0 }, clouds: { value: 0 }, fade: { value: 1 },
    haze: { value: new THREE.Color(0.075, 0.09, 0.118) }, zen: { value: new THREE.Color(0.014, 0.019, 0.032) }, glow: { value: new THREE.Color(0.24, 0.12, 0.05) },
    maskL: { value: 0 }, maskR: { value: 0 }, outL: { value: new THREE.Color() }, outR: { value: new THREE.Color() }, warm: { value: new THREE.Color(1.0, 0.62, 0.32) },
  };
  P.U = U;
  const HASH = "float azH21( vec2 p ) { vec3 p3 = fract( vec3( p.xyx ) * 0.1031 ); p3 += dot( p3, p3.yzx + 33.33 ); return fract( ( p3.x + p3.y ) * p3.z ); }\n";

  // Window visibility of a point q (outside) from p (inside): the segment p->q must cross a side wall inside a window.
  const WINFN = `
    uniform vec4 azSw[ 2 ]; uniform vec3 azSwC[ 2 ]; uniform vec4 azFlash; uniform float azFlick; varying vec3 vAzW;
    float azWin( vec3 p, vec3 q ) {
      float xw = sign( q.x ) * 1.69;
      float t = ( xw - p.x ) / ( q.x - p.x );
      if ( t <= 0.0 || t >= 1.0 ) return 0.0;
      vec3 h = p + ( q - p ) * t;
      float vy = smoothstep( 1.0, 1.05, h.y ) * smoothstep( 1.92, 1.87, h.y ) * ( 1.0 - 0.85 * smoothstep( 0.03, 0.0, abs( h.y - 1.635 ) ) );
      float m = abs( mod( h.z + 1.0, 2.0 ) - 1.0 );
      return vy * smoothstep( 0.48, 0.42, m ) * step( -9.0, h.z ) * step( h.z, 9.0 );
    }
    vec3 azOutside( vec3 p, vec3 n ) {
      vec3 e = vec3( 0.0 );
      for ( int i = 0; i < 2; i ++ ) {
        if ( azSw[ i ].w <= 0.0 ) continue;
        vec3 L = azSw[ i ].xyz - p; float d2 = dot( L, L );
        e += azSwC[ i ] * ( azSw[ i ].w * max( dot( n, L * inversesqrt( d2 ) ), 0.0 ) / ( d2 + 0.6 ) ) * azWin( p, azSw[ i ].xyz );
      }
      if ( azFlash.w > 0.001 ) e += vec3( 0.55, 0.66, 1.0 ) * azFlash.w * max( dot( n, azFlash.xyz ), 0.0 ) * azWin( p, p + azFlash.xyz * 40.0 );
      return e;
    }
`;

  // ---------------- materials ----------------
  const MATS = {};
  const sway = (sh, on) => {
    if (!on) return;
    sh.uniforms.azSway = U.sway; sh.uniforms.azO = U.origin;
    sh.vertexShader = "uniform vec2 azSway; uniform vec3 azO;\n" + sh.vertexShader.replace("#include <project_vertex>", `
      vec4 azP = modelMatrix * vec4( transformed, 1.0 );
      vec3 azPiv = vec3( 0.0, 2.845, 2.0 * floor( ( azP.z - azO.z ) * 0.5 + 0.5 ) ) + azO;
      vec3 azR = azP.xyz - azPiv; float azC = cos( azSway.x ), azS = sin( azSway.x );
      azR.xy = vec2( azR.x * azC - azR.y * azS, azR.x * azS + azR.y * azC );
      azC = cos( azSway.y ); azS = sin( azSway.y );
      azR.zy = vec2( azR.z * azC - azR.y * azS, azR.z * azS + azR.y * azC );
      azP.xyz = azPiv + azR;
      vec4 mvPosition = viewMatrix * azP;
      gl_Position = projectionMatrix * mvPosition;`);
  };
  function bakedMat(def, swaying) {
    const ex = def.extras || {}, pbr = def.pbrMetallicRoughness || {};
    const f = pbr.baseColorFactor || [1, 1, 1, 1];
    const m = new THREE.MeshStandardMaterial({ color: new THREE.Color(f[0], f[1], f[2]), roughness: pbr.roughnessFactor !== undefined ? pbr.roughnessFactor : 0.6, metalness: pbr.metallicFactor || 0 });
    if (ex.tex && P.tex[ex.tex]) m.map = P.tex[ex.tex];
    m.lightMap = P.tex.lightmap; m.lightMapIntensity = ((P.meta.lightmap && P.meta.lightmap.scale) || 1) * P.gain;
    if (def.emissiveFactor) { m.emissive = new THREE.Color().fromArray(def.emissiveFactor); }
    m.envMapIntensity = 0.5;
    m.name = def.name;
    m.onBeforeCompile = sh => {
      Object.assign(sh.uniforms, { azSw: U.sw, azSwC: U.swC, azFlash: U.flash, azFlick: U.flick });
      sway(sh, swaying);
      sh.vertexShader = "varying vec3 vAzW;\n" + sh.vertexShader.replace("#include <worldpos_vertex>", "#include <worldpos_vertex>\nvAzW = ( modelMatrix * vec4( transformed, 1.0 ) ).xyz;");
      sh.fragmentShader = WINFN + sh.fragmentShader.replace("#include <lights_fragment_end>", `#include <lights_fragment_end>
        vec3 azN = inverseTransformDirection( normal, viewMatrix );
        vec3 azBk = lightMapTexelToLinear( texture2D( lightMap, vUv2 ) ).rgb * lightMapIntensity * azFlick;
        vec3 azEx = azOutside( vAzW, azN );
        reflectedLight.directDiffuse = vec3( 0.0 );
        reflectedLight.indirectDiffuse = ( azBk + azEx ) * material.diffuseColor;
        float azL = dot( azBk, vec3( 0.3, 0.59, 0.11 ) );
        reflectedLight.indirectSpecular = reflectedLight.indirectSpecular * clamp( azL * 2.5, 0.08, 0.7 ) + ( azBk + azEx ) * material.specularColor * ( 0.35 + 0.65 * ( 1.0 - material.specularRoughness ) );
        reflectedLight.directSpecular *= 0.6;`);
    };
    m.customProgramCacheKey = () => "azBaked" + (swaying ? "S" : "") + (m.map ? "M" : "");
    return m;
  }
  function lampMat(swaying) {
    // opal glass: brightest where the bulb sits (the bottom 13 cm of a pendant bowl / sconce tulip), cooler at the rim
    const m = new THREE.MeshBasicMaterial({ color: new THREE.Color(1.0, 0.72, 0.42).multiplyScalar(2.1), side: THREE.DoubleSide });
    m.userData.base = m.color.clone();
    m.onBeforeCompile = sh => {
      sway(sh, swaying);
      sh.vertexShader = "varying vec3 vAzLw;\n" + sh.vertexShader.replace("#include <fog_vertex>", "#include <fog_vertex>\nvAzLw = ( modelMatrix * vec4( transformed, 1.0 ) ).xyz;");
      sh.fragmentShader = "varying vec3 vAzLw;\n" + sh.fragmentShader.replace("#include <color_fragment>", `#include <color_fragment>
        float azLk = smoothstep( 0.14, 0.0, mod( vAzLw.y - 1.70, 0.83 ) );
        diffuseColor.rgb *= 0.72 + 0.55 * azLk;
        diffuseColor.b *= 0.85 + 0.15 * azLk;`);
    };
    m.customProgramCacheKey = () => "azLamp" + (swaying ? "S" : "");
    return m;
  }
  function litMat(def) {
    const pbr = def.pbrMetallicRoughness || {}, f = pbr.baseColorFactor || [1, 1, 1, 1];
    return new THREE.MeshStandardMaterial({ color: new THREE.Color(f[0], f[1], f[2]), roughness: pbr.roughnessFactor, metalness: pbr.metallicFactor || 0, envMapIntensity: 0.8 });
  }

  // ---------------- rain on glass ----------------
  function glassMat() {
    return new THREE.ShaderMaterial({
      uniforms: { uT: U.time, uV: U.speed, uOutL: U.outL, uOutR: U.outR, uWarm: U.warm, uFlick: U.flick, uFlash: U.flash },
      vertexShader: "varying vec3 vW; varying vec3 vN; void main(){ vec4 w = modelMatrix * vec4( position, 1.0 ); vW = w.xyz; vN = normalize( mat3( modelMatrix ) * normal ); gl_Position = projectionMatrix * viewMatrix * w; }",
      fragmentShader: `uniform float uT; uniform float uV; uniform vec3 uOutL; uniform vec3 uOutR; uniform vec3 uWarm; uniform float uFlick; uniform vec4 uFlash;
        varying vec3 vW; varying vec3 vN;
        ${HASH}
        vec3 beads( vec2 uv, float sc, float t, float seed ) {
          vec2 g = uv * sc; vec2 id = floor( g ); vec2 f = fract( g ) - 0.5;
          vec2 o = ( vec2( azH21( id + seed ), azH21( id + seed + 17.3 ) ) - 0.5 ) * 0.6;
          float life = fract( t * ( 0.025 + 0.05 * azH21( id + 5.3 + seed ) ) + azH21( id + 9.1 ) );
          float r = mix( 0.10, 0.32, pow( azH21( id + seed * 3.1 + 1.0 ), 3.0 ) ) * smoothstep( 0.0, 0.12, life ) * smoothstep( 1.0, 0.82, life );
          vec2 d = ( f - o ) / max( r, 1e-3 ); d.y *= 1.12;
          float aa = smoothstep( 0.45, 0.18, length( fwidth( g ) ) );
          float m = smoothstep( 1.0, 0.72, length( d ) ) * step( 0.25, azH21( id + seed + 3.3 ) ) * aa;
          return vec3( m, d * m );
        }
        vec3 runner( vec2 uv, float t, vec2 dir, float sc, float seed ) {
          vec2 perp = vec2( -dir.y, dir.x );
          vec2 q = vec2( dot( uv, perp ), dot( uv, -dir ) ) * sc;
          float col = floor( q.x ); float fx = fract( q.x ) - 0.5;
          float hs = azH21( vec2( col, seed ) );
          float L = 4.0 + 4.0 * hs;
          float ph = ( q.y + t * sc * ( 0.05 + 0.10 * hs + uV * 0.10 ) ) / L + hs * 7.0;
          float cid = floor( ph ); float fy = fract( ph );
          float on = step( 0.55, azH21( vec2( col, cid ) + seed ) );
          float dx = fx - sin( ph * 9.0 + col ) * 0.12 - ( azH21( vec2( cid, col ) ) - 0.5 ) * 0.3;
          vec2 hd = vec2( dx, ( fy - 0.1 ) * L ) / 0.28;
          float head = smoothstep( 1.0, 0.7, length( hd ) );
          float trail = smoothstep( 0.09, 0.03, abs( dx ) ) * smoothstep( 0.1, 0.16, fy ) * smoothstep( 0.9, 0.25, fy );
          float spk = smoothstep( 0.35, 0.2, length( vec2( dx, fract( fy * L * 1.5 ) - 0.5 ) ) ) * trail;
          float aa = smoothstep( 0.5, 0.2, length( fwidth( q ) ) );
          float m = max( head, spk * 0.8 ) * on * aa;
          return vec3( m, hd * head * on * aa );
        }
        void main() {
          vec2 uv = vec2( vW.z, vW.y );
          vec2 dir = normalize( vec2( -uV * 0.12, -1.0 ) );
          vec3 a = beads( uv, 70.0, uT, 0.0 ), b = beads( uv, 30.0, uT * 0.8, 4.0 ), c = runner( uv, uT, dir, 26.0, 1.0 ), d = runner( uv + 0.37, uT * 1.1, dir, 41.0, 7.0 );
          float m = max( max( a.x, b.x ), max( c.x, d.x ) );
          vec2 n = a.yz + b.yz + c.yz + d.yz;
          vec3 V = normalize( cameraPosition - vW );
          float fres = pow( 1.0 - abs( dot( V, normalize( vN ) ) ), 4.0 );
          vec3 outc = vW.x > 0.0 ? uOutR : uOutL;
          outc += vec3( 0.5, 0.6, 0.8 ) * uFlash.w * 0.6;
          vec3 hl = normalize( vec3( n * 0.9, 0.55 ) );
          float spec = pow( clamp( dot( hl, normalize( vec3( 0.35, 0.75, 0.6 ) ) ), 0.0, 1.0 ), 18.0 );
          vec3 drop = outc * ( 0.7 + 1.8 * clamp( 0.45 - n.y * 0.6, 0.0, 1.4 ) ) + uWarm * uFlick * ( spec * 0.9 + 0.03 );
          drop *= 1.0 - 0.55 * smoothstep( 0.55, 1.0, length( n ) );
          float mist = smoothstep( 1.30, 1.0, vW.y ) * 0.18 + smoothstep( 1.80, 1.92, vW.y ) * 0.08;
          vec3 base = uWarm * uFlick * 0.035 + outc * 0.15;
          vec3 col = mix( base, drop, m );
          float alpha = mix( 0.04 + fres * 0.4 + mist, 0.62, m );
          gl_FragColor = vec4( col, alpha );
          #include <tonemapping_fragment>
          #include <encodings_fragment>
        }`,
      transparent: true, depthWrite: false, side: THREE.DoubleSide, extensions: { derivatives: true },
    });
  }

  // ---------------- night landscape (ray-cast layers on a proxy plane) ----------------
  function sceneryMat() {
    return new THREE.ShaderMaterial({
      uniforms: { uNoise: { value: R.noiseTexture }, uS: U.dist, uV: U.speed, uT: U.time, uCl: U.clouds, uFade: U.fade, uFlash: U.flash,
        uHaze: U.haze, uZen: U.zen, uGlow: U.glow, uMaskL: U.maskL, uMaskR: U.maskR },
      vertexShader: "varying vec3 vW; void main(){ vec4 w = modelMatrix * vec4( position, 1.0 ); vW = w.xyz; gl_Position = projectionMatrix * viewMatrix * w; }",
      fragmentShader: `uniform sampler2D uNoise; uniform float uS; uniform float uV; uniform float uT; uniform float uCl; uniform float uFade; uniform vec4 uFlash;
        uniform vec3 uHaze; uniform vec3 uZen; uniform vec3 uGlow; uniform float uMaskL; uniform float uMaskR;
        varying vec3 vW;
        float h1( float n ) { return fract( sin( n * 127.1 + 3.7 ) * 43758.5453 ); }
        float nz( float z, float per, float row ) { return texture2D( uNoise, vec2( z / per, row ) ).r; }
        float bit( float mask, float k ) { return mod( floor( mask / exp2( k ) ), 2.0 ); }
        vec3 sky( vec3 rd ) {
          float el = rd.y;
          vec3 c = mix( uHaze, uZen, smoothstep( -0.02, 0.5, el ) );
          float ahead = smoothstep( -0.4, 1.0, rd.z );
          c += uGlow * exp( -max( el, 0.0 ) * 8.0 ) * ( 0.25 + 1.1 * ahead * ahead );
          vec2 cuv = rd.xz / ( max( el, 0.0 ) + 0.12 ) * 0.045 + vec2( 0.13, uCl );
          float cl = texture2D( uNoise, cuv ).r * 0.62 + texture2D( uNoise, cuv * 2.9 + 0.31 ).g * 0.38;
          float cov = smoothstep( 0.38, 0.72, cl ) * smoothstep( -0.04, 0.10, el );
          vec3 cc = uHaze * 1.35 + uGlow * 0.9 * exp( -max( el, 0.0 ) * 3.0 ) * ( 0.4 + ahead );
          c = mix( c, cc, cov * 0.85 );
          c += vec3( 0.5, 0.56, 0.75 ) * uFlash.w * ( 0.2 + cov * 1.4 ) * smoothstep( -0.1, 0.25, el );
          return c;
        }
        void main() {
          if ( uFade < 0.999 ) { float dn = texture2D( uNoise, vW.zy * 0.031 ).r * 0.7 + texture2D( uNoise, vW.zy * 0.17 ).g * 0.3; if ( dn > uFade * 1.05 - 0.02 ) discard; }
          vec3 ro = cameraPosition; vec3 rd = normalize( vW - ro );
          float side = vW.x > 0.0 ? 1.0 : -1.0;
          float ax = max( abs( rd.x ), 1e-4 );
          vec3 col = sky( rd );
          float tG = rd.y < -1e-4 ? ( -1.15 - ro.y ) / rd.y : 1e7;
          if ( tG < 1e6 ) {
            vec3 pg = ro + rd * tG; float zg = pg.z + uS; float gx = abs( pg.x );
            float bk = 1.0 + uV * 0.45;
            float st = texture2D( uNoise, vec2( pg.x * 0.9, zg * 0.35 / bk ) ).g * 0.6 + texture2D( uNoise, vec2( pg.x * 3.1, zg * 1.4 / bk ) ).a * 0.4;
            vec3 cg = mix( vec3( 0.020, 0.018, 0.016 ), vec3( 0.006, 0.009, 0.006 ), smoothstep( 2.4, 3.2, gx ) );
            cg *= 0.6 + 0.8 * st;
            float pud = smoothstep( 0.58, 0.66, texture2D( uNoise, vec2( pg.x * 0.11, zg * 0.12 / bk ) ).r ) * smoothstep( 2.8, 3.5, gx );
            cg = mix( cg, sky( reflect( rd, vec3( 0.0, 1.0, 0.0 ) ) ) * 0.55, pud );
            col = mix( cg, uHaze, 1.0 - exp( -tG / 260.0 ) );
          }
          // ridge (1.5 km) and hills with village lights (400 m)
          float tR = ( 1500.0 - side * ro.x ) / ax; vec3 pr = ro + rd * tR; float zr = pr.z + uS;
          float hr = 50.0 + 70.0 * nz( zr, 3600.0, 0.62 ) + 25.0 * nz( zr, 900.0, 0.18 );
          if ( tR < tG ) col = mix( col, mix( vec3( 0.010, 0.013, 0.018 ), uHaze, 0.72 ), smoothstep( 0.0, max( fwidth( pr.y ), 0.5 ), hr - ( pr.y + 1.15 ) ) );
          float tH = ( 400.0 - side * ro.x ) / ax; vec3 ph = ro + rd * tH; float zh = ph.z + uS; float yh = ph.y + 1.15;
          float hh = 6.0 + 26.0 * nz( zh, 900.0, 0.21 ) + 10.0 * nz( zh, 180.0, 0.47 ) + 3.0 * nz( zh, 45.0, 0.83 );
          if ( tH < tG ) {
            vec3 ch = vec3( 0.006, 0.008, 0.011 );
            vec2 lc = vec2( zh / 6.0, yh / 3.0 ); vec2 li = floor( lc ); vec2 lf = ( fract( lc ) - 0.5 ) * vec2( 6.0, 3.0 );
            float lh = fract( sin( dot( mod( li, vec2( 600.0, 64.0 ) ), vec2( 12.9898, 78.233 ) ) ) * 43758.5453 );
            float lpx = max( fwidth( zh ), 0.08 );
            float ld = step( 0.955, lh ) * step( yh, hh - 1.5 ) * step( 0.5, yh ) * smoothstep( lpx * 1.8, lpx * 0.5, length( lf ) );
            ch += vec3( 1.0, 0.6, 0.28 ) * ld * ( 2.2 + 0.4 * sin( uT * 2.0 + lh * 60.0 ) );
            ch = mix( ch, uHaze, 1.0 - exp( -tH / 700.0 ) );
            col = mix( col, ch, smoothstep( 0.0, max( fwidth( yh ), 0.05 ), hh - yh ) );
          }
          // tree line, hedges, cottages (38 m)
          float tM = ( 38.0 - side * ro.x ) / ax; vec3 pm = ro + rd * tM; float zm = pm.z + uS; float ym = pm.y + 1.15;
          if ( tM < tG ) {
            float field = smoothstep( 0.44, 0.52, nz( zm, 300.0, 0.91 ) );
            float th = ( 3.5 + 5.0 * nz( zm, 120.0, 0.13 ) + 2.0 * nz( zm, 20.0, 0.57 ) ) * mix( 0.72, 1.0, sqrt( abs( sin( zm * 3.14159265 / 9.0 ) ) ) );
            th = mix( th, 0.7 + 0.5 * nz( zm, 15.0, 0.33 ), field );
            float ci = floor( zm / 120.0 ); float cf = zm - ci * 120.0 - 60.0; float chs = h1( mod( ci, 30.0 ) + side * 31.0 );
            float house = step( 0.5, chs ) * step( abs( cf ), 5.0 );
            float sil = max( th, house * ( 5.2 + ( 5.0 - abs( cf ) ) * 0.5 ) );
            vec3 cm = vec3( 0.0035, 0.0045, 0.0055 );
            float win = house * step( abs( abs( cf ) - 2.3 ), 0.5 ) * step( 1.5, ym ) * step( ym, 2.6 ) * step( 0.25, h1( ci * 3.1 + side ) );
            cm += vec3( 1.0, 0.58, 0.26 ) * 1.7 * win;
            float lp = step( 0.62, h1( mod( ci, 30.0 ) * 1.7 + side * 5.0 ) ) * ( 1.0 - house );
            vec2 lq = vec2( cf - 12.0, ym - 4.2 );
            cm = mix( cm, uHaze * 0.6, 1.0 - exp( -tM / 400.0 ) );
            col = mix( col, cm, smoothstep( 0.0, max( fwidth( ym ), 0.02 ) * 1.5, sil - ym ) );
            col += vec3( 1.0, 0.52, 0.18 ) * lp * ( smoothstep( 0.18, 0.0, length( lq ) ) * 3.0 + exp( -dot( lq, lq ) * 0.25 ) * 0.12 );
            col = mix( col, vec3( 0.004 ), lp * smoothstep( 0.07, 0.03, abs( lq.x ) ) * step( ym, 4.1 ) );
          }
          // poles, wires and sodium lamps beside the track (3.3 m)
          float tN = ( 3.3 - side * ro.x ) / ax; vec3 pn = ro + rd * tN; float zn = pn.z + uS; float yn = pn.y + 1.15;
          if ( tN < tG ) {
            float bl = uV * 0.011 + fwidth( zn ) * 0.6;
            float pz = mod( zn, 45.0 ) - 22.5;
            float pole = smoothstep( 0.11 + bl, max( 0.11 - bl, 0.0 ), abs( pz ) ) * ( 0.11 / ( 0.11 + bl ) ) * step( yn, 6.6 );
            float arm = smoothstep( 0.7 + bl, 0.7 - bl, abs( pz ) ) * smoothstep( 0.08, 0.05, abs( yn - 6.2 ) ) * ( 0.7 / ( 0.7 + bl ) );
            float u = pz / 22.5; float sag = 0.55 * ( 1.0 - u * u );
            float wpx = max( fwidth( yn ), 0.004 );
            float wire = ( smoothstep( wpx * 1.4, 0.0, abs( yn - 6.15 + sag ) ) + smoothstep( wpx * 1.4, 0.0, abs( yn - 5.85 + sag ) ) ) * 0.7;
            float li = floor( zn / 180.0 ); float lz = zn - li * 180.0 - 90.0;
            float on = bit( side > 0.0 ? uMaskR : uMaskL, mod( li, 20.0 ) );
            float lpole = on * smoothstep( 0.07 + bl, max( 0.07 - bl, 0.0 ), abs( lz ) ) * step( yn, 4.55 ) * ( 0.07 / ( 0.07 + bl ) );
            float a = clamp( max( max( pole, arm ), max( wire, lpole ) ), 0.0, 1.0 );
            col = mix( col, vec3( 0.010, 0.010, 0.012 ), a );
            vec2 hd = vec2( lz / ( 1.0 + bl * 6.0 ), yn - 4.6 );
            col += vec3( 1.0, 0.5, 0.16 ) * on * ( smoothstep( 0.22, 0.0, length( hd ) ) * 4.0 / ( 1.0 + bl * 4.0 ) + exp( -dot( hd, hd ) * 0.6 ) * 0.35 );
          }
          gl_FragColor = vec4( col, 1.0 );
          #include <tonemapping_fragment>
          #include <encodings_fragment>
        }`,
      side: THREE.DoubleSide, extensions: { derivatives: true },
    });
  }

  // ---------------- the carriage ----------------
  const LAMP_MASK = [0b10010110010010110101, 0b01101001001101001010]; // which 180 m cells have a lamp, per side (L, R)
  const deq = g => {   // quantised positions -> float (raycasting in r128 reads raw attribute values)
    const a = g.attributes.position;
    if (!a || !a.normalized) return;
    const k = a.array instanceof Int16Array ? 32767 : a.array instanceof Uint16Array ? 65535 : a.array instanceof Int8Array ? 127 : a.array instanceof Uint8Array ? 255 : 1;
    const out = new Float32Array(a.count * 3);
    for (let i = 0; i < a.count; i++) { out[i * 3] = Math.max(-1, a.getX(i) / k); out[i * 3 + 1] = Math.max(-1, a.getY(i) / k); out[i * 3 + 2] = Math.max(-1, a.getZ(i) / k); }
    g.setAttribute("position", new THREE.BufferAttribute(out, 3));
  };

  /** Build the carriage under `parent` (at the origin). Returns a controller, or null when the props did not load. */
  P.train = (L, parent) => {
    if (!P.ready) return null;
    const meta = P.meta, lay = meta.layout;
    const root = P.gltf.scene.clone(true);
    root.userData = {}; root.userData.noMerge = true; root.userData.noEcho = true;
    const nodes = {};
    root.children.forEach(n => { nodes[n.name] = n; });
    const defs = {};
    (P.gltf.parser.json.materials || []).forEach(d => { defs[d.name] = d; });
    const mk = (name, swaying) => {
      const k = name + (swaying ? "|s" : "");
      if (MATS[k]) return MATS[k];
      const d = defs[name] || { name, extras: {} }, kind = (d.extras || {}).kind;
      return (MATS[k] = kind === "lamp" ? lampMat(swaying) : kind === "glass" ? glassMat() : kind === "lit" ? litMat(d) : bakedMat(d, swaying));
    };
    root.traverse(o => {
      if (!o.isMesh) return;
      const nm = o.material && o.material.name;
      o.material = mk(nm, o.parent && o.parent.name === "pendants" || o.name === "pendants" || (o.parent && o.parent.parent && o.parent.parent.name === "pendants"));
      o.castShadow = false; o.receiveShadow = false;
      o.userData.noMerge = true; o.userData.noEcho = true;
      if (nm === "glass") o.renderOrder = 2;
    });
    ["coat", "watch", "letter"].forEach(k => nodes[k] && nodes[k].traverse(o => { if (o.isMesh) deq(o.geometry); }));
    // the exterior shell casts the moon's shadow over the people inside
    if (nodes.exterior) nodes.exterior.traverse(o => { if (o.isMesh) { o.castShadow = true; o.receiveShadow = true; } });
    parent.add(root);

    // outside: proxy planes for the ray-cast landscape
    const sg = new THREE.PlaneGeometry(520, 120);
    const scen = new THREE.Group(); scen.userData.noMerge = scen.userData.noEcho = true;
    for (const sx of [-1, 1]) {
      const m = new THREE.Mesh(sg, sceneryMat());
      m.position.set(sx * 10.5, 20, 0); m.rotation.y = -sx * Math.PI / 2; m.frustumCulled = false; m.renderOrder = 10;
      scen.add(m);
    }
    root.add(scen);
    // slanted rain outside (sheared by the train's speed)
    const rainG = new THREE.Group(); rainG.matrixAutoUpdate = false; rainG.userData.noMerge = true; root.add(rainG);
    for (const sx of [-1, 1]) R.rain(rainG, [7, 10, 46], [sx * 5.6, -2, 0], 520);

    // lamps for the people (pooled point lights) + lamp flicker
    const lampLights = (meta.lights || []).filter(l => l.w > 10).map(l => L.light(root, 0xffb878, l.w > 20 ? 1.9 : 1.1, l.w > 20 ? 5.0 : 3.8, l.p[0], l.p[1] - 0.15, l.p[2]));
    const sweepL = [L.light(root, 0xff8a3a, 0, 9, -3.3, 3.4, 0), L.light(root, 0xff8a3a, 0, 9, 3.3, 3.4, 0)];
    const flashL = L.light(root, 0xaec8ff, 0, 10, -2.6, 2.2, 0);
    const lamps = [MATS["lampGlass"], MATS["lampGlass|s"]].filter(Boolean);
    U.maskL.value = LAMP_MASK[0]; U.maskR.value = LAMP_MASK[1];

    const T = {
      root, nodes, layout: lay, meta, scenery: scen,
      speed: 24, target: 24, dist: 137, moving: true, inside: true, flashT: 9 + Math.random() * 10, flick: { t: 4 + Math.random() * 6, k: 0 },
      doorsOpen: false, doorT: 0, onStop: null, fadeT: 1,
    };
    const cam = R.camera;
    let swayT = 0, jolt = 0, nextJoint = 0.8, flashSeq = null, cl = 0;
    T.brake = (sec = 7) => new Promise(ok => { T.target = 0; T.brakeTime = sec; T.onStop = ok; });
    T.openDoors = () => { T.doorsOpen = true; };
    T.setInside = v => { T.inside = v; if (nodes.exterior) nodes.exterior.visible = !v || T.speed < 0.01 && U.fade.value <= 0; };
    T.update = (dt, t) => {
      // speed (braking eases out)
      if (T.speed !== T.target) {
        const dv = T.speed > T.target ? -Math.max(0.6, T.speed / Math.max(0.5, T.brakeTime || 6) * 1.6) : 4;
        T.speed = dv < 0 ? Math.max(T.target, T.speed + dv * dt) : Math.min(T.target, T.speed + dv * dt);
        if (T.speed === T.target && T.target === 0 && T.onStop) { const f = T.onStop; T.onStop = null; f(); }
      }
      const v = T.speed, k = v / 24;
      T.dist = (T.dist + v * dt) % 3600;
      cl = (cl + v * dt * 0.045 / 900 + dt * 0.0006) % 1;
      U.dist.value = T.dist; U.speed.value = v; U.time.value = t; U.clouds.value = cl;
      // sway: carriage roll + rail-joint jolts (lamps swing, camera rocks very slightly)
      swayT += dt * (0.6 + 0.4 * k);
      nextJoint -= dt * v / 25;
      if (nextJoint <= 0) { nextJoint += 1 + Math.random() * 0.15; jolt = 1; }
      jolt = Math.max(0, jolt - dt * 6);
      const roll = k * (Math.sin(swayT * 2.1) * 0.006 + Math.sin(swayT * 5.3) * 0.0018) + jolt * k * 0.002;
      U.sway.value.set(roll * 6, k * Math.sin(swayT * 1.3) * 0.02);
      if (T.inside && !AZ.player.dying && cam) { cam.rotation.z += roll * 0.35; cam.position.y += (Math.sin(swayT * 7.7) * 0.0015 + jolt * 0.004) * k; }
      // electric flicker of the lamps (whole carriage)
      T.flick.t -= dt;
      if (T.flick.t <= 0) { T.flick.k = 0.35; T.flick.t = 6 + Math.random() * 14; }
      T.flick.k = Math.max(0, T.flick.k - dt);
      const fl = T.flick.k > 0 ? 1 - 0.22 * Math.abs(Math.sin(T.flick.k * 55)) * (T.flick.k / 0.35) : 1;
      U.flick.value = fl;
      lamps.forEach(m => m.color.copy(m.userData.base).multiplyScalar(fl));
      lampLights.forEach(l => { l.intensity = (l.base || (l.base = l.intensity)) * fl; });
      // passing sodium lamps (same cells as the shader): the nearest one per side lights the carriage through the windows
      for (let s = 0; s < 2; s++) {
        const mask = LAMP_MASK[s], sx = s ? 1 : -1;
        let best = null;
        for (let c = Math.floor((T.dist - 120) / 180); c <= Math.floor((T.dist + 120) / 180); c++) {
          const ci = ((c % 20) + 20) % 20;
          if (!((mask >> ci) & 1)) continue;
          const z = c * 180 + 90 - T.dist;
          if (!best || Math.abs(z) < Math.abs(best)) best = z;
        }
        const on = best !== null && Math.abs(best) < 40 && U.fade.value > 0.5 ? 1 : 0;
        const z = best || 0, inten = on * 6.5 * Math.min(1, 1.4 - Math.abs(z) / 40);
        U.sw.value[s].set(sx * 3.3, 3.45, z, Math.max(0, inten));
        sweepL[s].position.set(sx * 3.3, 3.4, z); sweepL[s].intensity = Math.max(0, inten) * 0.3;
        const out = s ? U.outR.value : U.outL.value;
        out.setRGB(0.05, 0.065, 0.09).multiplyScalar(U.fade.value > 0.5 ? 1 : 1.6);
        out.r += 0.4 * Math.max(0, inten) / 6.5; out.g += 0.2 * Math.max(0, inten) / 6.5; out.b += 0.05 * Math.max(0, inten) / 6.5;
      }
      // lightning (while the train runs): two quick pulses, thunder a little later
      T.flashT -= dt;
      if (T.flashT <= 0 && T.moving) {
        T.flashT = 22 + Math.random() * 26;
        const sd = Math.random() < 0.5 ? -1 : 1;
        U.flash.value.set(sd * 0.75, 0.62, (Math.random() - 0.3) * 0.3, 0);
        flashSeq = { t: 0, thunder: 1.4 + Math.random() * 1.6, played: false };
      }
      if (flashSeq) {
        const s = flashSeq; s.t += dt;
        const e = s.t < 0.09 ? 1 : s.t < 0.18 ? 0.25 : s.t < 0.30 ? 0.8 * (1 - (s.t - 0.18) / 0.12) : Math.max(0, 0.35 - (s.t - 0.3));
        U.flash.value.w = e * 1.2 * (T.inside ? 1 : 0.4);
        flashL.position.set(Math.sign(U.flash.value.x) * 2.4, 2.0, cam ? cam.position.z : 0); flashL.intensity = e * 3;
        if (!s.played && s.t > s.thunder) { s.played = true; AZ.sfx("water", 0.45, 0.42); AZ.sfx("hazard", 0.25, 0.35); }
        if (s.t > s.thunder + 0.5) flashSeq = null;
      } else { U.flash.value.w = 0; flashL.intensity = 0; }
      // rain shear follows the relative wind
      rainG.matrix.set(1, 0, 0, 0, 0, 1, 0, 0, 0, 1.1 * k, 1, 0, 0, 0, 0, 1); rainG.matrixWorldNeedsUpdate = true;
      // doors slide into the wall pockets
      if (T.doorsOpen && T.doorT < 1) {
        T.doorT = Math.min(1, T.doorT + dt / 1.1);
        const e = T.doorT * T.doorT * (3 - 2 * T.doorT), off = e * (lay.door.hw + 0.04);
        if (nodes.doorL) nodes.doorL.position.x = (nodes.doorL.userData.x0 !== undefined ? nodes.doorL.userData.x0 : (nodes.doorL.userData.x0 = nodes.doorL.position.x)) - off;
        if (nodes.doorR) nodes.doorR.position.x = (nodes.doorR.userData.x0 !== undefined ? nodes.doorR.userData.x0 : (nodes.doorR.userData.x0 = nodes.doorR.position.x)) + off;
      }
      // the landscape dissolves once the train has stopped (the station takes its place)
      if (T.fadeOut && U.fade.value > 0) { U.fade.value = Math.max(0, U.fade.value - dt / 2.2); if (U.fade.value === 0) { scen.visible = false; T.setInside(T.inside); } }
      scen.visible = U.fade.value > 0 && T.inside;
      if (nodes.exterior) nodes.exterior.visible = !T.inside || (T.speed === 0 && U.fade.value === 0);
    };
    T.reset = () => { U.fade.value = 1; T.fadeOut = false; };
    T.reset();
    L.update(T.update);
    return T;
  };
})();
