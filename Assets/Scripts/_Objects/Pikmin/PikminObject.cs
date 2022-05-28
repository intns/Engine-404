/*
 * PikminObject.cs
 * Created by: Ambrosia
 * Created on: 30/4/2020 (dd/mm/yy)
 */

using UnityEngine;

public enum PikminColour
{
	Red = 0,
	Yellow,
	Blue,
	Size // Also used as a "NONE" variable
}

public enum PikminMaturity
{
	Leaf = 0,
	Bud,
	Flower,
	Size
}

[CreateAssetMenu(fileName = "NewPikminType", menuName = "New Pikmin Type")]
public class PikminObject : ScriptableObject
{
	[Header("Pikmin Specific")]
	public PikminColour _PikminColour;
	public Color _DeathSpiritPikminColour = Color.white;

	[Header("Maturity Head Types")]
	public GameObject _Leaf;
	public GameObject _Bud;
	public GameObject _Flower;

	[Header("Movement")]
	public float[] _MaxSpeedPerMaturity = new float[3];
	public float[] _MaxAccelPerMaturity = new float[3];
	public float _RotationSpeed = 10;
	public float _ThrowingHeight = 5;

	[Header("Idle")]
	public float _SearchRadius = 5;

	[Header("Attacking")]
	public float _AttackDamage = 2.5f;

	[Header("Audio")]
	public float _AudioVolume = 1;

	[Space]
	public AudioClip _DeathNoise = null;
	public AudioClip _ThrowNoise = null;
	[Space]
	public AudioClip _IdleNoise = null;
	[Space]
	public AudioClip _HeldNoise = null;
	[Space]
	public AudioClip _AttackScreechNoise = null;
	public AudioClip _AttackHitNoise = null;
	[Space]
	public AudioClip _CarryAddNoise = null;
	public AudioClip _CarryingNoise = null;

	public float GetMaxSpeed(PikminMaturity maturity)
	{
		return _MaxSpeedPerMaturity[(int)maturity];
	}

	public float GetAcceleration(PikminMaturity maturity)
	{
		return _MaxAccelPerMaturity[(int)maturity];
	}
}
