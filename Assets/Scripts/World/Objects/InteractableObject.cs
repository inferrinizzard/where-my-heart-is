using UnityEngine;

public abstract class InteractableObject : MonoBehaviour
{
	public ClippableObject hitboxObject;
	/// <summary> Reference to the player. </summary>
	[HideInInspector] public Player player;
	/// <summary> Whether or not this is the active item </summary>
	[HideInInspector] public bool active;
	public string prompt = "Press E to Interact";
	string flavorText = "";
	DialogueSystem dialogue;
	[HideInInspector] Renderer[] renderers;

	public virtual void Interact()
	{
		if (flavorText != "")
			StartCoroutine(dialogue.WriteDialogue(flavorText));
	}

	protected virtual void Start()
	{
		renderers = GetComponentsInChildren<Renderer>();
		dialogue = GameManager.Instance.dialogue;
		player = Player.Instance;
		if (hitboxObject) hitboxObject.GetComponent<ClippableObject>().tiedInteractable = this;
	}

	void OnMouseEnter()
	{
		if (!player.heldObject && !this.TryComponent<OutlineObject>() && (transform.position - player.transform.position).sqrMagnitude < player.playerReach * player.playerReach)
			GameManager.Instance.VFX.SetTargetColour(null);
	}

	void OnMouseExit() => GameManager.Instance.VFX.SetTargetColour(Color.black);

	void OnWillRenderObject() => GameManager.Instance.VFX.RenderGlowMap(renderers);
}
