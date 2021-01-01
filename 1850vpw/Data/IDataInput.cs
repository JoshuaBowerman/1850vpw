using System;
using System.Collections.Generic;
using System.Text;

namespace _1850vpw.Data
{
    interface IDataInput
    {

        /*
         * Returns the next frame that is received unless it times out.
         * in which case the bool it returns is false; 
         * the int array is the frame, unformatted.
         * it can be turned into a Frame object 
         */
        Tuple<bool,int[]> NextFrame(int timeout);

        /*
         * Sends a frame object over the bus
         */
        //void SendFrame(Frame obj);

    }
}
