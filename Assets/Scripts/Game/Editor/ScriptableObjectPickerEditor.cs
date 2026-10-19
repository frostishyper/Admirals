using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;


// Unity Editor Script - Not Attached To A GameObject
// Provides Searchable And Optionally Filtered ScriptableObject Fields.
[CustomPropertyDrawer(
    typeof(ScriptableObjectPickerAttribute)
)]
public class ScriptableObjectPickerDrawer :
    PropertyDrawer
{
    private const float Spacing =
        2f;

    private const float PickButtonWidth =
        44f;

    private const float RemoveButtonWidth =
        24f;


    public override float GetPropertyHeight(
        SerializedProperty Property,
        GUIContent Label
    )
    {
        float Line =
            EditorGUIUtility.singleLineHeight;


        if (!IsCollection(
                Property))
        {
            return Line;
        }


        if (!Property.isExpanded)
        {
            return Line;
        }


        return
            Line +
            Spacing +
            (
                Property.arraySize *
                (
                    Line +
                    Spacing
                )
            ) +
            Line;
    }


    public override void OnGUI(
        Rect Position,
        SerializedProperty Property,
        GUIContent Label
    )
    {
        ScriptableObjectPickerAttribute PickerAttribute =
            (ScriptableObjectPickerAttribute)attribute;


        Type ExpectedType =
            GetExpectedReferenceType();


        if (ExpectedType == null)
        {
            EditorGUI.PropertyField(
                Position,
                Property,
                Label,
                true
            );

            return;
        }


        if (IsCollection(
                Property))
        {
            DrawCollection(
                Position,
                Property,
                Label,
                ExpectedType,
                PickerAttribute
            );

            return;
        }


        DrawReference(
            Position,
            Property,
            Label,
            ExpectedType,
            PickerAttribute
        );
    }


    private void DrawCollection(
        Rect Position,
        SerializedProperty Property,
        GUIContent Label,
        Type ExpectedType,
        ScriptableObjectPickerAttribute PickerAttribute
    )
    {
        float Line =
            EditorGUIUtility.singleLineHeight;


        Rect HeaderRect =
            new Rect(
                Position.x,
                Position.y,
                Position.width,
                Line
            );


        Property.isExpanded =
            EditorGUI.Foldout(
                HeaderRect,
                Property.isExpanded,
                $"{Label.text} ({Property.arraySize})",
                true
            );


        if (!Property.isExpanded)
        {
            return;
        }


        EditorGUI.indentLevel++;


        float CurrentY =
            HeaderRect.yMax +
            Spacing;


        for (
            int i = 0;
            i < Property.arraySize;
            i++
        )
        {
            SerializedProperty Element =
                Property.GetArrayElementAtIndex(
                    i
                );


            Rect ElementRect =
                new Rect(
                    Position.x,
                    CurrentY,
                    Position.width,
                    Line
                );


            Rect ReferenceRect =
                new Rect(
                    ElementRect.x,
                    ElementRect.y,
                    ElementRect.width -
                    RemoveButtonWidth -
                    Spacing,
                    ElementRect.height
                );


            Rect RemoveRect =
                new Rect(
                    ReferenceRect.xMax +
                    Spacing,
                    ElementRect.y,
                    RemoveButtonWidth,
                    ElementRect.height
                );


            DrawReference(
                ReferenceRect,
                Element,
                new GUIContent(
                    $"Element {i}"
                ),
                ExpectedType,
                PickerAttribute
            );


            if (GUI.Button(
                    RemoveRect,
                    "−"))
            {
                RemoveArrayElement(
                    Property,
                    i
                );

                EditorGUI.indentLevel--;

                return;
            }


            CurrentY +=
                Line +
                Spacing;
        }


        Rect AddRect =
            new Rect(
                Position.x,
                CurrentY,
                Position.width,
                Line
            );


        if (GUI.Button(
                AddRect,
                "+ Add"))
        {
            AddEmptyArrayElement(
                Property
            );
        }


        EditorGUI.indentLevel--;
    }


    private void DrawReference(
        Rect Position,
        SerializedProperty Property,
        GUIContent Label,
        Type ExpectedType,
        ScriptableObjectPickerAttribute PickerAttribute
    )
    {
        if (Property.propertyType !=
            SerializedPropertyType.ObjectReference)
        {
            EditorGUI.PropertyField(
                Position,
                Property,
                Label,
                true
            );

            return;
        }


        Rect ObjectRect =
            new Rect(
                Position.x,
                Position.y,
                Position.width -
                PickButtonWidth -
                Spacing,
                Position.height
            );


        Rect PickRect =
            new Rect(
                ObjectRect.xMax +
                Spacing,
                Position.y,
                PickButtonWidth,
                Position.height
            );


        UnityEngine.Object PreviousValue =
            Property.objectReferenceValue;


        EditorGUI.BeginChangeCheck();


        UnityEngine.Object NewValue =
            EditorGUI.ObjectField(
                ObjectRect,
                Label,
                PreviousValue,
                ExpectedType,
                false
            );


        if (EditorGUI.EndChangeCheck())
        {
            if (NewValue == null ||
                MatchesFilter(
                    NewValue,
                    PickerAttribute))
            {
                Property.objectReferenceValue =
                    NewValue;
            }
            else
            {
                Debug.LogWarning(
                    $"'{NewValue.name}' does not satisfy the filter for '{Property.displayName}'."
                );
            }
        }


        if (GUI.Button(
                PickRect,
                "Pick"))
        {
            FilteredAssetDropdown Dropdown =
                new FilteredAssetDropdown(
                    new AdvancedDropdownState(),
                    ExpectedType,
                    PickerAttribute,
                    Property.serializedObject.targetObjects,
                    Property.propertyPath
                );


            Dropdown.Show(
                PickRect
            );
        }
    }


    // Adds a genuinely empty reference instead of duplicating the previous array element.
    private void AddEmptyArrayElement(
        SerializedProperty Property
    )
    {
        int NewIndex =
            Property.arraySize;


        Property.InsertArrayElementAtIndex(
            NewIndex
        );


        SerializedProperty NewElement =
            Property.GetArrayElementAtIndex(
                NewIndex
            );


        NewElement.objectReferenceValue =
            null;


        Property.serializedObject.ApplyModifiedProperties();
    }


    // Unity may only null an ObjectReference on the first delete call.
    // Calling it again removes the now-empty array element.
    private void RemoveArrayElement(
        SerializedProperty Property,
        int Index
    )
    {
        int PreviousSize =
            Property.arraySize;


        Property.DeleteArrayElementAtIndex(
            Index
        );


        if (Property.arraySize ==
            PreviousSize)
        {
            Property.DeleteArrayElementAtIndex(
                Index
            );
        }


        Property.serializedObject.ApplyModifiedProperties();
    }


    private bool IsCollection(
        SerializedProperty Property
    )
    {
        return
            Property.isArray &&
            Property.propertyType !=
            SerializedPropertyType.String;
    }


    private Type GetExpectedReferenceType()
    {
        if (fieldInfo == null)
        {
            return null;
        }


        Type FieldType =
            fieldInfo.FieldType;


        if (FieldType.IsArray)
        {
            return
                FieldType.GetElementType();
        }


        if (FieldType.IsGenericType &&
            FieldType.GetGenericTypeDefinition() ==
            typeof(List<>))
        {
            return
                FieldType.GetGenericArguments()[0];
        }


        return
            FieldType;
    }


    // Applies a filter by reading the explicitly named property or field
    // from the candidate asset. The picker never guesses which member matters.
    private static bool MatchesFilter(
        UnityEngine.Object Asset,
        ScriptableObjectPickerAttribute PickerAttribute
    )
    {
        if (Asset == null ||
            !PickerAttribute.HasFilter)
        {
            return true;
        }


        Type AssetType =
            Asset.GetType();


        PropertyInfo Property =
            AssetType.GetProperty(
                PickerAttribute.FilterMemberName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );


        if (Property != null)
        {
            object Value =
                Property.GetValue(
                    Asset
                );


            return
                Equals(
                    Value,
                    PickerAttribute.FilterValue
                );
        }


        FieldInfo Field =
            AssetType.GetField(
                PickerAttribute.FilterMemberName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic
            );


        if (Field != null)
        {
            object Value =
                Field.GetValue(
                    Asset
                );


            return
                Equals(
                    Value,
                    PickerAttribute.FilterValue
                );
        }


        return false;
    }


    private sealed class FilteredAssetDropdown :
        AdvancedDropdown
    {
        private readonly Type _ExpectedType;

        private readonly ScriptableObjectPickerAttribute _PickerAttribute;

        private readonly UnityEngine.Object[] _Targets;

        private readonly string _PropertyPath;


        public FilteredAssetDropdown(
            AdvancedDropdownState State,
            Type ExpectedType,
            ScriptableObjectPickerAttribute PickerAttribute,
            UnityEngine.Object[] Targets,
            string PropertyPath
        )
            : base(State)
        {
            _ExpectedType =
                ExpectedType;

            _PickerAttribute =
                PickerAttribute;

            _Targets =
                Targets;

            _PropertyPath =
                PropertyPath;


            minimumSize =
                new Vector2(
                    380f,
                    320f
                );
        }


        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem Root =
                new AdvancedDropdownItem(
                    $"Select {_ExpectedType.Name}"
                );


            Root.AddChild(
                new AssetItem(
                    "None",
                    null
                )
            );


            string[] GUIDs =
                AssetDatabase.FindAssets(
                    $"t:{_ExpectedType.Name}"
                );


            List<UnityEngine.Object> Assets =
                new List<UnityEngine.Object>();


            for (
                int i = 0;
                i < GUIDs.Length;
                i++
            )
            {
                string Path =
                    AssetDatabase.GUIDToAssetPath(
                        GUIDs[i]
                    );


                UnityEngine.Object Asset =
                    AssetDatabase.LoadAssetAtPath(
                        Path,
                        _ExpectedType
                    );


                if (Asset == null ||
                    !MatchesFilter(
                        Asset,
                        _PickerAttribute))
                {
                    continue;
                }


                Assets.Add(
                    Asset
                );
            }


            Assets.Sort(
                (
                    A,
                    B
                ) =>
                string.Compare(
                    A.name,
                    B.name,
                    StringComparison.OrdinalIgnoreCase
                )
            );


            for (
                int i = 0;
                i < Assets.Count;
                i++
            )
            {
                Root.AddChild(
                    new AssetItem(
                        Assets[i].name,
                        Assets[i]
                    )
                );
            }


            return Root;
        }


        protected override void ItemSelected(
            AdvancedDropdownItem Item
        )
        {
            if (Item is not AssetItem SelectedItem)
            {
                return;
            }


            SerializedObject SerializedTargets =
                new SerializedObject(
                    _Targets
                );


            SerializedProperty Property =
                SerializedTargets.FindProperty(
                    _PropertyPath
                );


            if (Property == null)
            {
                return;
            }


            Property.objectReferenceValue =
                SelectedItem.Asset;


            SerializedTargets.ApplyModifiedProperties();
        }


        private sealed class AssetItem :
            AdvancedDropdownItem
        {
            public UnityEngine.Object Asset
            {
                get;
            }


            public AssetItem(
                string Name,
                UnityEngine.Object Asset
            )
                : base(Name)
            {
                this.Asset =
                    Asset;
            }
        }
    }
}