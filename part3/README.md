TODO: Instructions for part 3, additional features

## Container Insights

# Secret Management

## Part 4 - Description

- Grant access to yourself to add secrets in Azure Key Vault by assigning the "Key Vault Secrets Officer" role to your user account.

- Create a secret in Azure Key Vault using the Azure CLI.

- Create a User Assigned Managed Identity, give it access to get secrets from the Key Vault, and create a federated credential for Kubernetes service account access.

- Create a service account in Kubernetes to be used by the pod, and annotate it with the client ID and tenant ID of the User Managed Identity.

- Create a Secret Store and an External Secret in Kubernetes to reference the Azure Key Vault and sync secrets.

- Deploy a pod that uses the secret and write out the secret in the logs. Verify the logs using command line or OpenLens.


## Grant access to Azure keyvault 
Use existing Azure keyvault provided by hackathon. 

Grant access to yourself to add secrets in Azure keyvault by adding you as a "Key Vault Secrets Officer" role. 

```shell
az ad signed-in-user show --query id --output tsv
# Get the id from the query above
az role assignment create --role "Key Vault Secrets Officer" --assignee [id] --scope /subscriptions/[subscriptionId]/resourceGroups/rg-[prefix]/providers/Microsoft.KeyVault/vaults/[KeyVaultName]
```
Create secret in Azure Keyvault
```shell
az keyvault secret set --vault-name [KeyVaultName] --name [prefix]-secret --value [SecretValue]
```

## Create a User Assigned Managed Identity used to access the keyvault

Create a resourcegroup and a User Managed Identity, give it access to the User Managed Identity to get secrets from the keyvault.
```shell
az group create -n rg-[prefix] -l swedencentral
az identity create --name umi-[prefix] --resource-group rg-[prefix] --location swedencentral
az identity show --name umi-[prefix] --resource-group rg-[prefix] --query clientId --output tsv

# Get the clienId from the query above
az role assignment create --role "Key Vault Secrets User" --assignee [clientId] --scope /subscriptions/[subscriptionId]/resourceGroups/rg-[prefix]/providers/Microsoft.KeyVault/vaults/[KeyVaultName]
```

Create a federated credential that is used by k8s service account to access the keyvault. Use the portal ( User Managed Identity --> Federated credentials --> Add Credential --> Kubernetes Accessing Azure Resources) or use the CLI. 

- Cluster Issuer URL: 
```shell
az aks show --name [aks-cluster name] -g [aks-cluster resourcegroup] --query "oidcIssuerProfile.issuerUrl" --output tsv 
```
- Namespace:  [prefix]
- Service Account:  svc-[prefix] 
- Name:  fc-[prefix]

```shell
az identity federated-credential create --name fc-[prefix] --identity-name umi-[prefix] -g rg-[prefix] --issuer [Cluster Issuer URL] --subject system:serviceaccount:[prefix]:svc-[prefix] --audiences api://AzureADTokenExchange
```

To learn more about Workload identities in AKS see this [link](https://learn.microsoft.com/en-us/azure/aks/workload-identity-overview?tabs=dotnet). 


## Create a service account to be used by the pod

Get the clientid and tenantid from the User Managed Identity either through the Azure Portal or CLI. Continue to use the deploy.yaml file. 

```yaml
az identity show --name umi-[prefix] -g rg-[prefix] --query "{clientId:clientId,tenantId:tenantId}" --output tsv
```

Add this to deploy.yaml.

```yaml
apiVersion: v1
kind: ServiceAccount
metadata:
  name: svc-[prefix]
  annotations:
    azure.workload.identity/client-id: [clientId]
    azure.workload.identity/tenant-id: [tenantId]
---
```
Apply the yaml and verify that the service account has been created. Use commandline or OpenLens.    

```shell
kubectl apply -f deploy.yaml
kubectl get sa
```

## Create a Secret store and a secret used by External Secrets 

A Secret store is a reference to the Azure Keyvault to be used.


```yaml
apiVersion: external-secrets.io/v1beta1
kind: SecretStore
metadata:
  name: azure-store
spec:
  provider:
    azurekv:
      authType: WorkloadIdentity
      vaultUrl: "https://[KeyVaultName].vault.azure.net"
      serviceAccountRef:
        name: svc-[prefix]

---

apiVersion: external-secrets.io/v1beta1
kind: ExternalSecret
metadata:
  name: [prefix]-external-secret
spec:
  secretStoreRef:
    name: azure-store
    kind: SecretStore
  target:
    name: [prefix]-secret #secret name in k8s 
    creationPolicy: Owner
  data:
  - secretKey: [prefix]-secret #secret name in akv
    remoteRef:
      key: [prefix]-secret 
---
```

Apply the yaml and verify that the secret has been created. Use commandline or OpenLens.    

```shell
kubectl apply -f deploy.yaml
kubectl get secret
kubectl describe secret [prefix]-secret
```

## Use the secret in a Deployment 

Deploy a pod and write out the secret in the logs. 

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: secret-app
spec:
  replicas: 1
  selector:
    matchLabels:
      app: secret-app
  template:
    metadata:
      labels:
        app: secret-app
    spec:
      nodeSelector:
          kubernetes.io/os: linux
      containers:
      - name: busybox
        image: busybox
        env:
        - name: MYSECRET
          valueFrom:
            secretKeyRef:
              name: [prefix]-secret
              key: [prefix]-secret
        command: ["/bin/sh"]
        args: ["-c", "echo $MYSECRET && sleep 3600"]
```

Check the logs using commandline or OpenLens. 

```shell
kubectl get pods
kubectl logs [podname]
```

