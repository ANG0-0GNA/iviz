﻿

namespace Iviz.Msgs
{
    /// <summary>
    /// Interface for all ROS messages.
    /// All classes or structs representing ROS messages derive from this.
    /// </summary>
    public interface IMessage
    {
        /// <summary>
        /// Create a new instance of a message of this type.
        /// </summary>
        /// <returns>New message</returns>
        IMessage Create();

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
        /// Length of this message in bytes.
        /// </summary>
        int GetLength();
    }

}