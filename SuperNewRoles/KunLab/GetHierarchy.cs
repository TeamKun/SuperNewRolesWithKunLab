using System.Linq;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

public class GetHierarchy
{
    public static string GetHierarchyText()
    {
        GameObject[] allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
        List<GameObject> rootObjects = new List<GameObject>();
        foreach (GameObject obj in allObjects)
        {
            if (obj.transform.parent == null)
            {
                rootObjects.Add(obj);
            }
        }
        string hierarchy = "";

        foreach (GameObject rootGameObject in rootObjects)
        {
            hierarchy += PrintHierarchy(rootGameObject.transform, "");
        }

        return hierarchy;
    }

    private static string PrintHierarchy(Transform transform, string indentation)
    {
        string hierarchy = transform.parent == null ?  indentation +  transform.name + "\n" : indentation + "L " + transform.name + "\n";

        // only iterate through immediate children
        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i);
            hierarchy += PrintHierarchy(child, indentation + "  ");
        }

        return hierarchy;
    }
}