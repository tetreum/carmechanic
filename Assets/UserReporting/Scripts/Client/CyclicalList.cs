using System;
using System.Collections;
using System.Collections.Generic;

namespace Unity.Cloud
{
    /// <summary>
    ///     Represents a cyclical list.
    /// </summary>
    /// <typeparam name="T">The type of the items in the list.</typeparam>
    public class CyclicalList<T> : IList<T>
    {
        #region Constructors

        /// <summary>
        ///     Creates a new instance of the <see cref="CyclicalList{T}" /> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        public CyclicalList(int capacity)
        {
            items = new T[capacity];
        }

        #endregion

        #region Nested Types

        /// <summary>
        ///     Represents an enumerator for a cyclical list.
        /// </summary>
        private struct Enumerator : IEnumerator<T>
        {
            #region Constructors

            /// <summary>
            ///     Creates a new instance of the <see cref="Enumerator" /> class.
            /// </summary>
            /// <param name="list">The list.</param>
            public Enumerator(CyclicalList<T> list)
            {
                this.list = list;
                currentIndex = -1;
            }

            #endregion

            #region Fields

            private int currentIndex;

            private readonly CyclicalList<T> list;

            #endregion

            #region Properties

            /// <summary>
            ///     Gets the current item.
            /// </summary>
            public T Current
            {
                get
                {
                    if (currentIndex < 0 || currentIndex >= list.Count) return default;
                    return list[currentIndex];
                }
            }

            /// <summary>
            ///     Gets the current item.
            /// </summary>
            object IEnumerator.Current => Current;

            #endregion

            #region Methods

            /// <summary>
            ///     Disposes of the enumerator.
            /// </summary>
            public void Dispose()
            {
                // Empty
            }

            /// <summary>
            ///     Moves to the next item.
            /// </summary>
            /// <returns>A value indicating whether the move was successful.</returns>
            public bool MoveNext()
            {
                currentIndex++;
                return currentIndex < list.Count;
            }

            /// <summary>
            ///     Resets the enumerator.
            /// </summary>
            public void Reset()
            {
                currentIndex = 0;
            }

            #endregion
        }

        #endregion

        #region Fields

        private readonly T[] items;

        private int nextPointer;

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the capacity.
        /// </summary>
        public int Capacity => items.Length;

        /// <summary>
        ///     Gets the count.
        /// </summary>
        public int Count { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether the cyclical list is read only.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        ///     Indexes into the cyclical list.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The item at the specified index.</returns>
        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count) throw new IndexOutOfRangeException();
                return items[GetPointer(index)];
            }
            set
            {
                if (index < 0 || index >= Count) throw new IndexOutOfRangeException();
                items[GetPointer(index)] = value;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Adds an item to the cyclical list.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Add(T item)
        {
            items[nextPointer] = item;
            Count++;
            if (Count > items.Length) Count = items.Length;
            nextPointer++;
            if (nextPointer >= items.Length) nextPointer = 0;
        }

        /// <summary>
        ///     Clears the cyclical list.
        /// </summary>
        public void Clear()
        {
            Count = 0;
            nextPointer = 0;
        }

        /// <summary>
        ///     Determines whether the cyclical list contains the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>A value indicating whether the cyclical list contains the specified item.</returns>
        public bool Contains(T item)
        {
            foreach (var listItem in this)
                if (listItem.Equals(item))
                    return true;
            return false;
        }

        /// <summary>
        ///     Copies the cyclical list to an array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">The array index.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            var i = 0;
            foreach (var listItem in this)
            {
                var currentArrayIndex = arrayIndex + i;
                if (currentArrayIndex >= array.Length) break;
                array[currentArrayIndex] = listItem;
                i++;
            }
        }

        /// <summary>
        ///     Gets the enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        /// <summary>
        ///     Gets the enumerator.
        /// </summary>
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Gets the next eviction.
        /// </summary>
        /// <returns>The next eviction.</returns>
        public T GetNextEviction()
        {
            return items[nextPointer];
        }

        /// <summary>
        ///     Gets a pointer.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The pointer.</returns>
        private int GetPointer(int index)
        {
            if (index < 0 || index >= Count) throw new IndexOutOfRangeException();
            if (Count < items.Length) return index;
            return (nextPointer + index) % Count;
        }

        /// <summary>
        ///     Gets the index of the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>The index of the specified item.</returns>
        public int IndexOf(T item)
        {
            var i = 0;
            foreach (var listItem in this)
            {
                if (listItem.Equals(item)) return i;
                i++;
            }

            return -1;
        }

        /// <summary>
        ///     Inserts an item into the cyclical list. This is a no-op on a cyclical list.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        public void Insert(int index, T item)
        {
            if (index < 0 || index >= Count) throw new IndexOutOfRangeException();
        }

        /// <summary>
        ///     Removes an item from the cyclical list.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>A value indicating whether the removal was successful. This is a no-op on a cyclical list.</returns>
        public bool Remove(T item)
        {
            return false;
        }

        /// <summary>
        ///     Removes an item from the cyclical list at the specified index. This is a no-op on a cyclical list.
        /// </summary>
        /// <param name="index">The index.</param>
        public void RemoveAt(int index)
        {
            if (index < 0 || index >= Count) throw new IndexOutOfRangeException();
        }

        #endregion
    }
}