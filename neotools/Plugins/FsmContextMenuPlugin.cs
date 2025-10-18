#if TOOLS
using System.IO;
using Godot;
using FileAccess = Godot.FileAccess;

namespace NeoTools.LeoEcs.Plugin;

public partial class FsmContextMenuPlugin : EditorContextMenuPlugin
{
    private AcceptDialog? _stateMachineDialog;
    private AcceptDialog? _stateDialog;
    private LineEdit? _stateMachineNameInput;
    private LineEdit? _stateNameInput;
    private string? _currentPath;

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

        PopupMenu popupMenu = new();
        popupMenu.AddItem("Create StateMachine", 0);
        popupMenu.AddItem("Create StateMachineState", 1);
        popupMenu.IdPressed += OnFsmSubmenuOption;

        AddContextSubmenuItem("FSM", popupMenu);
    }

    private void OnFsmSubmenuOption(long id)
    {
        switch (id)
        {
            case 0:
                ShowStateMachineDialog();
                break;
            case 1:
                ShowStateDialog();
                break;
        }
    }

    private void ShowStateMachineDialog()
    {
        if (_stateMachineDialog == null)
        {
            _stateMachineDialog = new AcceptDialog
            {
                Title = "Create StateMachine",
                Size = new Vector2I(400, 150),
                OkButtonText = "Create"
            };

            VBoxContainer container = new();
            container.AddThemeConstantOverride("separation", 10);

            Label label = new()
            {
                Text = "Enter StateMachine name:"
            };
            container.AddChild(label);

            _stateMachineNameInput = new LineEdit
            {
                PlaceholderText = "Player"
            };
            container.AddChild(_stateMachineNameInput);

            _stateMachineDialog.AddChild(container);
            _stateMachineDialog.Confirmed += OnStateMachineDialogConfirmed;

            EditorInterface.Singleton.GetBaseControl().AddChild(_stateMachineDialog);
        }

        _stateMachineNameInput.Text = "";
        _stateMachineDialog.PopupCentered();
        _stateMachineNameInput.GrabFocus();
    }

    private void ShowStateDialog()
    {
        if (_stateDialog == null)
        {
            _stateDialog = new AcceptDialog
            {
                Title = "Create StateMachineState",
                Size = new Vector2I(400, 150),
                OkButtonText = "Create"
            };

            VBoxContainer container = new();
            container.AddThemeConstantOverride("separation", 10);

            Label label = new()
            {
                Text = "Enter State name:"
            };
            container.AddChild(label);

            _stateNameInput = new LineEdit
            {
                PlaceholderText = "Idle"
            };
            container.AddChild(_stateNameInput);

            _stateDialog.AddChild(container);
            _stateDialog.Confirmed += OnStateDialogConfirmed;

            EditorInterface.Singleton.GetBaseControl().AddChild(_stateDialog);
        }

        _stateNameInput.Text = "";
        _stateDialog.PopupCentered();
        _stateNameInput.GrabFocus();
    }

    private void OnStateMachineDialogConfirmed()
    {
        string name = _stateMachineNameInput.Text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            GD.PrintErr("StateMachine name cannot be empty!");
            return;
        }

        CreateStateMachineFile(name);
    }

    private void OnStateDialogConfirmed()
    {
        string name = _stateNameInput.Text.Trim();

        if (string.IsNullOrEmpty(name))
        {
            GD.PrintErr("State name cannot be empty!");
            return;
        }

        CreateStateFile(name);
    }

    private void CreateStateMachineFile(string name)
    {
        string fileName = $"{name}StateMachine.cs";
        string fullPath = Path.Combine(ProjectSettings.GlobalizePath(_currentPath), fileName);

        if (FileAccess.FileExists(fullPath))
        {
            GD.PrintErr($"File {fileName} already exists!");
            return;
        }

        string template = $@"using Godot;

namespace NeoTools.Utils;

[GlobalClass]
public partial class {name}StateMachine : AnimationFsm
{{
}}
";

        try
        {
            using FileAccess file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PrintErr($"Failed to create file: {FileAccess.GetOpenError()}");
                return;
            }

            _ = file.StoreString(template);
            GD.Print($"StateMachine created: {_currentPath}/{fileName}");

            EditorInterface.Singleton.GetResourceFilesystem().Scan();
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"Failed to create StateMachine: {ex.Message}");
        }
    }

    private void CreateStateFile(string name)
    {
        string fileName = $"{name}FsmState.cs";
        string fullPath = Path.Combine(ProjectSettings.GlobalizePath(_currentPath), fileName);

        if (FileAccess.FileExists(fullPath))
        {
            GD.PrintErr($"File {fileName} already exists!");
            return;
        }

        string template = $@"using Godot;

namespace NeoTools.Utils;

[GlobalClass]
public abstract partial class {name}FsmState : AnimationFsmState
{{
}}
";

        try
        {
            using FileAccess file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Write);
            if (file == null)
            {
                GD.PrintErr($"Failed to create file: {FileAccess.GetOpenError()}");
                return;
            }

            _ = file.StoreString(template);
            GD.Print($"State created: {_currentPath}/{fileName}");

            EditorInterface.Singleton.GetResourceFilesystem().Scan();
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"Failed to create State: {ex.Message}");
        }
    }
}
#endif
