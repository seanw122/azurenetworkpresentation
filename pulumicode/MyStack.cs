using System.Collections.Generic;
using Pulumi;
using Pulumi.Azure.Compute;
using Pulumi.Azure.Compute.Inputs;
using Pulumi.Azure.Core;
using Pulumi.Azure.Network;
using Pulumi.Azure.Network.Inputs;

class MyStack : Stack
{
	private const string Location = "SouthCentralUS";
	private ResourceGroup _ResourceGroup;
	private Dictionary<string, Subnet> _Subnets;

    public MyStack()
    {
        _Subnets = new Dictionary<string, Subnet>();


        // Create an Azure Resource Group
        _ResourceGroup = new ResourceGroup("rsgMarketPlace", new ResourceGroupArgs
        {
            Location = Location,
            Name = "rsgMarketPlace"
        }, new CustomResourceOptions
        {
            Protect = true
        });

        BuildVNet();
        BuildNSG();

        BuildSupplierVMs(3);
        BuildFoodVMs(2);
        BuildClothingVMs(1);
        BuildFarmingVMs(1);
        BuildGardenVMs(1);

    }

    // 21 - 2048
    // 22 - 1024
    // 23 - 512
    // 24 - 256 
    // 25 - 128
    // 26 - 64
    // 27 - 32
    // 28 - 16
    // 29 - 8

    //.0 ~ .3 && .255
    // .1 subnet gateway

    //255.255.255.0  <-- 255

    //10.0.0.0/8  per subscription and per region
    //10.0.0.0/8  sub 1; region central us
    //10.0.0.0/8  sub 1; region east us 2
    //120.0.128.0
    
    private void BuildVNet()
    {
        var vnet = new VirtualNetwork("vnet1", new VirtualNetworkArgs
        {
            Name = "vnet1",
            Location = _ResourceGroup.Location,
            AddressSpaces = new[] {"10.0.0.0/22"}, //1024 addresses
            ResourceGroupName = _ResourceGroup.Name
        }, new CustomResourceOptions
        {
            Protect = true
        });

        var subnetSuppliers= new Subnet("Suppliers", new SubnetArgs
        {
            Name = "Suppliers",
            ResourceGroupName = _ResourceGroup.Name,
            AddressPrefixes = new[]{ "10.0.0.0/25"}, //128 addresses
            VirtualNetworkName = vnet.Name
        }, new CustomResourceOptions
        {
	        Protect = true
        });


        var subnetFood = new Subnet("Food", new SubnetArgs
        {
	        Name = "Food",
	        ResourceGroupName = _ResourceGroup.Name,
            AddressPrefixes = new[]{ "10.0.0.128/25"}, //128 addresses
            VirtualNetworkName = vnet.Name
        }, new CustomResourceOptions
        {
	        Protect = true
        });

        var subnetClothing = new Subnet("Clothing", new SubnetArgs
        {
	        Name = "Clothing",
	        ResourceGroupName = _ResourceGroup.Name,
            AddressPrefixes = new[]{ "10.0.1.0/25"}, //128 addresses
            VirtualNetworkName = vnet.Name
        }, new CustomResourceOptions
        {
	        Protect = true
        });

        var subnetFarming = new Subnet("Farming", new SubnetArgs
        {
	        Name = "Farming",
	        ResourceGroupName = _ResourceGroup.Name,
            AddressPrefixes = new[]{ "10.0.1.128/25"}, //128 addresses
            VirtualNetworkName = vnet.Name
        }, new CustomResourceOptions
        {
	        Protect = true
        });

        var subnetGarden = new Subnet("Garden", new SubnetArgs
        {
	        Name = "Garden",
	        ResourceGroupName = _ResourceGroup.Name,
            AddressPrefixes = new[]{ "10.0.2.0/25"}, //128 addresses
            VirtualNetworkName = vnet.Name
        }, new CustomResourceOptions
        {
	        Protect = true
        });

        _Subnets.Add("Suppliers",subnetSuppliers);
        _Subnets.Add("Food",subnetFood);
        _Subnets.Add("Clothing",subnetClothing);
        _Subnets.Add("Farming",subnetFarming);
        _Subnets.Add("Garden",subnetGarden);
    }

    private void BuildSupplierVMs(int countOfVMs)
    {
	    if (countOfVMs < 1) return;

	    for (int i = 0; i < countOfVMs; i++)
	    {
		    BuildVM(i, "Suppliers", "vm");
	    }
    }

    private void BuildFoodVMs(int countOfVMs)
    {
	    if (countOfVMs < 1) return;

	    for (int i = 0; i < countOfVMs; i++)
	    {
		    BuildVM(i, "Food", "vmFood");
	    }
    }

    private void BuildClothingVMs(int countOfVMs)
    {
	    if (countOfVMs < 1) return;

	    for (int i = 0; i < countOfVMs; i++)
	    {
		    BuildVM(i, "Clothing", "vmClothing");
	    }
    }

    private void BuildGardenVMs(int countOfVMs)
    {
	    if (countOfVMs < 1) return;

	    for (int i = 0; i < countOfVMs; i++)
	    {
		    BuildVM(i, "Garden", "vmGarden");
	    }
    }

    private void BuildFarmingVMs(int countOfVMs)
    {
	    if (countOfVMs < 1) return;

	    for (int i = 0; i < countOfVMs; i++)
	    {
		    BuildVM(i, "Farming", "vmFarming");
	    }
    }

    private void BuildVM(int index, string subnetName, string vmPrefix)
    {
	    var vmName = vmPrefix + index;

        var nic = new NetworkInterface(vmName + "-nic", new NetworkInterfaceArgs
        {
            ResourceGroupName = _ResourceGroup.Name,
            Location = _ResourceGroup.Location,
            Name = vmName + "-nic",
            IpConfigurations = new NetworkInterfaceIpConfigurationArgs
            {
				Name = "Ipconfig1",
                Primary = true,
                PrivateIpAddressAllocation = "Dynamic",  //Static
                PrivateIpAddressVersion = "IPv4",
                SubnetId = _Subnets[subnetName].Id
            }
        });

        var vm = new VirtualMachine(vmName, new VirtualMachineArgs
        {
			ResourceGroupName = _ResourceGroup.Name,
            Location = _ResourceGroup.Location,
            Name = vmName,
            VmSize = "Standard_B1ms",
            NetworkInterfaceIds = new[] {nic.Id},
            OsProfile = new VirtualMachineOsProfileArgs
            {
				AdminUsername = "thisisnotadmin",
                AdminPassword = "DontTellAnyone!",
                ComputerName = vmName
            },
            OsProfileLinuxConfig = new VirtualMachineOsProfileLinuxConfigArgs
            {
                DisablePasswordAuthentication = false
            },
            StorageImageReference = new VirtualMachineStorageImageReferenceArgs
            {
                Sku = "18.04-LTS",
                Publisher = "Canonical",
                Offer = "UbuntuServer",
                Version = "Latest"
            },
            StorageOsDisk = new VirtualMachineStorageOsDiskArgs
            {
                Name = vmName + "-OsDisk",
                Caching = "ReadWrite",
                CreateOption = "FromImage",
                OsType = "Linux",
                DiskSizeGb = 30,
                ManagedDiskType = "Premium_LRS"
            }

        });
    }

    private void BuildNSG()
    {
	    var rule1 = new NetworkSecurityGroupSecurityRuleArgs
	    {
            Name = "FoodMerchantTraffic",
            Access = "Deny", //Deny, Allow
            Direction = "Inbound", //Outbound
            Priority = 200,
            Protocol = "TCP", //UDP,TCP,ICMP
            SourcePortRange = "*",  // *== Any
            DestinationPortRange = "*",
            SourceAddressPrefixes = new []{ "10.0.0.128/25" },
            DestinationAddressPrefixes = new []{ "10.0.0.0/25" },
            Description = "Block access to suppliers by food merchants as there no suppliers here for food."
	    };


	    var rule2 = new NetworkSecurityGroupSecurityRuleArgs
	    {
		    Name = "MerchantTraffic",
		    Access = "Allow", //Deny, Allow
		    Direction = "Inbound", //Outbound
		    Priority = 210,
		    Protocol = "TCP", //UDP,TCP,ICMP
		    SourcePortRange = "*",  // *== Any
		    DestinationPortRanges = new[] { "80" },
		    SourceAddressPrefixes = new[] { "10.0.0.0/25", "10.0.1.0/25", "10.0.1.128/25", "10.0.2.0/25" },
		    DestinationAddressPrefixes = new[] { "10.0.0.128/25" }
	    };

        var securityRules = new List<NetworkSecurityGroupSecurityRuleArgs> {rule1, rule2};

	    var nsg = new NetworkSecurityGroup("nsg", new NetworkSecurityGroupArgs
	    {
			Location = _ResourceGroup.Location,
            ResourceGroupName = _ResourceGroup.Name,
            Name = "nsg",
            SecurityRules = securityRules
	    });

	    new SubnetNetworkSecurityGroupAssociation("assc1", new SubnetNetworkSecurityGroupAssociationArgs
	    {
			NetworkSecurityGroupId = nsg.Id,
            SubnetId = _Subnets["Food"].Id
	    });

	    new SubnetNetworkSecurityGroupAssociation("assc2", new SubnetNetworkSecurityGroupAssociationArgs
	    {
		    NetworkSecurityGroupId = nsg.Id,
		    SubnetId = _Subnets["Clothing"].Id
	    });
    }
}
