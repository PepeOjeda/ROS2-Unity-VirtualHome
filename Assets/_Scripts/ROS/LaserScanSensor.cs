using System;
using System.Collections.Generic;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;
using Unity.Robotics.Core;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using UnityEngine.Serialization;

public class LaserScanSensor : MonoBehaviour
{
    public string topic;
    [FormerlySerializedAs("TimeBetweenScansSeconds")]
    public double PublishPeriodSeconds = 0.1;
    public float RangeMetersMin = 0;
    public float RangeMetersMax = 1000;
    public float ScanAngleStartDegrees = -45;
    public float ScanAngleEndDegrees = 45;
    public float AngularResolutionDegrees = 1f;
    public float FrequencyHz = 10f;
    public string FrameId = "base_scan";
    public float noiseMu = 0;
    public float noiseSigma = 0;

    ROSConnection m_Ros;
    float[] ranges;

    double m_TimeLastScan = -1;

    protected virtual void Start()
    {
        m_Ros = ROSConnection.GetOrCreateInstance();
        m_Ros.RegisterPublisher<LaserScanMsg>(topic);

        //swap if needed
        if (ScanAngleEndDegrees < ScanAngleStartDegrees)
            (ScanAngleStartDegrees, ScanAngleEndDegrees) = (ScanAngleEndDegrees, ScanAngleStartDegrees);

        ranges = new float[Mathf.RoundToInt((ScanAngleEndDegrees - ScanAngleStartDegrees) / AngularResolutionDegrees) + 1];
    }

    public void Update()
    {
        if (Clock.NowTimeInSeconds - m_TimeLastScan < 1f / FrequencyHz)
            return;

        var yawBaseDegrees = transform.rotation.eulerAngles.y;
        for (int i = 0; i < ranges.Length; i++)
        {
            var yawDegrees = yawBaseDegrees + ScanAngleEndDegrees - i * AngularResolutionDegrees;
            var directionVector = Quaternion.Euler(0f, yawDegrees, 0f) * Vector3.forward;
            var measurementStart = transform.position;
            var measurementRay = new Ray(measurementStart, directionVector);
            var foundValidMeasurement = Physics.Raycast(measurementRay, out var hit, RangeMetersMax);
            // Only record measurement if it's within the sensor's operating range
            if (foundValidMeasurement && hit.distance >= RangeMetersMin)
                ranges[i] = hit.distance + (float)MathUtils.RandGaussian(noiseMu, noiseSigma);
            else
                ranges[i] = float.MaxValue;
        }

        var timestamp = new TimeStamp(Clock.time);
        var msg = new LaserScanMsg
        {
            header = new HeaderMsg
            {
                frame_id = FrameId,
                stamp = new TimeMsg
                {
                    sec = timestamp.Seconds,
                    nanosec = timestamp.NanoSeconds,
                }
            },
            range_min = RangeMetersMin,
            range_max = RangeMetersMax,
            angle_min = ScanAngleStartDegrees * Mathf.Deg2Rad,
            angle_max = ScanAngleEndDegrees * Mathf.Deg2Rad,
            angle_increment = AngularResolutionDegrees * Mathf.Deg2Rad,
            time_increment = 0,
            scan_time = (float)PublishPeriodSeconds,
            intensities = new float[ranges.Length],
            ranges = ranges,
        };

        m_Ros.Publish(topic, msg);

        m_TimeLastScan = Clock.NowTimeInSeconds;
    }
}
