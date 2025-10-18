#if TOOLS
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NeoTools.LeoEcs.Utils;

namespace NeoTools.LeoEcs.Plugin;

public partial class ComponentBakerInspectorPlugin : EditorInspectorPlugin
{
    public override bool _CanHandle(GodotObject @object)
    {
        bool canHandle = @object is EntityAuthoring;
        return canHandle;
    }

    public override bool _ParseProperty(GodotObject @object, Variant.Type type, string name,
        PropertyHint hintType, string hintString, PropertyUsageFlags usageFlags, bool wide)
    {
        if (name == "components")
        {
            AddPropertyEditor(name, new ComponentBakerAddButtonEditor());
            return false; // Позволяем стандартному инспектору отображать массив
        }
        return false;
    }
}

public partial class ComponentBakerAddButtonEditor : EditorProperty
{
    private Button addButton;
    private PopupPanel popup;
    private ItemList componentList;
    private LineEdit searchField;
    private List<Type> availableComponentTypes;
    private AcceptDialog errorDialog;

    public ComponentBakerAddButtonEditor()
    {
        // Создаем кнопку для добавления компонентов
        addButton = new Button
        {
            Text = "Add Component Baker",
            Icon = GetThemeIcon("Add", "EditorIcons")
        };
        addButton.Pressed += OnAddButtonPressed;
        AddChild(addButton);

        // Создаем попап для выбора компонента
        CreatePopup();

        // Создаем диалог для ошибок
        CreateErrorDialog();

        // Кэшируем доступные типы компонентов
        CacheAvailableComponents();

        SetBottomEditor(addButton);
    }

    private void CacheAvailableComponents()
    {
        availableComponentTypes = new List<Type>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            try
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsSubclassOf(typeof(ComponentBaker)) &&
                        !type.IsAbstract &&
                        type.GetCustomAttributes(typeof(GlobalClassAttribute), false).Length > 0)
                    {
                        availableComponentTypes.Add(type);
                    }
                }
            }
            catch { }
        }

        availableComponentTypes = availableComponentTypes.OrderBy(t => t.Name).ToList();
    }

    private void CreatePopup()
    {
        popup = new PopupPanel();
        AddChild(popup);

        var popupContainer = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(300, 400)
        };
        popup.AddChild(popupContainer);

        // Поле поиска
        searchField = new LineEdit
        {
            PlaceholderText = "Search component bakers..."
        };
        searchField.TextChanged += OnSearchTextChanged;
        popupContainer.AddChild(searchField);

        // Список компонентов
        componentList = new ItemList
        {
            SizeFlagsVertical = Control.SizeFlags.ExpandFill
        };
        componentList.ItemActivated += OnComponentSelected;
        popupContainer.AddChild(componentList);
    }

    private void CreateErrorDialog()
    {
        errorDialog = new AcceptDialog();
        errorDialog.Title = "Error";
        errorDialog.Size = new Vector2I(300, 120);
        EditorInterface.Singleton.GetBaseControl().AddChild(errorDialog);
    }

    private void OnAddButtonPressed()
    {
        UpdateComponentList("");
        popup.PopupCentered();
        searchField.GrabFocus();
    }

    private void OnSearchTextChanged(string newText)
    {
        UpdateComponentList(newText);
    }

    private void UpdateComponentList(string filter)
    {
        componentList.Clear();

        var filtered = string.IsNullOrEmpty(filter)
            ? availableComponentTypes
            : availableComponentTypes.Where(t =>
                t.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

        // Получаем текущие компоненты для проверки дубликатов
        var currentComponents = GetCurrentComponents();

        foreach (var type in filtered)
        {
            var isAlreadyAdded = currentComponents.Any(c => c.GetType() == type);
            componentList.AddItem(type.Name + (isAlreadyAdded ? " (already added)" : ""));
            componentList.SetItemMetadata(componentList.ItemCount - 1, type.AssemblyQualifiedName);

            // Делаем недоступными уже добавленные компоненты
            if (isAlreadyAdded)
            {
                componentList.SetItemDisabled(componentList.ItemCount - 1, true);
            }
        }
    }

    private List<ComponentBaker> GetCurrentComponents()
    {
        var propertyName = GetEditedProperty();
        var editedObject = GetEditedObject();
        var currentValue = editedObject.Get(propertyName).AsGodotArray();

        var components = new List<ComponentBaker>();
        foreach (var item in currentValue)
        {
            if (item.AsGodotObject() is ComponentBaker component)
            {
                components.Add(component);
            }
        }
        return components;
    }

    private void OnComponentSelected(long index)
    {
        if (componentList.IsItemDisabled((int)index))
        {
            // Пропускаем выбор недоступных элементов
            return;
        }

        var typeQualifiedName = componentList.GetItemMetadata((int)index).AsString();
        var type = Type.GetType(typeQualifiedName);

        if (type != null)
        {
            AddComponentBaker(type);
        }

        popup.Hide();
        searchField.Text = "";
    }

    private void AddComponentBaker(Type componentType)
    {
        var propertyName = GetEditedProperty();
        var editedObject = GetEditedObject();
        var currentValue = editedObject.Get(propertyName).AsGodotArray();

        // Проверяем дубликаты
        foreach (var item in currentValue)
        {
            if (item.AsGodotObject() is ComponentBaker existingComponent &&
                existingComponent.GetType() == componentType)
            {
                errorDialog.DialogText = $"Component {componentType.Name} is already added to this entity.";
                errorDialog.PopupCentered();
                return;
            }
        }

        // Создаем новый экземпляр через новый экземпляр GodotObject
        GodotObject newComponent = null;

        // Пытаемся создать через ClassDB (для GlobalClass типов)
        if (ClassDB.ClassExists(componentType.Name))
        {
            newComponent = (GodotObject)ClassDB.Instantiate(componentType.Name);
        }

        // Если не получилось, используем Activator и устанавливаем ResourceName
        if (newComponent == null)
        {
            newComponent = (ComponentBaker)Activator.CreateInstance(componentType);
            if (newComponent is Resource res)
            {
                // Устанавливаем имя ресурса для идентификации типа
                res.ResourceName = componentType.Name;
            }
        }

        // Добавляем в массив
        var newArray = new Godot.Collections.Array(currentValue);
        newArray.Add(newComponent);

        // Обновляем свойство с флагом изменения
        EmitChanged(propertyName, newArray);

        // Помечаем editedObject как измененный
        if (editedObject is Node node)
        {
            node.NotifyPropertyListChanged();
            EditorInterface.Singleton.MarkSceneAsUnsaved();
        }
    }

    public override void _UpdateProperty()
    {
        // Не нужно обновлять кнопку при изменении свойства
    }
}
#endif
