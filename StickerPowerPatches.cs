using HarmonyLib;
using MTM101BaldAPI.Registers;
using System.Linq;
using UnityEngine;

namespace StickerPowers;

[HarmonyPatch]
internal static class StickerPowerPatches
{
    [HarmonyPatch(typeof(StickerManager), "StickerValue"), HarmonyPostfix,
        HarmonyAfter("mtm101.rulerp.bbplus.baldidevapi")]
    private static void FoiledAgain(Sticker sticker, ref int __result, StickerManager __instance)
    {
        var activeStickerData = StickerPowerSave.Instance.activeStickerData;
        for (int i = 0; i < activeStickerData.Length; i++)
        {
            if (activeStickerData[i].sticker.sticker == sticker)
            {
                switch (activeStickerData[i].power.stickerPower)
                {
                    case StickerPower.Holographic:
                        __result *= 4;
                        break;
                    case StickerPower.Foil:
                        __result += __instance.activeStickerData.Count(d => d.sticker == sticker);
                        break;
                    case StickerPower.Shiny:
                        __result++;
                        break;
                    case StickerPower.Handmade:
                        __result += Mathf.CeilToInt(__instance.TotalInInventory(sticker) / 3f);
                        break;
                    case StickerPower.Burning:
                        var burning = (StickerPowerBurningStateData)activeStickerData[i];
                        __result += Mathf.RoundToInt((120 - burning.timer) / 10f);
                        break;
                    case StickerPower.Decay:
                        __result += 4;
                        break;
                }
                __result = Mathf.Min(__result, StickerMetaStorage.Instance.Get(sticker).value.stickerValueCap);
            }
        }
    }
    [HarmonyPatch(typeof(StickerManager), "Update"), HarmonyPostfix]
    private static void BurningTime()
    {
        if (BaseGameManager.Instance?.InPitstop() != false) return;
        foreach (var power in StickerPowerSave.Instance.activeStickerData)
        {
            if (power is StickerPowerBurningStateData)
                ((StickerPowerBurningStateData)power).Update();
        }
    }
    [HarmonyPatch(typeof(BaseGameManager), "CollectNotebooks"), HarmonyPrefix, HarmonyPriority(Priority.First)]
    private static void DecayStickerPower(int count)
    {
        var activeStickerData = StickerPowerSave.Instance.activeStickerData;
        for (int i = 0; i < activeStickerData.Length; i++)
        {
            if (activeStickerData[i] is StickerPowerDecayingStateData)
                ((StickerPowerDecayingStateData)activeStickerData[i]).AdvanceCollection(count);
        }
    }
}
