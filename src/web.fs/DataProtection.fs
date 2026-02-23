module web.DataProtection

open System.IO
open Microsoft.AspNetCore.DataProtection
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection

let private dataProtectionKeyName = "DataProtectionKey"

let private getDataProtectionKeyOrFail (configuration: IConfiguration) =
    let key = configuration.GetValue<string>(dataProtectionKeyName)
    if System.String.IsNullOrEmpty key then
        failwith $"{dataProtectionKeyName} environment variable is required for data protection"
    key

let private getKeysDirectory () =
    let dir = DirectoryInfo "keys_secret" // _secret suffix matches .gitignore pattern
    if not dir.Exists then
        dir.Create()
    dir

let configureDataProtection (services: IServiceCollection) (configuration: IConfiguration) =
    let key = getDataProtectionKeyOrFail configuration

    let keysDirectory = getKeysDirectory ()

    let creationTimestamp   = System.DateTimeOffset.Now.ToString "yyyy-MM-ddTHH:mm:ss.fffffffZ"
    let expirationTimestamp = System.DateTimeOffset.Now.AddMonths(3).ToString "yyyy-MM-ddTHH:mm:ss.fffffffZ"

    let xml =
        $"""<?xml version="1.0" encoding="utf-8"?>
<key id="b883cf52-5cf5-47d8-ad92-c50d2c89b566" version="1">
  <creationDate>{creationTimestamp}</creationDate>
  <activationDate>{creationTimestamp}</activationDate>
  <expirationDate>{expirationTimestamp}</expirationDate>
  <descriptor deserializerType="Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel.AuthenticatedEncryptorDescriptorDeserializer, Microsoft.AspNetCore.DataProtection, Version=10.0.0.0, Culture=neutral, PublicKeyToken=adb9793829ddae60">
    <descriptor>
      <encryption algorithm="AES_256_CBC" />
      <validation algorithm="HMACSHA256" />
      <masterKey p4:requiresEncryption="true" xmlns:p4="http://schemas.asp.net/2015/03/dataProtection">
        <!-- Warning: the key below is in an unencrypted form. -->
        <value>{key}</value>
      </masterKey>
    </descriptor>
  </descriptor>
</key>
"""

    let path = Path.Combine(keysDirectory.FullName, "key.xml")
    File.WriteAllText(path, xml)

    services
        .AddDataProtection()
        .DisableAutomaticKeyGeneration()
        .PersistKeysToFileSystem(keysDirectory)
    |> ignore
