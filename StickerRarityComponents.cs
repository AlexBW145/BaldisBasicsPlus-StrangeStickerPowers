using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace StickerPowers;

public enum StickerPower
{
    NoPower = 0,
    Foil,
    Holographic,
    Shiny,
    Handmade,
    Burning,
    Decay
}

[Serializable]
public class StickerPowerStateData(PoweredStickerData power, StickerStateData sticker)
{
    public PoweredStickerData power = power;
    public StickerStateData sticker = sticker;

    protected internal virtual void Write(BinaryWriter writer)
    {
    }
    protected internal virtual void Read(BinaryReader reader)
    {
    }
}
[Serializable]
public class StickerPowerBurningStateData(PoweredStickerData power, StickerStateData sticker, float timer) : StickerPowerStateData(power, sticker)
{
    internal static SoundObject burned;
    internal float timer = timer;
    private int lastValue = StickerManager.Instance?.StickerValue(sticker.sticker) ?? 1;
    internal void Update()
    {
        timer -= (BaseGameManager.Instance?.Ec?.PlayerTimeScale ?? 0) * Time.deltaTime;
        if (timer <= 0f)
        {
            var slot = StickerManager.Instance.activeStickerData.ToList().IndexOf(sticker);
            if (slot == -1) return;
            StickerManager.Instance.ApplySticker(new StickerStateData(Sticker.Nothing, 0, true, false), slot);
            StickerManager.Instance.appliedStickerRemainingNotebooks[slot] = 0;
            CoreGameManager.Instance.audMan.PlaySingle(burned);
        }
        else if (lastValue != StickerManager.Instance.StickerValue(sticker.sticker))
        {
            lastValue = StickerManager.Instance.StickerValue(sticker.sticker);
            StickerManager.Instance.applyStickers = true;
        }
    }

    protected internal override void Write(BinaryWriter writer)
    {
        base.Write(writer);
        writer.Write(timer);
    }
    protected internal override void Read(BinaryReader reader)
    {
        base.Read(reader);
        timer = reader.ReadSingle();
    }
}
[Serializable]
public class StickerPowerDecayingStateData(PoweredStickerData power, StickerStateData sticker) : StickerPowerStateData(power, sticker)
{
    internal int notebooksCollected = 0;
    private const int MAX_NOTEBOOKS = 5;
    public void AdvanceCollection(int count)
    {
        notebooksCollected += count;
        if (notebooksCollected >= MAX_NOTEBOOKS)
        {
            var slot = StickerManager.Instance.activeStickerData.ToList().IndexOf(sticker);
            if (slot == -1) return;
            StickerManager.Instance.ApplySticker(new StickerStateData(Sticker.Nothing, 0, true, false), slot);
            StickerManager.Instance.appliedStickerRemainingNotebooks[slot] = 0;
        }
    }

    protected internal override void Write(BinaryWriter writer)
    {
        base.Write(writer);
        writer.Write(notebooksCollected);
    }
    protected internal override void Read(BinaryReader reader)
    {
        base.Read(reader);
        notebooksCollected = reader.ReadInt32();
    }
}
/// <summary>
/// The base of a sticker power, you won't be using this that much.
/// </summary>
/// <param name="power"></param>
[Serializable]
public abstract class PoweredStickerData(StickerPower power)
{
    public StickerPower stickerPower = power;

    public abstract Material GetMaterial();
    public abstract bool IsValid(ExtendedStickerData data);
    public virtual string GetName() => string.Format(" ({0})", LocalizationManager.Instance.GetLocalizedText($"StickerPower_{EnumExtensions.GetExtendedName<StickerPower>((int)stickerPower)}"));
    public virtual StickerPowerStateData CreateStateData(StickerStateData sticker) => new(this, sticker);
}
[Serializable]
public class NothingPowerStickerData() : PoweredStickerData(StickerPower.NoPower)
{
    internal static Material defaultMaterial;
    public override bool IsValid(ExtendedStickerData data) => false;
    public override Material GetMaterial() => defaultMaterial;
    public override string GetName() => string.Empty;
}
/// <summary>
/// You are mostly gonna use or inherit this data component since it has everything needed for.
/// </summary>
[Serializable]
public class SimplePowerStickerData() : PoweredStickerData(StickerPower.NoPower)
{
    internal static readonly Dictionary<StickerPower, Material> assignedMaterials = new Dictionary<StickerPower, Material>();
    public override Material GetMaterial() => assignedMaterials[stickerPower];
    public override bool IsValid(ExtendedStickerData data) => !data.IsInvalidStickerToPowerify();
}

[Serializable]
public class BurningPowerStickerData() : SimplePowerStickerData()
{
    public override StickerPowerStateData CreateStateData(StickerStateData sticker) => new StickerPowerBurningStateData(this, sticker, UnityEngine.Random.Range(80, 120));
    public override bool IsValid(ExtendedStickerData data) => base.IsValid(data) && !data.sticker.GetMeta().flags.HasFlag(StickerFlags.IsBonus)
        && !data.sticker.GetMeta().tags.Contains("stickerpowers_burnless"); // From the previous math part, Signal Boost sticker slows the game with a high sticker value.
}
[Serializable]
public class DecayingPowerStickerData() : SimplePowerStickerData()
{
    public override StickerPowerStateData CreateStateData(StickerStateData sticker) => new StickerPowerDecayingStateData(this, sticker);
    public override bool IsValid(ExtendedStickerData data) => base.IsValid(data) && !data.sticker.GetMeta().flags.HasFlag(StickerFlags.IsBonus) && BaseGameManager.Instance?.GameMode != GameMode.Endless;
}

internal class StickerPowerShowcaseManager : MonoBehaviour
{
    public Queue<PoweredStickerData> stickerpowerQueue = new Queue<PoweredStickerData>();
    private PoweredStickerData _dequeuedPower;
    public void QueueSticker(PoweredStickerData stickerPowerState)
    {
        stickerpowerQueue.Enqueue(stickerPowerState);
    }
    public Material ShowQueuedSticker()
    {
        if (stickerpowerQueue.Count > 0)
        {
            _dequeuedPower = stickerpowerQueue.Dequeue();
            return _dequeuedPower.GetMaterial();
        }
        return NothingPowerStickerData.defaultMaterial;
    }
}

[Serializable]
public class WeightedStickerPower : WeightedSelection<StickerPower>;