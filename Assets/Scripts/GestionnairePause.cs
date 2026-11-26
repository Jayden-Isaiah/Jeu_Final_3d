using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// A placer dans la SCENE DU JEU (ex. sur l'objet GestionnaireJeu). UN SEUL dans le projet.
// Echap ou P : charge la scene du menu PAR-DESSUS le jeu (chargement additif).
// La scene du jeu n'est jamais dechargee : joueur, fusibles et compteur restent
// exactement dans leur etat, ce qui permet de reprendre la ou on etait.
public class GestionnairePause : MonoBehaviour
{
    // Lu par les scripts du joueur pour ignorer les entrees pendant la pause.
    public static bool EstEnPause { get; private set; }

    private static GestionnairePause instance;

    [Tooltip("Nom exact de la scene du menu (doit etre dans File > Build Profiles > Scene List).")]
    [SerializeField] private string nomSceneMenu = "MenuPause";

    [Tooltip("Affiche dans la Console chaque etape de la pause (utile pour deboguer).")]
    [SerializeField] private bool messagesDebug = true;

    private bool chargementEnCours;
    private float debutChargement;

    private void Awake()
    {
        // Un deuxieme GestionnairePause (ex. dans la scene du menu) ne doit PAS
        // remettre le jeu en marche : on retire seulement ce composant en trop.
        if (instance != null && instance != this)
        {
            Debug.LogWarning("GestionnairePause : un deuxieme exemplaire a ete trouve sur '" + name +
                             "' (scene " + gameObject.scene.name + "). Retire-le : il doit etre seulement dans la scene du jeu.");
            Destroy(this);
            return;
        }

        instance = this;
        EstEnPause = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;

#if ENABLE_INPUT_SYSTEM
        // Si l'Input System lit le clavier dans FixedUpdate, plus rien n'est lu
        // quand Time.timeScale = 0 : ni les touches, ni les clics sur les boutons.
        if (InputSystem.settings.updateMode == InputSettings.UpdateMode.ProcessEventsInFixedUpdate)
        {
            InputSystem.settings.updateMode = InputSettings.UpdateMode.ProcessEventsInDynamicUpdate;
            Debug.LogWarning("GestionnairePause : l'Input System etait en 'Process Events In Fixed Update', " +
                             "ce qui bloque le clavier et la souris pendant la pause. Passe en Dynamic Update. " +
                             "Corrige-le aussi dans Edit > Project Settings > Input System Package > Update Mode.");
        }
#endif
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            EstEnPause = false;
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }

    private void Update()
    {
        // Securite : si un chargement reste bloque plus de 3 secondes, on debloque.
        if (chargementEnCours && Time.unscaledTime - debutChargement > 3f)
        {
            Debug.LogWarning("GestionnairePause : chargement bloque, remise a zero.");
            chargementEnCours = false;
        }

        if (!ToucheMenu()) return;

        Log("Touche pause detectee. EstEnPause = " + EstEnPause + ", chargement en cours = " + chargementEnCours);
        if (chargementEnCours) return;

        if (EstEnPause) Reprendre();
        else MettreEnPause();
    }

    private bool ToucheMenu()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard clavier = Keyboard.current;
        return clavier != null && (clavier.escapeKey.wasPressedThisFrame || clavier.pKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P);
#endif
    }

    private void MettreEnPause()
    {
        if (!Application.CanStreamedLevelBeLoaded(nomSceneMenu))
        {
            Debug.LogError("GestionnairePause : la scene '" + nomSceneMenu +
                           "' est introuvable. Ajoute-la dans File > Build Profiles > Scene List.");
            return;
        }

        EstEnPause = true;
        Time.timeScale = 0f;          // fige la physique, les animations, les particules et Time.time
        AudioListener.pause = true;   // coupe les sons du jeu (la musique du menu les ignore)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        DemarrerChargement();
        AsyncOperation op = SceneManager.LoadSceneAsync(nomSceneMenu, LoadSceneMode.Additive);
        op.completed += _ =>
        {
            chargementEnCours = false;
            Log("Menu charge.");
        };
    }

    // Appele par la touche Echap/P ou par le bouton Reprendre du menu.
    public static void Reprendre()
    {
        if (instance == null)
        {
            Debug.LogWarning("GestionnairePause : aucun GestionnairePause actif dans la scene du jeu.");
            return;
        }
        if (!EstEnPause || instance.chargementEnCours) return;

        instance.Log("Reprise du jeu...");
        instance.DemarrerChargement();

        Scene menu = SceneManager.GetSceneByName(instance.nomSceneMenu);
        AsyncOperation op = menu.isLoaded ? SceneManager.UnloadSceneAsync(menu) : null;

        if (op == null)
        {
            instance.chargementEnCours = false;
            instance.Relancer();
            return;
        }

        op.completed += _ =>
        {
            if (instance == null) return;
            instance.chargementEnCours = false;
            instance.Relancer();
        };
    }

    private void DemarrerChargement()
    {
        chargementEnCours = true;
        debutChargement = Time.unscaledTime;
    }

    private void Relancer()
    {
        EstEnPause = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Log("Jeu repris.");
    }

    private void Log(string message)
    {
        if (messagesDebug) Debug.Log("[Pause] " + message);
    }
}