﻿using System;

namespace Iviz.Msgs
{
    /// <summary>
    /// Attribute that tells the Unity engine not to strip these fields even if no code accesses them
    /// (the only requirement is to have the exact name 'Preserve').
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class PreserveAttribute : System.Attribute { }

    public interface ISerializable
    {
        /// <summary>
        /// Fills this message with the information from the buffer.
        /// </summary>
        /// <param name="ptr">
        /// Pointer to the buffer position where the message starts.
        /// The position of the message end will be written to this pointer.
        /// </param>
        /// <param name="end">The maximum position that the function is allowed to read from.</param>
        unsafe void Deserialize(ref byte* ptr, byte* end);

        /// <summary>
        /// Fills the buffer with the information from this message.
        /// </summary>
        /// <param name="ptr">
        /// Pointer to the buffer position where the message will start.
        /// The position of the message end will be written to this pointer.
        /// </param>
        /// <param name="end">The maximum position that the function is allowed to write to.</param>
        unsafe void Serialize(ref byte* ptr, byte* end);

        /// <summary>
        /// Length of this message in bytes after serialization.
        /// </summary>
        int RosMessageLength { get; }
    }



}