using System;
using System.IO.Ports;

namespace _1850vpw
{
    class Program
    {

        private static SerialPort Com;

#if TESTING
        //Test Data
        private static bool usingTestData = false;
#endif

        static void Main(string[] args)
        {
#if DEBUG
            Console.WriteLine("J1850 VPW  --DEBUG--");.
            Console.Title = "J1850 VPW  --DEBUG--";
#elif TESTING
            Console.WriteLine("J1850 VPW  --TESTING--");
            Console.Title = "J1850 VPW  --TESTING--";

#else
            Console.WriteLine("J1850 VPW Interpreter");
            Console.Title = "J1850 VPW Interpreter";
#endif

            //Ask the User to select the appropriate Serial Port

            int index = -1;
            string[] ports = { };

            while (index == -1)
            {
                Console.WriteLine("\nPlease select the interface device from the following list.\nUsually this is COM3.");
                ports = SerialPort.GetPortNames();
                for (int i = 0; i < ports.Length; i++)
                {
                    Console.WriteLine(String.Format("{0}: {1}", i + 1, ports[i]));
                }
#if TESTING
                Console.WriteLine(String.Format("{0}: {1}", ports.Length + 1, "TEST DATA"));
#endif
                Console.Write("Selection:");
                string selection = Console.ReadLine();

                try
                {
                    index = int.Parse(selection);
                }

                catch (Exception e)
                {
                    index = -1; // redundant
                }

                //Check to see if the selection is valid
#if TESTING
                if (index - 1 > ports.Length || index < 1)
                {
                    index = -1;
                }
#else
                if (index > ports.Length || index < 1)
                {
                    index = -1;
                }
#endif
                //reset the screen if needed
                if(index == -1)
                {
                    Console.Clear();
                    Console.WriteLine("Your selection was invalid, please try again. For example if the the item you want is \"2: COM3\" you would need to enter 2 not COM3.");
                }
            }

            //Connect to the selected Device

            Console.Clear();
            Data.IDataInput input;
#if TESTING

            if(index > ports.Length)
            {
                Console.WriteLine("You have decided to use the testing interface, as such test data will be used.");
                usingTestData = true;
                input = new Data.TestData();
            }
            else
            {
                Console.WriteLine(String.Format("You have selected \"{0}\" as the interface.", ports[index - 1]));
                input = new Data.SerialData(new SerialPort(ports[index]));
            }
#else
            Console.WriteLine(String.Format("You have selected \"{0}\" as the interface.", ports[index - 1]));
            input = new Data.SerialData(new SerialPort(ports[index]));
#endif
            for(int i = 0; i < 10; i++)
            {
                try
                {
                    (new Frame(input.NextFrame(0).Item2)).Print();
                }catch(Exception e)
                {
                    Console.BackgroundColor = ConsoleColor.Red;
                    Console.WriteLine(e.Message);
                    Console.BackgroundColor = ConsoleColor.Black;
                }
            }








        }
    }
}
