using System;
using System.Collections;
using System.Collections.Generic;

namespace SCMonoGameUtilities.DearImGui.Demos.GuiElements.MiniApps;

/// <summary>
/// A basic circular buffer type used for storing window content by a few of the mini-apps -
/// on the assumption that we don't want window content to just grow forever.
/// </summary>
/// <typeparam name="T">The type of elements to be stored.</typeparam>
/// <param name="maxSize">The maximum number of elements that the buffer will store. The oldest elements will be dropped once this is size is reached.</param>
class RingBuffer<T>(int maxSize) : IEnumerable<T>
{
    // We *could* use something that automatically resizes itself (e.g. a List<>), so that we require
    // less memory while at less than capacity. However, on the assumption that we will be at capacity
    // most of the time, there isn't much point, and the code is simpler if we just use an array.
    public readonly T[] content = new T[maxSize];
    private int headIndex = 0;
    private int count = 0;

    public void Add(T item)
    {
        content[(headIndex + count) % content.Length] = item;

        if (count < content.Length)
        {
            count++;
        }
        else
        {
            headIndex++;
            headIndex %= content.Length;
        }
    }

    public void Clear()
    {
        Array.Clear(content); // NB: actually clear the array to avoid leaks when T is a reference type
        headIndex = 0;
        count = 0;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < count; i++)
        {
            yield return content[(headIndex + i) % content.Length];
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}