using UnityEngine;

public class SceneBootstrapper : MonoBehaviour
{
    [Header("Tower Data")] public TowerData archerData, mageData, freezerData, cannonData;
    [Header("Enemy Data")]  public EnemyData goblinData, orcData, ghostData;

    void Awake()
    {
        // Вежі та снаряди
        MakeTower(archerData,  TowerSprite(TKind.Archer),  ArrowSprite());
        MakeTower(mageData,    TowerSprite(TKind.Mage),    MagicOrbSprite());
        MakeTower(freezerData, TowerSprite(TKind.Freezer), IceCrystalSprite());
        MakeTower(cannonData,  TowerSprite(TKind.Cannon),  CannonballSprite());

        // Вороги
        MakeEnemy(goblinData, GoblinSprite(), 0.60f);
        MakeEnemy(orcData,    OrcSprite(),    0.85f);
        MakeEnemy(ghostData,  GhostSprite(),  0.70f);

        // Замок бази (великий спрайт для правого боку)
        SpawnCastle();
    }

    // ══════════════════════════════════════════════════════════════════════
    // TOWER FACTORIES
    // ══════════════════════════════════════════════════════════════════════

    void MakeTower(TowerData d, Sprite spr, Sprite projSpr)
    {
        if (d == null) return;
        if (d.towerPrefab == null)
        {
            var go = new GameObject($"Tower_{d.towerName}");
            go.SetActive(false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = spr; sr.sortingOrder = 2;
            go.AddComponent<Tower>(); go.AddComponent<TowerShooter>();
            DontDestroyOnLoad(go);
            d.towerPrefab = go;
        }
        if (d.projectilePrefab == null && projSpr != null)
        {
            var go = new GameObject($"Proj_{d.towerName}");
            go.SetActive(false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = projSpr; sr.sortingOrder = 4;
            go.transform.localScale = Vector3.one * 0.28f;
            go.AddComponent<Projectile>();
            DontDestroyOnLoad(go);
            d.projectilePrefab = go;
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // ENEMY FACTORY
    // ══════════════════════════════════════════════════════════════════════

    void MakeEnemy(EnemyData d, Sprite spr, float scale)
    {
        if (d == null || d.prefab != null) return;
        var go = new GameObject($"Enemy_{d.enemyName}");
        go.SetActive(false);
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = spr;
        sr.sortingOrder = 3;
        // КРИТИЧНО: Enemy має [RequireComponent(EnemyMover)] та [RequireComponent(EnemyHealth)]
        // тому Unity сам додає ці компоненти. НЕ додавати їх вручну — буде дубль!
        go.AddComponent<Enemy>();
        var h = go.GetComponent<EnemyHealth>();         // ← беремо авто-додану
        h.healthBarFill = BuildHPBar(go.transform);
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.42f; col.isTrigger = true;
        int el = LayerMask.NameToLayer("Enemy");
        if (el >= 0) go.layer = el;
        DontDestroyOnLoad(go);
        d.prefab = go;
    }

    // ══════════════════════════════════════════════════════════════════════
    // CASTLE BASE DECORATION
    // ══════════════════════════════════════════════════════════════════════

    void SpawnCastle()
    {
        // Знаходимо Base.Instance щоб дізнатись позицію
        var baseObj = FindAnyObjectByType<Base>();
        if (baseObj == null) return;
        Vector3 basePos = baseObj.transform.position;

        // Замок за базою (трохи зміщений)
        var castleGo = new GameObject("CastleDecoration");
        castleGo.transform.position = new Vector3(basePos.x + 0.3f, basePos.y - 0.2f, 0.5f);

        var sr = castleGo.AddComponent<SpriteRenderer>();
        sr.sprite       = CastleSprite();
        sr.sortingOrder = 1; // Позаду ворогів/веж

        // Маленький прапор
        var flagGo = new GameObject("Flag");
        flagGo.transform.SetParent(castleGo.transform, false);
        flagGo.transform.localPosition = new Vector3(0.05f, 0.95f, 0);
        flagGo.transform.localScale    = Vector3.one * 0.45f;
        var fsr = flagGo.AddComponent<SpriteRenderer>();
        fsr.sprite       = FlagSprite();
        fsr.sortingOrder = 2;

        // Знищуємо оригінальний маленький жовтий квадрат SpriteRenderer бази
        var baseSR = baseObj.GetComponent<SpriteRenderer>();
        if (baseSR != null) Destroy(baseSR);
    }

    // ══════════════════════════════════════════════════════════════════════
    // PIXEL ART: TOWER SPRITES (20×30)
    // ══════════════════════════════════════════════════════════════════════

    enum TKind { Archer, Mage, Freezer, Cannon }

    static Sprite TowerSprite(TKind k)
    {
        const int W = 20, H = 30;
        var px = new Color[W * H]; for (int i=0;i<px.Length;i++) px[i]=Color.clear;

        Color wall, dark, win, flag;
        switch (k)
        {
            case TKind.Archer:
                wall=new Color(.62f,.54f,.40f); dark=new Color(.35f,.28f,.18f);
                win=new Color(.08f,.12f,.25f,.9f); flag=new Color(.85f,.15f,.10f); break;
            case TKind.Mage:
                wall=new Color(.28f,.10f,.42f); dark=new Color(.14f,.04f,.22f);
                win=new Color(.55f,.10f,.85f,.8f); flag=new Color(.85f,.20f,1f); break;
            case TKind.Freezer:
                wall=new Color(.30f,.55f,.80f); dark=new Color(.15f,.30f,.55f);
                win=new Color(.55f,.85f,1f,.8f); flag=new Color(.55f,.90f,1f); break;
            default:
                wall=new Color(.30f,.26f,.22f); dark=new Color(.14f,.12f,.10f);
                win=new Color(.18f,.16f,.14f,.9f); flag=new Color(.70f,.10f,.10f); break;
        }
        Color light = Color.Lerp(wall, Color.white, .18f);

        void S(int x,int y,Color c){if(x>=0&&x<W&&y>=0&&y<H)px[y*W+x]=c;}

        // Основа
        for(int y=0;y<=2;y++) for(int x=3;x<=16;x++)
            S(x,y, x==3||x==16?dark:wall);
        // Тіло
        for(int y=3;y<=18;y++) for(int x=4;x<=15;x++)
            S(x,y, x==4||x==15?dark:wall);
        // Тіньова смуга
        for(int y=3;y<=18;y++){S(14,y,dark); if(y%4==0) for(int x=5;x<14;x++) S(x,y,dark);}
        // Вікна
        for(int y=9;y<=14;y++) for(int x=6;x<=8;x++)
            S(x,y, x==6||x==8||y==9||y==14?wall:win);
        for(int y=9;y<=14;y++) for(int x=11;x<=13;x++)
            S(x,y, x==11||x==13||y==9||y==14?wall:win);
        // Зубці
        for(int y=19;y<=23;y++) for(int x=4;x<=15;x++){
            bool m=(x>=4&&x<=5)||(x>=8&&x<=9)||(x>=12&&x<=13);
            S(x,y, m?(y==23?light:wall):Color.clear);
        }
        // Прапор
        for(int y=24;y<=28;y++) S(9,y,new Color(.4f,.35f,.2f));
        for(int y=25;y<=28;y++) for(int x=10;x<=13;x++)
            S(x,y, y==28||x==13?dark:flag);

        var tex=new Texture2D(W,H){filterMode=FilterMode.Point};
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex,new Rect(0,0,W,H),new Vector2(.5f,.15f),W);
    }

    // ══════════════════════════════════════════════════════════════════════
    // PIXEL ART: PROJECTILES
    // ══════════════════════════════════════════════════════════════════════

    /// Стріла (Archer) — 16×6, вказує вгору після rotation -90°
    static Sprite ArrowSprite()
    {
        const int W=6, H=16;
        var px=new Color[W*H]; for(int i=0;i<px.Length;i++) px[i]=Color.clear;
        Color shaft=new Color(.55f,.40f,.20f);
        Color tip  =new Color(.80f,.80f,.85f);
        Color flt  =new Color(.70f,.15f,.10f);

        void S(int x,int y,Color c){if(x>=0&&x<W&&y>=0&&y<H)px[y*W+x]=c;}

        // Наконечник (top)
        S(2,15,tip); S(3,15,tip);
        S(1,14,tip); S(2,14,tip); S(3,14,tip); S(4,14,tip);
        S(2,13,tip); S(3,13,tip);
        // Древко
        for(int y=4;y<=12;y++){S(2,y,shaft); S(3,y,shaft);}
        // Оперення (bottom)
        S(0,3,flt); S(1,3,flt); S(2,3,shaft); S(3,3,shaft); S(4,3,flt); S(5,3,flt);
        S(0,2,flt); S(1,2,flt); S(2,2,shaft); S(3,2,shaft); S(4,2,flt); S(5,2,flt);
        S(2,1,shaft); S(3,1,shaft);
        S(2,0,shaft); S(3,0,shaft);

        var tex=new Texture2D(W,H){filterMode=FilterMode.Point};
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex,new Rect(0,0,W,H),new Vector2(.5f,.5f),H/2);
    }

    /// Магічна куля (Mage) — 12×12 фіолетова з ореолом
    static Sprite MagicOrbSprite()
    {
        const int S=12;
        var px=new Color[S*S]; for(int i=0;i<px.Length;i++) px[i]=Color.clear;
        Color core=new Color(.90f,.25f,1f); Color glow=new Color(.60f,.10f,.85f,.7f);
        Color hi  =new Color(1f,.75f,1f);

        float cx=S/2f-.5f, cy=S/2f-.5f;
        for(int y=0;y<S;y++) for(int x=0;x<S;x++){
            float r=Mathf.Sqrt((x-cx)*(x-cx)+(y-cy)*(y-cy));
            if(r<2.5f) px[y*S+x]=hi;
            else if(r<4f) px[y*S+x]=core;
            else if(r<5.5f) px[y*S+x]=glow;
        }
        // Зірочки навколо
        void Dot(int x,int y){if(x>=0&&x<S&&y>=0&&y<S) px[y*S+x]=hi;}
        Dot(1,5); Dot(10,6); Dot(5,0); Dot(6,11);

        var tex=new Texture2D(S,S){filterMode=FilterMode.Point};
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex,new Rect(0,0,S,S),new Vector2(.5f,.5f),S/2);
    }

    /// Крижаний кристал (Freezer) — 10×10 шестикутник
    static Sprite IceCrystalSprite()
    {
        const int S=10;
        var px=new Color[S*S]; for(int i=0;i<px.Length;i++) px[i]=Color.clear;
        Color core=new Color(.55f,.88f,1f); Color edge=new Color(.25f,.55f,.80f);
        Color hi  =Color.white;

        // Хрест (шестикутна зірка)
        for(int i=0;i<S;i++){
            px[4*S+i]=core; px[5*S+i]=core;
            px[i*S+4]=core; px[i*S+5]=core;
        }
        // Діагональ
        for(int i=2;i<8;i++){
            px[i*S+i]=edge; px[i*S+(9-i)]=edge;
        }
        // Центр
        for(int y=3;y<7;y++) for(int x=3;x<7;x++) px[y*S+x]=core;
        // Блик
        px[3*S+4]=hi; px[3*S+5]=hi; px[4*S+4]=hi;

        var tex=new Texture2D(S,S){filterMode=FilterMode.Point};
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex,new Rect(0,0,S,S),new Vector2(.5f,.5f),S/2);
    }

    /// Ядро (Cannon) — 10×10 темна куля з блиском
    static Sprite CannonballSprite()
    {
        const int S=10;
        var px=new Color[S*S]; for(int i=0;i<px.Length;i++) px[i]=Color.clear;
        Color body=new Color(.28f,.25f,.22f); Color edge=new Color(.14f,.12f,.10f);
        Color hi  =new Color(.55f,.50f,.42f);

        float cx=S/2f-.5f, cy=S/2f-.5f;
        for(int y=0;y<S;y++) for(int x=0;x<S;x++){
            float r=Mathf.Sqrt((x-cx)*(x-cx)+(y-cy)*(y-cy));
            if(r<3.8f) px[y*S+x]=body;
            else if(r<4.5f) px[y*S+x]=edge;
        }
        px[3*S+3]=hi; px[3*S+4]=hi; px[4*S+3]=hi;

        var tex=new Texture2D(S,S){filterMode=FilterMode.Point};
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex,new Rect(0,0,S,S),new Vector2(.5f,.5f),S/2);
    }

    // ══════════════════════════════════════════════════════════════════════
    // PIXEL ART: ENEMY SPRITES
    // ══════════════════════════════════════════════════════════════════════

    static Sprite GoblinSprite()
    {
        // 14×16 гоблін (зелений, маленький)
        const int W=14, H=16;
        var px=new Color[W*H]; for(int i=0;i<px.Length;i++) px[i]=Color.clear;
        Color b=new Color(.30f,.58f,.15f); Color d=new Color(.18f,.38f,.08f);
        Color e=new Color(.08f,.08f,.10f); Color w=new Color(.90f,.88f,.80f);
        Color t=new Color(.65f,.88f,.35f); // світлий живіт

        void S(int x,int y,Color c){if(x>=0&&x<W&&y>=0&&y<H)px[y*W+x]=c;}

        // Голова
        for(int y=10;y<=14;y++) for(int x=3;x<=10;x++) S(x,y,b);
        S(3,14,d); S(10,14,d); S(3,10,d); S(10,10,d);
        // Вуха
        S(2,13,d); S(2,12,d); S(11,13,d); S(11,12,d);
        // Очі
        S(4,12,e); S(5,12,e); S(8,12,e); S(9,12,e);
        // Рот / зуби
        S(4,11,d); S(5,11,w); S(6,11,w); S(7,11,w); S(8,11,w); S(9,11,d);
        // Тіло
        for(int y=6;y<=9;y++) for(int x=4;x<=9;x++) S(x,y,y<=7?t:b);
        // Руки
        for(int y=6;y<=8;y++){S(2,y,b); S(3,y,b); S(10,y,b); S(11,y,b);}
        // Ноги
        for(int y=2;y<=5;y++){S(4,y,b); S(5,y,b); S(8,y,b); S(9,y,b);}
        // Тіні
        for(int y=2;y<=14;y++){
            if(px[y*W+4].a>.1f) S(4,y,Color.Lerp(px[y*W+4],d,.4f));
        }

        var tex=new Texture2D(W,H){filterMode=FilterMode.Point};
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex,new Rect(0,0,W,H),new Vector2(.5f,.15f),W);
    }

    static Sprite OrcSprite()
    {
        // 16×18 Орк (темно-зелений, великий)
        const int W=16, H=18;
        var px=new Color[W*H]; for(int i=0;i<px.Length;i++) px[i]=Color.clear;
        Color b=new Color(.22f,.42f,.12f); Color d=new Color(.10f,.22f,.05f);
        Color e=new Color(.85f,.20f,.10f); // червоні очі орка
        Color w=new Color(.88f,.82f,.70f); Color t=new Color(.35f,.55f,.20f);
        Color arm=new Color(.18f,.35f,.10f);

        void S(int x,int y,Color c){if(x>=0&&x<W&&y>=0&&y<H)px[y*W+x]=c;}

        // Голова велика
        for(int y=12;y<=16;y++) for(int x=2;x<=13;x++) S(x,y,b);
        S(2,16,d); S(13,16,d); S(2,12,d); S(13,12,d);
        // Ікла
        S(5,12,w); S(6,12,w); S(9,12,w); S(10,12,w);
        // Очі
        for(int dy=-1;dy<=1;dy++) for(int dx=-1;dx<=1;dx++){
            S(5+dx,15+dy,e); S(10+dx,15+dy,e);
        }
        S(5,15,d); S(10,15,d); // зіниці
        // Роги
        S(2,17,d); S(1,17,d); S(1,16,d);
        S(13,17,d); S(14,17,d); S(14,16,d);
        // Тіло
        for(int y=6;y<=11;y++) for(int x=3;x<=12;x++) S(x,y,y>=9?b:t);
        // Руки масивні
        for(int y=5;y<=10;y++){
            S(0,y,arm); S(1,y,arm); S(2,y,arm);
            S(13,y,arm); S(14,y,arm); S(15,y,arm);
        }
        // Кулаки
        for(int x=0;x<=2;x++){S(x,4,arm); S(x,3,arm);}
        for(int x=13;x<=15;x++){S(x,4,arm); S(x,3,arm);}
        // Ноги
        for(int y=1;y<=5;y++){
            S(4,y,b); S(5,y,b); S(6,y,b);
            S(9,y,b); S(10,y,b); S(11,y,b);
        }

        var tex=new Texture2D(W,H){filterMode=FilterMode.Point};
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex,new Rect(0,0,W,H),new Vector2(.5f,.15f),W);
    }

    static Sprite GhostSprite()
    {
        const int S=14;
        var px=new Color[S*S]; for(int i=0;i<px.Length;i++) px[i]=Color.clear;
        Color body=new Color(.78f,.85f,1f,.80f); Color dark=new Color(.40f,.45f,.65f,.70f);
        Color eye=new Color(.08f,.06f,.18f,.95f); Color glow=new Color(.90f,.95f,1f,.50f);

        float cx=S/2f-.5f, cy=S/2f-.2f;
        for(int y=0;y<S;y++) for(int x=0;x<S;x++){
            float dx=x-cx, dy=y-cy;
            float r=Mathf.Sqrt(dx*dx+dy*dy);
            float nr=r/(S*.4f);
            if(y<S-2){
                if(nr<.90f){float e=Mathf.Clamp01(1f-(nr-.65f)/.25f);
                    px[y*S+x]=Color.Lerp(dark,body,e);}
                else if(nr<1.08f)
                    px[y*S+x]=new Color(body.r,body.g,body.b,body.a*(1f-(nr-.9f)/.18f));
            } else {
                int wx=x-(int)cx;
                if(Mathf.Abs(wx)<2&&y==S-2) px[y*S+x]=body;
                if(Mathf.Abs(wx-4)<2) px[y*S+x]=body;
                if(Mathf.Abs(wx+4)<2) px[y*S+x]=body;
            }
        }
        // Очі
        void Eye(int ex,int ey){
            for(int dy=-1;dy<=1;dy++) for(int dx=-1;dx<=1;dx++)
                if(ex+dx>=0&&ex+dx<S&&ey+dy>=0&&ey+dy<S) px[(ey+dy)*S+(ex+dx)]=eye;
        }
        Eye(5,7); Eye(9,7);
        // Ореол
        for(int y=2;y<=4;y++) for(int x=4;x<=10;x++)
            if(px[y*S+x].a>.3f) px[y*S+x]=Color.Lerp(px[y*S+x],glow,.4f);

        var tex=new Texture2D(S,S){filterMode=FilterMode.Point};
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex,new Rect(0,0,S,S),new Vector2(.5f,.5f),S);
    }

    // ══════════════════════════════════════════════════════════════════════
    // PIXEL ART: CASTLE (Base decoration) 40×32
    // ══════════════════════════════════════════════════════════════════════

    static Sprite CastleSprite()
    {
        const int W=40, H=32;
        var px=new Color[W*H]; for(int i=0;i<px.Length;i++) px[i]=Color.clear;
        Color wall=new Color(.60f,.52f,.38f); Color dark=new Color(.32f,.26f,.16f);
        Color light=new Color(.78f,.70f,.55f); Color win=new Color(.08f,.12f,.25f,.9f);
        Color gate=new Color(.12f,.10f,.08f); Color roof=new Color(.48f,.18f,.12f);

        void S(int x,int y,Color c){if(x>=0&&x<W&&y>=0&&y<H)px[y*W+x]=c;}
        Color Get(int x,int y)=>x>=0&&x<W&&y>=0&&y<H?px[y*W+x]:Color.clear;

        // ── Головна вежа (центр) ──────────────────────────────────────
        for(int y=0;y<=28;y++) for(int x=15;x<=24;x++)
            S(x,y, x==15||x==24?dark:wall);
        // Зубці головної вежі
        for(int y=29;y<=31;y++) for(int x=15;x<=24;x++){
            bool m=(x>=15&&x<=16)||(x>=19&&x<=20)||(x>=23&&x<=24);
            S(x,y,m?wall:Color.clear);
        }
        // Вікно центральної вежі
        for(int y=18;y<=24;y++) for(int x=18;x<=21;x++)
            S(x,y,x==18||x==21||y==18?wall:win);
        // Арка воріт
        for(int y=0;y<=11;y++) for(int x=17;x<=22;x++){
            float cx2=19.5f, yTop=11;
            float r=Mathf.Sqrt((x-cx2)*(x-cx2)+(y-yTop)*(y-yTop));
            bool inArch=y<=7||(y<=11&&r<=3.5f);
            if(inArch) S(x,y,gate);
        }

        // ── Ліва бокова вежа ─────────────────────────────────────────
        for(int y=0;y<=20;y++) for(int x=3;x<=11;x++)
            S(x,y, x==3||x==11?dark:wall);
        for(int y=21;y<=23;y++) for(int x=3;x<=11;x++){
            bool m=(x>=3&&x<=4)||(x>=7&&x<=8)||(x>=10&&x<=11);
            S(x,y,m?wall:Color.clear);
        }
        // Вікно лівої вежі
        for(int y=11;y<=16;y++) for(int x=6;x<=9;x++)
            S(x,y,x==6||x==9||y==11||y==16?wall:win);
        // Прапор лівої вежі
        for(int y=24;y<=28;y++) S(7,y,new Color(.4f,.35f,.2f));
        for(int y=24;y<=27;y++) for(int x=8;x<=11;x++) S(x,y,roof);

        // ── Права бокова вежа ────────────────────────────────────────
        for(int y=0;y<=20;y++) for(int x=28;x<=36;x++)
            S(x,y, x==28||x==36?dark:wall);
        for(int y=21;y<=23;y++) for(int x=28;x<=36;x++){
            bool m=(x>=28&&x<=29)||(x>=32&&x<=33)||(x>=35&&x<=36);
            S(x,y,m?wall:Color.clear);
        }
        // Вікно правої вежі
        for(int y=11;y<=16;y++) for(int x=30;x<=33;x++)
            S(x,y,x==30||x==33||y==11||y==16?wall:win);
        // Прапор правої вежі
        for(int y=24;y<=28;y++) S(32,y,new Color(.4f,.35f,.2f));
        for(int y=24;y<=27;y++) for(int x=33;x<=36;x++) S(x,y,roof);

        // ── З'єднуючі стіни ──────────────────────────────────────────
        for(int y=0;y<=8;y++) for(int x=12;x<=14;x++) S(x,y,wall);
        for(int y=0;y<=8;y++) for(int x=25;x<=27;x++) S(x,y,wall);
        // Зубці стін
        for(int y=9;y<=11;y++) for(int x=12;x<=14;x++){
            S(x,y, (x==12||x==14)?wall:Color.clear);
        }
        for(int y=9;y<=11;y++) for(int x=25;x<=27;x++){
            S(x,y, (x==25||x==27)?wall:Color.clear);
        }

        // ── Горизонтальні лінії каменю ────────────────────────────────
        for(int y=5;y<=25;y+=4)
            for(int x=0;x<W;x++)
                if(Get(x,y).a>.5f) S(x,y,dark);

        var tex=new Texture2D(W,H){filterMode=FilterMode.Point};
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex,new Rect(0,0,W,H),new Vector2(.5f,.05f),(float)W/1.8f);
    }

    static Sprite FlagSprite()
    {
        const int W=12, H=14;
        var px=new Color[W*H]; for(int i=0;i<px.Length;i++) px[i]=Color.clear;
        Color pole=new Color(.4f,.35f,.2f); Color flag=new Color(.85f,.15f,.10f);
        Color hi  =new Color(.95f,.4f,.3f);
        for(int y=0;y<H;y++) { px[y*W+0]=pole; px[y*W+1]=pole; }
        for(int y=8;y<H;y++) for(int x=2;x<W;x++)
            px[y*W+x]=x<4?hi:flag;
        var tex=new Texture2D(W,H){filterMode=FilterMode.Point};
        tex.SetPixels(px); tex.Apply();
        return Sprite.Create(tex,new Rect(0,0,W,H),new Vector2(0,.0f),W);
    }

    // ══════════════════════════════════════════════════════════════════════
    // HP BAR
    // ══════════════════════════════════════════════════════════════════════

    /// HP-бар через Image.fillAmount — простіше і надійніше за Slider.
    /// Повертає Image з Type=Filled, Method=Horizontal, який можна напряму
    /// змінювати: image.fillAmount = ratio.
    static UnityEngine.UI.Image BuildHPBar(Transform parent)
    {
        var cvGo = new GameObject("HPCanvas");
        cvGo.transform.SetParent(parent, false);
        cvGo.transform.localPosition = new Vector3(0, 1.2f, -0.1f);
        cvGo.transform.localScale    = Vector3.one * 0.014f;

        var cv = cvGo.AddComponent<Canvas>();
        cv.renderMode      = RenderMode.WorldSpace;
        cv.sortingOrder    = 10;
        cv.overrideSorting = true;

        var rt = cvGo.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(64, 10);

        // Темна рамка (фон)
        var frame = new GameObject("Frame");
        frame.transform.SetParent(cvGo.transform, false);
        var frameI = frame.AddComponent<UnityEngine.UI.Image>();
        frameI.color = new Color(0.02f, 0.02f, 0.02f, 0.95f);
        var frameRt = frameI.rectTransform;
        frameRt.anchorMin = Vector2.zero;
        frameRt.anchorMax = Vector2.one;
        frameRt.offsetMin = new Vector2(-1, -1);
        frameRt.offsetMax = new Vector2( 1,  1);

        // Темний внутрішній фон
        var bgGo = new GameObject("BG");
        bgGo.transform.SetParent(cvGo.transform, false);
        var bgI = bgGo.AddComponent<UnityEngine.UI.Image>();
        bgI.color = new Color(0.15f, 0.05f, 0.05f, 0.95f);
        var bgRt = bgI.rectTransform;
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = bgRt.offsetMax = Vector2.zero;

        // Fill — Image з anchor-based ширшою. EnemyHealth змінюватиме anchorMax.x.
        var fGo = new GameObject("Fill");
        fGo.transform.SetParent(cvGo.transform, false);
        var fI = fGo.AddComponent<UnityEngine.UI.Image>();
        fI.color      = new Color(0.18f, 0.92f, 0.22f);
        // Просто Type=Simple, без спрайту — Unity Image малює суцільний колір
        fI.type       = UnityEngine.UI.Image.Type.Simple;
        var fRt = fI.rectTransform;
        fRt.anchorMin = Vector2.zero;
        fRt.anchorMax = Vector2.one;       // повна ширина на старті
        fRt.offsetMin = fRt.offsetMax = Vector2.zero;
        return fI;
    }

    /// Однопіксельний білий спрайт (потрібен для Image.Type.Filled)
    static Sprite _solidSprite;
    static Sprite MakeSolidSprite()
    {
        if (_solidSprite != null) return _solidSprite;
        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false){ filterMode = FilterMode.Point };
        var px = new Color[4]; for (int i=0;i<4;i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        _solidSprite = Sprite.Create(tex, new Rect(0,0,2,2), new Vector2(0.5f,0.5f), 4);
        return _solidSprite;
    }
}
