# Instructions for part 1, containerizing the application

## Part 1 - Description

- Understand docker architecture, learn about docker images, layers and containers.
- Understand what a Dockerfile is and the most common instructions like `FROM`, `COPY`, `ADD`, `RUN`, `WORKDIR`, `USER`, `CMD`
- Create a multistage Dockerfile to build and package an existing .NET application that utilizes COM+ components written in C++
- Use docker desktop to build and run containers locally.
- Create queues in Azure Servicebus and connect to them from the container

## Tooling used in part 1

- [Docker Desktop](https://docs.docker.com/desktop/install/windows-install/): To build and run applications locally
- [ServiceBus explorer](https://learn.microsoft.com/en-us/azure/service-bus-messaging/explorer): To send and receive messages on Azure Servicebus. The servicebus explorer can be accessed directly in the [Azure portal]((https://learn.microsoft.com/en-us/azure/service-bus-messaging/explorer)) or installed as a [local application](https://github.com/paolosalvatori/ServiceBusExplorer)

## Introduction to the application

TODO: INSERT ARCHITECTURAL DIAGRAM

## Understand containers

Containers are a lightweight, portable, and efficient way to run applications. They allow you to run an application and its dependencies in an isolated and resource limited process.

- **Isolation:** Containers provide process and filesystem isolation. Each container runs in its own environment, which includes its own filesystem, system libraries, and process space. This means that a containerized application cannot see or affect other applications or the host system, providing a secure and consistent runtime environment.

- **Resource Limits:** Containers can be limited in terms of the resources they use, such as CPU, memory, and disk space. This ensures that an application running in a container does not consume more than its fair share of resources, which is particularly important when running multiple containers in a shared environment like Kubernetes.

- **Efficiency:** Unlike virtual machines, as containers share the host system’s kernel no virtualization layer is needed, which makes them much more efficient in terms of system resources. They start up quickly and have a smaller footprint, making them ideal for deploying microservices and scalable cloud-native applications.

- **Portability:** Containers are designed to be portable. A containerized application will run the same way regardless of where it’s deployed, whether it’s on a developer’s laptop, a test environment, or a production server in the cloud. This makes containers an excellent choice for continuous integration and continuous deployment (CI/CD) pipelines.

- **Consistency:** By packaging an application and its dependencies together, containers ensure that the application will run consistently across different environments. This eliminates the “it works on my machine” problem and makes it easier to develop, test, and deploy applications.

### Containers and images

A (container) image is a blueprint for a container, while a container is a running instance of an image. Images are static and unchangeable, while containers are dynamic and can be created, started, stopped, and deleted as needed.

During this lab we will build an image from an existing .NET application and then use this image to create containers (i.e running the application)

### Images and Layers

Container images use a layered filesystem, where each layer represents a set of file changes or additions. These layers are stacked on top of each other to form the final image. Below are some common terminology related to image layers:

- **Base Layer:** The bottom layer is typically a base image, for example a base system for building or running .NET applications.

- **Intermediate Layers:** Above the base layer, there can be multiple intermediate layers. Each layer represents a change to the filesystem, such as installing a package, adding application code, or setting environment variables.

- **Layer Sharing:** Layers can be shared between images. If two images have the same layers up to a certain point, they can share those layers, which makes pulling and pushing images more efficient.

- **Copy-on-Write:** When a container is created from an image, a new writable layer is added on top of the image’s layers. All changes made to the running container, such as creating new files or modifying existing ones, are written to this layer. This layer is only part of the running container, not the image. When the running container is stopped and disposed this layer is removed.

### Dockerfile anatomy

A [Dockerfile](https://docs.docker.com/build/concepts/dockerfile/) is a series of instructions on how to build a Docker image. It’s a text file that contains instructions which are executed in sequence to assemble an image.

Each instruction creates a new layer in the final image. The Instructions are typically written in upper case for readability. In this lab we will use the following commands

- **FROM**: Specifies the base image to use. This is often the first instruction in a Dockerfile.
- **COPY**: Copies files from the host machine into the image.
- **ADD**: Similar to `COPY`, but also supports URLs and tar file extraction.
- **RUN**: Executes a command in the shell and creates a new layer with the results.
- **WORKDIR**: Sets the working directory for any `RUN`, `CMD`, `ENTRYPOINT`, `COPY`, and `ADD` instructions that follow it.
- **USER**: The USER instruction in a Dockerfile is used to set the username or UID  to use when running the image and for any `RUN`, `CMD`, and `ENTRYPOINT` instructions that follow it in the Dockerfile
- **CMD**: Defines the command that will run when the container is started

### Multistage build

A [multistage build](https://docs.docker.com/build/building/multi-stage/) is a feature in Docker that allows you to use multiple `FROM` statements in a single Dockerfile, each representing a separate stage of the build process. This helps in creating lean and efficient Docker images as builds can be based on an image containing SDKs and other build tools and the resulting binaries and dependencies can be copied over to streamlined runtime image.

## Containerizing the application

The application code itself will not be modified as part of the containerization. In this lab the application will be built from source using docker, meaning that no developer tools (except docker desktop) are needed locally.

### Create the dockerfile

A [Dockerfile](src/Dockerfile) for building the application and creating the final image can be found in the [src/](src/) directory

This Dockerfile sets up a multistage build where the application is built in the first stage and the runtime environment is prepared in the second stage. Take some time to familiarize yourself with the Dockerfile content. Below is a detailed summary of the provided Dockerfile

#### Escape Directive

- The #escape=\ directive sets the escape character for the Dockerfile to backtick (`), which is useful for Windows paths, which makes extensive use of the default escape character (backslash).
Build Stage:

#### Build stage

- The build stage starts with the .NET Framework SDK image `mcr.microsoft.com/dotnet/framework/sdk:4.8.1.`
- Working Directory: Sets the working directory to `/app`.
- Restore Dependencies: Copies the `.csproj` files into the image and runs `msbuild` to restore the dependencies.
- Build Application: Copies the source code into the image and runs `msbuild` to build the application, outputting to `C:\app\bin`.
- Separate Executables: Moves the .exe and .exe.config files from `C:\app\bin` to `C:\app\program` to take advantage of Docker layer caching.

#### Runtime Stage

- Base Image: The runtime stage starts with the .NET Framework runtime image `mcr.microsoft.com/dotnet/framework/runtime:4.8.1`.
- Install C++ Redistributable: Downloads and installs the C++ Redistributable 2015-2019, which is required by the application.
- Working Directory: Sets the working directory to `/app`.
- Register COM Component: Copies the COM component `HelloWorldCom.dll` into the image and registers it using `regsvr32`.
- Add Application Files: Copies the dependencies and main executable from the build stage into the runtime image.
- Set User: Switches to the `ContainerUser` user for running the application.
- Command: Defines the command to run when the container starts, which is `BillingBatchEventProxy.exe`.

### Building the container image

To create an image the `docker build` command is used.

<mark>PLEASE NOTE: Before running the command, make sure that docker desktop is started and is running in windows container mode</mark>

By executing the below command in the same folder as the `Dockerfile` a new image called `eventproxy:1.0` is created.

```shell
docker build -t eventproxy:1.0 .
```

### Explore the final image

Once the final image is created this image can be used in the `docker run` command to create a container based on the image.

The below command launchs a new container based on the `eventproxy:1.0` image and executed the command `cmd` inside.
The [`-i` flag](https://docs.docker.com/reference/cli/docker/container/run/#interactive) keeps the container's STDIN open and let's you send input to the container.
The [`-t` flag](https://docs.docker.com/reference/cli/docker/container/run/#tty) connects your terminal to the IO stream of the container.

Combining the `i` and `t` flag and starting a shell in the container using `cmd` allows you to browse the filesystem inside the container as well as launch processes inside it. This is sometimes refered to as the "ssh" of containers.

Take some time to explore the container
- What does the filesystem look like (what files are present)?
- What processes are running?

```shell
docker run -it eventproxy:1.0 cmd
```

## Run the containerized application locally

The final image contains all the runtime dependencies for the application, thus it can be launched on any system capable of running windows containers. In the section the container will be launched (with configuration injected) and the application inside will receive and send messages on queues in an Azure Servicebus namespace  

### Create Azure Service Bus queues

Azure servicebus queues can be created either using the [Azure portal](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quickstart-portal#create-a-queue-in-the-azure-portal) or the azure cli as in the examples below

```shell
az login
az servicebus queue create --resource-group [resourcegroup name] --namespace-name [servicebus namespace name] --name [prefix]-proxy
az servicebus queue create --resource-group [resourcegroup name] --namespace-name [servicebus namespace name] --name [prefix]-reply
```

### Retrieve the connecting string for the servicebus

Get the primary connection string for the Servicebus namespace either from the Azure portal or run the below command.

```shell
az servicebus namespace authorization-rule keys list -g [resourcegroup name] --namespace-name [servicebus namespace name] --name RootManageSharedAccessKey --query primaryConnectionString -o tsv
```

### Run the containerized application

If the container is started without a command supplied to docker run, it will default to the command specified in the `Dockerfile`, which is `BillingBatchEventProxy.exe`

However the application requires some configuration to run. It needs to know what queue to listen on and credentials to access the servicebus to send and receive messages. The application expects this configuration to be available in environment variables. Environment variables can be set in the container by passing them to docker run using the `-e` flag

```shell
docker run -e ConnectionStrings__ServiceBus="[ConnectionString]" -e QueueSettings__QueueName="[prefix]-proxy" eventproxy:1.0
```

Once the application has started you should see the following logs outputted

```
Starting BillingBatchEventProxy listen on queue [[prefix]-proxy]
```

Use ServiceBusExplorer and add a message to the `[prefix]-proxy` queue using either the [portal](https://learn.microsoft.com/en-us/azure/service-bus-messaging/explorer#send-a-message-to-a-queue-or-topic) or the [open source client](https://github.com/paolosalvatori/ServiceBusExplorer)

- An example message body can be found in [example-message.json](../example-message.json)
- Set the `replyTo` header to `[prefix]-reply`

Monitor the log output and check the `[prefix]-reply` queue for processed messages.

<mark>PLEASE NOTE: The application will process one message from the queue and then exit. If you want to process multiple messages you need to launch one container per message.</mark>

## Simplify orchestration with docker compose

Docker Compose is a tool that allows you to define and manage multi-container Docker applications. It uses a YAML file to configure the services including how to build them and what environment variables to pass in at startup.

The [docker compose](../src/docker-compose.yml) file defines a service that will be built from a Dockerfile in the current directory, with environment variables loaded from config.env, and a label set for Visual Studio debugging. The image name can be optionally prefixed with a registry specified by the DOCKER_REGISTRY environment variable. Here’s a detailed explanation of the configuration:

**Service Definition:**
- `billingbatcheventproxy:` This is the name of the service. It represents a container that will be run.

**Image:**
- `image: ${DOCKERR​EGISTRY−eventproxy}`: The image to use for the container. The `${DOCKER_REGISTRY-}` syntax allows for an optional registry prefix to be specified via an environment variable. IfDOCKER_REGISTRY` is not set, it defaults to an empty string.

**Labels:**
- `com.microsoft.visual-studio.project-name: "BillingBatchEventProxy":` This label is used by Visual Studio to identify the project associated with the container. It’s helpful for debugging purposes.

**Environment Variables:**

`env_file: - config.env`: The env_file option specifies a file from which to read environment variables. The variables defined in config.env will be passed to the container.

**Build Configuration:**

- `build`: This section defines the build context and Dockerfile to use for building the image.
  - `context: .`: The build context is set to the current directory.
  - `dockerfile: Dockerfile`: The Dockerfile in the build context directory will be used for building the image.

### Create a config.env

The config.en file is used to pass environment variables into the container. Place a file under the [src/](../src/) folder named `config.env` with the following content

```shell
ConnectionStrings__ServiceBus="[ConnectionString]"
QueueSettings__QueueName=[prefix]-proxy
```

### Build the container image using docker compose

```shell
docker compose build
```

### Run the container using docker compose

```shell
docker compose up
```


## Debug in Visual Studio

The containerized application can be debugged from within Visual Studio, please follow the instructions below and set a few breakpoints and step through the code:

- Open the solution in Visual Studio `Billing.Batch.EventProxy.PoC.sln`
- Set the solution configuration to `Debug`
- Select `docker-compose` as the startup item
- Start Debugging `Debug ->Start debugging` or hit `F5`