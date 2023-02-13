# PEM to PPK Converter

Used by the Toolkit in order to convert (Prm) Key Pair to PPK when making an SSH connection to EC2 instances.

The solution is capable of being loaded and compiled in Visual Studio 2022. The final executable and debug symbols are compiled into `/Release/bin`

This application is not automatically produced. To include updates to this tool in the Toolkit, make a Release compile, then copy the exe and pdb file into the `/thirdparty` folder.

## Usage

```
PemToPPKConverter.exe "pem-path-input" "ppk-path-output"
```

The tool expects the quotation marks as part of the command line arguments.

