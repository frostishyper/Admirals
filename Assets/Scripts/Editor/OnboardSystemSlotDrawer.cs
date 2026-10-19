using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Unity Editor Script - Not Attached To A GameObject
// Provides Installation Authoring For Onboard System Slots
[CustomPropertyDrawer(typeof(OnboardSystemSlot))]
public class OnboardSystemSlotDrawer : PropertyDrawer
{
    private const float SPACING = 4f;
    private const float PICK_BUTTON_WIDTH = 55f;


    public override void OnGUI(
        Rect Position,
        SerializedProperty Property,
        GUIContent Label)
    {
        EditorGUI.BeginProperty(Position, Label, Property);


        SerializedProperty SystemProperty =
            Property.FindPropertyRelative("_System");

        SerializedProperty MaxChargesProperty =
            Property.FindPropertyRelative("_MaxCharges");

        SerializedProperty DeploymentLoadoutProperty =
            Property.FindPropertyRelative("_DeploymentLoadout");


        float LineHeight =
            EditorGUIUtility.singleLineHeight;

        float CurrentY =
            Position.y;


        Rect SystemRect =
            new Rect(
                Position.x,
                CurrentY,
                Position.width,
                LineHeight
            );


        EditorGUI.PropertyField(
            SystemRect,
            SystemProperty,
            new GUIContent("System")
        );


        CurrentY =
            SystemRect.yMax + SPACING;


        Rect ChargesRect =
            new Rect(
                Position.x,
                CurrentY,
                Position.width,
                LineHeight
            );


        MaxChargesProperty.intValue =
            Mathf.Max(
                0,
                EditorGUI.IntField(
                    ChargesRect,
                    "Max Charges",
                    MaxChargesProperty.intValue
                )
            );


        CurrentY =
            ChargesRect.yMax + SPACING;


        DeploymentSystemDefinition DeploymentSystem =
            SystemProperty.objectReferenceValue
            as DeploymentSystemDefinition;


        if (DeploymentSystem == null)
        {
            EditorGUI.EndProperty();
            return;
        }


        Rect AcceptedRect =
            new Rect(
                Position.x,
                CurrentY,
                Position.width,
                LineHeight
            );


        EditorGUI.LabelField(
            AcceptedRect,
            "Accepts",
            GetAcceptedTypesLabel(DeploymentSystem)
        );


        CurrentY =
            AcceptedRect.yMax + SPACING;


        Rect FoldoutRect =
            new Rect(
                Position.x,
                CurrentY,
                Position.width,
                LineHeight
            );


        DeploymentLoadoutProperty.isExpanded =
            EditorGUI.Foldout(
                FoldoutRect,
                DeploymentLoadoutProperty.isExpanded,
                "Deployment Loadout",
                true
            );


        CurrentY =
            FoldoutRect.yMax + SPACING;


        if (!DeploymentLoadoutProperty.isExpanded)
        {
            EditorGUI.EndProperty();
            return;
        }


        int PreviousIndent =
            EditorGUI.indentLevel;

        EditorGUI.indentLevel++;


        Rect SizeRect =
            new Rect(
                Position.x,
                CurrentY,
                Position.width,
                LineHeight
            );


        int NewSize =
            Mathf.Max(
                0,
                EditorGUI.IntField(
                    SizeRect,
                    "Slots",
                    DeploymentLoadoutProperty.arraySize
                )
            );


        if (NewSize != DeploymentLoadoutProperty.arraySize)
        {
            DeploymentLoadoutProperty.arraySize =
                NewSize;
        }


        CurrentY =
            SizeRect.yMax + SPACING;


        UnitDefinition HostUnit =
            Property.serializedObject.targetObject
            as UnitDefinition;


        for (int Index = 0;
             Index < DeploymentLoadoutProperty.arraySize;
             Index++)
        {
            SerializedProperty SlotProperty =
                DeploymentLoadoutProperty
                    .GetArrayElementAtIndex(Index);

            SerializedProperty UnitProperty =
                SlotProperty.FindPropertyRelative("_Unit");

            SerializedProperty CountProperty =
                SlotProperty.FindPropertyRelative("_Count");


            Rect UnitRect =
                new Rect(
                    Position.x,
                    CurrentY,
                    Position.width,
                    LineHeight
                );


            DrawCompatibleUnitField(
                UnitRect,
                UnitProperty,
                Index,
                DeploymentSystem,
                HostUnit
            );


            CurrentY =
                UnitRect.yMax + SPACING;


            Rect CountRect =
                new Rect(
                    Position.x,
                    CurrentY,
                    Position.width,
                    LineHeight
                );


            CountProperty.intValue =
                Mathf.Max(
                    1,
                    EditorGUI.IntField(
                        CountRect,
                        "Count",
                        CountProperty.intValue
                    )
                );


            CurrentY =
                CountRect.yMax + SPACING;


            UnitDefinition CurrentUnit =
                UnitProperty.objectReferenceValue
                as UnitDefinition;


            if (CurrentUnit != null &&
                !DeploymentSystem.AcceptsUnit(CurrentUnit))
            {
                Rect WarningRect =
                    new Rect(
                        Position.x,
                        CurrentY,
                        Position.width,
                        LineHeight * 2f
                    );


                EditorGUI.HelpBox(
                    WarningRect,
                    $"{CurrentUnit.name} is not compatible with {DeploymentSystem.SystemName}.",
                    MessageType.Error
                );


                CurrentY =
                    WarningRect.yMax + SPACING;
            }
        }


        EditorGUI.indentLevel =
            PreviousIndent;


        EditorGUI.EndProperty();
    }


    public override float GetPropertyHeight(
        SerializedProperty Property,
        GUIContent Label)
    {
        float LineHeight =
            EditorGUIUtility.singleLineHeight;

        float Height =
            LineHeight;


        // Max Charges
        Height +=
            SPACING +
            LineHeight;


        SerializedProperty SystemProperty =
            Property.FindPropertyRelative("_System");


        DeploymentSystemDefinition DeploymentSystem =
            SystemProperty.objectReferenceValue
            as DeploymentSystemDefinition;


        if (DeploymentSystem == null)
        {
            return Height;
        }


        // Accepted Unit Types
        Height +=
            SPACING +
            LineHeight;


        SerializedProperty DeploymentLoadoutProperty =
            Property.FindPropertyRelative("_DeploymentLoadout");


        // Foldout
        Height +=
            SPACING +
            LineHeight;


        if (!DeploymentLoadoutProperty.isExpanded)
        {
            return Height;
        }


        // Slot Count
        Height +=
            SPACING +
            LineHeight;


        for (int Index = 0;
             Index < DeploymentLoadoutProperty.arraySize;
             Index++)
        {
            // Unit
            Height +=
                SPACING +
                LineHeight;

            // Count
            Height +=
                SPACING +
                LineHeight;


            SerializedProperty SlotProperty =
                DeploymentLoadoutProperty
                    .GetArrayElementAtIndex(Index);

            SerializedProperty UnitProperty =
                SlotProperty.FindPropertyRelative("_Unit");


            UnitDefinition CurrentUnit =
                UnitProperty.objectReferenceValue
                as UnitDefinition;


            if (CurrentUnit != null &&
                !DeploymentSystem.AcceptsUnit(CurrentUnit))
            {
                Height +=
                    SPACING +
                    LineHeight * 2f;
            }
        }


        return Height;
    }


    private void DrawCompatibleUnitField(
        Rect Position,
        SerializedProperty UnitProperty,
        int SlotIndex,
        DeploymentSystemDefinition DeploymentSystem,
        UnitDefinition HostUnit)
    {
        Rect ObjectFieldRect =
            new Rect(
                Position.x,
                Position.y,
                Position.width -
                PICK_BUTTON_WIDTH -
                SPACING,
                Position.height
            );


        Rect ButtonRect =
            new Rect(
                ObjectFieldRect.xMax + SPACING,
                Position.y,
                PICK_BUTTON_WIDTH,
                Position.height
            );


        UnitDefinition CurrentUnit =
            UnitProperty.objectReferenceValue
            as UnitDefinition;


        EditorGUI.BeginChangeCheck();


        UnitDefinition SelectedUnit =
            EditorGUI.ObjectField(
                ObjectFieldRect,
                $"Slot {SlotIndex}",
                CurrentUnit,
                typeof(UnitDefinition),
                false
            ) as UnitDefinition;


        if (EditorGUI.EndChangeCheck())
        {
            if (SelectedUnit == null ||
                DeploymentSystem.AcceptsUnit(SelectedUnit))
            {
                UnitProperty.objectReferenceValue =
                    SelectedUnit;
            }
            else
            {
                Debug.LogWarning(
                    $"{SelectedUnit.name} cannot be assigned to {DeploymentSystem.SystemName} because its Unit Type is not accepted."
                );
            }
        }


        if (GUI.Button(ButtonRect, "Pick..."))
        {
            DeploymentUnitPickerWindow.Open(
                ButtonRect,
                UnitProperty.serializedObject.targetObjects,
                UnitProperty.propertyPath,
                DeploymentSystem,
                HostUnit
            );
        }
    }


    private string GetAcceptedTypesLabel(
        DeploymentSystemDefinition DeploymentSystem)
    {
        UnitTypeDefinition[] Types =
            DeploymentSystem.AcceptedUnitTypes;


        if (Types == null ||
            Types.Length == 0)
        {
            return "None";
        }


        string Result = "";


        for (int Index = 0;
             Index < Types.Length;
             Index++)
        {
            if (Index > 0)
            {
                Result += ", ";
            }


            Result +=
                Types[Index] != null
                    ? Types[Index].UnitTypeName
                    : "None";
        }


        return Result;
    }
}


// Unity Editor Window - Not Attached To A GameObject
// Shows Only Units Compatible With A Deployment System
public class DeploymentUnitPickerWindow : EditorWindow
{
    private class UnitResult
    {
        public UnitDefinition Unit;
        public string Path;
    }


    private UnityEngine.Object[] _Targets;
    private string _PropertyPath;

    private DeploymentSystemDefinition _DeploymentSystem;
    private UnitDefinition _HostUnit;

    private string _SearchText = "";
    private Vector2 _ScrollPosition;


    private readonly List<UnitResult> _Units =
        new List<UnitResult>();


    public static void Open(
        Rect ButtonRect,
        UnityEngine.Object[] Targets,
        string PropertyPath,
        DeploymentSystemDefinition DeploymentSystem,
        UnitDefinition HostUnit)
    {
        DeploymentUnitPickerWindow Window =
            CreateInstance<DeploymentUnitPickerWindow>();


        Window._Targets =
            Targets;

        Window._PropertyPath =
            PropertyPath;

        Window._DeploymentSystem =
            DeploymentSystem;

        Window._HostUnit =
            HostUnit;


        Window.titleContent =
            new GUIContent("Select Compatible Unit");


        Window.LoadCompatibleUnits();


        Vector2 ScreenPosition =
            GUIUtility.GUIToScreenPoint(
                new Vector2(
                    ButtonRect.x,
                    ButtonRect.yMax
                )
            );


        Window.ShowAsDropDown(
            new Rect(
                ScreenPosition,
                ButtonRect.size
            ),
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
            $"Compatible With: {_DeploymentSystem.SystemName}",
            EditorStyles.miniLabel
        );


        EditorGUILayout.Space(4);


        if (GUILayout.Button(
            "None",
            GUILayout.Height(26f)))
        {
            AssignUnit(null);
            return;
        }


        EditorGUILayout.Space(4);


        _ScrollPosition =
            EditorGUILayout.BeginScrollView(
                _ScrollPosition
            );


        bool FoundResult =
            false;


        foreach (UnitResult Result in _Units)
        {
            if (!MatchesSearch(Result))
            {
                continue;
            }


            FoundResult =
                true;


            DrawUnitResult(Result);
        }


        if (!FoundResult)
        {
            EditorGUILayout.HelpBox(
                "No compatible units found.",
                MessageType.Info
            );
        }


        EditorGUILayout.EndScrollView();
    }


    private void LoadCompatibleUnits()
    {
        _Units.Clear();


        string[] GUIDs =
            AssetDatabase.FindAssets(
                "t:UnitDefinition"
            );


        foreach (string GUID in GUIDs)
        {
            string Path =
                AssetDatabase.GUIDToAssetPath(GUID);


            UnitDefinition Unit =
                AssetDatabase.LoadAssetAtPath<UnitDefinition>(
                    Path
                );


            if (Unit == null)
            {
                continue;
            }


            if (_HostUnit != null &&
                Unit == _HostUnit)
            {
                continue;
            }


            if (!_DeploymentSystem.AcceptsUnit(Unit))
            {
                continue;
            }


            _Units.Add(
                new UnitResult
                {
                    Unit = Unit,
                    Path = Path
                }
            );
        }


        _Units.Sort(
            (A, B) =>
            {
                int NameComparison =
                    string.Compare(
                        A.Unit.name,
                        B.Unit.name,
                        StringComparison.OrdinalIgnoreCase
                    );


                if (NameComparison != 0)
                {
                    return NameComparison;
                }


                return string.Compare(
                    A.Path,
                    B.Path,
                    StringComparison.OrdinalIgnoreCase
                );
            }
        );
    }


    private bool MatchesSearch(UnitResult Result)
    {
        if (string.IsNullOrWhiteSpace(_SearchText))
        {
            return true;
        }


        string Search =
            _SearchText.Trim();


        return
            Result.Unit.name.IndexOf(
                Search,
                StringComparison.OrdinalIgnoreCase
            ) >= 0
            ||
            Result.Path.IndexOf(
                Search,
                StringComparison.OrdinalIgnoreCase
            ) >= 0;
    }


    private void DrawUnitResult(UnitResult Result)
    {
        GUIStyle ResultStyle =
            new GUIStyle(GUI.skin.button);


        ResultStyle.alignment =
            TextAnchor.MiddleLeft;

        ResultStyle.wordWrap =
            false;


        string TypeName =
            Result.Unit.UnitType != null
                ? Result.Unit.UnitType.UnitTypeName
                : "No Unit Type";


        GUIContent Content =
            new GUIContent(
                Result.Unit.name +
                "  [" +
                TypeName +
                "]\n" +
                Result.Path,
                AssetPreview.GetMiniThumbnail(
                    Result.Unit
                )
            );


        if (GUILayout.Button(
            Content,
            ResultStyle,
            GUILayout.Height(42f)))
        {
            AssignUnit(Result.Unit);
        }
    }


    private void AssignUnit(
        UnitDefinition SelectedUnit)
    {
        foreach (UnityEngine.Object Target in _Targets)
        {
            if (Target == null)
            {
                continue;
            }


            Undo.RecordObject(
                Target,
                "Assign Deployment Unit"
            );


            SerializedObject SerializedTarget =
                new SerializedObject(Target);


            SerializedProperty UnitProperty =
                SerializedTarget.FindProperty(
                    _PropertyPath
                );


            if (UnitProperty == null)
            {
                continue;
            }


            UnitProperty.objectReferenceValue =
                SelectedUnit;


            SerializedTarget.ApplyModifiedProperties();


            EditorUtility.SetDirty(Target);
        }


        Close();
    }
}