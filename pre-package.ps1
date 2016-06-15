(Get-Content SecretsVault/SecretsVault.nuspec.template).replace('@build', $env:appveyor_build_version) | Set-Content SecretsVault/SecretsVault.nuspec

