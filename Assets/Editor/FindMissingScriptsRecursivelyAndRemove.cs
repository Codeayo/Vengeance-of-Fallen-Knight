using UnityEditor;
using UnityEngine;

public class FindMissingScripts : EditorWindow
{
    private static int goCount;
    private static int componentsCount;
    private static int missingCount;

    [MenuItem("Tools/Find & Remove Missing Scripts")]
    public static void ShowWindow()
    {
        GetWindow(typeof(FindMissingScripts), false, "Missing Script Finder");
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Scan Selected Objects"))
        {
            FindInSelected();
        }

        GUILayout.Space(10);

        if (goCount > 0)
        {
            EditorGUILayout.LabelField("GameObjects Scanned:", goCount.ToString());
            EditorGUILayout.LabelField("Components Checked:", componentsCount.ToString());
            EditorGUILayout.LabelField("Missing Scripts Removed:", missingCount.ToString());
        }
    }

    private static void FindInSelected()
    {
        GameObject[] selectedObjects = Selection.gameObjects;

        goCount = 0;
        componentsCount = 0;
        missingCount = 0;

        foreach (GameObject go in selectedObjects)
        {
            FindInGameObject(go);
        }

        Debug.Log($"Finished scanning! {goCount} objects, {componentsCount} components, {missingCount} missing scripts removed.");

        AssetDatabase.SaveAssets();
    }

    private static void FindInGameObject(GameObject go)
    {
        goCount++;

        Component[] components = go.GetComponents<Component>();
        SerializedObject serializedObject = new SerializedObject(go);
        SerializedProperty prop = serializedObject.FindProperty("m_Component");

        int removedCount = 0;

        for (int i = 0; i < components.Length; i++)
        {
            componentsCount++;

            if (components[i] == null)
            {
                missingCount++;
                prop.DeleteArrayElementAtIndex(i - removedCount);
                removedCount++;
            }
        }

        serializedObject.ApplyModifiedProperties();

        foreach (Transform child in go.transform)
        {
            FindInGameObject(child.gameObject);
        }
    }
}
