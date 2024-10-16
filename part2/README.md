# Deploy ScaledJobs in AKS utilizing KEDA and process messages from Servicebus queues

## Part 2 - Description

- Understand kubernetes architecture, learn about Kubernetes components, namespaces, YAML, KEDA, secrets, deployments, jobs, and scaled jobs.

- Install and use tools like Kubectl, OpenLens, and Kubectx & Kubens for managing Kubernetes.

- Connect to AKS Cluster, use Azure CLI to log in, set the subscription, and get credentials for the AKS cluster. Verify the connection by listing existing namespaces.

- Push Docker Image to Azure Container Registry, build and push your Docker image to the Azure Container Registry.

- Create Namespace in AKS, create a deploy.yaml file with the namespace definition. Apply the YAML file and switch to the new namespace.

- Create a Secret in AKS, obtain the Servicebus connection string and create a secret in the deploy.yaml file. Apply the YAML file and verify the secret creation.

- Create TriggerAuthentication and ScaledJob, add TriggerAuthentication and ScaledJob definitions to the deploy.yaml file. Apply the YAML file to create the resources.

- Test the Solution, add a message to the ServiceBus queue and monitor the job processing using OpenLens or Kubectl.


All commands should be run from the root of the repo if not stated else.  

## Understand k8s architecture

[INSERT PICTURE]

## Tooling for k8s

- [Kubectl](https://kubernetes.io/docs/tasks/tools/#kubectl)
 Commandline tool for interacting with k8s.
- [OpenLens](https://github.com/MuhammedKalkan/OpenLens/releases/tag/v6.2.5) Visual tool for working with k8s.

- [Kubectx & Kubens](https://github.com/ahmetb/kubectx) Commandline tool for interacting with k8s.

- Create a non-persistent alias in Powershell for kubectl 
```shell
Set-Alias -Name k -Value kubectl
```

- Create a non-persistent alias in shell for kubectl 
```shell
alias k='kubectl'
```

- Create a persistent alias in powershell/shell for kubectl, ask GitHub Copilot
```shell
how to create alias in [bash | powershell] for kubectl
```

## Connect to AKS cluster

Use Azure CLI to connect to the AKS Cluster, this creates a .kube folder and a config file that is being used to connect to AKS. 

```shell
az login

az account set --subscription [subscriptionId]

az aks get-credentials --resource-group [resourcegroup name] --name [AKS cluster name] --overwrite-existing

kubelogin convert-kubeconfig -l azurecli
```

Verify connection to AKS by list existing namespaces
```shell
kubectl get namespace
```


## Push docker image to Azure Container Registry

Login to Azure Container Registry and push the docker image. 

```shell
az acr login -n [Acr name]

docker build -t "[Acr name].azurecr.io/[prefix]/eventproxy:1.0" src/.

docker push "[Acr name].azurecr.io/[prefix]/eventproxy:1.0"
```

## Create namespace in AKS

Create a file called deploy.yaml in the root of the repo. Insert YAML snippet in the file and save. 

```yaml
apiVersion: v1
kind: Namespace
metadata:
  name: [prefix]
--- 
```
Apply the yaml and switch current namespace.   


```shell
kubectl apply -f deploy.yaml

kubens [prefix]
or 
kubectl config set-context --current --namespace=[prefix]
```
Verify that the namespace was created connection to AKS by list existing namespaces.

## Create secret in AKS

Putting secrets into yaml is <mark>NOT GOOD PRACTISE</mark>, this is only a hack so to simplify it we are creating secrets using yaml. 
A recommended approach would be to use [External Secrets](https://external-secrets.io/latest/provider/azure-key-vault/) together with Azure KeyVault. 

Get the primary connection string for the Servicebus namespce either from the Azure portal or run the command.

```shell
az servicebus namespace authorization-rule keys list -g [resourcegroup name] --namespace-name [servicebus namespace name] --name RootManageSharedAccessKey --query primaryConnectionString -o tsv
```

Add the following yaml to the deploy.yaml file. 

```shell
apiVersion: v1
kind: Secret
metadata:
  name: servicebus-secret
type: Opaque
stringData:
  ConnectionString: [servicebus connectionstring]
---
```

Apply the yaml and verify that the secret has been created.   

```shell
kubectl apply -f deploy.yaml
kubectl get secrets
kubectl describe secret servicebus-secret 
```

## Create TriggerAuthentication and ScaledJob

The last step is to create the ScaledJob and the TriggerAuthentication that will be used for KEDA to authenticate against Servicebus. 

```shell
apiVersion: keda.sh/v1alpha1
kind: TriggerAuthentication
metadata:
  name: azure-servicebus-auth
spec:
  secretTargetRef:
  - parameter: connection
    name: servicebus-secret
    key: ConnectionString
---
apiVersion: keda.sh/v1alpha1
kind: ScaledJob
metadata:
  name: eventproxyjob-scaledobject
spec:
  jobTargetRef:
    template:
      spec:
        nodeSelector:
          kubernetes.io/os: windows
          kubernetes.azure.com/os-sku: Windows2019
        containers:
        - name: eventproxyjob
          image: [Acr name].azurecr.io/[prefix]/eventproxy:1.0
          imagePullPolicy: Always
          env: 
          - name: ConnectionStrings__ServiceBus
            valueFrom:
              secretKeyRef:
                name: servicebus-secret
                key: ConnectionString
          - name: QueueSettings__QueueName
            value: [prefix]-proxy
        restartPolicy: Never
  pollingInterval: 10
  successfulJobsHistoryLimit: 3
  failedJobsHistoryLimit: 3
  maxReplicaCount: 3
  triggers:
  - type: azure-servicebus
    authenticationRef:
      name: azure-servicebus-auth
    metadata:
      queueName: [prefix]-proxy
      messageCount: "1"
---
```

## Test the solution

Use ServiceBusExplorer and add a message to the [prefix]-proxy queue. Monitor in OpenLens or use Kubectl to see how k8s creats a ScaledJob for each message and process it. View the reply queue for processed messages. 

Run the following command to monitor the progress or look under Pods in OpenLens.  

```shell
kubectl get pods -w
```

Use ServiceBusExplorer to send this message, make sure to set the ReplyTo header to [prefix]-reply under the Sender tab.



```json
{
  "JobId": "deb74855-196d-492b-a547-e941310e2a5d",
  "ProgId": "HelloWorld",
  "Origin": "BatchEmulator",
  "Dt": "2024-10-11T12:53:22.135851+02:00",
  "JobItems": [
    {
      "CallSequence": 1,
      "Method": "Start",
      "Parameters": [
        {
          "type": "String",
          "value": "John"
        }
      ]
    },
    {
      "CallSequence": 2,
      "Method": "Start",
      "Parameters": [
        {
          "type": "String",
          "value": "Jane"
        }
      ]
    }
  ]
}
```

Run the following command to look at the logs or view them in OpenLens. 

```shell
kubectl get pods
kubectl logs [podname]
```

Send five more messages and see how many pods are being shown in OpenLens. Also monitor the outcome of the windows where kubectl get pods -w is running.

## Test the solution with a badly formatted message

- Send a message where the replyTo queue is not specified and examine the logs
- Send a message where the body is not valid json and examine the logs.