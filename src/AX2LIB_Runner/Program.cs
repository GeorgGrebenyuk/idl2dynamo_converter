using System.Runtime.CompilerServices;

namespace AX2LIB
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string dir_path = @"E:\Code\NanosoftWork\trunk_API\ru\ncX_NCAuto_and_OdaX_docomatic\source\23";
            string save_path = @"C:\Users\Georg\Documents\GitHub\idl2dynamo_converter\src\AXLIB_TestResult";
            foreach (string idl_path in Directory.GetFiles(dir_path, "*.IDL", SearchOption.AllDirectories))
            {
                IDL_reader reader = new IDL_reader(idl_path);
                reader.Start();
                NET_DLL_Writer writer = new NET_DLL_Writer(reader.NET_prototype, save_path);
                writer.Create();
            }
            Console.WriteLine("End!");
        }
    }
}