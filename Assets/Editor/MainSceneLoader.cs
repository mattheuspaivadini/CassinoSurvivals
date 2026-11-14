using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Deixa o projeto pronto para jogar no editor:
/// - abre Assets/Scenes/Main.unity se o editor estiver numa cena vazia (Untitled);
/// - o botão Play sempre começa pela Main, qualquer que seja a cena aberta.
/// </summary>
[InitializeOnLoad]
static class MainSceneLoader
{
    const string MainScene = "Assets/Scenes/Main.unity";

    static MainSceneLoader()
    {
        // shortcut: força a Main no Play; remova quando o jogo tiver menu/várias cenas.
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScene);
        EditorApplication.delayCall += OpenMainIfUntitled;
    }

    static void OpenMainIfUntitled()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        var active = SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(active.path) && !active.isDirty && AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScene) != null)
            EditorSceneManager.OpenScene(MainScene);
    }

    [MenuItem("Cassino Survivals/Abrir Cena Principal")]
    static void OpenMain()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(MainScene);
    }
}
