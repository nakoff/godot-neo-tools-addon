#if TOOLS
using System.IO;
using Godot;
using FileAccess = Godot.FileAccess;

namespace NeoTools.LeoEcs.Plugin;

public partial class ComponentContextMenuPlugin : EditorContextMenuPlugin
{
    private AcceptDialog _dialog;
    private LineEdit _nameInput;
    private string _currentPath;

    public override void _PopupMenu(string[] paths)
    {
        if (paths.Length > 0)
        {
            _currentPath = paths[0];

            if (Godot.FileAccess.FileExists(ProjectSettings.GlobalizePath(_currentPath)))
            {
                _currentPath = _currentPath.GetBaseDir();
            }
        }
        else
        {
            _currentPath = "res://";
        }

        var createComponentAction = new System.Action<Godot.Collections.Array>((args) => { ShowInputDialog(); });
        AddContextMenuItem("Create Component", Callable.From<Godot.Collections.Array>(createComponentAction));
    }

    private void ShowInputDialog()
    {
        if (_dialog == null)
        {
            _dialog = new AcceptDialog();
            _dialog.Title = "Create Component";
            _dialog.Size = new Vector2I(400, 150);
            _dialog.OkButtonText = "Create";

            var container = new VBoxContainer();
            container.AddThemeConstantOverride("separation", 10);

            var label = new Label();
            label.Text = "Enter component name:";
            container.AddChild(label);

            _nameInput = new LineEdit();
            _nameInput.PlaceholderText = "MyComponent";
            container.AddChild(_nameInput);

            _dialog.AddChild(container);
            _dialog.Confirmed += OnDialogConfirmed;

            EditorInterface.Singleton.GetBaseControl().AddChild(_dialog);
        }

        _nameInput.Text = "";
        _dialog.PopupCentered();
        _nameInput.GrabFocus();
    }

    private void OnDialogConfirmed()
    {
        string componentName = _nameInput.Text.Trim();

        if (string.IsNullOrEmpty(componentName))
        {
            GD.PrintErr("Component name cannot be empty!");
            return;
        }

        if (!componentName.EndsWith("Component"))
        {
            componentName += "Component";
        }

        CreateComponentFile(componentName);
    }

    private void CreateComponentFile(string componentName)
    {
        string fileName = $"{componentName}Baker.cs";
        string fullPath = Path.Combine(ProjectSettings.GlobalizePath(_currentPath), fileName);

        if (FileAccess.FileExists(fullPath))
        {
            GD.PrintErr($"File {fileName} already exists!");
            return;
        }

        string template = $@"using Godot;
using Leopotam.EcsLite;
using NeoTools.LeoEcs.Utils;

namespace Game.Ecs;

public struct {componentName}
{{
    public int Value;
    public Node Node;
}}

[GlobalClass]
public partial class {componentName}Baker : ComponentBaker
{{
    [Export] public int Value;
    [Export(PropertyHint.NodePathValidTypes, ""Node"")] public NodePath? NodePath;

    public override void Bake(EcsWorld world, int entity, Node owner)
    {{
        var addedStash = world.GetPool<AddedMarkerComponent>();
        if (!addedStash.Has(entity)) addedStash.Add(entity);

        var stash = world.GetPool<{componentName}>();
        ref var component = ref stash.Add(entity);

        component.Value = Value;
        component.Node = owner.GetNode<Node>(NodePath);
    }}
}}
";

        try
        {
            using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PrintErr($"Failed to create file: {FileAccess.GetOpenError()}");
                return;
            }

            file.StoreString(template);
            GD.Print($"Component created: {_currentPath}/{fileName}");

            EditorInterface.Singleton.GetResourceFilesystem().Scan();
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"Failed to create component: {ex.Message}");
        }
    }
}
#endif
