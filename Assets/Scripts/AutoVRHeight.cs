using UnityEngine;
using Unity.XR.CoreUtils;
using System.Collections;


public class AutoVRHeight : MonoBehaviour
{
    [Tooltip("Height to use when running in Editor/Simulator without a headset.")]
    public float simulatedHeight = 1.7f;

    [Tooltip("Height to use when running on Quest or with a real headset.")]
    public float realVRHeight = 0f;

    [Tooltip("Check this to force Simulated Height even if a VR device is detected.")]
    public bool forceSimulatorMode = false;

    IEnumerator Start()
    {
        yield return null;

        XROrigin xrOrigin = GetComponent<XROrigin>();
        if (xrOrigin == null)
        {
            Debug.LogError("AutoVRHeight: No XROrigin component found on this object!");
            yield break;
        }

        if (Application.platform == RuntimePlatform.Android)
        {
            SetHeight(xrOrigin, realVRHeight, "Quest (Android)");
            yield break;
        }

        if (Application.isEditor)
        {
            if (forceSimulatorMode)
            {
                SetHeight(xrOrigin, simulatedHeight, "Editor (Forced Simulator Mode)");
                yield break;
            }

            string deviceName = UnityEngine.XR.XRSettings.loadedDeviceName;

            if (UnityEngine.XR.XRSettings.isDeviceActive && 
                !deviceName.ToLower().Contains("mock") && 
                !deviceName.ToLower().Contains("simulator"))
            {
                SetHeight(xrOrigin, realVRHeight, $"Editor (Link/VR Active: {deviceName})");
            }
            else
            {
                SetHeight(xrOrigin, simulatedHeight, $"Editor (Simulator/Mock: {deviceName})");
            }
        }
    }

    void SetHeight(XROrigin rig, float height, string reason)
    {
        rig.CameraYOffset = height;
        
        if (height > 0.1f) 
        {
            rig.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Device;
        }
        else
        {
            rig.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.Floor;
        }

        if (rig.CameraFloorOffsetObject != null)
        {
            Vector3 pos = rig.CameraFloorOffsetObject.transform.localPosition;
            rig.CameraFloorOffsetObject.transform.localPosition = new Vector3(pos.x, height, pos.z);
            Debug.Log($"[AutoVRHeight] Directly set CameraFloorOffsetObject.localPosition.y = {height}");
        }
        Debug.Log($"[AutoVRHeight] Set Offset to {height} & Mode to {rig.RequestedTrackingOriginMode} ({reason})");
    }
}
