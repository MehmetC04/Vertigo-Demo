// ╔══════════════════════════════════════════════════════════════════════╗
// ║          Wind Effect FX — Custom Shader GUI                         ║
// ║  Bu dosya "Wind Effect FX/Wind FX" shader'ının Unity Inspector      ║
// ║  arayüzünü çizer. ShaderGUI'yi override eder.                       ║
// ╚══════════════════════════════════════════════════════════════════════╝

using System;
using UnityEngine;
using UnityEditor;

internal class WindFX_ShaderGUI : ShaderGUI
{
    // ─────────────────────────────────────────────────────────────────────
    // ENUM TANIMLAMALARI
    // ─────────────────────────────────────────────────────────────────────

    public enum BlendMode
    {
        Additive,               
        AdditiveMultiply,       
        AdditiveSoft,           
        AlphaBlended,           
        Blend,                  
        Multiply,               
        MultiplyDouble,         
        AlphaBlendedPremultiply 
    }

    public enum NumberOfTexture
    {
        One   = 0,
        Two   = 1,
        Three = 2
    }

    public enum BlendTexture
    {
        MultiplyMultiply = 0,   
        AddAdd           = 1,   
        MultiplyAdd      = 2,   
        AddMultiply      = 3    
    }

    public enum BlendAlpha
    {
        MultiplyMultiply = 0,
        AddAdd           = 1,
        MultiplyAdd      = 2,
        AddMultiply      = 3
    }

    public enum MirrorAxis
    {
        Off = 0,
        On  = 1
    }

    // ─────────────────────────────────────────────────────────────────────
    // TOOLTIP İÇERİKLERİ
    // ─────────────────────────────────────────────────────────────────────
    private static class Styles
    {
        public static GUIContent sTintColor = new GUIContent(
            "Tint Color (RGBA)",
            "Materyalin genel renk tonu.\n" +
            "RGB → renk çarpanı (0.5,0.5,0.5 = orijinal renk)\n" +
            "A   → saydamlık çarpanı");

        public static GUIContent sBri = new GUIContent(
            "Brightness",
            "Parlaklık ofseti.\n" +
            " -1 = tamamen siyah\n" +
            "  0 = orijinal\n" +
            " +1 = tamamen beyaz");

        public static GUIContent sCon = new GUIContent(
            "Contrast",
            "Kontrast çarpanı.\n" +
            "  0 = düz gri (kontrast yok)\n" +
            "  1 = orijinal\n" +
            "  2 = çok yüksek kontrast");

        public static GUIContent sSoftPart = new GUIContent(
            "Soft Particles Factor",
            "Partiküller opak bir yüzeyle kesiştiğinde kenarı yumuşatır.\n" +
            "Çalışması için Camera'da Depth Texture açık olmalıdır.\n" +
            "  Küçük değer (0.01) → çok yumuşak, neredeyse görünmez kenar\n" +
            "  Büyük değer (5.0)  → sert kesişme");

        public static GUIContent sSpeed = new GUIContent(
            "Global Speed",
            "Tüm Pan (kaydırma) ve Rot (döndürme) animasyonlarına\n" +
            "uygulanan global hız çarpanı.\n" +
            "  0   = tamamen durur\n" +
            "  1   = orijinal hız\n" +
            "  2   = 2× hız\n" +
            " 10   = maksimum hız\n\n" +
            "Shader içinde: scaledTime = Time.y × _Speed");

        public static GUIContent sMirrorU = new GUIContent(
            "Mirror U  (X ekseni)",
            "UV'nin X eksenini Pan uygulanmadan ÖNCE aynalanır.\n\n" +
            "Formül: abs(frac(u.x) × 2 − 1)\n\n" +
            "  Off → Normal akış\n" +
            "  On  → Ayna: her iki taraf da merkezden dışa doğru akar\n\n" +
            "NOT: Dokunun seamless olmasına gerek yoktur.");

        public static GUIContent sMirrorV = new GUIContent(
            "Mirror V  (Y ekseni)",
            "UV'nin Y eksenini Pan uygulanmadan ÖNCE aynalanır.\n\n" +
            "  Off → Normal dikey akış\n" +
            "  On  → Üst ve alt yarı merkeze doğru akar\n\n" +
            "Hem Mirror U hem Mirror V açıksa radyal simetri elde edilir.");

        public static string sRenderingMode = "Rendering Mode";
        public static readonly string[] sBlendNames = new string[]
        {
            "Additive",
            "Additive Multiply",
            "Additive Soft",
            "Alpha Blended",
            "Blend",
            "Multiply",
            "Multiply Double",
            "Alpha Blended Premultiply"
        };

        public static string sNumbTex = "Number Of Textures";
        public static readonly string[] sNumbTexNames = Enum.GetNames(typeof(NumberOfTexture));

        public static string sBlendTex = "Blending Texture (RGB)";
        public static readonly string[] sBlendTexNames2 = new string[] { "Multiply", "Add" };
        public static readonly string[] sBlendTexNames3 = new string[] { "Multiply Multiply", "Add Add", "Multiply Add", "Add Multiply" };

        public static string sBlendAlpha = "Blending Alpha (A)";
        public static readonly string[] sBlendAlphaNames2 = new string[] { "Multiply", "Add" };
        public static readonly string[] sBlendAlphaNames3 = new string[] { "Multiply Multiply", "Add Add", "Multiply Add", "Add Multiply" };

        public static GUIContent sMainTex = new GUIContent(
            "Texture (RGBA)",
            "VFX dokusu.\n" +
            "RGB kanalı → renk bilgisi\n" +
            "A kanalı   → saydamlık maskesi");

        public static GUIContent sInvTex = new GUIContent(
            "Invert RGB",
            "RGB kanalını ters çevirir.\n" +
            "  0 = orijinal renk\n" +
            "  1 = renkler tersine döner");

        public static GUIContent sAlpha = new GUIContent(
            "Alpha Override",
            "Alpha kanalını tamamen devre dışı bırakır.\n" +
            "  0 = doku kendi alpha'sını kullanır\n" +
            "  1 = her piksel tam opak davranır");

        public static GUIContent sInvAlpha = new GUIContent(
            "Invert Alpha",
            "Alpha kanalını ters çevirir.\n" +
            "  0 = orijinal alpha\n" +
            "  1 = görünür alanlar şeffaf olur");

        public static GUIContent sScale = new GUIContent(
            "Scale (XY) + Pivot (ZW)",
            "Doku ölçeği ve ölçek merkezi.\n" +
            "  XY → ölçek çarpanı\n" +
            "  ZW → pivot noktası (0.5, 0.5 = tam merkez)");

        public static GUIContent sTile = new GUIContent(
            "Tile  [X=Cols  Y=Rows  Z=FPS  W=StartFrame]",
            "Sprite sheet animasyon ayarları.\n\n" +
            "  X = Yatay kare sayısı\n" +
            "  Y = Dikey kare sayısı\n" +
            "  Z = FPS\n" +
            "  W = Başlangıç karesi");

        public static GUIContent sPan = new GUIContent(
            "Pan  [XY = Speed (units/sec)]",
            "UV kaydırma (pan) hızı.\n\n" +
            "Global Speed (_Speed) ile çarpılarak tek yönde sürekli akar.");

        public static GUIContent sRot = new GUIContent(
            "Rot  [XY=Pivot  Z=AngSpeed(rad/s)  W=StartAngle(°)]",
            "UV döndürme ayarları.\n\n" +
            "  XY → Pivot (dönüş merkezi)\n" +
            "  Z  → Açısal hız (radyan/saniye)\n" +
            "  W  → Başlangıç açısı (derece)");

        public static string headerWindFX  = "💨  WIND EFFECT FX";
        public static string headerGeneral = "⚙  GENEL AYARLAR";
        public static string headerAnim    = "🎬  ANİMASYON";
        public static string headerTex1    = "🖼  DOKU KATMANI 1";
        public static string headerTex2    = "🖼  DOKU KATMANI 2";
        public static string headerTex3    = "🖼  DOKU KATMANI 3";
    }

    // ─────────────────────────────────────────────────────────────────────
    // FOLDOUT DURUM DEĞİŞKENLERİ
    // ─────────────────────────────────────────────────────────────────────
    bool tex1Fold;
    bool tex1AnimFold;
    bool tex1AnimEnable;

    bool tex2Fold;
    bool tex2AnimFold;
    bool tex2AnimEnable;

    bool tex3Fold;
    bool tex3AnimFold;
    bool tex3AnimEnable;

    // ─────────────────────────────────────────────────────────────────────
    // MATERIAL PROPERTY REFERANSLARI
    // ─────────────────────────────────────────────────────────────────────
    MaterialEditor    m_MaterialEditor;

    MaterialProperty  blendMode;
    MaterialProperty  rQueue;
    MaterialProperty  numberTexture;
    MaterialProperty  blendTex;
    MaterialProperty  blendAlpha;
    MaterialProperty  tintColor;
    MaterialProperty  bri;
    MaterialProperty  con;
    MaterialProperty  softPart;

    MaterialProperty  speed;
    MaterialProperty  mirrorU;   
    MaterialProperty  mirrorV;   

    MaterialProperty  mainTex1;
    MaterialProperty  texScale1;
    MaterialProperty  invTex1;
    MaterialProperty  alphaTex1;
    MaterialProperty  invAlphaTex1;
    MaterialProperty  fTAE;
    MaterialProperty  tile1;
    MaterialProperty  pan1;
    MaterialProperty  rot1;
    MaterialProperty  fTexFoldout;
    MaterialProperty  fTexAnimFoldout;

    MaterialProperty  mainTex2;
    MaterialProperty  texScale2;
    MaterialProperty  invTex2;
    MaterialProperty  alphaTex2;
    MaterialProperty  invAlphaTex2;
    MaterialProperty  sTAE;
    MaterialProperty  tile2;
    MaterialProperty  pan2;
    MaterialProperty  rot2;
    MaterialProperty  sTexFoldout;
    MaterialProperty  sTexAnimFoldout;

    MaterialProperty  mainTex3;
    MaterialProperty  texScale3;
    MaterialProperty  invTex3;
    MaterialProperty  alphaTex3;
    MaterialProperty  invAlphaTex3;
    MaterialProperty  tTAE;
    MaterialProperty  tile3;
    MaterialProperty  pan3;
    MaterialProperty  rot3;
    MaterialProperty  tTexFoldout;
    MaterialProperty  tTexAnimFoldout;

    // ─────────────────────────────────────────────────────────────────────
    // PROPERTY BULMA
    // ─────────────────────────────────────────────────────────────────────
    void FindProperties(MaterialProperty[] props, Material material)
    {
        blendMode     = FindProperty("_Mode",       props);
        rQueue        = FindProperty("_Queue",      props);
        numberTexture = FindProperty("_numberTex",  props);
        blendTex      = FindProperty("_rgbc",       props);
        blendAlpha    = FindProperty("_ac",         props);
        tintColor     = FindProperty("_TintColor",  props);
        bri           = FindProperty("_Bri",        props);
        con           = FindProperty("_Con",        props);
        softPart      = FindProperty("_InvFade",    props);

        speed         = FindProperty("_Speed",      props);
        mirrorU       = FindProperty("_MirrorU",    props);
        mirrorV       = FindProperty("_MirrorV",    props);

        mainTex1      = FindProperty("_MainTex",    props);
        texScale1     = FindProperty("_Scale",      props);
        invTex1       = FindProperty("_inv1",       props);
        alphaTex1     = FindProperty("_Alpha1",     props);
        invAlphaTex1  = FindProperty("_invAlpha1",  props);
        fTAE          = FindProperty("_fTAE",       props);
        tile1         = FindProperty("_Tile1",      props);
        pan1          = FindProperty("_Pan1",       props);
        rot1          = FindProperty("_Rot1",       props);
        fTexFoldout       = FindProperty("_fT",    props);
        fTexAnimFoldout   = FindProperty("_fTA",   props);
        tex1Fold      = fTexFoldout.floatValue     > 0.5f;
        tex1AnimFold  = fTexAnimFoldout.floatValue > 0.5f;
        tex1AnimEnable= fTAE.floatValue            > 0.5f;

        mainTex2      = FindProperty("_MainTex2",   props);
        texScale2     = FindProperty("_Scale2",     props);
        invTex2       = FindProperty("_inv2",       props);
        alphaTex2     = FindProperty("_Alpha2",     props);
        invAlphaTex2  = FindProperty("_invAlpha2",  props);
        sTAE          = FindProperty("_sTAE",       props);
        tile2         = FindProperty("_Tile2",      props);
        pan2          = FindProperty("_Pan2",       props);
        rot2          = FindProperty("_Rot2",       props);
        sTexFoldout       = FindProperty("_sT",    props);
        sTexAnimFoldout   = FindProperty("_sTA",   props);
        tex2Fold      = sTexFoldout.floatValue     > 0.5f;
        tex2AnimFold  = sTexAnimFoldout.floatValue > 0.5f;
        tex2AnimEnable= sTAE.floatValue            > 0.5f;

        mainTex3      = FindProperty("_MainTex3",   props);
        texScale3     = FindProperty("_Scale3",     props);
        invTex3       = FindProperty("_inv3",       props);
        alphaTex3     = FindProperty("_Alpha3",     props);
        invAlphaTex3  = FindProperty("_invAlpha3",  props);
        tTAE          = FindProperty("_tTAE",       props);
        tile3         = FindProperty("_Tile3",      props);
        pan3          = FindProperty("_Pan3",       props);
        rot3          = FindProperty("_Rot3",       props);
        tTexFoldout       = FindProperty("_tT",    props);
        tTexAnimFoldout   = FindProperty("_tTA",   props);
        tex3Fold      = tTexFoldout.floatValue     > 0.5f;
        tex3AnimFold  = tTexAnimFoldout.floatValue > 0.5f;
        tex3AnimEnable= tTAE.floatValue            > 0.5f;
    }

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] props)
    {
        Material material = materialEditor.target as Material;
        m_MaterialEditor = materialEditor;
        FindProperties(props, material);

        EditorGUI.BeginChangeCheck();
        DrawGUI(material);
        if (EditorGUI.EndChangeCheck())
        {
            foreach (var obj in blendMode.targets)
                SetupMaterialWithBlendMode((Material)obj, (BlendMode)blendMode.floatValue);
        }
    }

    void DrawGUI(Material material)
    {
        DrawTitle();
        GUILayout.Space(4);

        DrawGeneralSection(material);
        GUILayout.Space(6);

        DrawAnimationSection();
        GUILayout.Space(6);

        DrawTextureSection(material);
    }

    void DrawTitle()
    {
        var titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 13,
            alignment = TextAnchor.MiddleCenter
        };
        var boxStyle = new GUIStyle(GUI.skin.box);

        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.15f, 0.45f, 0.8f, 1f); 
        GUILayout.Box(Styles.headerWindFX, boxStyle, GUILayout.ExpandWidth(true));
        GUI.backgroundColor = prev;
    }

    void SectionHeader(string title)
    {
        GUILayout.Space(2);
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize  = 11,
            alignment = TextAnchor.MiddleLeft
        };
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        GUILayout.Box("  " + title, new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleLeft, fontStyle = FontStyle.Bold },
                      GUILayout.ExpandWidth(true));
        GUI.backgroundColor = prev;
        GUILayout.Space(2);
    }

    void DrawGeneralSection(Material material)
    {
        SectionHeader(Styles.headerGeneral);

        DrawBlendModePopup();

        m_MaterialEditor.ShaderProperty(rQueue, new GUIContent("Render Queue",
            "Render sırası.\n" +
            "  2000 → Opaque\n" +
            "  3000 → Transparent (varsayılan)\n" +
            "  4000 → Overlay\n\n" +
            "Düşük değer = önce çizilir  |  Yüksek değer = sonra çizilir"));
        material.renderQueue = (int)rQueue.floatValue;

        DrawNumberOfTexturePopup();

        if (numberTexture.floatValue > 0)
        {
            DrawBlendTexturePopup();
            DrawBlendAlphaPopup();
        }

        GUILayout.Space(4);
        EditorGUILayout.Separator();

        m_MaterialEditor.ColorProperty(tintColor, Styles.sTintColor.text);
        DrawTooltipProperty(bri,      Styles.sBri);
        DrawTooltipProperty(con,      Styles.sCon);
        DrawTooltipProperty(softPart, Styles.sSoftPart);
    }

    void DrawAnimationSection()
    {
        SectionHeader(Styles.headerAnim);

        DrawTooltipProperty(speed,   Styles.sSpeed);

        GUILayout.Space(4);

        // ── Mirror UV ──────────────────────────────────────────────
        EditorGUILayout.LabelField("Mirror UV  (Ayna Simetri)", EditorStyles.boldLabel);

        DrawTooltipProperty(mirrorU, Styles.sMirrorU);
        DrawTooltipProperty(mirrorV, Styles.sMirrorV);

        bool mu = mirrorU.floatValue > 0.5f;
        bool mv = mirrorV.floatValue > 0.5f;
        string mirrorMsg;
        if (mu && mv)
            mirrorMsg = "Mirror U+V: Dört köşeden merkeze doğru akan radyal simetri.";
        else if (mu)
            mirrorMsg = "Mirror U: Sol ve sağ kanat aynalı — her iki taraf merkezden dışa akar.";
        else if (mv)
            mirrorMsg = "Mirror V: Üst ve alt yarı aynalı — her iki taraf merkezden dışa akar.";
        else
            mirrorMsg = "Mirror kapalı: Normal tek yön UV akışı.";

        EditorGUILayout.HelpBox(mirrorMsg, mu || mv ? MessageType.Info : MessageType.None);
    }

    void DrawTextureSection(Material material)
    {
        DrawTextureFold(
            header:        Styles.headerTex1,
            mainTex:       mainTex1,
            texScale:      texScale1,
            invTex:        invTex1,
            alphaTex:      alphaTex1,
            invAlphaTex:   invAlphaTex1,
            taeProperty:   fTAE,
            tile:          tile1,
            pan:           pan1,
            rot:           rot1,
            foldoutProp:   fTexFoldout,
            animFoldProp:  fTexAnimFoldout,
            ref tex1Fold,
            ref tex1AnimFold,
            ref tex1AnimEnable
        );

        if (numberTexture.floatValue > 0.5f)
        {
            GUILayout.Space(4);
            DrawTextureFold(
                header:        Styles.headerTex2,
                mainTex:       mainTex2,
                texScale:      texScale2,
                invTex:        invTex2,
                alphaTex:      alphaTex2,
                invAlphaTex:   invAlphaTex2,
                taeProperty:   sTAE,
                tile:          tile2,
                pan:           pan2,
                rot:           rot2,
                foldoutProp:   sTexFoldout,
                animFoldProp:  sTexAnimFoldout,
                ref tex2Fold,
                ref tex2AnimFold,
                ref tex2AnimEnable
            );
        }

        if (numberTexture.floatValue > 1.5f)
        {
            GUILayout.Space(4);
            DrawTextureFold(
                header:        Styles.headerTex3,
                mainTex:       mainTex3,
                texScale:      texScale3,
                invTex:        invTex3,
                alphaTex:      alphaTex3,
                invAlphaTex:   invAlphaTex3,
                taeProperty:   tTAE,
                tile:          tile3,
                pan:           pan3,
                rot:           rot3,
                foldoutProp:   tTexFoldout,
                animFoldProp:  tTexAnimFoldout,
                ref tex3Fold,
                ref tex3AnimFold,
                ref tex3AnimEnable
            );
        }
    }

    void DrawTextureFold(
        string            header,
        MaterialProperty  mainTex,
        MaterialProperty  texScale,
        MaterialProperty  invTex,
        MaterialProperty  alphaTex,
        MaterialProperty  invAlphaTex,
        MaterialProperty  taeProperty,
        MaterialProperty  tile,
        MaterialProperty  pan,
        MaterialProperty  rot,
        MaterialProperty  foldoutProp,
        MaterialProperty  animFoldProp,
        ref bool isFold,
        ref bool isAnimFold,
        ref bool isAnimEnable)
    {
        isFold = GUILayout.Toggle(isFold, header, EditorStyles.toolbarButton);
        foldoutProp.floatValue = isFold ? 1f : 0f;

        if (!isFold) return;

        EditorGUI.indentLevel++;

        m_MaterialEditor.TexturePropertySingleLine(Styles.sMainTex, mainTex);
        m_MaterialEditor.TextureScaleOffsetProperty(mainTex);

        DrawTooltipProperty(invTex,      Styles.sInvTex);
        DrawTooltipProperty(alphaTex,    Styles.sAlpha);
        DrawTooltipProperty(invAlphaTex, Styles.sInvAlpha);
        DrawTooltipProperty(texScale,    Styles.sScale);

        GUILayout.Space(4);

        GUILayout.BeginHorizontal();
        isAnimEnable = GUILayout.Toggle(isAnimEnable, new GUIContent("",
            "Animasyonu etkinleştir.\n" +
            "Kapalıysa Pan / Rot / Tile parametreleri çalışmaz,\n" +
            "doku statik kalır."),
            GUILayout.Width(14));
        isAnimFold = GUILayout.Toggle(isAnimFold,
            new GUIContent("Animasyon",
                "Animasyon parametrelerini aç/kapat."),
            EditorStyles.foldout);
        GUILayout.EndHorizontal();

        animFoldProp.floatValue = isAnimFold ? 1f : 0f;

        if (isAnimEnable)
        {
            taeProperty.floatValue = 1f;

            if (isAnimFold)
            {
                EditorGUI.indentLevel++;

                m_MaterialEditor.VectorProperty(tile, Styles.sTile.text);
                DrawVectorTooltipHint(Styles.sTile.tooltip);

                m_MaterialEditor.VectorProperty(pan, Styles.sPan.text);
                DrawVectorTooltipHint(Styles.sPan.tooltip);

                m_MaterialEditor.VectorProperty(rot, Styles.sRot.text);
                DrawVectorTooltipHint(Styles.sRot.tooltip);

                EditorGUI.indentLevel--;
            }
        }
        else
        {
            taeProperty.floatValue = 0f;
            if (isAnimFold)
                EditorGUILayout.HelpBox("Animasyon devre dışı — sol checkbox'ı işaretle.", MessageType.Warning);
        }

        EditorGUI.indentLevel--;
    }

    void DrawTooltipProperty(MaterialProperty prop, GUIContent label)
    {
        m_MaterialEditor.ShaderProperty(prop, label);
    }

    void DrawVectorTooltipHint(string hint)
    {
        var style = new GUIStyle(EditorStyles.miniLabel)
        {
            wordWrap  = true,
            fontStyle = FontStyle.Italic
        };
        Color prev = GUI.contentColor;
        GUI.contentColor = new Color(0.7f, 0.8f, 1f, 1f);
        GUILayout.Label("ℹ  " + hint.Split('\n')[0], style); 
        GUI.contentColor = prev;
    }

    void DrawBlendModePopup()
    {
        EditorGUI.showMixedValue = blendMode.hasMixedValue;
        var mode = (BlendMode)blendMode.floatValue;
        EditorGUI.BeginChangeCheck();
        mode = (BlendMode)EditorGUILayout.Popup(
            new GUIContent(Styles.sRenderingMode,
                "Materyalin render blend modu."),
            (int)mode, Styles.sBlendNames);
        if (EditorGUI.EndChangeCheck())
        {
            m_MaterialEditor.RegisterPropertyChangeUndo("Rendering Mode");
            blendMode.floatValue = (float)mode;
        }
        EditorGUI.showMixedValue = false;
    }

    void DrawNumberOfTexturePopup()
    {
        EditorGUI.showMixedValue = numberTexture.hasMixedValue;
        var n = (NumberOfTexture)numberTexture.floatValue;
        EditorGUI.BeginChangeCheck();
        n = (NumberOfTexture)EditorGUILayout.Popup(
            new GUIContent(Styles.sNumbTex,
                "Kaç doku katmanı kullanılacak?"),
            (int)n, Styles.sNumbTexNames);
        if (EditorGUI.EndChangeCheck())
        {
            m_MaterialEditor.RegisterPropertyChangeUndo("Number Of Textures");
            numberTexture.floatValue = (float)n;
        }
        EditorGUI.showMixedValue = false;
    }

    void DrawBlendTexturePopup()
    {
        EditorGUI.showMixedValue = blendTex.hasMixedValue;
        var blend = (BlendTexture)blendTex.floatValue;
        EditorGUI.BeginChangeCheck();
        var label = new GUIContent(Styles.sBlendTex,
            "Doku katmanlarının RGB kanalları nasıl birleştirilsin?");
        if (numberTexture.floatValue == 1)
            blend = (BlendTexture)EditorGUILayout.Popup(label, (int)blend, Styles.sBlendTexNames2);
        else
            blend = (BlendTexture)EditorGUILayout.Popup(label, (int)blend, Styles.sBlendTexNames3);
        if (EditorGUI.EndChangeCheck())
        {
            m_MaterialEditor.RegisterPropertyChangeUndo("Blend Texture");
            blendTex.floatValue = (float)blend;
        }
        EditorGUI.showMixedValue = false;
    }

    void DrawBlendAlphaPopup()
    {
        EditorGUI.showMixedValue = blendAlpha.hasMixedValue;
        var alpha = (BlendAlpha)blendAlpha.floatValue;
        EditorGUI.BeginChangeCheck();
        var label = new GUIContent(Styles.sBlendAlpha,
            "Doku katmanlarının Alpha kanalları nasıl birleştirilsin?");
        if (numberTexture.floatValue == 1)
            alpha = (BlendAlpha)EditorGUILayout.Popup(label, (int)alpha, Styles.sBlendAlphaNames2);
        else
            alpha = (BlendAlpha)EditorGUILayout.Popup(label, (int)alpha, Styles.sBlendAlphaNames3);
        if (EditorGUI.EndChangeCheck())
        {
            m_MaterialEditor.RegisterPropertyChangeUndo("Blend Alpha");
            blendAlpha.floatValue = (float)alpha;
        }
        EditorGUI.showMixedValue = false;
    }

    public static void SetupMaterialWithBlendMode(Material material, BlendMode blendMode)
    {
        switch (blendMode)
        {
            case BlendMode.Additive:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                break;
            case BlendMode.AdditiveMultiply:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                break;
            case BlendMode.AdditiveSoft:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcColor);
                break;
            case BlendMode.AlphaBlended:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                break;
            case BlendMode.Blend:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                break;
            case BlendMode.Multiply:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.SrcColor);
                break;
            case BlendMode.MultiplyDouble:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.DstColor);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.SrcColor);
                break;
            case BlendMode.AlphaBlendedPremultiply:
                material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                break;
        }
    }

}