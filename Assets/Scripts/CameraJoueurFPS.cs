using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Camera a la premiere personne : placee a la hauteur des yeux (enfant du joueur).
// La souris tourne le corps du joueur (gauche/droite) et la camera (haut/bas).
// Echap libere le curseur; clic gauche le recapture.
public class CameraJoueurFPS : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform corpsJoueur;

    [Header("Souris")]
    [SerializeField] private float sensibilite = 2f;
    [SerializeField] private float angleMaxVertical = 80f;

    [Header("Fluidite")]
    [Tooltip("0 = aucun lissage. 0.03 a 0.06 donne un mouvement doux.")]
    [SerializeField] private float lissage = 0.04f;

    [Header("Balancement de marche (optionnel)")]
    [SerializeField] private ControleurJoueurFPS controleur;
    [SerializeField] private float amplitudeBalancement = 0.04f;
    [SerializeField] private float frequenceBalancement = 8f;

    private float angleHorizontal;
    private float angleVertical;
    private float angleHorizontalLisse;
    private float angleVerticalLisse;
    private float vitesseH;
    private float vitesseV;

    private Vector3 positionDepart;
    private float tempsBalancement;

    private void Start()
    {
        if (corpsJoueur == null)
        {
            Debug.LogWarning("CameraJoueurFPS : le champ Corps Joueur n'est pas assigne.");
            enabled = false;
            return;
        }

        positionDepart = transform.localPosition;
        angleHorizontal = angleHorizontalLisse = corpsJoueur.eulerAngles.y;
        VerrouillerCurseur(true);
    }

    private void Update()
    {
        GererCurseur();
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 souris = LireSouris();

        angleHorizontal += souris.x * sensibilite;
        angleVertical -= souris.y * sensibilite;
        angleVertical = Mathf.Clamp(angleVertical, -angleMaxVertical, angleMaxVertical);

        // Lissage pour eviter les mouvements brusques.
        angleHorizontalLisse = Mathf.SmoothDampAngle(angleHorizontalLisse, angleHorizontal, ref vitesseH, lissage);
        angleVerticalLisse = Mathf.SmoothDamp(angleVerticalLisse, angleVertical, ref vitesseV, lissage);

        corpsJoueur.rotation = Quaternion.Euler(0f, angleHorizontalLisse, 0f);
        transform.localRotation = Quaternion.Euler(angleVerticalLisse, 0f, 0f);

        AppliquerBalancement();
    }

    private Vector2 LireSouris()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current == null) return Vector2.zero;
        // Meme echelle que l'ancien Input.GetAxis("Mouse X").
        return Mouse.current.delta.ReadValue() * 0.1f;
#else
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
#endif
    }

    private void GererCurseur()
    {
#if ENABLE_INPUT_SYSTEM
        bool echap = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        bool clic = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#else
        bool echap = Input.GetKeyDown(KeyCode.Escape);
        bool clic = Input.GetMouseButtonDown(0);
#endif
        if (echap) VerrouillerCurseur(false);
        else if (clic && Cursor.lockState != CursorLockMode.Locked) VerrouillerCurseur(true);
    }

    private void VerrouillerCurseur(bool verrouille)
    {
        Cursor.lockState = verrouille ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !verrouille;
    }

    private void AppliquerBalancement()
    {
        if (controleur == null) return;

        if (controleur.EnMouvement)
        {
            tempsBalancement += Time.deltaTime * frequenceBalancement;
            float y = Mathf.Sin(tempsBalancement) * amplitudeBalancement;
            transform.localPosition = positionDepart + new Vector3(0f, y, 0f);
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, positionDepart, 10f * Time.deltaTime);
        }
    }
}
