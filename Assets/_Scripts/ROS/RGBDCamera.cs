using UnityEngine;
using System.Linq;
using System.Threading.Tasks;

using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.Core;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

using RosMessageTypes.Sensor;
using Header = RosMessageTypes.Std.HeaderMsg;
using ImageMsg = RosMessageTypes.Sensor.ImageMsg;
using System;

[RequireComponent(typeof(Camera))]
public class RGBDCamera : MonoBehaviour
{
    public string colorTopic = "/rgbd/color/raw";
    public string depthTopic = "/rgbd/depth/raw";
    public string infoTopic = "/rgbd/info";
    public RenderTexture colorRT;
    public RenderTexture depthRT;
    public Material depthMat;
    public float frequency = 30;

    CustomTimers.Countdown countdown;

    private ROSConnection ros;
    private Texture2D colorImage;
    private Texture2D depthImage;
    Camera _camera;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ImageMsg>(colorTopic, 1);
        ros.RegisterPublisher<ImageMsg>(depthTopic, 1);
        ros.RegisterPublisher<CameraInfoMsg>(infoTopic, 1);
        countdown = new(1 / frequency);
        _camera = GetComponent<Camera>();
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (!countdown.done)
            return;


        Header header = new(new TimeStamp(Clock.time), gameObject.name);
        publishInfo(header);
        publishColor(src, header);
        publishDepth(src, header);
        countdown.Restart();
    }

    void publishInfo(Header header)
    {
        var msg = CameraInfoGenerator.ConstructCameraInfoMessage(_camera, header);
        ros.Publish(infoTopic, msg);
    }

    void publishColor(RenderTexture src, Header header)
    {
        //vertical flip on gpu
        Graphics.Blit(src, colorRT, new Vector2(1, -1), new Vector2(0, 1));
        //Graphics.Blit(src, colorRT, new Vector2(1, 1), new Vector2(0, 0));
        ImageMsg msg = ReadImage(header, colorRT, TextureFormat.RGB24, ref colorImage);
        ros.Publish(colorTopic, msg);
    }

    void publishDepth(RenderTexture src, Header header)
    {
        //read depth buffer (material also does the vertical flip)
        Graphics.Blit(src, depthRT, depthMat, 0); // the pass index is required! otherwise nothing happens

        ImageMsg msg = ReadImage(header, depthRT, TextureFormat.R16, ref depthImage);
        ros.Publish(depthTopic, msg);
    }

    ImageMsg ReadImage(Header header, RenderTexture renderTexture, TextureFormat format, ref Texture2D image)
    {
        //read render texture (gpu) to Texture2D (cpu)  
        RenderTexture.active = renderTexture;
        if (!image)
            image = new Texture2D(renderTexture.width, renderTexture.height, format, false);
        image.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        image.Apply();
        RenderTexture.active = null;

        //compressed
        //ImageMsg msg = new(header,
        //	"png",
        //	image.EncodeToPNG());

        string encodingStr="";
        uint step = 0;
        switch(format)
        {
            case TextureFormat.RGB24:
                encodingStr = "rgb8";
                step=(uint)image.width * 3;
                break;
            case TextureFormat.R16:
                encodingStr = "mono16";
                step=(uint)image.width * 2;
                break;
            default:
                Debug.LogError($"Texture format {format} unsupported!");
                throw new Exception();
        }

        //raw
        ImageMsg msg = new(header,
            (uint)image.height, (uint)image.width, encodingStr, (byte)0, step,
            image.GetRawTextureData().ToArray());

        return msg;
    }
    void FlipVerticallyCPU(Texture2D image)
    {
        Color[] pixels = image.GetPixels();
        Color[] pixelsReversed = image.GetPixels();

        int width = image.width;
        int height = image.height;
        Parallel.For(0, image.height / 2, (i) =>
        {
            for (int j = 0; j < width; j++)
                pixelsReversed[i * width + j] = pixels[(height - i - 1) * width + j];
            for (int j = 0; j < width; j++)
                pixelsReversed[(height - i - 1) * width + j] = pixels[i * width + j];
        });
        image.SetPixels(pixelsReversed);
    }
}
