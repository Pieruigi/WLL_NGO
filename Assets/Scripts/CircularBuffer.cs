using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WLL_NGO
{
    public class CircularBuffer<T>
    {
        T[] buffer;
        int size;

        public CircularBuffer(int size) 
        {
            this.size = size;
            buffer = new T[size];
        }

        public void Add(T item, int index) { buffer[index % size] = item; }
        public T Get(int index) { return buffer[index % size]; }

    }

}
