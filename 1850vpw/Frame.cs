using System;
using System.Collections.Generic;
using System.Text;

namespace _1850vpw
{
    class Frame
    {
        public byte[] header; // Either 1 or 3 bits
        public byte[] data;
        public byte[] IFR = new byte[0];
        public byte[] CRC = new byte[2]; //the first is the origional frame CRC second is the IFR CRC
        public bool IFRMissing = false; //This is true if the frame should contain an IFR but does not
        public bool passedCRC => (CRCResults[0] && CRCResults[1]); // this is whether or not the data and ifr both passed the CRC
        public bool[] CRCResults = new bool[2] { true, true };




        public Frame(int[] frame) //using an entire int for 1 byte is overkill but we use negatives to represent control bits
        {
            /*Control Chars
             * 
             * -3 == Start of Frame
             * -4 == End of Data
             * -5 == End of Frame
             */

            //Header
            bool threeByteHeader;
            if (frame[0] != -3)
            {
                //This is a malformed frame

                //Try to correct
                int startOfFrameIndex = -1;
                for (int i = 0; (i < frame.Length) && (startOfFrameIndex == -1); i++)
                {
                    if (frame[i] == -3)
                    {
                        startOfFrameIndex = i;
                    }
                }

                if (startOfFrameIndex == -1)
                {
                    //This frame contains no begining
                    //Might just be garbage

                    // TODO: Setup a way to try to parse the frame anyways and see if the CRC is still valid.
                    //The only reason i haven't done this is that when the frame is missing its SOF it ussually is also preceded by garbage.

                    throw new Exception("Invalid Frame");
                }

                //move the frame so it starts with SOF
                int[] newFrame = new int[frame.Length - startOfFrameIndex];

                for (int i = startOfFrameIndex; i < frame.Length; i++)
                {
                    newFrame[i - startOfFrameIndex] = frame[startOfFrameIndex];
                }
            }


            // 1:1 2:2 3:4 4:8 5:16 ...
            //bit 4 is the header style, note that thats actually bit 5 we start counting from zero for this
            //second entry because first entry is the start of frame control character
            if (bit(frame[1], 4))
            {
                //1 byte header
                threeByteHeader = false;
                header = new byte[1] { (byte)frame[1] };
            }
            else
            {
                //3 byte header
                threeByteHeader = true;
                header = new byte[3] { (byte)frame[1], (byte)frame[2], (byte)frame[3] };
            }

            //Data
            int startingIndex;
            if (threeByteHeader)
            {
                startingIndex = 4;
            }
            else
            {
                startingIndex = 2;
            }

            int NextSectionIndex = 0;
            for (int i = startingIndex; (NextSectionIndex == 0) && (i < frame.Length); i++)
            {
                if (frame[i] < 0)
                {
                    NextSectionIndex = i;
                }
            }


            //Write the data
            data = new byte[NextSectionIndex - startingIndex - 1]; // the reason for the -1 is that the last byte is a CRC byte

            for (int i = startingIndex; i < NextSectionIndex - 1; i++)
            {
                data[i - startingIndex] = (byte)frame[i];
            }

            CRC[0] = (byte)frame[NextSectionIndex - 1];

            if (bit(header[0], 3)) //This is whether or not there is a IFR false is Required, true is not allowed
            {
                //No IFR

            }
            else
            {
                //IFR

                //Check if we got EOD or EOF
                if (frame[NextSectionIndex] == -4)
                {
                    //End of Data
                    IFR = new byte[frame.Length - (NextSectionIndex + 3)];
                    for (int i = NextSectionIndex + 1; i < frame.Length - 2; i++) // not the last 2 because second last is CRC and last is a control character
                    {
                        IFR[i - NextSectionIndex - 1] = (byte)frame[i];
                    }
                    CRC[1] = (byte)frame[frame.Length - 2];
                }
                else
                {
                    //End of Frame
                    IFRMissing = true;
                }


            }

            //Do CRC
            var word1 = new byte[header.Length + data.Length];
            header.CopyTo(word1, 0);
            data.CopyTo(word1, header.Length);
            CRCResults[0] = CRC[0] == computeCRC(word1);
            if (IFR.Length > 0)
            {
                CRCResults[1] = CRC[1] == computeCRC(IFR);
            }
        }

        /*
         * Gets the corresponding bit from a byte or int
         * bit is offset by 1 so bit 1 is i=0....
         */
        public static bool bit(int v, int i) => bit((byte)v, i);
        public static bool bit(byte v, int i)
        {
            return (((v % (1 << i + 1)) - (v % (1 << (i)))) > 0) ? true : false;
        }


        /*
         * Does CRC to the provided data
         * returns the CRC byte
         */
        public static byte computeCRC(byte[] data)
        {

            //This is not super straight foward
            //by that i mean it's some complicated math

            /*
             * 
             * The initial value is 0xFF
             * All frame bits that occur after SOF and before the CRC field are used to form the data segment polynomial which is designated as D(X)  this number can be interpreted as
             * an n-bit binary constant, where n is equal to the data length in bits.
             * 
             * The CRC division polynomial is X^8 + X^4 + X^3 + X^2 + 1  This polynomial is designated as P(X) this is 0x11D.
             * 
             * The remainder Polynomial R(X) is determained from (X^8* D(X) + X^n + X^n+1 + X^n+2 + X^n+3 + X^n+4 + X^n+5 + X^n+6 + X^n+7) / P(X) = Q(X) + (R(X)/P(X))
             * Q(X) is the quotient resulting from the division process
             * 
             * The CRC byte is equal to the ones compliment of R(X)
             * 
             * C(X) = X^k D(X) XOR R(X)
             * 
             */

            //all that work for it to end up this simple



            //We compute the table rather than just computing each one when needed since most messages are bigger than 256 bytes.
            byte[] table = new byte[256];


            //Generate Table
            for (int i = 0; i < 256; ++i)
            {
                int entry = i;
                for (int j = 0; j < 8; ++j)
                {
                    if ((entry & 0b10000000) != 0)
                    {
                        entry = (entry << 1) ^ 0x11D;
                    }
                    else
                    {
                        entry <<= 1;
                    }
                }
                table[i] = (byte)entry;
            }

            //Compute CRC

            byte crc = 0xFF; // remember i said it was 0xFF intitialy
            foreach (byte v in data)
            {
                crc = table[crc ^ v];
            }

            crc = (byte)((int)crc ^ 0xff); // it is inverted at the end


            return crc;


        }

        public override string ToString()
        {
            string result = "";

            for(int i = 0; i < 3; i++)
            {
                if(i < header.Length)
                {
                    result += String.Format("{0:X2} ", header[i]);
                }
                else
                {
                    result += String.Format("   ");
                }
            }
            result += "| ";
            for (int i = 0; i < 12; i++)
            {
                if (i < data.Length)
                {
                    result += String.Format("{0:X2} ", data[i]);
                }
                else
                {
                    result += String.Format("   ");
                }
            }
            result += "[" + String.Format("{0:X2}", CRC[0]) + "]";
            result += "| ";
            for (int i = 0; i < 12; i++)
            {
                if (i < IFR.Length)
                {
                    result += String.Format("{0:X2} ", IFR[i]);
                }
                else
                {
                    result += String.Format("   ");
                }
            }
            result += "[" + String.Format("{0:X2}", CRC[1]) + "]";
            result += "| ";
            return result;
        }

        public void Print()
        {

            for (int i = 0; i < 3; i++)
            {
                if (i < header.Length)
                {
                    Console.Write(String.Format("{0:X2} ", header[i]));
                }
                else
                {
                    Console.Write(String.Format("   "));
                }
            }
            Console.Write("| ");
            for (int i = 0; i < 12; i++)
            {
                if (i < data.Length)
                {
                    Console.Write(String.Format("{0:X2} ", data[i]));
                }
                else
                {
                    Console.Write(String.Format("   "));
                }
            }
            if (CRCResults[0])
            {
                Console.BackgroundColor = ConsoleColor.Green;
            }
            else
            {
                Console.BackgroundColor = ConsoleColor.Red;
            }
            Console.Write("[" + String.Format("{0:X2}", CRC[0]) + "]");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write("| ");
            for (int i = 0; i < 12; i++)
            {
                if (i < IFR.Length)
                {
                    Console.Write(String.Format("{0:X2} ", IFR[i]));
                }
                else
                {
                    Console.Write(String.Format("   "));
                }
            }
            if (CRCResults[0])
            {
                Console.BackgroundColor = ConsoleColor.Green;
            }
            else
            {
                Console.BackgroundColor = ConsoleColor.Red;
            }
            Console.Write("[" + String.Format("{0:X2}", CRC[1]) + "]");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine("| ");
        }
    }
}
