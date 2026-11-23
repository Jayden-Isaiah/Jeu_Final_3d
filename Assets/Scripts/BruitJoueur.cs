using UnityEngine;

// BRUIT DU JOUEUR : a placer sur le prefab JoueurFPS (a cote de ControleurJoueurFPS).
// Ne modifie PAS le controleur : lit simplement la vitesse du Rigidbody.
//   Immobile            -> silence total
//   Marche  (~3 m/s)    -> bruit moyen
//   Course  (~5.5 m/s)  -> gros bruit (shift)
[RequireComponent(typeof(Rigidbody))]
public class BruitJoueur : MonoBehaviour
{
    [Header("Portee du bruit (metres)")]
    [SerializeField] private float porteeMarche = 7f;
    [SerializeField] private float porteeCourse = 15f;

    [Header("Seuils de vitesse (m/s)")]
    [Tooltip("En dessous : le joueur est considere immobile (silence).")]
    [SerializeField] private float vitesseMinimale = 0.5f;
    [Tooltip("Au dessus : le joueur court. (Marche = 3, course = 5.5 dans ControleurJoueurFPS)")]
    [SerializeField] private float seuilCourse = 4.2f;

    [Header("Rythme des pas")]
    [SerializeField] private float intervallePasMarche = 0.55f;
    [SerializeField] private float intervallePasCourse = 0.35f;

    private Rigidbody corps;
    private float minuterie;

    private void Awake()
    {
        corps = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        Vector3 v = corps.linearVelocity;
        float vitesse = new Vector2(v.x, v.z).magnitude;

        if (vitesse < vitesseMinimale)
        {
            minuterie = 0f;   // le prochain pas fera du bruit tout de suite
            return;
        }

        minuterie -= Time.deltaTime;
        if (minuterie > 0f) return;

        bool court = vitesse >= seuilCourse;
        minuterie = court ? intervallePasCourse : intervallePasMarche;
        SystemeBruit.Emettre(transform.position, court ? porteeCourse : porteeMarche);
    }
}