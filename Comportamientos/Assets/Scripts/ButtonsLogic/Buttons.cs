using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Buttons : MonoBehaviour
{
    [Header("Character Spawns")]
    [SerializeField] Button spawnExplorer;
    [SerializeField] Button spawnPolice;
    [SerializeField] Button spawnCriminal;
    [SerializeField] Button spawnBeast;
    [SerializeField] Button spawnGhost;

    [Header("Character Cameras")]
    [SerializeField] Button explorerCamera;
    [SerializeField] Button policeCamera;
    [SerializeField] Button criminalCamera;
    [SerializeField] Button beastCamera;
    [SerializeField] Button ghostCamera;
    [SerializeField] Button generalCamera;

    [Header("Character Prefabs")]
    [SerializeField] GameObject explorer;
    [SerializeField] GameObject police;
    [SerializeField] GameObject criminal;
    [SerializeField] GameObject beast;
    [SerializeField] GameObject ghost;

    [Header("Character Positions")]
    [SerializeField] Transform explorerPosition;
    [SerializeField] Transform policePosition;
    [SerializeField] Transform criminalPosition;
    [SerializeField] Transform beastPosition;
    [SerializeField] Transform ghostPosition;

    [Header("Character Number Limit")]
    [SerializeField] int explorerN;
    [SerializeField] int policeN;
    [SerializeField] int criminalN;
    [SerializeField] int beastN;
    [SerializeField] int ghostN;


    [Header("General Camera")]
    [SerializeField] Camera mainCamera;

    // Start is called before the first frame update
    void Start()
    {
        spawnExplorer.onClick.AddListener(()=> 
        { 
            if(FindObjectsByType<ExplorerBehaviour>(FindObjectsSortMode.None).Length < explorerN) {
                SpawnCharacter(explorer, explorerPosition);
            }   
        });

        spawnPolice.onClick.AddListener(()=> 
        { 
            if(FindObjectsByType<PoliceBehaviour>(FindObjectsSortMode.None).Length < policeN){
                SpawnCharacter(police, policePosition);
            }
            
        });

        spawnCriminal.onClick.AddListener(()=> 
        {
            if (FindObjectsByType<CriminalBehaviour>(FindObjectsSortMode.None).Length < criminalN)
            SpawnCharacter(criminal, criminalPosition); 
        });

        spawnBeast.onClick.AddListener(()=> 
        {
            if (FindObjectsByType<BeastBehaviour>(FindObjectsSortMode.None).Length < beastN)
            SpawnCharacter(beast, beastPosition); 
        });

        spawnGhost.onClick.AddListener(()=> 
        {
            if (FindObjectsByType<GhostBehaviour>(FindObjectsSortMode.None).Length < ghostN)
            SpawnCharacter(ghost, ghostPosition); });


        explorerCamera.onClick.AddListener(() =>
        {
            var explorer = FindAnyObjectByType<ExplorerBehaviour>();
            if(explorer != null)
            {
                var explorerCamera = explorer.GetComponentInChildren<Camera>();
                if(explorerCamera != null)
                {
                    foreach(var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    {
                        camera.enabled = false;

                    }
                    explorerCamera.enabled = true; 
                }
            }
        });

        policeCamera.onClick.AddListener(() =>
        {
            var police = FindAnyObjectByType<PoliceBehaviour>();
            if (police != null)
            {
                var policeCamera = police.GetComponentInChildren<Camera>();
                if (policeCamera != null)
                {
                    foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    {
                        camera.enabled = false;

                    }
                    policeCamera.enabled = true;
                }
            }
        });

        criminalCamera.onClick.AddListener(() =>
        {
            var criminal = FindAnyObjectByType<CriminalBehaviour>();
            if (criminal != null)
            {
                var criminalCamera = criminal.GetComponentInChildren<Camera>();
                if (criminalCamera != null)
                {
                    foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    {
                        camera.enabled = false;

                    }
                    criminalCamera.enabled = true;
                }
            }
        });

        beastCamera.onClick.AddListener(() =>
        {
            var beast = FindAnyObjectByType<BeastBehaviour>();
            if (criminal != null)
            {
                var beastCamera = beast.GetComponentInChildren<Camera>();
                if (beastCamera != null)
                {
                    foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    {
                        camera.enabled = false;

                    }
                    beastCamera.enabled = true;
                }
            }
        });

        ghostCamera.onClick.AddListener(() =>
        {
            var ghost = FindAnyObjectByType<GhostBehaviour>();
            if (ghost != null)
            {
                var ghostCamera = ghost.GetComponentInChildren<Camera>();
                if (ghostCamera != null)
                {
                    foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    {
                        camera.enabled = false;

                    }
                    ghostCamera.enabled = true;
                }
            }
        });

        generalCamera.onClick.AddListener(() =>
        {
            foreach (var camera in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                camera.enabled = false;

            }
            mainCamera.enabled = true;
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SpawnCharacter(GameObject character, Transform position)
    {
        var characterClone = Instantiate(character, position.position, position.rotation);
        characterClone.transform.position = position.position;
        characterClone.SetActive(true);
    }
}
