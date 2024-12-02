using System.Collections.Concurrent;
using Unity.Burst;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Jobs;

public class TransformJobManager : MonoBehaviour
{
    private static TransformJobManager instance;
    public static TransformJobManager Instance { get => instance != null ? instance : CreateInstance(); }

    private static TransformJobManager CreateInstance ()
    {
        var obj = new GameObject("TransformJobManager");
        DontDestroyOnLoad(obj);
        return instance = obj.AddComponent<TransformJobManager>();
    }

    private ConcurrentQueue<TransformJobData> transformJobDatas = new();

    public static TransformJobData CreateJobData ( Transform transform, Vector3 translation, Quaternion rotation )
    {
        return new TransformJobData() { transform = transform, rotation = rotation, translation = translation };
    }

    public void EnqueueTransformTranslation ( TransformJobData obj )
    {
        transformJobDatas.Enqueue(obj);
    }

    void Update ()
    {
        if ( transformJobDatas.Count < 1 )
            return;

        TransformAccessArray transformAccessArray = new(transformJobDatas.Count);
        NativeArray<Vector3> translations = new(transformJobDatas.Count, Allocator.TempJob);
        NativeArray<Quaternion> rotations = new(transformJobDatas.Count, Allocator.TempJob);

        for ( var i = 0; i < transformJobDatas.Count; ++i )
        {
            var success = transformJobDatas.TryDequeue(out var data);
            if ( !success )
                continue;

            transformAccessArray.Add(data.transform);
            translations[i] = data.translation;
            rotations[i] = data.rotation;
        }

        BatchPerformTranslation job = new()
        {
            translations = translations,
            rotations = rotations
        };

        job.Schedule(transformAccessArray).Complete();

        transformAccessArray.Dispose();
        translations.Dispose();
        rotations.Dispose();
    }
}

[BurstCompile]
public struct BatchPerformTranslation : IJobParallelForTransform
{
    [ReadOnly] public NativeArray<Vector3> translations;
    [ReadOnly] public NativeArray<Quaternion> rotations;

    [BurstCompile]
    public void Execute ( int index, TransformAccess transform )
    {
        if ( translations[index] == Vector3.zero && rotations[index] == transform.rotation )
            return;

        transform.position += translations[index];
        transform.rotation = rotations[index];
    }
}

public struct TransformJobData
{
    public Transform transform;
    public Vector3 translation;
    public Quaternion rotation;
}