using System.Diagnostics;
using System.Management;
using HyperVSamples;

//var searcher = new ManagementObjectSearcher(@"root\virtualization\v2", "SELECT * FROM Msvm_ComputerSystem WHERE Description = 'Microsoft Virtual Machine'");
//foreach (var vm in searcher.Get())
//{
//    Dump(vm);
//}

var targets = new HashSet<string>(["ElementName", "EnabledState", "MemoryUsage", "Name", "ProcessorLoad", "UpTime", "Version"]);
var searcher2 = new ManagementObjectSearcher(@"root\virtualization\v2", "SELECT * FROM Msvm_SummaryInformation");
foreach (var vm in searcher2.Get())
{
    Dump(vm, targets);
}

//var searcher3 = new ManagementObjectSearcher(@"root\virtualization\v2", "SELECT * FROM Msvm_ProcessorSettingData");
//foreach (var vm in searcher3.Get())
//{
//    Dump(vm);
//}

//var searcher4 = new ManagementObjectSearcher(@"root\virtualization\v2", "SELECT * FROM Msvm_VirtualSystemManagementService");
//foreach (var vm in searcher4.Get())
//{
//    Dump(vm);
//}

static void Dump(ManagementBaseObject obj, HashSet<string> targets)
{
    Console.WriteLine("----------");
    foreach (var prop in obj.Properties)
    {
        if (targets.Contains(prop.Name))
        {
            Console.WriteLine($"{prop.Name} : {prop.Value?.GetType()} : {prop.Value}");
        }
    }
}

//GetSummaryInformationClassV2.GetSummaryInformation("VM-WORK-SERVICE");
//GetSummaryInformationClassV2.GetSummaryInformation("VM-WORK-ORACLE");
//GetSummaryInformationClassV2.GetSummaryInformation("VM-WORK-SERVICE");

//Console.ReadLine();

public class GetSummaryInformationClassV2
{
    public static void Dump(ManagementBaseObject obj)
    {
        Debug.WriteLine("----------");
        foreach (var prop in obj.Properties)
        {
            Debug.WriteLine($"{prop.Name} : {prop.Value}");
        }
    }

    public static void GetSummaryInformation(params string[] vmNames)
    {
        ManagementScope scope = new ManagementScope(@"root\virtualization\v2", null);
        ManagementObject virtualSystemService = Utility.GetServiceObject(scope, "Msvm_VirtualSystemManagementService");
        ManagementBaseObject inParams = virtualSystemService.GetMethodParameters("GetSummaryInformation");

        ManagementObject[] virtualSystemSettings = new ManagementObject[vmNames.Length];

        for (int i = 0; i < vmNames.Length; i++)
        {
            virtualSystemSettings[i] = GetVirtualSystemSetting(vmNames[i], scope);
        }

        UInt32[] requestedInformation = new UInt32[5];
        requestedInformation[0] = 1;    // ElementName
        requestedInformation[2] = 103;  // MemoryUsage
        requestedInformation[3] = 112;  // MemoryAvailable
        requestedInformation[4] = 101;

        inParams["SettingData"] = virtualSystemSettings;
        inParams["RequestedInformation"] = requestedInformation;

        ManagementBaseObject outParams = virtualSystemService.InvokeMethod("GetSummaryInformation", inParams, null);

        if ((UInt32)outParams["ReturnValue"] == ReturnCode.Completed)
        {
            Console.WriteLine("Summary information was retrieved successfully.");

            ManagementBaseObject[] summaryInformationArray =
                (ManagementBaseObject[])outParams["SummaryInformation"];

            foreach (ManagementBaseObject summaryInformation in summaryInformationArray)
            {
                Console.WriteLine("\nVirtual System Summary Information:");
                if ((null == summaryInformation["Name"]) ||
                    (summaryInformation["Name"].ToString().Length == 0))
                {
                    Console.WriteLine("\tThe VM or snapshot could not be found.");
                }
                else
                {
                    Console.WriteLine("\tName: {0}", summaryInformation["Name"].ToString());
                    Dump(summaryInformation);
                    foreach (UInt32 requested in requestedInformation)
                    {
                        switch (requested)
                        {
                            case 1:
                                Console.WriteLine("\tElementName: {0}", summaryInformation["ElementName"].ToString());
                                break;

                            case 103:
                                Console.WriteLine("\tMemoryUsage: {0}", summaryInformation["MemoryUsage"].ToString());
                                break;

                            case 112:
                                Console.WriteLine("\tMemoryAvailable: {0}", summaryInformation["MemoryAvailable"].ToString());
                                break;
                        }
                    }
                }
            }
        }
        else
        {
            Console.WriteLine("Failed to retrieve virtual system summary information");
        }

        inParams.Dispose();
        outParams.Dispose();
        virtualSystemService.Dispose();
    }

    public static ManagementObject GetVirtualSystemSetting(string vmName, ManagementScope scope)
    {
        ManagementObject virtualSystem = Utility.GetTargetComputer(vmName, scope);

        ManagementObjectCollection virtualSystemSettings = virtualSystem.GetRelated
         (
             "Msvm_VirtualSystemSettingData",
             "Msvm_SettingsDefineState",
             null,
             null,
             "SettingData",
             "ManagedElement",
             false,
             null
         );

        ManagementObject virtualSystemSetting = null;

        foreach (ManagementObject instance in virtualSystemSettings)
        {
            virtualSystemSetting = instance;
            break;
        }

        return virtualSystemSetting;

    }

}
