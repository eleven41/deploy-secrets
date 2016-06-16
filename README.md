# secrets-vault

secrets-vault is a .NET library and command-line EXE to store key/value pairs in a secure vault.

* [Amazon KMS](http://aws.amazon.com/kms/) is used to encrypt/decrypt the values.
* [Amazon S3](http://aws.amazon.com/s3/) is used to store the vault values.

## Getting Started

[SecretsVault.template](CloudFormationTemplates/SecretsVault.template) is a CloudFormation template you can use to build a stack for your vault. The stack includes:

* An S3 bucket to store your key values. This bucket:
  * Enforces AES256 encryption-at-rest.
  * Enforces HTTPS when getting and putting values.
* A KMS key to encrypt and decrypt the data.
* Two IAM Managed Policies to assign to your users and roles:
  * One "Admin" policy with "put" and "delete" permissions.
  * One "User" policy with "get" permissions.

## .NET Assembly to Use In Your Application

### Installation

Install from NuGet

    Install-Package secrets-vault

### Sample Code

```csharp
var log = new ConsoleLog();
Config config = Config.LoadFromAppSettingsAndVerify(false, log);
var client = new SecretsVaultClient(config);
string value = await client.GetAsync("my-key", log);
```

## Command-Line Utility

### Installation

1. Go to the [Releases](https://github.com/eleven41/secrets-vault/releases) page of this repo.
2. Download the "Vault-{build}.zip" file and unzip it.
3. Configure the vault by editing Vault.exe.config ([see below](#configuration)).
4. Run the `vault.exe` program.

### Command Line

`Vault.exe` operates like `git`, such that it uses verbs to drive the operation followed by flags for the verb.

To get help started, type:

```
$ vault help
```

The following command will retrieve the value for a key called "my-key".

```
$ vault get --key my-key
```

To put a value in the vault:

```
$ vault put --key my-key --value my-value
```

If your value has spaces, enclose it in quotes:

```
$ vault put --key value-with-spaces --value "this value has spaces"
```

If your value is empty, enclose it in quotes:

```
$ vault put --key empty-value --value ""
```



#### Verbs

* create-batch-file - Creates a skeleten batch file to mass load keys into the vault using `put-batch`. The file is a JSON file.
* delete - Deletes a key and value from the vault.
* get - Retrieves a value from the vault.
* put - Puts a value in the vault.
* put-batch - Puts many values into the vault from a file. The file is a JSON file and should be generated using
the `create-batch-file` verb.

## Configuration

### Vault Configuration

Configuration of the vault (for both the assembly and the command-line utility) is done using `<appSettings>` in the `Web.Config` and/or `App.Config` files.

The following `appSettings` should be configured:

* vault:KmsKeyId - **(Conditional)** The ID of the key used to encrypt values. This usually looks like a GUID. Only required to put key/values.
* vault:BucketName - **(Required)** Name of the bucket to store the key/values.
* vault:Prefix - (Optional) Prefix to use for the S3 object keys when stored in the bucket. For example, "settings/".

```xml
<appSettings>
  <add key="vault:KmsKeyId" value="<your KMS key ID>" />
  <add key="vault:BucketName" value="<your bucket name>" />
  <add key="vault:Prefix" value="<your optional prefix>" />
</appSettings>
```

### AWS Client Configuration

This library uses [aws-sdk-net](https://github.com/aws/aws-sdk-net) to AWS services. Please see that repo for
AWS client configuration (such as access keys, etc.)

## AWS IAM Policies

The AWS IAM user or role used to **put and delete** values, requires the following policy:

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "AllowPut",
            "Effect": "Allow",
            "Action": [
                "kms:Encrypt",
                "s3:DeleteObject",
                "s3:PutObject"
            ],
            "Resource": [
                "*"
            ]
        }
    ]
}
```

The AWS IAM user or role used to **get** values, requires the following policy:

```json
{
    "Version": "2012-10-17",
    "Statement": [
        {
            "Sid": "AllowGet",
            "Effect": "Allow",
            "Action": [
                "kms:Decrypt",
                "s3:GetObject"
            ],
            "Resource": [
                "*"
            ]
        }
    ]
}
```

## Tips

To improve security of your S3 buckets and KMS keys:

* Add statements to the S3 bucket policy and KMS key policy that restrict access to specific principals. These can only be IAM users, IAM roles, and AWS account roots.

