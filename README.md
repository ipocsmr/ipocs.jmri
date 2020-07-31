# IPOCS.JMRI
This project is a protocol translation server. It translates the IPOCS protocol to MQTT as understood by [JMRI](jmri.net) and vice versa.

## Installation
You can either clone the repository and build yourself using `dotnet build` or download a self-contained package from GitHub Releases.

For this application to be usable, you need a MQTT broker and JMRI to be set up and working.

## Configuring / Usage

IPOCS.JMRI has no command like parameters, all configuration is done by editing the `appconfig.json` file that resides in the same folder as the executable.

Example configuration file:

```
{
  "jmriConfig": "layout.xml",
  "ipocsConfig": "config.xml",
  "channel": "/trains",
  "mqttHost": "localhost"
}
```

Settings explained:

* jmriConfig - JMRI yard layout confiugration file in XML format
* ipocsConfig - XML file with the IPOCS object definitions
* channel - MQTT topic prefix used by JMRI
* mqttHost - resolvable hostname or IP address of MQTT broker to  be used
