using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using Unity.Robotics.Core;
using RosMessageTypes.Nav;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class SlideController : MonoBehaviour
{
    public string localizationTopic = "/giraff/groundTruth";
    public string odomTopic = "/giraff/odom";
    public string cmd_velTopic = "/giraff/cmd_vel";
    public string resetPoseTopic = "/giraff/resetPose";
    public float ROSTimeout = 0.5f;
    public string odom_frame = "giraff_odom";
    public string base_frame = "giraff_base_link";

    [SerializeField] Rigidbody robot_base_rb;
    struct CMD_vel
    {
        public float timestamp;
        public TwistMsg msg;
    }
    private CMD_vel lastMessage;

    ROSConnection ros;
    double[] emptyCovariance = {
        0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0,
        0, 0, 0, 0, 0, 0
    };

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<TwistMsg>(cmd_velTopic, ReceiveROSCmd);
        ros.Subscribe<PoseStampedMsg>(resetPoseTopic, ResetPose);
        ros.RegisterPublisher<PoseWithCovarianceStampedMsg>(localizationTopic);
        ros.RegisterPublisher<OdometryMsg>(odomTopic);
    }

    void Update()
    {
        TwistMsg currentTwist = new TwistMsg(new Vector3Msg(0, 0, 0), new Vector3Msg(0, 0, 0));

        if (lastMessage.msg != null && Time.time - lastMessage.timestamp < ROSTimeout)
            currentTwist = lastMessage.msg;

        //apply Twist
        {
            Vector3 linear = currentTwist.linear.From<FLU>();
            Vector3 angular = -currentTwist.angular.From<FLU>();
            robot_base_rb.MovePosition(robot_base_rb.transform.position + robot_base_rb.transform.localToWorldMatrix.MultiplyVector(linear * Time.deltaTime));
            robot_base_rb.MoveRotation(Quaternion.Euler(0, angular.y * Mathf.Rad2Deg * Time.deltaTime, 0) * robot_base_rb.transform.rotation);
        }

        //Localization msg
        {
            PoseWithCovarianceStampedMsg poseMsg = new();
            poseMsg.header = new(new TimeStamp(Clock.time), "map");

            TFFrame mapFrame = TFSystem.instance.GetTransform(poseMsg.header);
            Vector3 mapPosition = mapFrame.InverseTransformPoint(robot_base_rb.transform.position);
            Quaternion mapRotation = Quaternion.Inverse(mapFrame.rotation) * robot_base_rb.transform.rotation;

            poseMsg.pose = new(new PoseMsg(), emptyCovariance);
            poseMsg.pose.pose.position = mapPosition.To<FLU>();
            poseMsg.pose.pose.orientation = mapRotation.To<FLU>();
            ros.Publish(localizationTopic, poseMsg);
        }

        // Odom msg
        {
            OdometryMsg odometryMsg = new();
            odometryMsg.header = new(new TimeStamp(Clock.time), odom_frame);

            TFFrame odomFrame = TFSystem.instance.GetTransform(odometryMsg.header);
            Vector3 odomPosition = odomFrame.InverseTransformPoint(robot_base_rb.transform.position);
            Quaternion odomRotation = Quaternion.Inverse(odomFrame.rotation) * robot_base_rb.transform.rotation;

            odometryMsg.pose = new(new PoseMsg(), emptyCovariance);
            odometryMsg.pose.pose.position = odomPosition.To<FLU>();
            odometryMsg.pose.pose.orientation = odomRotation.To<FLU>();

            odometryMsg.child_frame_id = base_frame;
            odometryMsg.twist = new(currentTwist, emptyCovariance);
            ros.Publish(odomTopic, odometryMsg);
        }
    }


    void ReceiveROSCmd(TwistMsg cmdVel)
    {
        lastMessage.timestamp = Time.time;
        lastMessage.msg = cmdVel;
    }


    void ResetPose(PoseStampedMsg msg)
    {
        Debug.Log($"Resetting to position: {msg.pose.position}");
        TFFrame msgFrame = TFSystem.instance.GetTransform(msg.header);

        Vector3 position = msgFrame.TransformPoint(msg.pose.position.From(CoordinateSpaceSelection.FLU));
        Quaternion rotation = msgFrame.rotation * msg.pose.orientation.From(CoordinateSpaceSelection.FLU);
        robot_base_rb.Move(position, rotation);
        robot_base_rb.transform.SetPositionAndRotation(position, rotation);
    }
}
