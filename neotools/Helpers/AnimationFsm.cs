using Godot;
using Leopotam.EcsLite;

namespace NeoTools.Utils;

[GlobalClass]
public partial class AnimationFsm : Node
{
    [Export] public bool Debug { get; set; }
    private readonly Fsm _fsm = new();

    public void Init(EcsWorld world, int entity)
    {
        _fsm.Debug = Debug;
        foreach (Node? state in GetChildren())
        {
            if (state is AnimationFsmState animationState)
            {
                if (!animationState.Active)
                {
                    continue;
                }

                _fsm.AddState(animationState.GetState(world, entity));
            }
        }
    }

    public void Tick(float delta)
    {
        _fsm.Tick(delta);
    }
}
