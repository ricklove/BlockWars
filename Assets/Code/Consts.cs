using UnityEngine;
using System.Collections;

public static class Consts
{
    public static int ignoreRaycastLayerNumber = 2;
    public static int gameLayerNumber = 8;
    public static int editorLayerNumber = 9;
    public static int editorBackgroundLayerNumber = 10;
    public static int mouseHeightPlaneLayerNumber = 11;

    public static void SetLayer(GameObject parent, int layer)
    {
        parent.layer = layer;

        foreach (Transform childTransform in parent.transform)
        {
            SetLayer(childTransform.gameObject, layer);
        }
    }
}
