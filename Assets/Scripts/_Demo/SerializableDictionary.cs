using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace _Demo
{
	[Serializable]
	public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
	{
		[FormerlySerializedAs("_keys")]
		[SerializeField]
		List<TKey> _Keys = new();

		[FormerlySerializedAs("_values")]
		[SerializeField]
		List<TValue> _Values = new();

		public void OnBeforeSerialize()
		{
			_Keys.Clear();
			_Values.Clear();

			foreach (KeyValuePair<TKey, TValue> kvp in this)
			{
				_Keys.Add(kvp.Key);
				_Values.Add(kvp.Value);
			}
		}

		public void OnAfterDeserialize()
		{
			Clear();

			if (_Keys.Count != _Values.Count)
			{
				throw new("The number of keys and values does not match.");
			}

			for (int i = 0; i < _Keys.Count; i++)
			{
				Add(_Keys[i], _Values[i]);
			}
		}
	}
}
