using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

[RequireComponent(typeof (CharacterController))]
[RequireComponent(typeof (AudioSource))]
public class FirstPersonController : MonoBehaviour
{
	// Properties
	public PlayerStats OriginalStats { get { return m_OriginalStats; } }
	public PlayerStats Stats { get { return m_PlayerStats; } set { m_PlayerStats = value; } }

	// Private serialized fields
	[SerializeField] private PlayerStats m_PlayerStats;
	[SerializeField] [Range(0.001f, 10f)] private float m_GameOverDelay = 0.5f;
	[SerializeField] private bool m_IsWalking;
    [SerializeField] [Range(0f, 1f)] private float m_RunstepLenghten;
	[SerializeField] [Range(0f, JUMP_MAX_ACCEL)] private float m_FlyAcceleration;
    [SerializeField] private float m_StickToGroundForce;
    [SerializeField] private MouseLook m_MouseLook;
    [SerializeField] private bool m_UseFovKick;
    [SerializeField] private FOVKick m_FovKick = new FOVKick();
    [SerializeField] private bool m_UseHeadBob;
    [SerializeField] private CurveControlledBob m_HeadBob = new CurveControlledBob();
    [SerializeField] private LerpControlledBob m_JumpBob = new LerpControlledBob();
    [SerializeField] private float m_StepInterval;
    [SerializeField] private AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
    [SerializeField] private AudioClip m_JumpSound;           // the sound played when character leaves the ground.
    [SerializeField] private AudioClip m_LandSound;           // the sound played when character touches back on ground.

	// Private class references
    CharacterController m_CharacterController;
    Vector3 m_MoveDir = Vector3.zero;
    Vector3 m_OriginalCameraPosition;
	PlayerStats m_OriginalStats;
    AudioSource m_AudioSource;
    Vector2 m_Input;
    Camera m_Camera;

    CollisionFlags m_CollisionFlags;

	// Private fields
    float m_YRotation;
    float m_StepCycle;
    float m_NextStep;
	float m_ForceCooldown;
	float m_ForceTimer;
	float m_ForceInterpolation;
	float m_ForceCurVelocity;
    bool m_PreviouslyGrounded;
	bool m_GroundedTrigger;
	bool m_GameOverFlag;
	bool m_ForceJumping;
    bool m_Jumping;
    bool m_Jump;

	// Constants
	const float JUMP_MAX_ACCEL = 5f;
	const string GAME_OVER_SCENE = "GameOver";

    // Use this for initialization
    private void Start()
    {
		m_OriginalStats = m_PlayerStats;
        m_CharacterController = GetComponent<CharacterController>();
        m_Camera = Camera.main;
        m_OriginalCameraPosition = m_Camera.transform.localPosition;
        m_FovKick.Setup(m_Camera);
        m_HeadBob.Setup(m_Camera, m_StepInterval);
        m_StepCycle = 0f;
		m_ForceCooldown = 0.5f;
        m_NextStep = m_StepCycle/2f;
        m_Jumping = false;
        m_AudioSource = GetComponent<AudioSource>();
		m_MouseLook.Init(transform , m_Camera.transform);
    }

	// Update is called once per frame
	private void Update()
    {
        RotateView();
        // the jump state needs to read here to make sure it is not missed
		if(!m_ForceJumping)
		{
			if (!m_Jump)
			{
				m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
			}

			if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
			{
				StartCoroutine(m_JumpBob.DoBobCycle());
				PlayLandingSound();
				m_MoveDir.y = 0f;
				m_Jumping = false;
				if(!m_GroundedTrigger)
				{
					m_GroundedTrigger = true;
					GameManager.Current.ResetStreak(callEvent: true);
				}
			}
			if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded)
			{
				m_MoveDir.y = 0f;
			}
		}
		else
		{
			m_GroundedTrigger = false;
		}

		m_PreviouslyGrounded = m_CharacterController.isGrounded;
    }

    private void PlayLandingSound()
    {
        m_AudioSource.clip = m_LandSound;
        m_AudioSource.Play();
        m_NextStep = m_StepCycle + .5f;
    }


    private void FixedUpdate()
    {
        float speed;
        GetInput(out speed);

		// always move along the camera forward as it is the direction that it being aimed at
		Vector3 desiredMove = transform.forward*m_Input.y + transform.right*m_Input.x;
		Vector2 newSpeed = new Vector2(desiredMove.x * speed, desiredMove.z * speed);

		if(m_CharacterController.isGrounded && !m_ForceJumping)
		{
			// get a normal for the surface that is being touched to move along it
			RaycastHit hitInfo;
			Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
								m_CharacterController.height/2f, Physics.AllLayers, QueryTriggerInteraction.Ignore);
			desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

			m_MoveDir.x = newSpeed.x;
			m_MoveDir.z = newSpeed.y;
			if (m_Jump)
			{
				m_MoveDir.y = m_PlayerStats.jumpSpeed;
				PlayJumpSound();
				m_Jump = false;
				m_Jumping = true;
			}
			else
				m_MoveDir.y = -m_StickToGroundForce;
			m_ForceInterpolation = 0f;
			m_ForceCurVelocity = 0f;
			ProgressStepCycle(speed);
		}
		else
		{
			m_ForceInterpolation = Mathf.SmoothDamp(0f, 1f, ref m_ForceCurVelocity,
													JUMP_MAX_ACCEL - m_FlyAcceleration);
			m_MoveDir.x = Mathf.Lerp(m_MoveDir.x, newSpeed.x, m_ForceInterpolation);

			m_MoveDir.z = Mathf.Lerp(m_MoveDir.z, newSpeed.y, m_ForceInterpolation);
				
			m_MoveDir += Physics.gravity * m_PlayerStats.gravityMultiplier * Time.fixedDeltaTime;
		}
		if(m_CharacterController.enabled == true)
			m_CollisionFlags = m_CharacterController.Move(m_MoveDir*Time.fixedDeltaTime);

        UpdateCameraPosition(speed);

        m_MouseLook.UpdateCursorLock();
    }

    private void PlayJumpSound()
    {
        m_AudioSource.clip = m_JumpSound;
        m_AudioSource.Play();
    }


    private void ProgressStepCycle(float speed)
    {
        if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
        {
            m_StepCycle += (m_CharacterController.velocity.magnitude + (speed*(m_IsWalking ? 1f : m_RunstepLenghten)))*
                            Time.fixedDeltaTime;
        }

        if (!(m_StepCycle > m_NextStep))
        {
            return;
        }

        m_NextStep = m_StepCycle + m_StepInterval;

		 PlayFootStepAudio();
    }


    private void PlayFootStepAudio()
    {
        if (!m_CharacterController.isGrounded)
        {
            return;
        }
        // pick & play a random footstep sound from the array,
        // excluding sound at index 0
        int n = Random.Range(1, m_FootstepSounds.Length);
        m_AudioSource.clip = m_FootstepSounds[n];
        m_AudioSource.PlayOneShot(m_AudioSource.clip);
        // move picked sound to index 0 so it's not picked next time
        m_FootstepSounds[n] = m_FootstepSounds[0];
        m_FootstepSounds[0] = m_AudioSource.clip;
    }


    private void UpdateCameraPosition(float speed)
    {
        Vector3 newCameraPosition;
        if (!m_UseHeadBob)
        {
            return;
        }
        if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
        {
            m_Camera.transform.localPosition =
                m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
                                    (speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
            newCameraPosition = m_Camera.transform.localPosition;
            newCameraPosition.y = m_Camera.transform.localPosition.y - m_JumpBob.Offset();
        }
        else
        {
            newCameraPosition = m_Camera.transform.localPosition;
            newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
        }
        m_Camera.transform.localPosition = newCameraPosition;
    }


    private void GetInput(out float speed)
    {
        // Read input
        float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
        float vertical = CrossPlatformInputManager.GetAxis("Vertical");

        bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
        // On standalone builds, walk/run speed is modified by a key press.
        // keep track of whether or not the character is walking or running
        m_IsWalking = !Input.GetKey(KeyCode.LeftShift);
#endif
		// set the desired speed to be walking or running
		if(!m_ForceJumping)
			speed = m_IsWalking ? m_PlayerStats.walkSpeed : m_PlayerStats.runSpeed;
		else
			speed = m_PlayerStats.flySpeed;
        m_Input = new Vector2(horizontal, vertical);

        // normalize input if it exceeds 1 in combined length:
        if (m_Input.sqrMagnitude > 1)
        {
            m_Input.Normalize();
        }

        // handle speed change to give an fov kick
        // only if the player is going to a run, is running and the fovkick is to be used
        if (!m_ForceJumping && m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
        {
            StopAllCoroutines();
            StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
        }
    }


    private void RotateView()
    {
        m_MouseLook.LookRotation (transform, m_Camera.transform);
    }

	private void OnControllerColliderHit(ControllerColliderHit other)
	{
		// Rigidbody body = hit.collider.attachedRigidbody;

		if(!m_GameOverFlag && other.gameObject.CompareTag("DeathZone"))
		{
			m_GameOverFlag = true;
			other.collider.isTrigger = true;
			GameManager.Current.EndGame();
		}

		var platform = other.gameObject.GetComponentInParent<Platform>();
		if(platform != null)
		{
			platform.PlayerBoost(this);
			m_ForceJumping = true;
		}
		else
		{
			m_ForceJumping = false;
		}

		#region Default Collision Code
		// dont move the rigidbody if the character is on top of it
		//if (m_CollisionFlags == CollisionFlags.Below)
		//{
		//	return;
		//}

		//if (body == null || body.isKinematic)
		//{
		//	return;
		//}
		//body.AddForceAtPosition(m_CharacterController.velocity*0.1f, hit.point, ForceMode.Impulse);
		#endregion
	} 

	public bool ApplyForce(Vector3 force)
	{
		if (Time.timeSinceLevelLoad < m_ForceTimer)
			return false;

		m_ForceTimer = Time.timeSinceLevelLoad + m_ForceCooldown;
		m_MoveDir = force * m_PlayerStats.forceMultiplier;
		return true;
	}
}
