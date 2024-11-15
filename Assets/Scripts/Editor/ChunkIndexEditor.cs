using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

[CustomEditor(typeof(Minecraft.TerrainChunk))]
public class ChunkIndexEditor : Editor
{
    private VisualElement _rootElement;
    private VisualTreeAsset _treeAsset;
    private SerializedObject _chunk;

    private void OnEnable()
    {
        _rootElement = new VisualElement();
        _treeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Editor/ChunkIndex.uxml");
        _chunk = new SerializedObject(target);
    }

    public override VisualElement CreateInspectorGUI()
    {
        _rootElement.Clear();
        _treeAsset.CloneTree(_rootElement);

        VisualElement index = new Vector3IntField("Index");
        index.Bind(_chunk);

        _rootElement.Add(index);

        _chunk.ApplyModifiedProperties();
        _chunk.UpdateIfRequiredOrScript();
        return _rootElement;
    }
}