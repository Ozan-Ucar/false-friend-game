using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(BoxCollider2D))]
public class DialogTrigger : MonoBehaviour
{
    [Header("Dialog Content")]
    [Tooltip("Die Sätze, die in diesem Dialog gesprochen werden.")]
    public List<DialogLine> dialogLines = new List<DialogLine>();

    [Header("Settings")]
    [Tooltip("Soll dieser Dialog nur ein einziges Mal abgespielt werden?")]
    public bool playOnce = true;
    private bool hasPlayed = false;

    private void Awake()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (playOnce && hasPlayed) return;

            if (DialogManager.Instance != null && !DialogManager.Instance.IsDialogActive)
            {
                hasPlayed = true;
                DialogManager.Instance.StartDialog(dialogLines);
            }
        }
    }
}
