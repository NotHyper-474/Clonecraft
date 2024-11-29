using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using Minecraft;

[CustomEditor(typeof(Minecraft.TerrainChunk))]
public class ChunkIndexEditor : Editor
{
    private VisualElement _rootElement;
    private VisualTreeAsset _treeAsset;
    private SerializedObject _chunk;

    private void OnEnable()
    {
        _rootElement = new VisualElement();
        _treeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Editor/ChunkIndexEditor.uxml");
        _chunk = new SerializedObject(target);
    }

    public override VisualElement CreateInspectorGUI()
    {
        _rootElement.Clear();
        _treeAsset.CloneTree(_rootElement);
        
        // This looks ugly, but I couldn't figure out how to bind through the editor
        var chunkIndex = _chunk.FindProperty("_chunkIndex");
        ((Vector3IntField)_rootElement.ElementAt(0)).BindProperty(chunkIndex);

        _chunk.ApplyModifiedProperties();
        _chunk.UpdateIfRequiredOrScript();
        return _rootElement;
    }
}