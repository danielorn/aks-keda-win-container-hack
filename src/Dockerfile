#escape=`

###############################################################################
# Build .NET application 
###############################################################################
FROM mcr.microsoft.com/dotnet/framework/sdk:4.8.1 as build
WORKDIR /app

# Restore dependencies
COPY BillingBatchEventProxy/*.csproj ./BillingBatchEventProxy/
COPY BatchSupport/*.csproj ./BatchSupport/
RUN msbuild ./BillingBatchEventProxy/BillingBatchEventProxy.csproj -t:Restore

# Build project
COPY BillingBatchEventProxy/ ./BillingBatchEventProxy/
COPY BatchSupport/ ./BatchSupport/
RUN msbuild ./BillingBatchEventProxy/BillingBatchEventProxy.csproj /p:Configuration=Release /p:DebugSymbols=false /p:DebugType=None /p:outDir=C:\app\bin 

# Separate .exe and dependencies in separate folders
RUN mkdir C:\app\out\program
RUN for /r "C:\app\bin" %x in (*.exe, *.exe.config) do move "%x" "C:\app\out\program"

###############################################################################
# Create Runtime image
###############################################################################
FROM mcr.microsoft.com/dotnet/framework/runtime:4.8.1 as runtime

# Install C++ Redist 2015-2019
ADD https://aka.ms/vs/17/release/vc_redist.x86.exe vc_redist.x86.exe 
RUN vc_redist.x86.exe /install /passive /norestart

WORKDIR /app

# Copy and Register COM component
COPY HelloWorldCom/* complus/
RUN START /WAIT cmd /c %WINDIR%\SysWOW64\regsvr32 /s complus/HelloWorldCom.dll

# Add dependencies
COPY --from=build C:\app\bin .

# Add main executable
COPY --from=build C:\app\out\program .

# Switch to container user
USER ContainerUser

# Set container entrypoint
ENTRYPOINT ["BillingBatchEventProxy.exe"]