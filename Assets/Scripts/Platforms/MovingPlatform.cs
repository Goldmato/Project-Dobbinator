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

	Vector3 m_OriginalPos;
	Vector3 m_ExtentPos;
	Vector3 m_TargetPos;
	Vector3 m_Velocity;

	float m_MoveSpeed;
	bool  m_Direction;
	bool  m_RecalculatePath;

	const float MAX_SPEED = 10f;
	const string ARENA_WALL = "ArenaWall";

	protected override void Start()
	{
		base.Start();
		m_RigidBody = GetComponent<Rigidbody>();
		m_TargetPos = m_OriginalPos = transform.position;
		m_MoveSpeed = (MAX_SPEED - Random.Range(m_MoveSpeedLow, m_MoveSpeedHigh)) * GameManager.Current.DifficultyFactor;

		// Rotate platform around the y-axis by a random amount;
		transform.Rotate(transform.up, Random.Range(0, 360));
		m_ExtentPos = m_OriginalPos + (transform.forward * m_MoveRange);
	}

	void FixedUpdate()
	{
		if(m_RecalculatePath || Vector3.Distance(transform.position, m_TargetPos) < 0.1f)
		{
			m_RecalculatePath = false;
			m_Direction = !m_Direction;
			m_TargetPos = m_Direction ? m_ExtentPos : m_OriginalPos;
			m_RigidBody.velocity = (transform.position - m_TargetPos).normalized * m_MoveSpeed;
		}
	}

	void OnCollisionEnter(Collision other)
	{
		if(other.gameObject.CompareTag(ARENA_WALL))
			m_RecalculatePath = true;
	}
}
