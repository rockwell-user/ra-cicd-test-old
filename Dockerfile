# Use a Windows Server Core base image
FROM mcr.microsoft.com/windows/servercore:ltsc2019

# Metadata
# LABEL maintainer="Your Name <your.email@example.com>"

# Download Docker MSI installer
ADD https://download.docker.com/win/stable/Docker%20Desktop%20Installer.exe C:/DockerInstaller.exe

# Install Docker using the MSI installer
RUN C:/DockerInstaller.exe --quiet

# Install any necessary packages to execute an .exe file
# For example, you can install the .NET Core runtime
ADD https://download.visualstudio.microsoft.com/download/pr/834b6a47-d888-4b98-a7a4-6b417d2718ea/1f07d8f71a4b501a4adbf43dbdbe17c2/windowsdesktop-runtime-5.0.11-win-x64.exe C:/dotnet-runtime.exe
RUN C:/dotnet-runtime.exe /quiet /norestart

# Cleanup
RUN del C:/DockerInstaller.exe C:/dotnet-runtime.exe

# Optional: Set environment variables
# ENV PATH="C:\Program Files\Docker"

# # Use the appropriate base image for Windows
# FROM mcr.microsoft.com/windows/servercore:ltsc2019 AS builder

# # Install Docker
# SHELL ["powershell", "-Command", "$ErrorActionPreference = 'Stop'; $ProgressPreference = 'SilentlyContinue';"]

# RUN Invoke-WebRequest -Uri https://download.docker.com/components/engine/windows-server/20H2/docker-20.10.11.zip -OutFile C:\docker.zip ; \
#     Expand-Archive -Path C:\docker.zip -DestinationPath C:\docker

# RUN $env:PATH += ';C:\docker' ; \
#     [Environment]::SetEnvironmentVariable('Path', $env:PATH, [EnvironmentVariableTarget]::Machine)

# # Use a second stage to create the final image
# FROM mcr.microsoft.com/windows/servercore:ltsc2019

# # Copy Docker binaries from the builder stage
# COPY --from=builder /docker /docker

# # Set environment variable for Jenkins home
# ENV JENKINS_HOME C:\\jenkins_home

# # Expose Docker daemon port
# EXPOSE 2375

# # Start Docker daemon
# CMD ["cmd", "/c", "C:\\docker\\dockerd.exe", "-H", "npipe://", "--register-service"]
