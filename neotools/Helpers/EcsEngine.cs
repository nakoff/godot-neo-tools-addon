using Godot;
using Leopotam.EcsLite;

namespace NeoTools.LeoEcs.Utils;

public interface IEcsEngine
{
    EcsWorld? World { get; }
}

[GlobalClass]
public partial class EcsEngine : Node, IEcsEngine
{
    public EcsWorld? World { get; private set; }

    protected EcsSystems? systems;
    protected EcsSystems? physicsSystems;

    public override void _EnterTree()
    {
        World = new EcsWorld();
        systems = new EcsSystems(World);
        physicsSystems = new EcsSystems(World);

        SetProcess(false);
        SetPhysicsProcess(false);
    }

    public override void _Ready()
    {
        if (World == null)
        {
            GD.PrintErr("EcsEngine: World is null");
            return;
        }

        systems?.Init();
        physicsSystems?.Init();

        var entityNodes = GetTree().GetNodesInGroup("EcsEntities");
        GD.Print($"EcsEngine: EntityNodes: {entityNodes.Count}");

        foreach (var entityNode in entityNodes)
        {
            if (entityNode is EntityAuthoring entityAuthoring)
            {
                entityAuthoring.Apply(World);
            }
        }

        SetProcess(true);
        SetPhysicsProcess(true);
    }

    public override void _Process(double delta)
    {
        systems?.Run((float)delta);
    }

    public override void _PhysicsProcess(double delta)
    {
        physicsSystems?.Run((float)delta);
    }

    public override void _ExitTree()
    {
        if (systems != null)
        {
            systems.Destroy();
            systems = null;
        }
        if (physicsSystems != null)
        {
            physicsSystems.Destroy();
            physicsSystems = null;
        }
        if (World != null)
        {
            World.Destroy();
            World = null;
        }
    }
}
