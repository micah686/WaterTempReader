using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;

class SerialConsole
{
    // A flag to let our background reading task know when to stop
    static bool isRunning = true;

    static void Main(string[] args)
    {
        string portName = "/dev/ttyACM0"; // Windows: COM3 | Linux: /dev/ttyACM0
        int baudRate = 115200;

        using SerialPort serialPort = new SerialPort(portName, baudRate);
        serialPort.DtrEnable = true;
        serialPort.RtsEnable = true;

        try
        {
            serialPort.Open();
            Console.WriteLine($"Connected to {serialPort.PortName}.");
            Console.WriteLine("Type a message and press Enter to send. Type 'EXIT' to quit.\n");

            // 1. Start the background task that will continuously poll for data
            Task readTask = Task.Run(() => ContinuouslyReadData(serialPort));

            // 2. The Main loop handles sending user input
            while (isRunning)
            {
                // This blocks until the user hits Enter, but our background task keeps running!
                string input = Console.ReadLine();
                
                if (input?.ToUpper() == "EXIT")
                {
                    isRunning = false; // Tell the background loop to stop
                    break;
                }

                serialPort.WriteLine(input); 
            }

            // Give the background task a split-second to finish up before we close the port
            readTask.Wait(500); 
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
                Console.WriteLine("Port closed.");
            }
        }
    }

    // --- APPROACH 2 IMPLEMENTATION ---

    /// <summary>
    /// Grabs whatever data is currently in the buffer and returns it as a string.
    /// </summary>
    public static string ReadAvailableData(SerialPort port)
    {
        // Safely check if data is actually waiting to avoid exceptions
        if (port != null && port.IsOpen && port.BytesToRead > 0)
        {
            return port.ReadExisting();
        }
        
        return string.Empty; 
    }

    /// <summary>
    /// The background loop that constantly calls ReadAvailableData
    /// </summary>
    private static void ContinuouslyReadData(SerialPort port)
    {
        while (isRunning)
        {
            // Call our Approach 2 function
            string chunk = ReadAvailableData(port);
            
            if (!string.IsNullOrEmpty(chunk))
            {
                // Print the returned string directly to the console
                Console.Write(chunk);
            }

            // CRITICAL: A tiny pause (10 milliseconds). 
            // Without this, the 'while' loop will run millions of times a second 
            // and max out an entire CPU core doing nothing!
            Thread.Sleep(10); 
        }
    }
}