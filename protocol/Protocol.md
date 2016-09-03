# Unattended Protocol

Heavily inspired by the [Google Omaha](https://github.com/google/omaha) protocol, actually a subset of the protocol, a subset of the subset [implemented by CoreOS](https://coreos.com/docs/coreupdate/custom-apps/coreupdate-protocol/).

## Updating Request

For each unattended configuration listing in the directory, an update request will be sent to the configured update server in the format:
```
<?xml version="1.0" encoding="UTF-8"?>
<request protocol="3.0">
 <app appid="e96281a6-d1af-4bde-9a0a-97b76e56dc57" version="1.0.0" track="beta" bootid="{fake-client-018}">
  <event eventtype="3" eventresult="2"></event>
 </app>
</request>
```
