using UnityEngine;

/// <summary>
/// Простий статичний аудіо-бас для гри.
/// Генерує звуки процедурно (без external assets) і програє через AudioSource.
/// Автоматично створюється при першому виклику.
/// </summary>
public static class AudioBus
{
    static AudioSource _src;
    static AudioClip _shot, _hit, _explosion, _freeze, _death, _gold, _baseHit, _victory, _defeat;

    static void EnsureInit()
    {
        if (_src != null) return;
        var go = new GameObject("AudioBus");
        Object.DontDestroyOnLoad(go);
        _src = go.AddComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.spatialBlend = 0f; // 2D

        _shot      = MakeBlip   (frequency:880,  duration:0.05f, decay:25f,  volume:0.35f);
        _hit       = MakeNoise  (                duration:0.08f, decay:18f,  volume:0.4f);
        _explosion = MakeExplosion(             duration:0.30f);
        _freeze    = MakeSweep  (startF:1400, endF:300, duration:0.18f,      volume:0.35f);
        _death     = MakeSweep  (startF:500,  endF:80,  duration:0.30f,      volume:0.45f);
        _gold      = MakeBlip   (frequency:1320, duration:0.10f, decay:12f,  volume:0.30f);
        _baseHit   = MakeNoise  (                duration:0.20f, decay:6f,   volume:0.55f);
        _victory   = MakeArpeggio(new[]{523,659,784,1047}, durationEach:0.12f, volume:0.5f);
        _defeat    = MakeArpeggio(new[]{392,330,262,196},  durationEach:0.18f, volume:0.5f);
    }

    // ── Публічні методи ───────────────────────────────────────────────────
    public static void PlayShot()        { EnsureInit(); _src.PlayOneShot(_shot); }
    public static void PlayHit()         { EnsureInit(); _src.PlayOneShot(_hit); }
    public static void PlayExplosion()   { EnsureInit(); _src.PlayOneShot(_explosion); }
    public static void PlayFreeze()      { EnsureInit(); _src.PlayOneShot(_freeze); }
    public static void PlayEnemyDeath()  { EnsureInit(); _src.PlayOneShot(_death); }
    public static void PlayGold()        { EnsureInit(); _src.PlayOneShot(_gold); }
    public static void PlayBaseHit()     { EnsureInit(); _src.PlayOneShot(_baseHit); }
    public static void PlayVictory()     { EnsureInit(); _src.PlayOneShot(_victory); }
    public static void PlayDefeat()      { EnsureInit(); _src.PlayOneShot(_defeat); }

    // ══════════════════════════════════════════════════════════════════════
    // Процедурна генерація AudioClip
    // ══════════════════════════════════════════════════════════════════════

    const int SR = 22050;

    /// Короткий синусоїдний "blip" з експоненціальним затуханням.
    static AudioClip MakeBlip(float frequency, float duration, float decay, float volume)
    {
        int n = Mathf.Max(1, (int)(SR * duration));
        var data = new float[n];
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            float env = Mathf.Exp(-decay * t);
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * env * volume;
        }
        var c = AudioClip.Create("blip", n, 1, SR, false);
        c.SetData(data, 0);
        return c;
    }

    /// Шум з затуханням (для попадань/ударів).
    static AudioClip MakeNoise(float duration, float decay, float volume)
    {
        int n = Mathf.Max(1, (int)(SR * duration));
        var data = new float[n];
        var rnd = new System.Random(42);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            float env = Mathf.Exp(-decay * t);
            data[i] = ((float)rnd.NextDouble() * 2f - 1f) * env * volume;
        }
        var c = AudioClip.Create("noise", n, 1, SR, false);
        c.SetData(data, 0);
        return c;
    }

    /// Вибух — шум + низькочастотна синусоїда.
    static AudioClip MakeExplosion(float duration)
    {
        int n = Mathf.Max(1, (int)(SR * duration));
        var data = new float[n];
        var rnd = new System.Random(7);
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            float env = Mathf.Exp(-8f * t);
            float noise = ((float)rnd.NextDouble() * 2f - 1f);
            float boom  = Mathf.Sin(2f * Mathf.PI * 80f * t);
            data[i] = (noise * 0.6f + boom * 0.5f) * env * 0.55f;
        }
        var c = AudioClip.Create("explosion", n, 1, SR, false);
        c.SetData(data, 0);
        return c;
    }

    /// Лінійний "sweep" по частоті (для freeze / death).
    static AudioClip MakeSweep(float startF, float endF, float duration, float volume)
    {
        int n = Mathf.Max(1, (int)(SR * duration));
        var data = new float[n];
        float phase = 0f;
        for (int i = 0; i < n; i++)
        {
            float t = i / (float)SR;
            float u = t / duration;
            float f = Mathf.Lerp(startF, endF, u);
            phase += 2f * Mathf.PI * f / SR;
            float env = (1f - u) * Mathf.Exp(-1.5f * u);
            data[i] = Mathf.Sin(phase) * env * volume;
        }
        var c = AudioClip.Create("sweep", n, 1, SR, false);
        c.SetData(data, 0);
        return c;
    }

    /// Послідовність нот (арпеджіо) для перемоги/поразки.
    static AudioClip MakeArpeggio(int[] freqs, float durationEach, float volume)
    {
        int perNote = (int)(SR * durationEach);
        int total = perNote * freqs.Length;
        var data = new float[total];
        for (int n = 0; n < freqs.Length; n++)
        {
            for (int i = 0; i < perNote; i++)
            {
                float t = i / (float)SR;
                float env = Mathf.Exp(-5f * t);
                data[n * perNote + i] =
                    Mathf.Sin(2f * Mathf.PI * freqs[n] * t) * env * volume;
            }
        }
        var c = AudioClip.Create("arp", total, 1, SR, false);
        c.SetData(data, 0);
        return c;
    }
}
