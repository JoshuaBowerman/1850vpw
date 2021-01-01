using System;
using System.Collections.Generic;
using System.Text;

namespace _1850vpw.Data
{
    class TestData : IDataInput
    {

        private int index = 0;


        /*Control Chars
        * 
        * -3 == Start of Frame
        * -4 == End of Data
        * -5 == End of Frame
        */

        private int[][] TestFrames = new int[][]
        {
            new int[]
            {
                -3,0b00011000,0xf2,0x45,0x33,0x21,-5
            },
            new int[]
            {
                -3,0b00011000,0xf2,0x45,0x33,0xEE,-5
            },
            new int[]
            {
                -3,0b00001000,0xff,0x43,0x21,0x32,0x34,0x12,-4,0,23,24,54,-5
            },
            new int[]
            {
                2,1,2,3,4,3,2,3,4,2,2,3
            }
        };

        public Tuple<bool, int[]> NextFrame(int timeout)
        {
            Tuple<bool, int[]> result = new Tuple<bool, int[]>(true, TestFrames[index++ % TestFrames.Length]);
            return result;
        }

        public void SendFrame(Frame f)
        {

        }

    }
}
