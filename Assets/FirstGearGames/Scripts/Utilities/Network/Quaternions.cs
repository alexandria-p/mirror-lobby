using UnityEngine;

namespace FirstGearGames.Utilities.Networks
{

    public static class Quaternions
    {

        #region Const.
        /// <summary>
        /// Multiplier applied towards float values. Used to move the decimal on floats.
        /// </summary>
        private const float DECIMAL_MULTIPLIER = 10000f;
        #endregion

        /// <summary>
        /// Returns true if to use largest axis only to compress and decompress.
        /// </summary>
        /// <param name="largest"></param>
        /// <returns></returns>
        public static bool UseLargestOnly(byte largest)
        {
            return (largest >= 4 && largest <= 7);
        }

        /// <summary>
        /// Compresses a quaternion by a number of decimals specified by DECIMAL_MULTIPLIER.
        /// </summary>
        /// <param name="rotation"></param>
        /// <param name="largestIndex"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        public static void CompressQuaternion(Quaternion rotation, out byte largestIndex, out short a, out short b, out short c)
        {
            //Index of which axis contains the largest value.
            largestIndex = 0;
            //Largest value of all axes.
            float largestValue = float.MinValue;
            float sign = -1f;

            a = 0;
            b = 0;
            c = 0;

            /* Find the largest axis in the quaternion. Once found only the smallest 3 axes
             * will be transmitted, and an index indicating the largest. The last axis can be
             * reconstructed using the smaller 3 axes. */
            for (int i = 0; i < 4; i++)
            {
                float element = rotation[i];
                float abs = Mathf.Abs(rotation[i]);
                if (abs > largestValue)
                {
                    // We don't need to explicitly transmit the sign bit of the omitted element because you 
                    // can make the omitted element always positive by negating the entire quaternion if 
                    // the omitted element is negative (in quaternion space (x,y,z,w) and (-x,-y,-z,-w) 
                    // represent the same rotation.), but we need to keep track of the sign for use below.
                    sign = (element < 0) ? -1 : 1;

                    // Keep track of the index of the largest element
                    largestIndex = (byte)i;
                    largestValue = abs;
                }
            }

            // If the maximum value is approximately 1f (such as Quaternion.identity [0,0,0,1]), then we can 
            // reduce storage even further due to the fact that all other fields must be 0f by definition, so 
            // we only need to send the index of the largest field.
            if (Mathf.Approximately(largestValue, 1f))
            {
                largestIndex += 4;
                return;
            }

            // We multiply the value of each element by QUAT_PRECISION_MULT before converting to 16-bit integer 
            // in order to maintain precision. This is necessary since by definition each of the three smallest 
            // elements are less than 1.0, and the conversion to 16-bit integer would otherwise truncate everything 
            // to the right of the decimal place. This allows us to keep five decimal places.
            if (largestIndex == 0)
            {
                a = (short)(rotation.y * sign * DECIMAL_MULTIPLIER);
                b = (short)(rotation.z * sign * DECIMAL_MULTIPLIER);
                c = (short)(rotation.w * sign * DECIMAL_MULTIPLIER);
            }
            else if (largestIndex == 1)
            {
                a = (short)(rotation.x * sign * DECIMAL_MULTIPLIER);
                b = (short)(rotation.z * sign * DECIMAL_MULTIPLIER);
                c = (short)(rotation.w * sign * DECIMAL_MULTIPLIER);
            }
            else if (largestIndex == 2)
            {
                a = (short)(rotation.x * sign * DECIMAL_MULTIPLIER);
                b = (short)(rotation.y * sign * DECIMAL_MULTIPLIER);
                c = (short)(rotation.w * sign * DECIMAL_MULTIPLIER);
            }
            else
            {
                a = (short)(rotation.x * sign * DECIMAL_MULTIPLIER);
                b = (short)(rotation.y * sign * DECIMAL_MULTIPLIER);
                c = (short)(rotation.z * sign * DECIMAL_MULTIPLIER);
            }
        }

        /// <summary>
        /// Creates a Quaternion by taking values from CompressQuaternion.
        /// </summary>
        /// <param name="largestIndex"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public static Quaternion DecompressQuaternion(byte largestIndex, short a, short b, short c)
        {
            /* If to use largest index only then build the quaternion
             * with 0f values on everything but the largest index, and 1f
             * on the largest index. */
            if (UseLargestOnly(largestIndex))
            {
                float x = (largestIndex == 4) ? 1f : 0f;
                float y = (largestIndex == 5) ? 1f : 0f;
                float z = (largestIndex == 6) ? 1f : 0f;
                float w = (largestIndex == 7) ? 1f : 0f;

                return new Quaternion(x, y, z, w);
            }

            //Read the other three fields and derive the value of the omitted field
            float rA = (float)a / DECIMAL_MULTIPLIER;
            float rB = (float)b / DECIMAL_MULTIPLIER;
            float rC = (float)c / DECIMAL_MULTIPLIER;
            float rD = Mathf.Sqrt(1f - (rA * rA + rB * rB + rC * rC));

            if (largestIndex == 0)
                return new Quaternion(rD, rA, rB, rC);
            else if (largestIndex == 1)
                return new Quaternion(rA, rD, rB, rC);
            else if (largestIndex == 2)
                return new Quaternion(rA, rB, rD, rC);

            return new Quaternion(rA, rB, rC, rD);
        }

    }

}
