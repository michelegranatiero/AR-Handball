using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine;

public static class BlockUIExtensions
{
    public static bool IsPointOverUIObject(this Vector2 pos)
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            return true;
        }
        return false;
    }
}