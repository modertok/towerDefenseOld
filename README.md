# Tower Defense 2D — Звіт

> **▶ Грати в браузері:** https://modertok.github.io/towerDefenseOld/

Двовимірна гра жанру Tower Defense у середньовічному стилі, реалізована на Unity 6 (6000.4.8f1) під WebGL. Гравець у ролі **Захисника** будує вежі на сітці 12×8, а AI-Атакуючий формує хвилі ворогів. Мета — встояти 10 раундів, не давши базі впасти до 0 HP.

---

## 1. Опис правил та геймплею

### 1.1 Загальні правила
- Поле: фіксована сітка **12 колонок × 8 рядків**.
- Шлях ворогів — Z-подібна траєкторія з **6 waypoints**, прокладена через клітинки `isPath`.
- База Захисника має **20 HP**. Кожен ворог, що дійшов до бази, віднімає 1 HP і повертається до пулу.
- Грається **10 раундів**. На кожен раунд AI-Атакуючий формує хвилю в межах свого бюджету.
- **Перемога** Захисника: пережити всі 10 раундів з HP > 0.
- **Поразка**: HP бази впало до 0.

### 1.2 Цикл одного раунду
1. **Preparation** — гравець обирає вежу зі sidebar і клікає по зеленій (вільній) клітинці. За куплену вежу списується золото.
2. Натискання **▶ ПОЧАТИ ХВИЛЮ** переводить гру у стан Battle.
3. **Battle** — `WaveManager` спавнить ворогів з черги з інтервалом 0.8–1.2 с. Вежі автоматично таргетують ворога з найвищим Progress (найближчим до бази) і стріляють.
4. Коли всі вороги хвилі або вбиті, або дійшли до бази → **RoundEnd**: нараховується золото, бюджет атаки збільшується.
5. Після короткої паузи — наступний раунд (знов Preparation) або **GameOver**, якщо це був 10-й раунд або база впала.

### 1.3 Управління
| Дія | Кнопка |
|-----|--------|
| Обрати вежу зі sidebar | ЛКМ по кнопці вежі |
| Поставити вежу | ЛКМ по зеленій клітинці |
| Показати радіус дії вежі | ЛКМ по поставленій вежі |
| Скасувати вибір / сховати радіус | ПКМ |
| Почати хвилю | Кнопка `▶ ПОЧАТИ ХВИЛЮ` у HUD |

### 1.4 Вежі

| Вежа | Ціна | Швидк. атаки | Шкода | Дальність | Тип |
|------|-----:|-------------:|------:|----------:|-----|
| 🏹 Лучник  | 100g | 1.0 / с | 20 | 3.0 | Single |
| 🔮 Маг     | 150g | 0.6 / с | 30 | 2.0 | AoE (r=1.5) |
| ❄ Льодяник | 120g | 1.0 / с | 10 (½ при slow) | 3.0 | Slow −50% / 2 с |
| 💣 Гармата | 200g | 0.3 / с | 65 | 4.5 | Single (важка шкода) |

### 1.5 Вороги

| Ворог | HP | Швидк. | Бюджет (вартість для AI) | Золото за вбивство | Особливість |
|-------|---:|-------:|-------------------------:|-------------------:|-------------|
| Гоблін  | 40  | 3.0 | 10 | 5  | швидкий, слабкий |
| Орк     | 150 | 1.5 | 25 | 12 | повільний, міцний |
| Привид  | 80  | 2.0 | 20 | 8  | імунітет до Льодяника |

---

## 2. Схема станів (State Machine)

```
                ┌──────────┐
                │   Menu   │   ← MainMenu.unity
                └────┬─────┘
                     │ кнопка ГРАТИ
                     ▼
┌──────────────► Preparation ◄─────────────┐
│                    │                      │
│      кнопка ПОЧАТИ ХВИЛЮ                  │
│                    ▼                      │
│                  Battle                   │
│                    │                      │
│    всі вороги в хвилі знищені/дійшли      │
│                    ▼                      │
│                 RoundEnd                  │
│                    │                      │
│            current < totalRounds          │
└────────────────────┘   AND HP > 0
                     │
              інакше ▼
                 GameOver  → екран ПЕРЕМОГА / ВИ ПРОГРАЛИ
```

### 2.1 Реалізація — `GameStateManager.cs`

```csharp
public enum GameState { Menu, Preparation, Battle, RoundEnd, GameOver }

public class GameStateManager : MonoBehaviour {
    public static GameStateManager Instance { get; private set; }
    public GameState CurrentState { get; private set; }
    public event Action<GameState> OnStateChanged;
    public void SetState(GameState next) { CurrentState = next; OnStateChanged?.Invoke(next); }
}
```

Всі інші системи (`WaveManager`, `HudUI`, `TowerSidebar`, `StartWaveButton`, `GameOverUI`) підписуються на `OnStateChanged` і реагують самі — патерн **publish/subscribe**.

### 2.2 Ключові скрипти та їхні обов'язки

| Скрипт | Обов'язок |
|--------|-----------|
| `GameStateManager` | Центральний state machine (5 станів) |
| `GameManager` | Глобальна координація: ініціалізація, переходи між раундами, рестарт |
| `GridManager` | Сітка 12×8, конвертація world↔grid, перевірка `CanBuild`, генерація спрайтів трави/каменю |
| `WaveManager` | Спавн ворогів з черги; інтервал 0.8–1.2 с; макс 50 одночасно |
| `AIAttacker` | Алгоритм формування хвилі: спочатку Goblin, з r3 додає Orc, з r5 — Ghost |
| `EconomyManager` | Золото Захисника, бюджет атаки, події `OnGoldChanged` / `OnBudgetChanged` |
| `Base` | HP бази, подія `OnHPChanged` / `OnBaseDestroyed` |
| `EnemyPool` | Об'єктний пул ворогів (Unity `ObjectPool<T>`) |
| `ProjectilePool` | Об'єктний пул снарядів за типом вежі |
| `TowerPlacer` | Обробка кліків миші, розміщення веж, показ радіуса |
| `TowerShooter` | Таргетинг через `OverlapCircle`, кулдаун, спавн снаряда |
| `Projectile` | Політ до цілі, нанесення шкоди (Single / AoE / Slow) |
| `EnemyMover` | Рух waypoint→waypoint через `Vector3.MoveTowards`, `Progress` |
| `EnemyHealth` | HP, анімований HP-бар над ворогом, події смерті |
| `HudUI` | Верхня панель: золото, HP бази, раунд, стан |
| `TowerSidebar` | Ліва панель з 4 кнопками веж + відображення вибраної |
| `StartWaveButton` | Окремий MonoBehaviour для кнопки старту хвилі (бо лямбди не серіалізуються) |
| `GameOverUI` | Екран перемоги/поразки + кнопка рестарту |
| `TowerRangeDisplay` | LineRenderer-коло, що показує радіус дії вежі |
| `AudioBus` | Статичний клас-генератор звуків (без зовнішніх файлів) |
| `SceneBootstrapper` | Створює всі прокедурні спрайти ворогів/веж/снарядів/замку + HP-бари |
| `TDSceneBuilder` *(Editor)* | Авто-генерація `Game.unity` і `MainMenu.unity` з меню `TowerDefense/*` |
| `WebGLBuilder` *(Editor)* | Збірка WebGL у `/docs` для GitHub Pages |

---

## 3. Структура префабів та ScriptableObject-даних

### 3.1 Префаб ворога (`Enemy_Goblin`, `Enemy_Orc`, `Enemy_Ghost`)

```
Enemy_<Type>                       ← root, layer "Enemy"
├── SpriteRenderer (sortingOrder=3)   ← пиксельний спрайт ворога
├── CircleCollider2D (radius=0.42, trigger)
├── [Enemy]                            ← логіка, посилається на EnemyData
├── [EnemyMover]                       ← рух по waypoints
├── [EnemyHealth]                      ← HP, healthBarFill
└── HPCanvas (WorldSpace, sortingOrder=10)
    ├── Frame   (Image — темна рамка)
    ├── BG      (Image — темний фон)
    └── Fill    (Image — кольоровий індикатор HP)
                  ← змінюється через anchorMax.x і color
```

### 3.2 Префаб вежі (`Tower_Archer`, `Tower_Mage`, `Tower_Freezer`, `Tower_Cannon`)

```
Tower_<Type>
├── SpriteRenderer (sortingOrder=2)   ← пиксельний спрайт замкової вежі
├── BoxCollider2D (0.8×0.8, trigger)   ← додається при PlaceTower
├── [Tower]                            ← TowerData reference, gridPos
└── [TowerShooter]                     ← таргетинг + кулдаун
```

### 3.3 Префаб снаряда (`Proj_Archer/Mage/Freezer/Cannon`)

```
Proj_<Type>
├── SpriteRenderer (sortingOrder=4)   ← окремий спрайт: стріла / куля / кристал / ядро
└── [Projectile]                       ← рух до цілі, шкода/AoE/slow
```

### 3.4 ScriptableObject — `TowerData.cs`

```csharp
[CreateAssetMenu]
public class TowerData : ScriptableObject {
    public string towerName;
    public int cost;
    public float fireRate, range, damage;
    public AttackType attackType;   // Single, AoE, Slow
    public float slowFraction;       // 0.5 = -50%
    public float aoeRadius;          // для Маг'а
    public float slowDuration;       // для Льодяника
    public GameObject towerPrefab;
    public GameObject projectilePrefab;
}
```

Створено 4 інстанси: `ArcherData.asset`, `MageData.asset`, `FreezerData.asset`, `CannonData.asset`.

### 3.5 ScriptableObject — `EnemyData.cs`

```csharp
[CreateAssetMenu]
public class EnemyData : ScriptableObject {
    public string enemyName;
    public int cost;                 // вартість для AI-бюджету
    public int maxHP;
    public float moveSpeed;
    public int damageToBase;
    public int goldReward;
    public bool immuneToSlow;        // Привид = true
    public GameObject prefab;
}
```

Інстанси: `GoblinData.asset`, `OrcData.asset`, `GhostData.asset`.

---

## 4. Економіка та формування хвиль

### 4.1 Економіка Захисника
- **Стартове золото** = 300
- **Винагорода за вбивство** = `EnemyData.goldReward` (5 / 12 / 8)
- Витрата при покупці вежі = `TowerData.cost` (100–200g)
- Подія `OnGoldChanged` → HUD і Sidebar реактивно оновлюються; кнопки веж дезактивовуються, якщо золота не вистачає

### 4.2 Бюджет AI-Атакуючого
- **Стартовий бюджет** = 200
- На кожен раунд додається **+50** (`IncreaseBudget()` в `RoundEnd`)
- AI формує чергу ворогів так, щоб сума їхніх `EnemyData.cost` не перевищувала бюджету

### 4.3 Алгоритм `AIAttacker.BuildWave()`

```csharp
public Queue<EnemyData> BuildWave(int round, int budget) {
    var queue = new Queue<EnemyData>();
    int spent = 0;

    while (spent + goblinData.cost <= budget) {
        EnemyData choice;
        float r = UnityEngine.Random.value;
        // r3+: 30% орк, r5+: ще 25% привид, решта — гоблін
        if (round >= 5 && r < 0.25f)      choice = ghostData;
        else if (round >= 3 && r < 0.55f) choice = orcData;
        else                              choice = goblinData;

        if (spent + choice.cost > budget) choice = goblinData;
        queue.Enqueue(choice);
        spent += choice.cost;
    }
    return queue;
}
```

**Зростання складності:**
- Раунди 1–2: лише Гобліни
- Раунди 3–4: Гобліни + Орки
- Раунди 5+: Гобліни + Орки + Привиди

### 4.4 `WaveManager`

```csharp
IEnumerator SpawnRoutine() {
    while (queue.Count > 0 && AliveCount < MAX_ALIVE) {
        var data = queue.Dequeue();
        var e = EnemyPool.Instance.Spawn(data, waypoints[0].position);
        e.Initialize(data, waypoints);
        yield return new WaitForSeconds(Random.Range(0.8f, 1.2f));
    }
}
```

`OnEnemyDefeated()` / `OnEnemyReachedBase()` рахують залишок; коли `aliveCount == 0` і `queue.Count == 0` → `SetState(RoundEnd)`.

---

## 5. Pooling та оптимізація для WebGL

### 5.1 Object Pooling — `EnemyPool` і `ProjectilePool`

Використовується вбудований Unity `ObjectPool<T>` із `UnityEngine.Pool`.

```csharp
_pool[data] = new ObjectPool<Enemy>(
    createFunc:    () => Instantiate(data.prefab).GetComponent<Enemy>(),
    actionOnGet:   e => e.gameObject.SetActive(true),
    actionOnRelease: e => e.gameObject.SetActive(false),
    actionOnDestroy: e => Destroy(e.gameObject),
    defaultCapacity: 15,
    maxSize: 50
);
```

- **Pre-warm** на старті (`EnemyPool.PoolEntry.preWarm`): 15 гоблінів, 8 орків, 8 привидів — фізично створюються в неактивному стані, щоб не було хіт-стопу при першому спавні.
- Снаряди — окремий пул на кожний тип вежі (4 пули).
- При смерті/виході ворога з мапи: `Consume()` → `_pool.Release(this)` повертає об'єкт у пул замість `Destroy()`.
- Захист від подвійного повернення через прапор `_consumed`.

### 5.2 Інші оптимізації під WebGL

| Оптимізація | Реалізація |
|-------------|-----------|
| Sprite atlas | Усі спрайти генеруються процедурно (`Texture2D.SetPixels`) — один невеликий батч на гру |
| `PlayerSettings.WebGL.compressionFormat = Disabled` | Сумісність з GitHub Pages (вони не вміють auto-Brotli) |
| `WebGLExceptionSupport.None` | Прибирає overhead обробки виключень (приблизно −20% розміру) |
| `IL2CPP` scripting backend | Прискорена нативна виконавчість vs Mono |
| `WebGL.memorySize = 256 MB` | Достатньо для 50 ворогів + UI, не марно багато |
| `runInBackground = true` | Вкладка не пауза при втраті фокусу |
| Splash screen off | Швидший старт |
| Procedural audio (`AudioClip.Create`) | Жодних .ogg/.wav файлів — нульовий розмір ассетів аудіо |
| `DontDestroyOnLoad` prefabs | Префаби живуть один раз, не пересоздаються при зміні сцени |
| `[DisallowMultipleComponent]` на Enemy-компонентах | Захист від дублікатів через `RequireComponent` ланцюжки |
| `raycastTarget = false` на декоративних UI | Прибирає overhead GraphicRaycaster для непотрібних елементів |

**Фінальний розмір білда:** ~48 MB (`docs.wasm` 38 MB + `docs.data` 9.2 MB + framework/loader). Час холодного завантаження у браузері — ~5–10 секунд.

---

## 6. Інструкція запуску та збірки

### 6.1 Запуск у Unity
```
1. Відкрити проєкт у Unity Hub: D:\game\TowerDefense2D
2. Версія: Unity 6 (6000.4.8f1)
3. У меню Editor:
     TowerDefense → 1 - Build Game Scene    (генерує Game.unity)
     TowerDefense → 2 - Build Main Menu Scene
4. Запустити ▶ Play
```

### 6.2 Збірка WebGL

**Спосіб А (через меню):**
```
TowerDefense → 3 - Build WebGL (to /docs)
```

**Спосіб Б (командний рядок, для CI/CD):**
```bash
"C:\Program Files\Unity\Hub\Editor\6000.4.8f1\Editor\Unity.exe" \
    -batchmode -nographics -quit \
    -projectPath "D:\game\TowerDefense2D" \
    -executeMethod WebGLBuilder.BuildForPages \
    -logFile webgl_build.log
```

Тривалість на сучасному CPU: **5–12 хвилин** (IL2CPP компіляція — найдовший етап).

### 6.3 Деплой на GitHub Pages
```bash
git add docs/
git commit -m "WebGL build"
git push origin main
```
Pages автоматично оновиться (1–2 хвилини) за адресою:
**https://modertok.github.io/towerDefenseOld/**

Налаштування Pages: гілка `main`, директорія `/docs`.

---

## 7. Перелік ассетів та ШІ-генерація

### 7.1 Зовнішні ассети
**Жодних**. У грі **немає** жодного завантаженого .png / .ogg / .ttf файлу. Усе генерується у коді.

### 7.2 Процедурна генерація спрайтів
Реалізовано в `SceneBootstrapper.cs` через побудову `Texture2D` піксель-за-пікселем (`SetPixels`).

| Сутність | Метод | Розмір | Опис |
|----------|-------|-------:|------|
| Лучник       | `ArcherSprite()`  | 20×30 | замкова вежа з вікнами і стягом |
| Маг          | `MageSprite()`    | 20×30 | фіолетова вежа з зірочкою |
| Льодяник     | `FreezerSprite()` | 20×30 | блакитна вежа з кристалом |
| Гармата      | `CannonSprite()`  | 20×30 | темна вежа з гарматою |
| Гоблін       | `GoblinSprite()`  | 14×16 | зелений людиноподібний |
| Орк          | `OrcSprite()`     | 16×18 | темно-зелений з обладунком |
| Привид       | `GhostSprite()`   | 14×14 | напівпрозорий блакитний |
| Стріла       | `ArrowSprite()`   | 6×16  | дерев'яне древко з вістрям |
| Магічна куля | `OrbSprite()`     | 12×12 | світіння |
| Кристал льоду| `IceSprite()`     | 10×10 | блакитна зірка |
| Гарматне ядро| `BallSprite()`    | 10×10 | чорний шар |
| Замок (база) | `CastleSprite()`  | 40×32 | 3 вежі + брама |
| Трава        | `MakeGrassTile()` | 8×8   | випадковий шум для різноманіття |
| Камінь шляху | `MakeStoneTile()` | 8×8   | бруківка |

**Усі спрайти — авторські, написані вручну в коді з нуля.**

### 7.3 Процедурна генерація аудіо
`AudioBus.cs` створює AudioClip-и сінусом, шумом, sweep'ом і арпеджіо:

| Звук | Спосіб | Тривалість |
|------|--------|-----------:|
| Постріл вежі     | sin 880 Hz + decay | 0.05 с |
| Попадання        | шум + decay | 0.08 с |
| Вибух (AoE)      | шум + sin 80 Hz | 0.30 с |
| Заморозка        | sweep 1400→300 Hz | 0.18 с |
| Смерть ворога    | sweep 500→80 Hz | 0.30 с |
| Підбір золота    | sin 1320 Hz | 0.10 с |
| Удар по базі     | шум | 0.20 с |
| Перемога         | арпеджіо C-E-G-C | 0.48 с |
| Поразка          | арпеджіо G-E-C-G | 0.72 с |

**Усе аудіо — програмно згенероване, ніяких .wav/.ogg.**

### 7.4 Використання ШІ
Розробку допомагав вести **Claude (Anthropic) Sonnet 4.5/4.6** — у форматі парної роботи:
- Архітектурний дизайн (state machine, pool, події)
- Генерація початкового коду компонентів
- Дебаг проблем (наприклад, `[DisallowMultipleComponent]` для усунення дублювання EnemyHealth, перевід HP-бара з `Slider.value` на `anchorMax.x`, фікс блокування кліків через `raycastTarget`)
- Налаштування build-pipeline і GitHub Pages

Фінальний код, дизайн, баланс і всі творчі рішення — **прийняті та перевірені автором проєкту**.

---

## 8. Структура проєкту

```
TowerDefense2D/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/        # GameManager, GameStateManager, AudioBus, SceneBootstrapper
│   │   ├── Grid/        # GridManager, GridCell
│   │   ├── Enemies/     # Enemy, EnemyMover, EnemyHealth, EnemyPool
│   │   ├── Towers/      # Tower, TowerShooter, Projectile, ProjectilePool, TowerPlacer
│   │   ├── Wave/        # WaveManager, AIAttacker
│   │   ├── Economy/     # EconomyManager
│   │   ├── Base/        # Base
│   │   └── UI/          # HudUI, TowerSidebar, GameOverUI, MainMenuController, StartWaveButton
│   ├── Editor/
│   │   ├── TDSceneBuilder.cs    # авто-генерація сцен з меню TowerDefense/*
│   │   ├── WebGLBuilder.cs      # WebGL build у /docs
│   │   └── AutoOpenGameScene.cs # авто-відкриття Game.unity при старті Editor
│   ├── ScriptableObjects/
│   │   ├── TowerData/   # Archer/Mage/Freezer/Cannon .asset
│   │   └── EnemyData/   # Goblin/Orc/Ghost .asset
│   └── Scenes/          # MainMenu.unity, Game.unity
├── docs/                # WebGL build (для GitHub Pages)
├── ProjectSettings/
├── Packages/
├── README.md
└── .gitignore
```
