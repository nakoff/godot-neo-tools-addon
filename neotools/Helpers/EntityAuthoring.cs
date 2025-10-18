using Godot;
using Leopotam.EcsLite;

namespace NeoTools.LeoEcs.Utils;

[GlobalClass]
[Tool]
public partial class EntityAuthoring : Node
{
    [Export] private Godot.Collections.Array<ComponentBaker> components = new();

    public int Entity { get; private set; }

    public override void _EnterTree()
    {
        AddToGroup("EcsEntities");
    }

    public void Apply(EcsWorld world)
    {
        Entity = world.NewEntity();

        foreach (var componentBaker in components)
        {
            componentBaker.Bake(world, Entity, this);
        }
    }
}
