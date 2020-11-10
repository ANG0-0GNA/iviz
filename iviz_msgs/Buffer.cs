﻿using System;
using System.Runtime.CompilerServices;

namespace Iviz.Msgs
{
    /// <summary>
    /// Contains utilities to (de)serialize ROS messages from a byte array. 
    /// </summary>
    public unsafe struct Buffer
    {
        /// <summary>
        /// Current position.
        /// </summary>
        byte* ptr;

        /// <summary>
        /// Maximal position.
        /// </summary>
        readonly byte* end;

        Buffer(byte* ptr, byte* end)
        {
            this.ptr = ptr;
            this.end = end;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void Memcpy(void* dst, void* src, uint size)
        {
            System.Buffer.MemoryCopy(src, dst, size, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly void AssertInRange(uint off)
        {
            if (ptr + off <= end)
            {
                return;
            }

            if (ptr == default && end == default)
            {
                throw new InvalidOperationException("Buffer has not been initialized!");
            }

            throw new InvalidOperationException(
                $"Buffer: Requested {off} bytes, but only {(end - ptr)} remain!");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void AssertSize<T>(T[] array, uint size)
        {
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (array.Length != size)
            {
                throw new ArgumentException($"Invalid array size. Expected {size}, but got {array.Length}.",
                    nameof(array));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string DeserializeString()
        {
            AssertInRange(4);
            uint count = *(uint*) ptr;
            ptr += 4;
            if (count == 0)
            {
                return string.Empty;
            }

            AssertInRange(count);
            string val = BuiltIns.UTF8.GetString(ptr, (int) count);
            ptr += count;
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string[] DeserializeStringArray()
        {
            AssertInRange(4);
            uint count = *(uint*) ptr;
            ptr += 4;
            return count == 0 ? Array.Empty<string>() : DeserializeStringArray(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string[] DeserializeStringArray(uint count)
        {
            string[] val = new string[count];
            for (int i = 0; i < val.Length; i++)
            {
                val[i] = DeserializeString();
            }

            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Deserialize<T>() where T : unmanaged
        {
            AssertInRange((uint) sizeof(T));
            T val = *(T*) ptr;
            ptr += sizeof(T);
            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deserialize<T>(out T t) where T : unmanaged
        {
            AssertInRange((uint) sizeof(T));
            t = *(T*) ptr;
            ptr += sizeof(T);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] DeserializeStructArray<T>() where T : unmanaged
        {
            AssertInRange(4);
            uint count = *(uint*) ptr;
            ptr += 4;
            return count == 0 ? Array.Empty<T>() : DeserializeStructArray<T>(count);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] DeserializeStructArray<T>(uint count) where T : unmanaged
        {
            AssertInRange(count * (uint) sizeof(T));
            T[] val = new T[count];
            fixed (T* bPtr = val)
            {
                uint size = count * (uint) sizeof(T);
                Memcpy(bPtr, ptr, size);
                ptr += size;
            }

            return val;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] DeserializeArray<T>() where T : IMessage, new()
        {
            AssertInRange(4);
            uint count = *(uint*) ptr;
            ptr += 4;
            return count == 0 ? Array.Empty<T>() : new T[count];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize<T>(in T val) where T : unmanaged
        {
            AssertInRange((uint) sizeof(T));
            *(T*) ptr = val;
            ptr += sizeof(T);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize(string val)
        {
            uint count = (uint) BuiltIns.UTF8.GetByteCount(val);
            AssertInRange(4 + count);
            *(uint*) ptr = count;
            ptr += 4;
            if (count == 0) { return; }

            fixed (char* bPtr = val)
            {
                BuiltIns.UTF8.GetBytes(bPtr, val.Length, ptr, (int) count);
                ptr += count;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SerializeArray(string[] val, uint count)
        {
            if (count == 0)
            {
                AssertInRange(4);
                *(int*) ptr = val.Length;
                ptr += 4;
            }
            else
            {
                AssertSize(val, count);
            }

            foreach (string str in val)
            {
                Serialize(str);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SerializeStructArray<T>(T[] val, uint count) where T : unmanaged
        {
            if (count == 0)
            {
                AssertInRange((uint) (4 + val.Length * sizeof(T)));
                *(int*) ptr = val.Length;
                ptr += 4;
            }
            else
            {
                AssertSize(val, count);
                AssertInRange(count * (uint) sizeof(T));
            }

            fixed (T* bPtr = val)
            {
                uint size = (uint) (val.Length * sizeof(T));
                Memcpy(ptr, bPtr, size);
                ptr += size;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SerializeArray<T>(T[] val, uint count) where T : IMessage
        {
            if (count == 0)
            {
                AssertInRange(4);
                *(int*) ptr = val.Length;
                ptr += 4;
            }
            else
            {
                AssertSize(val, count);
            }

            foreach (T t in val)
            {
                t.RosSerialize(ref this);
            }
        }

        /// <summary>
        /// Deserializes a message of the given type from the buffer array.  
        /// </summary>
        /// <param name="generator">
        /// An arbitrary instance of the type T. Can be anything, such as new T().
        /// This is a (rather unclean) workaround to the fact that C# cannot invoke static functions from generics.
        /// So instead of using T.Deserialize(), we need an instance to do this.
        /// </param>
        /// <param name="buffer">
        /// The source byte array. 
        /// </param>
        /// <param name="size">
        /// Optional. The expected size of the message inside of the array. Must be less or equal the array size.
        /// </param>
        /// <typeparam name="T">Message type.</typeparam>
        /// <returns>The deserialized message.</returns>
        public static T Deserialize<T>(T generator, byte[] buffer, int size = -1) where T : ISerializable
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (generator == null)
            {
                throw new ArgumentNullException(nameof(generator));
            }

            fixed (byte* bPtr = buffer)
            {
                int span = (size == -1) ? buffer.Length : size;
                Buffer b = new Buffer(bPtr, bPtr + span);
                return (T) generator.RosDeserialize(ref b);
            }
        }

        /// <summary>
        /// Deserializes a message of the given type from the buffer array.  
        /// </summary>
        /// <param name="generator">
        /// An arbitrary instance of the type T. Can be anything, such as new T().
        /// This is a (rather unclean) workaround to the fact that C# cannot invoke static functions from generics.
        /// So instead of using T.Deserialize(), we need an instance to do this.
        /// </param>
        /// <param name="buffer">
        /// The source byte array. 
        /// </param>
        /// <param name="size">
        /// Optional. The expected size of the message inside of the array. Must be less or equal the array size.
        /// </param>
        /// <typeparam name="T">Message type.</typeparam>
        /// <returns>The deserialized message.</returns>
        public static T Deserialize<T>(IDeserializable<T> generator, byte[] buffer, int size = -1)
            where T : ISerializable
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (buffer.Length < size || size < -1)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            if (generator == null)
            {
                throw new ArgumentNullException(nameof(generator));
            }

            fixed (byte* bPtr = buffer)
            {
                int span = (size == -1) ? buffer.Length : size;
                Buffer b = new Buffer(bPtr, bPtr + span);
                return generator.RosDeserialize(ref b);
            }
        }

        /// <summary>
        /// Serializes the given message into the buffer array.
        /// </summary>
        /// <param name="message">The ROS message.</param>
        /// <param name="buffer">The destination byte array.</param>
        /// <returns>The number of bytes written.</returns>
        public static uint Serialize<T>(in T message, byte[] buffer) where T : ISerializable
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (message is null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            fixed (byte* bPtr = buffer)
            {
                Buffer b = new Buffer(bPtr, bPtr + buffer.Length);
                message.RosSerialize(ref b);
                return (uint) (b.ptr - bPtr);
            }
        }
    }
}