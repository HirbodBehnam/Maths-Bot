﻿using System;
using System.Collections.Generic;

namespace Maths_Bot
{
    public static class MathCore
    {
        /// <summary>
        /// Find factors of a number; Example: 6-> 1,2,3,6
        /// </summary>
        /// <param name="number">The number to find factors of</param>
        /// <returns>Array of factors</returns>
        public static uint[] Factors(uint number)
        {
            List<uint> factors = new List<uint>();
            uint to = (uint)Math.Sqrt(number);
            for(uint i = 1; i <= to; i++)
            {
                if(number % i == 0)
                {
                    factors.Add(i);
                    factors.Add(number / i);
                }
            }
            factors.Sort();
            if (factors[factors.Count / 2] == factors[factors.Count / 2 - 1]) //What is this? Remove this line and try 49
                factors.RemoveAt(factors.Count / 2);
            return factors.ToArray();
        }
        /// <summary>
        /// Find greatest common divisor of two numbers
        /// </summary>
        /// <param name="a">First number</param>
        /// <param name="b">Second number</param>
        /// <returns>The greatest common divisor</returns>
        public static uint GCD(uint a, uint b) => b == 0 ? a : GCD(b,a%b);
        /// <summary>
        /// Factorize a number to prime factors
        /// </summary>
        /// <param name="a">The number to factorize</param>
        /// <returns>List of prime factors</returns>
        public static uint[] Factorize(uint a)
        {
            List<uint> factors = new List<uint>();
            uint to = (uint)Math.Sqrt(a);
            while (a % 2 == 0)
            {
                factors.Add(2);
                a /= 2;
            }
            while (a % 3 == 0)
            {
                factors.Add(3);
                a /= 3;
            }
            uint i = 5;
            while(a != 1)
            {
                if(i > to)
                {
                    factors.Add(a);
                    break;
                }
                while(a % i == 0)
                {
                    factors.Add(i);
                    a /= i;
                }
                i+=2;
                while (a % i == 0)
                {
                    factors.Add(i);
                    a /= i;
                }
                i += 4;
            }
            return factors.ToArray();
        }
        /// <summary>
        /// Detect if a number is prime
        /// </summary>
        /// <param name="number">The number to test</param>
        /// <returns>1 if number is prime otherwise returns a factor of number</returns>
        public static uint DetectPrime(uint number)
        {
            if (number == 2 || number == 3 || number == 5 || number == 7)
                return 1;
            if (number % 2 == 0)
                return 2;
            if (number % 3 == 0)
                return 3;
            uint to = (uint)Math.Sqrt(number);
            for (uint i = 5; i <= to; i += 6)
            {
                if (number % i == 0)
                    return i;
                if (number % (i + 2) == 0)
                    return i + 2;
            }
            return 1;
        }
    }
}
