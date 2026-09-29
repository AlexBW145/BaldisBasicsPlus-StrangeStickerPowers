using System.Collections.Generic;
using UnityEngine;

namespace StickerPowers;

internal static class StickerRarityAnimator
{
    internal static HashSet<Material> materials = new HashSet<Material>();
    private static float elaspedTime = 0f;

    public static void Update()
    {
        elaspedTime += 0.1f * Time.unscaledDeltaTime;
        while (elaspedTime > 1f)
            elaspedTime--;
        foreach (var mat in materials)
            mat.SetVector("_TextureOffset", Vector2.one * elaspedTime);
    }
}
