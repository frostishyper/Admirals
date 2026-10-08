using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Unity Editor Script - Not Attached To A GameObject
// Provides A Searchable Picker For ScriptableObject References
// Supports Singular References, Arrays, And Lists
[CustomPropertyDrawer(typeof(ScriptableObjectPickerAttribute))]
public class ScriptableObjectPickerDrawer : PropertyDrawer
{
    private const float BUTTON_WIDTH = 55f;
    private const float SPACING = 4f;


    public override void OnGUI(
        Rect Position,
        SerializedProperty Property,
        GUIContent Label)
    {
        EditorGUI.BeginProperty(
            Position,
            Label,
            Property
        );


        Type RequiredType =
            GetRequiredType();


        if (RequiredType == null ||
            !typeof(ScriptableObject).IsAssignableFrom(RequiredType))
        {
            EditorGUI.HelpBox(
                Position,
                "ScriptableObjectPicker can only be used with ScriptableObject references.",
                MessageType.Error
            );

            EditorGUI.EndProperty();
            return;
        }


        // Unity Handles Array/List Containers Itself.
        // This Drawer Is Invoked For Each Object Reference Element.
        if (Property.propertyType !=
            SerializedPropertyType.ObjectReference)
        {
            EditorGUI.HelpBox(
                Position,
                "ScriptableObjectPicker requires a ScriptableObject reference.",
                MessageType.Error
            );

            EditorGUI.EndProperty();
            return;
        }


        Rect ObjectFieldRect =
            new Rect(
                Position.x,
                Position.y,
                Position.width -
                BUTTON_WIDTH -
                SPACING,
                Position.height
            );


        Rect ButtonRect =
            new Rect(
                ObjectFieldRect.xMax + SPACING,
                Position.y,
                BUTTON_WIDTH,
                Position.height
            );


        bool PreviousMixedValue =
            EditorGUI.showMixedValue;

        EditorGUI.showMixedValue =
            Property.hasMultipleDifferentValues;


        EditorGUI.BeginChangeCheck();


        UnityEngine.Object SelectedObject =
            EditorGUI.ObjectField(
                ObjectFieldRect,
                Label,
                Property.objectReferenceValue,
                RequiredType,
                false
            );


        if (EditorGUI.EndChangeCheck())
        {
            Property.objectReferenceValue =
                SelectedObject;
        }


        EditorGUI.showMixedValue =
            PreviousMixedValue;


        if (GUI.Button(
            ButtonRect,
            "Pick..."))
        {
            ScriptableObjectPickerWindow.Open(
                ButtonRect,
                Property.serializedObject.targetObjects,
                Property.propertyPath,
                RequiredType
            );
        }


        EditorGUI.EndProperty();
    }


    public override float GetPropertyHeight(
        SerializedProperty Property,
        GUIContent Label)
    {
        return EditorGUIUtility.singleLineHeight;
    }


    // Gets The ScriptableObject Type Accepted By This Field.
    // For Arrays And Lists, This Returns The Element Type.
    private Type GetRequiredType()
    {
        Type FieldType =
            fieldInfo.FieldType;


        // Example:
        // EffectDefinition[]
        if (FieldType.IsArray)
        {
            return FieldType.GetElementType();
        }


        // Example:
        // List<EffectDefinition>
        if (FieldType.IsGenericType &&
            FieldType.GetGenericTypeDefinition() ==
            typeof(List<>))
        {
            return
                FieldType.GetGenericArguments()[0];
        }


        // Example:
        // TierDefinition
        return FieldType;
    }
}


// Unity Editor Window - Not Attached To A GameObject
// Displays Compatible ScriptableObject Assets In A Searchable List
public class ScriptableObjectPickerWindow : EditorWindow
{
    private class AssetResult
    {
        public ScriptableObject Asset;
        public string Path;
    }


    private UnityEngine.Object[] _Targets;

    private string _PropertyPath;

    private Type _RequiredType;

    private string _SearchText = "";

    private Vector2 _ScrollPosition;


    private readonly List<AssetResult> _Assets =
        new List<AssetResult>();


    public static void Open(
        Rect ButtonRect,
        UnityEngine.Object[] Targets,
        string PropertyPath,
        Type RequiredType)
    {
        ScriptableObjectPickerWindow Window =
            CreateInstance<ScriptableObjectPickerWindow>();


        Window._Targets =
            Targets;

        Window._PropertyPath =
            PropertyPath;

        Window._RequiredType =
            RequiredType;


        Window.titleContent =
            new GUIContent(
                "Select " +
                RequiredType.Name
            );


        Window.LoadCompatibleAssets();


        Vector2 ScreenPosition =
            GUIUtility.GUIToScreenPoint(
                new Vector2(
                    ButtonRect.x,
                    ButtonRect.yMax
                )
            );


        Rect ScreenRect =
            new Rect(
                ScreenPosition,
                ButtonRect.size
            );


        Window.ShowAsDropDown(
            ScreenRect,
            new Vector2(
                540f,
                460f
            )
        );
    }


    private void OnGUI()
    {
        EditorGUILayout.Space(6);


        _SearchText =
            EditorGUILayout.TextField(
                "Search",
                _SearchText
            );


        EditorGUILayout.Space(4);


        EditorGUILayout.LabelField(
            "Showing: " +
            _RequiredType.Name,
            EditorStyles.miniLabel
        );


        EditorGUILayout.Space(4);


        if (GUILayout.Button(
            "None",
            GUILayout.Height(26f)))
        {
            AssignAsset(null);
            return;
        }


        EditorGUILayout.Space(4);


        _ScrollPosition =
            EditorGUILayout.BeginScrollView(
                _ScrollPosition
            );


        bool FoundResult =
            false;


        foreach (AssetResult Result
                 in _Assets)
        {
            if (!MatchesSearch(Result))
            {
                continue;
            }


            FoundResult =
                true;


            DrawAssetResult(Result);
        }


        if (!FoundResult)
        {
            EditorGUILayout.HelpBox(
                "No matching assets found.",
                MessageType.Info
            );
        }


        EditorGUILayout.EndScrollView();
    }


    private void LoadCompatibleAssets()
    {
        _Assets.Clear();


        string[] AssetGUIDs =
            AssetDatabase.FindAssets(
                "t:ScriptableObject"
            );


        foreach (string GUID
                 in AssetGUIDs)
        {
            string Path =
                AssetDatabase.GUIDToAssetPath(
                    GUID
                );


            UnityEngine.Object LoadedAsset =
                AssetDatabase.LoadAssetAtPath(
                    Path,
                    _RequiredType
                );


            if (LoadedAsset is not
                ScriptableObject ScriptableAsset)
            {
                continue;
            }


            if (!_RequiredType.IsAssignableFrom(
                ScriptableAsset.GetType()))
            {
                continue;
            }


            _Assets.Add(
                new AssetResult
                {
                    Asset =
                        ScriptableAsset,

                    Path =
                        Path
                }
            );
        }


        _Assets.Sort(
            (A, B) =>
            {
                int NameComparison =
                    string.Compare(
                        A.Asset.name,
                        B.Asset.name,
                        StringComparison.OrdinalIgnoreCase
                    );


                if (NameComparison != 0)
                {
                    return NameComparison;
                }


                return
                    string.Compare(
                        A.Path,
                        B.Path,
                        StringComparison.OrdinalIgnoreCase
                    );
            }
        );
    }


    private bool MatchesSearch(
        AssetResult Result)
    {
        if (string.IsNullOrWhiteSpace(
            _SearchText))
        {
            return true;
        }


        string Search =
            _SearchText.Trim();


        bool NameMatches =
            Result.Asset.name.IndexOf(
                Search,
                StringComparison.OrdinalIgnoreCase
            ) >= 0;


        bool PathMatches =
            Result.Path.IndexOf(
                Search,
                StringComparison.OrdinalIgnoreCase
            ) >= 0;


        return
            NameMatches ||
            PathMatches;
    }


    private void DrawAssetResult(
        AssetResult Result)
    {
        GUIStyle ResultStyle =
            new GUIStyle(
                GUI.skin.button
            );


        ResultStyle.alignment =
            TextAnchor.MiddleLeft;

        ResultStyle.wordWrap =
            false;

        ResultStyle.padding =
            new RectOffset(
                8,
                8,
                4,
                4
            );


        GUIContent Content =
            new GUIContent(
                Result.Asset.name +
                "\n" +
                Result.Path,
                AssetPreview.GetMiniThumbnail(
                    Result.Asset
                )
            );


        if (GUILayout.Button(
            Content,
            ResultStyle,
            GUILayout.Height(42f)))
        {
            AssignAsset(
                Result.Asset
            );
        }
    }


    private void AssignAsset(
        UnityEngine.Object SelectedAsset)
    {
        foreach (UnityEngine.Object Target
                 in _Targets)
        {
            if (Target == null)
            {
                continue;
            }


            Undo.RecordObject(
                Target,
                "Assign ScriptableObject"
            );


            SerializedObject SerializedTarget =
                new SerializedObject(
                    Target
                );


            SerializedProperty Property =
                SerializedTarget.FindProperty(
                    _PropertyPath
                );


            if (Property == null)
            {
                continue;
            }


            Property.objectReferenceValue =
                SelectedAsset;


            SerializedTarget.ApplyModifiedProperties();


            EditorUtility.SetDirty(
                Target
            );
        }


        Close();
    }
}