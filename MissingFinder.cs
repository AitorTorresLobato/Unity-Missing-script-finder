using UnityEditor;
using UnityEngine;

using System.Collections.Generic;

public class MissingFinder : EditorWindow
{
    private string scriptNameToFind = ""; // Name of the script to search for
    private bool includeInactive = true;

    [MenuItem("Tools/Missing Finder/Open Window")]
    public static void ShowWindow()
    {
        GetWindow<MissingFinder>("Missing Scripts");
    }

    private void OnGUI()
    {
        GUILayout.Label("Missing Script Finder", EditorStyles.boldLabel);
        GUILayout.Space(10);

        includeInactive = EditorGUILayout.Toggle("Include inactive objects", includeInactive);

        GUILayout.Space(10);
        if (GUILayout.Button("Find And Select Missing Scripts"))
        {
            FindAndSelectMissingScripts();
        }
        GUILayout.Space(5);
        if (GUILayout.Button("Find Missing Scripts (log only)"))
        {
            FindMissingScripts();
        }
        GUILayout.Space(5);
        if (GUILayout.Button("Remove Missing Scripts (with Undo)"))
        {
            if (EditorUtility.DisplayDialog(
                "Remove Missing Scripts",
                "This removes all 'Missing' components from the open scene. Can be undone with Ctrl+Z. Continue?",
                "Yes, Remove", "Cancel"))
            {
                RemoveMissingScripts();
            }
        }

        GUILayout.Space(20);
        GUILayout.Label("Find component by class name", EditorStyles.boldLabel);
        scriptNameToFind = EditorGUILayout.TextField("Script name", scriptNameToFind);
        if (GUILayout.Button("Find & Select Scripts"))
        {
            FindAndSelectAllScriptsInScene(scriptNameToFind);
        }
    }

    List<GameObject> GetAllGameObjects()
    {
        // FindObjectsOfType<GameObject>() does NOT include inactive objects in the hierarchy by
        // default. We walk the scene roots ourselves so inactive objects can be included too
        // (the "include inactive objects" option).
        var result = new List<GameObject>();
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots)
        {
            AddWithChildren(root, result);
        }
        return result;
    }

    void AddWithChildren(GameObject go, List<GameObject> list)
    {
        if (!includeInactive && !go.activeInHierarchy)
            return;
        list.Add(go);
        foreach (Transform child in go.transform)
            AddWithChildren(child.gameObject, list);
    }

    private void FindMissingScripts()
    {
        Debug.Log("Object with Missing Script: ");
        var allObjects = GetAllGameObjects();
        int total = 0;

        foreach (GameObject obj in allObjects)
        {
            Component[] components = obj.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component == null)
                {
                    Debug.Log("Object with Missing Script: " + obj.name, obj);
                    total++;
                }
            }
        }

        Debug.Log(total == 0
            ? "No missing scripts found in the scene."
            : "Total missing scripts found: " + total);
    }

    private void FindAndSelectAllScriptsInScene(string scriptName)
    {
        if (string.IsNullOrEmpty(scriptName))
        {
            Debug.LogWarning("Type a script name before searching.");
            return;
        }

        var allObjects = GetAllGameObjects();
        List<GameObject> objectsWithScript = new List<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            MonoBehaviour[] components = obj.GetComponents<MonoBehaviour>();

            foreach (MonoBehaviour component in components)
            {
                if (component != null && component.GetType().Name == scriptName)
                {
                    objectsWithScript.Add(obj);
                    break;
                }
            }
        }

        if (objectsWithScript.Count > 0)
        {
            Debug.Log("Objects with script '" + scriptName + "': " + objectsWithScript.Count);
            Selection.objects = objectsWithScript.ToArray();
        }
        else
        {
            Debug.Log("No objects with script '" + scriptName + "' found in the scene.");
        }
    }

    private void FindAndSelectMissingScripts()
    {
        var allObjects = GetAllGameObjects();
        var withMissing = new List<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            Component[] components = obj.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component == null)
                {
                    Debug.Log("Object with Missing Script: " + obj.name, obj);
                    withMissing.Add(obj);
                    break;
                }
            }
        }

        // Selects ALL objects with missing scripts (previously only the last one stayed selected).
        if (withMissing.Count > 0)
            Selection.objects = withMissing.ToArray();
        else
            Debug.Log("No missing scripts found in the scene.");
    }

    private void RemoveMissingScripts()
    {
        var allObjects = GetAllGameObjects();
        int totalRemoved = 0;

        foreach (GameObject obj in allObjects)
        {
            Undo.RegisterCompleteObjectUndo(obj, "Remove Missing Scripts");
            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
            totalRemoved += removed;
        }

        Debug.Log("Missing scripts removed: " + totalRemoved);
    }
}
