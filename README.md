# Unattended

A simple update library for .NET and Mono.

## Getting Started



## How Unattended works

1. When the Unattended service daemon is started, the application executable is run using a subprocess.
2. The update configurations are loaded from the specified directory.
3. Update configurations specify all libraries and files that should be checked for updates as well as how often and where.
4. According to the update schedule, the service daemon will check if updates are available.
5. If an update is found, the daemon will create a new 'active' directory for the application and make a clone of the current files into that directory.
6. Once the clone is completed, the updated files are inserted into the newly created 'active' directory.
7. Using [ioRPC](https://www.github.com/ProjectLimitless/ioRPC), Unattended checks with the application if it is Ok to restart it with the update applied.
8. Once the application returns the Ok to Unattended, the application if stopped and restarted.
9. If the application fails to start after an update, the previous version is started and the application if notified of the failure with details.

To learn more about update configurations, see the [Configurations wiki page](TODO: insert link)

*The update strategy is inspired by how [CoreOS Updates work](https://coreos.com/os/docs/latest/update-strategies.html)*

---
*A part of Project Limitless*

[![Project Limitless](https://www.donovansolms.com/downloads/projectlimitless.jpg)](https://www.projectlimitless.io)

[https://www.projectlimitless.io](https://www.projectlimitless.io)
