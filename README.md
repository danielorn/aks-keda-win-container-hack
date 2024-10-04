# Introduction

Hackathon with containerised .Net 4.8 windows application running on [AKS](https://learn.microsoft.com/en-us/azure/aks/) using [KEDA](https://keda.sh/) and [Azure Service Bus](https://learn.microsoft.com/en-us/azure/service-bus-messaging)

## Prerequisites

### Azure resources

- [Servicebus Namespace (Basic Tier)](https://learn.microsoft.com/en-us/azure/service-bus-messaging)
- [AKS cluster](https://learn.microsoft.com/en-us/azure/aks/) with a windows [nodepool](https://learn.microsoft.com/en-us/azure/aks/create-node-pools)

### Local Installation

- [Docker Desktop](https://docs.docker.com/desktop/install/windows-install/)
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli-windows)
- [ServiceBus explorer](https://github.com/paolosalvatori/ServiceBusExplorer)
- [Openlens 6.2.5](https://github.com/MuhammedKalkan/OpenLens/releases/tag/v6.2.5)

## Part 1: Containerizing the application

See instructions [here](part1/)

## Part 2: Deploy in AKS and trigger with KEDA

See instructions [here](part2/)

## Part 3: Additional AKS features

See instructions [here](part3/)

