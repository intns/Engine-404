using System.Collections.Generic;
using UnityEngine;

public class Bridge : MonoBehaviour, IPikminAttack, IHealth
{
	[Header("Components")]
	[SerializeField] GameObject _EndPiece;
	[SerializeField] GameObject _MidPiece;
	[SerializeField] GameObject _AttackPiece;
	[SerializeField] Transform _EndPosition;
	[Space]
	[SerializeField] AudioClip _StepSound;

	[Header("Settings")]
	[SerializeField] float _EndToMidYOffset = -0.16f;
	[SerializeField] float _EndToMidOffset = 0.9f;
	[SerializeField] float _MidToMidOffset = 2.25f;

	[Space]
	[SerializeField] float _HealthUntilStep = 10;

	[Header("Debugging")]
	[SerializeField] int _PieceCount;

	List<PikminAI> _AttackingPikmin = new List<PikminAI>();
	Vector3 _Piece1Position = Vector3.zero;
	float _CurrentHealth = 0;
	int _StepIndex = 0;

	AudioSource _Source = null;

	void Awake()
	{
		_Source = GetComponent<AudioSource>();

		_CurrentHealth = _HealthUntilStep;

		Vector3 rotation = transform.eulerAngles;
		rotation.y += 180;
		Instantiate(_EndPiece, transform.position, Quaternion.Euler(rotation));
	}

	public PikminIntention IntentionType => PikminIntention.Attack;
	bool IPikminAttack.IsAttackAvailable() => true;

	void OnDrawGizmos()
	{
		Mesh endPieceMesh = _EndPiece.GetComponentInChildren<MeshFilter>().sharedMesh;
		Mesh midPieceMesh = _MidPiece.GetComponentInChildren<MeshFilter>().sharedMesh;

		float distance = Vector3.Distance(transform.position, _EndPosition.position);
		_PieceCount = Mathf.CeilToInt(distance / _MidToMidOffset) - 1;

		Vector3 rotation = transform.eulerAngles;
		rotation.y += 180;
		Gizmos.DrawMesh(endPieceMesh, transform.position, Quaternion.Euler(rotation));

		// First piece off of the start piece
		Vector3 piece1Pos = transform.position;
		piece1Pos += transform.forward * _EndToMidOffset;
		piece1Pos -= Vector3.down * _EndToMidYOffset;
		Gizmos.DrawMesh(midPieceMesh, piece1Pos, transform.rotation);

		for (int i = 0; i < _PieceCount; i++)
		{
			Vector3 piecePos = piece1Pos + (_MidToMidOffset * i * transform.forward);
			Gizmos.DrawMesh(midPieceMesh, piecePos, transform.rotation);
		}

		Gizmos.DrawMesh(endPieceMesh, _EndPosition.position, transform.rotation);
	}


	#region Pikmin Attacking Implementation

	public void OnAttackRecieve(float damage, Transform hitPart = default)
	{
		SubtractHealth(damage);

		if (_CurrentHealth <= 0)
		{
			_CurrentHealth = _HealthUntilStep;

			if (_StepIndex >= _PieceCount)
			{
				_AttackPiece.SetActive(false);
				Instantiate(_EndPiece, _EndPosition.position, transform.rotation);

				// TODO: Play victory noise

				while (_AttackingPikmin.Count != 0)
				{
					_AttackingPikmin[0].ChangeState(PikminStates.Idle);
				}

				enabled = false;
				return;
			}

			if (_StepIndex == 0)
			{
				_Piece1Position = transform.position;
				_Piece1Position += transform.forward * _EndToMidOffset;
				_Piece1Position -= Vector3.down * _EndToMidYOffset;
				Instantiate(_MidPiece, _Piece1Position, transform.rotation);
				_AttackPiece.transform.position = _Piece1Position + transform.forward;
			}
			else
			{
				Vector3 piecePos = _Piece1Position + (_MidToMidOffset * _StepIndex * transform.forward);
				Instantiate(_MidPiece, piecePos, transform.rotation);
				_AttackPiece.transform.position = piecePos + transform.forward;
			}

			_Source.Stop();
			_Source.PlayOneShot(_StepSound);

			_StepIndex++;
		}
	}

	public void OnAttackStart(PikminAI attachedPikmin)
	{
		_AttackingPikmin.Add(attachedPikmin);
	}

	public void OnAttackEnd(PikminAI detachedPikmin)
	{
		_AttackingPikmin.Remove(detachedPikmin);
	}
	#endregion

	#region Health Implementation

	// 'Getter' functions
	public float GetCurrentHealth()
	{
		return _CurrentHealth;
	}

	public float GetMaxHealth()
	{
		return _HealthUntilStep;
	}

	// 'Setter' functions
	public float AddHealth(float give)
	{
		return _CurrentHealth += give;
	}

	public float SubtractHealth(float take)
	{
		return _CurrentHealth -= take;
	}

	public void SetHealth(float set)
	{
		_CurrentHealth = set;
	}

	#endregion
}
