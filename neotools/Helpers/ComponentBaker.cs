using Godot;
using Leopotam.EcsLite;

namespace NeoTools.LeoEcs.Utils;

[GlobalClass]
public abstract partial class ComponentBaker : Resource
{
    public abstract void Bake(EcsWorld world, int entity, Node owner);
}
