using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

// A placer dans la SCENE DU MENU (ex. sur le Canvas).
// Le bouton Reprendre appelle Reprendre() via son evenement On Click ().
public class MenuPause : MonoBehaviour
{
    [Header("Musique du menu (optionnel)")]
    [Tooltip("AudioSource de la musique du menu. Elle joue meme si le son du jeu est en pause.")]
    [SerializeField] private AudioSource musique;

    private void Awake()
    {
        VerifierEventSystem();

        if (musique != null)
        {
            musique.ignoreListenerPause = true;   // la pause coupe le jeu, pas le menu
            musique.Play();
        }
    }

    // Sans EventSystem, ou avec un module d'entree incompatible, les boutons ne recoivent aucun clic.
    private void VerifierEventSystem()
    {
        EventSystem[] systemes = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);

        if (systemes.Length == 0)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
            Debug.Log("[Pause] Aucun EventSystem : un EventSystem a ete cree pour le menu.");
            return;
        }

        if (systemes.Length > 1)
        {
            Debug.LogWarning("[Pause] " + systemes.Length + " EventSystems actifs. Supprime celui de la scene MenuPause.");
        }

#if ENABLE_INPUT_SYSTEM
        // L'ancien module (StandaloneInputModule) ne lit pas la souris avec le nouvel Input System.
        foreach (EventSystem es in systemes)
        {
            StandaloneInputModule ancien = es.GetComponent<StandaloneInputModule>();
            if (ancien != null)
            {
                Destroy(ancien);
                if (es.GetComponent<InputSystemUIInputModule>() == null)
                {
                    es.gameObject.AddComponent<InputSystemUIInputModule>();
                }
                Debug.LogWarning("[Pause] L'EventSystem '" + es.name + "' utilisait StandaloneInputModule : " +
                                 "remplace par InputSystemUIInputModule. Corrige-le aussi dans la scene " +
                                 "(bouton 'Replace with InputSystemUIInputModule').");
            }
        }
#endif
    }

    // Relie au bouton : On Click () > MenuPause.Reprendre
    public void Reprendre()
    {
        Debug.Log("[Pause] Bouton Reprendre clique.");
        GestionnairePause.Reprendre();
    }
}