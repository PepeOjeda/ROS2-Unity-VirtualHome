using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using Unity.Robotics.Core;
using RosMessageTypes.Nav;

public class SlideController : MonoBehaviour
{
    public string localizationTopic = "/giraff/groundTruth";
    public string odomTopic = "/giraff/odom";
    public string cmd_velTopic = "/giraff/cmd_vel";
    public float ROSTimeout = 0.5f;
    public string odom_frame = "giraff_odom";
    public string base_frame = "giraff_base_link";

    [SerializeField] Rigidbody robot_base_rb;
    struct CMD_vel
    {
        public float linear;
        public float angular;
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
        ros.RegisterPublisher<PoseWithCovarianceStampedMsg>(localizationTopic);
        ros.RegisterPublisher<OdometryMsg>(odomTopic);
    }

    void Update()
    {
        if(lastMessage.msg == null)
            return;
            
        if (Time.time - lastMessage.timestamp < ROSTimeout)
        {
            //Vector3 position = robot_base.transform.position + robot_base.transform.localToWorldMatrix.MultiplyVector(new Vector3(0, 0, lastMessage.linear * Time.deltaTime));
            //Quaternion rotation = Quaternion.Euler(0, lastMessage.angular * Mathf.Rad2Deg * Time.deltaTime, 0) * robot_base.transform.rotation;
            //robot_base.TeleportRoot(position, rotation);

            robot_base_rb.MovePosition(robot_base_rb.transform.position + robot_base_rb.transform.localToWorldMatrix.MultiplyVector(new Vector3(0, 0, lastMessage.linear * Time.deltaTime)));
            robot_base_rb.MoveRotation(Quaternion.Euler(0, lastMessage.angular * Mathf.Rad2Deg * Time.deltaTime, 0) * robot_base_rb.transform.rotation);
        }

        PoseWithCovarianceStampedMsg poseMsg = new();
        poseMsg.header = new(new TimeStamp(Clock.time), "map");
        poseMsg.pose = new(new PoseMsg(), emptyCovariance);
        poseMsg.pose.pose.position = new PointMsg(transform.position.x, transform.position.y, transform.position.z);
        poseMsg.pose.pose.orientation = new QuaternionMsg(transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w);
        ros.Publish(localizationTopic, poseMsg);
        

        OdometryMsg odometryMsg = new();
        odometryMsg.header = new(new TimeStamp(Clock.time), odom_frame);
        odometryMsg.child_frame_id = base_frame;
        odometryMsg.pose = poseMsg.pose;
        odometryMsg.twist = new(lastMessage.msg, emptyCovariance);
        ros.Publish(odomTopic, odometryMsg);
    }


    void ReceiveROSCmd(TwistMsg cmdVel)
    {
        lastMessage.linear = (float)cmdVel.linear.x;
        lastMessage.angular = -(float)cmdVel.angular.z;
        lastMessage.timestamp = Time.time;
        lastMessage.msg = cmdVel;
    }
}
