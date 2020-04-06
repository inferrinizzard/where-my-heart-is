﻿using System.Collections;
using System.Collections.Generic;

using UnityEngine;

/// <summary>
/// Place on a mirror object to create a planar reflection for it to draw if it uses the mirror material
/// </summary>
public class Mirror : MonoBehaviour
{
	public Material mirrorMaterial; // this 

	private Camera mainCamera;
	private Camera mirrorCamera;
	private RenderTexture reflection;

	// Start is called before the first frame update
	void Start()
	{
		mainCamera = Player.Instance.cam;

		mirrorCamera = new GameObject("Reflection Camera").AddComponent<Camera>();
		mirrorCamera.transform.parent = transform;
		mirrorCamera.enabled = false;

		reflection = new RenderTexture(Screen.width, Screen.height, 24);
		mirrorCamera.targetTexture = reflection;
		mirrorCamera.CopyFrom(mainCamera);
	}

	void OnWillRenderObject()
	{
		if (Vector3.Dot(mainCamera.transform.forward, transform.up) < 0)
		{
			RenderReflection();
		}
	}

	/// <summary>
	/// Renders the scene again from the perspective of a mirrored copy of the main camera and stores it in a render texture
	/// </summary>
	private void RenderReflection()
	{
		Transform mainTransform = mainCamera.transform;

		//TODO: base this on normal vector instead of assumption that normal is in positive local y
		Matrix4x4 mirrorMatrix = transform.localToWorldMatrix * Matrix4x4.Scale(new Vector3(1, -1, 1)) * transform.worldToLocalMatrix;

		mirrorCamera.transform.position = mirrorMatrix.MultiplyPoint(mainTransform.position);
		mirrorCamera.transform.LookAt(mirrorCamera.transform.position + mirrorMatrix.MultiplyVector(mainTransform.forward), mirrorMatrix.MultiplyVector(mainTransform.up));

		Vector3 normal = transform.up;
		Vector4 clipPlaneWorldSpace =
			new Vector4(
				normal.x,
				normal.y,
				normal.z,
				Vector3.Dot(transform.position, -normal));

		Vector4 clipPlaneCameraSpace =
			Matrix4x4.Transpose(Matrix4x4.Inverse(mirrorCamera.worldToCameraMatrix)) * clipPlaneWorldSpace;
		mirrorCamera.projectionMatrix = mirrorCamera.CalculateObliqueMatrix(clipPlaneCameraSpace);

		mirrorCamera.targetTexture = reflection;

		mirrorCamera.Render(); // TODO: can render with shader for stencil first
		mirrorMaterial.SetTexture("_ReflectionTex", reflection);
	}
}
