#if TOOLS
using Godot;

namespace NeoTools.LeoEcs.Plugin;

[Tool]
public partial class NeoTools : EditorPlugin
{
    private ComponentContextMenuPlugin? _contextMenu;
    private FsmContextMenuPlugin? _fsmContextMenu;
    private ComponentBakerInspectorPlugin? inspectorPlugin;

    public override void _EnterTree()
    {
        _contextMenu = new ComponentContextMenuPlugin();
        _fsmContextMenu = new FsmContextMenuPlugin();
        inspectorPlugin = new ComponentBakerInspectorPlugin();

        AddInspectorPlugin(inspectorPlugin);
        AddContextMenuPlugin(EditorContextMenuPlugin.ContextMenuSlot.Filesystem, _contextMenu);
        AddContextMenuPlugin(EditorContextMenuPlugin.ContextMenuSlot.Filesystem, _fsmContextMenu);
    }

    public override void _ExitTree()
    {
        RemoveContextMenuPlugin(_contextMenu);
        RemoveContextMenuPlugin(_fsmContextMenu);
        RemoveInspectorPlugin(inspectorPlugin);
    }
}
#endif
