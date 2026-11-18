using Content.Shared._Erida.ShieldPack;
using Content.Shared._Erida.ShieldPack.Components;
using Content.Shared.Coordinates;

namespace Content.Server._Erida.ShieldPack;

public sealed class ShieldPackSystem : SharedShieldPackSystem
{
    protected override void SpawnShieldEffect(Entity<ShieldPackComponent> ent, EntityUid wearer)
    {
        ent.Comp.CurrentShield ??= SpawnAttachedTo(ent.Comp.Shield, wearer.ToCoordinates());
    }

    protected override void DeleteShieldEffect(Entity<ShieldPackComponent> ent)
    {
        QueueDel(ent.Comp.CurrentShield);
        ent.Comp.CurrentShield = null;
    }
}
