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
        ISerializable RosDeserialize(Buffer b);

        /// <summary>
        /// Fills the buffer with the information from this message.
        /// </summary>
        /// <param name="ptr">
        /// Pointer to the buffer position where the message will start.
        /// The position of the message end will be written to this pointer.
        /// </param>
        /// <param name="end">The maximum position that the function is allowed to write to.</param>
        void RosSerialize(Buffer b);

        /// <summary>
        /// Length of this message in bytes after serialization.
        /// </summary>
        int RosMessageLength { get; }

        /// <summary>
        /// Checks if this message is valid. If not, throws an exception.
        /// </summary>
        void RosValidate();
    }



}