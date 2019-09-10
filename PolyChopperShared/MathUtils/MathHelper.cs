/*
 * This file is a version of one that was origanilly made for the MonoGame project.
 * It has been modified for use with the Clipper library by Gerhard de Clercq for PolyVoxel
 * gerharddeclercq@outlook.com
 */ 


using System;
using System.Collections.Generic;
using System.Text;


namespace MathUtils
{
    /// <summary>
    /// Contains commonly used precalculated values and mathematical operations.
    /// </summary>
    public static class MathHelper
    {
        /// <summary>
        /// Represents the mathematical constant e(2.71828175).
        /// </summary>
        public const float E = (float)Math.E;

        /// <summary>
        /// Represents the log base ten of e(0.4342945).
        /// </summary>
        public const float Log10E = 0.4342945f;

        /// <summary>
        /// Represents the log base two of e(1.442695).
        /// </summary>
        public const float Log2E = 1.442695f;

        /// <summary>
        /// Represents the value of pi(3.14159274).
        /// </summary>
        public const float Pi = (float)Math.PI;

        /// <summary>
        /// Represents the value of pi divided by two(1.57079637).
        /// </summary>
        public const float PiOver2 = (float)(Math.PI / 2.0);

        /// <summary>
        /// Represents the value of pi divided by four(0.7853982).
        /// </summary>
        public const float PiOver4 = (float)(Math.PI / 4.0);

        /// <summary>
        /// Represents the value of pi times two(6.28318548).
        /// </summary>
        public const float TwoPi = (float)(Math.PI * 2.0);

        /// <summary>
        /// Returns the Cartesian coordinate for one axis of a point that is defined by a given triangle and two normalized barycentric (areal) coordinates.
        /// </summary>
        /// <param name="value1">The coordinate on one axis of vertex 1 of the defining triangle.</param>
        /// <param name="value2">The coordinate on the same axis of vertex 2 of the defining triangle.</param>
        /// <param name="value3">The coordinate on the same axis of vertex 3 of the defining triangle.</param>
        /// <param name="amount1">The normalized barycentric (areal) coordinate b2, equal to the weighting factor for vertex 2, the coordinate of which is specified in value2.</param>
        /// <param name="amount2">The normalized barycentric (areal) coordinate b3, equal to the weighting factor for vertex 3, the coordinate of which is specified in value3.</param>
        /// <returns>Cartesian coordinate of the specified point with respect to the axis being used.</returns>
        public static long Barycentric(long value1, long value2, long value3, float amount1, float amount2)
        {
            return (long)(value1 + (value2 - value1) * amount1 + (value3 - value1) * amount2);
        }


        /// <summary>
        /// Performs a Catmull-Rom interpolation using the specified positions.
        /// </summary>
        /// <param name="value1">The first position in the interpolation.</param>
        /// <param name="value2">The second position in the interpolation.</param>
        /// <param name="value3">The third position in the interpolation.</param>
        /// <param name="value4">The fourth position in the interpolation.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <returns>A position that is the result of the Catmull-Rom interpolation.</returns>
        public static long CatmullRom(long value1, long value2, long value3, long value4, float amount)
        {
            // Using formula from http://www.mvps.org/directx/articles/catmull/
            // Internally using doubles not to lose precission
            double amountSquared = amount * amount;
            double amountCubed = amountSquared * amount;
            return (long)(0.5 * (2.0 * value2 +
                (value3 - value1) * amount +
                (2.0 * value1 - 5.0 * value2 + 4.0 * value3 - value4) * amountSquared +
                (3.0 * value2 - value1 - 3.0 * value3 + value4) * amountCubed));
        }


        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value. If <c>value</c> is less than <c>min</c>, <c>min</c> will be returned.</param>
        /// <param name="max">The maximum value. If <c>value</c> is greater than <c>max</c>, <c>max</c> will be returned.</param>
        /// <returns>The clamped value.</returns>
        public static long Clamp(long value, long min, long max)
        {
            // First we check to see if we're greater than the max
            value = (value > max) ? max : value;


            // Then we check to see if we're less than the min.
            value = (value < min) ? min : value;


            // There's no check to see if min > max.
            return value;
        }

        /// <summary>
        /// Restricts a value to be within a specified range.
        /// </summary>
        /// <param name="value">The value to clamp.</param>
        /// <param name="min">The minimum value. If <c>value</c> is less than <c>min</c>, <c>min</c> will be returned.</param>
        /// <param name="max">The maximum value. If <c>value</c> is greater than <c>max</c>, <c>max</c> will be returned.</param>
        /// <returns>The clamped value.</returns>
        public static int Clamp(int value, int min, int max)
        {
            value = (value > max) ? max : value;
            value = (value < min) ? min : value;
            return value;
        }

        /// <summary>
        /// Calculates the absolute value of the difference of two values.
        /// </summary>
        /// <param name="value1">Source value.</param>
        /// <param name="value2">Source value.</param>
        /// <returns>Distance between the two values.</returns>
        public static float Distance(long value1, long value2)
        {
            return Math.Abs(value1 - value2);
        }

        /// <summary>
        /// Performs a Hermite spline interpolation.
        /// </summary>
        /// <param name="value1">Source position.</param>
        /// <param name="tangent1">Source tangent.</param>
        /// <param name="value2">Source position.</param>
        /// <param name="tangent2">Source tangent.</param>
        /// <param name="amount">Weighting factor.</param>
        /// <returns>The result of the Hermite spline interpolation.</returns>
        public static long Hermite(long value1, long tangent1, long value2, long tangent2, float amount)
        {
            // All transformed to double not to lose precission
            // Otherwise, for high numbers of param:amount the result is NaN instead of Infinity
            double v1 = value1, v2 = value2, t1 = tangent1, t2 = tangent2, s = amount, result;
            double sCubed = s * s * s;
            double sSquared = s * s;


            if (amount == 0f)
                result = value1;
            else if (amount == 1f)
                result = value2;
            else
                result = (2 * v1 - 2 * v2 + t2 + t1) * sCubed +
                    (3 * v2 - 3 * v1 - 2 * t1 - t2) * sSquared +
                    t1 * s +
                    v1;
            return (long)result;
        }


        /// <summary>
        /// Linearly interpolates between two values.
        /// </summary>
        /// <param name="value1">Source value.</param>
        /// <param name="value2">Source value.</param>
        /// <param name="amount">Value between 0 and 1 indicating the weight of value2.</param>
        /// <returns>Interpolated value.</returns> 
        /// <remarks>This method performs the linear interpolation based on the following formula.
        /// <c>value1 + (value2 - value1) * amount</c>
        /// Passing amount a value of 0 will cause value1 to be returned, a value of 1 will cause value2 to be returned.
        /// </remarks>
        public static long Lerp(long value1, long value2, float amount)
        {
            return (long)(value1 + (value2 - value1) * amount);
        }

        /// <summary>
        /// Interpolates between two values using a cubic equation.
        /// </summary>
        /// <param name="value1">Source value.</param>
        /// <param name="value2">Source value.</param>
        /// <param name="amount">Weighting value.</param>
        /// <returns>Interpolated value.</returns>
        public static long SmoothStep(long value1, long value2, long amount)
        {
            // It is expected that 0 < amount < 1
            // If amount < 0, return value1
            // If amount > 1, return value2
            long result = MathHelper.Clamp(amount, 0, 1);
            result = MathHelper.Hermite(value1, 0, value2, 0, result);


            return result;
        }

        /// <summary>
        /// Reduces a given angle to a value between π and -π.
        /// </summary>
        /// <param name="angle">The angle to reduce, in radians.</param>
        /// <returns>The new angle, in radians.</returns>
        public static float WrapAngle(float angle)
        {
            angle = (float)Math.IEEERemainder((double)angle, 6.2831854820251465);
            if (angle <= -3.14159274f)
            {
                angle += 6.28318548f;
            }
            else
            {
                if (angle > 3.14159274f)
                {
                    angle -= 6.28318548f;
                }
            }
            return angle;
        }


        /// <summary>
        /// Determines if value is powered by two.
        /// </summary>
        /// <param name="value">A value.</param>
        /// <returns><c>true</c> if <c>value</c> is powered by two; otherwise <c>false</c>.</returns>
        public static bool IsPowerOfTwo(int value)
        {
            return (value > 0) && ((value & (value - 1)) == 0);
        }

        /// <summary>
        /// Calculates radians from degrees
        /// </summary>
        /// <param name="degrees">The degrees</param>
        /// <returns>The radians</returns>
        public static float ToRadians(float degrees)
        {
            return (float)(degrees * Math.PI / 180.0);
        }

        /// <summary>
        /// Calculates degrees from radians
        /// </summary>
        /// <param name="degrees">The radians</param>
        /// <returns>The degrees</returns>
        public static float ToDegrees(float radians)
        {
            return (float)(radians * 180.0 / Math.PI);
        }
    }
}

