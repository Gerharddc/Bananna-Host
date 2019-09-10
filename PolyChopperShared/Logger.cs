using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PolyChopper
{
    /// <summary>
    /// This class contains all the methods needed for logging slicing progress
    /// </summary>
    public static class Logger
    {
        //TODO: this class should have methods that can calculate the progress percentage based upon logged messages

        public delegate void logEventHandler(string message);

        /// <summary>
        /// This event is raised whenever a log message is generated
        /// </summary>
        public static event logEventHandler logEvent;

        /// <summary>
        /// This method logs a given progress message
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void logProgress(string message)
        {
            Debug.WriteLine(message);

            if (logEvent != null)
                logEvent(message);
        }
    }
}
