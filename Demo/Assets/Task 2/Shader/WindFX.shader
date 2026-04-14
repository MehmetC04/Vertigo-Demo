// ╔══════════════════════════════════════════════════════════════════════╗
// ║  Wind Effect FX — Wind FX Shader                                    ║
// ║  Shader adı   : "Wind Effect FX/Wind FX"                            ║
// ║  Editor sınıfı: WindFX_ShaderGUI                                    ║
// ║                                                                      ║
// ║  Özellikler:                                                         ║
// ║  • 3 katmanlı doku desteği (RGB + Alpha blending)                   ║
// ║  • Global Speed (_Speed): tüm pan + rot animasyonlarını ölçekler    ║
// ║  • Pan Mode (_PanMode): Loop (tek yön) veya PingPong (ileri-geri)   ║
// ║  • Mirror UV (_MirrorU/_MirrorV): ayna simetrik UV akışı            ║
// ║    → Her iki taraf merkezden dışa / dışardan merkeze doğru akar     ║
// ║  • Per-doku: Scale, Invert, Alpha override, Tile, Pan, Rot          ║
// ║  • Soft Particles desteği                                           ║
// ║  • 8 Blending modu (Additive, Alpha Blend, Multiply vs.)           ║
// ╚══════════════════════════════════════════════════════════════════════╝

Shader "Wind Effect FX/Wind FX"
{
    Properties
    {
        // ─────────────────────────────────────────────────────────────
        // GENEL GÖRSEL AYARLAR
        // ─────────────────────────────────────────────────────────────

        _TintColor ("Tint Color (RGBA)", Color) = (0.5, 0.5, 0.5, 0.5)
        // Genel renk tonu. RGB=renk çarpanı (0.5=orijinal). A=saydamlık çarpanı.

        _Bri ("Brightness", Range(-1, 1)) = 0
        // Parlaklık ofseti. -1=siyah  0=orijinal  +1=beyaz.

        _Con ("Contrast", Range(0, 2)) = 1
        // Kontrast çarpanı. 0=düz gri  1=orijinal  2=yüksek kontrast.

        // ─────────────────────────────────────────────────────────────
        // GLOBAL HIZ & MOD  ← YENİ
        // ─────────────────────────────────────────────────────────────

        _Speed ("Global Speed", Range(0, 10)) = 1.0
        // Tüm Pan (kaydırma) ve Rot (döndürme) animasyonlarına uygulanan çarpan.
        // 0=durur  1=orijinal  2=2× hız  10=maks hız
        // Shader içi hesap: scaledTime = _Time.y × _Speed

        [Enum(Loop,0, PingPong,1)] _PanMode ("Pan Mode", Float) = 0
        // Pan animasyonunun hareket biçimi:
        //   Loop(0)     → Doku tek yönde sürekli akar. Seamless rüzgar/su efektleri için.
        //   PingPong(1) → Doku ileri-geri salınır (sin dalgası). Titreyen efektler için.
        // Not: Rot (döndürme) her zaman tek yön döner, PanMode'dan etkilenmez.

        // ─────────────────────────────────────────────────────────────
        // AYNA UV MODU  ← YENİ
        // ─────────────────────────────────────────────────────────────

        [Enum(Off,0, On,1)] _MirrorU ("Mirror U (X ekseni)", Float) = 0
        // UV'nin X eksenini Pan uygulanmadan ÖNCE aynalanır.
        // Formül: mirrorU = abs(u.x * 2.0 - 1.0)
        // Sonuç: doku her iki kenarda merkeze doğru akar —
        //   Sol yarı  (0→0.5): sola gittiğinizde başlangıç → bitiş
        //   Sağ yarı  (0.5→1): sağa gittiğinizde bitiş → başlangıç
        // "Çift taraflı rüzgar" efekti için bu modu açın.
        // NOT: Dokunun seamless (dikişsiz) olması gerekmez;
        //      zaten ortadan simetrik olacak.

        [Enum(Off,0, On,1)] _MirrorV ("Mirror V (Y ekseni)", Float) = 0
        // UV'nin Y eksenini Pan uygulanmadan ÖNCE aynalanır.
        // Hem U hem V açıksa dört köşeden merkeze doğru akar (radyal ayna).

        // ─────────────────────────────────────────────────────────────
        // BLENDING MOD SEÇİCİLERİ
        // ─────────────────────────────────────────────────────────────

        [Enum(Multiply Multiply,0, Add Add,1, Multiply Add,2, Add Multiply,3)]
        _rgbc ("Blending Texture (RGB)", Float) = 0
        // Doku katmanlarının RGB kanalları nasıl birleştirilsin?
        // Multiply=koyulaştırır  Add=parlatır

        [Enum(Multiply Multiply,0, Add Add,1, Multiply Add,2, Add Multiply,3)]
        _ac ("Blending Alpha (A)", Float) = 0
        // Doku katmanlarının Alpha kanalları nasıl birleştirilsin?

        [Enum(One,0, Two,1, Three,2)] _numberTex ("Number of Textures", Float) = 0
        // Aktif doku katmanı sayısı. One=1  Two=2  Three=3.

        // ═════════════════════════════════════════════════════════════
        // DOKU KATMANI 1
        // ═════════════════════════════════════════════════════════════

        _MainTex ("Texture (RGBA) <1>", 2D) = "white" {}
        // Ana VFX dokusu. RGB=renk  A=saydamlık maskesi.

        _inv1 ("Invert RGB <1>", Range(0, 1)) = 0
        // 0=orijinal  1=RGB ters çevrilir (negatif görünüm)

        _Alpha1 ("Alpha Override <1>", Range(0, 1)) = 0
        // 1=alpha kanalı yok sayılır, tüm pikseller tam opak davranır

        _invAlpha1 ("Invert Alpha <1>", Range(0, 1)) = 0
        // 0=orijinal alpha  1=alpha ters çevrilir

        _Scale ("Scale(XY) + Pivot(ZW) <1>", Vector) = (1, 1, 0.5, 0.5)
        // XY=ölçek çarpanı (1=orijinal)  ZW=pivot UV uzayında (0.5,0.5=merkez)

        [Enum(Off,0, On,1)] _fTAE ("Animation <1>", Float) = 0.0
        // Doku 1 animasyonunu etkinleştir. Off iken Pan/Rot/Tile çalışmaz.

        _Tile1 ("Tile <1>  [X=Cols  Y=Rows  Z=FPS  W=StartFrame]", Vector) = (1, 1, 0, 0)
        // Sprite sheet animasyonu:
        //   X=sütun sayısı  Y=satır sayısı  Z=FPS(0=kapalı)  W=başlangıç karesi

        _Pan1 ("Pan <1>  [XY=Speed(units/sec)]", Vector) = (0, 0, 0, 0)
        // UV kaydırma hızı (birim/saniye). _Speed ile çarpılır.
        // PanMode=Loop → tek yön  |  PanMode=PingPong → ileri-geri

        _Rot1 ("Rot <1>  [XY=Pivot  Z=AngSpeed(rad/s)  W=StartAngle(°)]", Vector) = (0.5, 0.5, 0, 0)
        // Döndürme: XY=pivot UV uzayında  Z=açısal hız(rad/s, _Speed çarpılır)  W=başlangıç açısı(°)

        // ═════════════════════════════════════════════════════════════
        // DOKU KATMANI 2
        // ═════════════════════════════════════════════════════════════

        _MainTex2 ("Texture (RGBA) <2>", 2D) = "white" {}
        _inv2 ("Invert RGB <2>", Range(0, 1)) = 0
        _Alpha2 ("Alpha Override <2>", Range(0, 1)) = 0
        _invAlpha2 ("Invert Alpha <2>", Range(0, 1)) = 0
        _Scale2 ("Scale(XY) + Pivot(ZW) <2>", Vector) = (1, 1, 0.5, 0.5)
        [Enum(Off,0, On,1)] _sTAE ("Animation <2>", Float) = 0.0
        _Tile2 ("Tile <2>  [X=Cols  Y=Rows  Z=FPS  W=StartFrame]", Vector) = (1, 1, 0, 0)
        _Pan2 ("Pan <2>  [XY=Speed(units/sec)]", Vector) = (0, 0, 0, 0)
        _Rot2 ("Rot <2>  [XY=Pivot  Z=AngSpeed(rad/s)  W=StartAngle(°)]", Vector) = (0.5, 0.5, 0, 0)

        // ═════════════════════════════════════════════════════════════
        // DOKU KATMANI 3
        // ═════════════════════════════════════════════════════════════

        _MainTex3 ("Texture (RGBA) <3>", 2D) = "white" {}
        _inv3 ("Invert RGB <3>", Range(0, 1)) = 0
        _Alpha3 ("Alpha Override <3>", Range(0, 1)) = 0
        _invAlpha3 ("Invert Alpha <3>", Range(0, 1)) = 0
        _Scale3 ("Scale(XY) + Pivot(ZW) <3>", Vector) = (1, 1, 0.5, 0.5)
        [Enum(Off,0, On,1)] _tTAE ("Animation <3>", Float) = 0.0
        _Tile3 ("Tile <3>  [X=Cols  Y=Rows  Z=FPS  W=StartFrame]", Vector) = (1, 1, 0, 0)
        _Pan3 ("Pan <3>  [XY=Speed(units/sec)]", Vector) = (0, 0, 0, 0)
        _Rot3 ("Rot <3>  [XY=Pivot  Z=AngSpeed(rad/s)  W=StartAngle(°)]", Vector) = (0.5, 0.5, 0, 0)

        // ─────────────────────────────────────────────────────────────
        // SOFT PARTICLES
        // ─────────────────────────────────────────────────────────────

        _InvFade ("Soft Particles Factor", Range(0.01, 5.0)) = 5.0
        // Partiküller opak yüzeylerle kesişince kenarı yumuşatır.
        // Küçük=yumuşak kenar  Büyük=sert kesişme

        // ─────────────────────────────────────────────────────────────
        // DAHİLİ DURUM KAYDI  (Inspector'da gizli)
        // ─────────────────────────────────────────────────────────────
        [HideInInspector] _Mode      ("__mode",  Float) = 0.0
        [HideInInspector] _SrcBlend  ("__src",   Float) = 1.0
        [HideInInspector] _DstBlend  ("__dst",   Float) = 1.0
        [HideInInspector] _Queue     ("__queue", Float) = 3000.0

        [HideInInspector] _fT  ("__ft",  Float) = 1.0
        [HideInInspector] _fTA ("__fta", Float) = 0.0
        [HideInInspector] _sT  ("__st",  Float) = 1.0
        [HideInInspector] _sTA ("__sta", Float) = 0.0
        [HideInInspector] _tT  ("__tt",  Float) = 1.0
        [HideInInspector] _tTA ("__tta", Float) = 0.0
    }

    Category
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Blend [_SrcBlend] [_DstBlend]
        Cull Off    // Her iki yüz çizilir — single-quad VFX mesh'ler için kritik
        ZWrite Off  // Şeffaf objeler derinlik buffer'a yazmamalı

        SubShader
        {
            Pass
            {
                CGPROGRAM
                #pragma target 3.0
                #pragma vertex   vert
                #pragma fragment frag
                #pragma multi_compile_particles
                #pragma multi_compile_fog
                #include "UnityCG.cginc"

                // ── Genel uniform'lar ──────────────────────────────────────
                half  _Mode;
                fixed _fTAE;        // Doku 1 animasyon aktif mi?
                fixed _sTAE;        // Doku 2 animasyon aktif mi?
                fixed _tTAE;        // Doku 3 animasyon aktif mi?
                half  _rgbc;        // RGB blending modu
                half  _ac;          // Alpha blending modu
                fixed _numberTex;   // Aktif doku sayısı (0=1  1=2  2=3)
                fixed _Bri;         // Parlaklık ofseti
                fixed _Con;         // Kontrast çarpanı

                // ── Yeni: Hız & Mod ───────────────────────────────────────
                float _Speed;       // Global animasyon hızı çarpanı
                float _PanMode;     // 0=Loop  1=PingPong
                float _MirrorU;     // 0=Kapalı  1=X eksenini aynala (merkeze doğru akış)
                float _MirrorV;     // 0=Kapalı  1=Y eksenini aynala

                // ── Doku 1 uniform'ları ───────────────────────────────────
                sampler2D _MainTex;
                float4    _MainTex_ST;
                float     _inv1;        // RGB ters çevirme
                float     _Alpha1;      // Alpha override
                float     _invAlpha1;   // Alpha ters çevirme
                float4    _Tile1;       // Sprite sheet: X=cols Y=rows Z=fps W=startFrame
                float4    _Pan1;        // UV kaydırma hızı (birim/sn)
                float4    _Rot1;        // Döndürme: XY=pivot Z=açısal hız W=başlangıç açısı
                float4    _Scale;       // XY=ölçek ZW=pivot

                // ── Doku 2 uniform'ları ───────────────────────────────────
                sampler2D _MainTex2;
                float4    _MainTex2_ST;
                fixed     _inv2;
                fixed     _Alpha2;
                fixed     _invAlpha2;
                float4    _Tile2;
                float4    _Pan2;
                float4    _Rot2;
                float4    _Scale2;

                // ── Doku 3 uniform'ları ───────────────────────────────────
                sampler2D _MainTex3;
                float4    _MainTex3_ST;
                fixed     _inv3;
                fixed     _Alpha3;
                fixed     _invAlpha3;
                float4    _Tile3;
                float4    _Pan3;
                float4    _Rot3;
                float4    _Scale3;

                fixed4 _TintColor;

                // ── Vertex yapıları ───────────────────────────────────────
                struct appdata_t
                {
                    float4 vertex   : POSITION;
                    fixed4 color    : COLOR;
                    float2 texcoord : TEXCOORD0;
                };

                struct v2f
                {
                    float4 vertex    : SV_POSITION;
                    fixed4 color     : COLOR;
                    float2 texcoord  : TEXCOORD0;   // Doku 1 UV
                    float2 texcoord2 : TEXCOORD1;   // Doku 2 UV
                    float2 texcoord3 : TEXCOORD2;   // Doku 3 UV
                    UNITY_FOG_COORDS(3)
                    #ifdef SOFTPARTICLES_ON
                    float4 projPos   : TEXCOORD4;   // Soft particles için ekran uzayı
                    #endif
                };

                // ── Scale yardımcı fonksiyonu ─────────────────────────────
                // uv    : giriş UV
                // scale : XY=ölçek çarpanı  ZW=pivot (UV uzayı, 0.5=merkez)
                float2 ScaleUV(float2 uv, float4 scale)
                {
                    return ((uv - scale.zw) / scale.xy) + scale.zw;
                }

                // ── MirrorUV: X veya Y eksenini ayna simetrik yap ────────
                //
                // Formül: abs(kanal * 2.0 - 1.0)
                //
                // Girdi   →  Çıktı (U için örnek)
                //   0.0   →  1.0   (sol kenar)
                //   0.25  →  0.5
                //   0.5   →  0.0   (merkez = en düşük nokta)
                //   0.75  →  0.5
                //   1.0   →  1.0   (sağ kenar)
                //
                // Pan uygulanınca merkezdeki "0" en hızlı ilerler,
                // kenarlardaki "1" değerleri aynı noktadan başlar.
                // Sonuç: sol yarı ve sağ yarı birbirinin aynası —
                // doku her iki yönde de merkeze doğru veya merkezden dışa akar.
                //
                float2 ApplyMirror(float2 u)
                {
                    if (_MirrorU > 0.5)
                        u.x = abs(u.x * 2.0f - 1.0f);
                    if (_MirrorV > 0.5)
                        u.y = abs(u.y * 2.0f - 1.0f);
                    return u;
                }

                // ── UV_TPR: Pan + Rot + Tile birleşik animasyon fonksiyonu ─
                //
                // u       : giriş UV koordinatı
                // t       : Tile  [X=cols Y=rows Z=fps W=startFrame]
                // p       : Pan   [XY=hız(birim/sn), _Speed ile çarpılır]
                // r       : Rot   [XY=pivot Z=açısal hız(rad/s) W=başlangıç(°)]
                // panMode : 0=Loop  1=PingPong
                //
              // ── UV_TPR: Pan + Rot + Tile birleşik animasyon fonksiyonu ─
float3 UV_TPR(float2 u, float4 t, float4 p, float4 r, float panMode)
{
    u = ApplyMirror(u);

    float scaledTime = _Time.y * _Speed;
    float panTime;
    
    if (panMode < 0.5)
    {
        // LOOP MODU:
        // Eğer panTime'a frac() atarsan, akış bitip görünmez olduktan sonra 
        // sıfırdan temiz bir döngüyle tekrar başlar. (Burst efekti)
        // Senin istediğin gibi "hiç gerçekleşmesin, bir kere olsun bitsin" 
        // istiyorsan frac() kullanma, böyle kalsın.
        panTime = scaledTime; 
    }
    else
    {
        panTime = scaledTime + sin(scaledTime);
    }

    float angle = (r.z * scaledTime) + (3.14159265359f * (1.0f + (r.w / 180.0f)));
    float cosA = cos(angle);
    float sinA = sin(angle);

    // Pivot ve Pan işlemi
    float2 rawUV;
    rawUV.x = r.x - ((u.x - r.x) * cosA - (u.y - r.y) * sinA) + (p.x * panTime);
    rawUV.y = r.y - ((u.x - r.x) * sinA + (u.y - r.y) * cosA) + (p.y * panTime);

    // ── MÜKEMMEL ÇÖZÜM: GÖRÜNMEZLİK MASKESİ ──
    // UV koordinatı 1'i geçerse veya 0'ın altına düşerse maskeyi 0 (şeffaf) yapıyoruz.
    float mask = 1.0;
    if (rawUV.x > 1.0 || rawUV.x < 0.0 || rawUV.y > 1.0 || rawUV.y < 0.0)
    {
        mask = 0.0;
    }

    // Tile (Sprite Sheet) işlemi
    float2 v = float2(
        rawUV.x / t.x + floor(_Time.y * t.z + t.w) / t.x,
        (rawUV.y + t.y - 1.0f) / t.y - floor(floor(_Time.y * t.z + t.w) / t.x) / t.y
    );

    return float3(v, mask); // Artık xy: UV, z: Maske döndürüyor
}

                // ── Vertex Shader ─────────────────────────────────────────
                v2f vert(appdata_t v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    #ifdef SOFTPARTICLES_ON
                    o.projPos = ComputeScreenPos(o.vertex);
                    COMPUTE_EYEDEPTH(o.projPos.z);
                    #endif
                    o.color     = v.color;
                    o.texcoord  = TRANSFORM_TEX(v.texcoord, _MainTex);
                    o.texcoord2 = TRANSFORM_TEX(v.texcoord, _MainTex2);
                    o.texcoord3 = TRANSFORM_TEX(v.texcoord, _MainTex3);
                    UNITY_TRANSFER_FOG(o, o.vertex);
                    return o;
                }

                sampler2D_float _CameraDepthTexture;
                float           _InvFade;

                // ── Fragment (Piksel) Shader ──────────────────────────────
                fixed4 frag(v2f i) : SV_Target
                {
                    // ── Soft Particles: derinlik tabanlı kenar yumuşatma ──
                    // Partiküller opak yüzeylere değdiğinde keskin kenar yerine
                    // yumuşak bir geçiş oluşturur. Camera > Depth Texture açık olmalı.
                    #ifdef SOFTPARTICLES_ON
                    float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE_PROJ(
                                       _CameraDepthTexture, UNITY_PROJ_COORD(i.projPos)));
                    float partZ  = i.projPos.z;
                    float fade   = saturate(_InvFade * (sceneZ - partZ));
                    i.color.a   *= fade;
                    #endif

                    fixed4 c;       // Doku 1 rengi
                    fixed4 d = 0;   // Doku 2 rengi (varsayılan: siyah/şeffaf)
                    fixed4 e = 0;   // Doku 3 rengi (varsayılan: siyah/şeffaf)

                    // ── DOKU 1 örnekleme ──────────────────────────────
                    i.texcoord = ScaleUV(i.texcoord, _Scale);
                    if (_fTAE > 0.5)
                        // Animasyon açık: Pan + Rot + Tile uygulanmış UV
                        c = tex2D(_MainTex, UV_TPR(i.texcoord, _Tile1, _Pan1, _Rot1, _PanMode));
                    else
                        // Animasyon kapalı: doku statik
                        c = tex2D(_MainTex, i.texcoord);

                    c.rgb = lerp(c.rgb, 1 - c.rgb, _inv1);      // RGB ters çevirme
                    c.a   = lerp(c.a,   1 - c.a,   _invAlpha1); // Alpha ters çevirme
                    c.a   = lerp(c.a,   1,          _Alpha1);    // Alpha override (tam opak)

                    // ── DOKU 2 örnekleme ──────────────────────────────
                    if (_numberTex > 0.5)
                    {
                        i.texcoord2 = ScaleUV(i.texcoord2, _Scale2);
                        if (_sTAE > 0.5)
                            d = tex2D(_MainTex2, UV_TPR(i.texcoord2, _Tile2, _Pan2, _Rot2, _PanMode));
                        else
                            d = tex2D(_MainTex2, i.texcoord2);

                        d.rgb = lerp(d.rgb, 1 - d.rgb, _inv2);
                        d.a   = lerp(d.a,   1 - d.a,   _invAlpha2);
                        d.a   = lerp(d.a,   1,          _Alpha2);
                    }

                    // ── DOKU 3 örnekleme ──────────────────────────────
                    if (_numberTex > 1.5)
                    {
                        i.texcoord3 = ScaleUV(i.texcoord3, _Scale3);
                        if (_tTAE > 0.5)
                            e = tex2D(_MainTex3, UV_TPR(i.texcoord3, _Tile3, _Pan3, _Rot3, _PanMode));
                        else
                            e = tex2D(_MainTex3, i.texcoord3);

                        e.rgb = lerp(e.rgb, 1 - e.rgb, _inv3);
                        e.a   = lerp(e.a,   1 - e.a,   _invAlpha3);
                        e.a   = lerp(e.a,   1,          _Alpha3);
                    }

                    // ── RGB Blending ──────────────────────────────────
                    // Multiply koyulaştırır (maske etkisi),
                    // Add parlatır (ışık/glow etkisi).
                    if (_rgbc == 0) { if (_numberTex > 0.5) c.rgb *= d.rgb; if (_numberTex > 1.5) c.rgb *= e.rgb; }
                    if (_rgbc == 1) { if (_numberTex > 0.5) c.rgb += d.rgb; if (_numberTex > 1.5) c.rgb += e.rgb; }
                    if (_rgbc == 2) { if (_numberTex > 0.5) c.rgb *= d.rgb; if (_numberTex > 1.5) c.rgb += e.rgb; }
                    if (_rgbc == 3) { if (_numberTex > 0.5) c.rgb += d.rgb; if (_numberTex > 1.5) c.rgb *= e.rgb; }

                    // ── Alpha Blending ────────────────────────────────
                    if (_ac == 0) { if (_numberTex > 0.5) c.a *= d.a; if (_numberTex > 1.5) c.a *= e.a; }
                    if (_ac == 1) { if (_numberTex > 0.5) c.a += d.a; if (_numberTex > 1.5) c.a += e.a; }
                    if (_ac == 2) { if (_numberTex > 0.5) c.a *= d.a; if (_numberTex > 1.5) c.a += e.a; }
                    if (_ac == 3) { if (_numberTex > 0.5) c.a += d.a; if (_numberTex > 1.5) c.a *= e.a; }

                    // ── Render Blend Modları ──────────────────────────
                    // GPU blend faktörleri WindFX_ShaderGUI tarafından
                    // _SrcBlend / _DstBlend olarak atanır.

                    if (_Mode == 0) // Additive — ışık/enerji efektleri
                    {
                        c = 2.0f * i.color * _TintColor * c;
                        UNITY_APPLY_FOG_COLOR(i.fogCoord, c, fixed4(0,0,0,0));
                    }
                    if (_Mode == 1) // Additive Multiply — renk + alpha çarpımı
                    {
                        c.rgb = i.color.rgb * _TintColor.rgb * c.rgb * 2.0f;
                        c.a   = (1 - c.a) * (_TintColor.a * i.color.a * 2.0f);
                        UNITY_APPLY_FOG_COLOR(i.fogCoord, c, fixed4(0,0,0,0));
                    }
                    if (_Mode == 2) // Additive Soft — alpha ile yumuşatılmış
                    {
                        c     = 2.0f * i.color * _TintColor * c;
                        c.rgb *= c.a;
                        UNITY_APPLY_FOG_COLOR(i.fogCoord, c, fixed4(0,0,0,0));
                    }
                    if (_Mode == 3) // Alpha Blend — standart şeffaflık
                    {
                        c = 2.0f * i.color * _TintColor * c;
                        UNITY_APPLY_FOG(i.fogCoord, c);
                    }
                    if (_Mode == 4) // Blend — koyu blend
                    {
                        c = i.color * _TintColor * c;
                        UNITY_APPLY_FOG_COLOR(i.fogCoord, c, fixed4(0,0,0,0));
                    }
                    if (_Mode == 5) // Multiply — karartma / lens efektleri
                    {
                        c = i.color * _TintColor * c;
                        c = lerp(half4(1,1,1,1), c, c.a);
                        UNITY_APPLY_FOG_COLOR(i.fogCoord, c, fixed4(1,1,1,1));
                    }
                    if (_Mode == 6) // Multiply Double — çift kuvvetli çarpım
                    {
                        c.rgb = 2.0f * i.color.rgb * _TintColor.rgb * c.rgb;
                        c.a   = i.color.a * _TintColor.a * c.a;
                        c     = lerp(fixed4(0.5f,0.5f,0.5f,0.5f), c, c.a);
                        UNITY_APPLY_FOG_COLOR(i.fogCoord, c, fixed4(0.5,0.5,0.5,0.5));
                    }
                    if (_Mode == 7) // Alpha Blended Premultiply — sprite premul
                    {
                        c = i.color * _TintColor * c * i.color.a;
                    }

                    // ── Brightness & Contrast son adımı ───────────────
                    // 1) Kontrast uygulanır
                    // 2) Parlaklık eklenir
                    // 3) Alpha ile çarpılır → şeffaf bölgede renk sıfırlanır
                    c.rgb = (1 - (1 - (c.rgb * _Con)) * _Con + _Bri) * c.a;

                    return c;
                }
                ENDCG
            }
        }
    }

    // Bu shader'ın Inspector arayüzünü WindFX_ShaderGUI çizer.
    // Dosya: Assets/Editor/WindFX_ShaderGUI.cs
    CustomEditor "WindFX_ShaderGUI"
}
