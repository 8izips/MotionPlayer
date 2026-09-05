using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(MotionPlayer))]
public class MotionPlayerEditor : Editor
{
    MotionPlayer instance;
    SerializedProperty playOnAwakeProperty;
    SerializedProperty deactivateOnEndProperty;
    SerializedProperty stateProperty;
    ReorderableList states;
    bool playDetailFoldOpened = false;

    void OnEnable()
    {
        instance = (MotionPlayer)target;
        playOnAwakeProperty = serializedObject.FindProperty("playOnAwake");
        deactivateOnEndProperty = serializedObject.FindProperty("deactivateOnEnd");
        stateProperty = serializedObject.FindProperty("_states");

        states = new ReorderableList(serializedObject, stateProperty, true, true, true, true);
        states.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Animation State");
        states.drawElementCallback = DrawStateElement;
        states.onAddCallback = AddState;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawAnimatorSettings();

        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.PropertyField(playOnAwakeProperty, new GUIContent("Play On Awake"));
        EditorGUILayout.PropertyField(deactivateOnEndProperty, new GUIContent("Deactivate On End"));

        if (Application.isPlaying)
            StateOnPlay();
        else
            StateOnEditor();

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
            Undo.RecordObject(animator, "Motion Player Animator Settings");
            animator.avatar = avatar;
            animator.applyRootMotion = applyRootMotion;
            instance.UpdateMode = updateMode;
            animator.cullingMode = cullingMode;
            EditorUtility.SetDirty(animator);
            PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
        }
        EditorGUILayout.EndVertical();
    }

    void DrawStateElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty element = stateProperty.GetArrayElementAtIndex(index);
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
        int index = stateProperty.arraySize;
        stateProperty.arraySize++;

        SerializedProperty element = stateProperty.GetArrayElementAtIndex(index);
        element.FindPropertyRelative("name").stringValue = "State " + index;
        element.FindPropertyRelative("_clip").objectReferenceValue = null;
        element.FindPropertyRelative("_speed").floatValue = 1.0f;
        element.FindPropertyRelative("_applyFootIK").boolValue = false;
        element.FindPropertyRelative("_applyPlayableIK").boolValue = false;
        list.index = index;
    }

    void StateOnEditor()
    {
        EditorGUILayout.LabelField("Editor Mode", EditorStyles.boldLabel);
        states.DoLayoutList();
        StateDetailOnEditor();
    }

    void StateDetailOnEditor()
    {
        if (states.index < 0 || states.index >= stateProperty.arraySize)
            return;

        SerializedProperty state = stateProperty.GetArrayElementAtIndex(states.index);
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

    void StateOnPlay()
    {
        EditorGUILayout.LabelField("Play Mode", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Play"))
            instance.Play();
        if (GUILayout.Button("Stop"))
            instance.Stop();
        EditorGUILayout.EndHorizontal();

        Color weightZeroColor = new Color(0.18f, 0.3f, 0.15f, 0.7f);
        Color weightOneColor = new Color(0.68f, 0.825f, 0.65f, 0.3f);
        EditorGUILayout.BeginVertical("Box");
        EditorGUI.indentLevel++;

        playDetailFoldOpened = EditorGUILayout.Foldout(playDetailFoldOpened, playDetailFoldOpened ? "Detail" : "Simple");
        var runtimeStates = instance.States;
        if (runtimeStates != null) {
            for (int i = 0; i < runtimeStates.Count; i++) {
                MotionPlayer.AnimationState state = runtimeStates[i];
                if (state == null)
                    continue;

                EditorGUILayout.LabelField(state.name, i == instance.CurStateIndex ? EditorStyles.boldLabel : EditorStyles.label);
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
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
        Repaint();
    }
}
