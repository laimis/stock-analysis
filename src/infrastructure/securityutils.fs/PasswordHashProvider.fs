namespace securityutils

open System
open System.Security.Cryptography
open System.Text
open core.fs.Adapters.Authentication

// https://dotnetcodr.com/2017/10/26/how-to-hash-passwords-with-a-salt-in-net-2/
type PasswordHashProvider() =
    
    let generate (passwordBytes: byte[]) (saltBytes: byte[]) : HashAndSalt =
        let passwordWithSaltBytes = Array.concat [passwordBytes; saltBytes]
        use sha512 = SHA512.Create()
        let hashBytes = sha512.ComputeHash(passwordWithSaltBytes)
        { Hash = Convert.ToBase64String(hashBytes)
          Salt = Convert.ToBase64String(saltBytes) }
    
    member _.GenerateHashAndSalt(password: string, saltLength: int) : HashAndSalt =
        let passwordBytes = Encoding.UTF8.GetBytes(password)
        let saltBytes = RandomNumberGenerator.GetBytes(saltLength)
        generate passwordBytes saltBytes
    
    member _.GenerateHash(password: string, salt: string) : string =
        let passwordBytes = Encoding.UTF8.GetBytes(password)
        let saltBytes = Convert.FromBase64String(salt)
        let result = generate passwordBytes saltBytes
        result.Hash
    
    interface IPasswordHashProvider with
        member this.GenerateHashAndSalt password saltLength =
            this.GenerateHashAndSalt(password, saltLength)
        
        member this.GenerateHash password salt =
            this.GenerateHash(password, salt)

