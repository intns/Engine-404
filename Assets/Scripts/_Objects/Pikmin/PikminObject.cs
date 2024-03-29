/*
 * PikminObject.cs
 * Created by: Ambrosia
 * Created on: 30/4/2020 (dd/mm/yy)
 * Last update by : Senka
 * Last update on : 10/7/2022
 */

using UnityEngine;

// If you add any pikmin colour here, don't forget to
// update all the pikmin type related functions
public enum PikminColour
{
	Red = 0,
	Yellow,
	Blue,

	Size,
}

public enum PikminMaturity
{
	Leaf = 0,
	Bud,
	Flower,
	Size,
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

	[Header("Fire")]
	public bool _IsAffectedByFire = true;
	public float _FireDeathTimer = 5.0f;

	[Header("Audio")]
	public float _AudioVolume = 1;

	[Space]
	public AudioClip _DeathNoise;
	public AudioClip _ThrowNoise;
	[Space]
	public AudioClip _IdleNoise;
	[Space]
	public AudioClip _HeldNoise;
	[Space]
	public AudioClip _AttackScreechNoise;
	public AudioClip _AttackHitNoise;
	[Space]
	public AudioClip _CarryAddNoise;
	public AudioClip _CarryingNoise;

	public float GetAcceleration(PikminMaturity maturity)
	{
		return _MaxAccelPerMaturity[(int)maturity];
	}

	public float GetMaxSpeed(PikminMaturity maturity)
	{
		return _MaxSpeedPerMaturity[(int)maturity];
	}
}
