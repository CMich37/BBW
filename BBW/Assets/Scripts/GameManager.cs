using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Player")]
    public GameObject player;
    [Header("House")]
    public HouseGenerator houseGenerator;
    [Header("Husband")]
    public GameObject husband;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartSpawn();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void StartSpawn()
    {
        
    }
}
