using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Utility.Collections
{
    public static class CollectionExtensions
    {
        public static bool RemoveAtSwapBack<T>(this IList<T> list, int index, out T swappedItem)
        {
            if (list.Count < 0)
            {
                swappedItem = default(T);
                return false;
            }

            var index1 = list.Count - 1;
            if (index1 == index)
            {
                swappedItem = default(T);
                list.RemoveAt(index1);
                return false;
            }

            swappedItem = list[index] = list[index1];
            list.RemoveAt(index1);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap<T>(
            this IList<T> list,
            int sourceIndex,
            int destinationIndex,
            out T newDestinationValue)
        {
            newDestinationValue = list[sourceIndex];
            list[sourceIndex] = list[destinationIndex];
            list[destinationIndex] = newDestinationValue;
        }
    }
}