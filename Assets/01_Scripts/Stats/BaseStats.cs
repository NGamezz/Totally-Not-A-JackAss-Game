using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/BaseStats", order = 1)]
public sealed class BaseStats : ScriptableObject
{
    public float moveSpeed;
    public float health;
}