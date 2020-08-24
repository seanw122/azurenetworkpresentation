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

namespace pulumicode
{
	class SecondRegion
	{
		private readonly VirtualNetwork _primaryVirtualNetwork;
		private const string Location = "EastUS2";
		private ResourceGroup _ResourceGroup;
		private Dictionary<string, Subnet> _Subnets;
		private List<NetworkInterface> _foodVmNics;
		private VirtualNetwork _vnet;
		private Output<string> _stgPrimaryEndpoint;

		public SecondRegion(VirtualNetwork primaryVirtualNetwork)
		{
			_primaryVirtualNetwork = primaryVirtualNetwork;
			_Subnets = new Dictionary<string, Subnet>();
			_foodVmNics = new List<NetworkInterface>();

			// Create an Azure Resource Group
			_ResourceGroup = new ResourceGroup("rsgMarketPlaceII", new ResourceGroupArgs
			{
				Location = Location,
				Name = "rsgMarketPlaceII"
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

			BuildVNetPeering();
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
			var stg = new Account("stgBootDiag2", new AccountArgs
			{
				Name = "stgnwkpresvmbootdiag2",
				ResourceGroupName = _ResourceGroup.Name,
				Location = _ResourceGroup.Location,
				AccountKind = "StorageV2",
				AccountTier = "Standard",
				AccountReplicationType = "LRS"
			});

			return stg.PrimaryBlobEndpoint;
		}

		private VirtualNetwork BuildVNet()
		{
			var vnet = new VirtualNetwork("vnet2", new VirtualNetworkArgs
			{
				Name = "vnet2",
				Location = _ResourceGroup.Location,
				AddressSpaces = new[] { "10.1.0.0/22" }, //1024 addresses
				ResourceGroupName = _ResourceGroup.Name
			}, new CustomResourceOptions
			{
				Protect = true
			});

			var subnetSuppliers = new Subnet("Suppliers2", new SubnetArgs
			{
				Name = "Suppliers2",
				ResourceGroupName = _ResourceGroup.Name,
				AddressPrefixes = new[] { "10.1.0.0/25" }, //128 addresses
				VirtualNetworkName = vnet.Name
			}, new CustomResourceOptions
			{
				Protect = true
			});


			var subnetFood = new Subnet("Food2", new SubnetArgs
			{
				Name = "Food2",
				ResourceGroupName = _ResourceGroup.Name,
				AddressPrefixes = new[] { "10.1.0.128/25" }, //128 addresses
				VirtualNetworkName = vnet.Name
			}, new CustomResourceOptions
			{
				Protect = true
			});

			var subnetClothing = new Subnet("Clothing2", new SubnetArgs
			{
				Name = "Clothing2",
				ResourceGroupName = _ResourceGroup.Name,
				AddressPrefixes = new[] { "10.1.1.0/25" }, //128 addresses
				VirtualNetworkName = vnet.Name
			}, new CustomResourceOptions
			{
				Protect = true
			});

			var subnetFarming = new Subnet("Farming2", new SubnetArgs
			{
				Name = "Farming2",
				ResourceGroupName = _ResourceGroup.Name,
				AddressPrefixes = new[] { "10.1.1.128/25" }, //128 addresses
				VirtualNetworkName = vnet.Name
			}, new CustomResourceOptions
			{
				Protect = true
			});

			var subnetGarden = new Subnet("Garden2", new SubnetArgs
			{
				Name = "Garden2",
				ResourceGroupName = _ResourceGroup.Name,
				AddressPrefixes = new[] { "10.1.2.0/25" }, //128 addresses
				VirtualNetworkName = vnet.Name
			}, new CustomResourceOptions
			{
				Protect = true
			});

			//var subnetFirewall = new Subnet("AzureFirewallSubnet", new SubnetArgs
			//{
			//	Name = "AzureFirewallSubnet",
			//	ResourceGroupName = _ResourceGroup.Name,
			//	AddressPrefixes = new[] { "10.1.2.128/25" }, //128 addresses
			//	VirtualNetworkName = vnet.Name
			//}, new CustomResourceOptions
			//{
			//	Protect = true
			//});

			var subnetAppGtwy = new Subnet("AppGtwySubnet2", new SubnetArgs
			{
				Name = "AppGtwySubnet2",
				ResourceGroupName = _ResourceGroup.Name,
				AddressPrefixes = new[] { "10.1.3.0/25" }, //128 addresses
				VirtualNetworkName = vnet.Name
			}, new CustomResourceOptions
			{
				Protect = true
			});

			_Subnets.Add("Suppliers2", subnetSuppliers);
			_Subnets.Add("Food2", subnetFood);
			_Subnets.Add("Clothing2", subnetClothing);
			_Subnets.Add("Farming2", subnetFarming);
			_Subnets.Add("Garden2", subnetGarden);
			//_Subnets.Add("AzureFirewallSubnet", subnetFirewall);
			_Subnets.Add("AppGtwySubnet2", subnetAppGtwy);

			return vnet;
		}

		private void BuildSupplierVMs(int countOfVMs)
		{
			if (countOfVMs < 1) return;

			for (int i = 0; i < countOfVMs; i++)
			{
				BuildVM(i, "Suppliers2", "vmSupplier2", false);
			}
		}

		private void BuildFoodVMs(int countOfVMs)
		{
			var availSet = BuildAvailabilitySet(false);

			if (countOfVMs < 1) return;

			for (int i = 0; i < countOfVMs; i++)
			{
				BuildVM(i, "Food2", "vmFood2", availSet, true, false);
			}
		}

		private void BuildClothingVMs(int countOfVMs)
		{
			if (countOfVMs < 1) return;

			for (int i = 0; i < countOfVMs; i++)
			{
				BuildVM(i, "Clothing2", "vmClothing2", false);
			}
		}

		private void BuildGardenVMs(int countOfVMs)
		{
			if (countOfVMs < 1) return;

			for (int i = 0; i < countOfVMs; i++)
			{
				BuildVM(i, "Garden2", "vmGarden2", false);
			}
		}

		private void BuildFarmingVMs(int countOfVMs)
		{
			if (countOfVMs < 1) return;

			for (int i = 0; i < countOfVMs; i++)
			{
				BuildVM(i, "Farming2", "vmFarming2", false);
			}
		}

		private AvailabilitySet BuildAvailabilitySet(bool protect)
		{
			var _ = new AvailabilitySet("foodAvailabilitySet2", new AvailabilitySetArgs
			{
				ResourceGroupName = _ResourceGroup.Name,
				Location = _ResourceGroup.Location,
				Name = "foodAvailabilitySet2"
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
				SourceAddressPrefixes = new[] { "10.1.0.128/25" },
				DestinationAddressPrefixes = new[] { "10.1.0.0/25" },
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
				SourceAddressPrefixes = new[] { "10.1.0.0/25", "10.1.1.0/25", "10.1.1.128/25", "10.1.2.0/25" },
				DestinationAddressPrefixes = new[] { "10.1.0.128/25" }
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
			// DestinationAddressPrefixes = new[] { "10.1.0.0/25" },
			// Description = "Block access to suppliers by everyone else."
			//};

			var securityRules = new List<NetworkSecurityGroupSecurityRuleArgs> { rule1, rule2 };

			var nsg = new NetworkSecurityGroup("nsg2", new NetworkSecurityGroupArgs
			{
				Location = _ResourceGroup.Location,
				ResourceGroupName = _ResourceGroup.Name,
				Name = "nsg2",
				SecurityRules = securityRules
			}, new CustomResourceOptions
			{
				Protect = true
			});

			new SubnetNetworkSecurityGroupAssociation("assc12", new SubnetNetworkSecurityGroupAssociationArgs
			{
				NetworkSecurityGroupId = nsg.Id,
				SubnetId = _Subnets["Food2"].Id
			}, new CustomResourceOptions
			{
				Protect = true
			});

			new SubnetNetworkSecurityGroupAssociation("assc22", new SubnetNetworkSecurityGroupAssociationArgs
			{
				NetworkSecurityGroupId = nsg.Id,
				SubnetId = _Subnets["Clothing2"].Id
			}, new CustomResourceOptions
			{
				Protect = true
			});

			new SubnetNetworkSecurityGroupAssociation("assc32", new SubnetNetworkSecurityGroupAssociationArgs
			{
				NetworkSecurityGroupId = nsg.Id,
				SubnetId = _Subnets["Farming2"].Id
			}, new CustomResourceOptions
			{
				Protect = true
			});

			new SubnetNetworkSecurityGroupAssociation("assc42", new SubnetNetworkSecurityGroupAssociationArgs
			{
				NetworkSecurityGroupId = nsg.Id,
				SubnetId = _Subnets["Garden2"].Id
			}, new CustomResourceOptions
			{
				Protect = true
			});

			new SubnetNetworkSecurityGroupAssociation("assc52", new SubnetNetworkSecurityGroupAssociationArgs
			{
				NetworkSecurityGroupId = nsg.Id,
				SubnetId = _Subnets["Suppliers2"].Id
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
				SubnetId = _Subnets["Food2"].Id,
				PrivateIpAddressAllocation = "Dynamic",
				PrivateIpAddressVersion = "IPv4"
			};

			var lb = new LoadBalancer("foodLb2", new LoadBalancerArgs
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

			var backendpool = new BackendAddressPool("Pool12", new BackendAddressPoolArgs
			{
				Name = "Pool12",
				ResourceGroupName = _ResourceGroup.Name,
				LoadbalancerId = lb.Id
			});

			new NetworkInterfaceBackendAddressPoolAssociation("association12",
				new NetworkInterfaceBackendAddressPoolAssociationArgs
				{
					BackendAddressPoolId = backendpool.Id,
					NetworkInterfaceId = _foodVmNics[0].Id,
					IpConfigurationName = "Ipconfig1"
				});

			new NetworkInterfaceBackendAddressPoolAssociation("association22",
				new NetworkInterfaceBackendAddressPoolAssociationArgs
				{
					BackendAddressPoolId = backendpool.Id,
					NetworkInterfaceId = _foodVmNics[1].Id,
					IpConfigurationName = "Ipconfig1"
				});

			var lbRule = new Rule("rule12", new RuleArgs
			{
				Name = "rule12",
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
				NextHopInIpAddress = "10.1.2.132",  //pretend firewall address
				NextHopType = "VirtualAppliance"
			};

			var rt = new RouteTable("routeTable12", new RouteTableArgs
			{
				Name = "routeTable12",
				ResourceGroupName = _ResourceGroup.Name,
				Location = _ResourceGroup.Location,
				Routes = new InputList<RouteTableRouteArgs> { route1 }
			});

			new SubnetRouteTableAssociation("assoc12", new SubnetRouteTableAssociationArgs
			{
				SubnetId = _Subnets["Food2"].Id,
				RouteTableId = rt.Id
			});

			new SubnetRouteTableAssociation("assoc22", new SubnetRouteTableAssociationArgs
			{
				SubnetId = _Subnets["Farming2"].Id,
				RouteTableId = rt.Id
			});

			new SubnetRouteTableAssociation("assoc32", new SubnetRouteTableAssociationArgs
			{
				SubnetId = _Subnets["Garden2"].Id,
				RouteTableId = rt.Id
			});

			new SubnetRouteTableAssociation("assoc42", new SubnetRouteTableAssociationArgs
			{
				SubnetId = _Subnets["Suppliers2"].Id,
				RouteTableId = rt.Id
			});

			new SubnetRouteTableAssociation("assoc52", new SubnetRouteTableAssociationArgs
			{
				SubnetId = _Subnets["Clothing2"].Id,
				RouteTableId = rt.Id
			});
		}

		private void BuildFirewall()
		{
			var firewallIpConfig = new FirewallIpConfigurationArgs
			{
				SubnetId = _Subnets["AzureFirewallSubnet"].Id,
				Name = "Foo",
				PublicIpAddressId = new PublicIp("firewallPip2", new PublicIpArgs
				{
					ResourceGroupName = _ResourceGroup.Name,
					Location = _ResourceGroup.Location,
					Sku = "Standard",
					Name = "firewallPip2",
					AllocationMethod = "Static",
					IpVersion = "IPv4"
				}).Id
			};

			var fw = new Firewall("snowman2", new FirewallArgs
			{
				Name = "snowman2",
				ResourceGroupName = _ResourceGroup.Name,
				Location = _ResourceGroup.Location,
				ThreatIntelMode = "Off",
				IpConfigurations = new List<FirewallIpConfigurationArgs> { firewallIpConfig }
			});
		}

		private void BuildAppGateway()
		{
			var examplePublicIp = new PublicIp("examplePublicIp2", new PublicIpArgs
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

			var appGtwy = new ApplicationGateway("appGtwy12", new ApplicationGatewayArgs
			{
				Name = "appGtwy12",
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

		private void BuildVNetPeering()
		{
			//_primaryVirtualNetwork
			//_vnet

			var example_1VirtualNetworkPeering = new VirtualNetworkPeering("vnetPeerEUS_CUS", new VirtualNetworkPeeringArgs
			{
				ResourceGroupName = _ResourceGroup.Name,
				VirtualNetworkName = _vnet.Name,
				RemoteVirtualNetworkId = _primaryVirtualNetwork.Id,
				AllowForwardedTraffic = false,
				AllowGatewayTransit = false,
				UseRemoteGateways = false,
				AllowVirtualNetworkAccess = true
			});
			var example_2VirtualNetworkPeering = new VirtualNetworkPeering("vnetPeerCUS_EUS", new VirtualNetworkPeeringArgs
			{
				ResourceGroupName = "rsgMarketPlace",
				VirtualNetworkName = _primaryVirtualNetwork.Name,
				RemoteVirtualNetworkId = _vnet.Id,
				AllowForwardedTraffic = false,
				AllowGatewayTransit = false,
				UseRemoteGateways = false,
				AllowVirtualNetworkAccess = true
			});
		}
	}
}
