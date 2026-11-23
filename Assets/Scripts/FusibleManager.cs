using UnityEngine;

public class Fusible : MonoBehaviour
{

    [SerializeField] private GameObject fusible_1;
    [SerializeField] private GameObject fusible_2;
    [SerializeField] private GameObject fusible_3;
    [SerializeField] private Renderer fusibleStatusLight;
    public bool isOn = false;

    void Start()
    {
        fusible_1.SetActive(false);
        fusible_2.SetActive(false);
        fusible_3.SetActive(false);
        fusibleStatusLight.material.color = Color.red;
    }

    void Update()
    {
        if (isOn) fusibleStatusLight.material.color = Color.green;
        else fusibleStatusLight.material.color = Color.red;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E)) 
        {
            switch (GameManager.instance.nbr_batterie) 
            {
                case 0:
                    Debug.Log("Vous avez aucunne batterie en possession.");
                    return;
                case 1:
                    fusible_1.SetActive(true);
                    Debug.Log("Une baterrie ajouté.");
                    return;
                case 2:
                    fusible_1.SetActive(true);
                    fusible_2.SetActive(true);
                    Debug.Log("Deux baterrie ajouté.");
                    return;
                case 3:
                    fusible_1.SetActive(true);
                    fusible_2.SetActive(true);
                    fusible_3.SetActive(true);
                    isOn = true;
                    Debug.Log("Trois baterrie ajouté et la porte est ouverte.");
                    return;
            }
        }
    }
}
