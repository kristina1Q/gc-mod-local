using System;
using System.IO;
using System.Linq;
using AssetRipper.Primitives;
using LibCpp2IL;
using Iced.Intel;

namespace Inspector
{
    class Program
    {
        static void Main(string[] args)
        {
            string pePath = @"c:\Users\34632\girlscreation_r\GameAssembly.dll";
            string metaPath = @"c:\Users\34632\girlscreation_r\GC_Data\il2cpp_data\Metadata\global-metadata.dat";
            var uv = UnityVersion.Parse("6000.3.8f1");

            LibCpp2IlMain.LoadFromFile(pePath, metaPath, uv);

            ulong tVa = 0x182F52E20;
            long o = LibCpp2IlMain.Binary.MapVirtualAddressToRaw(tVa);
            uint tok = BitConverter.ToUInt32(LibCpp2IlMain.Binary.GetRawBinaryContent(), (int)o);
            Console.WriteLine($"Token at 0x{tVa:X}: 0x{tok:X8}");
            uint type = tok >> 29;
            uint index = tok & 0x1FFFFFFF;
            Console.WriteLine($"type={type}, index={index}");
            var tmpText = LibCpp2IlMain.TheMetadata.typeDefs.FirstOrDefault(t => t.Name == "TMP_Text");
            if (tmpText != null)
            {
                Console.WriteLine("Found TMP_Text typeDef: " + tmpText.FullName);
                foreach (var method in tmpText.Methods)
                {
                    if (method.Name.Contains("text") || method.Name.Contains("OnEnable"))
                    {
                        Console.WriteLine($"  Method: {method.Name} (params: {method.Parameters.Length})");
                    }
                }
            }
            var tmpUgui = LibCpp2IlMain.TheMetadata.typeDefs.FirstOrDefault(t => t.Name == "TextMeshProUGUI");
            if (tmpUgui != null)
            {
                Console.WriteLine("Found TextMeshProUGUI typeDef: " + tmpUgui.FullName);
                foreach (var method in tmpUgui.Methods)
                {
                    if (method.Name.Contains("OnEnable") || method.Name.Contains("Awake"))
                    {
                        Console.WriteLine($"  Method: {method.Name} (params: {method.Parameters.Length})");
                    }
                }
            }
        }
    }
}
