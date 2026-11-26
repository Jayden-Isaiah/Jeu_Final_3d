using UnityEngine;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// JOUEUR : detecte ce qu'il regarde et transmet l'interaction. Il ne connait ni le score
// ni l'objectif. A placer sur la Main Camera du joueur.
// Lance un rayon (Raycast) depuis le centre de l'ecran : si le joueur regarde
// un objet interactif a portee, un message s'affiche et E declenche l'interaction.
public class InteractionJoueurFPS : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float portee = 2.5f;
    [Tooltip("Couches que le rayon peut toucher. Laisser Everything sauf la couche du joueur.")]
    [SerializeField] private LayerMask couches = ~0;

    [Header("Interface (optionnel)")]
    [SerializeField] private TextMeshProUGUI texteInteraction;

    private IInteractable cibleActuelle;

    private void Start()
    {
        AfficherTexte(null);
    }

    private void Update()
    {
        // Pendant la pause : pas d'interaction et pas de message a l'ecran.
        if (GestionnairePause.EstEnPause)
        {
            AfficherTexte(null);
            return;
        }

        cibleActuelle = ChercherCible();
        AfficherTexte(cibleActuelle);

        if (cibleActuelle != null && ToucheInteraction())
        {
            cibleActuelle.Interagir(this);
        }
    }

    private IInteractable ChercherCible()
    {
        Ray rayon = new Ray(transform.position, transform.forward);

        // QueryTriggerInteraction.Collide : on detecte aussi les colliders en Is Trigger.
        if (Physics.Raycast(rayon, out RaycastHit impact, portee, couches, QueryTriggerInteraction.Collide))
        {
            // GetComponentInParent : fonctionne meme si le collider est sur un enfant du modele.
            return impact.collider.GetComponentInParent<IInteractable>();
        }
        return null;
    }

    private bool ToucheInteraction()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    private void AfficherTexte(IInteractable cible)
    {
        if (texteInteraction == null) return;

        if (cible != null)
        {
            texteInteraction.text = "[E] " + cible.TexteInteraction;
            texteInteraction.enabled = true;
        }
        else
        {
            texteInteraction.enabled = false;
        }
    }

    // Visualise le rayon dans la vue Scene (objet selectionne).
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(transform.position, transform.forward * portee);
    }
}