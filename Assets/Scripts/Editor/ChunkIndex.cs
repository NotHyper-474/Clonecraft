using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

[CustomEditor(typeof(Minecraft.TerrainChunk))]
public class ChunkIndex : Editor
{
    private VisualElement _RootElement;
    private VisualTreeAsset _TreeAsset;
    private SerializedObject _Chunk;

    private void OnEnable()
    {
        _RootElement = new VisualElement();
        _TreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Editor/ChunkIndex.uxml");
        _Chunk = new SerializedObject(target);
    }

    public override VisualElement CreateInspectorGUI()
    {
        _RootElement.Clear();
        _TreeAsset.CloneTree(_RootElement);

        VisualElement index = new Vector3IntField("Index");
        index.Bind(_Chunk);

        _RootElement.Add(index);

        _Chunk.ApplyModifiedProperties();
        _Chunk.UpdateIfRequiredOrScript();
        return _RootElement;

        // VisualElements objects can contain other VisualElement following a tree hierarchy.
        /*VisualElement nodeArea = new GradientField("Test");
        root.Add(nodeArea);

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Editor/ChunkIndex.uxml");
        VisualElement labelFromUXML = visualTree.Instantiate();
        root.Add(labelFromUXML);

        // A stylesheet can be added to a VisualElement.
        // The style will be applied to the VisualElement and all of its children.
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/ChunkIndex.uss");
        VisualElement labelWithStyle = new Label("Hello World! With Style");
        labelWithStyle.styleSheets.Add(styleSheet);
        root.Add(labelWithStyle);*/
    }
}