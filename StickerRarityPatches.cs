using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UI;

namespace StickerPowers;

[HarmonyPatch]
internal static class StickerRarityPatches
{
    [HarmonyPatch(typeof(StickerManager), "Update"), HarmonyPostfix]
    private static void UpdatingCache()
    {
        StickerPowerSave.Instance.Update();
    }
    [HarmonyPatch(typeof(StickerManagerExtensions), "AddExistingSticker"), HarmonyPostfix]
    private static void ChanceForPower(this StickerManager me, StickerStateData data, bool animation)
    {
        var state = data.GetMeta().value;
        if (!StickerPowerSave.Instance.stickerInventory.ContainsKey(data))
        {
            var power = StickerPowerMetaStorage.Instance.Get(state.GetStickerPowerByChance()).value;
            StickerPowerSave.Instance.tracker.Add(data);
            StickerPowerSave.Instance.stickerInventory.Add(data, power.CreateStateData(data));
            if (animation)
                CoreGameManager.Instance.GetHud(0).stickerPacketAnimationManager.GetComponent<StickerPowerShowcaseManager>().QueueSticker(power);
        }
        else if (animation)
            CoreGameManager.Instance.GetHud(0).stickerPacketAnimationManager.GetComponent<StickerPowerShowcaseManager>().QueueSticker(StickerPowerSave.Instance.stickerInventory[data].power);
    }
    [HarmonyPatch(typeof(StickerManager), "ApplySticker"), HarmonyPrefix,
        HarmonyBefore("mtm101.rulerp.bbplus.baldidevapi")]
    private static void ApplyPower(StickerStateData sticker, int slot, bool __runOriginal)
    {
        if (!__runOriginal || !StickerPowerSave.Instance.stickerInventory.ContainsKey(sticker))
            return;
        StickerPowerSave.Instance.aboutToBeAppliedRarityData = StickerPowerSave.Instance.stickerInventory[sticker];
        StickerPowerSave.Instance.aboutToBeAppliedRaritySlot = slot;
    }

    [HarmonyPatch(typeof(StickerManager), "RemoveStickerFromInventory"), HarmonyPrefix]
    private static void RemovePower(int inventoryId, StickerManager __instance)
    {
        StickerPowerSave.Instance.stickerInventory.Remove(__instance.stickerInventory[inventoryId]);
        StickerPowerSave.Instance.tracker.Remove(__instance.stickerInventory[inventoryId]);
    }
    [HarmonyPatch(typeof(StickerManager), "ClearAppliedStickers"), HarmonyPostfix]
    private static void ClearAppliedPowers(StickerStateData[] ___activeStickerData, bool[] ___slotUpgraded)
    {
        for (int i = 0; i < StickerPowerSave.Instance.activeStickerData.Length; i++)
            if (StickerPowerSave.Instance.activeStickerData[i].sticker != ___activeStickerData[i])
                StickerPowerSave.Instance.activeStickerData[i] = new(StickerPowerMetaStorage.Instance.Get(StickerPower.NoPower).value, ___activeStickerData[i]);
    }

    [HarmonyPatch(typeof(HudManager), "Awake"), HarmonyPostfix]
    private static void CreateShowcase(StickerPacketAnimationManager ___stickerPacketAnimationManager)
    {
        ___stickerPacketAnimationManager.gameObject.AddComponent<StickerPowerShowcaseManager>();
    }
    [HarmonyPatch(typeof(StickerScreenController), "InitializeStickers"), HarmonyPostfix]
    private static void UpdateMaterials(List<InventorySticker> ___inventoryStickers, Image[] ___appliedStickerImage)
    {
        for (int i = 0; i < ___inventoryStickers.Count; i++)
        {
            var data = StickerPowerSave.Instance.stickerInventory[StickerManager.Instance.stickerInventory[___inventoryStickers[i].inventoryId]];
            ___inventoryStickers[i].image.material = data.power.GetMaterial();
        }
        for (int i = 0; i < ___appliedStickerImage.Length; i++)
        {
            var data = StickerPowerSave.Instance.activeStickerData[i];
            ___appliedStickerImage[i].material = data.power.GetMaterial();
        }
    }
    private static int Organize(StickerScreenController __instance, int num)
    {
        var powerManager = StickerPowerSave.Instance;
        for (int i = 0; i < StickerManager.Instance.stickerInventory.Count; i++)
        {
            if (!StickerManager.Instance.stickerInventory[i].opened) continue;
            bool stackExists = false;
            var sticker = StickerManager.Instance.stickerInventory[i];
            foreach (InventorySticker stickerInventory in __instance.inventoryStickers)
            {
                var inventorySticker = StickerManager.Instance.stickerInventory[stickerInventory.inventoryId];
                if (sticker.sticker == inventorySticker.sticker &&
                    powerManager.stickerInventory[sticker].power.stickerPower == powerManager.stickerInventory[inventorySticker].power.stickerPower)
                {
                    stickerInventory.SetInventoryId(i);
                    stackExists = true;
                }
            }
            if (!stackExists)
            {
                InventorySticker newsticker = GameObject.Instantiate(__instance.inventoryStickerPrefab, __instance.inventoryStickersTransform);
                newsticker.Initialize(__instance, num, i);
                __instance.inventoryStickers.Add(newsticker);
                num++;
            }
        }
        return num;
    }

    [HarmonyPatch(typeof(StickerScreenController), "InitializeStickers"), HarmonyTranspiler]
    private static IEnumerable<CodeInstruction> OrganizeStickers(IEnumerable<CodeInstruction> instructions) => new CodeMatcher(instructions).Start()
        /*.MatchForward(false, new CodeMatch(OpCodes.Ldloc_2)) // next instruction is OpCodes.Brtrue_S WHICH I WAS UNABLE TO SEARCH FOR.
        .ThrowIfInvalid("Uh oh...")
        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldloc_2), new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldloc_1), new CodeInstruction(OpCodes.Ldloc_0),*/
        .MatchForward(false, new CodeMatch(OpCodes.Ldarg_0), new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(StickerScreenController), nameof(StickerScreenController.UpdateStickerInventoryPositions))))
        .ThrowIfInvalid("Uh oh...")
        .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldloc_0),
        Transpilers.EmitDelegate(Organize), new CodeInstruction(OpCodes.Stloc_0))
        .InstructionEnumeration();
    [HarmonyPatch(typeof(StickerScreenController), "UpdateStickerInventoryPositions"), HarmonyPostfix]
    private static void UpdateCount(StickerScreenController __instance)
    {
        for (int i = 0; i < __instance.inventoryStickers.Count; i++)
            __instance.inventoryStickers[i].SetValue(StickerManager.Instance.TotalInInventory(__instance.inventoryStickers[i].Sticker) - StickerPowerSave.Instance.stickerInventory.Count(x =>
            __instance.inventoryStickers[i].Sticker == x.Key.sticker &&
            !(x.Value.power.stickerPower == StickerPowerSave.Instance.stickerInventory[StickerManager.Instance.stickerInventory[__instance.inventoryStickers[i].inventoryId]].power.stickerPower)
            ));
    }

    [HarmonyPatch(typeof(StickerPacketAnimationManager), "ShowQueuedSticker"), HarmonyPrefix]
    private static void ShowPower(StickerPacketAnimationManager __instance)
    {
        var mat = __instance.GetComponent<StickerPowerShowcaseManager>().ShowQueuedSticker();
        __instance.overStickerImage.material = mat;
        __instance.underStickerImage.material = mat;
    }

    [HarmonyPatch(typeof(ExtendedStickerData), "GetLocalizedStickerTitle"), HarmonyPostfix]
    private static void ApplyStatusPower(StickerStateData data, ref string __result)
    {
        PoweredStickerData powerdata;
        if (StickerPowerSave.Instance.appliedTracker.Contains(data))
            powerdata = StickerPowerSave.Instance.activeStickerData[StickerPowerSave.Instance.appliedTracker.ToList().IndexOf(data)].power;
        else
            powerdata = StickerPowerSave.Instance.stickerInventory[data].power;
        __result += powerdata.GetName();
    }
}
