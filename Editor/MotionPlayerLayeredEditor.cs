using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(MotionPlayerLayered))]
public class MotionPlayerLayeredEditor : Editor
{
    MotionPlayerLayered instance;
    SerializedProperty playOnAwakeProperty;
    SerializedProperty deactivateOnEndProperty;
    SerializedProperty layersProperty;
    ReorderableList layers;
    ReorderableList states;
    int cachedLayerIndex = -1;
    bool playDetailFoldOpened = false;

    void OnEnable()
    {
        instance = (MotionPlayerLayered)target;
        playOnAwakeProperty = serializedObject.FindProperty("playOnAwake");
        deactivateOnEndProperty = serializedObject.FindProperty("deactivateOnEnd");
        layersProperty = serializedObject.FindProperty("_layers");

        layers = new ReorderableList(serializedObject, layersProperty, true, true, true, true);
        layers.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Animation Layer");
        layers.drawElementCallback = DrawLayerElement;
        layers.onAddCallback = AddLayer;
        layers.onSelectCallback = _ => RebuildStateList();
        layers.onReorderCallback = _ => RebuildStateList();
        layers.onRemoveCallback = RemoveLayer;

        if (layersProperty != null && layersProperty.arraySize > 0)
            layers.index = 0;
        RebuildStateList();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        if (layers.index != cachedLayerIndex)
            RebuildStateList();

        DrawAnimatorSettings();

        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.PropertyField(playOnAwakeProperty, new GUIContent("Play On Awake"));
        EditorGUILayout.PropertyField(deactivateOnEndProperty, new GUIContent("Deactivate On End"));

        if (Application.isPlaying)
            LayerOnPlay();
        else
            LayerOnEditor();

        EditorGUILayout.EndVertical();
        serializedObject.ApplyModifiedProperties();
    }

    void DrawAnimatorSettings()
    {
        Animator animator = instance.Animator;
        if (animator == null)
            return;

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.ObjectField("Animator", animator, typeof(Animator), true);

        EditorGUILayout.BeginVertical("Box");
        Avatar avatar = (Avatar)EditorGUILayout.ObjectField("Avatar", animator.avatar, typeof(Avatar), false);
        bool applyRootMotion = EditorGUILayout.Toggle("Apply Root Motion", animator.applyRootMotion);
        AnimatorUpdateMode updateMode = (AnimatorUpdateMode)EditorGUILayout.EnumPopup("Update Mode", animator.updateMode);
        AnimatorCullingMode cullingMode = (AnimatorCullingMode)EditorGUILayout.EnumPopup("Culling Mode", animator.cullingMode);

        if (avatar != animator.avatar || applyRootMotion != animator.applyRootMotion || updateMode != animator.updateMode || cullingMode != animator.cullingMode) {
            Undo.RecordObject(animator, "Motion Player Layered Animator Settings");
            animator.avatar = avatar;
            animator.applyRootMotion = applyRootMotion;
            instance.UpdateMode = updateMode;
            animator.cullingMode = cullingMode;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
        }
        EditorGUILayout.EndVertical();
    }

    void DrawLayerElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty layer = layersProperty.GetArrayElementAtIndex(index);
        SerializedProperty nameProperty = layer.FindPropertyRelative("name");
        SerializedProperty additiveProperty = layer.FindPropertyRelative("_additive");
        SerializedProperty weightProperty = layer.FindPropertyRelative("_weight");

        rect.y += 1.0f;
        rect.height = EditorGUIUtility.singleLineHeight;

        float typeWidth = 70.0f;
        float weightWidth = 48.0f;
        Rect nameRect = new Rect(rect.x, rect.y, rect.width - typeWidth - weightWidth - 8.0f, rect.height);
        Rect typeRect = new Rect(nameRect.xMax + 4.0f, rect.y, typeWidth, rect.height);
        Rect weightRect = new Rect(typeRect.xMax + 4.0f, rect.y, weightWidth, rect.height);

        EditorGUI.PropertyField(nameRect, nameProperty, GUIContent.none);
        EditorGUI.LabelField(typeRect, index == 0 ? "Base" : additiveProperty.boolValue ? "Additive" : "Override");
        EditorGUI.LabelField(weightRect, index == 0 ? "1.00" : weightProperty.floatValue.ToString("0.00"));
    }

    void AddLayer(ReorderableList list)
    {
        int index = layersProperty.arraySize;
        layersProperty.arraySize++;

        SerializedProperty layer = layersProperty.GetArrayElementAtIndex(index);
        layer.FindPropertyRelative("name").stringValue = "Layer " + index;
        layer.FindPropertyRelative("_mask").objectReferenceValue = null;
        layer.FindPropertyRelative("_additive").boolValue = false;
        layer.FindPropertyRelative("_weight").floatValue = 0.0f;

        SerializedProperty stateArray = layer.FindPropertyRelative("_states");
        stateArray.arraySize = 1;
        InitializeState(stateArray.GetArrayElementAtIndex(0), "Default");

        list.index = index;
        RebuildStateList();
    }

    void RemoveLayer(ReorderableList list)
    {
        if (layersProperty.arraySize <= 1)
            return;

        ReorderableList.defaultBehaviours.DoRemoveButton(list);
        if (list.index < 0 && layersProperty.arraySize > 0)
            list.index = Mathf.Clamp(cachedLayerIndex, 0, layersProperty.arraySize - 1);
        RebuildStateList();
    }

    void RebuildStateList()
    {
        cachedLayerIndex = layers != null ? layers.index : -1;
        states = null;

        if (layersProperty == null || cachedLayerIndex < 0 || cachedLayerIndex >= layersProperty.arraySize)
            return;

        SerializedProperty layer = layersProperty.GetArrayElementAtIndex(cachedLayerIndex);
        SerializedProperty stateArray = layer.FindPropertyRelative("_states");
        states = new ReorderableList(serializedObject, stateArray, true, true, true, true);
        states.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Animation State");
        states.drawElementCallback = DrawStateElement;
        states.onAddCallback = AddState;
    }

    void DrawStateElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        if (states == null)
            return;

        SerializedProperty element = states.serializedProperty.GetArrayElementAtIndex(index);
        SerializedProperty nameProperty = element.FindPropertyRelative("name");
        SerializedProperty clipProperty = element.FindPropertyRelative("_clip");

        float halfWidth = rect.width * 0.5f;
        rect.y += 1.0f;
        rect.height = EditorGUIUtility.singleLineHeight;

        Rect nameRect = new Rect(rect.x, rect.y, halfWidth - 2.0f, rect.height);
        Rect clipRect = new Rect(rect.x + halfWidth, rect.y, halfWidth, rect.height);
        EditorGUI.PropertyField(nameRect, nameProperty, GUIContent.none);
        EditorGUI.PropertyField(clipRect, clipProperty, GUIContent.none);
    }

    void AddState(ReorderableList list)
    {
        SerializedProperty stateArray = list.serializedProperty;
        int index = stateArray.arraySize;
        stateArray.arraySize++;
        InitializeState(stateArray.GetArrayElementAtIndex(index), "State " + index);
        list.index = index;
    }

    void InitializeState(SerializedProperty state, string stateName)
    {
        state.FindPropertyRelative("name").stringValue = stateName;
        state.FindPropertyRelative("_clip").objectReferenceValue = null;
        state.FindPropertyRelative("_speed").floatValue = 1.0f;
        state.FindPropertyRelative("_applyFootIK").boolValue = false;
        state.FindPropertyRelative("_applyPlayableIK").boolValue = false;
    }

    void LayerOnEditor()
    {
        EditorGUILayout.LabelField("Editor Mode", EditorStyles.boldLabel);
        layers.DoLayoutList();
        DrawLayerDetail();
    }

    void DrawLayerDetail()
    {
        if (layers.index < 0 || layers.index >= layersProperty.arraySize)
            return;

        SerializedProperty layer = layersProperty.GetArrayElementAtIndex(layers.index);
        SerializedProperty nameProperty = layer.FindPropertyRelative("name");

        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField(string.IsNullOrEmpty(nameProperty.stringValue) ? "Layer" : nameProperty.stringValue, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;

        EditorGUILayout.PropertyField(nameProperty, new GUIContent("Name"));
        using (new EditorGUI.DisabledScope(layers.index == 0)) {
            EditorGUILayout.PropertyField(layer.FindPropertyRelative("_mask"), new GUIContent("Avatar Mask"));
            EditorGUILayout.PropertyField(layer.FindPropertyRelative("_additive"), new GUIContent("Additive"));
            EditorGUILayout.PropertyField(layer.FindPropertyRelative("_weight"), new GUIContent("Weight"));
        }

        if (layers.index == 0)
            EditorGUILayout.HelpBox("Layer 0 is the Base layer. Avatar Mask and Additive are ignored, and its layer weight is always 1.", MessageType.Info);

        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();

        if (states != null) {
            states.DoLayoutList();
            DrawStateDetail();
        }
    }

    void DrawStateDetail()
    {
        if (states == null || states.index < 0 || states.index >= states.serializedProperty.arraySize)
            return;

        SerializedProperty state = states.serializedProperty.GetArrayElementAtIndex(states.index);
        SerializedProperty nameProperty = state.FindPropertyRelative("name");

        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.LabelField(string.IsNullOrEmpty(nameProperty.stringValue) ? "State" : nameProperty.stringValue, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(state.FindPropertyRelative("_speed"), new GUIContent("Speed"));
        EditorGUILayout.PropertyField(state.FindPropertyRelative("_applyFootIK"), new GUIContent("Apply Foot IK"));
        EditorGUILayout.PropertyField(state.FindPropertyRelative("_applyPlayableIK"), new GUIContent("Apply Playable IK"));
        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
    }

    void LayerOnPlay()
    {
        EditorGUILayout.LabelField("Play Mode", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Play"))
            instance.Play();
        if (GUILayout.Button("Stop"))
            instance.Stop();
        EditorGUILayout.EndHorizontal();

        playDetailFoldOpened = EditorGUILayout.Foldout(playDetailFoldOpened, playDetailFoldOpened ? "Detail" : "Simple");
        var runtimeLayers = instance.Layers;
        if (runtimeLayers == null)
            return;

        Color weightZeroColor = new Color(0.18f, 0.3f, 0.15f, 0.7f);
        Color weightOneColor = new Color(0.68f, 0.825f, 0.65f, 0.3f);

        for (int l = 0; l < runtimeLayers.Count; l++) {
            MotionPlayerLayered.AnimationLayer layer = runtimeLayers[l];
            if (layer == null)
                continue;

            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.LabelField((l == 0 ? "Base : " : layer.additive ? "Additive : " : "Override : ") + layer.name, EditorStyles.boldLabel);
            EditorGUILayout.FloatField("Layer Weight", layer.weight);

            for (int i = 0; i < layer.StateCount; i++) {
                MotionPlayerLayered.AnimationState state = layer.GetState(i);
                if (state == null)
                    continue;

                EditorGUILayout.LabelField(state.name, i == layer.CurStateIndex ? EditorStyles.boldLabel : EditorStyles.label);
                EditorGUILayout.ObjectField(state.clip, typeof(AnimationClip), false);

                Rect controlRect = GUILayoutUtility.GetLastRect();
                Rect timeRect = controlRect;
                timeRect.x += 16.0f;
                timeRect.y += 1.0f;
                timeRect.width -= 35.0f;
                timeRect.height -= 2.0f;
                timeRect.width *= Mathf.Repeat(state.normalizedTime, 1.0f);
                EditorGUI.DrawRect(timeRect, Color.Lerp(weightZeroColor, weightOneColor, state.weight));

                if (playDetailFoldOpened) {
                    EditorGUILayout.FloatField("Time", state.time);
                    EditorGUILayout.FloatField("Normalized Time", state.normalizedTime);
                    EditorGUILayout.FloatField("Weight", state.weight);
                    EditorGUILayout.FloatField("Fade Speed", state.fadeSpeed);
                }
            }

            EditorGUILayout.EndVertical();
        }

        Repaint();
    }
}
