using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int nbr_batterie = 0;
    [SerializeField] private int max_batterie = 3;
    [SerializeField] private Text ui_batterie;

    public static GameManager instance;


    void Start()
    {
        if (instance == null) instance = this;
        ui_batterie.text = nbr_batterie + "/" + max_batterie;
    }

    void Update()
    {

    }

    public void CollectBatterie() 
    {
        nbr_batterie++;
        ui_batterie.text = nbr_batterie + "/" + max_batterie;
    }
}
