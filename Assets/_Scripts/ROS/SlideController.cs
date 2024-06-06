using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;

public class SlideController : MonoBehaviour
{

	public float ROSTimeout = 0.5f;
	//[SerializeField] ArticulationBody robot_base;
	[SerializeField] Rigidbody robot_base_rb;
	struct CMD_vel
	{
		public float linear;
		public float angular;
		public float timestamp;
	}
	private CMD_vel lastMessage;

	ROSConnection ros;

	void Start()
	{
		ros = ROSConnection.GetOrCreateInstance();
		ros.Subscribe<TwistMsg>("cmd_vel", ReceiveROSCmd);
	}

	void Update()
	{
		if (Time.time - lastMessage.timestamp < ROSTimeout)
		{
			//Vector3 position = robot_base.transform.position + robot_base.transform.localToWorldMatrix.MultiplyVector(new Vector3(0, 0, lastMessage.linear * Time.deltaTime));
			//Quaternion rotation = Quaternion.Euler(0, lastMessage.angular * Mathf.Rad2Deg * Time.deltaTime, 0) * robot_base.transform.rotation;
			//robot_base.TeleportRoot(position, rotation);

			robot_base_rb.MovePosition(robot_base_rb.transform.position + robot_base_rb.transform.localToWorldMatrix.MultiplyVector(new Vector3(0, 0, lastMessage.linear * Time.deltaTime)));
			robot_base_rb.MoveRotation(Quaternion.Euler(0, lastMessage.angular * Mathf.Rad2Deg * Time.deltaTime, 0) * robot_base_rb.transform.rotation);
		}
	}


	void ReceiveROSCmd(TwistMsg cmdVel)
	{
		lastMessage.linear = (float)cmdVel.linear.x;
		lastMessage.angular = -(float)cmdVel.angular.z;
		lastMessage.timestamp = Time.time;
	}
}
