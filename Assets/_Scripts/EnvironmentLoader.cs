using DevLocker.Utils;
using RobotAtVirtualHome;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnvironmentLoader : MonoBehaviour
{
    [SerializeField] string commandTopic = "/load_environment";
    [SerializeField] SceneReference scene;
    [SerializeField] SimulationOptions options;

    void Start()
    {
        var ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<Int32Msg>(commandTopic, OnCommandReceived);
        Debug.Log($"Waiting to receive commands on {commandTopic}");
    }

    void OnCommandReceived(Int32Msg msg)
    {
        if(msg.data == 0)
        {
            SceneManager.UnloadSceneAsync(scene.ScenePath, UnloadSceneOptions.None);
            Debug.Log("Unloading Robot@VirtualHome scene");
        }
        
        else
        {
            options.houseSelected = msg.data;
            Debug.Log($"Loading Robot@VirtualHome scene with environment {options.houseSelected}");
            SceneManager.LoadScene(scene.ScenePath, LoadSceneMode.Additive);
        }
    }

}