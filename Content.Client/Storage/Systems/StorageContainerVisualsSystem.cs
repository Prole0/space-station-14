using Content.Client.Items.Systems;
using Content.Client.Storage.Components;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Rounding;
using Content.Shared.Storage;
using Robust.Client.GameObjects;

namespace Content.Client.Storage.Systems;

/// <inheritdoc cref="StorageContainerVisualsComponent"/>
public sealed class StorageContainerVisualsSystem : VisualizerSystem<StorageContainerVisualsComponent>
{
    [Dependency] private readonly ItemSystem _itemSystem = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StorageContainerVisualsComponent, GetInhandVisualsEvent>(OnGetHeldVisuals);
    }

    protected override void OnAppearanceChange(EntityUid uid, StorageContainerVisualsComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, StorageVisuals.StorageUsed, out var used, args.Component))
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, StorageVisuals.Capacity, out var capacity, args.Component))
            return;

        var fraction = used / (float)capacity;

        if (!SpriteSystem.LayerMapTryGet((uid, args.Sprite), component.FillLayer, out var fillLayer, false))
            return;

        var closestFillSprite = ContentHelpers.RoundToLevels(fraction, 1, component.MaxFillLevels + 1);

        if (closestFillSprite > 0)
        {
            if (component.FillBaseName == null)
                return;

            SpriteSystem.LayerSetVisible((uid, args.Sprite), fillLayer, true);
            var stateName = component.FillBaseName + closestFillSprite;
            SpriteSystem.LayerSetRsiState((uid, args.Sprite), fillLayer, stateName);
        }
        else
        {
            SpriteSystem.LayerSetVisible((uid, args.Sprite), fillLayer, false);
        }

        _itemSystem.VisualsChanged(uid);
    }

    private void OnGetHeldVisuals(EntityUid uid, StorageContainerVisualsComponent component, GetInhandVisualsEvent args)
    {
        if (component.InHandsFillBaseName == null)
            return;

        if (!TryComp(uid, out AppearanceComponent? appearance))
            return;

        if (!TryComp<ItemComponent>(uid, out var item))
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, StorageVisuals.StorageUsed, out var used, appearance))
            return;

        if (!AppearanceSystem.TryGetData<int>(uid, StorageVisuals.Capacity, out var capacity, appearance))
            return;

        var fraction = used / (float)capacity;

        var closestFillSprite = ContentHelpers.RoundToLevels(fraction, 1, component.InHandsMaxFillLevels + 1);

        if (closestFillSprite > 0)
        {
            var layer = new PrototypeLayerData();

            var heldPrefix = item.HeldPrefix == null ? "inhand-" : $"{item.HeldPrefix}-inhand-";
            var key = heldPrefix + args.Location.ToString().ToLowerInvariant() + component.InHandsFillBaseName + closestFillSprite;

            layer.State = key;

            args.Layers.Add((key, layer));
        }
    }
}
