/// <summary>
///	Public container for player stats
/// </summary>
[System.Serializable]
public struct PlayerStats
{
	// Fields and properties
	public float forceMultiplier;
	public float gravityMultiplier;
	public float walkSpeed;
	public float runSpeed;
	public float jumpSpeed;
	public float flySpeed;

	/// <summary>
	/// Shorthand for default values (1, 1, 1, 1, 1, 1)
	/// </summary>
	public static PlayerStats Default = new PlayerStats(1, 1, 1, 1, 1, 1);

	// Main constructor 
	public PlayerStats(float _ForceMult, float _GravMult, float _WalkSpeed, float _RunSpeed, 
					   float _JumpSpeed, float _FlySpeed)
	{
		forceMultiplier = _ForceMult;
		gravityMultiplier = _GravMult;
		walkSpeed = _WalkSpeed;
		runSpeed = _RunSpeed;
		jumpSpeed = _JumpSpeed;
		flySpeed  = _FlySpeed;
	}

	// Operator overload for addition
	public static PlayerStats operator +(PlayerStats p1, PlayerStats p2)
	{
		return new PlayerStats(p1.forceMultiplier + p2.forceMultiplier, p1.gravityMultiplier + p2.gravityMultiplier,
							   p1.walkSpeed + p2.walkSpeed, p1.runSpeed + p2.runSpeed, p1.jumpSpeed + p2.jumpSpeed,
							   p1.flySpeed + p2.flySpeed);
	}
}
