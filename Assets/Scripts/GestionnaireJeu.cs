using UnityEngine;
using UnityEngine.Events;
using TMPro;

// GESTIONNAIRE DU JEU : la seule source de verite pour la progression.
// - Les fusibles le PREVIENNENT (ils ne comptent rien eux-memes).
// - Le joueur ne connait pas la progression.
// - Quand les fusibles sont tous ramasses, le courant revient :
//   les neons cessent de clignoter et les lumieres de secours s'allument.
// A placer sur un GameObject vide nomme GestionnaireJeu (un seul dans la scene).
public class GestionnaireJeu : MonoBehaviour
{
    public static GestionnaireJeu Instance { get; private set; }

    [Header("Objectif")]
    [SerializeField] private int fusiblesRequis = 3;

    [Header("Interface (optionnel)")]
    [SerializeField] private TextMeshProUGUI texteCompteur;
    [SerializeField] private TextMeshProUGUI texteMessage;
    [SerializeField] private float dureeMessage = 2.5f;

    [Header("Retour du courant")]
    [Tooltip("Neons qui arretent de clignoter quand le courant revient.")]
    [SerializeField] private LumiereClignotante[] neonsAStabiliser;
    [Tooltip("Objets desactives au depart, actives quand le courant revient (lumieres de secours...).")]
    [SerializeField] private GameObject[] objetsAActiver;
    [Tooltip("Actions supplementaires configurables dans l'Inspector.")]
    [SerializeField] private UnityEvent objectifAtteint;

    private int fusiblesRamasses;
    private bool termine;
    private float finMessage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Deux GestionnaireJeu dans la scene : le second est supprime.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        MettreAJourCompteur();
        AfficherMessage("Panne de courant. Trouve " + fusiblesRequis + " fusibles.");
    }

    private void Update()
    {
        if (texteMessage != null && texteMessage.enabled && Time.time > finMessage)
        {
            texteMessage.enabled = false;
        }
    }

    // Appele par ObjetCollectable.
    public void AjouterFusible()
    {
        if (termine) return;

        fusiblesRamasses++;
        MettreAJourCompteur();
        Debug.Log("Fusibles : " + fusiblesRamasses + " / " + fusiblesRequis);

        if (fusiblesRamasses >= fusiblesRequis)
        {
            RetablirCourant();
        }
        else
        {
            int restants = fusiblesRequis - fusiblesRamasses;
            AfficherMessage("Fusible ramasse. Encore " + restants + ".");
        }
    }

    private void RetablirCourant()
    {
        termine = true;
        AfficherMessage("Courant retabli !");
        Debug.Log("Objectif atteint : courant retabli.");

        foreach (LumiereClignotante neon in neonsAStabiliser)
        {
            if (neon != null) neon.Stabiliser();
        }

        foreach (GameObject objet in objetsAActiver)
        {
            if (objet != null) objet.SetActive(true);
        }

        objectifAtteint.Invoke();
    }

    private void MettreAJourCompteur()
    {
        if (texteCompteur != null)
        {
            texteCompteur.text = "Fusibles : " + fusiblesRamasses + " / " + fusiblesRequis;
        }
    }

    private void AfficherMessage(string message)
    {
        if (texteMessage == null) return;
        texteMessage.text = message;
        texteMessage.enabled = true;
        finMessage = Time.time + dureeMessage;
    }
}