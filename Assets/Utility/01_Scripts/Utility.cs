using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using UnityEngine;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.VisualScripting;

namespace Utility
{
    public class ObjectPool<T>
    {
        private readonly object _lock = new object();
        private readonly Stack<T> _pool;
        private readonly Func<T> _factory;

        public ObjectPool(Func<T> factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _pool = new Stack<T>();
        }

        public T Rent()
        {
            lock (_lock)
            {
                return _pool.Count > 0 ? _pool.Pop() : _factory();
            }
        }

        public void Return(T item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            lock (_lock)
            {
                _pool.Push(item);
            }
        }
    }

    public static class Unsafe
    {
        public static unsafe void MemCopy<T>([In] [ReadOnly] NativeList<T> src, [In] [ReadOnly] Span<T> dst)
            where T : unmanaged
        {
            Debug.Assert(src.Length == dst.Length, "Memcpy, Length of src is nto equal to dst");
            fixed (T* dstPtr = dst)
            {
                MemCopyInternal<T>(dstPtr, src.GetUnsafeReadOnlyPtr(), src.Length);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void MemCopyInternal<T>(void* dst, void* src, int count) where T : unmanaged
        {
            var size = UnsafeUtility.SizeOf<T>();
            UnsafeUtility.MemCpy(dst, src, size * count);
        }

        public static unsafe void MemCopy<T>(this NativeList<T> src, [In] [ReadOnly] NativeArray<T> dst)
            where T : unmanaged
        {
            var length = src.Length;
            Debug.Assert(length == dst.Length, "Memcpy, Length of src is nto equal to dst");

            MemCopyInternal<T>(dst.GetUnsafePtr(), src.GetUnsafeReadOnlyPtr(), length);
        }

        public static unsafe void MemCopy<T>([In] [ReadOnly] NativeList<T> src, T[] dst) where T : unmanaged
        {
            var length = src.Length;
            Debug.Assert(length == dst.Length, "Memcpy, Length of src is nto equal to dst");

            fixed (T* dstPtr = dst)
            {
                MemCopyInternal<T>(dstPtr, src.GetUnsafeReadOnlyPtr(), length);
            }
        }
    }

    public static class ListPool<T>
    {
        private static readonly object Lock = new();

        private static readonly Stack<List<T>> Free = new();
        private static readonly HashSet<List<T>> Rented = new();

        public static List<T> Rent()
        {
            lock (Lock)
            {
                if (!Free.TryPop(out var list))
                {
                    list = new List<T>();
                }

                Rented.Add(list);
                return list;
            }
        }

        public static void Return(List<T> list)
        {
            lock (Lock)
            {
                if (!Rented.Contains(list))
                {
                    return;
                }

                Rented.Remove(list);
                Free.Push(list);
            }
        }
    }

    public readonly struct TempBuffer<T> : IDisposable
    {
        public Span<T> Slice => Buffer.AsSpan(0, _desiredLength);
        public T[] Buffer { get; }
        
        private readonly int _desiredLength;

        public static implicit operator T[](TempBuffer<T> buffer) => buffer.Buffer;
   
        public static TempBuffer<T> Create(int size)
        {
            return new TempBuffer<T>(size);
        }

        public void Dispose()
        {
            System.Buffers.ArrayPool<T>.Shared.Return(Buffer);
        }

        private TempBuffer(int desiredLength)
        {
            _desiredLength = desiredLength;
            Buffer = System.Buffers.ArrayPool<T>.Shared.Rent(_desiredLength);
        }
    }

    public readonly unsafe struct UnsafeBuffer<T> : IDisposable
    {
        [NativeDisableUnsafePtrRestriction] private readonly IntPtr _ptr;
        public readonly int Length;

        public static UnsafeBuffer<T> Create(int size)
        {
            var data = Marshal.AllocHGlobal(Marshal.SizeOf<T>() * size);
            return new UnsafeBuffer<T>(data, size);
        }

        private UnsafeBuffer(IntPtr data, int length)
        {
            _ptr = data;
            Length = length;
        }

        public T this[int index]
        {
            get
            {
                if (index > Length || index < 0) throw new IndexOutOfRangeException();
                return UnsafeUtility.ReadArrayElement<T>(_ptr.ToPointer(), index);
            }
            set
            {
                if (index > Length || index < 0) throw new IndexOutOfRangeException();
                UnsafeUtility.WriteArrayElement(_ptr.ToPointer(), index, value);
            }
        }

        public void Dispose()
        {
            if (_ptr == IntPtr.Zero)
                return;

            Marshal.FreeHGlobal(_ptr);
        }
    }

    [BurstCompile]
    public static class Utility
    {
        private static readonly CancellationTokenSource Cts = new();

        //Make sure the tasks are cancelled when quitting the application.
        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            Application.quitting += OnQuit;
        }

        public static IEnumerable<TSource[]> Chunk<TSource>(this IEnumerable<TSource> source, int size)
        {
            if (size < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(source));
            }

            return ChunkIterator(source, size);
        }

        private static IEnumerable<TSource[]> ChunkIterator<TSource>(IEnumerable<TSource> source, int size)
        {
            using var e = source.GetEnumerator();

            if (!e.MoveNext()) yield break;

            var arraySize = Math.Min(size, 4);

            int i;
            do
            {
                var array = new TSource[arraySize];

                // Store the first item.
                array[0] = e.Current;
                i = 1;

                if (size != array.Length)
                {
                    // This is the first chunk. As we fill the array, grow it as needed.
                    for (; i < size && e.MoveNext(); i++)
                    {
                        if (i >= array.Length)
                        {
                            arraySize = (int)Math.Min((uint)size, 2 * (uint)array.Length);
                            Array.Resize(ref array, arraySize);
                        }

                        array[i] = e.Current;
                    }
                }
                else
                {
                    // For all but the first chunk, the array will already be correctly sized.
                    // We can just store into it until either it's full or MoveNext returns false.
                    Debug.Assert(array.Length == size);
                    for (; (uint)i < (uint)array.Length && e.MoveNext(); i++)
                    {
                        array[i] = e.Current;
                    }
                }

                if (i != array.Length)
                {
                    Array.Resize(ref array, i);
                }

                yield return array;
            } while (i >= size && e.MoveNext());
        }

        private static void OnQuit()
        {
            Application.quitting -= OnQuit;
            Cts.Cancel();
            Cts.Dispose();
        }

        [BurstCompile] //Manually Compile the delegate and cache it, for better performance.
        public static bool BoundsIntersect(ref Bounds a, ref Bounds bounds)
        {
            return a.min.x <= bounds.max.x && a.max.x >= bounds.min.x && a.min.y <= bounds.max.y &&
                   a.max.y >= bounds.min.y && a.min.z <= bounds.max.z && a.max.z >= bounds.min.z;
        }

        public static async Task WaitUntil(Func<bool> condition, TimeSpan checkInterval, CancellationToken cancelToken)
        {
            using var linkedcts = CancellationTokenSource.CreateLinkedTokenSource(Cts.Token, cancelToken);
            var newToken = linkedcts.Token;

            while (condition() == false)
            {
                newToken.ThrowIfCancellationRequested();
                await Task.Delay(checkInterval, newToken);
            }
        }

        [DisallowMultipleComponent]
        public sealed class AsyncGameObjectDeactivationTrigger : MonoBehaviour
        {
            private bool _isDeactivated;
            private Action _delegates;

            private void OnDisable()
            {
                _isDeactivated = true;
                _delegates?.Invoke();
            }

            public void Subscribe(Action action)
            {
                if (_isDeactivated)
                    return;

                _delegates += action;
            }
        }

        public static string CleanString(string str)
        {
            try
            {
                return Regex.Replace(str, @"[^\w\.@-]", "", RegexOptions.None, TimeSpan.FromSeconds(1.0));
            }
            catch (RegexMatchTimeoutException)
            {
                return string.Empty;
            }
        }

        public static void ParallelForBatch(Action<int, int> body, int start, int end)
        {
            var threadCount = Environment.ProcessorCount;
            var len = end - start;

            Parallel.For(start, threadCount, (workerId, loopState) =>
            {
                var max = len * (workerId + 1) / threadCount;
                body(len * workerId, max);
            });
        }

        public static Task StreamedTimer(Action<int> action, int start, int end, TimeSpan interval,
            bool threadSafe = false, CancellationToken token = default)
        {
            if (!threadSafe)
            {
                return StreamedTimer(action, start, end, interval, token);
            }

            return Task.Run(async () =>
            {
                using var linkedcts = CancellationTokenSource.CreateLinkedTokenSource(Cts.Token, token);
                var newToken = linkedcts.Token;

                while (start <= end)
                {
                    newToken.ThrowIfCancellationRequested();

                    try
                    {
                        action(start++);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }

                    await Task.Delay(interval, newToken);
                }
            });
        }

        public static async Task StreamedTimer(Action<int> action, int start, int end, TimeSpan interval,
            CancellationToken token = default)
        {
            using var linkedcts = CancellationTokenSource.CreateLinkedTokenSource(Cts.Token, token);
            var newToken = linkedcts.Token;

            while (start <= end)
            {
                newToken.ThrowIfCancellationRequested();

                try
                {
                    action(start++);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                await Task.Delay(interval, newToken);
            }
        }

        public static Task DelayedAction(Action action, TimeSpan delay, bool treadSafe = false,
            CancellationToken token = default)
        {
            if (!treadSafe)
            {
                return DelayedAction(action, delay, token);
            }

            return Task.Run(async () =>
            {
                using var linkedcts = CancellationTokenSource.CreateLinkedTokenSource(Cts.Token, token);
                var newToken = linkedcts.Token;

                try
                {
                    await Task.Delay(delay, newToken);
                    action();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            });
        }

        public static async Task DelayedAction(Action actionToPerform, TimeSpan delay,
            CancellationToken token = default)
        {
            using var linkedcts = CancellationTokenSource.CreateLinkedTokenSource(Cts.Token, token);
            var newToken = linkedcts.Token;

            try
            {
                await Task.Delay(delay, newToken);
                actionToPerform();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
    }

    public static class CachedYielders
    {
        //Cleanup the cached yielders, when closing the application.
        [RuntimeInitializeOnLoadMethod]
        private static void Initalize()
        {
            Application.quitting += OnQuit;
        }

        private static void OnQuit()
        {
            Application.quitting -= OnQuit;
            CachedSeconds.Clear();
        }

        private static WaitForFixedUpdate _fixedUpdate;

        public static WaitForFixedUpdate WaitForFixedUpdate()
        {
            return _fixedUpdate ??= new WaitForFixedUpdate();
        }

        private static readonly Dictionary<float, WaitForSeconds> CachedSeconds = new();

        public static WaitForSeconds GetWaitForSeconds(float seconds)
        {
            if (CachedSeconds.TryGetValue(seconds, out var waitFor))
                return waitFor;

            var newWaitFor = new WaitForSeconds(seconds);
            CachedSeconds.Add(seconds, newWaitFor);
            return newWaitFor;
        }
    }
}