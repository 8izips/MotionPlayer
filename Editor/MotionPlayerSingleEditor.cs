using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MotionPlayerSingle))]
public class MotionPlayerSingleEditor : Editor
{
    MotionPlayerSingle instance;
    SerializedProperty animClipProperty;
    SerializedProperty playOnAwakeProperty;
    SerializedProperty deactivateOnEndProperty;

    void OnEnable()
    {
        instance = (MotionPlayerSingle)target;
        animClipProperty = serializedObject.FindProperty("animClip");
        playOnAwakeProperty = serializedObject.FindProperty("playOnAwake");
        deactivateOnEndProperty = serializedObject.FindProperty("deactivateOnEnd");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawAnimatorSettings();

        EditorGUILayout.BeginVertical("Box");
        EditorGUILayout.PropertyField(playOnAwakeProperty, new GUIContent("Play On Awake"));
        EditorGUILayout.PropertyField(deactivateOnEndProperty, new GUIContent("Deactivate On End"));
        EditorGUILayout.PropertyField(animClipProperty, new GUIContent("Animation Clip"));
        EditorGUILayout.EndVertical();

        if (Application.isPlaying) {
            EditorGUILayout.BeginVertical("Box");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Play"))
                instance.Play();
            if (GUILayout.Button("Stop"))
                instance.Stop();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

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
}
