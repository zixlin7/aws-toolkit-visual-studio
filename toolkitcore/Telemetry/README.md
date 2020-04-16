# Telemetry Client

All AWS Toolkits send metrics data (when enabled) to a common service. The client used to communicate with this service is produced from a [definition file](/toolkitcore/Telemetry/client-source/telemetry-2017-07-25.normal.json) that is shared by all of the toolkits. The generator owned by [AWS SDK for .Net](https://github.com/aws/aws-sdk-net) is used to produce a C# generator that is compatible with the toolkit.

The definition file and all supporting files reside in [/toolkitcore/Telemetry/client-source](/toolkitcore/Telemetry/client-source). The generated client resides in [/toolkitcore/Telemetry/Client](/toolkitcore/Telemetry/Client). When changes are made to the service, the definition file is updated, and a new client is generated.

## Generating an Updated Client

The client only needs to be generated whenever there are changes to the [service definition file](/toolkitcore/Telemetry/client-source/telemetry-2017-07-25.normal.json).

-   Update the [service definition file](/toolkitcore/Telemetry/client-source/telemetry-2017-07-25.normal.json)
-   From a terminal, go to a temporary location and clone the AWS SDK for .Net

```
git clone https://github.com/aws/aws-sdk-net.git --depth 1
```

-   Compile the generator found in `generator\AWSSDKGenerator.sln` of the cloned repo.
-   Locate the compiled executable (for example: `generator\ServiceClientGenerator\bin\Release\ServiceClientGenerator.exe`), and copy its full path.
-   From a terminal, go to the root of this repo
-   Run `msbuild buildtools\TelemetryClient.proj /p:ClientGeneratorPath=SERVICE_GENERATOR_PATH` with appropriate substitution for `SERVICE_GENERATOR_PATH`
-   The generated client project and code should now be updated. Commit the changes to the service definition file and the generated files
-   You may now delete your temporary clone of aws-sdk-net
