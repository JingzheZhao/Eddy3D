WindTunnel
==========

Installation
------------

-   [Download and install Docker](https://download.docker.com/win/stable/Docker%20for%20Windows%20Installer.exe)

- Windows + R -> compmgmt.msc -> Check whether your user account is added to "Local users and groups" "groups" "docker-users"

- RESTART

- Check if Hyper-V Virtualization is enabled in BIOS of your machine

If not, open PowerShell as administrator and

Enable Hyper-V with

> dism.exe /Online /Enable-Feature:Microsoft-Hyper-V /All

or enable Hypervisor with

> bcdedit /set hypervisorlaunchtype auto

Now restart the system and try again.

- Open commandline with elevated credentials

- Start eddy with one of the testFiles

- Done


# Troubleshooting
---
> Solution exception: Could not load file or assembly...

Navigate to the folder you downloaded the files to and tick the 'unblock' box in the file properties.

