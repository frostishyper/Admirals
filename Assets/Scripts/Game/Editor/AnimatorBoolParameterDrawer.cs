#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;


[CustomPropertyDrawer(
    typeof(AnimatorBoolParameterAttribute)
)]
public sealed class AnimatorBoolParameterDrawer :
    PropertyDrawer
{
    public override void OnGUI(
        Rect Position,
        SerializedProperty Property,
        GUIContent Label
    )
    {
        if (Property.propertyType !=
            SerializedPropertyType.String)
        {
            EditorGUI.PropertyField(
                Position,
                Property,
                Label
            );

            return;
        }


        Component Component =
            Property.serializedObject.targetObject
                as Component;

        Animator Animator =
            Component != null
                ? Component.GetComponent<Animator>()
                : null;

        AnimatorController Controller =
            GetAnimatorController(
                Animator != null
                    ? Animator.runtimeAnimatorController
                    : null
            );


        if (Controller == null)
        {
            EditorGUI.HelpBox(
                Position,
                "Assign an Animator Controller before selecting a Bool parameter.",
                MessageType.Warning
            );

            return;
        }


        List<string> Names =
            new List<string>();

        Names.Add(
            "<None>"
        );


        AnimatorControllerParameter[] Parameters =
            Controller.parameters;


        for (int i = 0;
            i < Parameters.Length;
            i++)
        {
            AnimatorControllerParameter Parameter =
                Parameters[i];


            if (Parameter.type ==
                AnimatorControllerParameterType.Bool)
            {
                Names.Add(
                    Parameter.name
                );
            }
        }


        int SelectedIndex =
            0;


        for (int i = 1;
            i < Names.Count;
            i++)
        {
            if (Names[i] ==
                Property.stringValue)
            {
                SelectedIndex =
                    i;

                break;
            }
        }


        int NewIndex =
            EditorGUI.Popup(
                Position,
                Label.text,
                SelectedIndex,
                Names.ToArray()
            );


        Property.stringValue =
            NewIndex > 0
                ? Names[NewIndex]
                : string.Empty;
    }


    private static AnimatorController GetAnimatorController(
        RuntimeAnimatorController RuntimeController
    )
    {
        if (RuntimeController is AnimatorController Controller)
        {
            return Controller;
        }


        if (RuntimeController is AnimatorOverrideController Override)
        {
            return
                Override.runtimeAnimatorController
                    as AnimatorController;
        }


        return null;
    }
}

#endif
