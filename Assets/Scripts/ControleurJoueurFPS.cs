using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Deplacement du joueur a la premiere personne avec un Rigidbody.
// WASD / fleches : avancer, reculer, pas lateraux. Shift : courir.
// Le deplacement suit l'orientation du joueur (la ou il regarde).
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class ControleurJoueurFPS : MonoBehaviour
{
    [Header("Vitesses")]
    [SerializeField] private float vitesseMarche = 3f;
    [SerializeField] private float vitesseCourse = 5.5f;

    [Header("Fluidite")]
    [Tooltip("Plus la valeur est grande, plus le joueur atteint vite sa vitesse.")]
    [SerializeField] private float acceleration = 10f;

    private Rigidbody corps;
    private Vector3 directionLocale;   // X = lateral, Z = avant/arriere
    private bool court;

    public bool EnMouvement => directionLocale.sqrMagnitude > 0.01f;

    private void Awake()
    {
        corps = GetComponent<Rigidbody>();

        // Le joueur ne doit jamais basculer : la rotation est geree par la camera.
        corps.freezeRotation = true;
        corps.interpolation = RigidbodyInterpolation.Interpolate;
        corps.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void Update()
    {
        float x = 0f;
        float z = 0f;

#if ENABLE_INPUT_SYSTEM
        Keyboard clavier = Keyboard.current;
        if (clavier != null)
        {
            if (clavier.aKey.isPressed || clavier.leftArrowKey.isPressed) x -= 1f;
            if (clavier.dKey.isPressed || clavier.rightArrowKey.isPressed) x += 1f;
            if (clavier.sKey.isPressed || clavier.downArrowKey.isPressed) z -= 1f;
            if (clavier.wKey.isPressed || clavier.upArrowKey.isPressed) z += 1f;
            court = clavier.leftShiftKey.isPressed;
        }
#else
        x = Input.GetAxisRaw("Horizontal");
        z = Input.GetAxisRaw("Vertical");
        court = Input.GetKey(KeyCode.LeftShift);
#endif

        // Normaliser : la diagonale n'est pas plus rapide que l'avant.
        directionLocale = new Vector3(x, 0f, z).normalized;
    }

    private void FixedUpdate()
    {
        // Convertir la direction locale en direction monde (selon l'orientation du joueur).
        Vector3 directionMonde = transform.TransformDirection(directionLocale);
        float vitesse = court ? vitesseCourse : vitesseMarche;

        Vector3 cible = directionMonde * vitesse;
        Vector3 actuelle = corps.linearVelocity;

        // Interpolation horizontale seulement; la gravite garde le controle de Y.
        Vector3 horizontale = Vector3.Lerp(
            new Vector3(actuelle.x, 0f, actuelle.z),
            cible,
            acceleration * Time.fixedDeltaTime);

        corps.linearVelocity = new Vector3(horizontale.x, actuelle.y, horizontale.z);
    }
}
