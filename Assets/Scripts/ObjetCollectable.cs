using UnityEngine;

// OBJET INTERACTIF : un fusible que le joueur ramasse en le regardant et en appuyant sur E.
// Responsabilites : s'animer, reagir a l'interaction, produire la retroaction visuelle,
// puis PREVENIR le gestionnaire. Il ne compte rien lui-meme.
//
// Structure du prefab PF_Fusible :
//   PF_Fusible   (ce script + Collider Is Trigger + Animator)
//   ├── Modele   (le visuel, anime par l'Animator)
//   └── PS_Aura  (Particle System en boucle : lueur autour du fusible)
public class ObjetCollectable : MonoBehaviour, IInteractable
{
    // Nom du parametre Trigger dans l'Animator Controller (doit etre identique).
    private static readonly int ParamRamasser = Animator.StringToHash("Ramasser");

    [Header("Objet")]
    [SerializeField] private string nomObjet = "fusible";

    [Header("Animation")]
    [Tooltip("Animator du fusible (etats Fusible_Attente -> Fusible_Ramassage).")]
    [SerializeField] private Animator animateur;
    [Tooltip("Temps laisse a l'animation de ramassage avant de supprimer l'objet.")]
    [SerializeField] private float delaiDestruction = 0.5f;

    [Header("Particules")]
    [Tooltip("Lueur en boucle autour du fusible (enfant). Arretee au ramassage.")]
    [SerializeField] private ParticleSystem effetAura;
    [Tooltip("Prefab d'etincelles cree a la position du fusible au ramassage.")]
    [SerializeField] private ParticleSystem effetRamassage;

    [Header("Son (optionnel)")]
    [SerializeField] private AudioClip sonRamassage;

    private bool ramasse;

    public string TexteInteraction => "Ramasser le " + nomObjet;

    private void Start()
    {
        if (animateur == null)
        {
            Debug.LogWarning("ObjetCollectable : aucun Animator assigne sur " + name + ". L'objet disparaitra sans animation.");
        }
    }

    public void Interagir(InteractionJoueurFPS joueur)
    {
        // Protection contre le double declenchement (test obligatoire du livrable).
        if (ramasse) return;
        ramasse = true;

        // 1. Desactiver les colliders : le rayon ne peut plus toucher l'objet.
        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }

        // 2. Animation de ramassage (Animator).
        if (animateur != null)
        {
            animateur.SetTrigger(ParamRamasser);
        }

        // 3. Particules : la lueur s'eteint, les etincelles jaillissent.
        if (effetAura != null)
        {
            effetAura.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
        if (effetRamassage != null)
        {
            Instantiate(effetRamassage, transform.position, Quaternion.identity);
        }

        // PlayClipAtPoint : le son continue meme apres la destruction de l'objet.
        if (sonRamassage != null)
        {
            AudioSource.PlayClipAtPoint(sonRamassage, transform.position);
        }

        // 4. Prevenir le gestionnaire.
        if (GestionnaireJeu.Instance != null)
        {
            GestionnaireJeu.Instance.AjouterFusible();
        }
        else
        {
            Debug.LogWarning("ObjetCollectable : aucun GestionnaireJeu dans la scene.");
        }

        // 5. Supprimer l'objet apres l'animation.
        Destroy(gameObject, animateur != null ? delaiDestruction : 0f);
    }
}