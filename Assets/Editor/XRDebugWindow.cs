using UnityEditor;
using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class XRDebugWindow : EditorWindow
{
    GameObject xrOrigin;
    Transform cameraTransform;
    float previewCameraYOffset = 0f;
    float scaleMultiplier = 1f;
    bool autoApplyScale = false;

    [MenuItem("Window/XR/Debug Window")]
    public static void ShowWindow()
    {
        GetWindow<XRDebugWindow>("XR Debug");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("XR Debug Tools", EditorStyles.boldLabel);

        xrOrigin = (GameObject)EditorGUILayout.ObjectField("XR Origin", xrOrigin, typeof(GameObject), true);

        if (xrOrigin != null)
        {
            // try to find camera
            if (cameraTransform == null || cameraTransform.root.gameObject != xrOrigin)
            {
                var cam = xrOrigin.GetComponentInChildren<Camera>();
                cameraTransform = cam != null ? cam.transform : null;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Transforms", EditorStyles.boldLabel);
            Vector3 rootPos = xrOrigin.transform.position;
            Vector3 rootRot = xrOrigin.transform.eulerAngles;
            Vector3 rootScale = xrOrigin.transform.localScale;
            EditorGUILayout.Vector3Field("Origin Position", rootPos);
            EditorGUILayout.Vector3Field("Origin Rotation", rootRot);
            EditorGUILayout.Vector3Field("Origin Scale", rootScale);

            if (cameraTransform != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Camera", EditorStyles.boldLabel);
                EditorGUILayout.Vector3Field("Camera World Pos", cameraTransform.position);
                EditorGUILayout.Vector3Field("Camera Local Pos", cameraTransform.localPosition);
                EditorGUILayout.Vector3Field("Camera Local Rot", cameraTransform.localEulerAngles);

                EditorGUILayout.LabelField($"Eye Height (world Y): {cameraTransform.position.y:F3}");
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

            if (GUILayout.Button("Recenter Tracking (TryRecenter + Floor)"))
            {
                RecenterAndFloor();
            }

            previewCameraYOffset = EditorGUILayout.FloatField("Preview Camera Y Offset", previewCameraYOffset);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Preview Offset to Camera Offset"))
            {
                ApplyPreviewCameraYOffset(previewCameraYOffset);
            }
            if (GUILayout.Button("Clear Camera Offset"))
            {
                ApplyPreviewCameraYOffset(0f);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            scaleMultiplier = EditorGUILayout.Slider("Player Scale", scaleMultiplier, 0.25f, 4f);
            autoApplyScale = EditorGUILayout.Toggle("Auto-apply Scale", autoApplyScale);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Apply Scale")) ApplyScale(scaleMultiplier);
            if (GUILayout.Button("Reset Scale")) ApplyScale(1f);
            EditorGUILayout.EndHorizontal();

            if (autoApplyScale && !EditorApplication.isPlaying)
            {
                // live preview while editing
                xrOrigin.transform.localScale = Vector3.one * scaleMultiplier;
                EditorUtility.SetDirty(xrOrigin);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Assign your XR Origin (GameObject) to inspect camera height and apply adjustments.", MessageType.Info);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("This window only affects the Editor scene. Runtime recenter/scale is recommended via a small runtime component for device testing.", MessageType.None);
    }

    void RecenterAndFloor()
    {
        var subs = new List<XRInputSubsystem>();
        SubsystemManager.GetSubsystems(subs);
        foreach (var s in subs)
        {
            s.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
            s.TryRecenter();
        }
        Debug.Log("XR: Tried to set Floor tracking origin and recenter.");
    }

    void ApplyPreviewCameraYOffset(float y)
    {
        if (xrOrigin == null) return;
        // Try to find a common Camera Offset child by name
        var offset = xrOrigin.transform.Find("Camera Offset");
        if (offset == null) offset = xrOrigin.transform.Find("CameraOffset");
        if (offset != null)
        {
            Undo.RecordObject(offset.transform, "Apply Camera Offset");
            offset.localPosition = new Vector3(offset.localPosition.x, y, offset.localPosition.z);
            EditorUtility.SetDirty(offset);
            Debug.Log($"Set Camera Offset.y to {y}");
        }
        else
        {
            Debug.LogWarning("No 'Camera Offset' child found on XR Origin. Create one or set offset manually.");
        }
    }

    void ApplyScale(float s)
    {
        if (xrOrigin == null) return;
        Undo.RecordObject(xrOrigin.transform, "Apply XR Origin Scale");
        xrOrigin.transform.localScale = Vector3.one * s;
        EditorUtility.SetDirty(xrOrigin);
        Debug.Log($"Applied XR Origin scale {s}");
    }
}
