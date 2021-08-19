using UnityEngine;

public class GridMesh : MonoBehaviour
{
	[HideInInspector] public Renderer _Renderer = null;
	private MeshCollider _Collider = null;

	private void Awake()
	{
		_Renderer = GetComponent<Renderer>();
		_Collider = gameObject.AddComponent<MeshCollider>();
		_Collider.sharedMesh = GetComponent<MeshFilter>().mesh;
		gameObject.layer = LayerMask.NameToLayer("Map");
	}

	private void OnMouseOver()
	{
		if (GridManager._Instance._Stage == GM_Stage.Texturing)
		{
			_Renderer.material.SetColor("_BaseColor", Color.yellow);
			if (Input.GetMouseButtonDown(0))
			{
				_Renderer.material.SetTexture("_BaseColorMap", GridManager._Instance.Texturing_GetTex());
				_Renderer.material.SetFloat("_Smoothness", 0);
			}
		}
	}

	private void OnMouseExit()
	{
		if (GridManager._Instance._Stage == GM_Stage.Texturing)
		{
			_Renderer.material.SetColor("_BaseColor", Color.white);
		}
	}
}
