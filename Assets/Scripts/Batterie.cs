using UnityEngine;

public class Batterie : MonoBehaviour
{
    [SerializeField] ParticleSystem _particleSystem;

    void Start()
    {
        _particleSystem.Stop();
    }

    void Update()
    {
        
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E))
        {
            GameManager.instance.CollectBatterie();
            _particleSystem.Play();
            gameObject.SetActive(false);
            Debug.Log("Batterie collectée.");
        }
    }
}
