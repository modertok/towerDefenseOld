# Tower Defense 2D

Класичний Tower Defense у середньовічному стилі. Захищай замок від хвиль ворогів!

## 🎮 Грати онлайн

**[▶ Запустити гру в браузері](https://modertok.github.io/towerDefenseOld/)**

## 🛡 Як грати

1. **Підготовка** — обери вежу з лівої панелі і клікни на зелену клітинку поля
2. Натисни **▶ ПОЧАТИ ХВИЛЮ** у верхній панелі — починається битва
3. Вежі стріляють по ворогах автоматично
4. Лівий клік по вежі → показує її радіус дії
5. Захисти базу 10 раундів — переможи!

## 🏰 Типи веж

| Вежа | Ціна | Атака | Дальність |
|------|------|-------|-----------|
| 🏹 Лучник  | 100g | одна ціль (20 dmg) | 3.0 |
| 🔮 Маг     | 150g | AoE-вибух (30 dmg, r=1.5) | 2.0 |
| ❄ Льодяник | 120g | уповільнення -50% | 3.0 |
| 💣 Гармата | 200g | велика шкода (65 dmg) | 4.5 |

## 👹 Типи ворогів

| Ворог | HP | Швидкість | Особливість |
|-------|----|---------:|-------------|
| Гоблін   | 40  | 3.0 | швидкий, слабкий |
| Орк      | 150 | 1.5 | повільний, міцний |
| Привид   | 80  | 2.0 | імунітет до Льодяника |

## 🛠 Стек

- Unity **6 (6000.4.8f1)**
- WebGL build (IL2CPP, без стискання — сумісно з GitHub Pages)
- New Input System
- Object Pooling для ворогів та снарядів
- ScriptableObjects для TowerData / EnemyData
- Процедурно згенеровані 2D піксельні спрайти і звуки (без зовнішніх ассетів)

## 📂 Структура

```
Assets/
├── Scripts/
│   ├── Core/        # GameManager, GameStateManager, AudioBus, SceneBootstrapper
│   ├── Grid/        # GridManager, GridCell
│   ├── Enemies/     # Enemy, EnemyMover, EnemyHealth, EnemyPool
│   ├── Towers/      # Tower, TowerShooter, Projectile, ProjectilePool, TowerPlacer
│   ├── Wave/        # WaveManager, AIAttacker
│   ├── Economy/     # EconomyManager
│   ├── Base/        # Base
│   └── UI/          # HudUI, TowerSidebar, GameOverUI, MainMenuController, StartWaveButton
├── Editor/
│   ├── TDSceneBuilder.cs   # авто-генерація сцени з меню TowerDefense
│   ├── WebGLBuilder.cs     # WebGL build у /docs для GitHub Pages
│   └── AutoOpenGameScene.cs
└── Scenes/          # MainMenu.unity, Game.unity
```

## 🚀 Локальний запуск

1. Відкрий проект у Unity 6 (6000.4.8f1)
2. У меню: **TowerDefense → 1 - Build Game Scene**
3. **TowerDefense → 2 - Build Main Menu Scene**
4. **▶ Play**

## 🌐 Зібрати WebGL для GitHub Pages

**TowerDefense → 3 - Build WebGL (to /docs)** — зібрати у папку `docs/`, потім commit + push, і GitHub Pages автоматично оновиться.
