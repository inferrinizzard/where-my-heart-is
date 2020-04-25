using System.Collections;
using System.Collections.Generic;

using UnityEngine;

/// <summary> Handles the behavior of an object that can be picked up. </summary>
[ExecuteInEditMode]
public class Pickupable : InteractableObject
{
	[HideInInspector] public Transform oldParent;

	protected Vector3 initialPosition;
	protected Quaternion initialRotation;
	public bool dissolves = false;
	Collider col;

	[HideInInspector] public bool isEntangled = false;
	[ConditionalHide("isEntangled", true), SerializeField, Header("Entangled Colliders")] Collider realCollider;
	[ConditionalHide("isEntangled", true), SerializeField] Collider heartCollider;

	void Awake() => prompt = "Press E to Pick Up";

	protected override void Start()
	{
		base.Start();
		col = GetComponent<Collider>();
	}

	void Update()
	{
		isEntangled = hitboxObject is EntangledClippable;
		if (active)
		{
			// If the object is being inspected, run Looking.
			if (player.looking) Looking();
		}
	}

	/// <summary> Manages behavior of the object when being inspected. </summary>
	public void Looking()
	{
		// Set the rotations based on the mouse movement.
		float rotX = Input.GetAxis("Mouse X") * 2f;
		float rotY = Input.GetAxis("Mouse Y") * 2f;

		// Rotate the object based on previous rotations.
		transform.rotation = Quaternion.AngleAxis(-rotX, player.heldObjectLocation.up) * transform.rotation;
		transform.rotation = Quaternion.AngleAxis(rotY, player.heldObjectLocation.forward) * transform.rotation;
	}

	public override void Interact()
	{
		if (!player.heldObject)
			PickUp();
		else if (player.looking)
			player.looking = false;
		else
			PutDown();
	}

	public void PickUp()
	{
		initialPosition = transform.position;
		initialRotation = transform.rotation;

		oldParent = transform.parent;
		transform.parent = player.heldObjectLocation; // set the new parent to the hold object location object
		transform.localPosition = Vector3.zero; // set the position to local zero to match the position of the hold object location target
	}

	public void PutDown()
	{
		if (this.TryComponent(out ClippableObject clippable) &&
			!clippable.IntersectsBound(player.window.fieldOfViewModel) &&
			clippable.uncutCopy)
		{
			transform.position = initialPosition;
			transform.rotation = initialRotation;
		}

		transform.parent = oldParent;
	}

	public IEnumerator DissolveOnDrop(float time = .25f)
	{
		transform.parent = oldParent;
		col.enabled = false;
		Material mat = GetComponent<MeshRenderer>().material;
		mat.EnableKeyword("DISSOLVE_MANUAL");
		int ManualDissolveID = Shader.PropertyToID("_ManualDissolve");

		float start = Time.time;
		bool inProgress = true;

		while (inProgress)
		{
			yield return null;
			float step = Time.time - start;
			mat.SetFloat(ManualDissolveID, step / time);
			if (step > time)
				inProgress = false;
		}
		mat.DisableKeyword("DISSOLVE_MANUAL");
		mat.SetFloat(ManualDissolveID, 1);

		Interact();
		col.enabled = true;
		active = false;
	}
}
