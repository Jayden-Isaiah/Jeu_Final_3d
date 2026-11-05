using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Porte de service du metro : le joueur entre dans la zone (trigger) et appuie sur E.
// La porte pivote sur sa charniere. Appuyer de nouveau la referme.
// A placer sur un objet qui possede un Collider avec Is Trigger coche (la zone d'interaction).
public class PorteInteractive : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Objet parent place sur la charniere; le battant est son enfant.")]
    [SerializeField] private Transform charniere;

    [Header("Ouverture")]
    [SerializeField] private float angleOuverture = 100f;
    [SerializeField] private float vitesse = 3f;
    [SerializeField] private bool ouvertureUnique = false;

    [Header("Son (optionnel)")]
    [SerializeField] private AudioSource sourceAudio;
    [SerializeField] private AudioClip sonPorte;

    private Quaternion rotationFermee;
    private Quaternion rotationOuverte;
    private bool ouverte;
    private bool joueurDansZone;
    private bool dejaUtilisee;

    private void Start()
    {
        if (charniere == null)
        {
            Debug.LogWarning("PorteInteractive : le champ Charniere n'est pas assigne sur " + name);
            enabled = false;
            return;
        }

        rotationFermee = charniere.localRotation;
        rotationOuverte = rotationFermee * Quaternion.Euler(0f, angleOuverture, 0f);
    }

    private void Update()
    {
        if (joueurDansZone && ToucheInteraction())
        {
            Basculer();
        }

        Quaternion cible = ouverte ? rotationOuverte : rotationFermee;
        charniere.localRotation = Quaternion.Slerp(charniere.localRotation, cible, vitesse * Time.deltaTime);
    }

    private bool ToucheInteraction()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.E);
#endif
    }

    private void Basculer()
    {
        if (ouvertureUnique && dejaUtilisee) return;

        ouverte = !ouverte;
        dejaUtilisee = true;
        Debug.Log(ouverte ? "Porte ouverte." : "Porte fermee.");

        if (sourceAudio != null && sonPorte != null)
        {
            sourceAudio.PlayOneShot(sonPorte);
        }
    }

    private void OnTriggerEnter(Collider autre)
    {
        if (autre.CompareTag("Player"))
        {
            joueurDansZone = true;
            Debug.Log("Appuie sur E pour utiliser la porte.");
        }
    }

    private void OnTriggerExit(Collider autre)
    {
        if (autre.CompareTag("Player"))
        {
            joueurDansZone = false;
        }
    }
}
