using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using static ImGuiNET.ImGui;

namespace SCMonoGameUtilities.DearImGui.Demos.GuiElements.MiniApps;

// Property editor that demonstrates displaying appropriate
// input fields based on the runtime type of a data record.
class PropertyEditorWindow(bool isOpen = false)
{
    public bool IsOpen = isOpen;

    private readonly ExampleTreeNode rootNode = ExampleTreeNode.MakeRandomTree();
    private readonly unsafe ImGuiTextFilterPtr filter = new(ImGuiNative.ImGuiTextFilter_ImGuiTextFilter(null));

    // We do some reflection in this window, to determine what input controls to use for a given type.
    // It would of course be needlessly slow to reflect in each and every frame. So we just do it the first
    // time we encounter an object of a particular type, and store our findings in a dictionary of sets of
    // "PropertyEditor" instances, keyed by the type they are for:
    private readonly Dictionary<Type, PropertyEditor[]> propertyEditorsByDataType = [];

    private ExampleTreeNode visibleNode = null;

    ~PropertyEditorWindow() => filter.Destroy();

    public void Update()
    {
        if (!IsOpen) return;

        SetNextWindowSize(new(430, 450), ImGuiCond.FirstUseEver);
        if (Begin("Example: Property editor", ref IsOpen))
        {
            UpdateTreePane();
            SameLine();
            UpdatePropertiesPane();
        }

        End();
    }

    private void UpdateTreePane()
    {
        if (BeginChild("##tree", new(300, 0), ImGuiChildFlags.ResizeX | ImGuiChildFlags.Borders | ImGuiChildFlags.NavFlattened))
        {
            SetNextItemWidth(-float.Epsilon);
            SetNextItemShortcut(ImGuiKey.ModCtrl | ImGuiKey.F, ImGuiInputFlags.Tooltip);
            PushItemFlag(ImGuiItemFlags.NoNavDefaultFocus, true);
            filter.Draw("##Filter");
            PopItemFlag();

            // currently using a table to benefit from RowBg feature
            if (BeginTable("##bg", 1, ImGuiTableFlags.RowBg))
            {
                foreach (var childNode in rootNode.Children)
                    if (filter.PassFilter(childNode.Name)) // Filter root node
                        UpdateTreeNode(childNode);
                EndTable();
            }
        }
        EndChild();

        void UpdateTreeNode(ExampleTreeNode node)
        {
            TableNextRow();
            TableNextColumn();

            PushID(node.Id);

            ImGuiTreeNodeFlags tree_flags = ImGuiTreeNodeFlags.None;
            tree_flags |= ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick; // Standard opening mode as we are likely to want to add selection afterwards
            tree_flags |= ImGuiTreeNodeFlags.NavLeftJumpsBackHere; // Left arrow support
            tree_flags |= ImGuiTreeNodeFlags.SpanFullWidth;        // Span full width for easier mouse reach
            //tree_flags |= ImGuiTreeNodeFlags.DrawLinesToNodes;   // Always draw hierarchy outlines

            if (node == visibleNode) tree_flags |= ImGuiTreeNodeFlags.Selected;
            if (node.Children.Count == 0) tree_flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet;

            bool node_open = TreeNodeEx("", tree_flags, node.Name);

            if (IsItemFocused()) visibleNode = node;

            if (node_open)
            {
                foreach (var child in node.Children)
                    UpdateTreeNode(child);
                TreePop();
            }

            PopID();
        }
    }

    private void UpdatePropertiesPane()
    {
        BeginGroup(); // Lock X position

        if (visibleNode != null)
        {
            Text(visibleNode.Name);
            TextDisabled($"UID: {visibleNode.Id}");
            TextDisabled($"Data Type: {visibleNode.Data?.GetType().Name ?? "<None>"}");

            Separator();

            if (BeginTable("##properties", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.ScrollY))
            {
                // Push object ID after we entered the table, so table is shared for all objects
                PushID(visibleNode.Id);
                TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed);
                TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch, 2.0f); // Default twice larger

                if (visibleNode.Data != null)
                {
                    // NB: Here we do a dictionary lookup of the appropriate set of property editors in every frame.
                    // Should be fine, but if we wanted to avoid even that and are okay with our tree structure
                    // growing a bit, we *could* (also) store the editor array against the tree node and check that
                    // first, so that for any given node, it is a direct reference every time after the first
                    // (though of course would then need logic to reset this if stored data type can be changed
                    // - but that's easy enough).
                    var dataType = visibleNode.Data.GetType();
                    if (!propertyEditorsByDataType.TryGetValue(dataType, out var propertyEditors))
                    {
                        propertyEditors = propertyEditorsByDataType[dataType] = PropertyEditor.CreatePropertyEditorsForType(dataType);
                    }

                    foreach (var propertyEditor in propertyEditors)
                    {
                        TableNextRow();
                        PushID(propertyEditor.Label);
                        TableNextColumn();
                        AlignTextToFramePadding();
                        TextUnformatted(propertyEditor.Label);
                        TableNextColumn();

                        propertyEditor.Update(visibleNode.Data);

                        PopID();
                    }
                }

                PopID();
                EndTable();
            }
        }

        EndGroup();    
    }

    // Abstract base class for our property editors - types capable of submitting the appropriate ImGui elements for
    // some object property, and handling any interaction with them by updating the property appropriately.
    // Includes a static method for creating a set of property editors appropriate for a given data type.
    private abstract class PropertyEditor(string label)
    {
        private static readonly Dictionary<Type, Type> ConcreteEditorTypesByPropertyType = new()
        {
            [typeof(bool)] = typeof(BoolPropertyEditor<>),
            [typeof(int)] = typeof(IntPropertyEditor<>),
            [typeof(float)] = typeof(FloatPropertyEditor<>),
            [typeof(Vector2)] = typeof(Vector2PropertyEditor<>),
            [typeof(string)] = typeof(StringPropertyEditor<>),
        };

        public string Label => label;

        public static PropertyEditor[] CreatePropertyEditorsForType(Type dataType)
        {
            List<PropertyEditor> editors = [];

            var editableProperties = dataType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite);

            foreach (var property in editableProperties)
            {
                // There are many ways to do this kind of thing (that is, invoke stuff without knowing the relevant type at compile time).
                // Simpler ways wouldn't involve any generic types, but would ultimately involve invoking the property getters and setters
                // in a needlessly inefficient fashion - using the object type for property values (which would needlessly box value-typed properties)
                // and/or the Delegate type (with its DynamicInvoke method - also slower than necessary). To pay some sort of attention to performance
                // (because after all, our update code will be called many times per second), here we use generic types - in general, compile-time
                // typing will perform a little better. To accomplish this, we just do some relatively simple dynamic stuff (*once* per data type) here
                // with Activator, to instantiate the appropriate generically typed editors. After that (*many* times per second), the instantiated
                // property editors can access the properties in an efficent manner.
                if (ConcreteEditorTypesByPropertyType.TryGetValue(property.PropertyType, out var editorType))
                {
                    editorType = editorType.MakeGenericType(dataType);
                }
                else
                {
                    // We could of course just omit unsupported property types entirely, instead.
                    // Or, well, do whatever we want with them..
                    editorType = typeof(UnsupportedPropertyEditor);
                }

                editors.Add((PropertyEditor)Activator.CreateInstance(editorType, property));
            }

            return [.. editors];
        }

        public abstract void Update(object data);
    }

    // Abstract derivation of PropertyEditor with generic type parameters for a specific data type and type of property.
    // These type parameters ultimately mean that it can get and set a specific property of the specific data type in an efficient manner.
    private abstract class PropertyEditor<TData, TProperty>(PropertyInfo property) : PropertyEditor(property.Name)
    {
        private readonly Func<TData, TProperty> getter = property.GetMethod.CreateDelegate<Func<TData, TProperty>>();
        private readonly Action<TData, TProperty> setter = property.SetMethod.CreateDelegate<Action<TData, TProperty>>();

        public override void Update(object data)
        {
            if (data is not TData dataOfCorrectType)
            {
                throw new ArgumentException("Data is not of correct type for this editor", nameof(data));
            }

            TProperty value = getter(dataOfCorrectType);
            if (Control(ref value))
            {
                setter(dataOfCorrectType, value);
            }
        }

        protected abstract bool Control(ref TProperty propertyValue);
    }

    // Concrete type for editing string-valued properties.
    // NB: in the constructor, we *could* examine the PropertyInfo for e.g. System.ComponentModel.DataAnnotations
    // attributes attached to the property, and use what we find to tweak the details of the control (e.g. set max
    // length appropriately). And of course the same applies for the other types - look for range attributes for
    // the numeric ones, etc.
    private class StringPropertyEditor<TData>(PropertyInfo property) : PropertyEditor<TData, string>(property)
    {
        protected override bool Control(ref string value)
        {
            // NB: will populate the prop if its null. a bit more logic would be needed to make sure that
            // we don't unless something is explicitly typed in, while also making sure that InputText doesn't throw.
            // If on the other hand we want to make sure we explicitly distinguish between null and empty, could add a checkbox.
            // Neither is particularly difficult - consider this an exercise for the reader.
            value ??= string.Empty;
            return InputText("##Editor", ref value, 28);
        }
    }

    private class IntPropertyEditor<TData>(PropertyInfo property) : PropertyEditor<TData, int>(property)
    {
        protected override bool Control(ref int value)
        {
            SetNextItemWidth(-float.Epsilon);
            return DragInt("##Editor", ref value);
        }
    }

    private class FloatPropertyEditor<TData>(PropertyInfo property) : PropertyEditor<TData, float>(property)
    {
        protected override bool Control(ref float value)
        {
            float v_min = 0.0f, v_max = 1.0f;
            SetNextItemWidth(-float.Epsilon);
            return SliderFloat("##Editor", ref value, v_min, v_max);
        }
    }

    private class Vector2PropertyEditor<TData>(PropertyInfo property) : PropertyEditor<TData, Vector2>(property)
    {
        protected override bool Control(ref Vector2 value)
        {
            float v_min = 0.0f, v_max = 1.0f;
            SetNextItemWidth(-float.Epsilon);
            return SliderFloat2("##Editor", ref value, v_min, v_max);
        }
    }

    private class BoolPropertyEditor<TData>(PropertyInfo property) : PropertyEditor<TData, bool>(property)
    {
        protected override bool Control(ref bool value) => Checkbox("##Editor", ref value);
    }

    private class UnsupportedPropertyEditor(PropertyInfo property) : PropertyEditor(property.Name)
    {
        public override void Update(object data)
        {
            TextDisabled("Unsupported field type!");
        }
    }
}

class ExampleTreeNode(int id, string name, object data)
{
    public int Id { get; } = id;

    public string Name { get; } = name;

    public List<ExampleTreeNode> Children { get; } = [];

    public object Data { get; set; } = data;

    public static ExampleTreeNode MakeRandomTree()
    {
        string[] l1NodeNames = { "Apple", "Banana", "Cherry", "Kiwi", "Mango", "Orange", "Pear", "Pineapple", "Strawberry", "Watermelon" };
        const int root_items_multiplier = 2;
        int uid = 0;

        ExampleTreeNode node_root = new(++uid, "<ROOT>", null);
        for (int idx_L1 = 0; idx_L1 < l1NodeNames.Length * root_items_multiplier; idx_L1++)
        {
            ExampleTreeNode node_L1 = new(++uid, $"{l1NodeNames[idx_L1 / root_items_multiplier]} {idx_L1 % root_items_multiplier}", MakeRandomData());
            node_root.Children.Add(node_L1);

            int numberOfL2Children = Random.Shared.Next(2, 5);
            for (int idx_L2 = 0; idx_L2 < numberOfL2Children; idx_L2++)
            {
                ExampleTreeNode node_L2 = new(++uid, $"Child {idx_L2}", MakeRandomData());
                node_L1.Children.Add(node_L2);

                var numberOfL3Children = Random.Shared.Next(2);
                for (int idx_L3 = 0; idx_L3 < numberOfL3Children; idx_L3++)
                {
                    ExampleTreeNode node_L3 = new(++uid, "Sub-child 0", MakeRandomData());
                    node_L2.Children.Add(node_L3);
                }
            }
        }

        return node_root;

        static object MakeRandomData()
        {
            return Random.Shared.Next(4) switch
            {
                0 => new ExampleDataTypeA() { MyInt = Random.Shared.Next(100), MyVec2 = new(Random.Shared.NextSingle(), Random.Shared.NextSingle()) },
                1 => new ExampleDataTypeB() { MyBool= Random.Shared.Next(1) == 1, MyInt = Random.Shared.Next(100) },
                2 => new ExampleDataTypeC() { MyString = "Foo", MyList = [] },
                _ => null,
            };
        }
    }
}

class ExampleDataTypeA
{
    public int MyInt { get; set; }
    public Vector2 MyVec2 { get; set; }
}

class ExampleDataTypeB
{
    public bool MyBool { get; set; }
    public int MyInt { get; set; }
}

class ExampleDataTypeC
{
    public string MyString { get; set; }

    // to demo graceful handling of unsupported prop types
    public List<int> MyList { get; set; }
}