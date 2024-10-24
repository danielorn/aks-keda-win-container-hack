TODO: Instructions for part 3, additional features

# Container Insights

## Part 3 - Description

- Learn about container insights and how to access it
- Learn about kusto query language
- Query logs from all containepodsrs in a namespace
- Group logs by pod
- Count the number of logged errors for each pod
- Filter the result to show only pods that logged errors

## What is container insights

[Container Insights](https://learn.microsoft.com/en-us/azure/azure-monitor/containers/container-insights-overview) is a feature of Azure Monitor that provides comprehensive monitoring of containerized applications. It collects performance metrics, inventory data, health state information and application level logs from container hosts and containers, which is then available for analysis in Azure Monitor Logs.

## Query logs from your containers
KQL, or Kusto Query Language, is a powerful language used to query and analyze large datasets in for example Azure Monitor Logs. It’s designed to be easy to read and write, with a syntax similar to SQL.

All output to STDOUT and STDERR from containers in the AKS cluster is sent to a log analytics workspace from where they can be queried using KQL.

To query logs for containers, navigate to the AKS instance in the Azure portal and click "Monitoring ->Logs" in the left hand side menu

### List all logs from containers in your namespace

this query returns all records from the ContainerLogV2 table where the PodNamespace is equal to [prefix]. To make this query functional, you would replace [prefix] with the actual namespace value you’re looking for.

```kql
ContainerLogV2
| where PodNamespace == '[prefix]'
```

#### Detailed query breakdown

- **ContainerLogV2**

  This is the name of the table being queried. It  contains log data for containers, with various columns including PodNamespace, TimeGenerated, and LogMessage.

- **| where PodNamespace == ‘[prefix]’**

  Filters the rows to include only those where the PodNamespace column matches the specified [prefix].

### Group the log entries by container

- Use the kusto operator [summarize](https://learn.microsoft.com/en-us/kusto/query/summarize-operator?view=microsoft-fabric) to aggregate the content of the table based on `PodName`

```kql
ContainerLogV2
| where PodNamespace == '[prefix]'
| extend LogMsg = pack('TimeGenerated', TimeGenerated, 'LogLevel', LogLevel, 'LogMessage', LogMessage)
| summarize dateTime= min(TimeGenerated), 
    Logs = make_list(LogMsg, 1000) by PodName 
```

#### Detailed query breakdown

- **| extend LogMsg = pack(‘TimeGenerated’, TimeGenerated, 'LogLevel', LogLevel, ‘LogMessage’, LogMessage)**
   Creates a new column LogMsg which is a dynamic object containing the TimeGenerated LogLevel and LogMessage fields.

- **| summarize dateTime= min(TimeGenerated), Logs = make_list(LogMsg, 1000) by PodName**

  Groups the data by PodName.
  - For each group, calculates dateTime as the minimum value of TimeGenerated, representing the earliest log entry for each pod.
  - Creates Logs as a list of up to 1000 LogMsg objects for each pod.

### Count the number of errors for each container

```kql
ContainerLogV2
| where PodNamespace == '[prefix]'
| extend LogMsg = pack('TimeGenerated', TimeGenerated, 'LogLevel', LogLevel, 'LogMessage', LogMessage)
| extend ErrorCount = case(indexof(tostring(LogMessage), "ERROR:") != -1, 1, 0)
| summarize dateTime= min(TimeGenerated), 
    TotalErrors = sum(ErrorCount), 
    Logs = make_list(LogMsg, 1000) by PodName
```

#### Detailed query breakdown
- **| extend ErrorCount = case(indexof(tostring(LogMessage), “ERROR:”) != -1, 1, 0)**
  
  Creates a new column ErrorCount which is set to 1 if the LogMessage contains the substring “ERROR:”, and 0 otherwise.

- **| summarize dateTime= min(TimeGenerated), Logs = make_list(LogMsg, 1000), TotalErrors = sum(ErrorCount) by PodName**

  Groups the data by PodName.
  - For each group, calculates dateTime as the minimum value of TimeGenerated, representing the earliest log entry for each pod.
  - Creates Logs as a list of up to 1000 LogMsg objects for each pod.
  - Calculates TotalErrors as the sum of ErrorCount for each pod.

### Filter the result to display only containers that have errors
```kql
ContainerLogV2
| where PodNamespace == '[prefix]'
| extend LogMsg = pack('TimeGenerated', TimeGenerated, 'LogMessage', LogMessage)
| extend ErrorCount = case(indexof(tostring(LogMessage), "ERROR:") != -1, 1, 0)
| summarize dateTime= min(TimeGenerated), 
    Logs = make_list(LogMsg, 1000), 
    TotalErrors = sum(ErrorCount) by PodName
| where TotalErrors > 0
```

#### Detailed query breakdown
- **| where TotalErrors >0**
  
  Filter result to show only rows where TotalErrors are larger than 0 (i.e only pods that logged errors)

### More log examples

Please see: https://learn.microsoft.com/en-us/azure/azure-monitor/containers/container-insights-log-query#container-logs

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
az role assignment create --role "Key Vault Secrets Officer" --assignee [id] --scope /subscriptions/[subscriptionId]/resourceGroups/[resourcegroup]/providers/Microsoft.KeyVault/vaults/[KeyVaultName]
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
az role assignment create --role "Key Vault Secrets User" --assignee [clientId] --scope /subscriptions/[subscriptionId]/resourceGroups/[resourcegroup]/providers/Microsoft.KeyVault/vaults/[KeyVaultName]
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
  refreshInterval: 1m
  secretStoreRef:
    name: azure-store
    kind: SecretStore
  target:
    name: [prefix]-secret #secret name in k8s 
    creationPolicy: Owner
  data:
  - secretKey: [prefix]-secret # secret key name in k8s
    remoteRef:
      key: [prefix]-secret #secret name in akv
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

Use External secrets for part 2. 
Delete existing secret called "servicebus-secret" in your namespace. 
Create a secret in Azure keyvault 

```shell
az keyvault secret set --vault-name [KeyVaultName] --name [prefix]-secret-sb --value [your servicebus connectionstring]
```

Create an External Secret and verify that it's being synced. 

```yaml
apiVersion: external-secrets.io/v1beta1
kind: ExternalSecret
metadata:
  name: [prefix]-external-secret-servicebus
spec:
  refreshInterval: 1m
  secretStoreRef:
    name: azure-store
    kind: SecretStore
  target:
    name: servicebus-secret #secret name in k8s 
    creationPolicy: Owner
  data:
  - secretKey: ConnectionString # secret key name in k8s
    remoteRef:
      key: [prefix]-secret-sb #secret name in akv
---
```

```shell
kubectl apply -f deploy.yaml
```

# Deploy a web app 


## Create a pod and expose a service

Create a pod and expose it using a service with type ClusterIP. 

```yaml
apiVersion: v1
kind: Pod
metadata:
  name: hello-kubernetes-pod
  labels:
    app: hello-kubernetes
spec:
  containers:
  - name: hello-kubernetes
    image: paulbouwer/hello-kubernetes:1.10
    env:
      - name: MESSAGE 
        value: "This is only a pod!"
      - name: KUBERNETES_NAMESPACE
        valueFrom:
          fieldRef:
            fieldPath: metadata.namespace
      - name: KUBERNETES_POD_NAME
        valueFrom:
          fieldRef:
            fieldPath: metadata.name
      - name: KUBERNETES_NODE_NAME
        valueFrom:
          fieldRef:
            fieldPath: spec.nodeName
    ports:
    - containerPort: 8080
  nodeSelector:
    kubernetes.io/os: linux
---
apiVersion: v1
kind: Service
metadata:
  name: hello-kubernetes-svc
  annotations:
    service.beta.kubernetes.io/azure-load-balancer-internal: "true"
spec:
  type: ClusterIP
  ports:
  - protocol: TCP
    port: 80
    targetPort: 8080
  selector:
    app: hello-kubernetes
```

Check the service IP address using OpenLens or kubectl. 
```shell
kubectl apply -f deploy.yaml
kubectl get svc
kubectl describe svc hello-kubernetes-svc 
```
Try to navigate to the IP from a browser. 

Change the service type to LoadBalancer instead of ClusterIP, save the yaml file and deploy. 

Check the service IP address using OpenLens or kubectl.

```shell
kubectl apply -f deploy.yaml
kubectl describe svc hello-kubernetes-svc 
```

Try to navigate to the IP from a browser. 

Delete the pod 
```shell
kubectl delete pod hello-kubernetes-pod
```
## Create a deployment

Create a deployment, deploy it.  

```yaml 
apiVersion: apps/v1
kind: Deployment
metadata:
  name: hello-kubernetes-deployment
spec:
  replicas: 1
  selector:
    matchLabels:
      app: hello-kubernetes
  template:
    metadata:
      labels:
        app: hello-kubernetes
    spec:
      nodeSelector:
          kubernetes.io/os: linux
      containers:
      - name: hello-kubernetes
        image: paulbouwer/hello-kubernetes:1.10
        env:
        - name: MESSAGE 
          value: "Nice message :-) "
        - name: KUBERNETES_NAMESPACE
          valueFrom:
            fieldRef:
              fieldPath: metadata.namespace
        - name: KUBERNETES_POD_NAME
          valueFrom:
            fieldRef:
              fieldPath: metadata.name
        - name: KUBERNETES_NODE_NAME
          valueFrom:
            fieldRef:
              fieldPath: spec.nodeName
        ports:
        - containerPort: 8080
```

```shell
kubectl apply -f deploy.yaml
kubectl get pods -o wide
```

Check the number of ready pods and which node it is running on. 

Deploy the pod using OpenLens or kubectl, what happens?
Check the nodename. 

## Scale the app to multiple replicas

Change value replicas to 2 instead of 1, deploy it. 

```shell
kubectl apply -f deploy.yaml
kubectl get pods -o wide
```
Browse to the IP address multiple times. 
Check the nodenames, check the logs, traffic is being distributed between the pods. 

