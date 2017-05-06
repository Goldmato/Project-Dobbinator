using UnityEngine;

public class PlatformController : MonoBehaviour
{
	// Fields and properties 
	public static PlatformController Instance { get { return m_current; } }

	public float[] CurrentScaleFactor { get; private set; }

	static PlatformController m_current;

	[SerializeField] [Range(1, 10)]     private int   scaleCalcs = 5;
	[SerializeField] [Range(1f, 100f)]  private float scaleSpeed = 50f;
	[SerializeField] [Range(0.01f, 5f)] private float scaleFactor = 2.5f;
	[SerializeField] [Range(0f, 1f)]    private float scaleOffset = 0.5f;
	[SerializeField] [Range(0f, 10f)]   private float scaleVarianceMin = 1f;
	[SerializeField] [Range(0f, 10f)]   private float scaleVarianceMax = 5f;

	float[] m_phaseShift;

	void Awake()
	{
		// Initialize the PlatformController singleton
		m_current = this;
	}

	void OnEnable()
	{
		// Initialize arrays
		CurrentScaleFactor = new float[scaleCalcs];
		m_phaseShift = new float[scaleCalcs];
		for(int i = 0; i < m_phaseShift.Length; i++)
		{
			m_phaseShift[i] = Random.Range(scaleVarianceMin, scaleVarianceMax);
		}
	}

	void FixedUpdate()
	{
		// Simple cosin formula for resizing the platforms
		for(int i = 0; i < CurrentScaleFactor.Length; i++)
		{
			CurrentScaleFactor[i] = Mathf.Abs(Mathf.Cos(Time.timeSinceLevelLoad *
													 Time.deltaTime * scaleSpeed + m_phaseShift[i]) * scaleFactor) + scaleOffset;
		}
	}
}
