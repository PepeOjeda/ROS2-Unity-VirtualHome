using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using Unity.Robotics.Core;
using RosMessageTypes.Nav;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class SlideControllerNoisy : SlideController
{
    [System.Serializable]
    public struct Noise
    {
        public float sigmaForward, sigmaSide, sigmaAngle;
    }
    [SerializeField] Noise movementNoise;
    [SerializeField] float acceleration = 1;
    [SerializeField] float angleAcceleration = 1;

    Vector3 odomPosition;
    Quaternion odomRotation;

    Vector3 currentSpeed = new();
    float currentAngularSpeed = 0;


    protected override TwistMsg CalculateAndApplyMovement()
    {
        // accelerate slowly rather than adjust the speed instantly
        if (lastMessage.msg != null && Time.time - lastMessage.timestamp < ROSTimeout)
        {
            currentSpeed = Vector3.MoveTowards(currentSpeed, lastMessage.msg.linear.From<FLU>(), acceleration * Time.deltaTime);
            currentAngularSpeed = Mathf.MoveTowards(currentAngularSpeed, -(float)lastMessage.msg.angular.z, angleAcceleration * Time.deltaTime);
        }
        else
            return new();

        robot_base_rb.MovePosition(robot_base_rb.transform.position + robot_base_rb.transform.localToWorldMatrix.MultiplyVector(currentSpeed * Time.deltaTime));
        robot_base_rb.MoveRotation(Quaternion.Euler(0, currentAngularSpeed * Mathf.Rad2Deg * Time.deltaTime, 0) * robot_base_rb.transform.rotation);

        return lastMessage.msg; // pretend we are executing the movement perfectly
    }

    protected override void PublishOdometry(Vector3 previousPosition, Quaternion previousRotation, TwistMsg twist)
    {
        float angularSpeed = twist.angular.From<FLU>().y;
        float deltaAngle = angularSpeed * Time.deltaTime;

        Vector3 linearMovementVelocity = twist.linear.From<FLU>();
        odomPosition += linearMovementVelocity * Time.deltaTime;
        odomRotation = odomRotation * Quaternion.AngleAxis(deltaAngle, Vector3.up);

        OdometryMsg odometryMsg = new();
        odometryMsg.header = new(new TimeStamp(Clock.time), odom_frame);
        odometryMsg.pose = new(new PoseMsg(), movementCovariance);
        odometryMsg.pose.pose.position = odomPosition.To<FLU>();
        odometryMsg.pose.pose.orientation = odomRotation.To<FLU>();

        odometryMsg.child_frame_id = base_frame;
        odometryMsg.twist.covariance = movementCovariance;
        odometryMsg.twist.twist = twist;

        ros.Publish(odomTopic, odometryMsg);
    }

    protected override void ResetPose(PoseStampedMsg msg)
    {
        base.ResetPose(msg);
        odomPosition = transform.position;
        odomRotation = transform.rotation;
    }

    protected override void Setup()
    {
        base.Setup();
        movementCovariance[0] = movementNoise.sigmaForward * movementNoise.sigmaForward;
        movementCovariance[6 * 1 + 1] = movementNoise.sigmaSide * movementNoise.sigmaSide;
        movementCovariance[6 * 5 + 5] = movementNoise.sigmaAngle * movementNoise.sigmaAngle;
    }

}
