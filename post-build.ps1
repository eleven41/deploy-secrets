7z a Vault-$env:appveyor_build_version.zip $env:appveyor_build_folder\Vault\bin\$env:configuration\*.dll
7z a Vault-$env:appveyor_build_version.zip $env:appveyor_build_folder\Vault\bin\$env:configuration\Vault.exe
7z a Vault-$env:appveyor_build_version.zip $env:appveyor_build_folder\Vault\bin\$env:configuration\Vault.exe.config