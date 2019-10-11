
# Dedicated Hosts Manager
Azure Dedicated Host (DH) provides physical servers that host one or more Azure virtual machines; host-level isolation means that capacity is dedicated to your organization and servers are not shared with other customers. To use DH, users currently need to manage DH themselves - e.g. when to spin up or spin down Hosts, determine VM placement on Hosts, bin pack VMs compactly on Hosts to minimize Host usage and optimize for cost, or use another Host selection strategy, manage VM creation traffic burst scenarios, etc.

The Dedicated Hosts Manager library abstracts Host Management logic from users, and makes it easy for users to use DH. Users only need to specify the number and SKU of VMs that need to be allocated, and this library takes care of the rest. This library is packaged as an Azure Function that can be deployed in your subscription, and is easy to integrate with . The library is extensible and allows for customizing Host selection logic.

# Usage
1. Deploy the Dedicated Hosts Manager function in your subscription and setup the below config.
    _Application settings:_
    ```json
    {
        "name": "APPINSIGHTS_INSTRUMENTATIONKEY",
        "value": "App Insights Instrumentation Key",
    },
    {
        "name": "DhgCreateRetryCount",
        "value": "10",
    },
    {
        "name": "LockBlobPrefix",
        "value": "lock-",
    },
    {
        "name": "LockContainerName",
        "value": "dhm-sync",
    },
    {
        "name": "LockIntervalInSeconds",
        "value": "60",
    },
    {
        "name": "LockRetryCount",
        "value": "10",
    },
    {
        "name": "MaxIntervalToCheckForVmInSeconds",
        "value": "30",
    },
    {
        "name": "MinIntervalToCheckForVmInSeconds",
        "value": "20",
    },
    {
        "name": "RetryCountToCheckVmState",
        "value": "10",
    },
    {
        "name": "VmToHostMapping",
        "value": "{\"Standard_D2s_v3\":\"DSv3-Type1\",\"Standard_D4s_v3\":\"DSv3-Type1\",\"Standard_D8s_v3\":\"DSv3-Type1\",\"Standard_D16s_v3\":\"DSv3-Type1\",\"Standard_D32-8s_v3\":\"DSv3-Type1\",\"Standard_D32-16s_v3\":\"DSv3-Type1\",\"Standard_D32s_v3\":\"DSv3-Type1\",\"Standard_D48s_v3\":\"DSv3-Type1\",\"Standard_D64-16s_v3\":\"DSv3-Type1\",\"Standard_D64-32s_v3\":\"DSv3-Type1\",\"Standard_D64s_v3\":\"DSv3-Type1\",\"Standard_E2s_v3\":\"ESv3-Type1\",\"Standard_E4s_v3\":\"ESv3-Type1\",\"Standard_E8s_v3\":\"ESv3-Type1\",\"Standard_E16s_v3\":\"ESv3-Type1\",\"Standard_E32-8s_v3\":\"ESv3-Type1\",\"Standard_E32-16s_v3\":\"ESv3-Type1\",\"Standard_E32s_v3\":\"ESv3-Type1\",\"Standard_E48s_v3\":\"ESv3-Type1\",\"Standard_E64-16s_v3\":\"ESv3-Type1\",\"Standard_E64-32s_v3\":\"ESv3-Type1\",\"Standard_E64s_v3\":\"ESv3-Type1\",\"Standard_F2s_v3\":\"FSv2-Type2\",\"Standard_F4s_v3\":\"FSv2-Type2\",\"Standard_F8s_v3\":\"FSv2-Type2\",\"Standard_F16s_v3\":\"FSv2-Type2\",\"Standard_F32-8s_v3\":\"FSv2-Type2\",\"Standard_F32-16s_v3\":\"FSv2-Type2\",\"Standard_F32s_v3\":\"FSv2-Type2\",\"Standard_F48s_v3\":\"FSv2-Type2\",\"Standard_F64-16s_v3\":\"FSv2-Type2\",\"Standard_F64-32s_v3\":\"FSv2-Type2\",\"Standard_F64s_v3\":\"FSv2-Type2\"}",
    }

    ```
    _Connection strings:_
    ```json
    {
        "name": "StorageConnectionString",
        "value": "Storage connection string",
    }
    ```

2. Deploy the Dedicated Host Manager Test function in your subscription with the below config.

    _Application settings:_
    ```json
    {
        "name": "AuthEndpoint",
        "value": "https://login.microsoftonline.us/",
    },
    {
        "name": "AzureRmEndpoint",
        "value": "https://management.usgovcloudapi.net/",
    },
    {
        "name": "ClientId",
        "value": "Client ID from AAD service principal",
    },
    {
        "name": "FairfaxClientSecret",
        "value": "Client secret from AAD service principal",
    },
    {
        "name": "Location",
        "value": "usgovvirginia",
    },
    {
        "name": "SubscriptionId",
        "value": "Your subscription id",
    },
    {
        "name": "TenantId",
        "value": "Your tenant id",
    }
    ```

3. Run the test Function to provision VMs on Dedicated Hosts


# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
