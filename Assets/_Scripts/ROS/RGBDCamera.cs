using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using Unity.Robotics.Core;

using ImageMsg = RosMessageTypes.Sensor.ImageMsg;
using System.Linq;
using System;
using System.Threading.Tasks;

[RequireComponent(typeof(Camera))]
public class RGBDCamera : MonoBehaviour
{
    public string colorTopic = "/rgbd/color/raw";
    public string depthTopic = "/rgbd/depth/raw";
    public RenderTexture customRenderTexture;
    public Material depthMat;
    public float frequency = 30;


    CustomTimers.Countdown countdown;

    private ROSConnection ros;
    private Texture2D image;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<ImageMsg>(colorTopic, 1);
        ros.RegisterPublisher<ImageMsg>(depthTopic, 1);
        countdown = new(1 / frequency);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (!countdown.done)
            return;

        publishColor(src);
        publishDepth(src);
        countdown.Restart();
    }

    void publishColor(RenderTexture src)
    {
        //vertical flip on gpu
        Graphics.Blit(src, customRenderTexture, new Vector2(1, -1), new Vector2(0, 1));
        ImageMsg msg = ReadImage();
        ros.Publish(colorTopic, msg);
    }

    void publishDepth(RenderTexture src)
    {
        //read depth buffer (material also does the vertical flip)
        Graphics.Blit(src, customRenderTexture, depthMat, 0); // the pass index is required! otherwise nothing happens

        ImageMsg msg = ReadImage();
        ros.Publish(depthTopic, msg);
    }

    ImageMsg ReadImage()
    {
        //read render texture (gpu) to Texture2D (cpu)  
        RenderTexture.active = customRenderTexture;
        if (!image)
            image = new Texture2D(customRenderTexture.width, customRenderTexture.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, customRenderTexture.width, customRenderTexture.height), 0, 0);
        image.Apply();
        RenderTexture.active = null;

        //compressed
        //ImageMsg msg = new(
        //	new RosMessageTypes.Std.HeaderMsg(new TimeStamp(Clock.time), gameObject.name),
        //	"png",
        //	image.EncodeToPNG());

        //raw
        ImageMsg msg = new(
            new RosMessageTypes.Std.HeaderMsg(new TimeStamp(Clock.time), gameObject.name),
            (uint)image.height, (uint)image.width, "rgb8", (byte)1, (uint)image.width * 3,
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
