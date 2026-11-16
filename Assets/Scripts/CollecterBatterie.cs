using UnityEngine;

public class CollecterBatterie : MonoBehaviour
{
    void Start()
    {
        
    }

    void Update()
    {

    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Batterie") && Input.GetKeyDown(KeyCode.E)) 
        {
            GameManager.instance.CollectBatterie();
            other.gameObject.SetActive(false);
            Debug.Log("Batterie collectée.");
            
        }
    }
}
