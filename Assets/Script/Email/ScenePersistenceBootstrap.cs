using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
public class ScenePersistenceBootstrap : MonoBehaviour
{
    void Awake()
    {
        // Singleton — only one bootstrap should exist
        if (FindObjectsOfType<ScenePersistenceBootstrap>().Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }
 
    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
 
    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // After any scene loads, if we have pending data, it will be consumed
        // by DeliveryManager.Start() and ShopSystem.Start() calling TryRestore.
        if (ScenePersistenceManager.HasPendingRestore)
            Debug.Log($"[ScenePersist] Scene '{scene.name}' loaded. Pending restore ready.");
    }
}