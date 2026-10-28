#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Exercises the saved Game scene, including its prefab overrides, in a private
// preview scene. It never saves or replaces the developer's loaded scenes.
public static class HUDLayoutValidationEditor
{
    private const string RequestPath = "Temp/HUDLayoutValidation.request";
    private const string ReportPath = "Temp/HUDLayoutValidation.txt";

    [InitializeOnLoadMethod]
    private static void RegisterRequestedCheck()
    {
        if (File.Exists(RequestPath))
            EditorApplication.update += RunRequestedCheck;
    }

    private static void RunRequestedCheck()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
            EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        EditorApplication.update -= RunRequestedCheck;
        File.Delete(RequestPath);
        Validate();
    }

    [MenuItem("Admirals/HUD/Validate Top Bar Layout")]
    public static void Validate()
    {
        var Report = new StringBuilder();
        var Scene = EditorSceneManager.OpenPreviewScene("Assets/Scenes/Game.unity");
        try
        {
            GameplayHUD Hud = Scene.GetRootGameObjects()
                .SelectMany(Root => Root.GetComponentsInChildren<GameplayHUD>(true))
                .Single();
            Hud.SendMessage("Awake");
            Canvas Canvas = Hud.GetComponentInParent<Canvas>();
            Canvas.GetComponent<CanvasScaler>().enabled = false;
            Canvas.renderMode = RenderMode.WorldSpace;
            RectTransform CanvasRect = (RectTransform)Canvas.transform;
            CanvasRect.localScale = Vector3.one;
            RectTransform Top = (RectTransform)Hud.transform.Find("TopBar");
            Transform Controls = Top.Find("MatchControlsRow");
            Require(Controls != null, "MatchControlsRow must be a child of TopBar.");
            Require(!Controls.GetComponent<LayoutElement>().ignoreLayout,
                "MatchControlsRow must reserve layout width.");
            Require(Top.GetComponent<HorizontalLayoutGroup>() != null,
                "TopBar must lay out the active phase and shared controls together.");

            var Phases = new Action[] { Hud.ShowDeploymentPhase,
                Hud.ShowMovementPhase, Hud.ShowPreBattlePhase, Hud.ShowSearchPhase,
                Hud.ShowBattlePhase, Hud.ShowEndPhase };
            foreach (float Width in new[] { 1280f, 1108.5f, 1720f })
            foreach (string Player in new[] { "Player", "Lt. Tensor",
                "Admiral With A Very Long Display Name" })
            for (int Phase = 0; Phase < Phases.Length; Phase++)
            {
                CanvasRect.sizeDelta = new Vector2(Width, 720f);
                Hud.SetPlayerName(Player);
                Phases[Phase]();
                Hud.SetMovementStatus(12, 24, 60f, true);
                Hud.SetPreBattleStatus(12, 60f, true);
                Hud.SetSearchStatus(12, 24, 90f, true);
                Hud.SetBattleStatus(12, 90f, true);
                Hud.SetEndStatus(12, 60f, true);
                Controls.Find("Text_TurnNumber").GetComponent<TMP_Text>().text = "TURN 999";

                // The longest terminal button is part of the same layout.
                if (Phase == 5)
                {
                    Hud.FinishEndButton.GetComponentInChildren<TMP_Text>().text = "RETURN TO LOBBY";
                    var Layout = Hud.FinishEndButton.GetComponent<LayoutElement>();
                    Layout.minWidth = Layout.preferredWidth = Mathf.Ceil(
                        Hud.FinishEndButton.GetComponentInChildren<TMP_Text>()
                            .GetPreferredValues("RETURN TO LOBBY").x) + 32f;
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Hud.transform);
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)Hud.transform);
                TMP_Text[] Labels = Top.GetComponentsInChildren<TMP_Text>();
                Rect Bar = WorldRect(Top);
                var Bounds = new List<Rect>();
                foreach (TMP_Text Label in Labels)
                {
                    Label.ForceMeshUpdate();
                    Rect Rect = WorldRect(Label.rectTransform);
                    Require(Contains(Bar, Rect),
                        $"{Width}/{Phase}/{Player}: {Label.name} escapes TopBar: {Rect} vs {Bar}");
                    Vector2 Rendered = Label.GetRenderedValues();
                    Require(Rendered.x <= Label.rectTransform.rect.width + 1f,
                        $"{Width}/{Phase}/{Player}: {Label.name} text exceeds its allocated width " +
                        $"({Rendered.x} > {Label.rectTransform.rect.width}).");
                    Bounds.Add(Rect);
                }
                for (int A = 0; A < Bounds.Count; A++)
                for (int B = A + 1; B < Bounds.Count; B++)
                    Require(!Overlaps(Bounds[A], Bounds[B]),
                        $"{Width}/{Phase}/{Player}: {Labels[A].name} overlaps {Labels[B].name}.");
                Report.AppendLine($"PASS width={Width}, phase={Phase}, player={Player}");
            }

            var Input = Scene.GetRootGameObjects()
                .SelectMany(Root => Root.GetComponentsInChildren<HumanGameplayInput>(true)).Single();
            Require(new SerializedObject(Input).FindProperty("_PassAndPlayCamera")
                .objectReferenceValue is GameplayCamera,
                "Game scene must wire the Pass & Play camera.");
            Report.AppendLine("PASS Pass & Play camera scene wiring");
            Report.AppendLine("ALL CHECKS PASSED");
            Debug.Log(Report.ToString());
        }
        catch (Exception Error)
        {
            Report.AppendLine("FAILED: " + Error);
            Debug.LogError(Report.ToString());
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(Scene);
            File.WriteAllText(ReportPath, Report.ToString());
        }
    }

    private static Rect WorldRect(RectTransform Transform)
    {
        var Corners = new Vector3[4];
        Transform.GetWorldCorners(Corners);
        return Rect.MinMaxRect(Corners[0].x, Corners[0].y, Corners[2].x, Corners[2].y);
    }

    private static bool Contains(Rect Outer, Rect Inner) =>
        Inner.xMin >= Outer.xMin - 0.5f && Inner.xMax <= Outer.xMax + 0.5f &&
        Inner.yMin >= Outer.yMin - 0.5f && Inner.yMax <= Outer.yMax + 0.5f;

    private static bool Overlaps(Rect A, Rect B) =>
        Mathf.Min(A.xMax, B.xMax) - Mathf.Max(A.xMin, B.xMin) > 0.5f &&
        Mathf.Min(A.yMax, B.yMax) - Mathf.Max(A.yMin, B.yMin) > 0.5f;

    private static void Require(bool Condition, string Message)
    {
        if (!Condition)
            throw new InvalidOperationException(Message);
    }
}
#endif
