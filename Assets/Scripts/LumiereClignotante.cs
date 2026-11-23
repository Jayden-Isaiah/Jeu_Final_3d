using UnityEngine;

// Fait vaciller une lumiere comme un neon defectueux du metro.
// Stabiliser() (appele par le GestionnaireJeu) la fixe a pleine intensite : le courant est revenu.
// A placer sur un objet qui possede un composant Light.
[RequireComponent(typeof(Light))]
public class LumiereClignotante : MonoBehaviour
{
    [Header("Intensite")]
    [SerializeField] private float intensiteMin = 0.2f;
    [SerializeField] private float intensiteMax = 1.5f;

    [Header("Rythme")]
    [Tooltip("Vitesse du vacillement continu.")]
    [SerializeField] private float vitesse = 8f;
    [Tooltip("Chance par seconde que le neon s'eteigne brusquement.")]
    [Range(0f, 1f)][SerializeField] private float chanceCoupure = 0.3f;
    [SerializeField] private float dureeCoupureMax = 0.25f;

    [Header("Objet emissif (optionnel)")]
    [Tooltip("Le tube du neon : son emission suit l'intensite de la lumiere.")]
    [SerializeField] private Renderer rendererNeon;

    private Light lumiere;
    private float graine;
    private float finCoupure;
    private bool stable;
    private Material materiauNeon;
    private Color couleurEmission;

    private void Awake()
    {
        lumiere = GetComponent<Light>();
        graine = Random.Range(0f, 100f); // chaque neon vacille differemment

        if (rendererNeon != null)
        {
            materiauNeon = rendererNeon.material;
            couleurEmission = materiauNeon.GetColor("_EmissionColor");
        }
    }

    private void Update()
    {
        if (stable) return;

        // Coupure brusque aleatoire.
        if (Time.time > finCoupure && Random.value < chanceCoupure * Time.deltaTime)
        {
            finCoupure = Time.time + Random.Range(0.05f, dureeCoupureMax);
        }

        float intensite;
        if (Time.time < finCoupure)
        {
            intensite = 0f;
        }
        else
        {
            // PerlinNoise : variation douce et irreguliere (plus naturelle que Random).
            float bruit = Mathf.PerlinNoise(graine, Time.time * vitesse);
            intensite = Mathf.Lerp(intensiteMin, intensiteMax, bruit);
        }

        AppliquerIntensite(intensite);
    }

    // Le courant est revenu : le neon reste allume.
    public void Stabiliser()
    {
        stable = true;
        AppliquerIntensite(intensiteMax);
    }

    private void AppliquerIntensite(float intensite)
    {
        lumiere.intensity = intensite;

        if (materiauNeon != null)
        {
            materiauNeon.SetColor("_EmissionColor", couleurEmission * (intensite / intensiteMax));
        }
    }
}