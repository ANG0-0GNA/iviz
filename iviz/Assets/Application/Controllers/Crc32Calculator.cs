﻿namespace Iviz.Controllers
{
    public sealed class Crc32Calculator
    {
        const uint DefaultPolynomial = 0xedb88320u;
        const uint DefaultSeed = 0xffffffffu;

        readonly uint[] table;

        public Crc32Calculator()
        {
            table = InitializeTable();
        }

        static uint[] InitializeTable()
        {
            var createTable = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                uint entry = (uint) i;
                for (int j = 0; j < 8; j++)
                {
                    if ((entry & 1) == 1)
                    {
                        entry = (entry >> 1) ^ DefaultPolynomial;
                    }
                    else
                    {
                        entry >>= 1;
                    }
                }

                createTable[i] = entry;
            }

            return createTable;
        }

        unsafe uint CalculateHash(uint hash, byte* ptr, int size)
        {
            for (; size != 0; size--)
            {
                uint val = *ptr++;
                hash = (hash >> 8) ^ table[val ^ hash & 0xff];
            }

            return hash;
        }

        public unsafe uint Compute<T>(T value, uint startHash = DefaultSeed) where T : unmanaged
        {
            T* ptr = &value;
            return CalculateHash(startHash, (byte*) ptr, sizeof(T));
        }

        public unsafe uint Compute<T>(T[] array, uint startHash = DefaultSeed) where T : unmanaged
        {
            fixed (T* ptr = array)
            {
                return CalculateHash(startHash, (byte*) ptr, array.Length * sizeof(T));
            }
        }
    }
}