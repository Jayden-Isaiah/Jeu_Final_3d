using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

// ENNEMI AVEUGLE (SK Swamp) : ne VOIT jamais le joueur, il reagit seulement au BRUIT.
//
//   Repos -> Patrouille -> (entend un bruit) -> Alerte -> Enquete -> Fouille -> Patrouille
//   S'il touche le joueur pendant qu'il cherche -> Attaque.
//   Si le joueur reste silencieux et loin, il ne le trouve jamais.
//
// A PLACER SUR l'ennemi : NavMeshAgent + Animator + ce script.
// Scene : un NavMesh "cuit" (NavMeshSurface, package AI Navigation) et un joueur avec BruitJoueur.
//
// PARAMETRES DE L'ANIMATOR :
//   Vitesse  (float)   OBLIGATOIRE : Blend Tree Breathing Idle / Injured Walk / Fast Run
//   Fouille  (bool)    OPTIONNEL : boucle "il ecoute / regarde autour"
//   Alerte   (trigger) OPTIONNEL : reaction "j'ai entendu quelque chose"
//   Attaque  (trigger) OPTIONNEL : animation d'attaque
// Les parametres optionnels qui n'existent pas dans ton Animator sont ignores sans erreur :
// sans animation d'alerte/fouille il reste immobile en "Breathing Idle", et sans animation
// d'attaque le coup est donne apres "delaiCoupSansAnimation".
// Si tu ajoutes une animation d'attaque : evenement d'animation sur l'image du coup -> Frapper()
[RequireComponent(typeof(NavMeshAgent))]
public class EnnemiAveugle : MonoBehaviour
{
    public enum Etat { Repos, Patrouille, Alerte, Enquete, Fouille, Attaque }

    [Header("References")]
    [SerializeField] private Transform joueur;
    [SerializeField] private Animator animateur;

    [Header("Ouie (aucune vision)")]
    [Tooltip("1 = normal. Plus grand = meilleure ouie.")]
    [SerializeField] private float multiplicateurOuie = 1f;
    [Tooltip("Chaque mur entre le bruit et l'ennemi multiplie la portee par (1 - cette valeur).")]
    [Range(0f, 1f)][SerializeField] private float attenuationMur = 0.5f;
    [SerializeField] private LayerMask couchesObstacles = ~0;
    [Tooltip("Un bruit de cette portee ou plus fait COURIR l'ennemi.")]
    [SerializeField] private float seuilGrosBruit = 12f;

    [Header("Vitesses")]
    [SerializeField] private float vitessePatrouille = 1.2f;
    [SerializeField] private float vitesseEnquete = 2.4f;
    [SerializeField] private float vitesseCourse = 4.5f;
    [SerializeField] private float vitesseRotation = 6f;

    [Header("Durees (secondes)")]
    [SerializeField] private float dureeRepos = 3f;
    [SerializeField] private float dureeAlerte = 1.2f;
    [SerializeField] private float dureeEcoute = 4f;
    [SerializeField] private int endroitsAFouiller = 2;
    [SerializeField] private float rayonFouille = 5f;
    [SerializeField] private float rayonPatrouille = 14f;

    [Header("Attaque (contact : il ne te voit pas)")]
    [SerializeField] private float porteeAttaque = 1.7f;
    [SerializeField] private float delaiAttaque = 2.5f;
    [Tooltip("Sans animation d'attaque : delai avant que le coup soit donne.")]
    [SerializeField] private float delaiCoupSansAnimation = 0.6f;
    [Tooltip("Appele quand le coup touche le joueur (game over, ecran rouge, etc.).")]
    [SerializeField] private UnityEvent joueurAttrape;

    [Header("Noms des parametres Animator")]
    [SerializeField] private string pVitesse = "Vitesse";
    [SerializeField] private string pFouille = "Fouille";
    [SerializeField] private string pAlerte = "Alerte";
    [SerializeField] private string pAttaque = "Attaque";

    [Header("Sons (optionnel)")]
    [SerializeField] private AudioSource sourceAudio;
    [SerializeField] private AudioClip sonAlerte;
    [SerializeField] private AudioClip sonAttaque;

    public Etat EtatActuel { get; private set; } = Etat.Repos;

    private NavMeshAgent agent;
    private Vector3 positionBruit;
    private float forceBruitEntendu;
    private float minuterie;
    private float derniereAttaque = -99f;
    private int endroitsRestants;
    private bool ecoute;   // true = immobile a ecouter, false = marche vers un endroit
    private readonly HashSet<string> parametresAnimator = new HashSet<string>();

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (animateur == null) animateur = GetComponentInChildren<Animator>();

        // L'agent deplace l'ennemi ; on tourne a la main (plus lent, plus inquietant).
        agent.updateRotation = false;
        if (animateur != null)
        {
            animateur.applyRootMotion = false;
            foreach (AnimatorControllerParameter p in animateur.parameters)
                parametresAnimator.Add(p.name);
        }

        // Un Rigidbody non-kinematic se bat contre le NavMeshAgent (glissement, tremblements).
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }

    private void Start()
    {
        if (joueur == null)
        {
            // Trouve le joueur automatiquement via son composant de deplacement.
            ControleurJoueurFPS c = FindFirstObjectByType<ControleurJoueurFPS>();
            if (c != null) joueur = c.transform;
        }
        ChangerEtat(Etat.Repos);
    }

    private void OnEnable() { SystemeBruit.BruitEmis += EntendreBruit; }
    private void OnDisable() { SystemeBruit.BruitEmis -= EntendreBruit; }

    // ---------------------------------------------------------------- OUIE
    private void EntendreBruit(Vector3 position, float portee)
    {
        if (EtatActuel == Etat.Attaque || EtatActuel == Etat.Alerte) return;

        float entendu = portee * multiplicateurOuie;

        // Les murs etouffent le son.
        Vector3 de = transform.position + Vector3.up;
        Vector3 vers = position + Vector3.up;
        float distance = Vector3.Distance(de, vers);
        int murs = Physics.RaycastAll(de, (vers - de).normalized, distance,
                                      couchesObstacles, QueryTriggerInteraction.Ignore).Length;
        // Le corps du joueur et le sol ne comptent pas comme murs : on retire 1 au besoin.
        entendu *= Mathf.Pow(1f - attenuationMur, Mathf.Max(0, murs - 1));

        if (distance > entendu) return;

        // Deja en chasse : ne reagit qu'a un bruit assez fort par rapport a l'actuel.
        bool enChasse = EtatActuel == Etat.Enquete || EtatActuel == Etat.Fouille;
        if (enChasse && entendu < forceBruitEntendu * 0.6f) return;

        if (!NavMesh.SamplePosition(position, out NavMeshHit impact, 3f, NavMesh.AllAreas)) return;

        positionBruit = impact.position;
        forceBruitEntendu = entendu;

        if (EtatActuel == Etat.Enquete) { agent.SetDestination(positionBruit); return; }
        ChangerEtat(Etat.Alerte);
    }

    // ---------------------------------------------------------------- BOUCLE
    private void Update()
    {
        if (animateur != null)
            animateur.SetFloat(pVitesse, agent.velocity.magnitude, 0.1f, Time.deltaTime);

        Tourner();

        switch (EtatActuel)
        {
            case Etat.Repos:
                if ((minuterie -= Time.deltaTime) <= 0f) ChangerEtat(Etat.Patrouille);
                break;

            case Etat.Patrouille:
                if (Arrive()) ChangerEtat(Etat.Repos);
                break;

            case Etat.Alerte:
                RegarderVers(positionBruit);
                if ((minuterie -= Time.deltaTime) <= 0f) ChangerEtat(Etat.Enquete);
                break;

            case Etat.Enquete:
                if (ToucheJoueur(1f)) { ChangerEtat(Etat.Attaque); break; }
                if (Arrive()) { endroitsRestants = endroitsAFouiller; ChangerEtat(Etat.Fouille); }
                break;

            case Etat.Fouille:
                if (ToucheJoueur(0.8f)) { ChangerEtat(Etat.Attaque); break; }
                MettreAJourFouille();
                break;

            case Etat.Attaque:
                MettreAJourAttaque();
                break;
        }
    }

    private void MettreAJourFouille()
    {
        if (ecoute)
        {
            if ((minuterie -= Time.deltaTime) > 0f) return;
            if (endroitsRestants > 0) { endroitsRestants--; AllerAuProchainEndroit(); }
            else ChangerEtat(Etat.Patrouille);
        }
        else if (Arrive())
        {
            CommencerEcoute();
        }
    }

    private void MettreAJourAttaque()
    {
        agent.isStopped = true;
        if (joueur != null) RegarderVers(joueur.position);

        if (Time.time - derniereAttaque >= delaiAttaque)
        {
            derniereAttaque = Time.time;
            if (!DeclencherTrigger(pAttaque)) Invoke(nameof(Frapper), delaiCoupSansAnimation);
            if (sourceAudio != null && sonAttaque != null) sourceAudio.PlayOneShot(sonAttaque);
        }

        if ((minuterie -= Time.deltaTime) <= 0f)
        {
            // Il est aveugle : si tu t'es eloigne en silence, il fouille l'endroit.
            positionBruit = transform.position;
            endroitsRestants = endroitsAFouiller;
            ChangerEtat(Etat.Fouille);
        }
    }

    // ---------------------------------------------------------------- ETATS
    private void ChangerEtat(Etat suivant)
    {
        EtatActuel = suivant;
        agent.isStopped = false;
        DefinirBool(pFouille, false);

        switch (suivant)
        {
            case Etat.Repos:
                agent.ResetPath();
                agent.isStopped = true;
                minuterie = dureeRepos + Random.Range(0f, 2f);
                break;

            case Etat.Patrouille:
                agent.speed = vitessePatrouille;
                agent.SetDestination(PointAleatoire(transform.position, rayonPatrouille));
                break;

            case Etat.Alerte:
                agent.ResetPath();
                agent.isStopped = true;
                minuterie = dureeAlerte;
                DeclencherTrigger(pAlerte);
                if (sourceAudio != null && sonAlerte != null) sourceAudio.PlayOneShot(sonAlerte);
                break;

            case Etat.Enquete:
                agent.speed = forceBruitEntendu >= seuilGrosBruit ? vitesseCourse : vitesseEnquete;
                agent.SetDestination(positionBruit);
                break;

            case Etat.Fouille:
                CommencerEcoute();
                break;

            case Etat.Attaque:
                agent.ResetPath();
                agent.isStopped = true;
                minuterie = delaiAttaque + 1f;
                derniereAttaque = -99f;
                break;
        }
    }

    private void CommencerEcoute()
    {
        ecoute = true;
        agent.ResetPath();
        agent.isStopped = true;
        minuterie = dureeEcoute;
        DefinirBool(pFouille, true);
    }

    private void AllerAuProchainEndroit()
    {
        ecoute = false;
        agent.isStopped = false;
        agent.speed = vitessePatrouille;
        DefinirBool(pFouille, false);
        agent.SetDestination(PointAleatoire(positionBruit, rayonFouille));
    }

    // ---------------------------------------------------------------- OUTILS
    private void DefinirBool(string nom, bool valeur)
    {
        if (animateur != null && parametresAnimator.Contains(nom)) animateur.SetBool(nom, valeur);
    }

    // Retourne false si le parametre n'existe pas (pas d'animation correspondante).
    private bool DeclencherTrigger(string nom)
    {
        if (animateur == null || !parametresAnimator.Contains(nom)) return false;
        animateur.SetTrigger(nom);
        return true;
    }

    private bool Arrive()
    {
        return !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f;
    }

    private bool ToucheJoueur(float facteur)
    {
        return joueur != null && Vector3.Distance(transform.position, joueur.position) <= porteeAttaque * facteur;
    }

    private void Tourner()
    {
        Vector3 v = new Vector3(agent.velocity.x, 0f, agent.velocity.z);
        if (v.sqrMagnitude < 0.05f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(v), vitesseRotation * Time.deltaTime);
    }

    private void RegarderVers(Vector3 positionMonde)
    {
        Vector3 d = positionMonde - transform.position;
        d.y = 0f;
        if (d.sqrMagnitude < 0.01f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(d), vitesseRotation * Time.deltaTime);
    }

    private Vector3 PointAleatoire(Vector3 origine, float rayon)
    {
        for (int i = 0; i < 12; i++)
        {
            if (NavMesh.SamplePosition(origine + Random.insideUnitSphere * rayon, out NavMeshHit impact, rayon, NavMesh.AllAreas))
                return impact.position;
        }
        return origine;
    }

    // A appeler par un EVENEMENT D'ANIMATION sur l'image du coup de l'animation d'attaque.
    public void Frapper()
    {
        if (joueur == null) return;
        if (Vector3.Distance(transform.position, joueur.position) > porteeAttaque + 0.6f) return;
        joueurAttrape.Invoke();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, porteeAttaque);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 7f * multiplicateurOuie);     // portee d'un pas de marche
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, 15f * multiplicateurOuie);    // portee d'un pas de course
    }
}