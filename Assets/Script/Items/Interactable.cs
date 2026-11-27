using System.Collections.Generic;
using UnityEngine;

/// <summary>Something the knight can use with Interact (E / gamepad Y): notes, shrines, gates.</summary>
public abstract class Interactable : MonoBehaviour
{
    public static readonly List<Interactable> All = new List<Interactable>();

    [SerializeField] protected float range = 2.2f;

    public float Range => range;
    public abstract string Prompt { get; }
    public virtual bool CanInteract => true;
    public abstract void Interact(Player player);

    protected virtual void OnEnable() => All.Add(this);
    protected virtual void OnDisable() => All.Remove(this);
}
