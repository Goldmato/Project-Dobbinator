using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovingPlatform : Platform
{
	// Fields and properties
	public static int MoveRange { get { return m_MoveRange * 2; } }

	[SerializeField] [Range(0, 100)] static int m_MoveRange = 50;
	[SerializeField] [Range(0.01f, MAX_SPEED)] float m_MoveSpeedLow = MAX_SPEED / 2;
	[SerializeField] [Range(0.01f, MAX_SPEED)] float m_MoveSpeedHigh = MAX_SPEED;

	Rigidbody m_RigidBody;

	float m_MoveSpeed;

	const float MAX_SPEED = 10f;

	protected override void Start()
	{
		base.Start();
		m_RigidBody = GetComponent<Rigidbody>();
		m_MoveSpeed = (MAX_SPEED - Random.Range(m_MoveSpeedLow, m_MoveSpeedHigh)) * GameManager.Current.DifficultyFactor;

		// Rotate platform around the y-axis by a random amount;
		transform.Rotate(transform.up, Random.Range(0, 360));
		m_RigidBody.velocity = transform.forward * m_MoveSpeed;
	}
}
