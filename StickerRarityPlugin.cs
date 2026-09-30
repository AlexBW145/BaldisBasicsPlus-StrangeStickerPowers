using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace StickerPowers;

[BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VERSION)]
[BepInDependency("mtm101.rulerp.bbplus.baldidevapi", "11.0.0.0")]
public class StickerRarityPlugin : BaseUnityPlugin
{
    public const string 
        PLUGIN_GUID = "alexbw145.bbplus.stickerpowers",
        PLUGIN_NAME = "Strange New Sticker Powers",
        PLUGIN_VERSION = "1.0.0.1";
    internal static new ManualLogSource Logger;
    private static readonly AssetManager assets = new AssetManager();
    internal static StickerRarityPlugin Instance { get; private set; }
    internal StickerPowerMetaStorage meta;

    private void Awake()
    {
        Instance = this;
        new Harmony(PLUGIN_GUID).PatchAllConditionals();
        meta = new StickerPowerMetaStorage();
        Logger = base.Logger;
        LoadingEvents.RegisterOnAssetsLoaded(Info, AssetsLoad(), LoadingEventOrder.Start);
        LoadingEvents.RegisterOnAssetsLoaded(Info, PreLoad(), LoadingEventOrder.Pre);
        StickerPowerSave.Instance = new StickerPowerSave(Info);
        ModdedSaveGame.AddSaveHandler(StickerPowerSave.Instance);
    }

    private void LateUpdate() => StickerRarityAnimator.Update();

    private IEnumerator AssetsLoad()
    {
        yield return 2;
        yield return "Loading Assets...";
        var rarities = AssetLoader.TexturesFromFolder(Path.Combine(AssetLoader.GetModPath(this), "Texture2D", "TextureRarityWraps"));
        foreach (var rarity in rarities)
            rarity.wrapMode = TextureWrapMode.Repeat;
        assets.AddRange(rarities);
        AssetLoader.LocalizationFromFunction((lang) => new Dictionary<string, string>
        {
            { "StickerPower_NoPower", "Common" },
            { "StickerPower_Foil", "Foiled" },
            { "StickerPower_Holographic", "Holographic" },
            { "StickerPower_Shiny", "Shiny" },
            { "StickerPower_Handmade", "Handmade" },
            { "StickerPower_Burning", "On Fire" },
            { "StickerPower_Decay", "Decaying" },
            { "Sfx_FireFueled", "[Something burned]" }
        });
        var bundle = AssetBundle.LoadFromFile(Path.Combine(AssetLoader.GetModPath(this), $"stickerpowershader{Application.platform switch
        {
            RuntimePlatform.WindowsPlayer => ".win64",
            RuntimePlatform.OSXPlayer => ".osx",
            RuntimePlatform.LinuxPlayer => ".linux",
            _ => throw new Exception("Is this android or what??")
        }}"));
        var operation = bundle.LoadAllAssetsAsync<Shader>();
        yield return new WaitUntil(() => operation.isDone);
        assets.Add("CustomShader", operation.allAssets.First());
    }

    private IEnumerator PreLoad()
    {
        yield return 5;
        yield return "Initial Setup";
        StickerScreenController stickerController = Resources.FindObjectsOfTypeAll<StickerScreenController>().Last(x => x.GetInstanceID() >= 0);
        var baseMaterial = stickerController.inventoryStickerPrefab.image.material;
        NothingPowerStickerData.defaultMaterial = baseMaterial;
        new StickerPowerBuilder<NothingPowerStickerData>(Info)
            .SetEnum(StickerPower.NoPower)
            .SetMaterial(baseMaterial)
            .MarkVisualAsNotAnimated()
            .Build();
        var shader = assets.Get<Shader>("CustomShader");
        StickerPowerSave.Instance.Reset();
        StickerMetaStorage.Instance.Get(Sticker.InventorySlot).tags.Add("stickerpowers_powerless");
        //StickerMetaStorage.Instance.Get(Sticker.MapRange).tags.Add("stickerpowers_burnless"); // Maybe? I nerfed the power too hard.

        yield return "Creating Sticker Flair - Foil";
        var foilMaterial = Instantiate(baseMaterial);
        foilMaterial.name = "StickerMaterial_Foil";
        foilMaterial.shader = shader;
        foilMaterial.SetTexture("_DetailTex", assets.Get<Texture2D>("FoilStickerTexture"));
        foilMaterial.SetVector("_Tiling", Vector2.one * 0.25f);
        //foilMaterial.SetTextureScale("_DetailTex", Vector2.one * 64f);
        //foilMaterial.SetFloat("_Strength", 1f);
        new StickerPowerBuilder<SimplePowerStickerData>(Info)
            .SetEnum(StickerPower.Foil)
            .SetMaterial(foilMaterial)
            .Build();
        StickerPower.Foil.ApplyChance(0.0235f);

        yield return "Creating Sticker Flair - Holographic";
        var holoMaterial = Instantiate(baseMaterial);
        holoMaterial.name = "StickerMaterial_Holographic";
        holoMaterial.shader = shader;
        holoMaterial.SetTexture("_DetailTex", assets.Get<Texture2D>("HolographicStickerTexture"));
        holoMaterial.SetVector("_Tiling", Vector2.one * 0.5f);
        //holoMaterial.SetTextureScale("_DetailTex", Vector2.one * 64f);
        //holoMaterial.SetFloat("_Strength", 1f);
        new StickerPowerBuilder<SimplePowerStickerData>(Info)
            .SetEnum(StickerPower.Holographic)
            .SetMaterial(holoMaterial)
            .Build();
        StickerPower.Holographic.ApplyChance(0.0025f);

        yield return "Creating Sticker Flair - Shiny";
        var shinyMaterial = Instantiate(baseMaterial);
        shinyMaterial.name = "StickerMaterial_Shiny";
        shinyMaterial.shader = shader;
        shinyMaterial.SetTexture("_DetailTex", assets.Get<Texture2D>("ShinyStickerTexture"));
        shinyMaterial.SetVector("_Tiling", Vector2.one * 2f);
        new StickerPowerBuilder<SimplePowerStickerData>(Info)
            .SetEnum(StickerPower.Shiny)
            .SetMaterial(shinyMaterial)
            .Build();
        StickerPower.Shiny.ApplyChance(0.1f);

        yield return "Creating Sticker Flair - Handmade";
        var handmadeMaterial = Instantiate(baseMaterial);
        handmadeMaterial.name = "StickerMaterial_Handmade";
        handmadeMaterial.shader = shader;
        handmadeMaterial.SetTexture("_DetailTex", assets.Get<Texture2D>("HandmadeStickerTexture"));
        handmadeMaterial.SetVector("_Tiling", Vector2.one * 0.75f);
        new StickerPowerBuilder<SimplePowerStickerData>(Info)
            .SetEnum(StickerPower.Handmade)
            .SetMaterial(handmadeMaterial)
            .MarkVisualAsNotAnimated()
            .Build();
        StickerPower.Handmade.ApplyChance(0.055f);

        yield return "Creating Sticker Flair - On Fire";
        var fireMaterial = Instantiate(baseMaterial);
        fireMaterial.name = "StickerMaterial_Burning";
        fireMaterial.shader = shader;
        fireMaterial.SetTexture("_DetailTex", assets.Get<Texture2D>("BurningStickerTexture"));
        fireMaterial.SetVector("_Tiling", Vector2.one * 4f);
        fireMaterial.SetColor("_Color", new(1f, 1f, 0f));
        new StickerPowerBuilder<BurningPowerStickerData>(Info)
            .SetEnum(StickerPower.Burning)
            .SetMaterial(fireMaterial)
            .Build();
        StickerPower.Burning.ApplyChance(0.085f);
        StickerPowerBurningStateData.burned = AssetFinder.FindOfTypeWithName<SoundObject>("FireFueled", true);

        yield return "Creating Sticker Flair - Decaying";
        var decayMaterial = Instantiate(baseMaterial);
        decayMaterial.name = "StickerMaterial_Decaying";
        decayMaterial.shader = shader;
        decayMaterial.SetTexture("_DetailTex", assets.Get<Texture2D>("DecayingStickerTexture"));
        decayMaterial.SetVector("_Tiling", Vector2.one * 1f);
        new StickerPowerBuilder<DecayingPowerStickerData>(Info)
            .SetEnum(StickerPower.Decay)
            .SetMaterial(decayMaterial)
            .MarkVisualAsNotAnimated()
            .Build();
        StickerPower.Decay.ApplyChance(0.1f);
    }
}

internal class StickerPowerSave(PluginInfo info) : ModdedSaveGameIOBinary
{
    public static StickerPowerSave Instance { get; internal set; }
    public override PluginInfo pluginInfo => info;
    public Dictionary<StickerStateData, StickerPowerStateData> stickerInventory = new Dictionary<StickerStateData, StickerPowerStateData>();
    public StickerPowerStateData[] activeStickerData = new StickerPowerStateData[4];

    internal List<StickerStateData> tracker = new List<StickerStateData>();
    internal StickerStateData[] appliedTracker = new StickerStateData[4];
    public void UpdateCache()
    {
        for (int i = 0; i < StickerManager.Instance.stickerInventory.Count; i++)
        {
            if (!tracker.Contains(StickerManager.Instance.stickerInventory[i]))
                stickerInventory.Add(StickerManager.Instance.stickerInventory[i], StickerPowerMetaStorage.Instance.Get(StickerManager.Instance.stickerInventory[i].GetMeta().value.GetStickerPowerByChance()).value.CreateStateData(StickerManager.Instance.stickerInventory[i]));
        }
        tracker = new(StickerManager.Instance.stickerInventory);
        List<StickerStateData> list = new List<StickerStateData>();
        foreach (var cacheData in stickerInventory)
            if (!StickerManager.Instance.stickerInventory.Contains(cacheData.Key))
                list.Add(cacheData.Key);
        list.ForEach(x => stickerInventory.Remove(x));
    }
    public void UpdateActiveCache()
    {
        StickerStateData[] array = appliedTracker;
        appliedTracker = new StickerStateData[StickerManager.Instance.activeStickerData.Length];
        for (int i = 0; i < StickerManager.Instance.activeStickerData.Length; i++)
        {
            if (array[i] != StickerManager.Instance.activeStickerData[i])
                activeStickerData[i] = StickerPowerMetaStorage.Instance.Get(StickerManager.Instance.activeStickerData[i].GetMeta().value.GetStickerPowerByChance()).value.CreateStateData(StickerManager.Instance.activeStickerData[i]);
            appliedTracker[i] = StickerManager.Instance.activeStickerData[i];
        }
    }
    private bool InventoryCacheStale()
    {
        if (StickerManager.Instance.stickerInventory.Count != tracker.Count)
            return true;
        for (int i = 0; i < StickerManager.Instance.stickerInventory.Count; i++)
        {
            if (tracker[i] != StickerManager.Instance.stickerInventory[i])
                return true;
        }
        return false;
    }
    private bool ActiveCacheStale()
    {
        if (appliedTracker.Length != StickerManager.Instance.activeStickerData.Length)
            return true;
        for (int i = 0; i < StickerManager.Instance.activeStickerData.Length; i++)
        {
            if (appliedTracker[i] != StickerManager.Instance.activeStickerData[i])
                return true;
        }
        return false;
    }
    internal StickerPowerStateData aboutToBeAppliedRarityData;
    internal int aboutToBeAppliedRaritySlot = -1;
    private void StickersUpdated()
    {
        if (aboutToBeAppliedRarityData != null) // Thanks Missed the Texture, although you did create this code first for Arcade Eternity...
        {
            if (ActiveCacheStale())
            {
                activeStickerData[aboutToBeAppliedRaritySlot] = aboutToBeAppliedRarityData.power.CreateStateData(StickerManager.Instance.activeStickerData[aboutToBeAppliedRaritySlot]);
                appliedTracker[aboutToBeAppliedRaritySlot] = StickerManager.Instance.activeStickerData[aboutToBeAppliedRaritySlot];
                UpdateActiveCache();
            }
            aboutToBeAppliedRarityData = null;
            aboutToBeAppliedRaritySlot = -1;
        }
    }
    internal void Update()
    {
        if (InventoryCacheStale())
            UpdateCache();
        if (ActiveCacheStale() && aboutToBeAppliedRarityData == null)
            UpdateActiveCache();
    }

    public override void Load(BinaryReader reader)
    {
        var stickermetastorage = StickerMetaStorage.Instance;
        List<StickerPowerStateData> powers = new List<StickerPowerStateData>();
        for (int i = 0; i < reader.ReadInt32(); i++)
        {
            StickerPower? powerEnum;
            EnumExtensions.GetFromExtendedNameSafe(reader.ReadString(), out powerEnum);
            StickerPowerMetaData data = StickerPowerMetaStorage.Instance.Get(powerEnum.Value);
            PoweredStickerData state = data.value;
            var power = state.CreateStateData(StickerManager.Instance.stickerInventory[i]);
            power.Read(reader);
            powers.Add(power);
        }
        for (int i = 0; i < powers.Count; i++)
            stickerInventory.Add(StickerManager.Instance.stickerInventory[i], powers[i]);
        for (int i = 0; i < reader.ReadInt32(); i++)
        {
            StickerPower? powerEnum;
            EnumExtensions.GetFromExtendedNameSafe(reader.ReadString(), out powerEnum);
            StickerPowerMetaData data = StickerPowerMetaStorage.Instance.Get(powerEnum.Value);
            PoweredStickerData state = data.value;
            var power = state.CreateStateData(StickerManager.Instance.activeStickerData[i]);
            power.Read(reader);
            activeStickerData[i] = power;
        }
    }
    public override void Save(BinaryWriter writer)
    {
        List<StickerPowerStateData> powers = new List<StickerPowerStateData>();
        for (int i = 0; i < StickerManager.Instance.stickerInventory.Count; i++)
            powers.Add(stickerInventory[StickerManager.Instance.stickerInventory[i]]);
        writer.Write(powers.Count);
        for (int i = 0; i < powers.Count; i++)
        {
            writer.Write(powers[i].power.stickerPower.ToStringExtended());
            powers[i].Write(writer);
        }
        writer.Write(activeStickerData.Length);
        for (int i = 0; i < activeStickerData.Length; i++)
        {
            writer.Write(activeStickerData[i].power.stickerPower.ToStringExtended());
            activeStickerData[i].Write(writer);
        }
    }

    public override void Reset()
    {
        StickerManager stickerMan = StickerManager.Instance ?? Resources.FindObjectsOfTypeAll<StickerManager>().Last(x => x.IsPrefab());
        stickerInventory.Clear();
        for (int i = 0; i < activeStickerData.Length; i++)
            activeStickerData[i] = new(StickerPowerMetaStorage.Instance.Get(StickerPower.NoPower).value, stickerMan.activeStickerData[i]);
    }

    public override void OnCGMCreated(CoreGameManager instance, bool isFromSavedGame)
    {
        if (!isFromSavedGame)
            Reset();
        tracker = new List<StickerStateData>();
        appliedTracker = new StickerStateData[4];
        StickerManager.Instance.OnStickerApplied += StickersUpdated;
    }
}

public static class StickerPowerExtensions
{
    internal static readonly Dictionary<StickerPower, float> gambler = new();
    public static void ApplyChance(this StickerPower power, float chance) => gambler.Add(power == StickerPower.NoPower ? throw new OperationCanceledException("StickerPower.NoPower is not considered to be for gambling...") : power, Mathf.Clamp(chance, 0f, 1f));
    public static StickerPower GetStickerPowerByChance(this ExtendedStickerData data)
    {
        if (data.IsInvalidStickerToPowerify())
            return StickerPower.NoPower;
        StickerMetaStorage stickermetastorage = StickerMetaStorage.Instance;
        StickerPowerMetaStorage stickerpowerstorage = StickerPowerMetaStorage.Instance;
        System.Random gamble = new();
        var gambleList = gambler.Keys.ToList();
        gambleList.Shuffle();
        foreach (var power in gambleList)
        {
            var state = stickerpowerstorage.Get(power).value;
            if (!state.IsValid(stickermetastorage.Get(data.sticker).value)) continue;
            if (gamble.NextDouble() < (double)gambler[power])
                return power;
        }
        return StickerPower.NoPower;
    }
    public static bool IsInvalidStickerToPowerify(this ExtendedStickerData data) =>
        data.sticker == Sticker.Nothing || StickerMetaStorage.Instance.Get(data).tags.Contains("stickerpowers_powerless") || data.stickerValueCap <= 2 || data.affectsLevelGeneration || data is ExtendedGluestickData || StickerMetaStorage.Instance.Get(data).tags.Contains("gluestick");

    /*public static StickerPowerStateData GetStateData(this StickerStateData state)
    {
        if (StickerManager.Instance.activeStickerData.Contains(state))
            return StickerPowerSave.Instance.activeStickerData.First(x => x.sticker == state);
        return StickerPowerSave.Instance.stickerInventory.First(x => x.Key == state).Value;
    }*/
}

public class StickerPowerMetaStorage : MetaStorage<StickerPower, StickerPowerMetaData, PoweredStickerData>
{
    public static StickerPowerMetaStorage Instance => StickerRarityPlugin.Instance.meta;

    public override void Add(StickerPowerMetaData toAdd) => metas.Add(toAdd.type, toAdd);

    public override StickerPowerMetaData Get(PoweredStickerData value) => metas.GetValueSafe(value.stickerPower);
}

public class StickerPowerMetaData : IMetadata<PoweredStickerData>
{
    private PoweredStickerData _value;
    public PoweredStickerData value => _value;

    private HashSet<string> _tags = new HashSet<string>();
    public HashSet<string> tags => _tags;

    private PluginInfo _info;
    public PluginInfo info => _info;

    public StickerPower type => _value.stickerPower;

    public StickerPowerMetaData(PluginInfo info, PoweredStickerData sticker)
    {
        _value = sticker;
        _info = info;
    }
}

public class StickerPowerBuilder<PowerData>(PluginInfo info) where PowerData : PoweredStickerData, new()
{
    string stickerEnumName = "";
    StickerPower stickerEnum = StickerPower.NoPower;
    string[] tags = new string[0];
    Material material;
    bool visuallyAnimated = true;

    /// <summary>
    /// Set the Items enum to use for the sticker power.
    /// </summary>
    /// <param name="sticker"></param>
    /// <returns></returns>
    public StickerPowerBuilder<PowerData> SetEnum(StickerPower sticker)
    {
        stickerEnum = sticker;
        stickerEnumName = "";
        return this;
    }

    /// <summary>
    /// Creates a sticker power enum using EnumExtensions with the specified name.
    /// </summary>
    /// <param name="enumToRegister"></param>
    /// <returns></returns>
    public StickerPowerBuilder<PowerData> SetEnum(string enumToRegister)
    {
        stickerEnum = StickerPower.NoPower;
        stickerEnumName = enumToRegister;
        return this;
    }

    /// <summary>
    /// Sets the metadata tags for this sticker power
    /// </summary>
    /// <param name="tags"></param>
    /// <returns></returns>
    public StickerPowerBuilder<PowerData> SetTagsArray(string[] tags)
    {
        this.tags = tags;
        return this;
    }

    /// <summary>
    /// Sets the metadata tags for this sticker power
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    public StickerPowerBuilder<PowerData> SetTags(params string[] tag) => SetTagsArray(tag);

    /// <summary>
    /// Sets the material visually appearing to a sticker that has this power.
    /// </summary>
    /// <param name="material"></param>
    /// <returns></returns>
    public StickerPowerBuilder<PowerData> SetMaterial(Material material)
    {
        this.material = material;
        return this;
    }

    /// <summary>
    /// If the material does not use the shader required for the visual tiling animation, it can be disabled.
    /// </summary>
    /// <returns></returns>
    public StickerPowerBuilder<PowerData> MarkVisualAsNotAnimated()
    {
        visuallyAnimated = false;
        return this;
    }

    public PowerData Build()
    {
        PowerData stickerData = new PowerData();
        if (stickerEnumName == "")
            stickerData.stickerPower = stickerEnum;
        else
            stickerData.stickerPower = EnumExtensions.ExtendEnum<StickerPower>(stickerEnumName);
        if (info != StickerPowerSave.Instance.pluginInfo && stickerData.stickerPower == StickerPower.NoPower) throw new Exception("You must assign an enum to the sticker power!");
        if (visuallyAnimated)
            StickerRarityAnimator.materials.Add(material);
        SimplePowerStickerData.assignedMaterials.Add(stickerData.stickerPower, material);
        StickerPowerMetaData tagsData = new(info, stickerData);
        tagsData.tags.UnionWith(tags);
        StickerPowerMetaStorage.Instance.Add(tagsData);
        return stickerData;
    }
}