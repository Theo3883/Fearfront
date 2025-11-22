using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class XrDebugLogs : MonoBehaviour {
    void Awake(){
        var x = GetComponent<XRBaseInteractable>(); if(!x) return;
        x.hoverEntered.AddListener(_=>Debug.Log("[XR] HOVER " + name));
        x.selectEntered.AddListener(_=>Debug.Log("[XR] SELECT " + name));
        x.activated.AddListener(_=>Debug.Log("[XR] ACTIVATED " + name));
    }
}