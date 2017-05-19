using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace FUJI.FileWhatcherSCUService
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ServiceBase[] ServicesToRun;
                ServicesToRun = new ServiceBase[]
                {
                new FileWatcherSCU()
                };
                ServiceBase.Run(ServicesToRun);
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
                Console.ReadKey();
            }
        }
    }
}
