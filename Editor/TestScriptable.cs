using System;
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(menuName = "Create TestScriptable", fileName = "TestScriptable", order = 0)]
public class TestScriptable : SerializedScriptableObject
{
    public GameObject GameObject;
    public float Size;
    [SerializeField] private float secondSIze;
}
