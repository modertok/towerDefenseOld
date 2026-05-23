using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Автоматично відкриває Game.unity при запуску Unity якщо відкрита SampleScene.
/// </summary>
[InitializeOnLoad]
public static class AutoOpenGameScene
{
    const string GAME_PATH = "Assets/Scenes/Game.unity";

    static AutoOpenGameScene()
    {
        // Відкладаємо виклик — чекаємо поки редактор повністю ініціалізується
        EditorApplication.delayCall += TryOpen;
    }

    static void TryOpen()
    {
        // НІКОЛИ не запускаємо під час гри або компіляції
        if (EditorApplication.isPlaying)    return;
        if (EditorApplication.isCompiling)  return;
        if (!System.IO.File.Exists(GAME_PATH)) return;

        var current = EditorSceneManager.GetActiveScene();
        // Відкриваємо тільки якщо зараз SampleScene або нова (несохранена) сцена
        if (current.name == "SampleScene" || string.IsNullOrEmpty(current.path))
        {
            EditorSceneManager.OpenScene(GAME_PATH);
        }
    }
}
