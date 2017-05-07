// This file is just for simple public interfaces
using UnityEngine;

public interface IInitializable
{
	void Initialize();
}

public interface IPlatform
{
	void PlayerBoost(FirstPersonController character);

	void SelfDestruct();
}
