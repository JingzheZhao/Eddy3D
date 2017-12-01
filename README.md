WindTunnel
==========

Installation
------------

-   [Download and install Docker](https://download.docker.com/win/stable/Docker%20for%20Windows%20Installer.exe)

- Windows + R -> compmgmt.msc -> Check whether your user account is added to "Local users and groups" "groups" "docker-users"

- RESTART

- Check if Hyper-V Virtualization is enabled in BIOS of your machine

(
Open PowerShell as administrator and

Enable Hyper-V with

dism.exe /Online /Enable-Feature:Microsoft-Hyper-V /All
or

Enable Hypervisor with

bcdedit /set hypervisorlaunchtype auto
Now restart the system and try again.
)

- Open commandline with elevated credentials

- Pull docker container 

> docker pull hfdresearch/swak4foamandpyfoam:latest-v4.1

- Done
