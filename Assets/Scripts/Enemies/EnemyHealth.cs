using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]      // ЗАБОРОНЯЄ дублікати компонента
[RequireComponent(typeof(Enemy))]
public class EnemyHealth : MonoBehaviour
{
    [Header("Health Bar — Image (anchorMax-based shrink)")]
    public Image healthBarFill;     // основний індикатор HP
    public Slider healthBar;        // ← застаріле (підтримка старих сцен)

    private float _maxHP;
    private float _currentHP;
    private Enemy _enemy;

    public bool IsDead { get; private set; }

    void Awake()
    {
        _enemy = GetComponent<Enemy>();
        // Якщо посилання загублено (інстанс/пул) — знайти Fill у дереві
        if (healthBarFill == null)
        {
            var fill = transform.Find("HPCanvas/Fill");
            if (fill != null) healthBarFill = fill.GetComponent<Image>();
        }
    }

    public void Initialize(float maxHP)
    {
        _maxHP     = maxHP;
        _currentHP = maxHP;
        IsDead     = false;

        SetVisualRatio(1f);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;
        _currentHP = Mathf.Max(0f, _currentHP - amount);

        float ratio = _maxHP > 0f ? _currentHP / _maxHP : 0f;
        SetVisualRatio(ratio);

        if (_currentHP <= 0f) Die();
    }

    /// Оновлює і ширину, і колір (зелений → жовтий → червоний).
    /// Працює через ANCHORS — найнадійніший спосіб для Image у WorldSpace Canvas.
    void SetVisualRatio(float ratio)
    {
        ratio = Mathf.Clamp01(ratio);

        Color c =
            ratio > 0.6f ? new Color(0.18f, 0.92f, 0.22f) :   // зелений
            ratio > 0.3f ? new Color(0.98f, 0.82f, 0.06f) :   // жовтий
                           new Color(0.95f, 0.18f, 0.10f);    // червоний

        if (healthBarFill != null)
        {
            // Anchor-based: shrinks from right side via anchorMax.x
            var rt = healthBarFill.rectTransform;
            rt.anchorMin    = new Vector2(0f, 0f);
            rt.anchorMax    = new Vector2(ratio, 1f);
            rt.offsetMin    = Vector2.zero;
            rt.offsetMax    = Vector2.zero;
            // Підтримка обох режимів — якщо тип Filled, ще й fillAmount
            healthBarFill.fillAmount = ratio;
            healthBarFill.color      = c;
        }
        if (healthBar != null)
        {
            healthBar.minValue = 0f;
            healthBar.maxValue = 1f;
            healthBar.value    = ratio;
            var fill = healthBar.fillRect != null
                ? healthBar.fillRect.GetComponent<Image>() : null;
            if (fill != null) fill.color = c;
        }
    }

    void Die()
    {
        if (IsDead) return;
        IsDead = true;
        EconomyManager.Instance?.AddGold(_enemy.Data.goldReward);
        WaveManager.Instance?.OnEnemyDefeated();
        AudioBus.PlayEnemyDeath();
        _enemy.Consume();
    }
}
