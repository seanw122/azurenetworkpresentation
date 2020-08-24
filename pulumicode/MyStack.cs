using System.Collections.Generic;
using Pulumi;
using Pulumi.Azure.Compute;
using Pulumi.Azure.Compute.Inputs;
using Pulumi.Azure.Core;
using Pulumi.Azure.FrontDoor.Inputs;
using Pulumi.Azure.Lb;
using Pulumi.Azure.Lb.Inputs;
using Pulumi.Azure.Network;
using Pulumi.Azure.Network.Inputs;
using Pulumi.Azure.Storage;
using pulumicode;

class MyStack : Stack
{
	private const string Location = "CentralUS";
	private ResourceGroup _ResourceGroup;
	private Dictionary<string, Subnet> _Subnets;
	private List<NetworkInterface> _foodVmNics;
	private VirtualNetwork _vnet;
	private Output<string> _stgPrimaryEndpoint;

	public MyStack()
	{
		_Subnets = new Dictionary<string, Subnet>();
		_foodVmNics = new List<NetworkInterface>();

		// Create an Azure Resource Group
		_ResourceGroup = new ResourceGroup("rsgMarketPlace", new ResourceGroupArgs
		{
			Location = Location,
			Name = "rsgMarketPlace"
		}, new CustomResourceOptions
		{
			Protect = true
		});

		_vnet = BuildVNet();

		_stgPrimaryEndpoint = BuildBootDiagStorageAccount();

		BuildNSG();

		BuildSupplierVMs(3);

		BuildFoodVMs(2);
		BuildClothingVMs(1);
		BuildFarmingVMs(1);
		BuildGardenVMs(1);
		BuildFoodLoadBalancer();
		//BuildFirewall();
		//BuildRouteTable();
		//BuildAppGateway();

		//new CosmosDB(_ResourceGroup).BuildCosmosDb();
		
		new SecondRegion(_vnet);
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
	//.1 subnet gateway

	//255.255.255.0  <-- 255

	//10.0.0.0/8  per subscription and per region
	//10.0.0.0/8  sub 1; region central us
	//10.0.0.0/8  sub 1; region east us 2


	private Output<string> BuildBootDiagStorageAccount()
	{
		var stg = new Account("stgBootDiag", new AccountArgs
		{
			Name = "stgnwkpresvmbootdiag",
			ResourceGroupName = _ResourceGroup.Name,
			Location = _ResourceGroup.Location,
			AccountKind = "StorageV2",
			AccountTier="Standard",
			AccountReplicationType = "LRS"
		});

		return stg.PrimaryBlobEndpoint;
	}

	private VirtualNetwork BuildVNet()
	{
		var vnet = new VirtualNetwork("vnet1", new VirtualNetworkArgs
		{
			Name = "vnet1",
			Location = _ResourceGroup.Location,
			AddressSpaces = new[] { "10.0.0.0/22" }, //1024 addresses
			ResourceGroupName = _ResourceGroup.Name
		}, new CustomResourceOptions
		{
			Protect = true
		});

		var subnetSuppliers = new Subnet("Suppliers", new SubnetArgs
		{
			Name = "Suppliers",
			ResourceGroupName = _ResourceGroup.Name,
			AddressPrefixes = new[] { "10.0.0.0/25" }, //128 addresses
			VirtualNetworkName = vnet.Name
		}, new CustomResourceOptions
		{
			Protect = true
		});


		var subnetFood = new Subnet("Food", new SubnetArgs
		{
			Name = "Food",
			ResourceGroupName = _ResourceGroup.Name,
			AddressPrefixes = new[] { "10.0.0.128/25" }, //128 addresses
			VirtualNetworkName = vnet.Name
		}, new CustomResourceOptions
		{
			Protect = true
		});

		var subnetClothing = new Subnet("Clothing", new SubnetArgs
		{
			Name = "Clothing",
			ResourceGroupName = _ResourceGroup.Name,
			AddressPrefixes = new[] { "10.0.1.0/25" }, //128 addresses
			VirtualNetworkName = vnet.Name
		}, new CustomResourceOptions
		{
			Protect = true
		});

		var subnetFarming = new Subnet("Farming", new SubnetArgs
		{
			Name = "Farming",
			ResourceGroupName = _ResourceGroup.Name,
			AddressPrefixes = new[] { "10.0.1.128/25" }, //128 addresses
			VirtualNetworkName = vnet.Name
		}, new CustomResourceOptions
		{
			Protect = true
		});

		var subnetGarden = new Subnet("Garden", new SubnetArgs
		{
			Name = "Garden",
			ResourceGroupName = _ResourceGroup.Name,
			AddressPrefixes = new[] { "10.0.2.0/25" }, //128 addresses
			VirtualNetworkName = vnet.Name
		}, new CustomResourceOptions
		{
			Protect = true
		});

		var subnetFirewall = new Subnet("AzureFirewallSubnet", new SubnetArgs
		{
			Name = "AzureFirewallSubnet",
			ResourceGroupName = _ResourceGroup.Name,
			AddressPrefixes = new[] { "10.0.2.128/25" }, //128 addresses
			VirtualNetworkName = vnet.Name
		}, new CustomResourceOptions
		{
			Protect = true
		});

		var subnetAppGtwy= new Subnet("AppGtwySubnet", new SubnetArgs
		{
			Name = "AppGtwySubnet",
			ResourceGroupName = _ResourceGroup.Name,
			AddressPrefixes = new[] { "10.0.3.0/25" }, //128 addresses
			VirtualNetworkName = vnet.Name
		}, new CustomResourceOptions
		{
			Protect = true
		});

		_Subnets.Add("Suppliers", subnetSuppliers);
		_Subnets.Add("Food", subnetFood);
		_Subnets.Add("Clothing", subnetClothing);
		_Subnets.Add("Farming", subnetFarming);
		_Subnets.Add("Garden", subnetGarden);
		_Subnets.Add("AzureFirewallSubnet", subnetFirewall);
		_Subnets.Add("AppGtwySubnet", subnetAppGtwy);

		return vnet;
	}

	private void BuildSupplierVMs(int countOfVMs)
	{
		if (countOfVMs < 1) return;

		for (int i = 0; i < countOfVMs; i++)
		{
			BuildVM(i, "Suppliers", "vmSupplier", false);
		}
	}

	private void BuildFoodVMs(int countOfVMs)
	{
		var availSet = BuildAvailabilitySet(false);

		if (countOfVMs < 1) return;

		for (int i = 0; i < countOfVMs; i++)
		{
			BuildVM(i, "Food", "vmFood", availSet, true, false);
		}
	}

	private void BuildClothingVMs(int countOfVMs)
	{
		if (countOfVMs < 1) return;

		for (int i = 0; i < countOfVMs; i++)
		{
			BuildVM(i, "Clothing", "vmClothing", false);
		}
	}

	private void BuildGardenVMs(int countOfVMs)
	{
		if (countOfVMs < 1) return;

		for (int i = 0; i < countOfVMs; i++)
		{
			BuildVM(i, "Garden", "vmGarden", false);
		}
	}

	private void BuildFarmingVMs(int countOfVMs)
	{
		if (countOfVMs < 1) return;

		for (int i = 0; i < countOfVMs; i++)
		{
			BuildVM(i, "Farming", "vmFarming", false);
		}
	}

	private AvailabilitySet BuildAvailabilitySet(bool protect)
	{
		var _ = new AvailabilitySet("foodAvailabilitySet", new AvailabilitySetArgs
		{
			ResourceGroupName = _ResourceGroup.Name,
			Location = _ResourceGroup.Location,
			Name = "foodAvailabilitySet"
		}, new CustomResourceOptions
		{
			Protect = protect
		});

		return _;
	}

	private void BuildVM(int index, string subnetName, string vmPrefix, bool protect = false)
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
		}, new CustomResourceOptions
		{
			Protect = protect
		});

		new VirtualMachine(vmName, new VirtualMachineArgs
		{
			ResourceGroupName = _ResourceGroup.Name,
			Location = _ResourceGroup.Location,
			Name = vmName,
			VmSize = "Standard_B1ms",
			NetworkInterfaceIds = new[] { nic.Id },
			BootDiagnostics = new VirtualMachineBootDiagnosticsArgs
			{
				Enabled = true,
				StorageUri = _stgPrimaryEndpoint
			},
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

		}, new CustomResourceOptions
		{
			Protect = protect
		});
	}

	private void BuildVM(int index, string subnetName, string vmPrefix, AvailabilitySet availabilitySet, bool track = false, bool protect = false)
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
		}, new CustomResourceOptions
		{
			Protect = protect
		});

		if (track)
		{
			_foodVmNics.Add(nic);
		}

		var vm = new VirtualMachine(vmName, new VirtualMachineArgs
		{
			ResourceGroupName = _ResourceGroup.Name,
			Location = _ResourceGroup.Location,
			Name = vmName,
			VmSize = "Standard_B1ms",
			NetworkInterfaceIds = new[] { nic.Id },
			AvailabilitySetId = availabilitySet.Id,
			BootDiagnostics = new VirtualMachineBootDiagnosticsArgs
			{
				Enabled= true,
				StorageUri = _stgPrimaryEndpoint
			},
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

		}, new CustomResourceOptions
		{
			Protect = protect
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
			SourceAddressPrefixes = new[] { "10.0.0.128/25" },
			DestinationAddressPrefixes = new[] { "10.0.0.0/25" },
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

		//var rule3 = new NetworkSecurityGroupSecurityRuleArgs
		//{
		// Name = "AllOtherTrafficToSuppliers",
		// Access = "Deny", //Deny, Allow
		// Direction = "Inbound", //Outbound
		// Priority = 220,
		// Protocol = "TCP", //UDP,TCP,ICMP
		// SourcePortRange = "*",  // *== Any
		// DestinationPortRange = "*",
		// SourceAddressPrefixes = new[] { "INTERNET" },
		// DestinationAddressPrefixes = new[] { "10.0.0.0/25" },
		// Description = "Block access to suppliers by everyone else."
		//};

		var securityRules = new List<NetworkSecurityGroupSecurityRuleArgs> { rule1, rule2 };

		var nsg = new NetworkSecurityGroup("nsg", new NetworkSecurityGroupArgs
		{
			Location = _ResourceGroup.Location,
			ResourceGroupName = _ResourceGroup.Name,
			Name = "nsg",
			SecurityRules = securityRules
		}, new CustomResourceOptions
		{
			Protect = true
		});

		new SubnetNetworkSecurityGroupAssociation("assc1", new SubnetNetworkSecurityGroupAssociationArgs
		{
			NetworkSecurityGroupId = nsg.Id,
			SubnetId = _Subnets["Food"].Id
		}, new CustomResourceOptions
		{
			Protect = true
		});

		new SubnetNetworkSecurityGroupAssociation("assc2", new SubnetNetworkSecurityGroupAssociationArgs
		{
			NetworkSecurityGroupId = nsg.Id,
			SubnetId = _Subnets["Clothing"].Id
		}, new CustomResourceOptions
		{
			Protect = true
		});

		new SubnetNetworkSecurityGroupAssociation("assc3", new SubnetNetworkSecurityGroupAssociationArgs
		{
			NetworkSecurityGroupId = nsg.Id,
			SubnetId = _Subnets["Farming"].Id
		}, new CustomResourceOptions
		{
			Protect = true
		});

		new SubnetNetworkSecurityGroupAssociation("assc4", new SubnetNetworkSecurityGroupAssociationArgs
		{
			NetworkSecurityGroupId = nsg.Id,
			SubnetId = _Subnets["Garden"].Id
		}, new CustomResourceOptions
		{
			Protect = true
		});

		new SubnetNetworkSecurityGroupAssociation("assc5", new SubnetNetworkSecurityGroupAssociationArgs
		{
			NetworkSecurityGroupId = nsg.Id,
			SubnetId = _Subnets["Suppliers"].Id
		}, new CustomResourceOptions
		{
			Protect = true
		});
	}

	private void BuildFoodLoadBalancer()
	{
		var frontEndConfig = new LoadBalancerFrontendIpConfigurationArgs
		{
			Name = "frontendconfig",
			SubnetId = _Subnets["Food"].Id,
			PrivateIpAddressAllocation = "Dynamic",
			PrivateIpAddressVersion = "IPv4"
		};

		var lb = new LoadBalancer("foodLb", new LoadBalancerArgs
		{
			ResourceGroupName = _ResourceGroup.Name,
			Location = _ResourceGroup.Location,
			Name = "foodLb",
			Sku = "Basic",  // Standard
			FrontendIpConfigurations = frontEndConfig
		});

		var healthProbe1 = new FrontdoorBackendPoolHealthProbeArgs
		{
			Name = "Probe1",
			Enabled = true,
			IntervalInSeconds = 5,
			Protocol = "TCP"
		};

		var backendpool = new BackendAddressPool("Pool1", new BackendAddressPoolArgs
		{
			Name = "Pool1",
			ResourceGroupName = _ResourceGroup.Name,
			LoadbalancerId = lb.Id
		});

		new NetworkInterfaceBackendAddressPoolAssociation("association1",
			new NetworkInterfaceBackendAddressPoolAssociationArgs
			{
				BackendAddressPoolId = backendpool.Id,
				NetworkInterfaceId = _foodVmNics[0].Id,
				IpConfigurationName = "Ipconfig1"
			});

		new NetworkInterfaceBackendAddressPoolAssociation("association2",
			new NetworkInterfaceBackendAddressPoolAssociationArgs
			{
				BackendAddressPoolId = backendpool.Id,
				NetworkInterfaceId = _foodVmNics[1].Id,
				IpConfigurationName = "Ipconfig1"
			});

		var lbRule = new Rule("rule1", new RuleArgs
		{
			Name = "rule1",
			ResourceGroupName = _ResourceGroup.Name,
			LoadbalancerId = lb.Id,
			Protocol = "TCP",
			FrontendPort = 80,
			BackendPort = 80,
			FrontendIpConfigurationName = frontEndConfig.Name,
			ProbeId = healthProbe1.Id,
			BackendAddressPoolId = backendpool.Id
		});
	}

	private void BuildRouteTable()
	{
		var route1 = new RouteTableRouteArgs
		{
			Name = "InternetBound",
			AddressPrefix = "0.0.0.0/0",
			NextHopInIpAddress = "10.0.2.132",  //pretend firewall address
			NextHopType = "VirtualAppliance"
		};

		var rt = new RouteTable("routeTable1", new RouteTableArgs
		{
			Name = "routeTable1",
			ResourceGroupName = _ResourceGroup.Name,
			Location = _ResourceGroup.Location,
			Routes = new InputList<RouteTableRouteArgs> { route1 }
		});

		new SubnetRouteTableAssociation("assoc1", new SubnetRouteTableAssociationArgs
		{
			SubnetId = _Subnets["Food"].Id,
			RouteTableId = rt.Id
		});

		new SubnetRouteTableAssociation("assoc2", new SubnetRouteTableAssociationArgs
		{
			SubnetId = _Subnets["Farming"].Id,
			RouteTableId = rt.Id
		});

		new SubnetRouteTableAssociation("assoc3", new SubnetRouteTableAssociationArgs
		{
			SubnetId = _Subnets["Garden"].Id,
			RouteTableId = rt.Id
		});

		new SubnetRouteTableAssociation("assoc4", new SubnetRouteTableAssociationArgs
		{
			SubnetId = _Subnets["Suppliers"].Id,
			RouteTableId = rt.Id
		});

		new SubnetRouteTableAssociation("assoc5", new SubnetRouteTableAssociationArgs
		{
			SubnetId = _Subnets["Clothing"].Id,
			RouteTableId = rt.Id
		});
	}

	private void BuildFirewall()
	{
		var firewallIpConfig = new FirewallIpConfigurationArgs
		{
			SubnetId = _Subnets["AzureFirewallSubnet"].Id,
			Name = "Foo",
			PublicIpAddressId = new PublicIp("firewallPip", new PublicIpArgs
			{
				ResourceGroupName = _ResourceGroup.Name,
				Location = _ResourceGroup.Location,
				Sku = "Standard",
				Name = "firewallPip",
				AllocationMethod = "Static",
				IpVersion = "IPv4"
			}).Id
		};

		var fw = new Firewall("snowman", new FirewallArgs
		{
			Name = "snowman",
			ResourceGroupName = _ResourceGroup.Name,
			Location = _ResourceGroup.Location,
			ThreatIntelMode = "Off",
			IpConfigurations = new List<FirewallIpConfigurationArgs> { firewallIpConfig }
		});
	}

	private void BuildAppGateway()
	{
		var examplePublicIp = new PublicIp("examplePublicIp", new PublicIpArgs
		{
			ResourceGroupName = _ResourceGroup.Name,
			Location = _ResourceGroup.Location,
			AllocationMethod = "Dynamic",
		});
		var backendAddressPoolName = _vnet.Name.Apply(name => $"{name}-beap");
		var frontendPortName = _vnet.Name.Apply(name => $"{name}-feport");
		var frontendIpConfigurationName = _vnet.Name.Apply(name => $"{name}-feip");
		var httpSettingName = _vnet.Name.Apply(name => $"{name}-be-htst");
		var listenerName = _vnet.Name.Apply(name => $"{name}-httplstn");
		var requestRoutingRuleName = _vnet.Name.Apply(name => $"{name}-rqrt");
		var redirectConfigurationName = _vnet.Name.Apply(name => $"{name}-rdrcfg");

		var appGtwy = new ApplicationGateway("appGtwy1", new ApplicationGatewayArgs
		{
			Name = "appGtwy1",
			ResourceGroupName = _ResourceGroup.Name,
			Location = _ResourceGroup.Location,
			Sku = new ApplicationGatewaySkuArgs
			{
				Name = "Standard_Small",
				Tier = "Standard",
				Capacity = 2,
			},
			GatewayIpConfigurations =
			{
				new ApplicationGatewayGatewayIpConfigurationArgs
				{
					Name = "my-gateway-ip-configuration",
					SubnetId = _Subnets["AppGtwySubnet"].Id,
				},
			},
			FrontendPorts =
			{
				new ApplicationGatewayFrontendPortArgs
				{
					Name = frontendPortName,
					Port = 80,
				},
			},
			FrontendIpConfigurations =
			{
				new ApplicationGatewayFrontendIpConfigurationArgs
				{
					Name = frontendIpConfigurationName,
					PublicIpAddressId = examplePublicIp.Id,
				},
			},
			BackendAddressPools =
			{
				new ApplicationGatewayBackendAddressPoolArgs
				{
					Name = backendAddressPoolName,
				},
			},
			BackendHttpSettings =
			{
				new ApplicationGatewayBackendHttpSettingArgs
				{
					Name = httpSettingName,
					CookieBasedAffinity = "Disabled",
					Path = "/path1/",
					Port = 80,
					Protocol = "Http",
					RequestTimeout = 60,
				},
			},
			HttpListeners =
			{
				new ApplicationGatewayHttpListenerArgs
				{
					Name = listenerName,
					FrontendIpConfigurationName = frontendIpConfigurationName,
					FrontendPortName = frontendPortName,
					Protocol = "Http",
				},
			},
			RequestRoutingRules =
			{
				new ApplicationGatewayRequestRoutingRuleArgs
				{
					Name = requestRoutingRuleName,
					RuleType = "Basic",
					HttpListenerName = listenerName,
					BackendAddressPoolName = backendAddressPoolName,
					BackendHttpSettingsName = httpSettingName,
				},
			},
		});
	}

	
}
