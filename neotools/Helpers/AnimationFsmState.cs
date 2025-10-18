using Godot;
using Leopotam.EcsLite;

namespace NeoTools.Utils;

[GlobalClass]
public abstract partial class AnimationFsmState : Node
{
    [Export] public bool Active = true;

    public abstract FsmState GetState(EcsWorld world, int entity);
}

