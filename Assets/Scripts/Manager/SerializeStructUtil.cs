using System.Runtime.InteropServices;
using System;
using UnityEngine;
using System.Collections.Generic;

public static class SerializeStructUtil
{
    public static byte[] StructToBytes<T>(T data) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        byte[] bytes = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(data, ptr, true);
        Marshal.Copy(ptr, bytes, 0, size);
        Marshal.FreeHGlobal(ptr);

        return bytes;
    }

    public static T BytesToStruct<T>(byte[] bytes, int offset = 0) where T : struct
    {
        int size = Marshal.SizeOf<T>();
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.Copy(bytes, offset, ptr, size);
        T data = Marshal.PtrToStructure<T>(ptr);
        Marshal.FreeHGlobal(ptr);

        return data;
    }

    public static byte[] SerializeQueue<T>(Queue<T> queue) where T : struct
    {
        int structSize = Marshal.SizeOf<T>();
        byte[] buffer = new byte[queue.Count * structSize];
        int offset = 0;

        foreach (T item in queue)
        {
            byte[] itemBytes = StructToBytes(item);
            Buffer.BlockCopy(itemBytes, 0, buffer, offset, structSize);
            offset += structSize;
        }

        return buffer;
    }

    public static Queue<T> DeserializeQueue<T>(byte[] data) where T : struct
    {
        int structSize = Marshal.SizeOf<T>();
        int count = data.Length / structSize;
        Queue<T> queue = new Queue<T>(count);

        for (int i = 0; i < count; i++)
        {
            T item = BytesToStruct<T>(data, i * structSize);
            queue.Enqueue(item);
        }

        return queue;
    }
}
