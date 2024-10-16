# Introduction

Hackathon with containerised .Net 4.8 windows application running on [AKS](https://learn.microsoft.com/en-us/azure/aks/) using [KEDA](https://keda.sh/) and [Azure Service Bus](https://learn.microsoft.com/en-us/azure/service-bus-messaging)

## Prerequisites

### Azure resources

- [Servicebus Namespace (Basic Tier)](https://learn.microsoft.com/en-us/azure/service-bus-messaging)
- [AKS cluster](https://learn.microsoft.com/en-us/azure/aks/) with a windows [nodepool](https://learn.microsoft.com/en-us/azure/aks/create-node-pools)

### Local Installations and preparations

#### Install the following tools

- [Docker Desktop](https://docs.docker.com/desktop/install/windows-install/)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-windows)
- [ServiceBus explorer](https://github.com/paolosalvatori/ServiceBusExplorer)
- [Openlens 6.2.5](https://github.com/MuhammedKalkan/OpenLens/releases/tag/v6.2.5)

#### Install kubectl and kubelogin
Use the [Azure CLI to install kubectl and kubelogin](https://learn.microsoft.com/en-us/cli/azure/aks?view=azure-cli-latest#az-aks-install-cli)

```sh
az aks install-cli
```

#### Pull docker images

1. Make sure that [the container type is set to Windows containers](https://learn.microsoft.com/en-us/virtualization/windowscontainers/quick-start/set-up-environment?tabs=dockerce#windows-10-and-11-1) in Docker Desktop
2. Pull the base images we will use during the lab by opening a terminal and run the following commands. This will download and cache the base images used in the hackathon

- `docker pull mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2019`
- `docker pull mcr.microsoft.com/dotnet/framework/runtime:4.8-windowsservercore-ltsc2019`


## Part 1: Containerizing the application

See instructions [here](part1/)

## Part 2: Deploy in AKS and trigger with KEDA

See instructions [here](part2/)

## Part 3: Additional AKS features

See instructions [here](part3/)

