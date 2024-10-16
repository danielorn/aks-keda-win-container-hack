#escape=`

###############################################################################
# Build .NET application 
###############################################################################
# This first build stage is based on top of the .NET Framework SDK Image
# https://hub.docker.com/r/microsoft/dotnet-framework-sdk
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8 as build
WORKDIR /app

# Restore dependencies
# To restore the dependencies the *.csproj files must first be copied into the
# image, then msbuild can be run towards the restore target
COPY BillingBatchEventProxy/*.csproj ./BillingBatchEventProxy/
COPY BatchSupport/*.csproj ./BatchSupport/
RUN msbuild ./BillingBatchEventProxy/BillingBatchEventProxy.csproj -t:Restore

# Build application
# To build the application the source code is copied into the image and msbuild
# is executed
COPY BillingBatchEventProxy/ ./BillingBatchEventProxy/
COPY BatchSupport/ ./BatchSupport/
RUN msbuild ./BillingBatchEventProxy/BillingBatchEventProxy.csproj /p:outDir=C:\app\bin `
    /p:Configuration=Release /p:DebugSymbols=false /p:DebugType=None 

# Separate .exe and dependencies in separate folders
# The output of the build is located in C:\app\bin. This folder contains all
# downloaded dependencies as well as the main executables. The below moves the
# .exe and .exe.config files to a separate folder C:\app\program. This is to 
# take advantage of the Docker layer caching mechanism in the runtime image.
RUN mkdir C:\app\program
RUN Get-ChildItem -Path "C:\app\bin" -Include *.exe, *.exe.config |` 
        ForEach-Object { Move-Item -Path $_.FullName -Destination "C:\app\program" }

###############################################################################
# Create Runtime image
###############################################################################
# This second (and final) build stage is based on top of the .NET Framework runtime image
# https://hub.docker.com/r/microsoft/dotnet-framework-runtime
FROM mcr.microsoft.com/dotnet/framework/runtime:4.8 as runtime

# Install C++ Redist 2015-2019
# The application depends on a complus component written in c++, thus the msvc
# runtime libraries must be installed
ADD https://aka.ms/vs/17/release/vc_redist.x86.exe vc_redist.x86.exe 
RUN vc_redist.x86.exe /install /passive /norestart

WORKDIR /app

# The com component (HelloWorldCom.dll) is copied into the final image
# and registered by regsvr32
COPY HelloWorldCom/* complus/
RUN START /WAIT cmd /c %WINDIR%\SysWOW64\regsvr32 /s complus/HelloWorldCom.dll

# Add dependencies and main executable from the build stage. 
# Since the dependencies are likely to change less frequently than the application
# code, this layer can likely be cached often and the difference in image size
# between two different image version will be less, leading to faster load times.
COPY --from=build C:\app\bin .
COPY --from=build C:\app\program .

# Define the command to run when the container starts
CMD ["BillingBatchEventProxy.exe"]