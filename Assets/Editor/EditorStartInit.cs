using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public class EditorStartInit
{
    static EditorStartInit()
    {
        // Build Settings의 0번째 씬(가장 첫 번째 씬)을 시작 씬으로 설정
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(EditorBuildSettings.scenes[0].path);
        EditorSceneManager.playModeStartScene = sceneAsset;
        Debug.Log(EditorBuildSettings.scenes[0].path + " 씬이 에디터 플레이 모드 시작 씬으로 지정됨");
    }
}
