﻿//
// Attach this script to your camera in order to use depth nodes in forward rendering
//

using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class EnableCameraDepthInForward : MonoBehaviour {
#if UNITY_EDITOR
	void OnDrawGizmos(){
		Set();
	}
#endif
	void Start () {
		Set();
	}
	void Set(){
		if(GetComponent<Camera>().depthTextureMode == DepthTextureMode.None)
			GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
	}
}
