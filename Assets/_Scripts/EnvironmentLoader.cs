using Cysharp.Threading.Tasks;
using DevLocker.Utils;
using RobotAtVirtualHome;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnvironmentLoader : MonoBehaviour
{
    [SerializeField] string commandTopic = "/load_environment";
    [SerializeField] SceneReference sceneRef;
    [SerializeField] SimulationOptions options;

    void Start()
    {
        var ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<Int32Msg>(commandTopic, (msg) => OnCommandReceived(msg).Forget());
        Debug.Log($"Waiting to receive commands on {commandTopic}");
    }

    async UniTaskVoid OnCommandReceived(Int32Msg msg)
    {
        Debug.Log("Received load msg");
        var ros = ROSConnection.GetOrCreateInstance();
        ros.Disconnect();

        if (msg.data == 0)
        {
            await SceneManager.UnloadSceneAsync(sceneRef.ScenePath, UnloadSceneOptions.None);
            Debug.Log("Unloading Robot@VirtualHome scene");
            ros.Connect();
        }
        else
        {
            Scene scene = SceneManager.GetSceneByPath(sceneRef.ScenePath);
            if (scene.isLoaded)
            {
                Debug.Log("Unloading Robot@VirtualHome scene");
                await SceneManager.UnloadSceneAsync(sceneRef.ScenePath, UnloadSceneOptions.None);
            }

            ros.Connect();
            LoadEnvironment(msg.data);
        }
    }

    void LoadEnvironment(int id)
    {
        options.houseSelected = id;
        Debug.Log($"Loading Robot@VirtualHome scene with environment {options.houseSelected}");
        SceneManager.LoadScene(sceneRef.ScenePath, LoadSceneMode.Additive);
    }

}