#define DEBUG
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

#if DEBUG
[ExecuteInEditMode]
#endif
public class Effects : MonoBehaviour
{
	Fade fadeController;
	[HideInInspector] public Wave waveController; // TODO: add wave controller?
	bool outlineOn = true;
	bool bloomOn = false;
	bool dissolveOn = false;
	[SerializeField] Material defaultGlowMat = default;

	[SerializeField, Range(0, 30)] float lightPower = 5;

	void Awake()
	{
		Shader.SetGlobalFloat("_LightAttenBias", 30 - lightPower);
		fadeController = GetComponent<Fade>();
		waveController = GetComponent<Wave>();

		ToggleMask(false);
		ToggleEdgeOutline(true); //outlineOn
		ToggleDissolve(dissolveOn);
		ToggleBoil(true);
		ToggleBird(true);

		currentGlow = new MaterialPropertyBlock();
	}

	void Update()
	{
#if DEBUG // debug toggles
		if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			ToggleEdgeOutline(outlineOn = !outlineOn);
			print($"edge: {outlineOn}");
		}
		if (Input.GetKeyDown(KeyCode.Alpha3))
		{
			ToggleBloom(bloomOn = !bloomOn);
			print($"bloom: {bloomOn}");
		}
		if (Input.GetKeyDown(KeyCode.Alpha4))
		{
			ToggleDissolve(dissolveOn = !dissolveOn);
			print($"dissolve: {dissolveOn}");
		}

#if UNITY_EDITOR
		Shader.SetGlobalFloat("_LightAttenBias", 30 - lightPower);
#endif
#endif
	}

	/// <summary> toggles mask on and off </summary>
	/// <param name="on"> Is mask on? </summary>
	// public void ToggleMask(bool on) => mask.enabled = on;
	public void ToggleMask(bool on) => ToggleEffect(on, "MASK");

	/// <summary> toggles edge outline on and off </summary>
	/// <param name="on"> Is edge outline on? </summary>
	public void ToggleEdgeOutline(bool on) => ToggleEffect(on, "OUTLINE");
	public void ToggleBloom(bool on) => ToggleEffect(on, "BLOOM");
	public void ToggleDissolve(bool on) => ToggleEffect(on, "DISSOLVE");
	public void ToggleBoil(bool on) => ToggleEffect(on, "BOIL");
	public void ToggleWave(bool on) => ToggleEffect(on, "WAVE");
	public void ToggleBird(bool on) => ToggleEffect(on, "BIRD");

	public void StartFade(bool fadingIn, float dur) => fadeController.StartFade(fadingIn, dur);

	// public void SetWave(float distance) => waveController.waveDistance = distance;
	public void SetWave(float distance) => Player.Instance.mask.screenMat.SetFloat("_WaveDistance", distance);

	void ToggleEffect(bool on, string keyword)
	{
		if (on)
			Shader.EnableKeyword(keyword);
		else
			Shader.DisableKeyword(keyword);
	}

	MaterialPropertyBlock currentGlow;
	Color targetColour = Color.black;
	int glowColourID = Shader.PropertyToID("_Colour");

	public void SetTargetColour(Color? c) => targetColour = c ?? defaultGlowMat.GetColor("_Colour");

	public void RenderGlowMap(Renderer[] renderers, Material mat = null, float baseTime = 2)
	{
		var currentColour = currentGlow.GetColor(glowColourID);
		bool atTargetColour = currentColour.Equals(targetColour);
		if (atTargetColour && currentColour.Equals(Color.black))
			return;

		ApplyOutline.drawGlow = true;
		mat = mat ?? defaultGlowMat;

		if (!atTargetColour)
			currentGlow.SetColor(glowColourID, Color.Lerp(currentColour, targetColour, Time.deltaTime / baseTime));
		foreach (Renderer r in renderers)
		{
			if (!atTargetColour)
				r.SetPropertyBlock(currentGlow);
			ApplyOutline.glowBuffer.DrawRenderer(r, mat);
		}
	}
}
