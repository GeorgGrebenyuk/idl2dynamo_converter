namespace AX2LIB
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IDL_reader reader = new IDL_reader(@"E:\Code\NanosoftWork\trunk_API\ru\ncX_NCAuto_and_OdaX_docomatic\source\23\NCAuto.IDL");
            NET_DLL_Writer writer = new NET_DLL_Writer(reader.NET_prototype, @"E:\Temp\IDL_PROJECT");
            writer.Create();

            Console.WriteLine("End!");
        }
    }
}